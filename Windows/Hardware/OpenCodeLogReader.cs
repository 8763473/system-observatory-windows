using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace HwMonitor.Hardware;

public sealed class OpenCodeTokenUsage
{
    public int jinrishurutokens { get; set; }
    public int jinrishuchutokens { get; set; }
    public int jinrituilitokens { get; set; }
    public int jinrihuancun { get; set; }
    public int leijishurutokens { get; set; }
    public int leijishuchutokens { get; set; }
    public int leijituilitokens { get; set; }
    public int leijihuancun { get; set; }
    public int jinrixiaoxishu { get; set; }
    public double jinrihuancunmingzhonglv { get; set; }
    public double leijihuancunmingzhonglv { get; set; }
    public string jinrihuancunmingzhongwenben { get; set; } = "-";
    public string jinriweimingzhongwenben { get; set; } = "-";
    public bool youshuju { get; set; }
}

internal static class OpenCodeLogReader
{
    private static readonly string dbLujing = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local", "share", "opencode", "opencode.db");

    private static string? sqliteLujing;

    private static string FindSqlite()
    {
        if (sqliteLujing is not null && File.Exists(sqliteLujing)) return sqliteLujing;

        var houxuan = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", "sqlite3.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "sqlite3", "sqlite3.exe"),
        };

        foreach (var p in houxuan)
        {
            if (File.Exists(p))
            {
                sqliteLujing = p;
                return p;
            }
        }

        return "";
    }

    public static OpenCodeTokenUsage QueryDeepSeekUsage()
    {
        var jieguo = new OpenCodeTokenUsage();

        if (!File.Exists(dbLujing)) return jieguo;
        var sqlite = FindSqlite();
        if (string.IsNullOrEmpty(sqlite)) return jieguo;

        try
        {
            var jinri = QuerySql(sqlite, dbLujing,
                "SELECT " +
                "COALESCE(SUM(json_extract(data,'$.tokens.input')),0)," +
                "COALESCE(SUM(json_extract(data,'$.tokens.output')),0)," +
                "COALESCE(SUM(json_extract(data,'$.tokens.reasoning')),0)," +
                "COALESCE(SUM(json_extract(data,'$.tokens.cache.read')),0)," +
                "COUNT(*) " +
                "FROM message WHERE json_extract(data,'$.modelID') LIKE '%deepseek%' " +
                "AND date(time_created/1000,'unixepoch')=date('now');");

            if (jinri is not null)
            {
                var bufen = jinri.Split('|');
                if (bufen.Length >= 5)
                {
                    jieguo.jinrishurutokens = ParseInt(bufen[0]);
                    jieguo.jinrishuchutokens = ParseInt(bufen[1]);
                    jieguo.jinrituilitokens = ParseInt(bufen[2]);
                    jieguo.jinrihuancun = ParseInt(bufen[3]);
                    jieguo.jinrixiaoxishu = ParseInt(bufen[4]);
                    jieguo.jinrihuancunmingzhonglv = CalcHitRate(jieguo.jinrishurutokens, jieguo.jinrihuancun);
                    jieguo.jinrihuancunmingzhongwenben = FormatToken(jieguo.jinrihuancun);
                    jieguo.jinriweimingzhongwenben = FormatToken(jieguo.jinrishurutokens);
                    jieguo.youshuju = jieguo.jinrixiaoxishu > 0;
                }
            }

            var leiji = QuerySql(sqlite, dbLujing,
                "SELECT " +
                "COALESCE(SUM(json_extract(data,'$.tokens.input')),0)," +
                "COALESCE(SUM(json_extract(data,'$.tokens.output')),0)," +
                "COALESCE(SUM(json_extract(data,'$.tokens.reasoning')),0)," +
                "COALESCE(SUM(json_extract(data,'$.tokens.cache.read')),0) " +
                "FROM message WHERE json_extract(data,'$.modelID') LIKE '%deepseek%';");

            if (leiji is not null)
            {
                var bufen = leiji.Split('|');
                if (bufen.Length >= 4)
                {
                    jieguo.leijishurutokens = ParseInt(bufen[0]);
                    jieguo.leijishuchutokens = ParseInt(bufen[1]);
                    jieguo.leijituilitokens = ParseInt(bufen[2]);
                    jieguo.leijihuancunmingzhonglv = CalcHitRate(jieguo.leijishurutokens, jieguo.leijihuancun);
                }
            }
        }
        catch
        {
        }

        return jieguo;
    }

    public static List<(DateOnly riqi, int shuru, int shuchu, int tuili, int huancun)> QueryAllDeepSeekDays()
    {
        var jieguo = new List<(DateOnly, int, int, int, int)>();

        if (!File.Exists(dbLujing)) return jieguo;
        var sqlite = FindSqlite();
        if (string.IsNullOrEmpty(sqlite)) return jieguo;

        try
        {
            var shuju = QuerySql(sqlite, dbLujing,
                "SELECT date(time_created/1000,'unixepoch')," +
                "SUM(json_extract(data,'$.tokens.input'))," +
                "SUM(json_extract(data,'$.tokens.output'))," +
                "SUM(json_extract(data,'$.tokens.reasoning'))," +
                "SUM(json_extract(data,'$.tokens.cache.read')) " +
                "FROM message WHERE json_extract(data,'$.modelID') LIKE '%deepseek%' " +
                "GROUP BY date(time_created/1000,'unixepoch') ORDER BY 1;");

            if (string.IsNullOrWhiteSpace(shuju)) return jieguo;

            foreach (var hang in shuju.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var bufen = hang.Split('|');
                if (bufen.Length >= 5 &&
                    DateOnly.TryParseExact(bufen[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var riqi))
                {
                    jieguo.Add((riqi, ParseInt(bufen[1]), ParseInt(bufen[2]), ParseInt(bufen[3]), ParseInt(bufen[4])));
                }
            }
        }
        catch
        {
        }

        return jieguo;
    }

    private static double CalcHitRate(int shuru, int huancun)
    {
        var zong = shuru + huancun;
        return zong > 0 ? Math.Round(huancun * 100d / zong, 1) : 0;
    }

    private static string? QuerySql(string sqlite, string db, string sql)
    {
        try
        {
            var jincheng = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = sqlite,
                    Arguments = $"\"{db}\" \"{sql}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                },
            };
            jincheng.Start();
            var shuchu = jincheng.StandardOutput.ReadToEnd();
            jincheng.WaitForExit(5000);
            if (jincheng.HasExited && jincheng.ExitCode == 0)
            {
                return shuchu.Trim();
            }
        }
        catch
        {
        }
        return null;
    }

    private static int ParseInt(string wenben)
    {
        return int.TryParse(wenben.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var zhi) ? zhi : 0;
    }

    public static string FormatToken(int zhi)
    {
        if (zhi <= 0) return "0";
        return zhi.ToString("N0", CultureInfo.InvariantCulture);
    }
}
