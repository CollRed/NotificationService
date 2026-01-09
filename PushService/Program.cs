using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using PushService;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseUrls("http://*:8080");
        webBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<NotificationLogRepository>();
            services.AddHostedService<PushConsumerService>();
        });

        webBuilder.Configure(app =>
        {
            app.UseRouting();
            app.UseHttpMetrics(); 
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics(); 
            });
        });
    })
    .Build();

await host.RunAsync();