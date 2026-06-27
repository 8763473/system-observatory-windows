using System.Text;

namespace HwMonitor.Settings;

public enum AppLanguage
{
    Chinese = 0,
    English = 1
}

public sealed class AppSettings : IDisposable
{
    public AppLanguage yuyan { get; set; } = AppLanguage.Chinese;
    public bool kaijiqidong { get; set; }
    public bool guanbihoutuopan { get; set; }
    public bool relayEnabled { get; set; }
    public string relayUrl { get; set; } = "";
    public string relayDeviceKey { get; set; } = "";
    public string relayAppId { get; set; } = "system-observatory";
    public string relayDeviceId { get; set; } = Environment.MachineName;
    public string deepSeekApiKey { get; set; } = "";
    public string beijinglujing { get; private set; } = "";
    public Image? beijingtupian { get; private set; }

    public static string shezhilujing => Path.Combine(AppContext.BaseDirectory, "settings.ini");

    public bool zhiyingwen => yuyan == AppLanguage.English;

    public static AppSettings Load()
    {
        var shezhi = new AppSettings();
        var lujing2 = shezhilujing;
        if (!File.Exists(lujing2))
        {
            return shezhi;
        }

        foreach (var yuan4 in File.ReadAllLines(lujing2, Encoding.UTF8))
        {
            var hang2 = yuan4.Trim();
            if (hang2.Length == 0 || hang2.StartsWith("[") || hang2.StartsWith(";")) continue;
            var fenjie = hang2.IndexOf('=');
            if (fenjie <= 0) continue;
            var jian4 = hang2[..fenjie].Trim();
            var shezhizhi = hang2[(fenjie + 1)..].Trim();

            if (jian4.Equals("language", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.yuyan = shezhizhi == "1" ? AppLanguage.English : AppLanguage.Chinese;
            }
            else if (jian4.Equals("autostart", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.kaijiqidong = shezhizhi == "1" || shezhizhi.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            else if (jian4.Equals("closeToTray", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.guanbihoutuopan = shezhizhi == "1" || shezhizhi.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            else if (jian4.Equals("background", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.SetBackground(shezhizhi, false);
            }
            else if (jian4.Equals("relayEnabled", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.relayEnabled = shezhizhi == "1" || shezhizhi.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            else if (jian4.Equals("relayUrl", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.relayUrl = shezhizhi;
            }
            else if (jian4.Equals("relayDeviceKey", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.relayDeviceKey = shezhizhi;
            }
            else if (jian4.Equals("relayAppId", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.relayAppId = string.IsNullOrWhiteSpace(shezhizhi) ? "system-observatory" : shezhizhi;
            }
            else if (jian4.Equals("relayDeviceId", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.relayDeviceId = string.IsNullOrWhiteSpace(shezhizhi) ? Environment.MachineName : shezhizhi;
            }
            else if (jian4.Equals("deepSeekApiKey", StringComparison.OrdinalIgnoreCase))
            {
                shezhi.deepSeekApiKey = shezhizhi;
            }
        }

        return shezhi;
    }

    public void Save()
    {
        var wenben = new StringBuilder();
        wenben.AppendLine("[settings]");
        wenben.AppendLine($"language={(yuyan == AppLanguage.English ? 1 : 0)}");
        wenben.AppendLine($"autostart={(kaijiqidong ? 1 : 0)}");
        wenben.AppendLine($"closeToTray={(guanbihoutuopan ? 1 : 0)}");
        wenben.AppendLine($"background={beijinglujing}");
        wenben.AppendLine($"relayEnabled={(relayEnabled ? 1 : 0)}");
        wenben.AppendLine($"relayUrl={relayUrl}");
        wenben.AppendLine($"relayDeviceKey={relayDeviceKey}");
        wenben.AppendLine($"relayAppId={relayAppId}");
        wenben.AppendLine($"relayDeviceId={relayDeviceId}");
        wenben.AppendLine($"deepSeekApiKey={deepSeekApiKey}");
        File.WriteAllText(shezhilujing, wenben.ToString(), Encoding.UTF8);
    }

    public bool SetBackground(string lujing3, bool baocun = true)
    {
        if (string.IsNullOrWhiteSpace(lujing3) || !File.Exists(lujing3)) return false;

        try
        {
            using var yijiazai = Image.FromFile(lujing3);
            var fuben = new Bitmap(yijiazai);
            beijingtupian?.Dispose();
            beijingtupian = fuben;
            beijinglujing = lujing3;
            if (baocun) Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void ClearBackground()
    {
        beijingtupian?.Dispose();
        beijingtupian = null;
        beijinglujing = "";
        Save();
    }

    public string Text(string zhongwen, string yingwen) => zhiyingwen ? yingwen : zhongwen;

    public void Dispose()
    {
        beijingtupian?.Dispose();
    }
}
