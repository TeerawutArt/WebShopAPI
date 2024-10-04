using Microsoft.EntityFrameworkCore;
using WebShoppingAPI.Models;

namespace WebShoppingAPI.Helpers
{

    public class CheckTimeEventService : IHostedService, IDisposable
    {
        private readonly Dictionary<Guid, Timer> _discountTimers = new Dictionary<Guid, Timer>();
        private readonly IServiceScopeFactory _scopeFactory;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _pollingTask;

        public CheckTimeEventService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await SetupTimersAsync(cancellationToken);
            _pollingTask = Task.Run(() => PollForNewDiscountsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

        private async Task SetupTimersAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var discounts = await appDbContext.Discounts.ToArrayAsync(cancellationToken);

                    foreach (var discount in discounts)
                    {
                        SetupTimer(discount);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"เกิดerror ที่SetupTimer : {ex.Message}");
            }
        }

        private void SetupTimer(DiscountModel discount)
        {
            var timeUntilStart = discount.StartTimeUTC - DateTime.UtcNow;
            var timeUntilEnd = discount.EndTimeUTC - DateTime.UtcNow;

            if (timeUntilStart < TimeSpan.Zero) timeUntilStart = TimeSpan.Zero;
            if (timeUntilEnd < TimeSpan.Zero) timeUntilEnd = TimeSpan.Zero;
            var startTimer = new Timer(async state => await StartDiscountAsync((Guid)state!, _cancellationTokenSource.Token), discount.Id, timeUntilStart, Timeout.InfiniteTimeSpan);
            _discountTimers[discount.Id] = startTimer;
            var endTimer = new Timer(async state => await EndDiscountAsync((Guid)state!, _cancellationTokenSource.Token), discount.Id, timeUntilEnd, Timeout.InfiniteTimeSpan);
            _discountTimers[discount.Id] = endTimer;
        }

        private async Task PollForNewDiscountsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //เปลี่ยนความถี่ในการเข้าไปดึงข้อมูลจาก db ในที่นี้ ทุกๆ 2 นาที
                //วิธีนี้ไม่ดีเพราะมีการเข้าถึงข้อมูลทุกๆ 2 นาที(ตั้งไว้2นาที) ทำให้ประสิทธิภาพตกได้ (และข้อมูลไม่ได้อัปเดททันทีด้วย)
                //แนะนำให้ใช้วิธีการส่งสัญญาณจากclientดีกว่า (SignalR,socket.io)  แต่มันต้องไปติดตั้งทั้ง client และ backend (เพิ่มเยอะไปเอาวิธีนี้ไปก่อน)
                await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var discounts = await appDbContext.Discounts.ToArrayAsync(cancellationToken);

                        var existingDiscountIds = new HashSet<Guid>(_discountTimers.Keys);

                        foreach (var discount in discounts)
                        {
                            if (!existingDiscountIds.Contains(discount.Id))
                            {
                                SetupTimer(discount);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"เกิด error จากการดึงข้อมูล : {ex.Message}"); //log มันตรงๆนี่หละ
                }
            }
        }

        private async Task StartDiscountAsync(Guid discountId, CancellationToken cancellationToken)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var discount = await dbContext.Discounts.FirstOrDefaultAsync(d => d.Id == discountId, cancellationToken);

                    if (discount != null)
                    {
                        discount.IsDiscounted = true;
                        dbContext.Discounts.Update(discount);
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"เกิดerror ที่ไอดี: {discountId}: {ex.Message}");
            }
        }

        private async Task EndDiscountAsync(Guid discountId, CancellationToken cancellationToken)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var discount = await dbContext.Discounts.Include(d => d.Products).FirstOrDefaultAsync(d => d.Id == discountId, cancellationToken);

                    if (discount != null)
                    {
                        foreach (var product in discount.Products)
                        {
                            product.DiscountId = null;
                            product.DiscountPrice = product.Price;
                        }
                        dbContext.Discounts.Remove(discount);
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"เกิดerror ที่ไอดี: {discountId}: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            foreach (var timer in _discountTimers.Values)
            {
                timer?.Change(Timeout.Infinite, 0);
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var timer in _discountTimers.Values)
            {
                timer?.Dispose();
            }
            _cancellationTokenSource.Dispose();
        }
    }
}
