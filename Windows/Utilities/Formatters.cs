using System.Globalization;

namespace HwMonitor.Utilities;

internal static class Formatters
{
    public static string Size(ulong qianzijie)
    {
        if (qianzijie >= 1_073_741_824UL) return (qianzijie / 1_073_741_824d).ToString("0.##", CultureInfo.InvariantCulture) + " TB";
        if (qianzijie >= 1_048_576UL) return (qianzijie / 1_048_576d).ToString("0.0", CultureInfo.InvariantCulture) + " GB";
        if (qianzijie >= 1024UL) return (qianzijie / 1024d).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
        return qianzijie.ToString(CultureInfo.InvariantCulture) + " KB";
    }

    public static string Bytes(ulong zijie2)
    {
        return Size(zijie2 / 1024UL);
    }

    public static string Uptime(long miao2, bool yingwen)
    {
        var tian = miao2 / 86_400;
        var shi = miao2 % 86_400 / 3600;
        var fen = miao2 % 3600 / 60;

        if (yingwen)
        {
            if (tian > 0) return $"{tian}d {shi}h {fen}m";
            if (shi > 0) return $"{shi}h {fen}m";
            return $"{fen}m";
        }

        if (tian > 0) return $"{tian}天{shi}时{fen}分";
        if (shi > 0) return $"{shi}时{fen}分";
        return $"{fen}分";
    }

    public static string Temperature(double wenduzhi)
    {
        return wenduzhi >= 0 ? wenduzhi.ToString("0.0", CultureInfo.InvariantCulture) : "-";
    }

    public static string Percent(double baifenbizhi)
    {
        return baifenbizhi >= 0 ? baifenbizhi.ToString("0.#", CultureInfo.InvariantCulture) + "%" : "-";
    }

    public static string Power(double wazhi, bool gusuan)
    {
        if (wazhi < 0) return "-";
        var qianzhui = gusuan ? "≈" : "";
        return qianzhui + Math.Round(wazhi).ToString("0", CultureInfo.InvariantCulture) + " W";
    }
}
