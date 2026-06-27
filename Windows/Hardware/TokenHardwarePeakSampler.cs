using System.Threading;

namespace HwMonitor.Hardware;

internal sealed class TokenHardwarePeakSampler : IDisposable
{
    private const int caijijiangehaomiao = 1500;

    private readonly CancellationTokenSource _guanbi = new();
    private readonly object _suo = new();
    private TokenHardwarePeak _fengzhi = TokenHardwarePeak.Empty;
    private ulong _shangcichuliqizongliang;
    private ulong _shangcichuliqikongxian;
    private bool _youshangcichuliqi;
    private int _zhengzaicaiji;

    private TokenHardwarePeakSampler()
    {
    }

    public static TokenHardwarePeakSampler Start()
    {
        var caijiqi = new TokenHardwarePeakSampler();
        _ = Task.Run(caijiqi.RunAsync);
        return caijiqi;
    }

    public TokenHardwarePeak StopAndSnapshot()
    {
        _guanbi.Cancel();
        return Snapshot();
    }

    public void Dispose()
    {
        _guanbi.Cancel();
        _guanbi.Dispose();
    }

    private async Task RunAsync()
    {
        while (!_guanbi.IsCancellationRequested)
        {
            SampleOnce();
            try
            {
                await Task.Delay(caijijiangehaomiao, _guanbi.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private TokenHardwarePeak Snapshot()
    {
        lock (_suo)
        {
            return _fengzhi;
        }
    }

    private void SampleOnce()
    {
        if (Interlocked.Exchange(ref _zhengzaicaiji, 1) == 1) return;
        try
        {
            var yangben = CollectSample();
            lock (_suo)
            {
                _fengzhi = Merge(_fengzhi, yangben);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _zhengzaicaiji, 0);
        }
    }

    private TokenHardwarePeak CollectSample()
    {
        var chuliqifengzhi = ReadCpuPercent();
        var neicun = ReadMemory();
        var xianqiafengzhi = ReadGpu();
        return new TokenHardwarePeak(
            chuliqifengzhi,
            xianqiafengzhi.xianqiafengzhi,
            xianqiafengzhi.xiancunbaifenbi,
            xianqiafengzhi.xiancunyiyong,
            xianqiafengzhi.xiancunzongliang,
            neicun.baifenbi,
            neicun.yiyong,
            neicun.zongliang);
    }

    private double ReadCpuPercent()
    {
        if (!NativeMethods.GetSystemTimes(out var kongxianshijian, out var neiheshijian, out var yonghushijian))
        {
            return -1;
        }

        var kongxian = kongxianshijian.ToUInt64();
        var zongliang = neiheshijian.ToUInt64() + yonghushijian.ToUInt64();
        if (!_youshangcichuliqi)
        {
            _shangcichuliqikongxian = kongxian;
            _shangcichuliqizongliang = zongliang;
            _youshangcichuliqi = true;
            return -1;
        }

        var zongchazhi = zongliang - _shangcichuliqizongliang;
        var kongxianchazhi = kongxian - _shangcichuliqikongxian;
        _shangcichuliqikongxian = kongxian;
        _shangcichuliqizongliang = zongliang;
        if (zongchazhi == 0) return -1;

        return Math.Clamp(100d * (1d - kongxianchazhi / (double)zongchazhi), 0d, 100d);
    }

    private static (double baifenbi, ulong yiyong, ulong zongliang) ReadMemory()
    {
        var zhuangtai = new NativeMethods.MemoryStatusEx
        {
            changdu2 = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MemoryStatusEx>()
        };
        if (!NativeMethods.GlobalMemoryStatusEx(ref zhuangtai))
        {
            return (-1, 0, 0);
        }

        var zongliang = zhuangtai.zongliangzhi / 1024UL;
        var keyong = zhuangtai.keyongzhi / 1024UL;
        var yiyong = zongliang > keyong ? zongliang - keyong : 0;
        var baifenbi = zongliang > 0 ? Math.Clamp(yiyong * 100d / zongliang, 0d, 100d) : -1;
        return (baifenbi, yiyong, zongliang);
    }

    private static (double xianqiafengzhi, double xiancunbaifenbi, ulong xiancunyiyong, ulong xiancunzongliang) ReadGpu()
    {
        try
        {
            var xianqia = HardwareSensors.ProbeNvidiaSmi().FirstOrDefault();
            if (xianqia is null)
            {
                return (-1, -1, 0, 0);
            }

            var xiancunbaifenbi = xianqia.zhizongliangzhi > 0
                ? Math.Clamp(xianqia.zhiyiyongzhi * 100d / xianqia.zhizongliangzhi, 0d, 100d)
                : -1;
            return (
                Math.Clamp(xianqia.shiyonglvbaifenbi4, 0d, 100d),
                xiancunbaifenbi,
                xianqia.zhiyiyongzhi,
                xianqia.zhizongliangzhi);
        }
        catch
        {
            return (-1, -1, 0, 0);
        }
    }

    private static TokenHardwarePeak Merge(TokenHardwarePeak dangqian, TokenHardwarePeak yangben)
    {
        return new TokenHardwarePeak(
            MaxPercent(dangqian.cpuzuigaobaifenbi, yangben.cpuzuigaobaifenbi),
            MaxPercent(dangqian.gpuzuigaobaifenbi, yangben.gpuzuigaobaifenbi),
            MaxPercent(dangqian.xiancunzuigaobaifenbi, yangben.xiancunzuigaobaifenbi),
            Math.Max(dangqian.xiancunyiyongfengzhi, yangben.xiancunyiyongfengzhi),
            Math.Max(dangqian.xiancunzongliang, yangben.xiancunzongliang),
            MaxPercent(dangqian.neicunzuigaobaifenbi, yangben.neicunzuigaobaifenbi),
            Math.Max(dangqian.neicunyiyongfengzhi, yangben.neicunyiyongfengzhi),
            Math.Max(dangqian.neicunzongliang, yangben.neicunzongliang));
    }

    private static double MaxPercent(double dangqian, double yangben)
    {
        var dangqian2 = double.IsFinite(dangqian) && dangqian > 0 ? dangqian : -1;
        var yangben2 = double.IsFinite(yangben) && yangben > 0 ? yangben : -1;
        return Math.Max(dangqian2, yangben2);
    }
}
