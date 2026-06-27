namespace HwMonitor.Models;

public sealed class CpuInfo
{
    public string xinghao2 { get; set; } = "Unknown CPU";
    public int hexin2 { get; set; }
    public int xiancheng { get; set; }
    public double shiyonglvbaifenbi { get; set; }
    public double zhipinlv { get; set; }
    public double wenduzhi { get; set; } = -1;
    public double gonghaowazhi { get; set; } = -1;
    public bool gonghaogusuan { get; set; } = true;
}

public sealed class MemInfo
{
    public ulong zongliangzhi3 { get; set; }
    public ulong yiyongzhi { get; set; }
    public ulong shengyuzhi { get; set; }
    public ulong keyongzhi3 { get; set; }
    public double shiyonglvbaifenbi2 { get; set; }
}

public sealed class DiskInfo
{
    public string cipanmingcheng { get; set; } = "";
    public string shebei2 { get; set; } = "";
    public string zhileixing { get; set; } = "";
    public ulong zongliangzhi4 { get; set; }
    public ulong yiyongzhi2 { get; set; }
    public ulong shengyuzhi2 { get; set; }
    public double shiyonglvbaifenbi3 { get; set; }
}

public sealed class GpuInfo
{
    public string xinghao3 { get; set; } = "";
    public double wenduzhi2 { get; set; } = -1;
    public double shiyonglvbaifenbi4 { get; set; }
    public double hexinpinlv { get; set; }
    public double neicunpinlv { get; set; }
    public double fengshanbaifenbi { get; set; } = -1;
    public ulong zhizongliangzhi { get; set; }
    public ulong zhiyiyongzhi { get; set; }
    public double gonghaowazhi { get; set; } = -1;
    public bool gonghaogusuan { get; set; } = true;
}

public sealed class FanInfo
{
    public string mingcheng9 { get; set; } = "Fan";
    public string leixing2 { get; set; } = "other";
    public double fengshanzhuansu { get; set; } = -1;
    public double baifenbi2 { get; set; } = -1;
}

public sealed class NetInfo
{
    public string mingcheng10 { get; set; } = "";
    public ulong xiazaizijie2 { get; set; }
    public ulong shangchuanzijie2 { get; set; }
    public ulong xiazaisudu2 { get; set; }
    public ulong shangchuansudu2 { get; set; }
}

public sealed class TokenDailyInfo
{
    public string riqi { get; set; } = "";
    public int shurutokens { get; set; }
    public int shuchutokens { get; set; }
    public int zongtokens { get; set; }
    public double zuichangrenwumiao { get; set; }
}

public sealed class TokenMonitorInfo
{
    public bool peizhiwanzheng { get; set; }
    public bool yunxing { get; set; }
    public bool houtaicaijiqiyunxing { get; set; }
    public string zhuangtai { get; set; } = "";
    public string tongjiriqi { get; set; } = "";
    public string moxingmingcheng { get; set; } = "";
    public int shurutokens { get; set; }
    public int shuchutokens { get; set; }
    public int jinrishurutokens { get; set; }
    public int jinrishuchutokens { get; set; }
    public int jinritokens { get; set; }
    public int leijitokens { get; set; }
    public int fengzhitokens { get; set; }
    public int dangqianlianxutianshu { get; set; }
    public int zuichanglianxutianshu { get; set; }
    public double zhunquesudu { get; set; }
    public double shishitokensudu { get; set; }
    public double zonghaoshi { get; set; }
    public double shouziziyanmiao { get; set; }
    public bool shouziziyangusuan { get; set; }
    public DateTime? gengxinshijian { get; set; }
    public List<TokenDailyInfo> huodongriqi { get; } = [];
}

public sealed class SystemSnapshot
{
    public string diannaomingcheng { get; set; } = "";
    public string zhimingcheng { get; set; } = "Windows";
    public string neihe { get; set; } = "";
    public long yunxingshijianmiao { get; set; }
    public double zonggonghaowazhi { get; set; } = -1;
    public bool zonggonghaogusuan { get; set; } = true;
    public CpuInfo chuliqi3 { get; set; } = new();
    public MemInfo neicun { get; set; } = new();
    public List<DiskInfo> cipan2 { get; } = [];
    public List<GpuInfo> xianqia7 { get; } = [];
    public List<FanInfo> fengshan3 { get; } = [];
    public List<NetInfo> wangluo { get; } = [];
    public TokenMonitorInfo? tokenjiankong { get; set; }
}
