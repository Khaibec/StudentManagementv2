using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentManagement.Api.Data;
using StudentManagement.Api.Services;

namespace StudentManagement.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Safe idempotent database initialization
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    var passwordHasher = services.GetRequiredService<IPasswordHasher>();
                    logger.LogInformation("Đang kiểm tra và khởi tạo dữ liệu StudentManagementv2Db...");
                    await DbInitializer.InitializeAsync(context, passwordHasher);
                    logger.LogInformation("Khởi tạo cơ sở dữ liệu thành công.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo cơ sở dữ liệu.");
                }
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
