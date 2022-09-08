namespace GrowattShine2Mqtt.Telegrams;

public class GrowattSPHData4Telegram : IGrowattTelegram
{
    public GrowattSPHData4Telegram(GrowattTelegramHeader header)
    {
        Header = header;
    }

    public GrowattTelegramHeader Header { get; set; }

    public string Datalogserial { get; set; }
    public string Pvserial { get; set; }
    public int Date { get; set; }
    public int Recortype1 { get; set; }
    public int Recortype2 { get; set; }
    public short Pvstatus { get; set; }
    public int Pvpowerin { get; set; }
    public short Pv1voltage { get; set; }
    public short Pv1current { get; set; }
    public int Pv1watt { get; set; }
    public short Pv2voltage { get; set; }
    public short Pv2current { get; set; }
    public int Pv2watt { get; set; }
    public uint Pvpowerout { get; set; }
    public short Pvfrequentie { get; set; }
    public short Pvgridvoltage { get; set; }
    public short Pvgridcurrent { get; set; }
    public int Pvgridpower { get; set; }
    public short Pvgridvoltage2 { get; set; }
    public short Pvgridcurrent2 { get; set; }
    public int Pvgridpower2 { get; set; }
    public short Pvgridvoltage3 { get; set; }
    public short Pvgridcurrent3 { get; set; }
    public int Pvgridpower3 { get; set; }
    public int Totworktime { get; set; }
    public int Eactoday { get; set; }
    public int Pvenergytoday { get; set; }
    public int Eactotal { get; set; }
    public int Epvtotal { get; set; }
    public int Epv1today { get; set; }
    public int Epv1total { get; set; }
    public int Epv2today { get; set; }
    public int Epv2total { get; set; }
    public short Pvtemperature { get; set; }
    public short Pvipmtemperature { get; set; }
    public short Pvboosttemp { get; set; }
    public short Bat_dsp { get; set; }
    public short Pbusvolt { get; set; }
    public short Nbusvolt { get; set; }
    public short Ipf { get; set; }
    public short Realoppercent { get; set; }
    public int Opfullwatt { get; set; }
    public short Deratingmode { get; set; }
    public int Eacharge_today { get; set; }
    public int Eacharge_total { get; set; }
    public short Batterytype { get; set; }
    public short Uwsysworkmode { get; set; }
    public short Systemfaultword0 { get; set; }
    public short Systemfaultword1 { get; set; }
    public short Systemfaultword2 { get; set; }
    public short Systemfaultword3 { get; set; }
    public short Systemfaultword4 { get; set; }
    public short Systemfaultword5 { get; set; }
    public short Systemfaultword6 { get; set; }
    public short Systemfaultword7 { get; set; }
    public int Pdischarge1 { get; set; }
    public int P1charge1 { get; set; }
    public short Vbat { get; set; }
    public short SOC { get; set; }
    public int Pactouserr { get; set; }
    public int Pactousers { get; set; }
    public int Pactousert { get; set; }
    public int Pactousertot { get; set; }
    public int Pactogridr { get; set; }
    public int Pactogrids { get; set; }
    public int Pactogridt { get; set; }
    public int Pactogridtot { get; set; }
    public int Plocaloadr { get; set; }
    public int Plocaloads { get; set; }
    public int Plocaloadt { get; set; }
    public int Plocaloadtot { get; set; }
    public int Ipm { get; set; }
    public int Battemp { get; set; }
    public short Spdspstatus { get; set; }
    public short Spbusvolt { get; set; }
    public int Etouser_tod { get; set; }
    public int Etouser_tot { get; set; }
    public int Etogrid_tod { get; set; }
    public int Etogrid_tot { get; set; }
    public int Edischarge1_tod { get; set; }
    public int Edischarge1_tot { get; set; }
    public int Eharge1_tod { get; set; }
    public int Eharge1_tot { get; set; }
    public int Elocalload_tod { get; set; }
    public int Elocalload_tot { get; set; }

    public static GrowattSPHData4Telegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        return new ByteDecoder<GrowattSPHData4Telegram>(new GrowattSPHData4Telegram(header), bytes)
            .ReadString(x => x.Datalogserial, 16 / 2, 10)
            .ReadString(x => x.Pvserial, 76 / 2, 10)
            .ReadInt16(x => x.Pvstatus, 158 / 2)
            .ReadInt32(x => x.Pvpowerin, 162 / 2)
            .ReadInt16(x => x.Pv1voltage, 170 / 2)
            .ReadInt16(x => x.Pv1current, 174 / 2)
            .ReadInt32(x => x.Pv1watt, 178 / 2)
            .ReadInt16(x => x.Pv2voltage, 186 / 2)
            .ReadInt16(x => x.Pv2current, 190 / 2)
            .ReadInt32(x => x.Pv2watt, 194 / 2)
            .ReadUInt32(x => x.Pvpowerout, 298 / 2)
            .ReadInt16(x => x.Pvfrequentie, 306 / 2)
            .ReadInt16(x => x.Pvgridvoltage, 310 / 2)
            .ReadInt16(x => x.Pvgridcurrent, 314 / 2)
            .ReadInt32(x => x.Pvgridpower, 318 / 2)
            .ReadInt16(x => x.Pvgridvoltage2, 326 / 2)
            .ReadInt16(x => x.Pvgridcurrent2, 330 / 2)
            .ReadInt32(x => x.Pvgridpower2, 334 / 2)
            .ReadInt16(x => x.Pvgridvoltage3, 342 / 2)
            .ReadInt16(x => x.Pvgridcurrent3, 346 / 2)
            .ReadInt32(x => x.Pvgridpower3, 350 / 2)
            .ReadInt32(x => x.Totworktime, 386 / 2)
            .ReadInt32(x => x.Eactoday, 370 / 2)
            .ReadInt32(x => x.Pvenergytoday, 370 / 2)
            .ReadInt32(x => x.Eactotal, 378 / 2)
            .ReadInt32(x => x.Epvtotal, 522 / 2)
            .ReadInt32(x => x.Epv1today, 394 / 2)
            .ReadInt32(x => x.Epv1total, 402 / 2)
            .ReadInt32(x => x.Epv2today, 410 / 2)
            .ReadInt32(x => x.Epv2total, 418 / 2)
            .ReadInt16(x => x.Pvtemperature, 530 / 2)
            .ReadInt16(x => x.Pvipmtemperature, 534 / 2)
            .ReadInt16(x => x.Pvboosttemp, 538 / 2)
            .ReadInt16(x => x.Bat_dsp, 546 / 2)
            .ReadInt16(x => x.Pbusvolt, 550 / 2)
            .ReadInt32(x => x.Eacharge_today, 606 / 2)
            .ReadInt32(x => x.Eacharge_total, 614 / 2)
            .ReadInt16(x => x.Batterytype, 634 / 2)
            .ReadInt16(x => x.Uwsysworkmode, 666 / 2)
            .ReadInt16(x => x.Systemfaultword0, 670 / 2)
            .ReadInt16(x => x.Systemfaultword1, 674 / 2)
            .ReadInt16(x => x.Systemfaultword2, 678 / 2)
            .ReadInt16(x => x.Systemfaultword3, 682 / 2)
            .ReadInt16(x => x.Systemfaultword4, 686 / 2)
            .ReadInt16(x => x.Systemfaultword5, 690 / 2)
            .ReadInt16(x => x.Systemfaultword6, 694 / 2)
            .ReadInt16(x => x.Systemfaultword7, 698 / 2)
            .ReadInt32(x => x.Pdischarge1, 702 / 2)
            .ReadInt32(x => x.P1charge1, 710 / 2)
            .ReadInt16(x => x.Vbat, 718 / 2)
            .ReadInt16(x => x.SOC, 722 / 2)
            .ReadInt32(x => x.Pactouserr, 726 / 2)
            .ReadInt32(x => x.Pactousertot, 750 / 2)
            .ReadInt32(x => x.Pactogridr, 758 / 2)
            .ReadInt32(x => x.Pactogridtot, 782 / 2)
            .ReadInt32(x => x.Plocaloadr, 790 / 2)
            .ReadInt32(x => x.Plocaloadtot, 814 / 2)
            .ReadInt16(x => x.Spdspstatus, 830 / 2)
            .ReadInt16(x => x.Spbusvolt, 834 / 2)
            .ReadInt32(x => x.Etouser_tod, 842 / 2)
            .ReadInt32(x => x.Etouser_tot, 850 / 2)
            .ReadInt32(x => x.Etogrid_tod, 858 / 2)
            .ReadInt32(x => x.Etogrid_tot, 866 / 2)
            .ReadInt32(x => x.Edischarge1_tod, 874 / 2)
            .ReadInt32(x => x.Edischarge1_tot, 882 / 2)
            .ReadInt32(x => x.Eharge1_tod, 890 / 2)
            .ReadInt32(x => x.Eharge1_tot, 898 / 2)
            .ReadInt32(x => x.Elocalload_tod, 906 / 2)
            .ReadInt32(x => x.Elocalload_tot, 914 / 2)
            .Result;
    }
}
