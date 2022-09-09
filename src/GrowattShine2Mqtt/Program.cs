using GrowattShine2Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Serilog;
using ToMqttNet;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((options, loggerConf) =>
{
	loggerConf
		.MinimumLevel.Debug()
		.Enrich.FromLogContext()
		.Enrich.WithThreadId()
		.WriteTo.Console(outputTemplate: "[{Timestamp:yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzz} {Level:u3} {ThreadId} {SourceContext}] {Message:lj}{NewLine}{Exception}")
		.ReadFrom.Configuration(options.Configuration);
});

var services = builder.Services;

services.AddSingleton<IGrowattTopicHelper, GrowattTopicHelper>();
services.AddSingleton<IGrowattTelegramParser, GrowattTelegramParser>();
services.AddHostedService<GrowattServerListener>();
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

services.AddSingleton<IGrowattMetrics, GrowattMetrics>();

services.AddSingleton<GrowattToMqttHandler>();
services.AddSingleton<IGrowattToMqttHandler>(x => x.GetRequiredService<GrowattToMqttHandler>());
services.AddHostedService(x => x.GetRequiredService<GrowattToMqttHandler>());

var app = builder.Build();



app.Run();
