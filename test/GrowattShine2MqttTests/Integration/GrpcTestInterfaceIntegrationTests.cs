using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrowattShine2Mqtt.Schema;
using Grpc.Net.Client;
using Grpc.Reflection.V1Alpha;

namespace GrowattShine2MqttTests.Integration;
public class GrpcTestInterfaceIntegrationTests : IAsyncLifetime
{
    private GrowattWebApplicationFactory webFactory = null!;

    [Fact]
    public async Task ReflectionShouldWork() {
        var reflectionClient = new ServerReflection.ServerReflectionClient(GrpcChannel.ForAddress($"http://localhost", new GrpcChannelOptions
        {
            HttpClient = webFactory.CreateClient()
        }));

        var reflectionStream = reflectionClient.ServerReflectionInfo();

        await reflectionStream.RequestStream.WriteAsync(new ServerReflectionRequest
        {
            ListServices = "x",
            
        });

        await reflectionStream.RequestStream.CompleteAsync();

        await reflectionStream.ResponseStream.MoveNext(default);
        var reflectionResponse = reflectionStream.ResponseStream.Current;


        Assert.NotNull(reflectionResponse);
        Assert.Contains(reflectionResponse.ListServicesResponse.Service, x => x.Name == GrowattTestService.Descriptor.FullName);
    }

    public async Task DisposeAsync()
    {
        await webFactory.DisposeAsync();
    }

    public Task InitializeAsync()
    {
        webFactory = new GrowattWebApplicationFactory();

        return Task.CompletedTask;
    }
}
