namespace HwMonitor.UI;

internal enum AppPage
{
    Monitor,
    MemoryTools,
    TokenMonitor
}

internal enum WindowButton
{
    None = -1,
    Settings = 0,
    MonitorHome = 1,
    MemoryTools = 2,
    OpenClawTokenMonitor = 3,
    Fullscreen = 4,
    Minimize = 5,
    Close = 6
}

internal enum SettingsAction
{
    None,
    Close,
    ToggleLanguage,
    PickBackground,
    ClearBackground,
    ToggleAutostart,
    ToggleCloseToTray,
    ToggleRelaySync,
    ConfigureRelaySync
}

internal enum MemoryToolsAction
{
    None,
    CleanMemory,
    ConfigureTokenCollector,
    ConfigureAndroidLink,
    CopyAndroidDeviceKey,
    CopyAndroidLinkUrl
}

internal enum TokenMonitorAction
{
    None,
    StartOrRefresh
}

internal enum TokenActivityMode
{
    Daily,
    Weekly
}

internal enum MonitorCard
{
    None,
    Cpu,
    Gpu,
    Memory,
    Fans,
    Disks,
    Network,
    MemoryTools,
    TokenMonitor,
    TokenCollector,
    AndroidLink
}

internal readonly record struct AnimationSnapshot(
    double dakaijieduan,
    double shezhijieduan,
    double yemianjieduan,
    double neirongjieduan,
    double shuaxinjieduan,
    double tishijieduan,
    double gongzuobomai,
    double gundongpianyi,
    double yemianpianyi,
    int yemianfangxiang,
    AppPage dangqianyemian,
    AppPage laiyuanyemian,
    AppPage mubiaoyemian,
    bool yemianqiehuanjihuo)
{
    public static AnimationSnapshot Static { get; } = new(
        dakaijieduan: 1,
        shezhijieduan: 0,
        yemianjieduan: 1,
        neirongjieduan: 1,
        shuaxinjieduan: 0,
        tishijieduan: 0,
        gongzuobomai: 0,
        gundongpianyi: 0,
        yemianpianyi: 0,
        yemianfangxiang: 0,
        dangqianyemian: AppPage.Monitor,
        laiyuanyemian: AppPage.Monitor,
        mubiaoyemian: AppPage.Monitor,
        yemianqiehuanjihuo: false);
}

internal static class UiAnimation
{
    public static double Clamp01(double dangqianzhi)
    {
        return Math.Clamp(dangqianzhi, 0d, 1d);
    }

    public static double EaseOutCubic(double jieduan)
    {
        jieduan = Clamp01(jieduan);
        var fanxiang = 1d - jieduan;
        return 1d - fanxiang * fanxiang * fanxiang;
    }

    public static double EaseInOutCubic(double jieduan)
    {
        jieduan = Clamp01(jieduan);
        return jieduan < 0.5d
            ? 4d * jieduan * jieduan * jieduan
            : 1d - Math.Pow(-2d * jieduan + 2d, 3d) / 2d;
    }

    public static double EaseOutBack(double jieduan)
    {
        jieduan = Clamp01(jieduan);
        const double qiangdu = 1.58d;
        var fanxiang = jieduan - 1d;
        return 1d + (qiangdu + 1d) * fanxiang * fanxiang * fanxiang + qiangdu * fanxiang * fanxiang;
    }

    public static double Lerp(double kaishi, double jieshu, double jieduan)
    {
        return kaishi + (jieshu - kaishi) * Clamp01(jieduan);
    }

    public static double Stagger(double jieduan, int suoyin, double jiange = 0.07d)
    {
        jieduan = Clamp01(jieduan);
        var kaishi = Math.Max(0, suoyin) * Math.Max(0, jiange);
        if (kaishi >= 0.92d) return jieduan >= 1d ? 1d : 0d;
        return EaseOutCubic((jieduan - kaishi) / (1d - kaishi));
    }

    public static double SequentialFadeOut(double jieduan)
    {
        return 1d - EaseOutCubic(Clamp01(jieduan) * 2d);
    }

    public static double SequentialFadeIn(double jieduan)
    {
        return EaseOutCubic((Clamp01(jieduan) - 0.5d) * 2d);
    }

    public static int yemianfangxiang(AppPage laiyuan, AppPage mubiao)
    {
        if (laiyuan == mubiao) return 0;
        return mubiao > laiyuan ? 1 : -1;
    }
}
