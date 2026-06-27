using HwMonitor.Models;

namespace HwMonitor.Hardware;

internal sealed class SensorSnapshot
{
    public double chuliqiwenduzhi { get; set; } = -1;
    public double chuliqigonghao { get; set; } = -1;
    public double xianqiawenduzhi { get; set; } = -1;
    public double xianqiagonghao { get; set; } = -1;
    public double xianqiashiyonglvbaifenbi { get; set; } = -1;
    public double xianqiahexinpinlv { get; set; } = -1;
    public double xianqianeicunpinlv { get; set; } = -1;
    public double xianqiafengshanbaifenbi { get; set; } = -1;
    public List<FanInfo> fengshan2 { get; } = [];

    public void AddFan(string mingcheng7, double zhuansu, double baifenbi)
    {
        if ((zhuansu <= 0 || zhuansu > 20_000) && (baifenbi <= 0 || baifenbi > 100)) return;
        mingcheng7 = string.IsNullOrWhiteSpace(mingcheng7) ? "Fan" : mingcheng7.Trim();

        var yiyou2 = fengshan2.FirstOrDefault(fengshanxiang2 => fengshanxiang2.mingcheng9.Equals(mingcheng7, StringComparison.OrdinalIgnoreCase));
        if (yiyou2 is not null)
        {
            if (zhuansu > 0) yiyou2.fengshanzhuansu = zhuansu;
            if (baifenbi > 0) yiyou2.baifenbi2 = baifenbi;
            return;
        }

        fengshan2.Add(new FanInfo
        {
            mingcheng9 = mingcheng7,
            leixing2 = FanType(mingcheng7),
            fengshanzhuansu = zhuansu,
            baifenbi2 = baifenbi
        });
    }

    private static string FanType(string mingcheng8)
    {
        if (Contains(mingcheng8, "GPU") || Contains(mingcheng8, "Graphics") || Contains(mingcheng8, "Video")) return "gpu";
        if (Contains(mingcheng8, "CPU") || Contains(mingcheng8, "Processor") || Contains(mingcheng8, "AIO") || Contains(mingcheng8, "Pump")) return "cpu";
        if (Contains(mingcheng8, "System") || Contains(mingcheng8, "SYS") || Contains(mingcheng8, "Chassis") || Contains(mingcheng8, "Case") || Contains(mingcheng8, "PCH") || Contains(mingcheng8, "Motherboard")) return "system";
        return "other";
    }

    private static bool Contains(string wenbenyuan2, string sousuoci2) => wenbenyuan2.Contains(sousuoci2, StringComparison.OrdinalIgnoreCase);
}
