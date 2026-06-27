using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace HwMonitor.Hardware;

public sealed class DeepSeekBalanceInfo
{
    public string zhuangtai { get; set; } = "未查询";
    public bool chenggong { get; set; }
    public string yuenwenben { get; set; } = "-";
    public string totalTokensText { get; set; } = "-";
    public string huancunmingzhonglvText { get; set; } = "-";
    public string huancunmingzhongwenben { get; set; } = "-";
    public string weimingzhongwenben { get; set; } = "-";
    public string shiyongsheshu { get; set; } = "";
    public string cuowu { get; set; } = "";
}

internal static class DeepSeekBalanceChecker
{
    private static readonly HttpClient _kehu = new() { Timeout = TimeSpan.FromSeconds(8) };

    private static readonly string[] yuEndpoints =
    [
        "https://api.deepseek.com/user/balance",
    ];

    public static async Task<DeepSeekBalanceInfo> QueryAsync(string apiKey, CancellationToken quxiao)
    {
        var jieguo = new DeepSeekBalanceInfo();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            jieguo.zhuangtai = "未配置密钥";
            jieguo.yuenwenben = "请在设置中填写";
            return jieguo;
        }

        jieguo.zhuangtai = "查询中";

        foreach (var endpoint in yuEndpoints)
        {
            try
            {
                using var qingqiu = new HttpRequestMessage(HttpMethod.Get, endpoint);
                qingqiu.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey.Trim()}");
                qingqiu.Headers.TryAddWithoutValidation("Accept", "application/json");

                using var xiangying = await _kehu.SendAsync(qingqiu, quxiao);
                var neirong = await xiangying.Content.ReadAsStringAsync(quxiao);

                if (xiangying.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(neirong))
                {
                    ParseBalance(neirong, jieguo);
                    jieguo.zhuangtai = "查询成功";
                    jieguo.chenggong = true;
                    return jieguo;
                }

                // Try parsing even non-200 responses as some APIs return data with error codes
                if (neirong.Contains("balance", StringComparison.OrdinalIgnoreCase) ||
                    neirong.Contains("total", StringComparison.OrdinalIgnoreCase))
                {
                    ParseBalance(neirong, jieguo);
                    if (jieguo.chenggong) return jieguo;
                }
            }
            catch (OperationCanceledException)
            {
                jieguo.zhuangtai = "请求超时";
                jieguo.cuowu = "网络超时";
            }
            catch (HttpRequestException cuowu)
            {
                jieguo.zhuangtai = "网络错误";
                jieguo.cuowu = cuowu.Message;
            }
            catch
            {
                jieguo.zhuangtai = "查询失败";
            }
        }

        return jieguo;
    }

    private static void ParseBalance(string json, DeepSeekBalanceInfo jieguo)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("balance_infos", out var balances) && balances.GetArrayLength() > 0)
            {
                double total = 0;
                foreach (var item in balances.EnumerateArray())
                {
                    if (item.TryGetProperty("total_balance", out var tb) && tb.ValueKind == JsonValueKind.String)
                    {
                        if (double.TryParse(tb.GetString(), out var val))
                            total += val;
                    }
                    else if (item.TryGetProperty("total_balance", out var tb2))
                    {
                        total += tb2.GetDouble();
                    }
                    if (item.TryGetProperty("currency", out var cur))
                        jieguo.shiyongsheshu = cur.GetString() ?? "";
                }
                jieguo.chenggong = true;
                jieguo.yuenwenben = $"¥ {total:F2}";
            }
            else if (root.TryGetProperty("balance", out var b))
            {
                jieguo.chenggong = true;
                if (b.ValueKind == JsonValueKind.String && double.TryParse(b.GetString(), out var val))
                    jieguo.yuenwenben = $"¥ {val:F2}";
                else
                    jieguo.yuenwenben = $"¥ {b.GetDouble():F2}";
            }

            if (root.TryGetProperty("total_tokens_used", out var tokens))
            {
                var t = tokens.ValueKind == JsonValueKind.String && long.TryParse(tokens.GetString(), out var tv) ? tv : tokens.GetInt64();
                jieguo.totalTokensText = t switch
                {
                    >= 100_000_000 => $"{t / 100_000_000d:F1}亿",
                    >= 10_000 => $"{t / 10_000d:F1}万",
                    _ => t.ToString("N0")
                };
            }
            else
            {
                jieguo.totalTokensText = "-";
            }
        }
        catch
        {
        }
    }
}
