using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatchingLib;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;
using System.Configuration;
using BaseHelper;

namespace MatchingCoreTest
{
    class Resp: ProcessOrderResult
    {
        public void ResetObj()
        {
            this.Success = false;
            this.Comment = string.Empty;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    class Tx : Transaction
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    class Req : RequestToMatching { }
    class ReqPool
    {
        static objPool<Req> Pool = new objPool<Req>(() => new Req() { order = new Order() });
        public static Req Checkout()
        {
            return Pool.Checkout();
        }
        public static void Checkin(Req req)
        {
            req.order.ResetObj();
            Pool.Checkin(req);
        }
    }
    class TxPool
    {
        static objPool<Tx> Pool = new objPool<Tx>(() => new Tx());
        public static Tx Checkout()
        {
            return Pool.Checkout();
        }
        public static void Checkin(Tx tx)
        {
            tx.ResetObj();
            Pool.Checkin(tx);
        }
    }
    class RespPool
    {
        static objPool<Resp> Pool = new objPool<Resp>(() => new Resp() { order = new Order() });
        public static Resp Checkout()
        {
            return Pool.Checkout();
        }
        public static void Checkin(Resp resp)
        {
            resp.order.ResetObj();
            resp.ResetObj();
            Pool.Checkin(resp);
        }
    }
    class Program
    {
        //static RabbitMqOut requestOut { get; set; }
        //static RabbitMqOut respOut { get; set; }
        //static RabbitMqOut txOut { get; set; }
        //static RabbitMqIn RespIn { get; set; }
        //static RabbitMqIn TxIn { get; set; }
        //static RabbitMqIn requestIn { get; set; }
        static SimpleTcpClient client { get; } = new SimpleTcpClient();
        static bool bIsRunning { get; set; } = true;
        static int rType { get; set; } = 0;
        static int RpsLimit = 10000;
        static int Rps = 0;
        static int Recps = 0;
        static int Txps = 0;
        static int failPs = 0;
        static bool Ismistake(int[] a)
        {
            if (a[5] == 4)
            {
                return true;
            }
            for (int i = 0; i < 5; i++)
            {
                if ((a[i] == 5 && a[i + 1] == 3) || (a[i] == 3 && a[i + 1] == 5))
                {
                    return true;
                }
            }
            return false;
        }
        static void Main(string[] args)
        {
            //string reqQueue = "ReqToMatchingCore";
            //string respQueue = "RespFromMatchingCore";
            //string txQueue = "TxFromMatchingCore";
            if (args.Length != 2) return;
            //string mqUriReq = ConfigurationManager.AppSettings["uriReq"].ToString();
            //string mqUriResp = ConfigurationManager.AppSettings["uriResp"].ToString();
            //string mqUriTx = ConfigurationManager.AppSettings["uriTx"].ToString();
            string RequestReceiverIP = ConfigurationManager.AppSettings["RequestReceiverIP"];
            int RequestReceiverPort = int.Parse(ConfigurationManager.AppSettings["RequestReceiverPort"]);
            string TxReceiverIP = ConfigurationManager.AppSettings["TxReceiverIP"];
            int TxReceiverPort = int.Parse(ConfigurationManager.AppSettings["TxReceiverPort"]);
            rType = int.Parse(args[0]);
            List<Task> tasks = new List<Task>();

            Task.Factory.StartNew(() => ResetRps());
            if (rType == 0 || rType == 1)
            {
                RpsLimit = int.Parse(args[1]);
                //requestOut = new RabbitMqOut(mqUriReq, reqQueue);
                client.Connect(RequestReceiverIP, RequestReceiverPort);
                tasks.Add(Task.Factory.StartNew(() => InitOrders()));
                tasks.Add(Task.Factory.StartNew(() => RespReceiver()));
            }
            else if (rType == 2)
            {
                ushort pre = ushort.Parse(args[1]);
                RespIn = new RabbitMqIn(mqUriResp, respQueue, pre);
                TxIn = new RabbitMqIn(mqUriTx, txQueue, pre);

                RespIn.BindReceived(MqRespInHandler);
                TxIn.BindReceived(MqTxInHandler);
            }
            else if (rType == 3)
            {
                ushort pre = ushort.Parse(args[1]);
                requestIn = new RabbitMqIn(mqUriReq, reqQueue, pre);

                requestIn.BindReceived(MqReqInHandler);
            }
            else if (rType == 4)
            {
                RpsLimit = int.Parse(args[1]);
                respOut = new RabbitMqOut(mqUriResp, respQueue);
                tasks = Task.Factory.StartNew(() => FailReply());
                //Task.Factory.StartNew(() => FailReply());
            }
            else if (rType == 5)
            {
                RpsLimit = int.Parse(args[1]);
                txOut = new RabbitMqOut(mqUriTx, txQueue);
                tasks = Task.Factory.StartNew(() => txReply());
                //Task.Factory.StartNew(() => FailReply());
            }
            //Console.ReadKey();
            //Req req = ReqPool.Checkout();
            //req.order.et = Order.ExecutionType.Limit;
            //req.order.id = Guid.NewGuid().ToString("N");
            //req.order.u = Guid.NewGuid().ToString("N");
            //req.order.t = Order.OrderType.Sell;
            //req.order.p = 850;
            //req.order.v = 500;
            //requestOut.Enqueue(req);
            //ReqPool.Checkin(req);
            Console.ReadKey();
            bIsRunning = false;
            if ((rType == 0 || rType == 1) && tasks != null)
                tasks.Wait();
            if (rType == 0 || rType == 1)
            {
                requestOut.Shutdown();
            }
            else if (rType == 2)
            {
                RespIn.Shutdown();
                TxIn.Shutdown();
            }
            else if (rType == 3)
            {
                requestIn.Shutdown();
            }
            else if (rType == 4)
            {
                tasks.Wait();
                respOut.Shutdown();
            }
            else if (rType == 5)
            {
                tasks.Wait();
                txOut.Shutdown();
            }
        }

        private static void RespReceiver()
        {
            while(bIsRunning)
            {

            }
        }

        private static void txReply()
        {
            while (bIsRunning)
            {
                for (int i = 0; i < 1000; i++)
                {
                    if (!bIsRunning)
                        return;
                    if (Rps < RpsLimit)
                    {
                        Tx tx = TxPool.Checkout();
                        //txOut.Enqueue(tx);
                        Rps++;
                    }
                }
            }
        }

        private static void FailReply()
        {
            ParallelOptions option = new ParallelOptions();
            option.MaxDegreeOfParallelism = 12;
            Req req = new Req();
            req.order = new Order();
            var binObj = req.ToBytes();
            while (bIsRunning)
            {
                //Parallel.For(0, 1000, option, item =>
                for (int i = 0; i < 1000; i++)
                {
                    if (!bIsRunning)
                        return;
                    if (Rps < RpsLimit)
                    {
                        var rejObj = Resp.ConstructRejectBuffer();
                        //respOut.Enqueue(rejObj.bytes);
                        //Resp.CheckIn(rejObj);
                        //Interlocked.Increment(ref Rps);
                        Rps++;
                    }
                }
                //);
            }
        }

        private static void MqReqInHandler(object sender, BasicDeliverEventArgs e)
        {
            var bytes = e.Body;
            Req req = ReqPool.Checkout();
            //Interlocked.Increment(ref Rps);
            ReqPool.Checkin(req);
            //TxIn.MsgFinished(e);
        }

        private static void ResetRps()
        {
            while(bIsRunning)
            {
                Thread.Sleep(1000);
                int tmp1 = Interlocked.Exchange(ref Rps, 0);
                int tmp2 = Interlocked.Exchange(ref failPs, 0);
                int tmp3 = Interlocked.Exchange(ref Recps, 0);
                int tmp4 = Interlocked.Exchange(ref Txps, 0);
                Console.WriteLine("Rps:" + tmp1);
                Console.WriteLine("Fail:" + tmp2);
                Console.WriteLine("Recps:" + tmp3);
                Console.WriteLine("Txps:" + tmp4);
            }
        }

        private static void InitOrders()
        {
            int gType = 0;
            ParallelOptions option = new ParallelOptions();
            option.MaxDegreeOfParallelism = 2;
            while (bIsRunning)
            {
                //Parallel.For(0, 1000, option, item =>
                for (int i = 0; i < 1000; i++)
                {
                    if (!bIsRunning)
                        return;
                    if (Rps < RpsLimit)
                    {
                        Req req;
                        Random rand = new Random();
                        int type = gType;
                        gType = gType == 0 ? 1 : 0;
                        req = ReqPool.Checkout();
                        req.dt = DateTime.Now;
                        if (type == 0)
                        {
                            double price;
                            int rangeRand = rand.Next(-1, 10000) + 1;
                            if (rType == 0)
                            {
                                price = 950 - (double)rangeRand / 100.00;
                            }
                            else
                            {
                                price = 1050.01 - (double)rangeRand / 100.00;
                            }
                            //price = decimal.Add(price, decimal.Divide(new decimal(rand.Next(0, int.MaxValue) + 1), new decimal(1000000000)));
                            //price = decimal.Add(new decimal(1040.00), decimal.Divide(new decimal(rangeRand), new decimal(100)));
                            long volume = (long)rand.Next(1, int.MaxValue);
                            long user = rand.Next(1, 10000);
                            string ticket = string.Format("{0}{1}", DateTime.Now.ToString("yyyyMMdd"), Guid.NewGuid().ToString("N"));
                            req.order.p = price;
                            req.order.v = volume;
                            req.order.u = user.ToString();
                            req.order.id = ticket;
                            req.order.t = Order.OrderType.Buy;
                        }
                        else
                        {
                            double price;
                            int rangeRand = rand.Next(-1, 10000) + 1;
                            if (rType == 0)
                            {
                                price = 950.01 + (double)rangeRand / 100.00;
                            }
                            else
                            {
                                price = 850 + (double)rangeRand / 100.00;
                            }
                            //decimal price = new decimal();
                            //price = decimal.Add(price, decimal.Divide(new decimal(rand.Next(0, int.MaxValue) + 1), new decimal(1000000000)));
                            //price = decimal.Subtract(new decimal(1000.00), decimal.Divide(new decimal(rangeRand), new decimal(100)));
                            long volume = (long)rand.Next(1, int.MaxValue);
                            long user = rand.Next(1, 10000);
                            string ticket = string.Format("{0}{1}", DateTime.Now.ToString("yyyyMMdd"), Guid.NewGuid().ToString("N"));
                            req.order.p = price;
                            req.order.v = volume;
                            req.order.u = user.ToString();
                            req.order.id = ticket;
                            req.order.t = Order.OrderType.Sell;
                        }
                        client.Send(req);
                        ReqPool.Checkin(req);
                        //Interlocked.Increment(ref Rps);
                        Rps++;
                    }
                }
                //);
            }
        }

        private static void MqTxInHandler(object sender, BasicDeliverEventArgs e)
        {
            var bytes = e.Body;
            Tx tx = TxPool.Checkout();
            //tx.FromBytes(bytes);
            //Console.WriteLine(tx.ToString());
            //Interlocked.Increment(ref Txps);
            TxPool.Checkin(tx);
            //TxIn.MsgFinished(e);
        }

        private static void MqRespInHandler(object sender, BasicDeliverEventArgs e)
        {
            var bytes = e.Body;
            Resp resp = RespPool.Checkout();
            resp.FromBytes(bytes);
            //Interlocked.Increment(ref Recps);
            if (!resp.Success) Interlocked.Increment(ref failPs);
            //Console.WriteLine(resp.ToString());
            RespPool.Checkin(resp);
            //RespIn.MsgFinished(e);
        }
    }
}
