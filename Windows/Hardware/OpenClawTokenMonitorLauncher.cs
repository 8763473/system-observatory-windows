using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace HwMonitor.Hardware;

internal readonly record struct OpenClawTokenMonitorResult(
    bool chenggong,
    bool yiyunxing,
    string zhongwentishi,
    string yingwentishi,
    int? jinchengbianhao);

internal readonly record struct TokenMonitorSnapshot(
    bool peizhiwanzheng,
    bool yunxing,
    bool bengyingyongqidong,
    bool waibuyiyunxing,
    bool houtairenwuyianzhuang,
    bool houtaicaijiqiyunxing,
    int? houtaicaijiqijincheng,
    string houtaizhuangtai,
    string houtaiyingwenzhuangtai,
    DateTime? houtaixintiaoshijian,
    int? jinchengbianhao,
    string zhuangtai,
    string yingwenzhuangtai,
    string jiantingdizhi,
    string mubiaodizhi,
    string tongjiriqi,
    string moxingmingcheng,
    int shurutokens,
    int shuchutokens,
    int jinrishurutokens,
    int jinrishuchutokens,
    int jinritokens,
    int zongshurutokens,
    int zongshuchutokens,
    int zongtokens,
    TokenUsageSummary tongjizonglan,
    IReadOnlyList<TokenDailyUsage> huodongriqi,
    double shishitokensudu,
    double zhunquesudu,
    double tigansudu,
    double zonghaoshi,
    double shouziziyanmiao,
    bool shouziziyangusuan,
    DateTime? gengxinshijian,
    IReadOnlyList<string> rizhi);

internal static class OpenClawTokenMonitorLauncher
{
    public const string jiankongmulu = @"E:\personal\Desktop\OpenClaw-Token-Monitor";
    public const string jiaobenming = "ollama-token-proxy.js";
    public const string yunxingrizhiming = "token-proxy-runtime.log";
    public const string jiantingdizhi = "http://127.0.0.1:11435";
    public const string mubiaodizhi = "http://127.0.0.1:11434";

    public static readonly string jiaobenlujing = Path.Combine(jiankongmulu, jiaobenming);
    public static readonly string yunxingrizhilujing = Path.Combine(jiankongmulu, yunxingrizhiming);

    private static readonly object suo = new();
    private static readonly List<string> rizhi = new();
    private static Process? jincheng;
    private static int? waibujincheng;
    private static bool waibujianting;
    private static bool waiburizhichushihua;
    private static long waiburizhiweizhi;
    private static DateTime shangciwaibuchaxun = DateTime.MinValue;
    private static DateTime shangcirizhidaoru = DateTime.MinValue;
    private static string shangcizuijinrizhijian = "";
    private static string zhuangtai = "未启动";
    private static string yingwenzhuangtai = "Not started";
    private static string moxingmingcheng = "未知模型";
    private static string tongjiriqi = TokenUsageStore.LoadToday().riqi;
    private static int shurutokens;
    private static int shuchutokens;
    private static int jinrishurutokens = TokenUsageStore.LoadToday().shurutokens;
    private static int jinrishuchutokens = TokenUsageStore.LoadToday().shuchutokens;
    private static int zongshurutokens;
    private static int zongshuchutokens;
    private static int benlunshurutokens;
    private static int benlunshuchutokens;
    private static bool benlunyijiru;
    private static TokenHardwarePeakSampler? benlunfengzhicaiji;
    private static TokenHardwarePeak benlunyingjianfengzhi = TokenHardwarePeak.Empty;
    private static double shishitokensudu = double.NaN;
    private static double zhunquesudu = double.NaN;
    private static double tigansudu = double.NaN;
    private static double zonghaoshi = double.NaN;
    private static double shouziziyanmiao = double.NaN;
    private static bool shouziziyangusuan;
    private static DateTime? gengxinshijian;

    public static OpenClawTokenMonitorResult Start()
    {
        if (!Directory.Exists(jiankongmulu))
        {
            return Fail("未找到 OpenClaw Token 监控目录", "OpenClaw Token monitor folder was not found");
        }

        if (!File.Exists(jiaobenlujing))
        {
            return Fail("未找到 Token 监控脚本", "OpenClaw Token monitor script was not found");
        }

        lock (suo)
        {
            if (jincheng is not null && !jincheng.HasExited)
            {
                zhuangtai = "监控运行中";
                yingwenzhuangtai = "Monitor running";
                return new OpenClawTokenMonitorResult(
                    chenggong: true,
                    yiyunxing: true,
                    zhongwentishi: $"OpenClaw Token 监控正在图形界面中运行 (PID {jincheng.Id})",
                    yingwentishi: $"OpenClaw Token monitor is running in the graphical page (PID {jincheng.Id})",
                    jinchengbianhao: jincheng.Id);
            }
        }

        var yunxing = FindRunningMonitorProcessId();
        var duankouyiyunxing = IsMonitorPortListening();
        if (yunxing is not null || duankouyiyunxing)
        {
            lock (suo)
            {
                waibujincheng = yunxing;
                waibujianting = duankouyiyunxing;
                zhuangtai = "外部监控已运行";
                yingwenzhuangtai = "External monitor running";
                AddLogLocked(yunxing is null
                    ? "外部 OpenClaw Token 监控端口已在监听"
                    : $"外部 OpenClaw Token 监控已在运行 (PID {yunxing.Value})");
                ReadExternalRuntimeLogLocked(jiluyongliang: false);
            }

            return new OpenClawTokenMonitorResult(
                chenggong: true,
                yiyunxing: true,
                zhongwentishi: yunxing is null ? "OpenClaw Token 监控端口已在外部监听" : $"OpenClaw Token 监控已在外部运行 (PID {yunxing.Value})",
                yingwentishi: yunxing is null ? "OpenClaw Token monitor port is already listening externally" : $"OpenClaw Token monitor is already running externally (PID {yunxing.Value})",
                jinchengbianhao: yunxing);
        }

        var nodelujing = FindNodePath();
        if (string.IsNullOrWhiteSpace(nodelujing))
        {
            return Fail("未找到 Node.js，无法启动 Token 监控", "Node.js was not found, so Token monitor could not start");
        }

        try
        {
            var qidong = BuildStartInfo(nodelujing);
            var xinjincheng = new Process
            {
                StartInfo = qidong,
                EnableRaisingEvents = true
            };
            xinjincheng.OutputDataReceived += (_, shijian) => HandleOutputLine(shijian.Data, cuowu: false);
            xinjincheng.ErrorDataReceived += (_, shijian) => HandleOutputLine(shijian.Data, cuowu: true);
            xinjincheng.Exited += (_, _) => HandleProcessExit(xinjincheng);

            if (!xinjincheng.Start())
            {
                xinjincheng.Dispose();
                return Fail("Token 监控启动失败", "Token monitor failed to start");
            }

            xinjincheng.BeginOutputReadLine();
            xinjincheng.BeginErrorReadLine();

            lock (suo)
            {
                jincheng?.Dispose();
                jincheng = xinjincheng;
                waibujincheng = null;
                waibujianting = false;
                ResetRoundLocked();
                RefreshDailyUsageLocked();
                zhuangtai = "监控启动中";
                yingwenzhuangtai = "Monitor starting";
                AddLogLocked($"已隐藏启动 OpenClaw Token 监控 (PID {xinjincheng.Id})");
            }

            return new OpenClawTokenMonitorResult(
                chenggong: true,
                yiyunxing: false,
                zhongwentishi: $"已打开 Token 监控图形页面 (PID {xinjincheng.Id})",
                yingwentishi: $"Token monitor graphical page is active (PID {xinjincheng.Id})",
                jinchengbianhao: xinjincheng.Id);
        }
        catch (Exception yichang)
        {
            return Fail($"Token 监控启动失败：{yichang.Message}", $"Token monitor failed to start: {yichang.Message}");
        }
    }

    public static TokenMonitorSnapshot Snapshot()
    {
        var xianzai = DateTime.UtcNow;
        var houtai = TokenCollectorService.Snapshot();
        lock (suo)
        {
            RefreshDailyUsageLocked();
            if (jincheng is not null && jincheng.HasExited)
            {
                jincheng.Dispose();
                jincheng = null;
                zhuangtai = "监控已退出";
                yingwenzhuangtai = "Monitor exited";
            }

            if (jincheng is null && (xianzai - shangciwaibuchaxun).TotalSeconds > 12)
            {
                waibujincheng = FindRunningMonitorProcessId();
                waibujianting = IsMonitorPortListening();
                shangciwaibuchaxun = xianzai;
                if (waibujincheng is not null || waibujianting)
                {
                    zhuangtai = "外部监控已运行";
                    yingwenzhuangtai = "External monitor running";
                }
            }
            if (jincheng is null && (waibujincheng is not null || waibujianting))
            {
                ReadExternalRuntimeLogLocked(jiluyongliang: true);
            }

            var peizhiwanzheng = Directory.Exists(OpenClawUsageLogReader.OpenClawRootPath()) || Directory.Exists(jiankongmulu) && File.Exists(jiaobenlujing);
            var yunxing = jincheng is not null && !jincheng.HasExited || waibujincheng is not null || waibujianting || houtai.caijiqiyunxing;
            var jinchengbianhao = jincheng is not null && !jincheng.HasExited ? jincheng.Id : waibujincheng;
            var zhuangtai2 = jincheng is null && waibujincheng is null && !waibujianting ? houtai.zhuangtai : zhuangtai;
            var yingwenzhuangtai2 = jincheng is null && waibujincheng is null && !waibujianting ? houtai.yingwenzhuangtai : yingwenzhuangtai;
            var tongjizonglan = TokenUsageStore.LoadSummary();
            var huodongriqi = TokenUsageStore.LoadActivity(tianshu: 371);
            var zuijin = TokenUsageStore.LoadRecent();
            if (zuijin is not null)
            {
                AddRecentUsageLogLocked(zuijin.Value);
            }

            var shouziziyanmiao2 = double.IsFinite(shouziziyanmiao) && shouziziyanmiao > 0
                ? shouziziyanmiao
                : zuijin?.shouziziyanmiao ?? double.NaN;
            var shouziziyangusuan2 = double.IsFinite(shouziziyanmiao) && shouziziyanmiao > 0
                ? shouziziyangusuan
                : zuijin?.shouziziyangusuan ?? false;
            if ((!double.IsFinite(shouziziyanmiao2) || shouziziyanmiao2 <= 0) && zuijin is not null)
            {
                var gusuan = OpenClawUsageLogReader.EstimateFirstTokenLatencyFromTurnDuration(zuijin.Value.renwumiao, zuijin.Value.shurutokens, zuijin.Value.shuchutokens);
                if (gusuan > 0)
                {
                    shouziziyanmiao2 = gusuan;
                    shouziziyangusuan2 = true;
                }
            }
            var zhunquesudu2 = double.IsFinite(zhunquesudu) && zhunquesudu > 0
                ? zhunquesudu
                : zuijin is not null && zuijin.Value.tokenspersecond > 0
                    ? zuijin.Value.tokenspersecond
                    : zuijin is not null
                        ? OpenClawUsageLogReader.CalculateAccurateTokenSpeed(zuijin.Value.shurutokens, zuijin.Value.shuchutokens, zuijin.Value.renwumiao, shouziziyanmiao2)
                        : double.NaN;
            var shishisudu2 = double.IsFinite(shishitokensudu) && shishitokensudu > 0
                ? shishitokensudu
                : zuijin is not null
                    ? OpenClawUsageLogReader.CalculateOverallTokenSpeed(zuijin.Value.shurutokens, zuijin.Value.shuchutokens, zuijin.Value.renwumiao)
                    : double.NaN;

            return new TokenMonitorSnapshot(
                peizhiwanzheng,
                yunxing,
                bengyingyongqidong: jincheng is not null && !jincheng.HasExited,
                waibuyiyunxing: jincheng is null && (waibujincheng is not null || waibujianting),
                houtai.renwuyianzhuang,
                houtai.caijiqiyunxing,
                houtai.jinchengbianhao,
                houtai.zhuangtai,
                houtai.yingwenzhuangtai,
                houtai.xintiaoshijian,
                jinchengbianhao,
                zhuangtai2,
                yingwenzhuangtai2,
                jiantingdizhi,
                mubiaodizhi,
                tongjiriqi,
                zuijin is not null && !string.IsNullOrWhiteSpace(zuijin.Value.moxingmingcheng) ? zuijin.Value.moxingmingcheng : moxingmingcheng,
                shurutokens > 0 ? shurutokens : zuijin?.shurutokens ?? 0,
                shuchutokens > 0 ? shuchutokens : zuijin?.shuchutokens ?? 0,
                jinrishurutokens,
                jinrishuchutokens,
                jinrishurutokens + jinrishuchutokens,
                zongshurutokens,
                zongshuchutokens,
                zongshurutokens + zongshuchutokens,
                tongjizonglan,
                huodongriqi,
                shishisudu2,
                zhunquesudu2,
                tigansudu,
                double.IsFinite(zonghaoshi) && zonghaoshi > 0 ? zonghaoshi : zuijin?.renwumiao ?? double.NaN,
                shouziziyanmiao2,
                shouziziyangusuan2,
                gengxinshijian ?? zuijin?.shijian,
                rizhi.ToArray());
        }
    }

    public static OpenClawUsageImportResult ImportUsageLogs()
    {
        var jieguo = OpenClawUsageLogReader.ImportKnownUsageLogs();
        lock (suo)
        {
            shangcirizhidaoru = DateTime.UtcNow;
            ApplyImportResultLocked(jieguo);
        }

        return jieguo;
    }

    internal static void ImportUsageLogsForTest(string wenjianlujing)
    {
        lock (suo)
        {
            ApplyImportResultLocked(new OpenClawUsageImportResult(0, 0, 0, 0, 0, TokenUsageStore.LoadRecent(wenjianlujing)));
        }
    }

    public static void StopOwnedProcess()
    {
        var daitingzhi = DetachOwnedProcessForStop();
        StopProcessQuietly(daitingzhi, shifang: true);
    }

    public static void RequestStopOwnedProcess()
    {
        var daitingzhi = DetachOwnedProcessForStop();
        if (daitingzhi is null) return;
        try
        {
            if (!daitingzhi.HasExited)
            {
                daitingzhi.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }

        _ = Task.Run(() => StopProcessQuietly(daitingzhi, shifang: true));
    }

    private static Process? DetachOwnedProcessForStop()
    {
        lock (suo)
        {
            if (jincheng is null) return null;
            var daitingzhi = jincheng;
            jincheng = null;
            zhuangtai = "监控已停止";
            yingwenzhuangtai = "Monitor stopped";
            AddLogLocked("已请求停止图形界面托管的 Token 监控");
            return daitingzhi;
        }
    }

    private static void StopProcessQuietly(Process? daitingzhi, bool shifang)
    {
        if (daitingzhi is null) return;
        try
        {
            if (!daitingzhi.HasExited)
            {
                daitingzhi.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }

        if (!shifang) return;
        try
        {
            daitingzhi.Dispose();
        }
        catch
        {
        }
    }

    internal static ProcessStartInfo BuildStartInfo(string nodelujing)
    {
        return new ProcessStartInfo
        {
            FileName = nodelujing,
            Arguments = QuoteArgument(jiaobenlujing),
            WorkingDirectory = jiankongmulu,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }

    internal static string? FindNodePath()
    {
        var houxuan = new List<string>();
        var chengxumulu = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(chengxumulu))
        {
            houxuan.Add(Path.Combine(chengxumulu, "nodejs", "node.exe"));
        }

        var lujingbianliang = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var mulu in lujingbianliang.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(mulu))
            {
                houxuan.Add(Path.Combine(mulu, "node.exe"));
            }
        }

        foreach (var nodelujing in houxuan.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(nodelujing)) return nodelujing;
            }
            catch
            {
            }
        }

        return null;
    }

    private static void HandleOutputLine(string? hangwenben, bool cuowu)
    {
        if (string.IsNullOrWhiteSpace(hangwenben)) return;
        lock (suo)
        {
            ProcessRuntimeLogLineLocked(hangwenben, cuowu, jiluyongliang: true);
        }
    }

    internal static TokenDailyUsage IngestRuntimeLogLinesForTest(string wenjianlujing, DateOnly riqi, IEnumerable<string> hangwenben)
    {
        lock (suo)
        {
            ResetRoundLocked();
            foreach (var hang in hangwenben)
            {
                ProcessRuntimeLogLineLocked(hang, cuowu: false, jiluyongliang: true, wenjianlujing, riqi);
            }

            return TokenUsageStore.LoadDay(wenjianlujing, riqi);
        }
    }

    private static void ProcessRuntimeLogLineLocked(string? hangwenben, bool cuowu, bool jiluyongliang, string? wenjianlujing = null, DateOnly? riqi = null)
    {
        if (string.IsNullOrWhiteSpace(hangwenben)) return;
        var wenben = hangwenben.Trim();
        if (wenben.Length == 0 || wenben.All(dangqianzhi => dangqianzhi == '=')) return;

        AddLogLocked(cuowu ? "错误: " + wenben : wenben);
        gengxinshijian = DateTime.UtcNow;
        if (cuowu)
        {
            zhuangtai = "监控异常";
            yingwenzhuangtai = "Monitor error";
        }

        if (wenben.Contains("已启动", StringComparison.OrdinalIgnoreCase))
        {
            zhuangtai = "监控运行中";
            yingwenzhuangtai = "Monitor running";
        }
        else if (wenben.Contains("收到请求", StringComparison.OrdinalIgnoreCase))
        {
            zhuangtai = "正在统计请求";
            yingwenzhuangtai = "Collecting request";
            var moxing = TextAfter(wenben, "| 模型:");
            if (!string.IsNullOrWhiteSpace(moxing)) moxingmingcheng = moxing;
            benlunshurutokens = 0;
            benlunshuchutokens = 0;
            benlunyijiru = false;
            shouziziyanmiao = double.NaN;
            shouziziyangusuan = false;
            BeginRoundHardwarePeakLocked(jiluyongliang && wenjianlujing is null);
        }
        else if (wenben.Contains("本轮回复完成", StringComparison.OrdinalIgnoreCase))
        {
            zhuangtai = "本轮统计完成";
            yingwenzhuangtai = "Round completed";
        }

        var moxing2 = TextAfter(wenben, "模型名称:");
        if (!string.IsNullOrWhiteSpace(moxing2)) moxingmingcheng = moxing2;

        var shuru = IntAfter(wenben, "输入 tokens:");
        if (shuru is not null)
        {
            shurutokens = shuru.Value;
            benlunshurutokens = shuru.Value;
        }

        var shuchu = IntAfter(wenben, "输出 tokens:");
        if (shuchu is not null)
        {
            shuchutokens = shuchu.Value;
            benlunshuchutokens = shuchu.Value;
        }

        var zhunquesudu2 = DoubleAfter(wenben, "准确输出速度:", "token/s");
        if (zhunquesudu2 is not null)
        {
            zhunquesudu = zhunquesudu2.Value;
        }

        var shishisudu2 = DoubleAfter(wenben, "实时近似速度:", "token/s");
        if (shishisudu2 is not null) shishitokensudu = shishisudu2.Value;

        var tigansudu2 = DoubleAfter(wenben, "体感输出速度:", "token/s");
        if (tigansudu2 is not null) tigansudu = tigansudu2.Value;

        var shouzi2 = FirstTokenLatencyAfter(wenben);
        if (shouzi2 is not null)
        {
            shouziziyanmiao = shouzi2.Value;
            shouziziyangusuan = false;
        }

        var zonghaoshi2 = DoubleAfter(wenben, "本轮总耗时:", "秒");
        if (zonghaoshi2 is not null)
        {
            zonghaoshi = zonghaoshi2.Value;
            if (jiluyongliang)
            {
                AddCompletedRoundLocked(wenjianlujing, riqi);
            }
        }
    }

    private static void ResetRoundLocked()
    {
        shurutokens = 0;
        shuchutokens = 0;
        benlunshurutokens = 0;
        benlunshuchutokens = 0;
        benlunyijiru = false;
        benlunfengzhicaiji?.Dispose();
        benlunfengzhicaiji = null;
        benlunyingjianfengzhi = TokenHardwarePeak.Empty;
        zhunquesudu = double.NaN;
        tigansudu = double.NaN;
        zonghaoshi = double.NaN;
        shouziziyanmiao = double.NaN;
        shouziziyangusuan = false;
        shishitokensudu = double.NaN;
    }

    private static void AddCompletedRoundLocked(string? wenjianlujing = null, DateOnly? riqi = null)
    {
        benlunyingjianfengzhi = FinishRoundHardwarePeakLocked();
        if (benlunyijiru) return;
        var shuru = Math.Max(0, benlunshurutokens > 0 ? benlunshurutokens : shurutokens);
        var shuchu = Math.Max(0, benlunshuchutokens > 0 ? benlunshuchutokens : shuchutokens);
        if (shuru + shuchu <= 0) return;

        if (!double.IsFinite(shouziziyanmiao) || shouziziyanmiao <= 0)
        {
            var gusuan = OpenClawUsageLogReader.EstimateFirstTokenLatency(zonghaoshi, shuchu, zhunquesudu);
            if (gusuan > 0)
            {
                shouziziyanmiao = gusuan;
                shouziziyangusuan = true;
            }
        }
        if (!double.IsFinite(shouziziyanmiao) || shouziziyanmiao <= 0)
        {
            var gusuan = OpenClawUsageLogReader.EstimateFirstTokenLatencyFromTurnDuration(zonghaoshi, shuru, shuchu);
            if (gusuan > 0)
            {
                shouziziyanmiao = gusuan;
                shouziziyangusuan = true;
            }
        }
        zhunquesudu = OpenClawUsageLogReader.CalculateAccurateTokenSpeed(shuru, shuchu, zonghaoshi, shouziziyanmiao);
        if (!double.IsFinite(shishitokensudu) || shishitokensudu <= 0)
        {
            shishitokensudu = OpenClawUsageLogReader.CalculateOverallTokenSpeed(shuru, shuchu, zonghaoshi);
        }

        var tongjiriqi2 = riqi ?? DateOnly.FromDateTime(DateTime.Now);
        var tongjiwenjian = wenjianlujing ?? TokenUsageStore.DefaultPath();
        var quchongjian = "runtime-live:" +
                  DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) +
                  "|" + moxingmingcheng +
                  "|" + shuru.ToString(CultureInfo.InvariantCulture) +
                  "|" + shuchu.ToString(CultureInfo.InvariantCulture) +
                  "|" + (double.IsFinite(zonghaoshi) ? zonghaoshi.ToString("0.###", CultureInfo.InvariantCulture) : "0");
        var tianjia = TokenUsageStore.AddUsageIfNew(
            tongjiwenjian,
            quchongjian,
            tongjiriqi2,
            shuru,
            shuchu,
            zonghaoshi,
            moxingmingcheng,
            zhunquesudu,
            DateTime.UtcNow,
            double.IsFinite(shouziziyanmiao) ? shouziziyanmiao : 0,
            shouziziyangusuan,
            benlunyingjianfengzhi);
        var dangri = tianjia.yongliang;
        tongjiriqi = dangri.riqi;
        jinrishurutokens = dangri.shurutokens;
        jinrishuchutokens = dangri.shuchutokens;
        zongshurutokens = dangri.shurutokens;
        zongshuchutokens = dangri.shuchutokens;
        benlunyijiru = true;
        AddLogLocked($"今日累计 tokens：输入 {jinrishurutokens}，输出 {jinrishuchutokens}，总计 {jinrishurutokens + jinrishuchutokens}");
    }

    private static void BeginRoundHardwarePeakLocked(bool qiyong)
    {
        benlunfengzhicaiji?.Dispose();
        benlunfengzhicaiji = null;
        benlunyingjianfengzhi = TokenHardwarePeak.Empty;
        if (!qiyong) return;
        benlunfengzhicaiji = TokenHardwarePeakSampler.Start();
    }

    private static TokenHardwarePeak FinishRoundHardwarePeakLocked()
    {
        var caijiqi = benlunfengzhicaiji;
        benlunfengzhicaiji = null;
        if (caijiqi is null) return benlunyingjianfengzhi;
        try
        {
            return caijiqi.StopAndSnapshot();
        }
        finally
        {
            caijiqi.Dispose();
        }
    }

    private static void ApplyImportResultLocked(OpenClawUsageImportResult jieguo)
    {
        RefreshDailyUsageLocked();
        if (jieguo.zuijin is not null)
        {
            var zuijin = jieguo.zuijin.Value;
            moxingmingcheng = string.IsNullOrWhiteSpace(zuijin.moxingmingcheng) ? moxingmingcheng : zuijin.moxingmingcheng;
            shurutokens = zuijin.shurutokens;
            shuchutokens = zuijin.shuchutokens;
            zonghaoshi = zuijin.renwumiao > 0 ? zuijin.renwumiao : zonghaoshi;
            if (zuijin.shouziziyanmiao > 0)
            {
                shouziziyanmiao = zuijin.shouziziyanmiao;
                shouziziyangusuan = zuijin.shouziziyangusuan;
            }
            else
            {
                var gusuan = OpenClawUsageLogReader.EstimateFirstTokenLatencyFromTurnDuration(zuijin.renwumiao, zuijin.shurutokens, zuijin.shuchutokens);
                if (gusuan > 0)
                {
                    shouziziyanmiao = gusuan;
                    shouziziyangusuan = true;
                }
            }
            if (zuijin.tokenspersecond > 0)
            {
                zhunquesudu = zuijin.tokenspersecond;
            }
            else
            {
                zhunquesudu = OpenClawUsageLogReader.CalculateAccurateTokenSpeed(zuijin.shurutokens, zuijin.shuchutokens, zuijin.renwumiao, shouziziyanmiao);
            }
            shishitokensudu = OpenClawUsageLogReader.CalculateOverallTokenSpeed(zuijin.shurutokens, zuijin.shuchutokens, zuijin.renwumiao);
            gengxinshijian = zuijin.shijian;
            AddRecentUsageLogLocked(zuijin);
        }

        if (jieguo.daorushuliang > 0)
        {
            zhuangtai = "日志已补读";
            yingwenzhuangtai = "Logs imported";
            AddLogLocked($"已从 OpenClaw 日志补读 {jieguo.daorushuliang} 条 Token 记录");
        }
        else if (jieguo.biaojishuliang > 0)
        {
            zhuangtai = "历史日志已建立基线";
            yingwenzhuangtai = "History baseline recorded";
            AddLogLocked($"已为 {jieguo.biaojishuliang} 条历史 Token 记录建立去重基线");
        }
    }

    private static void RefreshDailyUsageLocked()
    {
        var dangri = TokenUsageStore.LoadToday();
        if (!string.Equals(tongjiriqi, dangri.riqi, StringComparison.Ordinal))
        {
            ResetRoundLocked();
        }

        tongjiriqi = dangri.riqi;
        jinrishurutokens = dangri.shurutokens;
        jinrishuchutokens = dangri.shuchutokens;
        zongshurutokens = dangri.shurutokens;
        zongshuchutokens = dangri.shuchutokens;
    }

    private static void HandleProcessExit(Process tuichujincheng)
    {
        lock (suo)
        {
            if (!ReferenceEquals(jincheng, tuichujincheng)) return;
            var daima = 0;
            try
            {
                daima = tuichujincheng.ExitCode;
            }
            catch
            {
            }

            zhuangtai = daima == 0 ? "监控已退出" : $"监控已退出 ({daima})";
            yingwenzhuangtai = daima == 0 ? "Monitor exited" : $"Monitor exited ({daima})";
            AddLogLocked(zhuangtai);
        }
    }

    private static void ReadExternalRuntimeLogLocked(bool jiluyongliang)
    {
        if (!File.Exists(yunxingrizhilujing)) return;

        try
        {
            using var liu = new FileStream(yunxingrizhilujing, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var changdu = liu.Length;
            if (changdu < waiburizhiweizhi)
            {
                waiburizhichushihua = false;
                waiburizhiweizhi = 0;
            }

            var shoucidu = !waiburizhichushihua;
            if (shoucidu)
            {
                waiburizhiweizhi = Math.Max(0, changdu - 64 * 1024);
            }

            liu.Position = Math.Clamp(waiburizhiweizhi, 0, changdu);
            using var duqu = new StreamReader(liu, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
            var wenben = duqu.ReadToEnd();
            waiburizhiweizhi = changdu;
            waiburizhichushihua = true;
            if (string.IsNullOrWhiteSpace(wenben)) return;

            var hangshu = wenben.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            var kaishi = shoucidu && changdu > 64 * 1024 ? 1 : 0;
            for (var suoyin = kaishi; suoyin < hangshu.Length; suoyin++)
            {
                ProcessRuntimeLogLineLocked(hangshu[suoyin], cuowu: false, jiluyongliang: !shoucidu && jiluyongliang);
            }
        }
        catch
        {
        }
    }

    private static bool IsMonitorPortListening()
    {
        try
        {
            var lianjie = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            foreach (var duankou in lianjie)
            {
                if (duankou.Port != 11435) continue;
                if (IPAddress.IsLoopback(duankou.Address) || duankou.Address.Equals(IPAddress.Any) || duankou.Address.Equals(IPAddress.IPv6Any))
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private static int? FindRunningMonitorProcessId()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var chaxun = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'node.exe'");
            foreach (ManagementObject jincheng2 in chaxun.Get())
            {
                var mingling = (jincheng2["CommandLine"] as string) ?? "";
                if (!mingling.Contains(jiaobenming, StringComparison.OrdinalIgnoreCase) &&
                    !mingling.Contains(jiaobenlujing, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (int.TryParse(jincheng2["ProcessId"]?.ToString(), out var jinchengbianhao))
                {
                    return jinchengbianhao;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static void AddLogLocked(string wenben)
    {
        var qianzhui = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        rizhi.Add($"[{qianzhui}] {wenben}");
        while (rizhi.Count > 10)
        {
            rizhi.RemoveAt(0);
        }
    }

    private static void AddRecentUsageLogLocked(TokenRecentUsage zuijin)
    {
        if (string.IsNullOrWhiteSpace(zuijin.laiyuanjian)) return;
        if (string.Equals(shangcizuijinrizhijian, zuijin.laiyuanjian, StringComparison.Ordinal)) return;

        shangcizuijinrizhijian = zuijin.laiyuanjian;
        var haoshi = FormatSeconds(zuijin.renwumiao);
        var sudu = zuijin.tokenspersecond > 0
            ? zuijin.tokenspersecond.ToString("0.##", CultureInfo.InvariantCulture) + " token/s"
            : "-";
        var shouzizhi = zuijin.shouziziyanmiao;
        var shouzigusuan = zuijin.shouziziyangusuan;
        if (shouzizhi <= 0)
        {
            shouzizhi = OpenClawUsageLogReader.EstimateFirstTokenLatencyFromTurnDuration(zuijin.renwumiao, zuijin.shurutokens, zuijin.shuchutokens);
            shouzigusuan = shouzizhi > 0;
        }
        if (zuijin.tokenspersecond <= 0 && shouzizhi > 0)
        {
            sudu = OpenClawUsageLogReader.CalculateAccurateTokenSpeed(zuijin.shurutokens, zuijin.shuchutokens, zuijin.renwumiao, shouzizhi).ToString("0.##", CultureInfo.InvariantCulture) + " token/s";
        }
        var shouzi = shouzizhi > 0
            ? (shouzigusuan ? "≈" : "") + FormatSeconds(shouzizhi)
            : "-";
        AddLogLocked($"最近输出：{zuijin.moxingmingcheng}，输入 {zuijin.shurutokens}，输出 {zuijin.shuchutokens}，耗时 {haoshi}，速度 {sudu}，首字 {shouzi}");
        AddLogLocked("本轮峰值：" + FormatHardwarePeak(zuijin.yingjianfengzhi));
    }

    private static string FormatHardwarePeak(TokenHardwarePeak fengzhi)
    {
        return "峰值 CPU " + FormatPeakPercent(fengzhi.cpuzuigaobaifenbi) +
               " / GPU " + FormatPeakPercent(fengzhi.gpuzuigaobaifenbi) +
               " / 显存 " + FormatPeakMemory(fengzhi.xiancunzuigaobaifenbi, fengzhi.xiancunyiyongfengzhi, fengzhi.xiancunzongliang) +
               " / 内存 " + FormatPeakMemory(fengzhi.neicunzuigaobaifenbi, fengzhi.neicunyiyongfengzhi, fengzhi.neicunzongliang);
    }

    private static string FormatPeakMemory(double baifenbi, ulong yiyongqianzijie, ulong zongliangqianzijie)
    {
        var baifenbiwenben = FormatPeakPercent(baifenbi);
        if (yiyongqianzijie > 0 && zongliangqianzijie > 0)
        {
            return baifenbiwenben + " " + FormatGb(yiyongqianzijie) + "/" + FormatGb(zongliangqianzijie);
        }

        if (yiyongqianzijie > 0)
        {
            return baifenbiwenben + " " + FormatGb(yiyongqianzijie);
        }

        return baifenbiwenben;
    }

    private static string FormatPeakPercent(double baifenbi)
    {
        return double.IsFinite(baifenbi) && baifenbi > 0
            ? baifenbi.ToString("0.#", CultureInfo.InvariantCulture) + "%"
            : "-";
    }

    private static string FormatGb(ulong kb)
    {
        return (kb / 1_048_576d).ToString("0.0", CultureInfo.InvariantCulture) + " GB";
    }

    private static string FormatSeconds(double miaoshu)
    {
        if (!double.IsFinite(miaoshu) || miaoshu <= 0) return "-";
        return miaoshu >= 10
            ? miaoshu.ToString("0.#", CultureInfo.InvariantCulture) + "秒"
            : miaoshu.ToString("0.##", CultureInfo.InvariantCulture) + "秒";
    }

    private static string TextAfter(string wenben, string qianzhui)
    {
        var weizhi = wenben.IndexOf(qianzhui, StringComparison.OrdinalIgnoreCase);
        return weizhi < 0 ? "" : wenben[(weizhi + qianzhui.Length)..].Trim();
    }

    private static int? IntAfter(string wenben, string qianzhui)
    {
        var dangqianzhi = TextAfter(wenben, qianzhui);
        if (string.IsNullOrWhiteSpace(dangqianzhi)) return null;
        var kongge = dangqianzhi.IndexOf(' ');
        if (kongge >= 0) dangqianzhi = dangqianzhi[..kongge];
        return int.TryParse(dangqianzhi.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var shuliang) ? shuliang : null;
    }

    private static double? DoubleAfter(string wenben, string qianzhui, string houzhui)
    {
        var dangqianzhi = TextAfter(wenben, qianzhui);
        if (string.IsNullOrWhiteSpace(dangqianzhi)) return null;
        var weizhi = dangqianzhi.IndexOf(houzhui, StringComparison.OrdinalIgnoreCase);
        if (weizhi >= 0) dangqianzhi = dangqianzhi[..weizhi];
        return double.TryParse(dangqianzhi.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var shuliang) ? shuliang : null;
    }

    private static double? FirstTokenLatencyAfter(string wenben)
    {
        foreach (var qianzhui in new[] { "首字延迟:", "首字耗时:", "首字时间:", "首字用时:", "First token latency:", "First token:" })
        {
            var haomiao = DoubleAfter(wenben, qianzhui, "ms");
            if (haomiao is not null && wenben.Contains("ms", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(0, haomiao.Value / 1000d);
            }

            var miao = DoubleAfter(wenben, qianzhui, "秒") ?? DoubleAfter(wenben, qianzhui, "s");
            if (miao is not null)
            {
                return Math.Max(0, miao.Value);
            }
        }

        return null;
    }

    private static OpenClawTokenMonitorResult Fail(string zhongwen, string yingwen)
    {
        lock (suo)
        {
            zhuangtai = zhongwen;
            yingwenzhuangtai = yingwen;
            AddLogLocked(zhongwen);
        }

        return new OpenClawTokenMonitorResult(
            chenggong: false,
            yiyunxing: false,
            zhongwentishi: zhongwen,
            yingwentishi: yingwen,
            jinchengbianhao: null);
    }

    private static string QuoteArgument(string dangqianzhi)
    {
        return "\"" + dangqianzhi.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
