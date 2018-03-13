using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using MatchingLib;
using System.Configuration;
using System.Threading.Tasks;
using BaseHelper;

namespace TestSocketClient
{
    // State object for receiving data from remote device.  
    //public class StateObject
    //{
    //    // Client socket.  
    //    public Socket workSocket = null;
    //    // Size of receive buffer.  
    //    public const int BufferSize = 256;
    //    // Receive buffer.  
    //    public byte[] buffer = new byte[BufferSize];
    //    // Received data string.  
    //    public StringBuilder sb = new StringBuilder();
    //}

    public class AsynchronousClient
    {
        /*
        // The port number for the remote device.  
        private const int port = 9998;

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone { get; } =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone { get; } =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone { get; } =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;

        private static void StartClient()
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // The name of the   
                // remote device is "host.contoso.com".  
                //IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
                IPHostEntry ipHostInfo = Dns.GetHostEntry("127.0.0.1");
                int num = 0;
                bool bFound = false;
                foreach (var item in ipHostInfo.AddressList)
                {
                    if (item.AddressFamily == AddressFamily.InterNetwork)
                    {
                        bFound = true;
                        break;
                    }
                    num++;
                }
                if (!bFound)
                {
                    Console.WriteLine("No IPv4 address were found");
                    return;
                }
                IPAddress ipAddress = ipHostInfo.AddressList[num];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.  
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Send test data to the remote device.  
                Send(client, "This is a test<EOF>");
                sendDone.WaitOne();

                // Receive the response from the remote device.  
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console.  
                Console.WriteLine("Response received : {0}", response);

                // Release the socket.  
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        */
        static objPool<Req> ReqPool { get; } = new objPool<Req>(() => new Req());
        static objPool<SimpleTcpClient> clientPool { get; set; }
        static bool IsRunning { get; set; } = true;
        static int cnt = 0;
        public static int Main(String[] args)
        {
            string ip = ConfigurationManager.AppSettings["ip"].ToString();
            int port = int.Parse(ConfigurationManager.AppSettings["port"]);
            //StartClient();
            clientPool = new objPool<SimpleTcpClient>(() => new SimpleTcpClient(ip, port));
            Task task = Task.Factory.StartNew(() => SendReqTask());
            Task.Factory.StartNew(() => callback());
            Console.ReadKey();
            IsRunning = false;
            task.Wait();
            return 0;
        }
        public static void callback()
        {
            while(IsRunning)
            {
                Thread.Sleep(1000);
                int tmp1 = Interlocked.Exchange(ref cnt, 0);
                Console.WriteLine(string.Format("Rps:", tmp1));
            }
        }
        public static void SendReqTask()
        {
            while (IsRunning)
            {
                try
                {
                    SimpleTcpClient client = clientPool.Checkout();
                    client.LogInfo += Console.WriteLine;
                    client.LogError += Console.WriteLine;
                    Req req = ReqPool.Checkout();
                    var binObj = req.ToBytes();
                    client.Connect();
                    client.Send(binObj.bytes, (int)binObj.ms.Position);
                    byte[] bytes = null;
                    var len = client.Receive(out bytes);
                    Console.WriteLine(string.Format("len:{0}", len));
                    client.Shutdown();
                    Req.CheckIn(binObj);
                    ReqPool.Checkin(req);
                    Interlocked.Increment(ref cnt);
                    if (cnt >= 10) Environment.Exit(0);
                    clientPool.Checkin(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
    public class Req : RequestToMatching
    {
        public Req()
        {
            order = new Order();
        }
    }
}
