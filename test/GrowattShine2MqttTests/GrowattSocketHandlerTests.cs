using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;
using GrowattShine2Mqtt;
using GrowattShine2Mqtt.Telegrams;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace GrowattShine2MqttTests;

public class MeterFactoryStub : IMeterFactory
{
    public Meter Create(MeterOptions options)
    {
        return new Meter(options);
    }

    public void Dispose()
    {
    }
}

public class LoggerMock<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        
    }
}

public class GrowattToMqttHandlerMock : IGrowattToMqttHandler
{
    public Task HandleDataTelegramAsync(GrowattSPHData4Telegram data4Telegram){ return Task.CompletedTask; }
}

public class GrowattSocketHandlerTests
{
    private readonly GrowattSocketHandler _sut;

    private readonly GrowattSocketMock _growattSocketMock;

    public GrowattSocketHandlerTests()
    {
        _growattSocketMock = new GrowattSocketMock();
        var systemClockMock = new ClockMock();
        var timeZoneProviderMock = new DateTimeZoneProviderMock();

        _sut = new GrowattSocketHandler(
            new LoggerMock<GrowattSocketHandler>(),
            new GrowattToMqttHandlerMock(),
            new GrowattTelegramParser(new LoggerMock<GrowattTelegramParser>(), new GrowattTelegramEncrypter()),
            new GrowattMetrics(new MeterFactoryStub()),
            systemClockMock,
            timeZoneProviderMock,
            _growattSocketMock);
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldAckPing()
    {
        await _sut.HandleMessageAsync("00050006002001160d222c402040467734257761747447726f7761747447726f776174744772810f".ParseHex());

        var lastSentBuffer = _growattSocketMock.Sends.Last();

        Assert.Equal("00050006002001160d222c402040467734257761747447726f7761747447726f776174744772810f".ParseHex(), lastSentBuffer);
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldAckData3()
    {
        await _sut.HandleMessageAsync("00020006024101030d222c402040467734257761747447726f7761747447726f7761747447723c27244c3676405f4750747447726f7761747447726f7761747447726f77777d7d4f7055756174743b726e5e29747447166f1346647447557f79712d35765c5f773b303506726a7760747447726fc361c0748f72a777612724024a2d4653444476636c82c6747547286f776174546752211216543129171d1018545467726e7970747440946f7e617d744f726d774274714a487e6973fa60597e1c6684658c526a6f7761747447726f7a5b656a54067ceb68b07fff7265776b747e47786f7361707447726f77617474566c6f7738353506425a4756444147726e466174744761f37749656a57bc61766f257453727b67367be4475a6f7761747447726f7761747747726ec3618b3a6772903941748b09526f882f54654487c86662657747726f77617477af760b77a9747447726f4560a27678726f77617671433e6dcb6538761f726f7761747447736f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447716f77617e744d720b7705747447726f1361107447726e7761747547726f7761747447726f7761747447726f7761757447726f7761747447726f7761747447726f7705747e47726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447720b7705747447726f7761747447726f7761747447726f7761757447726f7761747447726f7761747447726f7761747447726f7761747447726f7760747447726f77617474476294".ParseHex());

        var lastSentBuffer = _growattSocketMock.Sends.First();

        Assert.Equal("000200060003010347099a", lastSentBuffer.ToHex(), ignoreCase: true);
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldSendTimeCommand()
    {
        await _sut.HandleMessageAsync("00020006024101030d222c402040467734257761747447726f7761747447726f7761747447723c27244c3676405f4750747447726f7761747447726f7761747447726f77777d7d4f7055756174743b726e5e29747447166f1346647447557f79712d35765c5f773b303506726a7760747447726fc361c0748f72a777612724024a2d4653444476636c82c6747547286f776174546752211216543129171d1018545467726e7970747440946f7e617d744f726d774274714a487e6973fa60597e1c6684658c526a6f7761747447726f7a5b656a54067ceb68b07fff7265776b747e47786f7361707447726f77617474566c6f7738353506425a4756444147726e466174744761f37749656a57bc61766f257453727b67367be4475a6f7761747447726f7761747747726ec3618b3a6772903941748b09526f882f54654487c86662657747726f77617477af760b77a9747447726f4560a27678726f77617671433e6dcb6538761f726f7761747447736f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447716f77617e744d720b7705747447726f1361107447726e7761747547726f7761747447726f7761747447726f7761757447726f7761747447726f7761747447726f7705747e47726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447720b7705747447726f7761747447726f7761747447726f7761757447726f7761747447726f7761747447726f7761747447726f7761747447726f7760747447726f77617474476294".ParseHex());

        var lastSentBuffer = _growattSocketMock.Sends.Last();

        Assert.Equal("00010006003701180d222c402040467734257761747447726f7761747447726f7761747447726f6861674677405d5a514d59774b4f47594e4474485f43b996", lastSentBuffer.ToHex(), ignoreCase: true);
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldAckData4()
    {
        await _sut.HandleMessageAsync("00020006033f01040d222c402040467734257761747447726f7761747447726f7761747447723c27244c3676405f4750747447726f7761747447726f7761747447726f77777d7d4f716f746174743b726a776155ec4cf16f79617464e079e6776f7474578a6f7663717447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f66e267f748b36f7f617463ac7df077667474477260c2617c7447726f7761747447726f77617d744773227766c04f47726f7f6174748f726f7769747447be6f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f76f5753146336e4f617472666b057761747447726f7761747447726f7761747447726f7761747447726f7766747447576f7761747447726e7761747447726f77617477af760b7764747447726f7761747447726f7761747447726f77617472e37476776f747447ba6f7761747447726f776174bc47726f7761747447726f7761747447726f777ac87447726f77617474477274cb617474a1726a6e05748e47726f6f617474f4726f77617474474e6f776175744772f87761747747726ff461747464726f7575747447726f7761747447726f7761747447726f7761747447726f7761747447726f776174744772ee77617474477c696e610c756972957708671447726f616175742374c77761747447726f7761747447726f77617474477269df64fc78fa7ec8766b766c47946faa6076774e725f7762747f47626f7761f47447760a7380747347726f526174744771877761747447726f7761747447726f776a747446ed6f77617f7447730c77616f2c4772742f61747457726e77617de147736f77647b65b86390669e658b47726f7761747447726f7761747447726f4661747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747446726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726f7761747447726fd752".ParseHex());

        var lastSentBuffer = _growattSocketMock.Sends.Last();

        Assert.Equal("0002000600030104473998", lastSentBuffer.ToHex(), ignoreCase: true);
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldSaveInverterRegister()
    {
        // Change AC Output source to Batt priority
        var input = "00010006002501060a252847233c4377415f7761747447726f7761747447726f7761747447726f766174748492"
            .ParseHex();

        await _sut.HandleMessageAsync(input);

        Assert.True(_sut.Info.InverterRegisterValues.ContainsKey(0x0001), "Register didn't exist");
        Assert.Equal(0x0000, _sut.Info.InverterRegisterValues[0x0001]);
    }
    

    [Fact]
    public async Task HandleMessageAsync_ShouldHandleQueryDataLoggerResponse()
    {
        var input = "00010006003701190D222C402040467734257761747447726F7761747447726F7761747447726F686167467743585A51435977434F45524E417E485A4E80A0"
            .ParseHex();

        await _sut.HandleMessageAsync(input);

        Assert.True(_sut.Info.DataloggerRegisterValues.ContainsKey((ushort)GrowattDataloggerRegisters.TIME), "Register didn't exist");

        var registerData = _sut.Info.DataloggerRegisterValues[(ushort)GrowattDataloggerRegisters.TIME];
        // 2022-09-16 23:02:19
        var text = Encoding.UTF8.GetString(registerData);

        Assert.Equal(new LocalDateTime(2017, 07, 01, 23, 59, 59).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), text);
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldHandleQueryInveter()
    {
        var input = "00010006002601050D222C402040467734257761747447726F7761747447726F7761747447726F7F617C7A579058"
            .ParseHex();

        await _sut.HandleMessageAsync(input);

        Assert.True(_sut.Info.InverterRegisterValues.ContainsKey(0x0008), "Register didn't exist");

        var registerData = _sut.Info.InverterRegisterValues[0x0008];
        Assert.Equal(3600, registerData);
    }

    [Fact]
    public async Task SendTelegramAsync_GrowattDataloggerQueryTelegram()
    {
        // Change AC Output source to Batt priority
        await _sut.SendTelegramAsync(new GrowattDataloggerQueryTelegram
        {
            LoggerId = "JPC7A420FJ",
            StartAddress = 0x0008,
            EndAddress = 0x0008,
        });
        var encrypter = new GrowattTelegramEncrypter();

        var lastSentBuffer = _growattSocketMock.Sends.Last();
        var resultDecrypted = encrypter.Decrypt(lastSentBuffer);


        var expected = "00010006002401190D222C402040467734257761747447726F7761747447726F7761747447726F7F617C3490";
        var expectedDecrypted = encrypter.Decrypt(expected.ParseHex());

        Assert.Equal(expectedDecrypted.ToHex(), resultDecrypted.ToHex(), ignoreCase: true);
    }


    public class GrowattSocketMock : IGrowattSocket
    {
        public int Available => 1;
        public int SocketId => 1337;
        public bool Connected => true;

        public List<ArraySegment<byte>> Sends = [];

        public Task<int> ReceiveAsync(ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(ArraySegment<byte> buffer)
        {
            Sends.Add(buffer);
            return Task.CompletedTask;
        }
    }

    public class ClockMock : IClock
    {
        public Instant GetCurrentInstant() => Instant.FromUtc(2022, 09, 09, 08, 03, 04);
    }

    public class DateTimeZoneProviderMock : IDateTimeZoneProvider
    {
        public DateTimeZone this[string id] => throw new NotImplementedException();

        public string VersionId => throw new NotImplementedException();

        public ReadOnlyCollection<string> Ids => throw new NotImplementedException();

        public DateTimeZone GetSystemDefault() => DateTimeZone.Utc;

        public DateTimeZone? GetZoneOrNull(string id)
        {
            throw new NotImplementedException();
        }
    }
}
