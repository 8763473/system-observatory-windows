using System.Runtime.InteropServices;

namespace HwMonitor.Hardware;

internal static class NativeMethods
{
    internal const uint jinchengkuaizhaobiaozhi = 0x00000002;
    internal static readonly IntPtr wuxiaojubingzhi = new(-1);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool GetSystemTimes(out FileTime kongxianshijian2, out FileTime neiheshijian2, out FileTime yonghushijian2);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx huanchong);

    [DllImport("kernel32.dll")]
    internal static extern ulong GetTickCount64();

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr OpenProcess(uint fangwenquanxian, bool jichengjubing, uint jinchengbianhao);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr jubing);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool K32EmptyWorkingSet(IntPtr jinchengjubing);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr CreateToolhelp32Snapshot(uint biaozhi, uint jinchengbianhao);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool Process32First(IntPtr kuaizhaojubing, ref ProcessEntry32 tiaomu);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool Process32Next(IntPtr kuaizhaojubing, ref ProcessEntry32 tiaomu);

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    internal static extern bool EnumDisplayDevices(string? zhishebei, uint suoyinzhizhi, ref DisplayDevice zhixianshishebei, uint biaozhi);

    [StructLayout(LayoutKind.Sequential)]
    internal struct FileTime
    {
        public uint dizhishijian;
        public uint gaozhishijian;

        public ulong ToUInt64() => ((ulong)gaozhishijian << 32) | dizhishijian;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryStatusEx
    {
        public uint changdu2;
        public uint neicunzhi;
        public ulong zongliangzhi;
        public ulong keyongzhi;
        public ulong zongliangyemianwenjian;
        public ulong keyongyemianwenjian;
        public ulong zongliangzhi2;
        public ulong keyongzhi2;
        public ulong keyongzhizhi;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ProcessEntry32
    {
        public uint daxiao;
        public uint shiyongcishu;
        public uint jinchengbianhao;
        public IntPtr morenzhi;
        public uint mokuaishiyongcishu;
        public uint xianchengshuliang;
        public uint fujinchengbianhao;
        public int jichuyouxianji;
        public uint yunxingshijianmiao;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string zhixingwenjian;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct DisplayDevice
    {
        public int jiegoudaxiao;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string shebeimingcheng;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string shebeizhi;

        public int zhuangtaizhi;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string shebeibianhao;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string shebeijian;
    }
}
