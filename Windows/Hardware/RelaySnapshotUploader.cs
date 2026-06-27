using System.Net.Http;
using System.Text;
using System.Text.Json;
using HwMonitor.Models;
using HwMonitor.Settings;

namespace HwMonitor.Hardware;

public enum RelayUploadState
{
    Disabled,
    MissingConfig,
    Uploading,
    Online,
    Failed
}

public sealed class RelaySnapshotUploader : IDisposable
{
    private static readonly JsonSerializerOptions xuanxiang = new()
    {
        WriteIndented = false
    };

    private readonly HttpClient _kehu = new()
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    private int _shangchuanzhong;

    public RelayUploadState zhuangtai { get; private set; } = RelayUploadState.Disabled;
    public string zhuangtaiwenben { get; private set; } = "未开启";
    public DateTime? shangcichenggong { get; private set; }

    public async Task UploadAsync(SystemSnapshot kuaizhao, AppSettings shezhi, CancellationToken quxiao)
    {
        if (!shezhi.relayEnabled)
        {
            zhuangtai = RelayUploadState.Disabled;
            zhuangtaiwenben = "未开启";
            return;
        }

        if (string.IsNullOrWhiteSpace(shezhi.relayUrl) || string.IsNullOrWhiteSpace(shezhi.relayDeviceKey))
        {
            zhuangtai = RelayUploadState.MissingConfig;
            zhuangtaiwenben = "缺少地址或密钥";
            return;
        }

        if (Interlocked.Exchange(ref _shangchuanzhong, 1) == 1) return;

        zhuangtai = RelayUploadState.Uploading;
        zhuangtaiwenben = "同步中";

        try
        {
            var lujing = BuildSnapshotUrl(shezhi);
            kuaizhao.tokenjiankong = BuildTokenInfo();
            var json = JsonSerializer.Serialize(kuaizhao, xuanxiang);
            using var neirong = new StringContent(json, Encoding.UTF8, "application/json");
            using var qingqiu = new HttpRequestMessage(HttpMethod.Post, lujing)
            {
                Content = neirong
            };
            qingqiu.Headers.TryAddWithoutValidation("X-Device-Key", shezhi.relayDeviceKey.Trim());

            using var xiangying = await _kehu.SendAsync(qingqiu, quxiao);
            if (xiangying.IsSuccessStatusCode)
            {
                zhuangtai = RelayUploadState.Online;
                zhuangtaiwenben = "已同步";
                shangcichenggong = DateTime.Now;
                return;
            }

            zhuangtai = RelayUploadState.Failed;
            zhuangtaiwenben = "中转返回 " + (int)xiangying.StatusCode;
        }
        catch (OperationCanceledException) when (quxiao.IsCancellationRequested)
        {
        }
        catch
        {
            zhuangtai = RelayUploadState.Failed;
            zhuangtaiwenben = "同步失败";
        }
        finally
        {
            Interlocked.Exchange(ref _shangchuanzhong, 0);
        }
    }

    private static string BuildSnapshotUrl(AppSettings shezhi)
    {
        var baseUrl = (shezhi.relayUrl ?? "").Trim().TrimEnd('/');
        var appId = Uri.EscapeDataString(CleanId(shezhi.relayAppId, "system-observatory"));
        var deviceId = Uri.EscapeDataString(CleanId(shezhi.relayDeviceId, Environment.MachineName));
        return $"{baseUrl}/api/apps/{appId}/devices/{deviceId}/snapshot";
    }

    public static TokenMonitorInfo BuildTokenInfoPublic()
    {
        return BuildTokenInfo();
    }

    private static TokenMonitorInfo BuildTokenInfo()
    {
        var token = OpenClawTokenMonitorLauncher.Snapshot();
        var xinxi = new TokenMonitorInfo
        {
            peizhiwanzheng = token.peizhiwanzheng,
            yunxing = token.yunxing,
            houtaicaijiqiyunxing = token.houtaicaijiqiyunxing,
            zhuangtai = token.zhuangtai,
            tongjiriqi = token.tongjiriqi,
            moxingmingcheng = string.IsNullOrWhiteSpace(token.moxingmingcheng) ? "未知模型" : token.moxingmingcheng,
            shurutokens = Math.Max(0, token.shurutokens),
            shuchutokens = Math.Max(0, token.shuchutokens),
            jinrishurutokens = Math.Max(0, token.jinrishurutokens),
            jinrishuchutokens = Math.Max(0, token.jinrishuchutokens),
            jinritokens = Math.Max(0, token.jinritokens),
            leijitokens = Math.Max(0, token.tongjizonglan.leijitokens),
            fengzhitokens = Math.Max(0, token.tongjizonglan.fengzhitokens),
            dangqianlianxutianshu = Math.Max(0, token.tongjizonglan.dangqianlianxutianshu),
            zuichanglianxutianshu = Math.Max(0, token.tongjizonglan.zuichanglianxutianshu),
            zhunquesudu = SafeNumber(token.zhunquesudu),
            shishitokensudu = SafeNumber(token.shishitokensudu),
            zonghaoshi = SafeNumber(token.zonghaoshi),
            shouziziyanmiao = SafeNumber(token.shouziziyanmiao),
            shouziziyangusuan = token.shouziziyangusuan,
            gengxinshijian = token.gengxinshijian
        };

        foreach (var dangri in token.huodongriqi.TakeLast(371))
        {
            xinxi.huodongriqi.Add(new TokenDailyInfo
            {
                riqi = dangri.riqi,
                shurutokens = Math.Max(0, dangri.shurutokens),
                shuchutokens = Math.Max(0, dangri.shuchutokens),
                zongtokens = Math.Max(0, dangri.zongtokens),
                zuichangrenwumiao = SafeNumber(dangri.zuichangrenwumiao)
            });
        }

        return xinxi;
    }

    private static string CleanId(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static double SafeNumber(double value)
    {
        return double.IsFinite(value) ? Math.Max(0, value) : 0;
    }

    public void Dispose()
    {
        _kehu.Dispose();
    }
}
