using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using SmartRoomFinder.Data;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;

namespace SmartRoomFinder.Services.Implementations
{
    public class DepositExpiryWorker : BackgroundService
    {
        private readonly ILogger<DepositExpiryWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DepositExpiryWorker(ILogger<DepositExpiryWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("DepositExpiryWorker running at: {time}", DateTimeOffset.Now);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // 1. Cancel Pending deposits after 15 minutes
                    var expiredPending = await dbContext.Deposits
                        .Where(d => d.Status == DepositStatus.Pending && d.CreatedAt.AddMinutes(15) < DateTime.UtcNow)
                        .ToListAsync(stoppingToken);

                    foreach (var deposit in expiredPending)
                    {
                        deposit.Status = DepositStatus.Refunded; // Actually it's just cancelled
                        _logger.LogInformation("Cancelled pending deposit {id}", deposit.Id);
                    }

                    // 2. Forfeit Paid deposits after ExpiresAt (default 3 days)
                    var expiredPaid = await dbContext.Deposits
                        .Include(d => d.Room)
                        .Where(d => d.Status == DepositStatus.Paid && d.ExpiresAt.HasValue && d.ExpiresAt.Value < DateTime.UtcNow)
                        .ToListAsync(stoppingToken);

                    foreach (var deposit in expiredPaid)
                    {
                        deposit.Status = DepositStatus.Forfeited;
                        if (deposit.Room != null)
                        {
                            deposit.Room.IsReserved = false;
                        }
                        _logger.LogInformation("Forfeited paid deposit {id}", deposit.Id);
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
