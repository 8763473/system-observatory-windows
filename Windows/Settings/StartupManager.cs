using Microsoft.Win32;
using System.Windows.Forms;

namespace HwMonitor.Settings;

internal static class StartupManager
{
    private const string zhucebiaojianlujing = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string yingyongming = "系统观测台";

    public static bool IsEnabled()
    {
        try
        {
            using var zhucebiaojian = Registry.CurrentUser.OpenSubKey(zhucebiaojianlujing, writable: false);
            var dangqianzhi = zhucebiaojian?.GetValue(yingyongming)?.ToString() ?? "";
            return dangqianzhi.Contains(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static bool SetEnabled(bool kaiqi)
    {
        try
        {
            using var zhucebiaojian = Registry.CurrentUser.CreateSubKey(zhucebiaojianlujing);
            if (zhucebiaojian is null) return false;
            if (kaiqi)
            {
                zhucebiaojian.SetValue(yingyongming, Quote(Application.ExecutablePath), RegistryValueKind.String);
            }
            else
            {
                zhucebiaojian.DeleteValue(yingyongming, throwOnMissingValue: false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Quote(string lujing)
    {
        return "\"" + lujing.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
