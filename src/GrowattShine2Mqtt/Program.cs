using System.Security.Cryptography.X509Certificates;
using GrowattShine2Mqtt;
using GrowattShine2Mqtt.Grpc;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using NodaTime;
using OpenTelemetry.Metrics;
using ToMqttNet;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

var services = builder.Services;

services.AddGrpc();
services.AddGrpcReflection();
services.AddHealthChecks();
services.AddSingleton<GrowattTopicHelper>();
services.AddSingleton<IGrowattTelegramParser, GrowattTelegramParser>();

services.AddSingleton<GrowattServerListener>();
services.AddHostedService(x => x.GetRequiredService<GrowattServerListener>());

builder.Services.AddMqttConnection()
    .Configure<IOptions<MqttOptions>>((options, mqttConfI) =>
    {
        var mqttConf = mqttConfI.Value;
        options.NodeId = "growatttomqtt";

        builder.Configuration.GetSection("MqttConnectionOptions").Bind(mqttConf);
        var tcpOptions = new MqttClientTcpOptions
        {
            Server = mqttConf.Server,
            Port = mqttConf.Port,
        };

        if (mqttConf.UseTls)
        {
            var caCrt = new X509Certificate2(mqttConf.CaCrt);
            var clientCrt = X509Certificate2.CreateFromPemFile(mqttConf.ClientCrt, mqttConf.ClientKey);


            tcpOptions.TlsOptions = new MqttClientTlsOptions
            {
                UseTls = true,
                SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
                ClientCertificatesProvider = new DefaultMqttCertificatesProvider(new List<X509Certificate>()
            {
                clientCrt, caCrt
            }),
                CertificateValidationHandler = (certContext) =>
                {
                    X509Chain chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    chain.ChainPolicy.VerificationTime = DateTime.Now;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);
                    chain.ChainPolicy.CustomTrustStore.Add(caCrt);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                    // convert provided X509Certificate to X509Certificate2
                    var x5092 = new X509Certificate2(certContext.Certificate);

                    return chain.Build(x5092);
                }
            };
        }


        options.ClientOptions.ChannelOptions = tcpOptions;
    });

services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();
        builder.AddMeter("GrowattShine2Mqtt");
        builder.AddMeter("Microsoft.AspNetCore.Hosting",
                         "Microsoft.AspNetCore.Server.Kestrel");
    });


services.AddOptions<GrowattServerOptions>().BindConfiguration(nameof(GrowattServerOptions));

services.AddSingleton<GrowattMetrics>();
services.AddSingleton<IGrowattTelegramEncrypter, GrowattTelegramEncrypter>();
services.AddSingleton<NodaTime.IClock>(x => NodaTime.SystemClock.Instance);
services.AddSingleton<NodaTime.IDateTimeZoneProvider>(x => DateTimeZoneProviders.Tzdb);

services.AddSingleton<GrowattToMqttHandler>();
services.AddSingleton<IGrowattToMqttHandler>(x => x.GetRequiredService<GrowattToMqttHandler>());
services.AddHostedService(x => x.GetRequiredService<GrowattToMqttHandler>());

var app = builder.Build();

app.UseRouting();

app.MapGrpcReflectionService();
app.MapGrpcService<GrowattTestServiceImpl>();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

public partial class Program { }
