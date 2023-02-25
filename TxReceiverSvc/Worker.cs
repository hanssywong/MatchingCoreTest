using TxReceiver;

namespace TxReceiverSvc
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private TxReceiverObj receiver { get; } = null!;
        private Task task { get; set; } = null!;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
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
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
            receiver.client.Connect("127.0.0.1", 59998);
            task = Task.Factory.StartNew(() => receiver.Receiver());
        }
    }
}
