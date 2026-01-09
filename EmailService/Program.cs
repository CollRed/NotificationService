using EmailService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseUrls("http://*:8080"); 
        webBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<NotificationLogRepository>();
            services.AddHostedService<EmailConsumerService>();
        });

        webBuilder.Configure(app =>
        {
            app.UseRouting();

            // Собираем метрики
            app.UseHttpMetrics();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics(); 
            });
        });
    })
    .Build();

await host.RunAsync();