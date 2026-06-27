using System.Globalization;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using HwMonitor.Models;
using Microsoft.Win32;

namespace HwMonitor.Hardware;

public sealed class HardwareCollector
{
    private ulong _shangcizongliangchuliqi;
    private ulong _shangcikongxianchuliqi;
    private bool _youshangcichuliqi;
    private readonly Dictionary<string, NetworkSample> _wangluoyangben = new(StringComparer.OrdinalIgnoreCase);

    private static string? _huancuncpuxinghao;
    private static double _huancuncpuzhucepinlv;
    private static bool _yijinghuancuncpuxinghao;
    private static int _huancuncpuhexin = -1;
    private static int _huancuncpuxiancheng = -1;
    private static double _huancuncpudangqianpinlv;
    private static DateTime _huancuncputuopushi;
    private static readonly TimeSpan _cputuopuhuancunqi = TimeSpan.FromSeconds(15);
    private static bool _yijinghuancuncputuopu;

    private static string? _huancunxitongming;

    private static List<GpuInfo>? _huancunxiankawmi;
    private static DateTime _huancunxiankawmishi;
    private static readonly TimeSpan _xiankawmihuancunqi = TimeSpan.FromSeconds(60);
    private static List<GpuInfo>? _huancunxianshishebei;
    private static bool _yijinghuancunxianshishebei;

    public SystemSnapshot Collect()
    {
        var chuanganqi = HardwareSensors.Probe();
        var kuaizhao = new SystemSnapshot
        {
            diannaomingcheng = Environment.MachineName,
            zhimingcheng = ReadOsName(),
            neihe = RuntimeInformation.OSArchitecture.ToString(),
            yunxingshijianmiao = (long)(NativeMethods.GetTickCount64() / 1000UL),
            chuliqi3 = CollectCpu(chuanganqi),
            neicun = CollectMemory()
        };

        CollectDisks(kuaizhao);
        CollectGpus(kuaizhao, chuanganqi);
        CollectFans(kuaizhao, chuanganqi);
        CollectNetwork(kuaizhao);
        PowerEstimator.Apply(kuaizhao);

        return kuaizhao;
    }

    private CpuInfo CollectCpu(SensorSnapshot chuanganqi2)
    {
        var chuliqi = new CpuInfo
        {
            xinghao2 = ReadCpuModel(out var zhucebiaopinlv),
            zhipinlv = zhucebiaopinlv,
            wenduzhi = chuanganqi2.chuliqiwenduzhi
        };

        ReadCpuTopology(chuliqi);
        chuliqi.shiyonglvbaifenbi = ReadCpuUsage();
        if (chuanganqi2.chuliqigonghao > 0)
        {
            chuliqi.gonghaowazhi = chuanganqi2.chuliqigonghao;
            chuliqi.gonghaogusuan = false;
        }
        return chuliqi;
    }

    private static string ReadCpuModel(out double pinlv){
        if (_yijinghuancuncpuxinghao)
        {
            pinlv = _huancuncpuzhucepinlv;
            return _huancuncpuxinghao ?? "Unknown CPU";
        }

        pinlv = 0;
        string xinghaojieguo = "Unknown CPU";
        try
        {
            using var jian = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            var xinghao = jian?.GetValue("ProcessorNameString")?.ToString()?.Trim();
            var yuanpinlv = jian?.GetValue("~MHz");
            if (yuanpinlv is not null) pinlv = Convert.ToDouble(yuanpinlv, CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(xinghao)) xinghaojieguo = xinghao;
        }
        catch{
            
        }

        _huancuncpuxinghao = xinghaojieguo;
        _huancuncpuzhucepinlv = pinlv;
        _yijinghuancuncpuxinghao = true;
        return xinghaojieguo;
    }

    private static void ReadCpuTopology(CpuInfo chuliqi2)
    {
        chuliqi2.xiancheng = Environment.ProcessorCount;
        chuliqi2.hexin2 = chuliqi2.xiancheng;

        if (_yijinghuancuncputuopu)
        {
            if (_huancuncpuxiancheng > 0) chuliqi2.xiancheng = _huancuncpuxiancheng;
            if (_huancuncpuhexin > 0) chuliqi2.hexin2 = _huancuncpuhexin;
            if (_huancuncpudangqianpinlv > 0) chuliqi2.zhipinlv = _huancuncpudangqianpinlv;
            if (DateTime.UtcNow - _huancuncputuopushi < _cputuopuhuancunqi) return;
        }

        try
        {
            using var chaxun7 = WmiSearcher(@"\\.\root\CIMV2", "SELECT NumberOfCores,NumberOfLogicalProcessors,CurrentClockSpeed FROM Win32_Processor");
            var hexin = 0;
            var luoji = 0;
            double dangqianpinlv = 0;
            foreach (ManagementObject duixiang in chaxun7.Get())
            {
                using (duixiang)
                {
                    hexin += ToInt(duixiang["NumberOfCores"]);
                    luoji += ToInt(duixiang["NumberOfLogicalProcessors"]);
                    dangqianpinlv = Math.Max(dangqianpinlv, ToDouble(duixiang["CurrentClockSpeed"], 0));
                }
            }

            if (hexin > 0) chuliqi2.hexin2 = hexin;
            if (luoji > 0) chuliqi2.xiancheng = luoji;
            if (dangqianpinlv > 0) chuliqi2.zhipinlv = dangqianpinlv;

            if (hexin > 0) _huancuncpuhexin = hexin;
            if (luoji > 0) _huancuncpuxiancheng = luoji;
            if (dangqianpinlv > 0) _huancuncpudangqianpinlv = dangqianpinlv;
            _yijinghuancuncputuopu = true;
            _huancuncputuopushi = DateTime.UtcNow;
        }
        catch
        {
            
        }
    }

    private double ReadCpuUsage()
    {
        if (!NativeMethods.GetSystemTimes(out var kongxianshijian, out var neiheshijian, out var yonghushijian))
        {
            return 0;
        }

        var kongxian = kongxianshijian.ToUInt64();
        var zongliang = neiheshijian.ToUInt64() + yonghushijian.ToUInt64();
        if (!_youshangcichuliqi)
        {
            _shangcikongxianchuliqi = kongxian;
            _shangcizongliangchuliqi = zongliang;
            _youshangcichuliqi = true;
            return 0;
        }

        var zongliangchazhi = zongliang - _shangcizongliangchuliqi;
        var kongxianchazhi = kongxian - _shangcikongxianchuliqi;
        _shangcikongxianchuliqi = kongxian;
        _shangcizongliangchuliqi = zongliang;

        if (zongliangchazhi == 0) return 0;
        var shiyonglv = 100d * (1d - kongxianchazhi / (double)zongliangchazhi);
        return Math.Clamp(shiyonglv, 0, 100);
    }

    private static MemInfo CollectMemory()
    {
        var zhuangtai = new NativeMethods.MemoryStatusEx
        {
            changdu2 = (uint)Marshal.SizeOf<NativeMethods.MemoryStatusEx>()
        };

        if (!NativeMethods.GlobalMemoryStatusEx(ref zhuangtai))
        {
            return new MemInfo();
        }

        var zongliang2 = zhuangtai.zongliangzhi / 1024UL;
        var keyong = zhuangtai.keyongzhi / 1024UL;
        var yiyong = zongliang2 > keyong ? zongliang2 - keyong : 0;
        return new MemInfo
        {
            zongliangzhi3 = zongliang2,
            keyongzhi3 = keyong,
            shengyuzhi = keyong,
            yiyongzhi = yiyong,
            shiyonglvbaifenbi2 = zongliang2 > 0 ? yiyong * 100d / zongliang2 : 0
        };
    }

    private static void CollectDisks(SystemSnapshot kuaizhao2)
    {
        foreach (var cipan in DriveInfo.GetDrives())
        {
            if (kuaizhao2.cipan2.Count >= 8) break;
            if (cipan.DriveType is not (DriveType.Fixed or DriveType.Removable)) continue;
            if (!cipan.IsReady) continue;

            try
            {
                var zongliang3 = (ulong)Math.Max(0, cipan.TotalSize / 1024L);
                var shengyu = (ulong)Math.Max(0, cipan.AvailableFreeSpace / 1024L);
                var yiyong2 = zongliang3 > shengyu ? zongliang3 - shengyu : 0;
                var juan = string.IsNullOrWhiteSpace(cipan.VolumeLabel) ? "" : $" ({cipan.VolumeLabel})";
                kuaizhao2.cipan2.Add(new DiskInfo
                {
                    cipanmingcheng = cipan.Name.TrimEnd('\\') + juan,
                    shebei2 = cipan.Name,
                    zhileixing = cipan.DriveFormat,
                    zongliangzhi4 = zongliang3,
                    shengyuzhi2 = shengyu,
                    yiyongzhi2 = yiyong2,
                    shiyonglvbaifenbi3 = zongliang3 > 0 ? yiyong2 * 100d / zongliang3 : 0
                });
            }
            catch
            {
                
            }
        }
    }

    private static void CollectGpus(SystemSnapshot kuaizhao3, SensorSnapshot chuanganqi3)
    {
        var smixianqia = HardwareSensors.ProbeNvidiaSmi();
        foreach (var xianqia in smixianqia.Take(4))
        {
            HardwareSensors.ApplyGpuSensorFallback(xianqia, chuanganqi3);
            AddOrMergeGpu(kuaizhao3, xianqia);
        }

        try
        {
            foreach (var xianqia2 in QueryWmiVideoControllers())
            {
                if (kuaizhao3.xianqia7.Count >= 4) break;
                if (string.IsNullOrWhiteSpace(xianqia2.xinghao3) || IsVirtualGpu(xianqia2.xinghao3)) continue;

                var gpu = new GpuInfo
                {
                    xinghao3 = xianqia2.xinghao3,
                    zhizongliangzhi = xianqia2.zhizongliangzhi
                };

                var smi = smixianqia.FirstOrDefault(dangqianzhi => GpuNamesMatch(gpu.xinghao3, dangqianzhi.xinghao3));
                if (smi is not null)
                {
                    gpu.wenduzhi2 = smi.wenduzhi2;
                    gpu.shiyonglvbaifenbi4 = smi.shiyonglvbaifenbi4;
                    gpu.zhizongliangzhi = smi.zhizongliangzhi > 0 ? smi.zhizongliangzhi : gpu.zhizongliangzhi;
                    gpu.zhiyiyongzhi = smi.zhiyiyongzhi;
                    gpu.fengshanbaifenbi = smi.fengshanbaifenbi;
                    gpu.hexinpinlv = smi.hexinpinlv;
                    gpu.neicunpinlv = smi.neicunpinlv;
                    gpu.gonghaowazhi = smi.gonghaowazhi;
                    gpu.gonghaogusuan = smi.gonghaogusuan;
                }

                HardwareSensors.ApplyGpuSensorFallback(gpu, chuanganqi3);
                AddOrMergeGpu(kuaizhao3, gpu);
            }
        }
        catch
        {
            
        }

        foreach (var xianqia3 in EnumerateDisplayDevices())
        {
            if (kuaizhao3.xianqia7.Count >= 4) break;
            AddOrMergeGpu(kuaizhao3, xianqia3);
        }

        if (smixianqia.Count > 0)
        {
            foreach (var xianqia4 in kuaizhao3.xianqia7)
            {
                var smi2 = smixianqia.FirstOrDefault(pipeixianqia => GpuNamesMatch(xianqia4.xinghao3, pipeixianqia.xinghao3));
                if (smi2 is null) continue;
                MergeGpu(xianqia4, smi2);
            }
        }
    }

    private static List<GpuInfo> QueryWmiVideoControllers()
    {
        if (_huancunxiankawmi is not null && DateTime.UtcNow - _huancunxiankawmishi < _xiankawmihuancunqi)
        {
            return _huancunxiankawmi;
        }

        var jieguo = new List<GpuInfo>();
        try
        {
            using var chaxunqi2 = WmiSearcher(@"\\.\root\CIMV2", "SELECT Name,AdapterRAM FROM Win32_VideoController");
            foreach (ManagementObject duixiang2 in chaxunqi2.Get())
            {
                using (duixiang2)
                {
                    var mingcheng = duixiang2["Name"]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(mingcheng) || IsVirtualGpu(mingcheng)) continue;
                    jieguo.Add(new GpuInfo
                    {
                        xinghao3 = mingcheng,
                        zhizongliangzhi = ToUInt64(duixiang2["AdapterRAM"]) / 1024UL
                    });
                }
            }
        }
        catch
        {
            
        }

        _huancunxiankawmi = jieguo;
        _huancunxiankawmishi = DateTime.UtcNow;
        return jieguo;
    }

    private static IEnumerable<GpuInfo> EnumerateDisplayDevices()
    {
        if (_yijinghuancunxianshishebei && _huancunxianshishebei is not null)
        {
            foreach (var xianqia in _huancunxianshishebei) yield return xianqia;
            yield break;
        }

        var jieguo = new List<GpuInfo>();
        for (uint suoyin = 0; suoyin < 16; suoyin++)
        {
            var shebei = new NativeMethods.DisplayDevice
            {
                jiegoudaxiao = Marshal.SizeOf<NativeMethods.DisplayDevice>()
            };
            if (!NativeMethods.EnumDisplayDevices(null, suoyin, ref shebei, 0)) break;

            var mingcheng2 = shebei.shebeizhi?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(mingcheng2) || IsVirtualGpu(mingcheng2)) continue;
            const int xianshishebeijihuo = 0x00000001;
            if ((shebei.zhuangtaizhi & xianshishebeijihuo) == 0) continue;

            jieguo.Add(new GpuInfo { xinghao3 = mingcheng2 });
        }

        _huancunxianshishebei = jieguo;
        _yijinghuancunxianshishebei = true;
        foreach (var xianqia in jieguo) yield return xianqia;
    }

    private static void AddOrMergeGpu(SystemSnapshot kuaizhao4, GpuInfo laizhi)
    {
        if (string.IsNullOrWhiteSpace(laizhi.xinghao3)) return;
        var yiyou = kuaizhao4.xianqia7.FirstOrDefault(huitu => GpuNamesMatch(huitu.xinghao3, laizhi.xinghao3));
        if (yiyou is null)
        {
            if (kuaizhao4.xianqia7.Count < 4) kuaizhao4.xianqia7.Add(laizhi);
            return;
        }

        MergeGpu(yiyou, laizhi);
    }

    private static void MergeGpu(GpuInfo mubiao, GpuInfo laiyuan)
    {
        if (string.IsNullOrWhiteSpace(mubiao.xinghao3)) mubiao.xinghao3 = laiyuan.xinghao3;
        if (mubiao.wenduzhi2 < 0 && laiyuan.wenduzhi2 >= 0) mubiao.wenduzhi2 = laiyuan.wenduzhi2;
        if (mubiao.shiyonglvbaifenbi4 <= 0 && laiyuan.shiyonglvbaifenbi4 > 0) mubiao.shiyonglvbaifenbi4 = laiyuan.shiyonglvbaifenbi4;
        if (mubiao.hexinpinlv <= 0 && laiyuan.hexinpinlv > 0) mubiao.hexinpinlv = laiyuan.hexinpinlv;
        if (mubiao.neicunpinlv <= 0 && laiyuan.neicunpinlv > 0) mubiao.neicunpinlv = laiyuan.neicunpinlv;
        if (mubiao.fengshanbaifenbi <= 0 && laiyuan.fengshanbaifenbi > 0) mubiao.fengshanbaifenbi = laiyuan.fengshanbaifenbi;
        if (mubiao.zhizongliangzhi <= 0 && laiyuan.zhizongliangzhi > 0) mubiao.zhizongliangzhi = laiyuan.zhizongliangzhi;
        if (mubiao.zhiyiyongzhi <= 0 && laiyuan.zhiyiyongzhi > 0) mubiao.zhiyiyongzhi = laiyuan.zhiyiyongzhi;
        if ((mubiao.gonghaowazhi <= 0 || mubiao.gonghaogusuan) && laiyuan.gonghaowazhi > 0)
        {
            mubiao.gonghaowazhi = laiyuan.gonghaowazhi;
            mubiao.gonghaogusuan = laiyuan.gonghaogusuan;
        }
    }

    private static void CollectFans(SystemSnapshot kuaizhao5, SensorSnapshot chuanganqi4)
    {
        foreach (var fengshan in chuanganqi4.fengshan2.Take(8))
        {
            kuaizhao5.fengshan3.Add(fengshan);
        }

        foreach (var xianqia5 in kuaizhao5.xianqia7)
        {
            if (kuaizhao5.fengshan3.Count >= 8) break;
            if (xianqia5.fengshanbaifenbi <= 0) continue;
            if (kuaizhao5.fengshan3.Any(fengshanxiang => fengshanxiang.leixing2 == "gpu")) continue;
            kuaizhao5.fengshan3.Add(new FanInfo { mingcheng9 = "GPU Fan", leixing2 = "gpu", baifenbi2 = xianqia5.fengshanbaifenbi });
        }
    }

    private void CollectNetwork(SystemSnapshot kuaizhao6)
    {
        var xianzai = DateTime.UtcNow;
        foreach (var wangka in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (kuaizhao6.wangluo.Count >= 8) break;
            if (wangka.OperationalStatus != OperationalStatus.Up) continue;
            if (wangka.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

            try
            {
                var tongji = wangka.GetIPv4Statistics();
                var xiazai = Math.Max(0, tongji.BytesReceived);
                var shangchuan = Math.Max(0, tongji.BytesSent);
                ulong xiazaisudu = 0;
                ulong shangchuansudu = 0;
                var jian2 = wangka.Id;

                if (_wangluoyangben.TryGetValue(jian2, out var shangci))
                {
                    var miao = Math.Max(0.25, (xianzai - shangci.shijian).TotalSeconds);
                    if (xiazai >= shangci.xiazaizijie) xiazaisudu = (ulong)((xiazai - shangci.xiazaizijie) / miao);
                    if (shangchuan >= shangci.shangchuanzijie) shangchuansudu = (ulong)((shangchuan - shangci.shangchuanzijie) / miao);
                }

                _wangluoyangben[jian2] = new NetworkSample(xiazai, shangchuan, xianzai);
                kuaizhao6.wangluo.Add(new NetInfo
                {
                    mingcheng10 = string.IsNullOrWhiteSpace(wangka.Description) ? wangka.Name : wangka.Description,
                    xiazaizijie2 = (ulong)xiazai,
                    shangchuanzijie2 = (ulong)shangchuan,
                    xiazaisudu2 = xiazaisudu,
                    shangchuansudu2 = shangchuansudu
                });
            }
            catch
            {
                
            }
        }
    }

    private static string ReadOsName()
    {
        if (_huancunxitongming is not null) return _huancunxitongming;

        try
        {
            using var jian3 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var chanpin = jian3?.GetValue("ProductName")?.ToString();
            var banben = jian3?.GetValue("CurrentBuild")?.ToString();
            var gengxinbanben = jian3?.GetValue("UBR")?.ToString();
            if (!string.IsNullOrWhiteSpace(chanpin))
            {
                var jieguo = string.IsNullOrWhiteSpace(banben) ? chanpin : $"{chanpin} (Build {banben}{(string.IsNullOrWhiteSpace(gengxinbanben) ? "" : "." + gengxinbanben)})";
                _huancunxitongming = jieguo;
                return jieguo;
            }
        }
        catch
        {
            
        }

        _huancunxitongming = RuntimeInformation.OSDescription;
        return _huancunxitongming;
    }

    private static ManagementObjectSearcher WmiSearcher(string fanweilujing, string chaxun)
    {
        var fanwei = new ManagementScope(fanweilujing);
        var chaxunqi3 = new ManagementObjectSearcher(fanwei, new ObjectQuery(chaxun));
        chaxunqi3.Options.Timeout = TimeSpan.FromMilliseconds(1400);
        chaxunqi3.Options.ReturnImmediately = true;
        chaxunqi3.Options.Rewindable = false;
        return chaxunqi3;
    }

    private static bool GpuNamesMatch(string zuo, string you)
    {
        if (string.IsNullOrWhiteSpace(zuo) || string.IsNullOrWhiteSpace(you)) return false;
        return zuo.Contains(you, StringComparison.OrdinalIgnoreCase) ||
               you.Contains(zuo, StringComparison.OrdinalIgnoreCase) ||
               NormalizeGpuName(zuo).Contains(NormalizeGpuName(you), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeGpuName(string yuanmingcheng)
    {
        return yuanmingcheng.Replace("NVIDIA", "", StringComparison.OrdinalIgnoreCase)
            .Replace("GeForce", "", StringComparison.OrdinalIgnoreCase)
            .Replace("AMD", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Radeon", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static bool IsVirtualGpu(string mingcheng3)
    {
        return mingcheng3.Contains("Miracast", StringComparison.OrdinalIgnoreCase) ||
               mingcheng3.Contains("Mirror", StringComparison.OrdinalIgnoreCase) ||
               mingcheng3.Contains("Indirect", StringComparison.OrdinalIgnoreCase) ||
               mingcheng3.Contains("Basic Render", StringComparison.OrdinalIgnoreCase);
    }

    private static int ToInt(object? zhengshuzhi)
    {
        try { return zhengshuzhi is null ? 0 : Convert.ToInt32(zhengshuzhi, CultureInfo.InvariantCulture); }
        catch { return 0; }
    }

    private static ulong ToUInt64(object? changzhengshuzhi)
    {
        try { return changzhengshuzhi is null ? 0 : Convert.ToUInt64(changzhengshuzhi, CultureInfo.InvariantCulture); }
        catch { return 0; }
    }

    private static double ToDouble(object? xiaoshuzhi, double beiyong)
    {
        try { return xiaoshuzhi is null ? beiyong : Convert.ToDouble(xiaoshuzhi, CultureInfo.InvariantCulture); }
        catch { return beiyong; }
    }

    private readonly record struct NetworkSample(long xiazaizijie, long shangchuanzijie, DateTime shijian);
}
