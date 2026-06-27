using System.Diagnostics;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Management;
using System.Text;
using HwMonitor.Models;

namespace HwMonitor.Hardware;

internal static class HardwareSensors
{
    private static readonly string[] zhixinxiyingshemingcheng =
    [
        @"Global\HWiNFO_SENS_SM2",
        "HWiNFO_SENS_SM2",
        @"Global\HWiNFO_SENSORS_SM",
        "HWiNFO_SENSORS_SM"
    ];

    private static readonly string[] hexinwenduyingshemingcheng =
    [
        "CoreTempMappingObject",
        @"Global\CoreTempMappingObject",
        @"Local\CoreTempMappingObject"
    ];

    public static SensorSnapshot Probe()
    {
        var chuanganqi5 = new SensorSnapshot();
        ProbeHwInfo(chuanganqi5);
        if (chuanganqi5.chuliqiwenduzhi < 0) ProbeCoreTemp(chuanganqi5);

        ProbeOpenHardwareNamespace(@"\\.\root\LibreHardwareMonitor", chuanganqi5);
        ProbeOpenHardwareNamespace(@"\\.\root\OpenHardwareMonitor", chuanganqi5);

        if (chuanganqi5.chuliqiwenduzhi < 0)
        {
            ProbeThermalZone(chuanganqi5);
        }

        return chuanganqi5;
    }

    public static List<GpuInfo> ProbeNvidiaSmi()
    {
        var liebiao = new List<GpuInfo>();
        var chengxu = FindNvidiaSmiPath();
        if (string.IsNullOrWhiteSpace(chengxu)) return liebiao;

        var shuchu = CaptureHidden(chengxu, "--query-gpu=name,temperature.gpu,utilization.gpu,memory.total,memory.used,fan.speed,clocks.gr,clocks.mem,power.draw --format=csv,noheader,nounits", 2500);
        if (string.IsNullOrWhiteSpace(shuchu)) return liebiao;

        foreach (var yuanhang in shuchu.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var bufen = yuanhang.Split(',').Select(yuanwenben => yuanwenben.Trim()).ToArray();
            if (bufen.Length < 8) continue;
            var gonghao = bufen.Length > 8 ? Number(bufen[8], -1) : -1;

            liebiao.Add(new GpuInfo
            {
                xinghao3 = bufen[0],
                wenduzhi2 = Number(bufen[1], -1),
                shiyonglvbaifenbi4 = Number(bufen[2], 0),
                zhizongliangzhi = (ulong)Math.Max(0, Number(bufen[3], 0) * 1024),
                zhiyiyongzhi = (ulong)Math.Max(0, Number(bufen[4], 0) * 1024),
                fengshanbaifenbi = Number(bufen[5], -1),
                hexinpinlv = Number(bufen[6], 0),
                neicunpinlv = Number(bufen[7], 0),
                gonghaowazhi = gonghao,
                gonghaogusuan = gonghao <= 0
            });
        }

        return liebiao;
    }

    public static void ApplyGpuSensorFallback(GpuInfo xianqia6, SensorSnapshot chuanganqi6)
    {
        if (chuanganqi6.xianqiawenduzhi >= 0) xianqia6.wenduzhi2 = chuanganqi6.xianqiawenduzhi;
        if (chuanganqi6.xianqiashiyonglvbaifenbi >= 0) xianqia6.shiyonglvbaifenbi4 = chuanganqi6.xianqiashiyonglvbaifenbi;
        if (chuanganqi6.xianqiahexinpinlv > 0) xianqia6.hexinpinlv = chuanganqi6.xianqiahexinpinlv;
        if (chuanganqi6.xianqianeicunpinlv > 0) xianqia6.neicunpinlv = chuanganqi6.xianqianeicunpinlv;
        if (chuanganqi6.xianqiafengshanbaifenbi > 0) xianqia6.fengshanbaifenbi = chuanganqi6.xianqiafengshanbaifenbi;
        if (chuanganqi6.xianqiagonghao > 0 && xianqia6.gonghaowazhi <= 0)
        {
            xianqia6.gonghaowazhi = chuanganqi6.xianqiagonghao;
            xianqia6.gonghaogusuan = false;
        }
    }

    private static void ProbeHwInfo(SensorSnapshot chuanganqi7)
    {
        foreach (var mingcheng4 in zhixinxiyingshemingcheng)
        {
            try
            {
                using var neicunwenjian = MemoryMappedFile.OpenExisting(mingcheng4);
                using var shitu = neicunwenjian.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                if (ReadUInt32(shitu, 0x00) is not (0x48576953 or 0x4E695748)) return;

                var chuanganqiweiyi = ReadUInt32(shitu, 0x14);
                var chuanganqidaxiao = ReadUInt32(shitu, 0x18);
                var chuanganqishuliang = ReadUInt32(shitu, 0x1C);
                var duquweiyi = ReadUInt32(shitu, 0x20);
                var duqudaxiao = ReadUInt32(shitu, 0x24);
                var duqushuliang = ReadUInt32(shitu, 0x28);

                if (chuanganqiweiyi == 0 || chuanganqidaxiao == 0 || chuanganqishuliang == 0 || duquweiyi == 0 || duqudaxiao < 0x124 || duqushuliang == 0)
                {
                    return;
                }

                for (uint suoyin2 = 0; suoyin2 < duqushuliang; suoyin2++)
                {
                    var hang = duquweiyi + suoyin2 * duqudaxiao;
                    if (hang + 0x124 > (ulong)shitu.Capacity) break;
                    if (ReadUInt32(shitu, (long)hang) != 1) continue;

                    var chuanganqisuoyin = ReadUInt32(shitu, (long)hang + 0x04);
                    var chuanganqizhi = ReadDouble(shitu, (long)hang + 0x11C);
                    var danwei = ReadAnsi(shitu, (long)hang + 0x10C, 16);
                    var biaoqian = ReadAnsi(shitu, (long)hang + 0x0C, 128);
                    var chuanganqimingcheng = "";

                    if (chuanganqisuoyin < chuanganqishuliang)
                    {
                        chuanganqimingcheng = ReadAnsi(shitu, (long)(chuanganqiweiyi + chuanganqisuoyin * chuanganqidaxiao + 0x08), 128);
                    }

                    if (danwei.Contains("C", StringComparison.OrdinalIgnoreCase) && chuanganqizhi is > 0 and < 125)
                    {
                        var zhixianqia = Contains(chuanganqimingcheng, "GPU") || Contains(biaoqian, "GPU");
                        var zhichuliqi = Contains(chuanganqimingcheng, "CPU") || Contains(chuanganqimingcheng, "Intel") || Contains(chuanganqimingcheng, "AMD") ||
                                    Contains(biaoqian, "CPU Package") || Contains(biaoqian, "CPU Core") || Contains(biaoqian, "Core Max") ||
                                    Contains(biaoqian, "Core 0");
                        if (!zhixianqia && zhichuliqi && chuanganqi7.chuliqiwenduzhi < 0) chuanganqi7.chuliqiwenduzhi = chuanganqizhi;
                    }

                    if ((Contains(danwei, "RPM") || Contains(biaoqian, "Fan")) && chuanganqizhi is > 0 and < 20_000)
                    {
                        chuanganqi7.AddFan(biaoqian, chuanganqizhi, -1);
                    }

                    if (Contains(danwei, "%") && Contains(biaoqian, "Fan") && chuanganqizhi is > 0 and <= 100)
                    {
                        chuanganqi7.AddFan(biaoqian, -1, chuanganqizhi);
                    }

                    if (Contains(danwei, "W") && chuanganqizhi is > 0 and < 1000)
                    {
                        var zhixianqia3 = Contains(chuanganqimingcheng, "GPU") || Contains(biaoqian, "GPU") || Contains(biaoqian, "Graphics");
                        var zhichuliqi3 = Contains(chuanganqimingcheng, "CPU") || Contains(chuanganqimingcheng, "Intel") || Contains(chuanganqimingcheng, "AMD") ||
                                          Contains(biaoqian, "CPU") || Contains(biaoqian, "Package") || Contains(biaoqian, "Processor");
                        if (zhixianqia3 && chuanganqi7.xianqiagonghao < 0) chuanganqi7.xianqiagonghao = chuanganqizhi;
                        if (!zhixianqia3 && zhichuliqi3 && chuanganqi7.chuliqigonghao < 0) chuanganqi7.chuliqigonghao = chuanganqizhi;
                    }

                    if (!Contains(chuanganqimingcheng, "GPU")) continue;
                    if (danwei.Contains("C", StringComparison.OrdinalIgnoreCase) && chuanganqizhi is > 0 and < 125 &&
                        (Contains(biaoqian, "GPU Temperature") || Contains(biaoqian, "GPU Core")) && !Contains(biaoqian, "Hot Spot"))
                    {
                        chuanganqi7.xianqiawenduzhi = chuanganqizhi;
                    }

                    if ((Contains(biaoqian, "GPU Utilization") || Contains(biaoqian, "GPU Core Load")) && chuanganqizhi is >= 0 and <= 100)
                    {
                        chuanganqi7.xianqiashiyonglvbaifenbi = chuanganqizhi;
                    }

                    if (Contains(danwei, "MHz") && chuanganqizhi is > 0 and < 10_000)
                    {
                        if ((Contains(biaoqian, "GPU Clock") || Contains(biaoqian, "Graphics Clock") || Contains(biaoqian, "Core Clock")) && chuanganqi7.xianqiahexinpinlv < 0)
                        {
                            chuanganqi7.xianqiahexinpinlv = chuanganqizhi;
                        }
                        if ((Contains(biaoqian, "Memory Clock") || Contains(biaoqian, "GPU Memory Clock")) && chuanganqi7.xianqianeicunpinlv < 0)
                        {
                            chuanganqi7.xianqianeicunpinlv = chuanganqizhi;
                        }
                    }

                    if (Contains(biaoqian, "Fan") && Contains(danwei, "%") && chuanganqizhi is > 0 and <= 100)
                    {
                        chuanganqi7.xianqiafengshanbaifenbi = chuanganqizhi;
                    }

                    if (Contains(danwei, "W") && chuanganqizhi is > 0 and < 1000 && chuanganqi7.xianqiagonghao < 0)
                    {
                        chuanganqi7.xianqiagonghao = chuanganqizhi;
                    }
                }

                return;
            }
            catch
            {
                
            }
        }
    }

    private static void ProbeCoreTemp(SensorSnapshot chuanganqi8)
    {
        foreach (var mingcheng5 in hexinwenduyingshemingcheng)
        {
            try
            {
                using var neicunwenjian2 = MemoryMappedFile.OpenExisting(mingcheng5);
                using var shitu2 = neicunwenjian2.CreateViewAccessor(0, 8192, MemoryMappedFileAccess.Read);
                var hexinshuliang = ReadUInt32(shitu2, 0x600);
                var chuliqishuliang = ReadUInt32(shitu2, 0x604);
                if (hexinshuliang == 0 || hexinshuliang > 256 || chuliqishuliang == 0 || chuliqishuliang > 8) return;

                var zhichizuidawendu = ReadByte(shitu2, 0xA7D) != 0;
                var yuan = ReadFloat(shitu2, 0x608);
                var wendu = (double)yuan;
                if (zhichizuidawendu)
                {
                    var zhizuida = ReadUInt32(shitu2, 0x400);
                    var zuidawendu = zhizuida is > 0 and < 130 ? zhizuida : 100;
                    wendu = zuidawendu - yuan;
                }

                if (wendu is > 0 and < 125)
                {
                    chuanganqi8.chuliqiwenduzhi = wendu;
                    return;
                }
            }
            catch
            {
                
            }
        }
    }

    private static void ProbeOpenHardwareNamespace(string fanweilujing2, SensorSnapshot chuanganqi9)
    {
        try
        {
            using var chaxunqi4 = Searcher(fanweilujing2, "SELECT Name,SensorType,Identifier,Value FROM Sensor");
            foreach (ManagementObject duixiang3 in chaxunqi4.Get())
            {
                using (duixiang3)
                {
                    var leixing = Text(duixiang3["SensorType"]);
                    var mingcheng6 = Text(duixiang3["Name"]);
                    var bianhao = Text(duixiang3["Identifier"]);
                    var chuanganqizhi2 = Number(duixiang3["Value"], double.NaN);
                    if (double.IsNaN(chuanganqizhi2)) continue;

                    var zhixianqia2 = Contains(bianhao, "gpu") || Contains(mingcheng6, "GPU");
                    var zhichuliqi2 = Contains(bianhao, "cpu") || Contains(mingcheng6, "CPU") || Contains(mingcheng6, "Package") || Contains(mingcheng6, "Core");

                    if (Contains(leixing, "Temperature") && chuanganqizhi2 is > 0 and < 125 && zhichuliqi2 && !zhixianqia2 && chuanganqi9.chuliqiwenduzhi < 0)
                    {
                        chuanganqi9.chuliqiwenduzhi = chuanganqizhi2;
                    }

                    if (Contains(leixing, "Power") && chuanganqizhi2 is > 0 and < 1000 && zhichuliqi2 && !zhixianqia2 && chuanganqi9.chuliqigonghao < 0)
                    {
                        chuanganqi9.chuliqigonghao = chuanganqizhi2;
                    }

                    if (zhixianqia2)
                    {
                        if (Contains(leixing, "Temperature") && chuanganqizhi2 is > 0 and < 125 && chuanganqi9.xianqiawenduzhi < 0)
                        {
                            chuanganqi9.xianqiawenduzhi = chuanganqizhi2;
                        }
                        if ((Contains(leixing, "Load") || Contains(leixing, "Control")) && chuanganqizhi2 is >= 0 and <= 100 && Contains(mingcheng6, "Core") && chuanganqi9.xianqiashiyonglvbaifenbi < 0)
                        {
                            chuanganqi9.xianqiashiyonglvbaifenbi = chuanganqizhi2;
                        }
                        if (Contains(leixing, "Clock") && chuanganqizhi2 is > 0 and < 10_000)
                        {
                            if ((Contains(mingcheng6, "Core") || Contains(mingcheng6, "GPU")) && chuanganqi9.xianqiahexinpinlv < 0) chuanganqi9.xianqiahexinpinlv = chuanganqizhi2;
                            if (Contains(mingcheng6, "Memory") && chuanganqi9.xianqianeicunpinlv < 0) chuanganqi9.xianqianeicunpinlv = chuanganqizhi2;
                        }
                        if (Contains(leixing, "Control") && Contains(mingcheng6, "Fan") && chuanganqizhi2 is > 0 and <= 100 && chuanganqi9.xianqiafengshanbaifenbi < 0)
                        {
                            chuanganqi9.xianqiafengshanbaifenbi = chuanganqizhi2;
                        }
                        if (Contains(leixing, "Power") && chuanganqizhi2 is > 0 and < 1000 && chuanganqi9.xianqiagonghao < 0)
                        {
                            chuanganqi9.xianqiagonghao = chuanganqizhi2;
                        }
                    }

                    if (Contains(leixing, "Fan"))
                    {
                        chuanganqi9.AddFan(mingcheng6, chuanganqizhi2, -1);
                    }
                    else if (Contains(leixing, "Control") && Contains(mingcheng6, "Fan"))
                    {
                        chuanganqi9.AddFan(mingcheng6, -1, chuanganqizhi2);
                    }
                }
            }
        }
        catch
        {
            
        }
    }

    private static void ProbeThermalZone(SensorSnapshot chuanganqi10)
    {
        try
        {
            using var chaxunqi5 = Searcher(@"\\.\root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject duixiang4 in chaxunqi5.Get())
            {
                using (duixiang4)
                {
                    var yuan2 = Number(duixiang4["CurrentTemperature"], -1);
                    var thermalwendu = yuan2 / 10d - 273.15;
                    if (thermalwendu is > 0 and < 125)
                    {
                        chuanganqi10.chuliqiwenduzhi = thermalwendu;
                        return;
                    }
                }
            }
        }
        catch
        {
            
        }

        try
        {
            using var chaxunqi6 = Searcher(@"\\.\root\CIMV2", "SELECT CurrentReading FROM Win32_TemperatureProbe");
            foreach (ManagementObject duixiang5 in chaxunqi6.Get())
            {
                using (duixiang5)
                {
                    var yuan3 = Number(duixiang5["CurrentReading"], -1);
                    var tancewendu = yuan3 / 10d;
                    if (tancewendu is > 0 and < 125)
                    {
                        chuanganqi10.chuliqiwenduzhi = tancewendu;
                        return;
                    }
                }
            }
        }
        catch
        {
            
        }
    }

    private static ManagementObjectSearcher Searcher(string fanweilujing3, string chaxun2)
    {
        var fanwei2 = new ManagementScope(fanweilujing3);
        var chaxunqi7 = new ManagementObjectSearcher(fanwei2, new ObjectQuery(chaxun2));
        chaxunqi7.Options.Timeout = TimeSpan.FromMilliseconds(1400);
        chaxunqi7.Options.ReturnImmediately = true;
        chaxunqi7.Options.Rewindable = false;
        return chaxunqi7;
    }

    private static string CaptureHidden(string wenjianmingcheng, string canshu, int chaoshizhi)
    {
        try
        {
            using var jincheng = new Process();
            jincheng.StartInfo = new ProcessStartInfo(wenjianmingcheng, canshu)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            if (!jincheng.Start()) return "";
            if (!jincheng.WaitForExit(chaoshizhi))
            {
                try { jincheng.Kill(entireProcessTree: true); } catch { }
                return "";
            }
            return jincheng.StandardOutput.ReadToEnd();
        }
        catch
        {
            return "";
        }
    }

    private static string FindNvidiaSmiPath()
    {
        var houxuan = new List<string>
        {
            Path.Combine(Environment.SystemDirectory, "nvidia-smi.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe")
        };

        var lujing = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(lujing))
        {
            foreach (var bufen2 in lujing.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    houxuan.Add(Path.Combine(bufen2.Trim(), "nvidia-smi.exe"));
                }
                catch
                {
                    
                }
            }
        }

        foreach (var houxuan2 in houxuan)
        {
            try
            {
                if (File.Exists(houxuan2)) return houxuan2;
            }
            catch
            {
                
            }
        }

        return "nvidia-smi.exe";
    }

    private static bool Contains(string wenbenyuan, string sousuoci)
    {
        return wenbenyuan.Contains(sousuoci, StringComparison.OrdinalIgnoreCase);
    }

    private static string Text(object? duixiangwenben) => duixiangwenben?.ToString() ?? "";

    private static double Number(object? duixiangzhi, double beiyong2)
    {
        try
        {
            if (duixiangzhi is null) return beiyong2;
            if (duixiangzhi is string wenbenzhi) return Number(wenbenzhi, beiyong2);
            return Convert.ToDouble(duixiangzhi, CultureInfo.InvariantCulture);
        }
        catch
        {
            return beiyong2;
        }
    }

    private static double Number(string shuzhiwenben, double beiyong3)
    {
        var qinglihou = new string(shuzhiwenben.Where(zifu => char.IsDigit(zifu) || zifu is '-' or '+' or '.' or ',').ToArray()).Replace(',', '.');
        return double.TryParse(qinglihou, NumberStyles.Float, CultureInfo.InvariantCulture, out var jieguo) ? jieguo : beiyong3;
    }

    private static uint ReadUInt32(MemoryMappedViewAccessor shitu3, long weiyi)
    {
        if (weiyi < 0 || weiyi + sizeof(uint) > shitu3.Capacity) return 0;
        shitu3.Read(weiyi, out uint wufuhaozhi);
        return wufuhaozhi;
    }

    private static byte ReadByte(MemoryMappedViewAccessor shitu4, long weiyi2)
    {
        if (weiyi2 < 0 || weiyi2 >= shitu4.Capacity) return 0;
        shitu4.Read(weiyi2, out byte zijiezhi);
        return zijiezhi;
    }

    private static double ReadDouble(MemoryMappedViewAccessor shitu5, long weiyi3)
    {
        if (weiyi3 < 0 || weiyi3 + sizeof(double) > shitu5.Capacity) return 0;
        shitu5.Read(weiyi3, out double shuangjingduzhi);
        return shuangjingduzhi;
    }

    private static float ReadFloat(MemoryMappedViewAccessor shitu6, long weiyi4)
    {
        if (weiyi4 < 0 || weiyi4 + sizeof(float) > shitu6.Capacity) return 0;
        shitu6.Read(weiyi4, out float danjingduzhi);
        return danjingduzhi;
    }

    private static string ReadAnsi(MemoryMappedViewAccessor shitu7, long weiyi5, int zuidachangdu)
    {
        if (weiyi5 < 0 || weiyi5 >= shitu7.Capacity || zuidachangdu <= 0) return "";
        var shuliang = (int)Math.Min(zuidachangdu, shitu7.Capacity - weiyi5);
        var zijie = new byte[shuliang];
        shitu7.ReadArray(weiyi5, zijie, 0, shuliang);
        var changdu = Array.IndexOf(zijie, (byte)0);
        if (changdu < 0) changdu = zijie.Length;
        return Encoding.Default.GetString(zijie, 0, changdu).Trim();
    }
}
