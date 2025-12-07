using GrowattShine2Mqtt;
using NodaTime;
using OpenTelemetry.Metrics;
using ToMqttNet;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

var services = builder.Services;

services.AddHealthChecks();
services.AddSingleton<GrowattTopicHelper>();
services.AddSingleton<IGrowattTelegramParser, GrowattTelegramParser>();

services.AddSingleton<GrowattServerListener>();
services.AddHostedService(x => x.GetRequiredService<GrowattServerListener>());

builder.Services.AddMqttConnection();

services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();
        builder.AddMeter("GrowattShine2Mqtt");
        builder.AddMeter("Microsoft.AspNetCore.Hosting",
                         "Microsoft.AspNetCore.Server.Kestrel");
    });


services.AddOptions<GrowattServerOptions>().BindConfiguration(GrowattServerOptions.SectionName);

services.AddSingleton<GrowattMetrics>();
services.AddSingleton<IGrowattTelegramEncrypter, GrowattTelegramEncrypter>();
services.AddSingleton<NodaTime.IClock>(x => NodaTime.SystemClock.Instance);
services.AddSingleton<NodaTime.IDateTimeZoneProvider>(x => DateTimeZoneProviders.Tzdb);

services.AddSingleton<GrowattToMqttHandler>();
services.AddSingleton<IGrowattToMqttHandler>(x => x.GetRequiredService<GrowattToMqttHandler>());
services.AddHostedService(x => x.GetRequiredService<GrowattToMqttHandler>());
services.AddOptions<GrowattToMqttOptions>()
    .BindConfiguration(GrowattToMqttOptions.Section);

var app = builder.Build();

app.UseRouting();

app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

public partial class Program { }
