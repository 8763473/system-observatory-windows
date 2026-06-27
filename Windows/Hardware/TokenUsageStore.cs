using System.Text.Json;
using System.Text.Json.Serialization;

namespace HwMonitor.Hardware;

internal readonly record struct TokenDailyUsage(
    string riqi,
    int shurutokens,
    int shuchutokens,
    double zuichangrenwumiao,
    int deepseekshurutokens = 0,
    int deepseekshuchutokens = 0)
{
    public int zongtokens => Math.Max(0, shurutokens) + Math.Max(0, shuchutokens);
    public int deepseekzongtokens => Math.Max(0, deepseekshurutokens) + Math.Max(0, deepseekshuchutokens);
}

internal readonly record struct TokenHardwarePeak(
    double cpuzuigaobaifenbi,
    double gpuzuigaobaifenbi,
    double xiancunzuigaobaifenbi,
    ulong xiancunyiyongfengzhi,
    ulong xiancunzongliang,
    double neicunzuigaobaifenbi,
    ulong neicunyiyongfengzhi,
    ulong neicunzongliang)
{
    public static TokenHardwarePeak Empty { get; } = new(-1, -1, -1, 0, 0, -1, 0, 0);
    public bool youshuju =>
        cpuzuigaobaifenbi > 0 ||
        gpuzuigaobaifenbi > 0 ||
        xiancunzuigaobaifenbi > 0 ||
        xiancunyiyongfengzhi > 0 ||
        neicunzuigaobaifenbi > 0 ||
        neicunyiyongfengzhi > 0;
}

internal readonly record struct TokenRecentUsage(
    string laiyuanjian,
    string riqi,
    string moxingmingcheng,
    int shurutokens,
    int shuchutokens,
    double renwumiao,
    double tokenspersecond,
    double shouziziyanmiao,
    bool shouziziyangusuan,
    DateTime shijian)
{
    public TokenHardwarePeak yingjianfengzhi { get; init; } = TokenHardwarePeak.Empty;
    public int zongtokens => Math.Max(0, shurutokens) + Math.Max(0, shuchutokens);
}

internal readonly record struct TokenUsageSummary(
    int leijitokens,
    int fengzhitokens,
    double zuichangrenwumiao,
    int dangqianlianxutianshu,
    int zuichanglianxutianshu);

internal static class TokenUsageStore
{
    private const string wenjianming = "token_usage_daily.json";
    private const string suoming = @"Local\SystemObservatoryTokenUsageStore";
    private static readonly JsonSerializerOptions xuanxiang = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string DefaultPath()
    {
        return Path.Combine(AppContext.BaseDirectory, wenjianming);
    }

    public static TokenDailyUsage LoadToday(string? wenjianlujing = null)
    {
        return LoadDay(wenjianlujing ?? DefaultPath(), DateOnly.FromDateTime(DateTime.Now));
    }

    public static TokenDailyUsage LoadDay(string wenjianlujing, DateOnly riqi)
    {
        var shuju = LoadFile(wenjianlujing);
        return LoadDayUnlocked(shuju, riqi);
    }

    public static TokenDailyUsage AddUsage(int shurutokens, int shuchutokens, string? wenjianlujing = null)
    {
        return AddUsage(wenjianlujing ?? DefaultPath(), DateOnly.FromDateTime(DateTime.Now), shurutokens, shuchutokens, 0);
    }

    public static TokenDailyUsage AddUsage(string wenjianlujing, DateOnly riqi, int shurutokens, int shuchutokens)
    {
        return AddUsage(wenjianlujing, riqi, shurutokens, shuchutokens, 0);
    }

    public static TokenDailyUsage AddUsage(int shurutokens, int shuchutokens, double renwumiaoshu, string? wenjianlujing = null)
    {
        return AddUsage(wenjianlujing ?? DefaultPath(), DateOnly.FromDateTime(DateTime.Now), shurutokens, shuchutokens, renwumiaoshu);
    }

    public static TokenDailyUsage AddUsage(string wenjianlujing, DateOnly riqi, int shurutokens, int shuchutokens, double renwumiaoshu)
    {
        return WithStoreLock(() =>
        {
            var shuju = LoadFile(wenjianlujing);
            var dangri = AddUsageToFile(shuju, riqi, shurutokens, shuchutokens, renwumiaoshu);
            SaveAtomic(wenjianlujing, shuju);
            return dangri;
        });
    }

    public static (bool yitianjia, TokenDailyUsage yongliang) AddUsageIfNew(
        string wenjianlujing,
        string laiyuanjian,
        DateOnly riqi,
        int shurutokens,
        int shuchutokens,
        double renwumiaoshu,
        string? moxingmingcheng,
        double tokenspersecond,
        DateTime shijian,
        double shouziziyanmiao = 0,
        bool shouziziyangusuan = false,
        TokenHardwarePeak? yingjianfengzhi = null)
    {
        if (string.IsNullOrWhiteSpace(laiyuanjian))
        {
            return (true, AddUsage(wenjianlujing, riqi, shurutokens, shuchutokens, renwumiaoshu));
        }

        return WithStoreLock(() =>
        {
            var shuju = LoadFile(wenjianlujing);
            if (shuju.yidaorujian.Contains(laiyuanjian))
            {
                return (false, LoadDayUnlocked(shuju, riqi));
            }

            var dangri = AddUsageToFile(shuju, riqi, shurutokens, shuchutokens, renwumiaoshu);
            shuju.yidaorujian.Add(laiyuanjian);
            shuju.zuijin = ToRecentDto(new TokenRecentUsage(
                laiyuanjian,
                DateKey(riqi),
                string.IsNullOrWhiteSpace(moxingmingcheng) ? "未知模型" : moxingmingcheng.Trim(),
                Math.Max(0, shurutokens),
                Math.Max(0, shuchutokens),
                SafeSeconds(renwumiaoshu),
                double.IsFinite(tokenspersecond) ? Math.Max(0, tokenspersecond) : 0,
                SafeSeconds(shouziziyanmiao),
                shouziziyangusuan && SafeSeconds(shouziziyanmiao) > 0,
                NormalizeUtc(shijian))
            {
                yingjianfengzhi = NormalizePeak(yingjianfengzhi)
            });
            SaveAtomic(wenjianlujing, shuju);
            return (true, dangri);
        });
    }

    public static void AddDeepSeekUsage(string wenjianlujing, DateOnly riqi, int shurutokens, int shuchutokens)
    {
        WithStoreLock(() =>
        {
            var shuju = LoadFile(wenjianlujing);
            var riqijian = DateKey(riqi);
            if (!shuju.riqijilu.TryGetValue(riqijian, out var dangri))
            {
                dangri = new TokenDailyUsageDto();
            }
            dangri.deepseekshurutokens = Math.Max(0, dangri.deepseekshurutokens) + Math.Max(0, shurutokens);
            dangri.deepseekshuchutokens = Math.Max(0, dangri.deepseekshuchutokens) + Math.Max(0, shuchutokens);
            shuju.riqijilu[riqijian] = dangri;
            SaveAtomic(wenjianlujing, shuju);
            return 0;
        });
    }

    public static void MarkIngested(string wenjianlujing, string laiyuanjian, TokenRecentUsage? zuijin = null)
    {
        if (string.IsNullOrWhiteSpace(laiyuanjian)) return;
        WithStoreLock(() =>
        {
            var shuju = LoadFile(wenjianlujing);
            shuju.yidaorujian.Add(laiyuanjian);
            if (zuijin is not null)
            {
                shuju.zuijin = ToRecentDto(zuijin.Value);
            }

            SaveAtomic(wenjianlujing, shuju);
            return 0;
        });
    }

    public static bool HasIngestedKeys(string? wenjianlujing = null)
    {
        return LoadFile(wenjianlujing ?? DefaultPath()).yidaorujian.Count > 0;
    }

    public static int IngestedKeyCount(string? wenjianlujing = null)
    {
        return LoadFile(wenjianlujing ?? DefaultPath()).yidaorujian.Count;
    }

    public static TokenRecentUsage? LoadRecent(string? wenjianlujing = null)
    {
        var shuju = LoadFile(wenjianlujing ?? DefaultPath());
        return shuju.zuijin is null ? null : FromRecentDto(shuju.zuijin);
    }

    public static IReadOnlyList<TokenDailyUsage> LoadActivity(string? wenjianlujing = null, int tianshu = 371)
    {
        return LoadActivity(wenjianlujing ?? DefaultPath(), DateOnly.FromDateTime(DateTime.Now), tianshu);
    }

    public static IReadOnlyList<TokenDailyUsage> LoadActivity(string wenjianlujing, DateOnly jieshuriqi, int tianshu)
    {
        tianshu = Math.Clamp(tianshu, 1, 900);
        var kaishi = jieshuriqi.AddDays(-(tianshu - 1));
        var shuju = LoadFile(wenjianlujing);
        var liebiao = new List<TokenDailyUsage>(tianshu);
        for (var suoyin = 0; suoyin < tianshu; suoyin++)
        {
            var riqi = kaishi.AddDays(suoyin);
            liebiao.Add(LoadDayUnlocked(shuju, riqi));
        }

        return liebiao;
    }

    public static TokenUsageSummary LoadSummary(string? wenjianlujing = null)
    {
        return LoadSummary(wenjianlujing ?? DefaultPath(), DateOnly.FromDateTime(DateTime.Now));
    }

    public static TokenUsageSummary LoadSummary(string wenjianlujing, DateOnly jinri)
    {
        var shuju = LoadFile(wenjianlujing);
        var youshuju = shuju.riqijilu
            .Select(dangqianzhi => (riqi: ParseDate(dangqianzhi.Key), yongliang: FromDailyDto(dangqianzhi.Key, dangqianzhi.Value)))
            .Where(dangqianzhi => dangqianzhi.riqi is not null && dangqianzhi.yongliang.zongtokens > 0)
            .Select(dangqianzhi => (riqi: dangqianzhi.riqi!.Value, dangqianzhi.yongliang))
            .OrderBy(dangqianzhi => dangqianzhi.riqi)
            .ToArray();
        if (youshuju.Length == 0)
        {
            return new TokenUsageSummary(0, 0, 0, 0, 0);
        }

        var leiji = youshuju.Sum(dangqianzhi => dangqianzhi.yongliang.zongtokens);
        var fengzhi = youshuju.Max(dangqianzhi => dangqianzhi.yongliang.zongtokens);
        var zuichangrenwu = youshuju.Max(dangqianzhi => dangqianzhi.yongliang.zuichangrenwumiao);
        var zuichanglianxu = 0;
        var dangqianlianxu = 0;
        DateOnly? shangyitian = null;
        foreach (var dangqianzhi in youshuju)
        {
            dangqianlianxu = shangyitian is not null && dangqianzhi.riqi.DayNumber == shangyitian.Value.DayNumber + 1
                ? dangqianlianxu + 1
                : 1;
            zuichanglianxu = Math.Max(zuichanglianxu, dangqianlianxu);
            shangyitian = dangqianzhi.riqi;
        }

        var jinrilianxu = 0;
        var riqijihe = youshuju.Select(dangqianzhi => dangqianzhi.riqi).ToHashSet();
        for (var riqi = jinri; riqijihe.Contains(riqi); riqi = riqi.AddDays(-1))
        {
            jinrilianxu++;
        }

        return new TokenUsageSummary(leiji, fengzhi, zuichangrenwu, jinrilianxu, zuichanglianxu);
    }

    internal static void SaveAtomic(string wenjianlujing, TokenUsageFile shuju)
    {
        var mulu = Path.GetDirectoryName(wenjianlujing);
        if (!string.IsNullOrWhiteSpace(mulu))
        {
            Directory.CreateDirectory(mulu);
        }

        var linshi = wenjianlujing + ".tmp";
        var beifen = wenjianlujing + ".bak";
        var jsonwendang = JsonSerializer.Serialize(shuju, xuanxiang);
        File.WriteAllText(linshi, jsonwendang);
        if (File.Exists(wenjianlujing))
        {
            try
            {
                File.Replace(linshi, wenjianlujing, beifen, ignoreMetadataErrors: true);
                return;
            }
            catch
            {
            }
        }

        File.Move(linshi, wenjianlujing, overwrite: true);
    }

    private static TokenDailyUsage AddUsageToFile(TokenUsageFile shuju, DateOnly riqi, int shurutokens, int shuchutokens, double renwumiaoshu)
    {
        var riqijian = DateKey(riqi);
        if (!shuju.riqijilu.TryGetValue(riqijian, out var dangri))
        {
            dangri = new TokenDailyUsageDto();
        }

        var renwushichang = SafeSeconds(renwumiaoshu);
        dangri.shurutokens = Math.Max(0, dangri.shurutokens) + Math.Max(0, shurutokens);
        dangri.shuchutokens = Math.Max(0, dangri.shuchutokens) + Math.Max(0, shuchutokens);
        dangri.zuichangrenwumiao = Math.Max(double.IsFinite(dangri.zuichangrenwumiao) ? Math.Max(0, dangri.zuichangrenwumiao) : 0, renwushichang);
        shuju.riqijilu[riqijian] = dangri;
        return FromDailyDto(riqijian, dangri);
    }

    private static TokenDailyUsage LoadDayUnlocked(TokenUsageFile shuju, DateOnly riqi)
    {
        var riqijian = DateKey(riqi);
        return shuju.riqijilu.TryGetValue(riqijian, out var dangri)
            ? FromDailyDto(riqijian, dangri)
            : new TokenDailyUsage(riqijian, 0, 0, 0);
    }

    private static TokenUsageFile LoadFile(string wenjianlujing)
    {
        if (!File.Exists(wenjianlujing))
        {
            return new TokenUsageFile();
        }

        try
        {
            var shuju = JsonSerializer.Deserialize<TokenUsageFile>(File.ReadAllText(wenjianlujing), xuanxiang);
            return shuju ?? new TokenUsageFile();
        }
        catch
        {
            return new TokenUsageFile();
        }
    }

    private static string DateKey(DateOnly riqi)
    {
        return riqi.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParseDate(string riqi)
    {
        return DateOnly.TryParseExact(riqi, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var jieguo)
            ? jieguo
            : null;
    }

    private static TokenDailyUsage FromDailyDto(string riqi, TokenDailyUsageDto dangri)
    {
        return new TokenDailyUsage(
            riqi,
            Math.Max(0, dangri.shurutokens),
            Math.Max(0, dangri.shuchutokens),
            double.IsFinite(dangri.zuichangrenwumiao) ? Math.Max(0, dangri.zuichangrenwumiao) : 0,
            Math.Max(0, dangri.deepseekshurutokens),
            Math.Max(0, dangri.deepseekshuchutokens));
    }

    private static TokenRecentUsageDto ToRecentDto(TokenRecentUsage zuijin)
    {
        var fengzhi = NormalizePeak(zuijin.yingjianfengzhi);
        var shuru = Math.Max(0, zuijin.shurutokens);
        var shuchu = Math.Max(0, zuijin.shuchutokens);
        var renwumiao = SafeSeconds(zuijin.renwumiao);
        var shouzimiao = SafeSeconds(zuijin.shouziziyanmiao);
        return new TokenRecentUsageDto
        {
            laiyuanjian = zuijin.laiyuanjian,
            riqi = zuijin.riqi,
            moxingmingcheng = string.IsNullOrWhiteSpace(zuijin.moxingmingcheng) ? "未知模型" : zuijin.moxingmingcheng,
            shurutokens = shuru,
            shuchutokens = shuchu,
            renwumiao = renwumiao,
            tokenspersecond = NormalizeStoredTokenSpeed(shuru, shuchu, renwumiao, shouzimiao, zuijin.tokenspersecond),
            shouziziyanmiao = shouzimiao,
            shouziziyangusuan = zuijin.shouziziyangusuan && shouzimiao > 0,
            cpuzuigaobaifenbi = fengzhi.cpuzuigaobaifenbi > 0 ? fengzhi.cpuzuigaobaifenbi : null,
            gpuzuigaobaifenbi = fengzhi.gpuzuigaobaifenbi > 0 ? fengzhi.gpuzuigaobaifenbi : null,
            xiancunzuigaobaifenbi = fengzhi.xiancunzuigaobaifenbi > 0 ? fengzhi.xiancunzuigaobaifenbi : null,
            xiancunyiyongqianzijie = fengzhi.xiancunyiyongfengzhi > 0 ? fengzhi.xiancunyiyongfengzhi : null,
            xiancunzongqianzijie = fengzhi.xiancunzongliang > 0 ? fengzhi.xiancunzongliang : null,
            neicunzuigaobaifenbi = fengzhi.neicunzuigaobaifenbi > 0 ? fengzhi.neicunzuigaobaifenbi : null,
            neicunyiyongqianzijie = fengzhi.neicunyiyongfengzhi > 0 ? fengzhi.neicunyiyongfengzhi : null,
            neicunzongqianzijie = fengzhi.neicunzongliang > 0 ? fengzhi.neicunzongliang : null,
            shijianutc = NormalizeUtc(zuijin.shijian).ToString("O", System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static TokenRecentUsage FromRecentDto(TokenRecentUsageDto zuijin)
    {
        var shijian = DateTime.TryParse(zuijin.shijianutc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var jieguo)
            ? jieguo
            : DateTime.MinValue;
        var shuru = Math.Max(0, zuijin.shurutokens);
        var shuchu = Math.Max(0, zuijin.shuchutokens);
        var renwumiao = SafeSeconds(zuijin.renwumiao);
        var shouzimiao = SafeSeconds(zuijin.shouziziyanmiao);
        return new TokenRecentUsage(
            zuijin.laiyuanjian ?? "",
            zuijin.riqi ?? "",
            string.IsNullOrWhiteSpace(zuijin.moxingmingcheng) ? "未知模型" : zuijin.moxingmingcheng,
            shuru,
            shuchu,
            renwumiao,
            NormalizeStoredTokenSpeed(shuru, shuchu, renwumiao, shouzimiao, zuijin.tokenspersecond),
            shouzimiao,
            zuijin.shouziziyangusuan && shouzimiao > 0,
            shijian)
        {
            yingjianfengzhi = NormalizePeak(new TokenHardwarePeak(
                zuijin.cpuzuigaobaifenbi ?? -1,
                zuijin.gpuzuigaobaifenbi ?? -1,
                zuijin.xiancunzuigaobaifenbi ?? -1,
                zuijin.xiancunyiyongqianzijie ?? 0,
                zuijin.xiancunzongqianzijie ?? 0,
                zuijin.neicunzuigaobaifenbi ?? -1,
                zuijin.neicunyiyongqianzijie ?? 0,
                zuijin.neicunzongqianzijie ?? 0))
        };
    }

    private static double NormalizeStoredTokenSpeed(int shurutokens, int shuchutokens, double renwumiao, double shouzimiao, double sudu)
    {
        _ = shurutokens;
        var shuchu = Math.Max(0, shuchutokens);
        if (shuchu <= 0) return 0;

        var renwu = SafeSeconds(renwumiao);
        var shouzi = SafeSeconds(shouzimiao);
        if (renwu > 0)
        {
            var youxiaomiao = shouzi > 0 ? renwu - shouzi : renwu;
            if (youxiaomiao > 0.05)
            {
                return shuchu / youxiaomiao;
            }
        }

        return double.IsFinite(sudu) && sudu > 0 && sudu <= 500 ? sudu : 0;
    }

    private static TokenHardwarePeak NormalizePeak(TokenHardwarePeak? fengzhi)
    {
        if (fengzhi is null) return TokenHardwarePeak.Empty;

        var dangqianzhi = fengzhi.Value;
        var xiancunzongliang = dangqianzhi.xiancunzongliang;
        var xiancunyiyong = xiancunzongliang > 0 ? Math.Min(dangqianzhi.xiancunyiyongfengzhi, xiancunzongliang) : dangqianzhi.xiancunyiyongfengzhi;
        var xiancunbaifenbi = SafePercent(dangqianzhi.xiancunzuigaobaifenbi);
        if (xiancunbaifenbi < 0 && xiancunzongliang > 0)
        {
            xiancunbaifenbi = Math.Clamp(xiancunyiyong * 100d / xiancunzongliang, 0d, 100d);
        }

        var neicunzongliang = dangqianzhi.neicunzongliang;
        var neicunyiyong = neicunzongliang > 0 ? Math.Min(dangqianzhi.neicunyiyongfengzhi, neicunzongliang) : dangqianzhi.neicunyiyongfengzhi;
        var neicunbaifenbi = SafePercent(dangqianzhi.neicunzuigaobaifenbi);
        if (neicunbaifenbi < 0 && neicunzongliang > 0)
        {
            neicunbaifenbi = Math.Clamp(neicunyiyong * 100d / neicunzongliang, 0d, 100d);
        }

        return new TokenHardwarePeak(
            SafePercent(dangqianzhi.cpuzuigaobaifenbi),
            SafePercent(dangqianzhi.gpuzuigaobaifenbi),
            xiancunbaifenbi,
            xiancunyiyong,
            xiancunzongliang,
            neicunbaifenbi,
            neicunyiyong,
            neicunzongliang);
    }

    private static double SafePercent(double baifenbi)
    {
        return double.IsFinite(baifenbi) && baifenbi > 0 ? Math.Clamp(baifenbi, 0d, 100d) : -1;
    }

    private static DateTime NormalizeUtc(DateTime shijian)
    {
        return shijian.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(shijian, DateTimeKind.Local).ToUniversalTime()
            : shijian.ToUniversalTime();
    }

    private static double SafeSeconds(double miaoshu)
    {
        return double.IsFinite(miaoshu) ? Math.Max(0, miaoshu) : 0;
    }

    private static T WithStoreLock<T>(Func<T> caozuo)
    {
        Mutex? suoti = null;
        var yijinghuode = false;
        try
        {
            suoti = new Mutex(false, suoming);
            yijinghuode = suoti.WaitOne(TimeSpan.FromSeconds(5));
        }
        catch
        {
        }

        try
        {
            return caozuo();
        }
        finally
        {
            if (yijinghuode && suoti is not null)
            {
                try
                {
                    suoti.ReleaseMutex();
                }
                catch
                {
                }
            }

            suoti?.Dispose();
        }
    }

    internal sealed class TokenUsageFile
    {
        [JsonPropertyName("days")]
        public Dictionary<string, TokenDailyUsageDto> riqijilu { get; set; } = new(StringComparer.Ordinal);
        [JsonPropertyName("ingestedKeys")]
        public HashSet<string> yidaorujian { get; set; } = new(StringComparer.Ordinal);
        [JsonPropertyName("recent")]
        public TokenRecentUsageDto? zuijin { get; set; }
    }

    internal sealed class TokenDailyUsageDto
    {
        [JsonPropertyName("inputTokens")]
        public int shurutokens { get; set; }
        [JsonPropertyName("outputTokens")]
        public int shuchutokens { get; set; }
        [JsonPropertyName("maxDurationSeconds")]
        public double zuichangrenwumiao { get; set; }
        [JsonPropertyName("deepseekInputTokens")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int deepseekshurutokens { get; set; }
        [JsonPropertyName("deepseekOutputTokens")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int deepseekshuchutokens { get; set; }
    }

    internal sealed class TokenRecentUsageDto
    {
        [JsonPropertyName("key")]
        public string? laiyuanjian { get; set; }
        [JsonPropertyName("date")]
        public string? riqi { get; set; }
        [JsonPropertyName("model")]
        public string? moxingmingcheng { get; set; }
        [JsonPropertyName("inputTokens")]
        public int shurutokens { get; set; }
        [JsonPropertyName("outputTokens")]
        public int shuchutokens { get; set; }
        [JsonPropertyName("durationSeconds")]
        public double renwumiao { get; set; }
        [JsonPropertyName("tokensPerSecond")]
        public double tokenspersecond { get; set; }
        [JsonPropertyName("firstTokenLatencySeconds")]
        public double shouziziyanmiao { get; set; }
        [JsonPropertyName("firstTokenEstimated")]
        public bool shouziziyangusuan { get; set; }
        [JsonPropertyName("cpuPeakPercent")]
        public double? cpuzuigaobaifenbi { get; set; }
        [JsonPropertyName("gpuPeakPercent")]
        public double? gpuzuigaobaifenbi { get; set; }
        [JsonPropertyName("vramPeakPercent")]
        public double? xiancunzuigaobaifenbi { get; set; }
        [JsonPropertyName("vramPeakUsedKb")]
        public ulong? xiancunyiyongqianzijie { get; set; }
        [JsonPropertyName("vramPeakTotalKb")]
        public ulong? xiancunzongqianzijie { get; set; }
        [JsonPropertyName("memoryPeakPercent")]
        public double? neicunzuigaobaifenbi { get; set; }
        [JsonPropertyName("memoryPeakUsedKb")]
        public ulong? neicunyiyongqianzijie { get; set; }
        [JsonPropertyName("memoryPeakTotalKb")]
        public ulong? neicunzongqianzijie { get; set; }
        [JsonPropertyName("timestampUtc")]
        public string? shijianutc { get; set; }
    }
}
