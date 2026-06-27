using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Text;
using Microsoft.Win32;

namespace HwMonitor.Hardware;

internal readonly record struct TokenCollectorStatus(
    bool renwuyianzhuang,
    bool caijiqiyunxing,
    int? jinchengbianhao,
    bool openclawmuluyouxiao,
    DateTime? xintiaoshijian,
    string zhuangtai,
    string yingwenzhuangtai);

internal readonly record struct TokenCollectorInstallResult(
    bool chenggong,
    string zhongwentishi,
    string yingwentishi);

internal static class TokenCollectorService
{
    private const string renwuming = "系统观测台 Token采集器";
    private const string caijicanshu = "--token-collector";
    private const int caijijiangehaomiao = 5000;
    private const string zhucebiaojianlujing = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private static readonly TimeSpan zhuangtaihuancunshichang = TimeSpan.FromSeconds(9);
    private static readonly object zhuangtaisuo = new();
    private static TokenCollectorStatus huancunzhuangtai = new(false, false, null, false, null, "检测中", "Checking");
    private static DateTime huancunzhuangtaishijian = DateTime.MinValue;
    private static bool zhuangtaishuaxinzhong;

    public static string HeartbeatPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "token_collector_heartbeat.txt");
    }

    public static void RunCollectorLoop()
    {
        if (FindCollectorProcessId(paichudangqian: true) is not null) return;

        while (true)
        {
            try
            {
                OpenClawUsageLogReader.ImportKnownUsageLogs();
                WriteHeartbeat();
            }
            catch
            {
            }

            Thread.Sleep(caijijiangehaomiao);
        }
    }

    public static TokenCollectorInstallResult InstallOrRepair()
    {
        var dangqianexe = CurrentExecutablePath();
        if (string.IsNullOrWhiteSpace(dangqianexe) || !File.Exists(dangqianexe))
        {
            return new TokenCollectorInstallResult(false, "未找到当前程序路径，无法安装采集服务", "The current executable path was not found, so the collector could not be installed");
        }

        var yunxingmingling = QuoteArgument(dangqianexe) + " " + caijicanshu;
        var chuangjian = RunHiddenProcess(
            "schtasks.exe",
            "/Create /TN " + QuoteArgument(renwuming) + " /TR " + QuoteArgument(yunxingmingling) + " /SC ONLOGON /RL LIMITED /F",
            15000);
        var yijihuarenwu = chuangjian.exitCode == 0;
        var yonghuqidongxiang = false;
        string? yonghuqidongcuowu = null;
        if (!yijihuarenwu)
        {
            yonghuqidongxiang = InstallRunKey(dangqianexe, out yonghuqidongcuowu);
        }

        if (!yijihuarenwu && !yonghuqidongxiang)
        {
            var tishi = ShortProcessText(chuangjian);
            if (!string.IsNullOrWhiteSpace(yonghuqidongcuowu))
            {
                tishi += "；启动项写入失败：" + yonghuqidongcuowu;
            }

            RefreshSnapshotNow();
            return new TokenCollectorInstallResult(false, "Token 采集服务配置失败：" + tishi, "Token collector configuration failed: " + tishi);
        }

        var qidong = StartCollectorNow();
        RefreshSnapshotNow();
        var fangshi = yijihuarenwu ? "计划任务" : "用户启动项";
        var fangshiyingwen = yijihuarenwu ? "Task Scheduler" : "user startup entry";
        return qidong.chenggong
            ? new TokenCollectorInstallResult(true, $"Token 后台采集服务已通过{fangshi}安装并启动", $"Token background collector has been installed through {fangshiyingwen} and started")
            : new TokenCollectorInstallResult(true, $"Token 后台采集服务已通过{fangshi}安装，等待下次登录自动启动", $"Token background collector has been installed through {fangshiyingwen} and will start on next logon");
    }

    public static TokenCollectorInstallResult StartCollectorNow()
    {
        if (FindCollectorProcessId() is not null)
        {
            return new TokenCollectorInstallResult(true, "Token 后台采集服务已在运行", "Token background collector is already running");
        }

        var dangqianexe = CurrentExecutablePath();
        if (string.IsNullOrWhiteSpace(dangqianexe) || !File.Exists(dangqianexe))
        {
            return new TokenCollectorInstallResult(false, "未找到当前程序路径", "The current executable path was not found");
        }

        try
        {
            using var jincheng = Process.Start(new ProcessStartInfo
            {
                FileName = dangqianexe,
                Arguments = caijicanshu,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            return jincheng is null
                ? new TokenCollectorInstallResult(false, "后台采集器启动失败", "Background collector failed to start")
                : new TokenCollectorInstallResult(true, $"后台采集器已隐藏启动 (PID {jincheng.Id})", $"Background collector started hidden (PID {jincheng.Id})");
        }
        catch (Exception yichang)
        {
            return new TokenCollectorInstallResult(false, "后台采集器启动失败：" + yichang.Message, "Background collector failed to start: " + yichang.Message);
        }
    }

    public static TokenCollectorStatus Snapshot()
    {
        lock (zhuangtaisuo)
        {
            var xianzai = DateTime.UtcNow;
            if ((xianzai - huancunzhuangtaishijian) <= zhuangtaihuancunshichang)
            {
                return huancunzhuangtai;
            }

            if (!zhuangtaishuaxinzhong)
            {
                zhuangtaishuaxinzhong = true;
                _ = Task.Run(RefreshSnapshotNow);
            }

            return huancunzhuangtai;
        }
    }

    public static TokenCollectorStatus RefreshSnapshotNow()
    {
        TokenCollectorStatus zhuangtai;
        try
        {
            zhuangtai = BuildSnapshotSlow();
        }
        catch
        {
            zhuangtai = new TokenCollectorStatus(false, false, null, Directory.Exists(OpenClawUsageLogReader.OpenClawRootPath()), ReadHeartbeatTime(), "状态检测失败", "Status check failed");
        }

        lock (zhuangtaisuo)
        {
            huancunzhuangtai = zhuangtai;
            huancunzhuangtaishijian = DateTime.UtcNow;
            zhuangtaishuaxinzhong = false;
            return huancunzhuangtai;
        }
    }

    private static TokenCollectorStatus BuildSnapshotSlow()
    {
        var renwu = IsCollectorRegistered();
        var jincheng = FindCollectorProcessId();
        var xintiao = ReadHeartbeatTime();
        var openclaw = Directory.Exists(OpenClawUsageLogReader.OpenClawRootPath());

        string zhuangtai;
        string yingwen;
        if (jincheng is not null)
        {
            zhuangtai = "后台采集运行中";
            yingwen = "Background collector running";
        }
        else if (renwu)
        {
            zhuangtai = "已安装，等待登录触发";
            yingwen = "Installed, waiting for logon";
        }
        else if (openclaw)
        {
            zhuangtai = "未配置";
            yingwen = "Not configured";
        }
        else
        {
            zhuangtai = "未找到 OpenClaw 日志目录";
            yingwen = "OpenClaw log folder not found";
        }

        return new TokenCollectorStatus(renwu, jincheng is not null, jincheng, openclaw, xintiao, zhuangtai, yingwen);
    }

    internal static int? FindCollectorProcessId(bool paichudangqian = false)
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            var dangqian = Environment.ProcessId;
            using var chaxun = new ManagementObjectSearcher("SELECT ProcessId, Name, ExecutablePath, CommandLine FROM Win32_Process");
            foreach (ManagementObject jincheng in chaxun.Get())
            {
                var mingling = jincheng["CommandLine"] as string ?? "";
                if (!mingling.Contains(caijicanshu, StringComparison.OrdinalIgnoreCase)) continue;
                var mingcheng = jincheng["Name"] as string ?? "";
                var kelujing = jincheng["ExecutablePath"] as string ?? "";
                if (!IsSystemObservatoryProcess(mingcheng, kelujing)) continue;
                if (!int.TryParse(jincheng["ProcessId"]?.ToString(), out var pid)) continue;
                if (paichudangqian && pid == dangqian) continue;
                return pid;
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool IsSystemObservatoryProcess(string mingcheng, string kelujing)
    {
        var dangqianexe = CurrentExecutablePath();
        if (!string.IsNullOrWhiteSpace(kelujing) && !string.IsNullOrWhiteSpace(dangqianexe))
        {
            try
            {
                if (string.Equals(Path.GetFullPath(kelujing), Path.GetFullPath(dangqianexe), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
            }
        }

        return string.Equals(mingcheng, "系统观测台.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTaskInstalled()
    {
        var jieguo = RunHiddenProcess("schtasks.exe", "/Query /TN " + QuoteArgument(renwuming), 10000);
        return jieguo.exitCode == 0;
    }

    private static bool IsCollectorRegistered()
    {
        return IsTaskInstalled() || IsRunKeyInstalled();
    }

    private static bool InstallRunKey(string dangqianexe, out string? cuowu)
    {
        try
        {
            using var zhucebiaojian = Registry.CurrentUser.CreateSubKey(zhucebiaojianlujing);
            if (zhucebiaojian is null)
            {
                cuowu = "无法打开当前用户启动项";
                return false;
            }

            zhucebiaojian.SetValue(renwuming, QuoteArgument(dangqianexe) + " " + caijicanshu, RegistryValueKind.String);
            cuowu = null;
            return true;
        }
        catch (Exception yichang)
        {
            cuowu = yichang.Message;
            return false;
        }
    }

    private static bool IsRunKeyInstalled()
    {
        try
        {
            using var zhucebiaojian = Registry.CurrentUser.OpenSubKey(zhucebiaojianlujing, writable: false);
            var dangqianzhi = zhucebiaojian?.GetValue(renwuming) as string;
            return !string.IsNullOrWhiteSpace(dangqianzhi) && dangqianzhi.Contains(caijicanshu, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static (int exitCode, string output, string error) RunHiddenProcess(string wenjian, string canshu, int dengdaihaomiao)
    {
        try
        {
            using var jincheng = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = wenjian,
                    Arguments = canshu,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            if (!jincheng.Start()) return (-1, "", "process did not start");
            if (!jincheng.WaitForExit(Math.Max(1000, dengdaihaomiao)))
            {
                try
                {
                    jincheng.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                return (-2, "", "timeout");
            }

            return (jincheng.ExitCode, jincheng.StandardOutput.ReadToEnd(), jincheng.StandardError.ReadToEnd());
        }
        catch (Exception yichang)
        {
            return (-3, "", yichang.Message);
        }
    }

    private static string CurrentExecutablePath()
    {
        var lujing = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(lujing)) return lujing;
        return Path.Combine(AppContext.BaseDirectory, "系统观测台.exe");
    }

    private static void WriteHeartbeat()
    {
        var wenben = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        File.WriteAllText(HeartbeatPath(), wenben, Encoding.UTF8);
    }

    private static DateTime? ReadHeartbeatTime()
    {
        try
        {
            var lujing = HeartbeatPath();
            if (!File.Exists(lujing)) return null;
            return DateTime.TryParse(File.ReadAllText(lujing, Encoding.UTF8), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var shijian)
                ? shijian
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ShortProcessText((int exitCode, string output, string error) jieguo)
    {
        var wenben = string.IsNullOrWhiteSpace(jieguo.error) ? jieguo.output : jieguo.error;
        wenben = wenben.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        if (wenben.Length > 120) wenben = wenben[..120];
        return string.IsNullOrWhiteSpace(wenben) ? "exit " + jieguo.exitCode.ToString(CultureInfo.InvariantCulture) : wenben;
    }

    private static string QuoteArgument(string dangqianzhi)
    {
        return "\"" + dangqianzhi.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
