using BaseHelper;
using MatchingLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TxReceiver
{
    class Tx : Transaction
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
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
    internal class TxReceiverObj
    {
        ILogger logger { get; }
        public TxReceiverObj(ILogger logger)
        {
            this.logger = logger;
        }
        public bool bIsRunning { get; set; } = true;
        public SimpleTcpClient client { get; } = new SimpleTcpClient();
        public void WriteLog(string msg)
        {
            logger.LogInformation(msg);
        }
        public void WriteError(string msg)
        {
            logger.LogError(msg);
        }
        public void Receiver()
        {

            logger.LogInformation("TxReceiver start");
            while (bIsRunning || !client.IsReceiveQueueEmpty)
            {
                try
                {
                    byte[] buffer;
                    if (client.ReceiveBuffer(out buffer))
                    {
                        var tx = TxPool.Checkout();
                        tx.FromBytes(buffer);
                        logger.LogInformation(tx.ToString());
                        client.CheckinBuffer(buffer);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    logger.LogError(e.ToString());
                }
            }
            logger.LogInformation("TxReceiver shutdown");
        }
    }
}
