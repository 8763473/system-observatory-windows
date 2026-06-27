using HwMonitor.Models;

namespace HwMonitor.Hardware;

internal static class PowerEstimator
{
    public static void Apply(SystemSnapshot kuaizhao)
    {
        if (kuaizhao.chuliqi3.gonghaowazhi <= 0)
        {
            kuaizhao.chuliqi3.gonghaowazhi = EstimateCpuWatts(kuaizhao.chuliqi3);
            kuaizhao.chuliqi3.gonghaogusuan = true;
        }

        foreach (var xianqia in kuaizhao.xianqia7)
        {
            if (xianqia.gonghaowazhi <= 0)
            {
                xianqia.gonghaowazhi = EstimateGpuWatts(xianqia);
                xianqia.gonghaogusuan = true;
            }
        }

        var zonggonghao = Math.Max(0, kuaizhao.chuliqi3.gonghaowazhi) + kuaizhao.xianqia7.Sum(xianqia => Math.Max(0, xianqia.gonghaowazhi));
        zonggonghao += EstimateBaseWatts(kuaizhao);
        kuaizhao.zonggonghaowazhi = RoundWatts(zonggonghao);
        kuaizhao.zonggonghaogusuan = true;
    }

    public static double EstimateCpuWatts(CpuInfo chuliqi)
    {
        if (chuliqi.gonghaowazhi > 0 && !chuliqi.gonghaogusuan) return RoundWatts(chuliqi.gonghaowazhi);

        var hexin = Math.Clamp(chuliqi.hexin2 > 0 ? chuliqi.hexin2 : chuliqi.xiancheng, 2, 64);
        var xiancheng = Math.Clamp(chuliqi.xiancheng > 0 ? chuliqi.xiancheng : hexin, hexin, 128);
        var shiyonglv = Math.Clamp(chuliqi.shiyonglvbaifenbi, 0, 100) / 100d;
        var pinlvyinzi = chuliqi.zhipinlv > 0 ? Math.Clamp(chuliqi.zhipinlv / 3200d, 0.72d, 1.28d) : 1d;
        var zuidagonghao = 18d + hexin * 7.4d + Math.Max(0, xiancheng - hexin) * 1.45d;
        var xinghao = chuliqi.xinghao2 ?? "";

        if (ContainsAny(xinghao, "HX", " H ", "-H", "K", "KF", "KS", "X3D", "Threadripper", "Ryzen 9", "Core i9")) zuidagonghao *= 1.18d;
        if (ContainsAny(xinghao, " U ", "-U", " Y ", "-Y", "N100", "N200", "Mobile")) zuidagonghao *= 0.62d;
        if (ContainsAny(xinghao, "Xeon", "EPYC")) zuidagonghao *= 1.28d;

        zuidagonghao = Math.Clamp(zuidagonghao, 18d, 220d);
        var kongxian = Math.Clamp(6d + hexin * 0.55d, 6d, 22d);
        var dongtai = Math.Pow(shiyonglv, 1.18d);
        return RoundWatts(kongxian + (zuidagonghao - kongxian) * dongtai * pinlvyinzi);
    }

    public static double EstimateGpuWatts(GpuInfo xianqia)
    {
        if (xianqia.gonghaowazhi > 0 && !xianqia.gonghaogusuan) return RoundWatts(xianqia.gonghaowazhi);

        var zuidagonghao = EstimateGpuMaxWatts(xianqia.xinghao3 ?? "");
        var kongxian = zuidagonghao <= 45 ? 4d : 9d;
        var shiyonglv = Math.Clamp(xianqia.shiyonglvbaifenbi4, 0, 100) / 100d;
        var xiancunlv = xianqia.zhizongliangzhi > 0 ? Math.Clamp(xianqia.zhiyiyongzhi / (double)xianqia.zhizongliangzhi, 0, 1) : 0d;
        var pinlvyinzi = xianqia.hexinpinlv > 0 ? Math.Clamp(xianqia.hexinpinlv / 2300d, 0.65d, 1.16d) : 0.9d;
        var dongtai = Math.Clamp(Math.Pow(shiyonglv, 1.05d) * 0.78d + xiancunlv * 0.10d + pinlvyinzi * 0.12d, 0d, 1d);
        return RoundWatts(kongxian + (zuidagonghao - kongxian) * dongtai);
    }

    private static double EstimateBaseWatts(SystemSnapshot kuaizhao)
    {
        var neicunjib = kuaizhao.neicun.zongliangzhi3 / 1_048_576d;
        var neicunwazhi = Math.Clamp(neicunjib * 0.32d, 3d, 18d);
        var cipanwazhi = Math.Clamp(kuaizhao.cipan2.Count * 3.8d, 3d, 26d);
        var fengshanwazhi = Math.Clamp(kuaizhao.fengshan3.Count * 1.1d, 1.5d, 12d);
        return RoundWatts(18d + neicunwazhi + cipanwazhi + fengshanwazhi);
    }

    private static double EstimateGpuMaxWatts(string xinghao)
    {
        var mingcheng = xinghao ?? "";
        double wazhi = 75d;

        if (ContainsAny(mingcheng, "4090")) wazhi = 450d;
        else if (ContainsAny(mingcheng, "4080")) wazhi = 320d;
        else if (ContainsAny(mingcheng, "4070")) wazhi = 220d;
        else if (ContainsAny(mingcheng, "4060")) wazhi = 135d;
        else if (ContainsAny(mingcheng, "3090")) wazhi = 350d;
        else if (ContainsAny(mingcheng, "3080")) wazhi = 320d;
        else if (ContainsAny(mingcheng, "3070")) wazhi = 220d;
        else if (ContainsAny(mingcheng, "3060")) wazhi = 170d;
        else if (ContainsAny(mingcheng, "3050")) wazhi = 120d;
        else if (ContainsAny(mingcheng, "7900")) wazhi = 330d;
        else if (ContainsAny(mingcheng, "7800")) wazhi = 260d;
        else if (ContainsAny(mingcheng, "7700")) wazhi = 230d;
        else if (ContainsAny(mingcheng, "7600")) wazhi = 165d;
        else if (ContainsAny(mingcheng, "Arc A770")) wazhi = 225d;
        else if (ContainsAny(mingcheng, "Arc", "Intel", "Iris", "UHD")) wazhi = 35d;

        if (ContainsAny(mingcheng, "Laptop", "Mobile", "Notebook", "Max-Q")) wazhi *= 0.62d;
        return Math.Clamp(wazhi, 18d, 480d);
    }

    private static double RoundWatts(double wazhi)
    {
        return Math.Round(Math.Clamp(wazhi, 0, 999), 0);
    }

    private static bool ContainsAny(string dangqianzhi, params string[] houxuan)
    {
        return houxuan.Any(neirong => dangqianzhi.Contains(neirong, StringComparison.OrdinalIgnoreCase));
    }
}
