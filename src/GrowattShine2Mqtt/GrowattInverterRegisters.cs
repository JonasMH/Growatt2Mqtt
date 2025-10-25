namespace GrowattShine2Mqtt;

public record GrowattInverterRegister(ushort Register, string Name)
{
}


public static class GrowattInverterRegisters
{

    public static readonly GrowattInverterRegister ExportLimitEnableDisable = new(122, "ExportLimitEnableDisable");

    /// <summary>
    /// Range: -1000~+1000, Step size: 0.1%
    /// </summary>
    public static readonly GrowattInverterRegister ExportLimitPowerRate = new(123, "ExportLimitPowerRate");

    // Not confirmed
    public static readonly GrowattInverterRegister LoadFirstDischargeStopSoC = new(608, "LoadFirstDischargeStopSoC");

    public static readonly GrowattInverterRegister Priority = new(1044, "Priority");
    public static readonly GrowattInverterRegister BatteryUpperTemperatureLimitForDischarge = new(1010, "BatteryUpperTemperatureLimitForDischarge");
    public static readonly GrowattInverterRegister BatteryFirstDischargeStopSoC = new(1071, "BatteryFirstDischargeStopSoC");
    public static readonly GrowattInverterRegister BatteryFirstChargeStopSoC = new(1091, "BatteryFirstChargeStopSoC");
    public static readonly GrowattInverterRegister BatteryFirstAcCharge = new(1092, "BatteryFirstAcCharge");
    public static readonly GrowattInverterRegister BatteryFirst1Start = new(1100, "BatteryFirst1Start");
    public static readonly GrowattInverterRegister BatteryFirst1Stop = new(1101, "BatteryFirst1Stop");
    public static readonly GrowattInverterRegister BatteryFirst1Enabled = new(1102, "BatteryFirst1Enabled");


    public static IReadOnlyDictionary<int, GrowattInverterRegister> AllRegisters { get; } = new Dictionary<int, GrowattInverterRegister>
    {
        { ExportLimitEnableDisable.Register, ExportLimitEnableDisable },
        { ExportLimitPowerRate.Register, ExportLimitPowerRate },
        { LoadFirstDischargeStopSoC.Register, LoadFirstDischargeStopSoC },
        { Priority.Register, Priority },
        { BatteryUpperTemperatureLimitForDischarge.Register, BatteryUpperTemperatureLimitForDischarge },
        { BatteryFirstChargeStopSoC.Register, BatteryFirstChargeStopSoC },
        { BatteryFirstDischargeStopSoC.Register, BatteryFirstDischargeStopSoC },
        { BatteryFirstAcCharge.Register, BatteryFirstAcCharge },
        { BatteryFirst1Start.Register, BatteryFirst1Start },
        { BatteryFirst1Stop.Register, BatteryFirst1Stop },
        { BatteryFirst1Enabled.Register, BatteryFirst1Enabled },
    };
}
