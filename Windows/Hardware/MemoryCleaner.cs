using System.Diagnostics;

namespace HwMonitor.Hardware;

internal readonly record struct MemoryCleanupResult(int chenggongshuliang, long shifangzijie);

internal readonly record struct MemoryCleanupCandidate(
    int jinchengbianhao,
    int dangqianjinchengbianhao,
    string mingcheng,
    int huihuabianhao,
    int dangqianhuihuabianhao,
    bool youkeshichuangkou,
    bool youkeshizuxianchuangkou,
    string zhixingwenjianlujing,
    string windowsmulu,
    long gongzuojizijie);

internal static class MemoryCleaner
{
    private const uint shezhizijinpeie = 0x0100;
    private const uint youxianchaxunxinxi = 0x1000;
    private const long zuixiaogongzuojizijie = 128L * 1024 * 1024;

    private static readonly HashSet<string> guanjianjinchengming = new(StringComparer.OrdinalIgnoreCase)
    {
        "system",
        "registry",
        "smss",
        "csrss",
        "wininit",
        "services",
        "lsass",
        "winlogon",
        "svchost",
        "fontdrvhost",
        "dwm",
        "explorer",
        "sihost",
        "taskhostw",
        "ctfmon",
        "audiodg",
        "conhost",
        "dllhost",
        "runtimebroker",
        "searchhost",
        "startmenuexperiencehost",
        "shellexperiencehost",
        "textinputhost",
        "applicationframehost",
        "securityhealthservice",
        "securityhealthsystray",
        "securityhealthhost",
        "memory compression",
        "msmpeng",
        "nissrv",
        "hipsdaemon",
        "hipstray"
    };

    private static readonly HashSet<string> jiaohuruanjianming = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "msedge",
        "firefox",
        "opera",
        "brave",
        "vivaldi",
        "webviewhost",
        "msedgewebview2",
        "jcef_helper",
        "devenv",
        "rider64",
        "rider.backend",
        "clion64",
        "idea64",
        "pycharm64",
        "webstorm64",
        "code",
        "codex",
        "node",
        "dotnet",
        "vbcscompiler",
        "powershell",
        "pwsh",
        "cmd",
        "windowsterminal",
        "wsl",
        "bash",
        "wetype_server",
        "wechat",
        "qq",
        "tim",
        "dingtalk",
        "feishu",
        "teams",
        "slack",
        "discord",
        "telegram"
    };

    public static MemoryCleanupResult Clean()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var chenggongshuliang = 0;
        long shifangzijie = 0;
        var dangqianjinchengbianhao = Environment.ProcessId;
        using var dangqianjincheng = Process.GetCurrentProcess();
        var dangqianhuihuabianhao = dangqianjincheng.SessionId;
        var windowsmulu = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        if (!ChangshiDuquFujinchengBiao(out var fujinchengbiao)) return new MemoryCleanupResult(0, 0);
        foreach (var jincheng in Process.GetProcesses())
        {
            using (jincheng)
            {
                if (!ChangshiChuangjianHouxuan(jincheng, dangqianjinchengbianhao, dangqianhuihuabianhao, windowsmulu, fujinchengbiao, out var houxuan)) continue;
                if (!ShouldClean(houxuan)) continue;

                var jubing = NativeMethods.OpenProcess(shezhizijinpeie | youxianchaxunxinxi, false, (uint)jincheng.Id);
                if (jubing == IntPtr.Zero) continue;

                try
                {
                    var qian = DuquGongzuojiDaxiao(jincheng);
                    if (!NativeMethods.K32EmptyWorkingSet(jubing)) continue;
                    chenggongshuliang++;
                    jincheng.Refresh();
                    var hou = DuquGongzuojiDaxiao(jincheng);
                    if (qian > hou) shifangzijie += qian - hou;
                }
                catch
                {
                }
                finally
                {
                    NativeMethods.CloseHandle(jubing);
                }
            }
        }

        return new MemoryCleanupResult(chenggongshuliang, shifangzijie);
    }

    internal static bool ShouldClean(MemoryCleanupCandidate houxuan)
    {
        if (houxuan.jinchengbianhao <= 4) return false;
        if (houxuan.jinchengbianhao == houxuan.dangqianjinchengbianhao) return false;
        if (houxuan.huihuabianhao != houxuan.dangqianhuihuabianhao) return false;
        if (houxuan.youkeshichuangkou) return false;
        if (houxuan.youkeshizuxianchuangkou) return false;
        if (string.IsNullOrWhiteSpace(houxuan.mingcheng)) return false;
        if (guanjianjinchengming.Contains(houxuan.mingcheng)) return false;
        if (jiaohuruanjianming.Contains(houxuan.mingcheng)) return false;
        if (string.IsNullOrWhiteSpace(houxuan.zhixingwenjianlujing)) return false;
        if (string.IsNullOrWhiteSpace(houxuan.windowsmulu)) return false;
        if (LuJingWeiYuMulu(houxuan.zhixingwenjianlujing, houxuan.windowsmulu)) return false;
        return houxuan.gongzuojizijie >= zuixiaogongzuojizijie;
    }

    private static bool ChangshiChuangjianHouxuan(Process jincheng, int dangqianjinchengbianhao, int dangqianhuihuabianhao, string windowsmulu, IReadOnlyDictionary<int, int> fujinchengbiao, out MemoryCleanupCandidate houxuan)
    {
        houxuan = default;
        try
        {
            jincheng.Refresh();
            var mingcheng = jincheng.ProcessName;
            var huihuabianhao = jincheng.SessionId;
            var youkeshichuangkou = jincheng.MainWindowHandle != IntPtr.Zero;
            if (!ChangshiJianchaKeShiZuXian(jincheng.Id, fujinchengbiao, out var youkeshizuxianchuangkou)) return false;
            var zhixingwenjianlujing = jincheng.MainModule?.FileName;
            var gongzuojizijie = jincheng.WorkingSet64;
            if (string.IsNullOrWhiteSpace(zhixingwenjianlujing)) return false;

            houxuan = new MemoryCleanupCandidate(
                jincheng.Id,
                dangqianjinchengbianhao,
                mingcheng,
                huihuabianhao,
                dangqianhuihuabianhao,
                youkeshichuangkou,
                youkeshizuxianchuangkou,
                zhixingwenjianlujing,
                windowsmulu,
                gongzuojizijie);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ChangshiDuquFujinchengBiao(out Dictionary<int, int> fujinchengbiao)
    {
        fujinchengbiao = [];
        var kuaizhao = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.jinchengkuaizhaobiaozhi, 0);
        if (kuaizhao == NativeMethods.wuxiaojubingzhi) return false;

        try
        {
            var tiaomu = new NativeMethods.ProcessEntry32
            {
                daxiao = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.ProcessEntry32>()
            };
            if (!NativeMethods.Process32First(kuaizhao, ref tiaomu)) return false;

            do
            {
                fujinchengbiao[(int)tiaomu.jinchengbianhao] = (int)tiaomu.fujinchengbianhao;
                tiaomu.daxiao = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.ProcessEntry32>();
            }
            while (NativeMethods.Process32Next(kuaizhao, ref tiaomu));

            return true;
        }
        finally
        {
            NativeMethods.CloseHandle(kuaizhao);
        }
    }

    private static bool ChangshiJianchaKeShiZuXian(int jinchengbianhao, IReadOnlyDictionary<int, int> fujinchengbiao, out bool youkeshizuxianchuangkou)
    {
        youkeshizuxianchuangkou = false;
        var yifangwen = new HashSet<int> { jinchengbianhao };
        var dangqianbianhao = jinchengbianhao;
        for (var cishu = 0; cishu < 16; cishu++)
        {
            if (!fujinchengbiao.TryGetValue(dangqianbianhao, out var fujinchengbianhao)) return cishu > 0;
            if (fujinchengbianhao <= 0) return true;
            if (!yifangwen.Add(fujinchengbianhao)) return false;

            try
            {
                using var fujincheng = Process.GetProcessById(fujinchengbianhao);
                fujincheng.Refresh();
                if (fujincheng.MainWindowHandle != IntPtr.Zero)
                {
                    youkeshizuxianchuangkou = true;
                    return true;
                }
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch
            {
                return false;
            }

            dangqianbianhao = fujinchengbianhao;
        }

        return false;
    }

    private static bool LuJingWeiYuMulu(string wenjianlujing, string mulu)
    {
        var wanzhengwenjianlujing = Path.GetFullPath(wenjianlujing);
        var wanzhengmulu = Path.TrimEndingDirectorySeparator(Path.GetFullPath(mulu));
        return wanzhengwenjianlujing.Equals(wanzhengmulu, StringComparison.OrdinalIgnoreCase) ||
               wanzhengwenjianlujing.StartsWith(wanzhengmulu + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static long DuquGongzuojiDaxiao(Process jincheng)
    {
        try
        {
            return jincheng.WorkingSet64;
        }
        catch
        {
            return 0;
        }
    }
}
