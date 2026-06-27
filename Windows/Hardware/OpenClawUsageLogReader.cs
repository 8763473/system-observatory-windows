using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HwMonitor.Hardware;

internal readonly record struct OpenClawUsageImportResult(
    int daorushuliang,
    int tiaoguoshuliang,
    int biaojishuliang,
    int wenjianshuliang,
    int tiaomujihe,
    TokenRecentUsage? zuijin)
{
    public OpenClawUsageImportResult Add(OpenClawUsageImportResult qita)
    {
        return new OpenClawUsageImportResult(
            daorushuliang + qita.daorushuliang,
            tiaoguoshuliang + qita.tiaoguoshuliang,
            biaojishuliang + qita.biaojishuliang,
            wenjianshuliang + qita.wenjianshuliang,
            tiaomujihe + qita.tiaomujihe,
            qita.zuijin ?? zuijin);
    }
}

internal static class OpenClawUsageLogReader
{
    private const string openclawhuihuamoshimiaoshu = @"agents\*\sessions";
    private const string yunxingrizhilujing = @"E:\personal\Desktop\OpenClaw-Token-Monitor\token-proxy-runtime.log";
    private const string yonglianghuancunming = ".usage-cost-cache.json";

    public static string OpenClawRootPath()
    {
        var yonghumulu = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(yonghumulu)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".openclaw")
            : Path.Combine(yonghumulu, ".openclaw");
    }

    public static OpenClawUsageImportResult ImportKnownUsageLogs(string? wenjianlujing = null)
    {
        var tongjiwenjian = wenjianlujing ?? TokenUsageStore.DefaultPath();
        var youquchongjian = TokenUsageStore.HasIngestedKeys(tongjiwenjian);
        var yiyoutongji = TokenUsageStore.LoadSummary(tongjiwenjian).leijitokens > 0;
        var jixianmoshi = !youquchongjian && yiyoutongji;
        var jieguo = ImportOpenClawUsageLogs(tongjiwenjian, FindOpenClawSessionFiles(), jixianmoshi);
        var yunxingrizhijieguo = ImportRuntimeLog(tongjiwenjian, yunxingrizhilujing, jixianmoshi);
        var huancunjieguo = ImportUsageCostCache(tongjiwenjian, FindUsageCostCachePath(), jixianmoshi);
        return jieguo.Add(yunxingrizhijieguo).Add(huancunjieguo);
    }

    internal static IReadOnlyList<string> FindOpenClawSessionFiles()
    {
        var genmulu = OpenClawRootPath();
        var huihuagenmulu = Path.Combine(genmulu, "agents");
        if (!Directory.Exists(huihuagenmulu)) return Array.Empty<string>();

        try
        {
            return Directory.EnumerateFiles(huihuagenmulu, "*.jsonl", SearchOption.AllDirectories)
                .Where(lujing => lujing.Contains(@"\sessions\", StringComparison.OrdinalIgnoreCase))
                .Where(lujing => !lujing.EndsWith(".trajectory.jsonl", StringComparison.OrdinalIgnoreCase))
                .Where(lujing => !lujing.Contains(".reset.", StringComparison.OrdinalIgnoreCase))
                .OrderBy(lujing => SafeLastWriteUtc(lujing))
                .TakeLast(500)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    internal static OpenClawUsageImportResult ImportOpenClawUsageLogs(string wenjianlujing, IEnumerable<string> huihuawenjianjihe, bool jinbiaojixian)
    {
        _ = openclawhuihuamoshimiaoshu;
        var daorushuliang = 0;
        var tiaoguoshuliang = 0;
        var biaojishuliang = 0;
        var wenjianshuliang = 0;
        var jilu = 0;
        TokenRecentUsage? zuijin = null;

        foreach (var huihuawenjian in huihuawenjianjihe.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(huihuawenjian)) continue;
            wenjianshuliang++;
            DateTime? shanggeyonghushijian = null;
            var hanghao = 0;

            foreach (var hang in ReadSharedLines(huihuawenjian))
            {
                hanghao++;
                var jiexijieguo = TryParseSessionUsage(huihuawenjian, hanghao, hang, ref shanggeyonghushijian);
                if (jiexijieguo is null) continue;

                jilu++;
                var shiyong = jiexijieguo.Value;
                zuijin = NewerRecent(zuijin, shiyong.zuijin);
                if (jinbiaojixian)
                {
                    TokenUsageStore.MarkIngested(wenjianlujing, shiyong.laiyuanjian, shiyong.zuijin);
                    biaojishuliang++;
                    continue;
                }

                var tianjia = TokenUsageStore.AddUsageIfNew(
                    wenjianlujing,
                    shiyong.laiyuanjian,
                    shiyong.riqi,
                    shiyong.shurutokens,
                    shiyong.shuchutokens,
                    shiyong.renwumiao,
                    shiyong.moxingmingcheng,
                    shiyong.tokenspersecond,
                    shiyong.shijian,
                    shiyong.shouziziyanmiao,
                    shiyong.shouziziyangusuan);
                    if (tianjia.yitianjia) daorushuliang++;
                    else tiaoguoshuliang++;

                    if (tianjia.yitianjia && IsDeepSeekModel(shiyong.moxingmingcheng))
                    {
                        TokenUsageStore.AddDeepSeekUsage(wenjianlujing, shiyong.riqi, shiyong.shurutokens, shiyong.shuchutokens);
                    }
                }

            }

        return new OpenClawUsageImportResult(daorushuliang, tiaoguoshuliang, biaojishuliang, wenjianshuliang, jilu, zuijin);
    }

    internal static OpenClawUsageImportResult ImportRuntimeLog(string wenjianlujing, string rizhilujing, bool jinbiaojixian)
    {
        if (!File.Exists(rizhilujing)) return new OpenClawUsageImportResult(0, 0, 0, 0, 0, null);

        var daorushuliang = 0;
        var tiaoguoshuliang = 0;
        var biaojishuliang = 0;
        var jilu = 0;
        TokenRecentUsage? zuijin = null;
        var riqi = DateOnly.FromDateTime(File.GetLastWriteTime(rizhilujing));
        var shijian = File.GetLastWriteTimeUtc(rizhilujing);
        var moxing = "未知模型";
        var shuru = 0;
        var shuchu = 0;
        var sudu = 0d;
        var shouziziyanmiao = 0d;
        var shouziziyangusuan = false;
        var hanghao = 0;

        foreach (var hang in ReadSharedLines(rizhilujing))
        {
            hanghao++;
            var wenben = hang.Trim();
            if (wenben.Length == 0) continue;
            var hangshijian = RuntimeLineTime(wenben, riqi);
            if (hangshijian is not null) shijian = hangshijian.Value.ToUniversalTime();

            var moxing2 = TextAfter(wenben, "模型名称:");
            if (!string.IsNullOrWhiteSpace(moxing2)) moxing = moxing2;

            var shuru2 = IntAfter(wenben, "输入 tokens:");
            if (shuru2 is not null) shuru = shuru2.Value;

            var shuchu2 = IntAfter(wenben, "输出 tokens:");
            if (shuchu2 is not null) shuchu = shuchu2.Value;

            var sudu2 = DoubleAfter(wenben, "准确输出速度:", "token/s");
            if (sudu2 is not null) sudu = sudu2.Value;

            var shouzi2 = FirstTokenLatencyAfter(wenben);
            if (shouzi2 is not null)
            {
                shouziziyanmiao = shouzi2.Value;
                shouziziyangusuan = false;
            }

            var haoshi = DoubleAfter(wenben, "本轮总耗时:", "秒");
            if (haoshi is null || shuru + shuchu <= 0) continue;

            jilu++;
            if (shouziziyanmiao <= 0)
            {
                var gusuan = EstimateFirstTokenLatency(haoshi.Value, shuchu, sudu);
                if (gusuan > 0)
                {
                    shouziziyanmiao = gusuan;
                    shouziziyangusuan = true;
                }
            }
            if (shouziziyanmiao <= 0)
            {
                var gusuan = EstimateFirstTokenLatencyFromTurnDuration(haoshi.Value, shuru, shuchu);
                if (gusuan > 0)
                {
                    shouziziyanmiao = gusuan;
                    shouziziyangusuan = true;
                }
            }
            sudu = CalculateAccurateTokenSpeed(shuru, shuchu, haoshi.Value, shouziziyanmiao);

            var quchongjian = "runtime:" + HashText(Path.GetFullPath(rizhilujing).ToLowerInvariant() + "|" + hanghao.ToString(CultureInfo.InvariantCulture) + "|" + moxing + "|" + shuru + "|" + shuchu + "|" + haoshi.Value.ToString("0.###", CultureInfo.InvariantCulture) + "|" + shouziziyanmiao.ToString("0.###", CultureInfo.InvariantCulture));
            var zuijinjilu = new TokenRecentUsage(quchongjian, riqi.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), moxing, shuru, shuchu, haoshi.Value, sudu, shouziziyanmiao, shouziziyangusuan, shijian);
            zuijin = NewerRecent(zuijin, zuijinjilu);

            if (jinbiaojixian)
            {
                TokenUsageStore.MarkIngested(wenjianlujing, quchongjian, zuijinjilu);
                biaojishuliang++;
            }
            else
            {
                var tianjia = TokenUsageStore.AddUsageIfNew(wenjianlujing, quchongjian, riqi, shuru, shuchu, haoshi.Value, moxing, sudu, shijian, shouziziyanmiao, shouziziyangusuan);
                if (tianjia.yitianjia) daorushuliang++;
                else tiaoguoshuliang++;

                if (tianjia.yitianjia && IsDeepSeekModel(moxing))
                {
                    TokenUsageStore.AddDeepSeekUsage(wenjianlujing, riqi, shuru, shuchu);
                }
            }

            shuru = 0;
            shuchu = 0;
            sudu = 0;
            shouziziyanmiao = 0;
            shouziziyangusuan = false;
        }

        return new OpenClawUsageImportResult(daorushuliang, tiaoguoshuliang, biaojishuliang, 1, jilu, zuijin);
    }

    internal static OpenClawUsageImportResult ImportUsageCostCache(string wenjianlujing, string? huancunlujing, bool jinbiaojixian)
    {
        if (string.IsNullOrWhiteSpace(huancunlujing) || !File.Exists(huancunlujing))
        {
            return new OpenClawUsageImportResult(0, 0, 0, 0, 0, null);
        }

        var daorushuliang = 0;
        var tiaoguoshuliang = 0;
        var biaojishuliang = 0;
        var wenjianshuliang = 0;
        var jilu = 0;
        var zuijinshijian = TokenUsageStore.LoadRecent(wenjianlujing)?.shijian ?? DateTime.MinValue;
        TokenRecentUsage? zuijin = null;

        try
        {
            using var jsonwendang = JsonDocument.Parse(File.ReadAllText(huancunlujing, Encoding.UTF8));
            var wenjianyuansujihe = ObjectProperty(jsonwendang.RootElement, "files");
            if (wenjianyuansujihe.ValueKind != JsonValueKind.Object)
            {
                return new OpenClawUsageImportResult(0, 0, 0, 0, 0, null);
            }

            foreach (var wenjianshuxing in wenjianyuansujihe.EnumerateObject())
            {
                if (wenjianshuxing.Value.ValueKind != JsonValueKind.Object) continue;
                wenjianshuliang++;
                var wenjiantiaomu = wenjianshuxing.Value;
                var huihuawenjian = StringProperty(wenjiantiaomu, "filePath");
                if (string.IsNullOrWhiteSpace(huihuawenjian)) huihuawenjian = wenjianshuxing.Name;

                var zhuanxietiaomujihe = ReadCacheTranscriptEntries(wenjiantiaomu);
                var yongliangtiaomujihe = ArrayProperty(wenjiantiaomu, "usageEntries");
                if (yongliangtiaomujihe.ValueKind != JsonValueKind.Array) continue;

                var suoyin = 0;
                foreach (var yongliang in yongliangtiaomujihe.EnumerateArray())
                {
                    suoyin++;
                    if (yongliang.ValueKind != JsonValueKind.Object) continue;

                    var shijian = DateTimeFromUnixMilliseconds(LongProperty(yongliang, "timestamp")) ?? SafeLastWriteUtc(huancunlujing);
                    if (zuijinshijian > DateTime.MinValue && shijian <= zuijinshijian)
                    {
                        tiaoguoshuliang++;
                        continue;
                    }

                    var shuru = IntProperty(yongliang, "input") ?? IntProperty(yongliang, "inputTokens") ?? IntProperty(yongliang, "promptTokens") ?? 0;
                    var shuchu = IntProperty(yongliang, "output") ?? IntProperty(yongliang, "outputTokens") ?? IntProperty(yongliang, "completionTokens") ?? 0;
                    if (shuru + shuchu <= 0) continue;

                    var moxing = StringProperty(yongliang, "model");
                    if (string.IsNullOrWhiteSpace(moxing)) moxing = "未知模型";
                    var yonghushijian = PreviousUserTimestamp(zhuanxietiaomujihe, shijian);
                    var renwumiao = yonghushijian is not null && shijian > yonghushijian.Value
                        ? Math.Max(0, (shijian - yonghushijian.Value).TotalSeconds)
                        : 0;
                    var shouziziyanmiao = EstimateFirstTokenLatencyFromTurnDuration(renwumiao, shuru, shuchu);
                    var shouziziyangusuan = shouziziyanmiao > 0;
                    var sudu = CalculateAccurateTokenSpeed(shuru, shuchu, renwumiao, shouziziyanmiao);
                    var riqi = DateOnly.FromDateTime(shijian.ToLocalTime());
                    var quchongjian = "usage-cache:" + HashText(Path.GetFullPath(huancunlujing).ToLowerInvariant() + "|" + huihuawenjian.ToLowerInvariant() + "|" + suoyin.ToString(CultureInfo.InvariantCulture) + "|" + shijian.ToString("O", CultureInfo.InvariantCulture) + "|" + moxing + "|" + shuru + "|" + shuchu);
                    var zuijinjilu = new TokenRecentUsage(quchongjian, riqi.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), moxing, shuru, shuchu, renwumiao, sudu, shouziziyanmiao, shouziziyangusuan, shijian);
                    zuijin = NewerRecent(zuijin, zuijinjilu);
                    jilu++;

                    if (jinbiaojixian)
                    {
                        TokenUsageStore.MarkIngested(wenjianlujing, quchongjian, zuijinjilu);
                        biaojishuliang++;
                        continue;
                    }

                    var tianjia = TokenUsageStore.AddUsageIfNew(wenjianlujing, quchongjian, riqi, shuru, shuchu, renwumiao, moxing, sudu, shijian, shouziziyanmiao, shouziziyangusuan);
                if (tianjia.yitianjia) daorushuliang++;
                else tiaoguoshuliang++;

                if (tianjia.yitianjia && IsDeepSeekModel(moxing))
                {
                    TokenUsageStore.AddDeepSeekUsage(wenjianlujing, riqi, shuru, shuchu);
                }
                }
            }
        }
        catch
        {
            return new OpenClawUsageImportResult(daorushuliang, tiaoguoshuliang, biaojishuliang, wenjianshuliang, jilu, zuijin);
        }

        return new OpenClawUsageImportResult(daorushuliang, tiaoguoshuliang, biaojishuliang, wenjianshuliang, jilu, zuijin);
    }

    private static UsageImportEntry? TryParseSessionUsage(string wenjian, int hanghao, string hang, ref DateTime? shanggeyonghushijian)
    {
        if (string.IsNullOrWhiteSpace(hang) || !hang.Contains("\"usage\"", StringComparison.OrdinalIgnoreCase))
        {
            TryRememberUserTime(hang, ref shanggeyonghushijian);
            return null;
        }

        try
        {
            using var jsonwendang = JsonDocument.Parse(hang);
            var genyuansu = jsonwendang.RootElement;
            var xiaoxi = ObjectProperty(genyuansu, "message");
            var role = StringProperty(xiaoxi, "role");
            var shijian = TimestampProperty(genyuansu) ?? TimestampProperty(xiaoxi) ?? SafeLastWriteUtc(wenjian);
            if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase))
            {
                shanggeyonghushijian = shijian;
                return null;
            }

            if (!string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase)) return null;
            var yongliang = ObjectProperty(xiaoxi, "usage");
            if (yongliang.ValueKind != JsonValueKind.Object) return null;

            var shuru = IntProperty(yongliang, "input") ?? IntProperty(yongliang, "inputTokens") ?? IntProperty(yongliang, "promptTokens") ?? IntProperty(yongliang, "prompt_eval_count") ?? 0;
            var shuchu = IntProperty(yongliang, "output") ?? IntProperty(yongliang, "outputTokens") ?? IntProperty(yongliang, "completionTokens") ?? IntProperty(yongliang, "eval_count") ?? 0;
            if (shuru + shuchu <= 0) return null;

            var moxing = StringProperty(xiaoxi, "model");
            if (string.IsNullOrWhiteSpace(moxing)) moxing = StringProperty(genyuansu, "model");
            if (string.IsNullOrWhiteSpace(moxing)) moxing = "未知模型";

            var renwumiao = 0d;
            if (shanggeyonghushijian is not null && shijian > shanggeyonghushijian.Value)
            {
                renwumiao = Math.Max(0, (shijian - shanggeyonghushijian.Value).TotalSeconds);
            }

            var shouziziyanmiao = EstimateFirstTokenLatencyFromTurnDuration(renwumiao, shuru, shuchu);
            var shouziziyangusuan = shouziziyanmiao > 0;
            var sudu = CalculateAccurateTokenSpeed(shuru, shuchu, renwumiao, shouziziyanmiao);
            var riqi = DateOnly.FromDateTime(shijian.ToLocalTime());
            var bianhao2 = StringProperty(genyuansu, "id");
            if (string.IsNullOrWhiteSpace(bianhao2)) bianhao2 = StringProperty(xiaoxi, "id");
            var huiyingbianhao = StringProperty(xiaoxi, "responseId");
            var quchongjian = "openclaw:" + HashText(Path.GetFullPath(wenjian).ToLowerInvariant() + "|" + hanghao.ToString(CultureInfo.InvariantCulture) + "|" + bianhao2 + "|" + huiyingbianhao + "|" + shijian.ToString("O", CultureInfo.InvariantCulture) + "|" + moxing + "|" + shuru + "|" + shuchu);
            var zuijin = new TokenRecentUsage(quchongjian, riqi.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), moxing, shuru, shuchu, renwumiao, sudu, shouziziyanmiao, shouziziyangusuan, shijian);
            return new UsageImportEntry(quchongjian, riqi, moxing, shuru, shuchu, renwumiao, sudu, shouziziyanmiao, shouziziyangusuan, shijian, zuijin);
        }
        catch
        {
            return null;
        }
    }

    private static void TryRememberUserTime(string hang, ref DateTime? shanggeyonghushijian)
    {
        if (string.IsNullOrWhiteSpace(hang) || !hang.Contains("\"role\":\"user\"", StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            using var jsonwendang = JsonDocument.Parse(hang);
            var shijian = TimestampProperty(jsonwendang.RootElement);
            if (shijian is not null) shanggeyonghushijian = shijian.Value;
        }
        catch
        {
        }
    }

    private static IEnumerable<string> ReadSharedLines(string wenjian)
    {
        using var liu = new FileStream(wenjian, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var duqu = new StreamReader(liu, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        while (duqu.ReadLine() is { } hang)
        {
            yield return hang;
        }
    }

    private static JsonElement ObjectProperty(JsonElement yuansu, string mingcheng)
    {
        return yuansu.ValueKind == JsonValueKind.Object && yuansu.TryGetProperty(mingcheng, out var dangqianzhi) && dangqianzhi.ValueKind == JsonValueKind.Object
            ? dangqianzhi
            : default;
    }

    private static JsonElement ArrayProperty(JsonElement yuansu, string mingcheng)
    {
        return yuansu.ValueKind == JsonValueKind.Object && yuansu.TryGetProperty(mingcheng, out var dangqianzhi) && dangqianzhi.ValueKind == JsonValueKind.Array
            ? dangqianzhi
            : default;
    }

    private static string StringProperty(JsonElement yuansu, string mingcheng)
    {
        if (yuansu.ValueKind != JsonValueKind.Object || !yuansu.TryGetProperty(mingcheng, out var dangqianzhi)) return "";
        return dangqianzhi.ValueKind == JsonValueKind.String ? dangqianzhi.GetString() ?? "" : "";
    }

    private static int? IntProperty(JsonElement yuansu, string mingcheng)
    {
        if (yuansu.ValueKind != JsonValueKind.Object || !yuansu.TryGetProperty(mingcheng, out var dangqianzhi)) return null;
        return dangqianzhi.ValueKind == JsonValueKind.Number && dangqianzhi.TryGetInt32(out var shuliang) ? shuliang : null;
    }

    private static long? LongProperty(JsonElement yuansu, string mingcheng)
    {
        if (yuansu.ValueKind != JsonValueKind.Object || !yuansu.TryGetProperty(mingcheng, out var dangqianzhi)) return null;
        return dangqianzhi.ValueKind == JsonValueKind.Number && dangqianzhi.TryGetInt64(out var shuliang) ? shuliang : null;
    }

    private static DateTime? TimestampProperty(JsonElement yuansu)
    {
        if (yuansu.ValueKind != JsonValueKind.Object || !yuansu.TryGetProperty("timestamp", out var dangqianzhi)) return null;
        if (dangqianzhi.ValueKind == JsonValueKind.String && DateTime.TryParse(dangqianzhi.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var shijian))
        {
            return shijian;
        }

        if (dangqianzhi.ValueKind == JsonValueKind.Number && dangqianzhi.TryGetInt64(out var haomiao) && haomiao > 0)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(haomiao).UtcDateTime;
            }
            catch
            {
            }
        }

        return null;
    }

    private static DateTime SafeLastWriteUtc(string wenjian)
    {
        try
        {
            return File.GetLastWriteTimeUtc(wenjian);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private static string? FindUsageCostCachePath()
    {
        var genmulu = OpenClawRootPath();
        var huihuagenmulu = Path.Combine(genmulu, "agents");
        if (!Directory.Exists(huihuagenmulu)) return null;
        try
        {
            return Directory.EnumerateFiles(huihuagenmulu, yonglianghuancunming, SearchOption.AllDirectories)
                .OrderByDescending(SafeLastWriteUtc)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static List<(DateTime shijian, string juese)> ReadCacheTranscriptEntries(JsonElement wenjiantiaomu)
    {
        var liebiao = new List<(DateTime shijian, string juese)>();
        var tiaomujihe = ArrayProperty(wenjiantiaomu, "transcriptEntries");
        if (tiaomujihe.ValueKind != JsonValueKind.Array) return liebiao;

        foreach (var tiaomu in tiaomujihe.EnumerateArray())
        {
            if (tiaomu.ValueKind != JsonValueKind.Object) continue;
            var shijian = DateTimeFromUnixMilliseconds(LongProperty(tiaomu, "timestamp"));
            var juese = StringProperty(tiaomu, "role");
            if (shijian is null || string.IsNullOrWhiteSpace(juese)) continue;
            liebiao.Add((shijian.Value, juese));
        }

        return liebiao.OrderBy(dangqianzhi => dangqianzhi.shijian).ToList();
    }

    private static DateTime? PreviousUserTimestamp(IReadOnlyList<(DateTime shijian, string juese)> tiaomujihe, DateTime shijian)
    {
        DateTime? yonghu = null;
        foreach (var tiaomu in tiaomujihe)
        {
            if (tiaomu.shijian > shijian) break;
            if (string.Equals(tiaomu.juese, "user", StringComparison.OrdinalIgnoreCase))
            {
                yonghu = tiaomu.shijian;
            }
        }

        return yonghu;
    }

    private static DateTime? DateTimeFromUnixMilliseconds(long? haomiao)
    {
        if (haomiao is null || haomiao.Value <= 0) return null;
        try
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(haomiao.Value).UtcDateTime;
        }
        catch
        {
            return null;
        }
    }

    private static TokenRecentUsage? NewerRecent(TokenRecentUsage? dangqian, TokenRecentUsage houxuan)
    {
        if (dangqian is null || houxuan.shijian > dangqian.Value.shijian) return houxuan;
        return dangqian;
    }

    private static DateTime? RuntimeLineTime(string wenben, DateOnly riqi)
    {
        if (!wenben.StartsWith("[", StringComparison.Ordinal)) return null;
        var jieshu = wenben.IndexOf(']');
        if (jieshu <= 1) return null;
        var shijianwenben = wenben[1..jieshu];
        return TimeOnly.TryParseExact(shijianwenben, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var shijian)
            ? riqi.ToDateTime(shijian, DateTimeKind.Local)
            : null;
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

    internal static double EstimateFirstTokenLatency(double zongmiaoshu, int shuchutokens, double tokenspersecond)
    {
        if (!double.IsFinite(zongmiaoshu) || zongmiaoshu <= 0) return 0;
        if (shuchutokens <= 0 || !double.IsFinite(tokenspersecond) || tokenspersecond <= 0) return 0;
        if (tokenspersecond > 500) return 0;

        var shengchengmiaoshu = shuchutokens / tokenspersecond;
        if (shengchengmiaoshu < Math.Max(0.2d, zongmiaoshu * 0.03d)) return 0;
        var yanshi = zongmiaoshu - shengchengmiaoshu;
        return yanshi > 0.05 ? yanshi : 0;
    }

    internal static double EstimateFirstTokenLatencyFromTurnDuration(double renwumiaoshu, int shuchutokens)
    {
        return EstimateFirstTokenLatencyFromTurnDuration(renwumiaoshu, 0, shuchutokens);
    }

    internal static double EstimateFirstTokenLatencyFromTurnDuration(double renwumiaoshu, int shurutokens, int shuchutokens)
    {
        if (!double.IsFinite(renwumiaoshu) || renwumiaoshu <= 0) return 0;
        var zongtokens = Math.Max(0, shurutokens) + Math.Max(0, shuchutokens);
        if (zongtokens <= 0) return 0;

        var shurubili = Math.Clamp(Math.Max(0, shurutokens) / (double)zongtokens, 0d, 1d);
        var bili = Math.Clamp(0.08d + shurubili * 0.12d, 0.10d, 0.22d);
        var gusuan = renwumiaoshu * bili;
        var shangxian = renwumiaoshu - 0.1d;
        if (shangxian <= 0) return 0;
        return Math.Clamp(gusuan, Math.Min(0.2d, shangxian), shangxian);
    }

    internal static double CalculateAccurateTokenSpeed(int shurutokens, int shuchutokens, double zongmiaoshu, double shouziziyanmiao)
    {
        if (!double.IsFinite(zongmiaoshu) || zongmiaoshu <= 0) return 0;
        if (!double.IsFinite(shouziziyanmiao) || shouziziyanmiao <= 0) return 0;
        var youxiaomiaoshu = zongmiaoshu - shouziziyanmiao;
        if (youxiaomiaoshu <= 0.05) return 0;
        var shengchengtokens = Math.Max(0, shuchutokens);
        return shengchengtokens > 0 ? shengchengtokens / youxiaomiaoshu : 0;
    }

    internal static double CalculateOverallTokenSpeed(int shurutokens, int shuchutokens, double zongmiaoshu)
    {
        if (!double.IsFinite(zongmiaoshu) || zongmiaoshu <= 0) return 0;
        var shengchengtokens = Math.Max(0, shuchutokens);
        return shengchengtokens > 0 ? shengchengtokens / zongmiaoshu : 0;
    }

    private static string HashText(string wenben)
    {
        var zijie = SHA256.HashData(Encoding.UTF8.GetBytes(wenben));
        return Convert.ToHexString(zijie).ToLowerInvariant();
    }

    private readonly record struct UsageImportEntry(
        string laiyuanjian,
        DateOnly riqi,
        string moxingmingcheng,
        int shurutokens,
        int shuchutokens,
        double renwumiao,
        double tokenspersecond,
        double shouziziyanmiao,
        bool shouziziyangusuan,
        DateTime shijian,
        TokenRecentUsage zuijin);

    private static bool IsDeepSeekModel(string? moxing)
    {
        if (string.IsNullOrWhiteSpace(moxing)) return false;
        return moxing.StartsWith("deepseek", StringComparison.OrdinalIgnoreCase) ||
               moxing.StartsWith("ds-", StringComparison.OrdinalIgnoreCase);
    }
}
