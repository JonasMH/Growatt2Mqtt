using GrowattShine2Mqtt;

namespace GrowattShine2MqttTests;

public class GrowattTelegramEncrypterTest {
    private readonly GrowattTelegramEncrypter _sut = new();

    public GrowattTelegramEncrypterTest()
    {

    }

    [Fact]
    public void ShouldDecryptExample()
    {
        var input = "309C236A6DB2265A4C807E70080045000050000A0000FE063641C0A804E4C0A800281658149F000019961656F70A501816D0AB96000000030006002001160D222C402040467734257761747447726F7761747447726F7761747447722142"
            .ParseHex();

        var result = _sut.Decrypt(input);


        var expected = "309c236a6db2265a0bf2110769743147723f776b7474b9745936a1dc70a3b2c77749622c53ed6f7778e262118565277962a4ece46f7761777441724f767779566b324f3727034062050e0315330628050e0315330628050e031533066630";

        Assert.Equal(expected, BitConverter.ToString([.. result]).Replace("-", ""), ignoreCase: true);
    }



    [Fact]
    public void ShouldDecryptEncryptToSame()
    {
        var input = "309C236A6DB2265A4C807E70080045000050000A0000FE063641C0A804E4C0A800281658149F000019961656F70A501816D0AB96000000030006002001160D222C402040467734257761747447726F7761747447726F7761747447722142"
            .ParseHex();

        var result = _sut.Decrypt(input);
        result = _sut.Decrypt(result);

        Assert.Equal(Convert.ToHexString(input), Convert.ToHexString(result));
    }
}
