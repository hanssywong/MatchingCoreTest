// See https://aka.ms/new-console-template for more information
using InputOrder;
using MatchingLib;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

if (args.Length == 1 && string.Compare(args[0],"help", StringComparison.OrdinalIgnoreCase) == 0 || args.Length < 4)
{
    HelpMsg();
    return;
}

ManualResetEventSlim evt = new();
SimpleTcpClient client  = new();
client.LogError += WriteError;
client.LogInfo += WriteLog;
bool bIsRunning = true;

#region Construct Order Request
Request req = new() { order = new() };
req.dt = DateTime.Now;
string ticket = string.Format("{0}{1}", DateTime.Now.ToString("yyyyMMdd"), Guid.NewGuid().ToString("N"));
double price = 0.0;
if(!double.TryParse(args[2], System.Globalization.NumberStyles.Number, null, out price))
{
    Console.WriteLine("Price is invalid");
    return;
}
req.order.p = price;
long volume = 0;
if (!long.TryParse(args[3], System.Globalization.NumberStyles.Number, null, out volume))
{
    Console.WriteLine("Volume is invalid");
    return;
}
req.order.v = volume;
req.id = args[0];
req.order.id = ticket;

int BuySell = -1;
if (!int.TryParse(args[1], System.Globalization.NumberStyles.Number, null, out BuySell))
{
    Console.WriteLine("Please input 0 or 1 for Buy/Sell");
    return;
}
req.order.t = BuySell == 0 ? Order.OrderType.Buy : Order.OrderType.Sell;
#endregion

#region Send Order and wait for reply then exit
Task task = Task.Factory.StartNew(() => RespReceiver());
Console.WriteLine("Send Order");
client.Connect("127.0.0.1", 59999);
client.Send(req);
Console.WriteLine("Wait for reply and then exit");
evt.Wait();
bIsRunning = false;
client.Shutdown();
#endregion

void HelpMsg()
{
    Console.WriteLine("Usage:");
    Console.WriteLine($"{Assembly.GetExecutingAssembly().GetName().Name} [User ID(string)] [0(Buy) or 1(Sell)] [Price(double)] [Volume(long)]");
    Console.WriteLine();
}

void WriteLog(string msg)
{
    Console.WriteLine(msg);
}
void WriteError(string msg)
{
    Console.WriteLine($"Err: {msg}");
}

void RespReceiver()
{
    Console.WriteLine("RespReceiver start");
    while (bIsRunning || !client.IsReceiveQueueEmpty)
    {
        try
        {
            byte[] buffer;
            if (client.ReceiveBuffer(out buffer))
            {
                var resp = new Resp() { order = new() };
                resp.FromBytes(buffer);
                Console.WriteLine("Order Response received:");
                Console.WriteLine(resp.ToString());
                client.CheckinBuffer(buffer);
                evt.Set();
            }
            else
            {
                Thread.Sleep(100);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
    Console.WriteLine("RespReceiver shutdown");
}

class Resp : ProcessOrderResult
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
