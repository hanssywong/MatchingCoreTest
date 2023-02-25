using TxReceiver;

namespace TxReceiverSvc
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private TxReceiverObj receiver { get; } = null!;
        private Task task { get; set; } = null!;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            receiver = new(logger);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            receiver.bIsRunning = false;
            receiver.client.Shutdown();
            task.Wait();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string server_ip = _config["ServerIP"] ?? "127.0.0.1";
            string server_port = _config["ServerPort"] ?? string.Empty;
            bool isInt = int.TryParse(server_port, out int port);
            if (!isInt) port = 59998;
            receiver.client.Connect(server_ip, port);
            task = Task.Factory.StartNew(() => receiver.Receiver());
        }
    }
}
