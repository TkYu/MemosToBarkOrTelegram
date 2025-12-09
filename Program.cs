using MemosToBarkOrTelegram.Models;
using MemosToBarkOrTelegram.Services;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace MemosToBarkOrTelegram
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Configure options
            builder.Services.Configure<MemosOptions>(
                builder.Configuration.GetSection(MemosOptions.SectionName));
            builder.Services.Configure<BarkOptions>(
                builder.Configuration.GetSection(BarkOptions.SectionName));
            builder.Services.Configure<TelegramOptions>(
                builder.Configuration.GetSection(TelegramOptions.SectionName));

            // Register HttpClient for BarkService with optional HTTP Auth
            builder.Services.AddHttpClient<IBarkService, BarkService>((serviceProvider, client) =>
            {
                var barkOptions = serviceProvider.GetRequiredService<IOptions<BarkOptions>>().Value;
                
                // Configure base address
                if (!string.IsNullOrEmpty(barkOptions.ServerUrl))
                {
                    client.BaseAddress = new Uri(barkOptions.ServerUrl.TrimEnd('/') + "/");
                }

                // Configure HTTP Basic Authentication if Auth is provided
                if (barkOptions.Auth == null ||
                    string.IsNullOrEmpty(barkOptions.Auth.Username) ||
                    string.IsNullOrEmpty(barkOptions.Auth.Password)) return;
                var byteArray = Encoding.ASCII.GetBytes($"{barkOptions.Auth.Username}:{barkOptions.Auth.Password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            });

            // Register HttpClient for TelegramService
            builder.Services.AddHttpClient<ITelegramService, TelegramService>();
            
            // Register NotificationService
            builder.Services.AddScoped<INotificationService, NotificationService>();

            var app = builder.Build();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
