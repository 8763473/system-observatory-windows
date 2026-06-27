using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HwMonitor.Models;

namespace HwMonitor.Hardware;

public enum EmbeddedServerState
{
    Stopped,
    Running,
    Failed
}

public sealed class EmbeddedServer : IDisposable
{
    private static readonly JsonSerializerOptions xuanxiang = new() { WriteIndented = false };
    private const string wsMagic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    private readonly object _suozhi = new();
    private readonly HashSet<WebSocket> _kehu = new();
    private string _zuijinjson = "{}";
    private SystemSnapshot? _zuijinkuaizhao;
    private DateTime _zuijinshijian = DateTime.MinValue;

    private TcpListener? _jiantingqi;
    private CancellationTokenSource? _quxiao;
    private Task? _jiantingrenwu;
    private string? _miyao;
    private Process? _suidaojincheng;
    private string _suidaoURL = "";

    public EmbeddedServerState zhuangtai { get; private set; } = EmbeddedServerState.Stopped;
    public string zhuangtaiwenben { get; private set; } = "未启动";
    public int duankou { get; private set; } = 8787;
    public string miyao => _miyao ?? "";
    public string? gongwangdizhi { get; set; }
    public string suidaoURL => _suidaoURL;

    public int lianjieshu { get { lock (_suozhi) return _kehu.Count; } }
    public string bendidizhi => $"http://127.0.0.1:{duankou}";

    public string androidDizhi
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_suidaoURL)) return _suidaoURL;
            var baseZhi = gongwangdizhi;
            if (!string.IsNullOrWhiteSpace(baseZhi)) return baseZhi.Trim().TrimEnd('/');
            return bendidizhi;
        }
    }

    public void Start(int duankouzhi, string? baocunmiyao)
    {
        Stop();
        duankou = duankouzhi;
        EnsureDeviceKey(baocunmiyao);

        try
        {
            _jiantingqi = new TcpListener(IPAddress.Any, duankou);
            _jiantingqi.Start();
            _quxiao = new CancellationTokenSource();
            _jiantingrenwu = Task.Run(() => AcceptLoop(_quxiao.Token));
            zhuangtai = EmbeddedServerState.Running;
            zhuangtaiwenben = $"监听 0.0.0.0:{duankou}";
            _ = Task.Run(StartTunnel);
        }
        catch (Exception cuowu)
        {
            zhuangtai = EmbeddedServerState.Failed;
            zhuangtaiwenben = "启动失败: " + cuowu.Message;
            _jiantingqi = null;
        }
    }

    public void Stop()
    {
        _quxiao?.Cancel();
        StopTunnel();
        try { _jiantingqi?.Stop(); } catch { }
        lock (_suozhi)
        {
            foreach (var ws in _kehu)
            {
                try { if (ws.State == WebSocketState.Open) ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "server stopping", CancellationToken.None).Wait(TimeSpan.FromSeconds(1)); } catch { }
            }
            _kehu.Clear();
        }
        _jiantingqi = null;
        zhuangtai = EmbeddedServerState.Stopped;
        zhuangtaiwenben = "未启动";
    }

    public void BroadcastSnapshot(SystemSnapshot kuaizhao)
    {
        kuaizhao.tokenjiankong = RelaySnapshotUploader.BuildTokenInfoPublic();
        var json = JsonSerializer.Serialize(kuaizhao, xuanxiang);
        var xiaoxizijie = Encoding.UTF8.GetBytes(json);

        lock (_suozhi)
        {
            _zuijinjson = json;
            _zuijinkuaizhao = kuaizhao;
            _zuijinshijian = DateTime.UtcNow;
            if (_kehu.Count == 0) return;

            var siwang = new List<WebSocket>();
            foreach (var ws in _kehu)
            {
                try
                {
                    if (ws.State == WebSocketState.Open)
                        ws.SendAsync(new ArraySegment<byte>(xiaoxizijie), WebSocketMessageType.Text, true, CancellationToken.None);
                    else
                        siwang.Add(ws);
                }
                catch { siwang.Add(ws); }
            }
            foreach (var d in siwang) _kehu.Remove(d);
        }
    }

    public string EnsureDeviceKey(string? baocunmiyao)
    {
        _miyao = LoadOrGenerateKey(baocunmiyao);
        return _miyao;
    }

    private void StartTunnel()
    {
        try
        {
            var ltlujing = FindLocalTunnelPath();
            if (string.IsNullOrWhiteSpace(ltlujing)) return;

            _suidaojincheng = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ltlujing,
                    Arguments = $"--port {duankou}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };

            var urlJieguo = new TaskCompletionSource<string>();
            _suidaojincheng.OutputDataReceived += (_, e) =>
            {
                var hang = e.Data;
                if (string.IsNullOrWhiteSpace(hang)) return;
                if (hang.Contains("loca.lt") || hang.Contains("https://"))
                {
                    var piPei = System.Text.RegularExpressions.Regex.Match(hang, @"https://[a-z0-9\-]+\.loca\.lt");
                    if (piPei.Success)
                    {
                        _suidaoURL = piPei.Value;
                        urlJieguo.TrySetResult(_suidaoURL);
                    }
                }
            };
            _suidaojincheng.Exited += (_, _) =>
            {
                if (string.IsNullOrEmpty(_suidaoURL))
                    urlJieguo.TrySetResult("");
            };

            _suidaojincheng.Start();
            _suidaojincheng.BeginOutputReadLine();
            _suidaojincheng.BeginErrorReadLine();

            // Wait up to 10 seconds for the URL
            urlJieguo.Task.Wait(TimeSpan.FromSeconds(10));
        }
        catch
        {
            _suidaoURL = "";
        }
    }

    private void StopTunnel()
    {
        if (_suidaojincheng is null) return;
        try
        {
            if (!_suidaojincheng.HasExited)
            {
                _suidaojincheng.Kill(entireProcessTree: true);
            }
        }
        catch { }
        _suidaojincheng = null;
        _suidaoURL = "";
    }

    private static string FindLocalTunnelPath()
    {
        var houxuan = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"npm\lt.cmd"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"npm\lt"),
            @"C:\Users\Yue\AppData\Roaming\npm\lt.cmd",
        };

        foreach (var p in houxuan)
        {
            try { if (File.Exists(p) || p.EndsWith(".cmd")) return p; }
            catch { }
        }
        return "";
    }

    private async Task AcceptLoop(CancellationToken quxiao)
    {
        while (!quxiao.IsCancellationRequested && _jiantingqi is not null)
        {
            TcpClient kehu;
            try
            {
                kehu = await _jiantingqi.AcceptTcpClientAsync(quxiao);
            }
            catch { break; }

            if (quxiao.IsCancellationRequested) { kehu.Close(); break; }
            _ = HandleClient(kehu, quxiao);
        }
    }

    private async Task HandleClient(TcpClient kehu, CancellationToken quxiao)
    {
        using var lianjie = kehu;
        lianjie.NoDelay = true;
        using var wangluoliu = lianjie.GetStream();
        var jiacun = new byte[8192];
        var duxie = new MemoryStream();

        try
        {
            while (true)
            {
                var dushu = await wangluoliu.ReadAsync(jiacun, quxiao);
                if (dushu == 0) break;
                duxie.Write(jiacun, 0, dushu);
                if (duxie.Length > 65536) break;
                if (HasCompleteHeader(duxie)) break;
            }
        }
        catch { return; }

        if (duxie.Length == 0) return;

        var qingqiuwenben = Encoding.UTF8.GetString(duxie.ToArray());
        var qingqiu = ParseHttpRequest(qingqiuwenben);
        if (qingqiu is null) return;

        if (qingqiu.isWebSocket)
        {
            await HandleWebSocket(wangluoliu, qingqiu, quxiao);
        }
        else
        {
            HandleHttp(wangluoliu, qingqiu);
        }
    }

    private async Task HandleWebSocket(NetworkStream wangluoliu, HttpRequest qingqiu, CancellationToken quxiao)
    {
        if (!ValidateKey(qingqiu.headers.GetValueOrDefault("X-Device-Key")))
        {
            var cuowu = "HTTP/1.1 401 Unauthorized\r\nConnection: close\r\n\r\n";
            var zijie = Encoding.UTF8.GetBytes(cuowu);
            await wangluoliu.WriteAsync(zijie, quxiao);
            return;
        }

        var wsKey = qingqiu.headers.GetValueOrDefault("Sec-WebSocket-Key") ?? "";
        if (string.IsNullOrEmpty(wsKey))
        {
            var cuowu = "HTTP/1.1 400 Bad Request\r\nConnection: close\r\n\r\n";
            var zijie = Encoding.UTF8.GetBytes(cuowu);
            await wangluoliu.WriteAsync(zijie, quxiao);
            return;
        }

        var acceptKey = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(wsKey + wsMagic)));
        var huifu = new StringBuilder();
        huifu.Append("HTTP/1.1 101 Switching Protocols\r\n");
        huifu.Append("Upgrade: websocket\r\n");
        huifu.Append("Connection: Upgrade\r\n");
        huifu.Append($"Sec-WebSocket-Accept: {acceptKey}\r\n");
        huifu.Append("\r\n");
        var huifuzijie = Encoding.UTF8.GetBytes(huifu.ToString());
        await wangluoliu.WriteAsync(huifuzijie, quxiao);

        var ws = WebSocket.CreateFromStream(wangluoliu, new WebSocketCreationOptions { IsServer = true, SubProtocol = null });

        lock (_suozhi) { _kehu.Add(ws); }

        try
        {
            var jiacun2 = new byte[4096];
            while (ws.State == WebSocketState.Open && !quxiao.IsCancellationRequested)
            {
                var jieguo = await ws.ReceiveAsync(new ArraySegment<byte>(jiacun2), quxiao);
                if (jieguo.MessageType == WebSocketMessageType.Close) break;
            }
        }
        catch { }
        finally
        {
            lock (_suozhi) { _kehu.Remove(ws); }
            try { ws.Dispose(); } catch { }
        }
    }

    private void HandleHttp(NetworkStream wangluoliu, HttpRequest qingqiu)
    {
        var lujing = qingqiu.path ?? "";

        try
        {
            if (lujing == "/health")
            {
                WriteJson(wangluoliu, 200, new { ok = true, duankou, lianjieshu, gongwang = androidDizhi });
                return;
            }

            if (!ValidateKey(qingqiu.headers.GetValueOrDefault("X-Device-Key")))
            {
                WriteJson(wangluoliu, 401, new { error = "invalid device key" });
                return;
            }

            if (lujing == "/api/snapshot/latest" && qingqiu.method == "GET")
            {
                lock (_suozhi)
                {
                    if (_zuijinkuaizhao is null)
                    {
                        WriteJson(wangluoliu, 404, new { error = "no snapshot yet" });
                        return;
                    }
                    WriteJson(wangluoliu, 200, new { receivedAt = _zuijinshijian.ToString("o"), snapshot = _zuijinkuaizhao });
                }
                return;
            }

            if (lujing == "/api/gateway" && qingqiu.method == "GET")
            {
                WriteJson(wangluoliu, 200, new { gateway = new { bendendizhi = bendidizhi, gongwangdizhi = androidDizhi, duankou } });
                return;
            }

            if (lujing == "/api/status" && qingqiu.method == "GET")
            {
                WriteJson(wangluoliu, 200, new { ok = true, duankou, lianjieshu, gongwang = androidDizhi });
                return;
            }

            WriteJson(wangluoliu, 404, new { error = "not found" });
        }
        catch
        {
            try { WriteJson(wangluoliu, 500, new { error = "internal error" }); } catch { }
        }
    }

    private bool ValidateKey(string? key)
    {
        return !string.IsNullOrEmpty(_miyao) && string.Equals(key?.Trim(), _miyao, StringComparison.Ordinal);
    }

    private static void WriteJson(NetworkStream wangluoliu, int daima, object neirong)
    {
        var json = JsonSerializer.Serialize(neirong);
        var zhengwen = Encoding.UTF8.GetBytes(json);
        var baotou = Encoding.UTF8.GetBytes($"HTTP/1.1 {daima} {ReasonPhrase(daima)}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {zhengwen.Length}\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Headers: Content-Type, X-Device-Key\r\nAccess-Control-Allow-Methods: GET, POST, OPTIONS\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n");
        wangluoliu.Write(baotou, 0, baotou.Length);
        wangluoliu.Write(zhengwen, 0, zhengwen.Length);
    }

    private static string ReasonPhrase(int daima) => daima switch
    {
        200 => "OK",
        204 => "No Content",
        401 => "Unauthorized",
        404 => "Not Found",
        500 => "Internal Server Error",
        _ => "OK"
    };

    private static bool HasCompleteHeader(MemoryStream duxie)
    {
        var wenben = Encoding.UTF8.GetString(duxie.ToArray());
        return wenben.Contains("\r\n\r\n");
    }

    private static HttpRequest? ParseHttpRequest(string wenben)
    {
        var qifen = wenben.IndexOf("\r\n\r\n");
        if (qifen < 0) return null;
        var baotou = wenben[..qifen];
        var hang = baotou.Split("\r\n");
        if (hang.Length == 0) return null;

        var qingqiuxing = hang[0].Split(' ');
        if (qingqiuxing.Length < 2) return null;

        var method = qingqiuxing[0];
        var path = qingqiuxing[1];
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < hang.Length; i++)
        {
            var fenjie = hang[i].IndexOf(':');
            if (fenjie > 0)
            {
                var jian = hang[i][..fenjie].Trim();
                var zhi = hang[i][(fenjie + 1)..].Trim();
                headers[jian] = zhi;
            }
        }

        var isWs = headers.TryGetValue("Upgrade", out var upgrade) &&
                   upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase);

        return new HttpRequest(method, path, headers, isWs);
    }

    private static string LoadOrGenerateKey(string? baocun)
    {
        if (!string.IsNullOrWhiteSpace(baocun) && baocun != "change-me") return baocun;

        var lujing = Path.Combine(AppContext.BaseDirectory, "embedded-server-key.txt");
        try
        {
            if (File.Exists(lujing))
            {
                var cunzai = File.ReadAllText(lujing, Encoding.UTF8).Trim();
                if (!string.IsNullOrWhiteSpace(cunzai)) return cunzai;
            }
        }
        catch { }

        var xinmiyao = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        try { File.WriteAllText(lujing, xinmiyao + "\n", Encoding.UTF8); } catch { }
        return xinmiyao;
    }

    public void Dispose()
    {
        Stop();
        _quxiao?.Dispose();
    }

    private sealed record HttpRequest(string method, string path, Dictionary<string, string> headers, bool isWebSocket);
}
