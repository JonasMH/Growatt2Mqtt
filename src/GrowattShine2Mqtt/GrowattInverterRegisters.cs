namespace GrowattShine2Mqtt;

public static class GrowattInverterRegisters
{
    public static readonly GrowattInverterRegister ExportLimitEnableDisable = new(122, "ExportLimitEnableDisable");

    /// <summary>
    /// Range: -1000~+1000, Step size: 0.1%
    /// </summary>
    public static readonly GrowattInverterRegister ExportLimitPowerRate = new(123, "ExportLimitPowerRate");

    // Not confirmed
    public static readonly GrowattInverterRegister LoadFirstChargeStopSoC = new(608, "LoadFirstChargeStopSoC");

    public static readonly GrowattInverterRegister Priority = new(1044, "Priority");
    public static readonly GrowattInverterRegister BatteryUpperTemperatureLimitForDischarge = new(1010, "BatteryUpperTemperatureLimitForDischarge");
    public static readonly GrowattInverterRegister BatteryFirstChargeStopSoC = new(1091, "BatteryFirstChargeStopSoC");
    public static readonly GrowattInverterRegister BatteryFirstAcCharge = new(1092, "BatteryFirstAcCharge");
    public static readonly GrowattInverterRegister BatteryFirst1Start = new(1100, "BatteryFirst1Start");
    public static readonly GrowattInverterRegister BatteryFirst1Stop = new(1101, "BatteryFirst1Stop");
    public static readonly GrowattInverterRegister BatteryFirst1Enabled = new(1102, "BatteryFirst1Enabled");
}
