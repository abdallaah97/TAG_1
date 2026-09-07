using Application.Services.DemoUserSeederService;

namespace API.BackgroundServices
{
    // Plain BackgroundService demo: loops for the lifetime of the app and adds one user
    // every tick. No dashboard, no persistence, no retries - if the app restarts the loop
    // just starts over.
    public class DemoUserBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DemoUserBackgroundService> _logger;

        public DemoUserBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DemoUserBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // BackgroundService is a singleton, DbContext is scoped: a scope has to be
                    // created by hand for every tick.
                    using var scope = _scopeFactory.CreateScope();
                    var seeder = scope.ServiceProvider.GetRequiredService<IDemoUserSeederService>();
                    await seeder.AddRandomUserAsync();

                    _logger.LogInformation("DemoUserBackgroundService added a new user.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DemoUserBackgroundService failed to add a user.");
                }
            }
        }
    }
}
