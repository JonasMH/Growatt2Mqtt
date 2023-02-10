using GrowattShine2Mqtt;
using GrowattShine2Mqtt.Grpc;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using ToMqttNet;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((options, loggerConf) =>
{
	loggerConf
		.MinimumLevel.Debug()
		.Enrich.FromLogContext()
		.Enrich.WithThreadId()
		.WriteTo.Console(new CompactJsonFormatter())
		.ReadFrom.Configuration(options.Configuration);
});

var services = builder.Services;

services.AddGrpc();
services.AddGrpcReflection();
services.AddHealthChecks();
services.AddSingleton<IGrowattTopicHelper, GrowattTopicHelper>();
services.AddSingleton<IGrowattTelegramParser, GrowattTelegramParser>();

services.AddSingleton<GrowattServerListener>();
services.AddSingleton<IGrowattServerListener>(x => x.GetRequiredService<GrowattServerListener>());
services.AddHostedService(x => x.GetRequiredService<GrowattServerListener>());

services.AddMqttConnection(options =>
{
	options.NodeId = "growatttomqtt";
	options.ClientId = "growatttomqtt";
});


services.AddSingleton<CollectorRegistry>(x =>
{
	var registry = Metrics.NewCustomRegistry();

	DotNetStats.Register(registry);

	return registry;
});

services.AddSingleton<MetricFactory>(x =>
{
	var factory = Metrics.WithCustomRegistry(x.GetRequiredService<CollectorRegistry>());

	return factory;
});


services.AddOptions<GrowattServerOptions>()
    .BindConfiguration(nameof(GrowattServerOptions));

services.AddSingleton<IGrowattMetrics, GrowattMetrics>();
services.AddSingleton<IGrowattTelegramEncrypter, GrowattTelegramEncrypter>();
services.AddSingleton<NodaTime.IClock>(x => NodaTime.SystemClock.Instance);
services.AddSingleton<NodaTime.IDateTimeZoneProvider>(x => DateTimeZoneProviders.Tzdb);

services.AddSingleton<GrowattToMqttHandler>();
services.AddSingleton<IGrowattToMqttHandler>(x => x.GetRequiredService<GrowattToMqttHandler>());
services.AddHostedService(x => x.GetRequiredService<GrowattToMqttHandler>());

var app = builder.Build();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcReflectionService();
    endpoints.MapGrpcService<GrowattTestServiceImpl>();
    endpoints.MapHealthChecks("/health");
    endpoints.MapMetrics("/metrics", endpoints.ServiceProvider.GetRequiredService<CollectorRegistry>());
});


app.Run();

public partial class Program { }
