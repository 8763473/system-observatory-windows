using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using HwMonitor.Hardware;
using HwMonitor.Models;
using HwMonitor.Settings;
using HwMonitor.Utilities;

namespace HwMonitor.UI;

internal sealed class MonitorRenderer : IDisposable
{
    public const int zhigaodu = 84;
    private const int zhikuandu = 78;
    private const int zhuchuangkoubanjing = 20;
    private const int zhubiaomianbanjing = zhuchuangkoubanjing;
    private const int tokenhuodonglieshu = 53;
    private const int tokenhuodonghangshu = 7;
    private const int tokenhuodongfangkuaishu = 371;
    private const int tokenhuodongbiaoqianshu = 2;
    private static readonly int[] tokenmeirireduyuzhi = { 1, 2_000_000, 4_000_000, 6_000_000, 8_000_000, 10_000_000 };
    private static readonly int[] tokenmeizhoureduyuzhi = { 1, 10_000_000, 20_000_000, 30_000_000, 40_000_000, 50_000_000 };
    private static readonly Color[] tokenreduyanse =
    {
        Color.FromArgb(72, 240, 242, 247),
        Color.FromArgb(166, 225, 237, 252),
        Color.FromArgb(190, 171, 203, 255),
        Color.FromArgb(210, 121, 181, 255),
        Color.FromArgb(225, 81, 159, 255),
        Color.FromArgb(235, 48, 122, 248),
        Color.FromArgb(240, 30, 86, 224)
    };

    private readonly Font _biaotiziti = new("Microsoft YaHei UI", 18f, FontStyle.Bold);
    private readonly Font _zhiziti = new("Microsoft YaHei UI", 12f, FontStyle.Bold);
    private readonly Font _zhiziti2 = new("Microsoft YaHei UI", 10f, FontStyle.Regular);
    private readonly Font _zhiziti3 = new("Microsoft YaHei UI", 9f, FontStyle.Regular);
    private readonly Font _zhiziti4 = new("Segoe UI", 10f, FontStyle.Bold);
    private readonly Font _anniuziti = new("Microsoft YaHei UI", 9f, FontStyle.Regular);

    private static readonly StringFormat _geshijin = new(StringFormatFlags.NoWrap) { Trimming = StringTrimming.EllipsisCharacter, Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
    private static readonly StringFormat _geshizhong = new(StringFormatFlags.NoWrap) { Trimming = StringTrimming.EllipsisCharacter, Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
    private static readonly StringFormat _geshiyuan = new(StringFormatFlags.NoWrap) { Trimming = StringTrimming.EllipsisCharacter, Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
    private static readonly Dictionary<float, Font> _yibiaopanziti = new();
    private static Dictionary<WindowButton, Rectangle>? _huancunanniukuang;
    private static Size _huancunanniuchicun;
    private DateTime _shangcihuizhishi = DateTime.UtcNow;
    private double _bencihuizhijiange = 16d;

    private readonly Dictionary<string, double> _huandongzhi = new(StringComparer.OrdinalIgnoreCase);

    private WindowButton _xuanfuanniu2 = WindowButton.None;
    private WindowButton _anxiaanniu2 = WindowButton.None;
    private MonitorCard _xuanfukapian2 = MonitorCard.None;
    private TokenActivityMode _tokenhuodongmoshi2 = TokenActivityMode.Daily;
    private string _tokenhuodongxuanfuriqi2 = "";
    private EmbeddedServerState _neiqianfuwuqizhuangtai = EmbeddedServerState.Stopped;
    private string _neiqianfuwuqitishi = "";
    private string _suidaoURL = "";
    private DeepSeekBalanceInfo _deepseekyuexinxi = new();

    private static readonly Color wenben2 = Color.FromArgb(26, 28, 35);
    private static readonly Color zhiwenben = Color.FromArgb(90, 95, 105);
    private static readonly Color zhiwenben2 = Color.FromArgb(140, 145, 155);
    private static readonly Color hang3 = Color.FromArgb(232, 235, 245);
    private static readonly Color fenhongyanse = Color.FromArgb(63, 81, 181);
    private static readonly Color lanse = Color.FromArgb(33, 150, 243);
    private static readonly Color zise = Color.FromArgb(156, 39, 176);
    private static readonly Color bohelvse = Color.FromArgb(0, 150, 136);
    private static readonly Color roufenbeijing = Color.FromArgb(248, 250, 252);
    private static readonly Color roufenbiaomian = Color.FromArgb(255, 255, 255);
    private static readonly Color roufencelan = Color.FromArgb(244, 247, 252);
    private static readonly Color roufenxuanze = Color.FromArgb(232, 234, 246);
    private static readonly Color roufenhover = Color.FromArgb(243, 245, 250);
    private static readonly Color roufenkapian = Color.FromArgb(240, 255, 255, 255);
    private static readonly Color roufenkapianbiankuang = Color.FromArgb(224, 228, 240);
    private static readonly Color tianlanbeijing = Color.FromArgb(237, 246, 255);
    private static readonly Color bohelvbeijing = Color.FromArgb(236, 249, 247);
    private static readonly Color dianzibeijing = Color.FromArgb(244, 240, 252);
    private static readonly Color roufenanniu = Color.FromArgb(232, 234, 246);
    private static readonly Color roufenbiankuang = Color.FromArgb(159, 168, 218);
    private static readonly Color meiguifenwenben = Color.FromArgb(48, 63, 159);
    private static readonly Color roufenjindutiao = Color.FromArgb(197, 202, 233);

    public void SetButtonState(WindowButton xuanfu2, WindowButton anxia)
    {
        _xuanfuanniu2 = xuanfu2;
        _anxiaanniu2 = anxia;
    }

    public void SetHoverCard(MonitorCard kapian)
    {
        _xuanfukapian2 = kapian;
    }

    public void SetTokenActivityState(TokenActivityMode huodongmoshi, string xuanfuriqi)
    {
        _tokenhuodongmoshi2 = huodongmoshi;
        _tokenhuodongxuanfuriqi2 = xuanfuriqi ?? "";
    }

    public void SetEmbeddedServerState(EmbeddedServerState zhuangtai, string tishi, string suidaoURL = "")
    {
        _neiqianfuwuqizhuangtai = zhuangtai;
        _neiqianfuwuqitishi = tishi ?? "";
        _suidaoURL = suidaoURL ?? "";
    }

    public void SetDeepSeekBalance(DeepSeekBalanceInfo xinxi)
    {
        _deepseekyuexinxi = xinxi ?? new DeepSeekBalanceInfo();
    }

    public int ContentHeight(Rectangle bianjie, SystemSnapshot? kuaizhao8, AppPage yemian)
    {
        if (yemian == AppPage.MemoryTools)
        {
            return AndroidLinkCardRect(bianjie).Bottom + 28;
        }
        if (yemian == AppPage.TokenMonitor)
        {
            return TokenMonitorStatusRect(bianjie).Bottom + 28;
        }

        var neirongkuandu = Math.Max(1, bianjie.Width - zhikuandu);
        var kuanpingbuju = neirongkuandu >= 940;
        var cipanshuliang = Math.Max(1, kuaizhao8?.cipan2.Count ?? 4);
        var cipanlieshu = neirongkuandu >= 780 ? 2 : 1;
        var cipanhangshu = (int)Math.Ceiling(cipanshuliang / (double)cipanlieshu);
        var cipangaodu = 72 + cipanhangshu * 92 + 18;

        if (kuanpingbuju)
        {
            return zhigaodu + 20 + 318 + 16 + 226 + 16 + cipangaodu + 16 + 260 + 28;
        }

        return zhigaodu + 20 + 302 + 14 + 330 + 14 + 226 + 14 + 226 + 14 + cipangaodu + 14 + 260 + 28;
    }

    public WindowButton HitButton(Rectangle bianjie2, Point zuobiao3)
    {
        foreach (var anniuxiang in ButtonRects(bianjie2))
        {
            if (anniuxiang.Value.Contains(zuobiao3)) return anniuxiang.Key;
        }
        return WindowButton.None;
    }

    public MemoryToolsAction HitMemoryTools(Rectangle bianjie3, int gundongzong, Point zuobiao4)
    {
        var zuobiao5 = ScrolledPoint(zuobiao4, gundongzong);
        if (MemoryCleanupActionRect(bianjie3).Contains(zuobiao5)) return MemoryToolsAction.CleanMemory;
        if (TokenCollectorConfigureActionRect(bianjie3).Contains(zuobiao5)) return MemoryToolsAction.ConfigureTokenCollector;
        if (AndroidDeviceKeyValueRect(bianjie3).Contains(zuobiao5)) return MemoryToolsAction.CopyAndroidDeviceKey;
        if (AndroidLinkUrlValueRect(bianjie3).Contains(zuobiao5)) return MemoryToolsAction.CopyAndroidLinkUrl;
        if (AndroidLinkActionRect(bianjie3).Contains(zuobiao5)) return MemoryToolsAction.ConfigureAndroidLink;
        return MemoryToolsAction.None;
    }

    public TokenMonitorAction HitTokenMonitor(Rectangle bianjie3, int gundongzong, Point zuobiao4)
    {
        var zuobiao5 = ScrolledPoint(zuobiao4, gundongzong);
        return TokenMonitorActionRect(bianjie3).Contains(zuobiao5)
            ? TokenMonitorAction.StartOrRefresh
            : TokenMonitorAction.None;
    }

    public TokenActivityMode? HitTokenActivityMode(Rectangle bianjie3, int gundongzong, Point zuobiao4)
    {
        var zuobiao5 = ScrolledPoint(zuobiao4, gundongzong);
        var quyu = TokenActivityRect(bianjie3);
        foreach (var moshi in new[] { TokenActivityMode.Daily, TokenActivityMode.Weekly })
        {
            if (TokenActivityTabRect(quyu, moshi).Contains(zuobiao5)) return moshi;
        }

        return null;
    }

    public string HitTokenActivityDay(Rectangle bianjie3, int gundongzong, TokenMonitorSnapshot tokenkuaizhao, Point zuobiao4, TokenActivityMode huodongmoshi)
    {
        var zuobiao5 = ScrolledPoint(zuobiao4, gundongzong);
        var quyu = TokenActivityRect(bianjie3);
        var jihe = TokenHeatmapGeometry(quyu);
        if (!jihe.wanggequyu.Contains(zuobiao5)) return "";

        var (kaishi, _) = TokenActivityDateRange(huodongmoshi);
        for (var suoyin = 0; suoyin < tokenhuodongfangkuaishu; suoyin++)
        {
            var riqi = kaishi.AddDays(suoyin);
            var fangkuai = TokenHeatmapCellRect(jihe, suoyin, huodongmoshi);
            if (!fangkuai.Contains(zuobiao5)) continue;
            return TokenDateKey(riqi);
        }

        return "";
    }

    public MonitorCard HitMonitorCard(Rectangle bianjie3, SystemSnapshot? kuaizhao8, AppPage yemian, int gundongzong, Point zuobiao4)
    {
        if (yemian == AppPage.MemoryTools)
        {
            var zuobiao5 = ScrolledPoint(zuobiao4, gundongzong);
            if (MemoryToolsCardRect(bianjie3).Contains(zuobiao5)) return MonitorCard.MemoryTools;
            if (TokenCollectorConfigRect(bianjie3).Contains(zuobiao5)) return MonitorCard.TokenCollector;
            if (AndroidLinkCardRect(bianjie3).Contains(zuobiao5)) return MonitorCard.AndroidLink;
            return MonitorCard.None;
        }
        if (yemian == AppPage.TokenMonitor)
        {
            return TokenMonitorStatusRect(bianjie3).Contains(ScrolledPoint(zuobiao4, gundongzong)) ? MonitorCard.TokenMonitor : MonitorCard.None;
        }

        if (kuaizhao8 is null) return MonitorCard.None;
        foreach (var kapian in MonitorCardRects(bianjie3, kuaizhao8, gundongzong))
        {
            if (kapian.Value.Contains(zuobiao4)) return kapian.Key;
        }

        return MonitorCard.None;
    }

    public Rectangle MemoryCleanupActionRect(Rectangle bianjie3)
    {
        var gongjukapian = MemoryToolsCardRect(bianjie3);
        return new Rectangle(gongjukapian.Left + 18, gongjukapian.Bottom - 62, Math.Min(156, gongjukapian.Width - 36), 40);
    }

    public Rectangle TokenCollectorConfigureActionRect(Rectangle bianjie3)
    {
        var peizhikapian = TokenCollectorConfigRect(bianjie3);
        return new Rectangle(peizhikapian.Left + 18, peizhikapian.Bottom - 62, Math.Min(196, peizhikapian.Width - 36), 40);
    }

    public Rectangle AndroidLinkActionRect(Rectangle bianjie3)
    {
        var anniuqu = AndroidLinkButtonRowRect(bianjie3);
        return anniuqu;
    }

    public Rectangle AndroidDeviceKeyValueRect(Rectangle bianjie3)
    {
        var lianjiekapian = AndroidLinkCardRect(bianjie3);
        var lianjieliekuan = Math.Max(1, (lianjiekapian.Width - 54) / 3);
        var lianjie1 = new Rectangle(lianjiekapian.Left + 18, lianjiekapian.Top + 104, lianjieliekuan, 54);
        var lianjie2 = new Rectangle(lianjie1.Right + 9, lianjie1.Top, lianjieliekuan, 54);
        return new Rectangle(lianjie2.Right + 9, lianjie1.Top, Math.Max(1, lianjiekapian.Right - 18 - lianjie2.Right - 9), 54);
    }

    public Rectangle AndroidLinkUrlValueRect(Rectangle bianjie3)
    {
        var lianjiekapian = AndroidLinkCardRect(bianjie3);
        var lianjieliekuan = Math.Max(1, (lianjiekapian.Width - 54) / 3);
        var lianjie1 = new Rectangle(lianjiekapian.Left + 18, lianjiekapian.Top + 104, lianjieliekuan, 54);
        return new Rectangle(lianjie1.Right + 9, lianjie1.Top, lianjieliekuan, 54);
    }

    public Rectangle TokenMonitorActionRect(Rectangle bianjie3)
    {
        var gongjukapian = TokenMonitorStatusRect(bianjie3);
        return new Rectangle(gongjukapian.Left + 18, gongjukapian.Bottom - 62, Math.Min(188, gongjukapian.Width - 36), 40);
    }

    public SettingsAction HitSettings(Rectangle bianjie3, Point zuobiao4, bool shezhidakai)
    {
        if (!shezhidakai) return SettingsAction.None;
        var mianban = SettingsPanelRect(bianjie3);
        if (!mianban.Contains(zuobiao4)) return SettingsAction.Close;

        var yuyan2 = SettingsLanguageRect(mianban);
        var xuanze = SettingsPickBackgroundRect(mianban);
        var qingchu = SettingsClearBackgroundRect(mianban);
        var ziqidong = AutostartToggleRect(mianban);
        var tuopan = CloseToTrayToggleRect(mianban);
        var relay = RelaySyncToggleRect(mianban);
        var relaypeizhi = RelaySyncConfigRect(mianban);

        if (yuyan2.Contains(zuobiao4)) return SettingsAction.ToggleLanguage;
        if (xuanze.Contains(zuobiao4)) return SettingsAction.PickBackground;
        if (qingchu.Contains(zuobiao4)) return SettingsAction.ClearBackground;
        if (ziqidong.Contains(zuobiao4)) return SettingsAction.ToggleAutostart;
        if (tuopan.Contains(zuobiao4)) return SettingsAction.ToggleCloseToTray;
        if (relay.Contains(zuobiao4)) return SettingsAction.ToggleRelaySync;
        if (relaypeizhi.Contains(zuobiao4)) return SettingsAction.ConfigureRelaySync;
        return SettingsAction.None;
    }

    public Rectangle SettingsPanelRect(Rectangle bianjie4)
    {
        var kuandu = Math.Min(420, Math.Max(330, bianjie4.Width - 48));
        const int gaodu = 526;
        var heng = bianjie4.Left + (bianjie4.Width - kuandu) / 2;
        var zong2 = bianjie4.Top + (bianjie4.Height - gaodu) / 2;
        if (zong2 < zhigaodu + 18) zong2 = zhigaodu + 18;
        return new Rectangle(heng, zong2, kuandu, gaodu);
    }

    private static Rectangle SettingsLanguageRect(Rectangle mianban)
    {
        return new Rectangle(mianban.Left + 24, mianban.Top + 82, mianban.Width - 48, 40);
    }

    private static Rectangle SettingsPickBackgroundRect(Rectangle mianban)
    {
        return new Rectangle(mianban.Left + 24, mianban.Top + 198, 132, 40);
    }

    private static Rectangle SettingsClearBackgroundRect(Rectangle mianban)
    {
        return new Rectangle(mianban.Left + 166, mianban.Top + 198, mianban.Width - 190, 40);
    }

    private static Rectangle AutostartToggleRect(Rectangle mianban)
    {
        return new Rectangle(mianban.Left + 24, mianban.Top + 274, mianban.Width - 48, 42);
    }

    private static Rectangle CloseToTrayToggleRect(Rectangle mianban)
    {
        return new Rectangle(mianban.Left + 24, mianban.Top + 326, mianban.Width - 48, 42);
    }

    private static Rectangle RelaySyncToggleRect(Rectangle mianban)
    {
        return new Rectangle(mianban.Left + 24, mianban.Top + 404, mianban.Width - 48, 42);
    }

    private static Rectangle RelaySyncConfigRect(Rectangle mianban)
    {
        return new Rectangle(mianban.Left + 24, mianban.Top + 454, mianban.Width - 48, 38);
    }

    public void Draw(Graphics huitu2, Rectangle bianjie5, SystemSnapshot? kuaizhao9, TokenMonitorSnapshot tokenkuaizhao, bool jiuxu, AppSettings shezhi2, bool shezhidakai2, bool quanping, bool neicunqinglizhong, string neicunqinglitishi, string neicunqinglijieguo, AppPage yemian, AnimationSnapshot donghua)
    {
        var huizhishi = DateTime.UtcNow;
        _bencihuizhijiange = Math.Clamp((huizhishi - _shangcihuizhishi).TotalMilliseconds, 1, 120);
        _shangcihuizhishi = huizhishi;

        huitu2.SmoothingMode = SmoothingMode.AntiAlias;
        huitu2.InterpolationMode = InterpolationMode.HighQualityBicubic;
        huitu2.PixelOffsetMode = PixelOffsetMode.HighQuality;
        huitu2.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        DrawBackground(huitu2, bianjie5, shezhi2);
        DrawMainSurface(huitu2, bianjie5, shezhi2);

        var kaiqizhuangtai = huitu2.Save();
        ApplyOpenTransform(huitu2, bianjie5, donghua.dakaijieduan);

        if (yemian == AppPage.Monitor && (!jiuxu || kuaizhao9 is null))
        {
            DrawHeader(huitu2, bianjie5, null, shezhi2, jiuxu2: false, shezhidakai2, quanping, yemian);
            DrawLoading(huitu2, bianjie5, shezhi2);
            DrawSidebar(huitu2, bianjie5, shezhi2, shezhidakai2, neicunqinglizhong, yemian, donghua);
            if (!string.IsNullOrWhiteSpace(neicunqinglitishi)) DrawToast(huitu2, bianjie5, neicunqinglitishi, donghua.tishijieduan);
            if (shezhidakai2 || donghua.shezhijieduan > 0.01) DrawSettingsPanel(huitu2, bianjie5, shezhi2, donghua.shezhijieduan);
            huitu2.Restore(kaiqizhuangtai);
            DrawOpeningWash(huitu2, bianjie5, donghua.dakaijieduan);
            return;
        }

        var neironggaodu = ContentHeight(bianjie5, kuaizhao9, yemian);
        var zuidagundong = Math.Max(0, neironggaodu - bianjie5.Height);
        var gundongzong = Math.Clamp((int)Math.Round(donghua.gundongpianyi), 0, zuidagundong);

        var neirongzhi = MainSurfaceRect(bianjie5);
        var zhuangtai2 = huitu2.Save();
        using (var biaomianlujing = MainSurfacePath(neirongzhi))
        {
            huitu2.SetClip(biaomianlujing);
        }
        if (donghua.yemianqiehuanjihuo)
        {
            DrawSequentialPageTransition(huitu2, bianjie5, kuaizhao9, tokenkuaizhao, shezhi2, neicunqinglizhong, neicunqinglijieguo, gundongzong, donghua);
        }
        else
        {
            DrawPageLayer(huitu2, bianjie5, kuaizhao9, tokenkuaizhao, shezhi2, neicunqinglizhong, neicunqinglijieguo, yemian, gundongzong, 0, donghua, mubiaoye: true);
        }
        huitu2.Restore(zhuangtai2);

        DrawHeader(huitu2, bianjie5, kuaizhao9, shezhi2, jiuxu2: jiuxu, shezhidakai2, quanping, yemian);
        DrawSidebar(huitu2, bianjie5, shezhi2, shezhidakai2, neicunqinglizhong, yemian, donghua);
        if (!string.IsNullOrWhiteSpace(neicunqinglitishi)) DrawToast(huitu2, bianjie5, neicunqinglitishi, donghua.tishijieduan);
        if (shezhidakai2 || donghua.shezhijieduan > 0.01) DrawSettingsPanel(huitu2, bianjie5, shezhi2, donghua.shezhijieduan);

        huitu2.Restore(kaiqizhuangtai);
        DrawOpeningWash(huitu2, bianjie5, donghua.dakaijieduan);
    }

    private void DrawPageLayer(Graphics huitu3, Rectangle bianjie6, SystemSnapshot? kuaizhao10, TokenMonitorSnapshot tokenkuaizhao, AppSettings shezhi3, bool neicunqinglizhong, string neicunqinglijieguo, AppPage yemian, int gundongzong2, double pianyi, AnimationSnapshot donghua, bool mubiaoye)
    {
        var zhuangtai = huitu3.Save();
        huitu3.TranslateTransform((float)pianyi, 0f);
        var donghua2 = mubiaoye ? donghua : donghua with { neirongjieduan = 1, shuaxinjieduan = 0 };
        if (yemian == AppPage.MemoryTools)
        {
            ApplyToolPageScroll(huitu3, gundongzong2);
            DrawMemoryTools(huitu3, bianjie6, kuaizhao10, tokenkuaizhao, shezhi3, neicunqinglizhong, neicunqinglijieguo, donghua2);
        }
        else if (yemian == AppPage.TokenMonitor)
        {
            ApplyToolPageScroll(huitu3, gundongzong2);
            DrawTokenMonitor(huitu3, bianjie6, kuaizhao10, tokenkuaizhao, shezhi3, donghua2);
        }
        else if (kuaizhao10 is not null)
        {
            DrawContent(huitu3, bianjie6, kuaizhao10, gundongzong2, shezhi3, donghua2);
        }
        else
        {
            DrawLoading(huitu3, bianjie6, shezhi3);
        }
        huitu3.Restore(zhuangtai);
    }

    private void DrawSequentialPageTransition(Graphics huitu3, Rectangle bianjie6, SystemSnapshot? kuaizhao10, TokenMonitorSnapshot tokenkuaizhao, AppSettings shezhi3, bool neicunqinglizhong, string neicunqinglijieguo, int gundongzong2, AnimationSnapshot donghua)
    {
        var xinruchang = UiAnimation.SequentialFadeIn(donghua.yemianjieduan);
        if (xinruchang <= 0.001d)
        {
            var jiuyedonghua = donghua with { neirongjieduan = 1d, shuaxinjieduan = 0d, yemianpianyi = 0d };
            DrawPageLayer(huitu3, bianjie6, kuaizhao10, tokenkuaizhao, shezhi3, neicunqinglizhong, neicunqinglijieguo, donghua.laiyuanyemian, gundongzong2, 0, jiuyedonghua, mubiaoye: false);
            DrawPageFadeWash(huitu3, bianjie6, UiAnimation.SequentialFadeOut(donghua.yemianjieduan));
            return;
        }

        var xinyedonghua = donghua with { neirongjieduan = xinruchang, shuaxinjieduan = 0d, yemianpianyi = 0d };
        DrawPageLayer(huitu3, bianjie6, kuaizhao10, tokenkuaizhao, shezhi3, neicunqinglizhong, neicunqinglijieguo, donghua.mubiaoyemian, gundongzong2, 0, xinyedonghua, mubiaoye: true);
        DrawPageFadeWash(huitu3, bianjie6, xinruchang);
    }

    private static void DrawPageFadeWash(Graphics huitu3, Rectangle bianjie6, double kejianjieduan)
    {
        var fugai = 1d - UiAnimation.Clamp01(kejianjieduan);
        if (fugai <= 0.001d) return;

        var quyu = MainSurfaceRect(bianjie6);
        using var lujing = MainSurfacePath(quyu);
        using var huabi = new SolidBrush(Color.FromArgb((int)Math.Round(255d * fugai), Color.White));
        huitu3.FillPath(huabi, lujing);
    }

    private static void ApplyToolPageScroll(Graphics huitu3, int gundongzong2)
    {
        if (gundongzong2 > 0) huitu3.TranslateTransform(0f, -gundongzong2);
    }

    private void DrawContent(Graphics huitu3, Rectangle bianjie6, SystemSnapshot kuaizhao10, int gundongzong2, AppSettings shezhi3, AnimationSnapshot donghua)
    {
        var zuo3 = zhikuandu + 24;
        var youzhi = 28;
        var kapianjiange = 16;
        var zong3 = zhigaodu + 20 - gundongzong2;
        var neirongkuandu2 = bianjie6.Width - zuo3 - youzhi;
        var kuanpingbuju2 = neirongkuandu2 >= 940;

        if (kuanpingbuju2)
        {
            var kapiankuandu = (neirongkuandu2 - kapianjiange) / 2;
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, kapiankuandu, 318), 0, MonitorCard.Cpu, donghua, (huitu, quyu) => DrawCpu(huitu, quyu, kuaizhao10, shezhi3));
            DrawAnimatedCard(huitu3, new Rectangle(zuo3 + kapiankuandu + kapianjiange, zong3, kapiankuandu, 318), 1, MonitorCard.Gpu, donghua, (huitu, quyu) => DrawGpu(huitu, quyu, kuaizhao10, shezhi3));
            zong3 += 334;
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, kapiankuandu, 226), 2, MonitorCard.Memory, donghua, (huitu, quyu) => DrawMemory(huitu, quyu, kuaizhao10, shezhi3));
            DrawAnimatedCard(huitu3, new Rectangle(zuo3 + kapiankuandu + kapianjiange, zong3, kapiankuandu, 226), 3, MonitorCard.Fans, donghua, (huitu, quyu) => DrawFans(huitu, quyu, kuaizhao10, shezhi3));
            zong3 += 242;
            var cipangaodu2 = DiskCardHeight(neirongkuandu2, kuaizhao10.cipan2.Count);
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, neirongkuandu2, cipangaodu2), 4, MonitorCard.Disks, donghua, (huitu, quyu) => DrawDisks(huitu, quyu, kuaizhao10, shezhi3));
            zong3 += cipangaodu2 + 16;
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, neirongkuandu2, 260), 5, MonitorCard.Network, donghua, (huitu, quyu) => DrawNetwork(huitu, quyu, kuaizhao10, shezhi3));
        }
        else
        {
            var kuandu2 = neirongkuandu2;
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, kuandu2, 302), 0, MonitorCard.Cpu, donghua, (huitu, quyu) => DrawCpu(huitu, quyu, kuaizhao10, shezhi3));
            zong3 += 316;
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, kuandu2, 330), 1, MonitorCard.Gpu, donghua, (huitu, quyu) => DrawGpu(huitu, quyu, kuaizhao10, shezhi3));
            zong3 += 344;
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, kuandu2, 226), 2, MonitorCard.Memory, donghua, (huitu, quyu) => DrawMemory(huitu, quyu, kuaizhao10, shezhi3));
            zong3 += 240;
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, kuandu2, 226), 3, MonitorCard.Fans, donghua, (huitu, quyu) => DrawFans(huitu, quyu, kuaizhao10, shezhi3));
            zong3 += 240;
            var cipangaodu3 = DiskCardHeight(kuandu2, kuaizhao10.cipan2.Count);
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, kuandu2, cipangaodu3), 4, MonitorCard.Disks, donghua, (huitu, quyu) => DrawDisks(huitu, quyu, kuaizhao10, shezhi3));
            zong3 += cipangaodu3 + 14;
            DrawAnimatedCard(huitu3, new Rectangle(zuo3, zong3, kuandu2, 260), 5, MonitorCard.Network, donghua, (huitu, quyu) => DrawNetwork(huitu, quyu, kuaizhao10, shezhi3));
        }
    }

    private IEnumerable<KeyValuePair<MonitorCard, Rectangle>> MonitorCardRects(Rectangle bianjie6, SystemSnapshot kuaizhao10, int gundongzong2)
    {
        var zuo3 = zhikuandu + 24;
        var youzhi = 28;
        var kapianjiange = 16;
        var zong3 = zhigaodu + 20 - gundongzong2;
        var neirongkuandu2 = bianjie6.Width - zuo3 - youzhi;
        var kuanpingbuju2 = neirongkuandu2 >= 940;

        if (kuanpingbuju2)
        {
            var kapiankuandu = (neirongkuandu2 - kapianjiange) / 2;
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Cpu, new Rectangle(zuo3, zong3, kapiankuandu, 318));
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Gpu, new Rectangle(zuo3 + kapiankuandu + kapianjiange, zong3, kapiankuandu, 318));
            zong3 += 334;
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Memory, new Rectangle(zuo3, zong3, kapiankuandu, 226));
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Fans, new Rectangle(zuo3 + kapiankuandu + kapianjiange, zong3, kapiankuandu, 226));
            zong3 += 242;
            var cipangaodu2 = DiskCardHeight(neirongkuandu2, kuaizhao10.cipan2.Count);
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Disks, new Rectangle(zuo3, zong3, neirongkuandu2, cipangaodu2));
            zong3 += cipangaodu2 + 16;
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Network, new Rectangle(zuo3, zong3, neirongkuandu2, 260));
        }
        else
        {
            var kuandu2 = neirongkuandu2;
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Cpu, new Rectangle(zuo3, zong3, kuandu2, 302));
            zong3 += 316;
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Gpu, new Rectangle(zuo3, zong3, kuandu2, 330));
            zong3 += 344;
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Memory, new Rectangle(zuo3, zong3, kuandu2, 226));
            zong3 += 240;
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Fans, new Rectangle(zuo3, zong3, kuandu2, 226));
            zong3 += 240;
            var cipangaodu3 = DiskCardHeight(kuandu2, kuaizhao10.cipan2.Count);
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Disks, new Rectangle(zuo3, zong3, kuandu2, cipangaodu3));
            zong3 += cipangaodu3 + 14;
            yield return new KeyValuePair<MonitorCard, Rectangle>(MonitorCard.Network, new Rectangle(zuo3, zong3, kuandu2, 260));
        }
    }

    private void DrawMemoryTools(Graphics huitu3, Rectangle bianjie6, SystemSnapshot? kuaizhao10, TokenMonitorSnapshot tokenkuaizhao, AppSettings shezhi3, bool neicunqinglizhong, string neicunqinglijieguo, AnimationSnapshot donghua)
    {
        var biaotizuo = zhikuandu + 24;
        DrawText(huitu3, shezhi3.Text("工具栏", "Toolbox"), _biaotiziti, wenben2, new RectangleF(biaotizuo, zhigaodu + 18, bianjie6.Width - biaotizuo - 28, 34), Align.NearCenter);
        DrawText(huitu3, shezhi3.Text("内存整理与后台采集配置", "Memory cleanup and background collection"), _zhiziti3, zhiwenben2, new RectangleF(biaotizuo, zhigaodu + 50, bianjie6.Width - biaotizuo - 28, 22), Align.NearCenter);

        var gongjukapian = MoveForStagger(MemoryToolsCardRect(bianjie6), donghua, 0);
        DrawCard(huitu3, gongjukapian, shezhi3.Text("整理内存", "Clean Memory"), bohelvse);

        var neicun = kuaizhao10?.neicun;
        var shiyonglv = Ease("memory-tools", neicun?.shiyonglvbaifenbi2 ?? 0);
        var shiyonglvwenben = kuaizhao10 is null ? "-" : shiyonglv.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        DrawText(huitu3, shezhi3.Text("当前使用率", "Current usage"), _zhiziti3, zhiwenben2, new RectangleF(gongjukapian.Left + 18, gongjukapian.Top + 64, gongjukapian.Width - 36, 20), Align.NearCenter);
        DrawText(huitu3, shiyonglvwenben, _biaotiziti, bohelvse, new RectangleF(gongjukapian.Left + 18, gongjukapian.Top + 82, gongjukapian.Width - 36, 34), Align.NearCenter);
        DrawProgress(huitu3, new Rectangle(gongjukapian.Left + 18, gongjukapian.Top + 124, gongjukapian.Width - 36, 7), shiyonglv);

        var biaoqian = new[] { shezhi3.Text("总量", "Total"), shezhi3.Text("已用", "Used"), shezhi3.Text("可用", "Available") };
        var dangqianzhi = kuaizhao10 is null
            ? new[] { "-", "-", "-" }
            : new[] { Formatters.Size(neicun!.zongliangzhi3), Formatters.Size(neicun.yiyongzhi), Formatters.Size(neicun.keyongzhi3) };
        var tongjikuandu = (gongjukapian.Width - 52) / 3;
        for (var suoyin = 0; suoyin < 3; suoyin++)
        {
            DrawStat(huitu3, new Rectangle(gongjukapian.Left + 18 + suoyin * (tongjikuandu + 8), gongjukapian.Top + 148, tongjikuandu, 52), dangqianzhi[suoyin], biaoqian[suoyin]);
        }

        var zhuangtai = neicunqinglizhong
            ? shezhi3.Text("正在整理内存，请稍候...", "Cleaning memory, please wait...")
            : string.IsNullOrWhiteSpace(neicunqinglijieguo)
                ? shezhi3.Text("安全释放可整理的进程工作集，不会结束进程。", "Safely releases eligible process working sets without ending processes.")
                : neicunqinglijieguo;
        DrawText(huitu3, zhuangtai, _zhiziti2, zhiwenben, new RectangleF(gongjukapian.Left + 18, gongjukapian.Top + 214, gongjukapian.Width - 36, 24), Align.NearCenter);

        var anniu = new Rectangle(gongjukapian.Left + 18, gongjukapian.Bottom - 62, Math.Min(156, gongjukapian.Width - 36), 40);
        FillRound(huitu3, anniu, 18, roufenanniu);
        DrawRoundBorder(huitu3, anniu, 18, roufenbiankuang);
        var anniutubiao = new Rectangle(anniu.Left + 15, anniu.Top + 8, 24, 24);
        DrawMemoryCleanupIcon(huitu3, anniutubiao, meiguifenwenben, neicunqinglizhong ? (float)(donghua.gongzuobomai * 22d - 11d) : 0f);
        var anniuwenben = anniu;
        anniuwenben.Offset(12, 0);
        DrawText(huitu3, neicunqinglizhong ? shezhi3.Text("正在整理", "Cleaning") : shezhi3.Text("立即整理", "Clean Now"), _anniuziti, meiguifenwenben, anniuwenben, Align.Center);

        DrawHoverHighlight(huitu3, gongjukapian, _xuanfukapian2 == MonitorCard.MemoryTools);

        var caijikapian = MoveForStagger(TokenCollectorConfigRect(bianjie6), donghua, 1);
        DrawCard(huitu3, caijikapian, shezhi3.Text("Token 后台采集", "Token Background Collector"), zise);
        DrawHoverHighlight(huitu3, caijikapian, _xuanfukapian2 == MonitorCard.TokenCollector);

        var houtaiyanse = tokenkuaizhao.houtaicaijiqiyunxing ? bohelvse : tokenkuaizhao.houtairenwuyianzhuang ? zise : fenhongyanse;
        var houtaiwenben = shezhi3.zhiyingwen ? tokenkuaizhao.houtaiyingwenzhuangtai : tokenkuaizhao.houtaizhuangtai;
        DrawStatusPill(huitu3, new Rectangle(caijikapian.Right - 178, caijikapian.Top + 18, 150, 30), houtaiwenben, houtaiyanse);

        var caijimiaoshu = shezhi3.Text(
            "安装后会注册当前 EXE 的隐藏采集模式，关闭主界面后仍继续补读 OpenClaw 日志。",
            "Registers this EXE as a hidden collector so OpenClaw logs keep being imported after the main window closes.");
        DrawText(huitu3, caijimiaoshu, _zhiziti2, zhiwenben, new RectangleF(caijikapian.Left + 18, caijikapian.Top + 62, caijikapian.Width - 36, 24), Align.NearCenter);

        var caijiliekuan = Math.Max(1, (caijikapian.Width - 54) / 3);
        var caiji1 = new Rectangle(caijikapian.Left + 18, caijikapian.Top + 104, caijiliekuan, 54);
        var caiji2 = new Rectangle(caiji1.Right + 9, caiji1.Top, caijiliekuan, 54);
        var caiji3 = new Rectangle(caiji2.Right + 9, caiji1.Top, Math.Max(1, caijikapian.Right - 18 - caiji2.Right - 9), 54);

        DrawTokenStat(huitu3, caiji1, tokenkuaizhao.houtairenwuyianzhuang ? shezhi3.Text("已安装", "Installed") : shezhi3.Text("未安装", "Not installed"), shezhi3.Text("计划任务", "Task"));
        DrawTokenStat(huitu3, caiji2, tokenkuaizhao.houtaicaijiqijincheng is null ? "-" : tokenkuaizhao.houtaicaijiqijincheng.Value.ToString(CultureInfo.InvariantCulture), "PID");
        DrawTokenStat(huitu3, caiji3, TokenNumber(tokenkuaizhao.shishitokensudu, " token/s"), shezhi3.Text("实时 token/s", "Realtime token/s"));

        var xintiao = tokenkuaizhao.houtaixintiaoshijian is null
            ? shezhi3.Text("心跳：-", "Heartbeat: -")
            : shezhi3.Text("心跳：", "Heartbeat: ") + tokenkuaizhao.houtaixintiaoshijian.Value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        DrawText(huitu3, xintiao + "    " + shezhi3.Text("日志源：OpenClaw 本地会话", "Source: local OpenClaw sessions"), _zhiziti3, zhiwenben2, new RectangleF(caijikapian.Left + 18, caijikapian.Top + 174, caijikapian.Width - 36, 22), Align.NearCenter);

        var caijianniu = TokenCollectorConfigureActionRect(bianjie6);
        caijianniu.Offset(caijikapian.Left - TokenCollectorConfigRect(bianjie6).Left, caijikapian.Top - TokenCollectorConfigRect(bianjie6).Top);
        FillRound(huitu3, caijianniu, 18, roufenanniu);
        DrawRoundBorder(huitu3, caijianniu, 18, roufenbiankuang);
        var caijianniutubiao = new Rectangle(caijianniu.Left + 15, caijianniu.Top + 8, 24, 24);
        DrawOpenClawTokenIcon(huitu3, caijianniutubiao, meiguifenwenben);
        var caijianniuwenben = caijianniu;
        caijianniuwenben.Offset(16, 0);
        DrawText(huitu3, tokenkuaizhao.houtairenwuyianzhuang ? shezhi3.Text("修复配置", "Repair Config") : shezhi3.Text("一键安装", "One-click Install"), _anniuziti, meiguifenwenben, caijianniuwenben, Align.Center);

        var androidkapian = MoveForStagger(AndroidLinkCardRect(bianjie6), donghua, 2);
        DrawCard(huitu3, androidkapian, shezhi3.Text("Android 连接", "Android Link"), lanse);
        DrawHoverHighlight(huitu3, androidkapian, _xuanfukapian2 == MonitorCard.AndroidLink);

        var androidzhuangtaiyanse = shezhi3.relayEnabled ? bohelvse : fenhongyanse;
        var androidzhuangtai = shezhi3.relayEnabled
            ? shezhi3.Text("已开启", "Enabled")
            : shezhi3.Text("未开启", "Disabled");
        DrawStatusPill(huitu3, new Rectangle(androidkapian.Right - 178, androidkapian.Top + 18, 150, 30), androidzhuangtai, androidzhuangtaiyanse);

        var androidmiaoshu = shezhi3.Text(
            "配置公网地址和设备密钥，Android 端通过 WebSocket 实时接收本机快照。",
            "Configure the public URL and device key; Android receives live snapshots over WebSocket.");
        DrawText(huitu3, androidmiaoshu, _zhiziti2, zhiwenben, new RectangleF(androidkapian.Left + 18, androidkapian.Top + 62, androidkapian.Width - 36, 24), Align.NearCenter);

        var lianjieliekuan = Math.Max(1, (androidkapian.Width - 54) / 3);
        var lianjie1 = new Rectangle(androidkapian.Left + 18, androidkapian.Top + 104, lianjieliekuan, 54);
        var lianjie2 = new Rectangle(lianjie1.Right + 9, lianjie1.Top, lianjieliekuan, 54);
        var lianjie3 = new Rectangle(lianjie2.Right + 9, lianjie1.Top, Math.Max(1, androidkapian.Right - 18 - lianjie2.Right - 9), 54);

        var gongwang = !string.IsNullOrWhiteSpace(_suidaoURL)
            ? ShortText(_suidaoURL, 24)
            : string.IsNullOrWhiteSpace(shezhi3.relayUrl)
                ? shezhi3.Text("未配置", "Not set")
                : ShortText(shezhi3.relayUrl, 24);
        var miyao = string.IsNullOrWhiteSpace(shezhi3.relayDeviceKey)
            ? shezhi3.Text("点击生成", "Click to create")
            : shezhi3.Text("点击复制", "Click to copy");

        var duankouzhuangtai = PortStatusText(shezhi3, _neiqianfuwuqizhuangtai);
        var duankouyanse = _neiqianfuwuqizhuangtai == EmbeddedServerState.Running ? bohelvse : fenhongyanse;
        DrawTokenStat(huitu3, lianjie1, duankouzhuangtai, shezhi3.Text("本机端口", "Local port"));
        DrawRoundBorder(huitu3, lianjie1, 8, Color.FromArgb(112, duankouyanse));
        DrawTokenStat(huitu3, lianjie2, gongwang, shezhi3.Text("公网地址", "Public URL"));
        DrawTokenStat(huitu3, lianjie3, miyao, shezhi3.Text("设备密钥", "Device key"));
        DrawRoundBorder(huitu3, lianjie3, 8, Color.FromArgb(112, fenhongyanse));

        var androidtishi = PortPromptText(shezhi3, _neiqianfuwuqizhuangtai, _neiqianfuwuqitishi);
        DrawText(huitu3, androidtishi, _zhiziti3, zhiwenben2, new RectangleF(androidkapian.Left + 18, androidkapian.Top + 168, androidkapian.Width - 36, 22), Align.NearCenter);

        var androidanniu = AndroidLinkActionRect(bianjie6);
        androidanniu.Offset(androidkapian.Left - AndroidLinkCardRect(bianjie6).Left, androidkapian.Top - AndroidLinkCardRect(bianjie6).Top);
        FillRound(huitu3, androidanniu, 18, roufenanniu);
        DrawRoundBorder(huitu3, androidanniu, 18, roufenbiankuang);
        var androidtubiao = new Rectangle(androidanniu.Left + 15, androidanniu.Top + 8, 24, 24);
        DrawAndroidLinkIcon(huitu3, androidtubiao, meiguifenwenben);
        var androidanniuwenben = androidanniu;
        androidanniuwenben.Offset(16, 0);
        DrawText(huitu3, shezhi3.Text("配置连接", "Configure Link"), _anniuziti, meiguifenwenben, androidanniuwenben, Align.Center);

    }

    private void DrawTokenMonitor(Graphics huitu3, Rectangle bianjie6, SystemSnapshot? kuaizhao10, TokenMonitorSnapshot tokenkuaizhao, AppSettings shezhi3, AnimationSnapshot donghua)
    {
        var biaotizuo = zhikuandu + 24;
        DrawText(huitu3, shezhi3.Text("Token 监控", "Token Monitor"), _biaotiziti, wenben2, new RectangleF(biaotizuo, zhigaodu + 18, bianjie6.Width - biaotizuo - 28, 34), Align.NearCenter);
        DrawText(huitu3, shezhi3.Text("直接读取 OpenClaw 本地日志，后台采集器可持续补齐数据", "Reads local OpenClaw logs directly; the background collector can keep data filled in"), _zhiziti3, zhiwenben2, new RectangleF(biaotizuo, zhigaodu + 50, bianjie6.Width - biaotizuo - 28, 22), Align.NearCenter);

        var zonglanquyu = MoveForStagger(TokenSummaryRect(bianjie6), donghua, 0);
        var huodongquyu = MoveForStagger(TokenActivityRect(bianjie6), donghua, 1);
        DrawTokenSummaryStrip(huitu3, zonglanquyu, tokenkuaizhao, shezhi3);
        DrawTokenSummaryDivider(huitu3, zonglanquyu, huodongquyu);
        DrawTokenActivityHeatmap(huitu3, huodongquyu, tokenkuaizhao, shezhi3);
        DrawTokenHardwareStrip(huitu3, MoveForStagger(TokenHardwareRect(bianjie6), donghua, 2), kuaizhao10, shezhi3);
        DrawDeepSeekBalanceCard(huitu3, MoveForStagger(DeepSeekBalanceRect(bianjie6), donghua, 3), shezhi3);

        var gongjukapian = MoveForStagger(TokenMonitorStatusRect(bianjie6), donghua, 4);
        DrawTokenPlainSectionTitle(huitu3, gongjukapian, shezhi3.Text("Token 日志", "Token Logs"));

        var zhuangtaiyanse = tokenkuaizhao.yunxing ? bohelvse : tokenkuaizhao.peizhiwanzheng ? zise : fenhongyanse;
        var zhuangtaiwenben = shezhi3.zhiyingwen ? tokenkuaizhao.yingwenzhuangtai : tokenkuaizhao.zhuangtai;
        DrawStatusPill(huitu3, new Rectangle(gongjukapian.Right - 178, gongjukapian.Top + 18, 150, 30), zhuangtaiwenben, zhuangtaiyanse);

        var miaoshu = tokenkuaizhao.houtaicaijiqiyunxing
            ? shezhi3.Text("后台采集器正在读取 OpenClaw 本地日志，关闭主界面后也会继续。", "The background collector is reading local OpenClaw logs and keeps running after the main window closes.")
            : tokenkuaizhao.peizhiwanzheng
                ? shezhi3.Text("可点击下方按钮立即补读日志；长期采集请在工具栏一键安装后台采集。", "Use the button below to import logs now; install the background collector in Toolbox for continuous capture.")
                : shezhi3.Text("未找到 OpenClaw 本地日志目录，请先完成 OpenClaw 配置。", "The local OpenClaw log folder was not found; configure OpenClaw first.");
        DrawText(huitu3, miaoshu, _zhiziti2, zhiwenben, new RectangleF(gongjukapian.Left + 18, gongjukapian.Top + 58, gongjukapian.Width - 36, 24), Align.NearCenter);

        var dizhi = shezhi3.Text("日志源：OpenClaw sessions + runtime log", "Source: OpenClaw sessions + runtime log");
        DrawText(huitu3, dizhi, _zhiziti3, zhiwenben2, new RectangleF(gongjukapian.Left + 18, gongjukapian.Top + 84, gongjukapian.Width - 36, 22), Align.NearCenter);
        DrawText(huitu3, shezhi3.Text("统计日期：", "Date: ") + tokenkuaizhao.tongjiriqi, _zhiziti3, zhiwenben2, new RectangleF(gongjukapian.Left + 18, gongjukapian.Top + 106, gongjukapian.Width - 36, 20), Align.NearCenter);

        var suanlie = Math.Max(1, (gongjukapian.Width - 63) / 4);
        var tongji1 = new Rectangle(gongjukapian.Left + 18, gongjukapian.Top + 138, suanlie, 54);
        var tongji2 = new Rectangle(tongji1.Right + 9, tongji1.Top, suanlie, 54);
        var tongji3 = new Rectangle(tongji2.Right + 9, tongji1.Top, suanlie, 54);
        var tongji4 = new Rectangle(tongji3.Right + 9, tongji1.Top, Math.Max(1, gongjukapian.Right - 18 - tongji3.Right - 9), 54);
        var tongji5 = new Rectangle(tongji1.Left, tongji1.Bottom + 10, suanlie, 54);
        var tongji6 = new Rectangle(tongji5.Right + 9, tongji5.Top, suanlie, 54);
        var tongji7 = new Rectangle(tongji6.Right + 9, tongji5.Top, suanlie, 54);
        var tongji8 = new Rectangle(tongji7.Right + 9, tongji5.Top, Math.Max(1, gongjukapian.Right - 18 - tongji7.Right - 9), 54);

        DrawTokenPlainStat(huitu3, tongji1, tokenkuaizhao.moxingmingcheng, shezhi3.Text("模型", "Model"));
        DrawTokenPlainStat(huitu3, tongji2, FormatTokenPlain(tokenkuaizhao.shurutokens), shezhi3.Text("输入 tokens", "Input tokens"));
        DrawTokenPlainStat(huitu3, tongji3, FormatTokenPlain(tokenkuaizhao.shuchutokens), shezhi3.Text("输出 tokens", "Output tokens"));
        DrawTokenPlainStat(huitu3, tongji4, TokenNumber(tokenkuaizhao.shishitokensudu, " token/s"), shezhi3.Text("实时 token/s", "Realtime token/s"));
        DrawTokenPlainStat(huitu3, tongji5, TokenNumber(tokenkuaizhao.zhunquesudu, " token/s"), shezhi3.Text("准确速度", "Accurate speed"));
        DrawTokenPlainStat(huitu3, tongji6, FormatFirstTokenLatency(tokenkuaizhao.shouziziyanmiao, tokenkuaizhao.shouziziyangusuan), shezhi3.Text("首字延迟", "First token"));
        DrawTokenPlainStat(huitu3, tongji7, FormatDurationMaybe(tokenkuaizhao.zonghaoshi), shezhi3.Text("本轮总耗时", "Round time"));
        DrawTokenPlainStat(huitu3, tongji8, $"{shezhi3.Text("今日输入", "Today input")} {FormatTokenPlain(tokenkuaizhao.jinrishurutokens)} / {shezhi3.Text("今日输出", "Today output")} {FormatTokenPlain(tokenkuaizhao.jinrishuchutokens)}", shezhi3.Text("今日总量", "Today total") + " " + FormatTokenPlain(tokenkuaizhao.jinritokens));

        var pidwenben = tokenkuaizhao.jinchengbianhao is null
            ? shezhi3.Text("PID：-", "PID: -")
            : shezhi3.Text($"PID：{tokenkuaizhao.jinchengbianhao}", $"PID: {tokenkuaizhao.jinchengbianhao}");
        var laiyuan = tokenkuaizhao.bengyingyongqidong
            ? shezhi3.Text("来源：图形界面隐藏启动", "Source: hidden graphical launch")
            : tokenkuaizhao.houtaicaijiqiyunxing
                ? shezhi3.Text("来源：后台采集器", "Source: background collector")
                : tokenkuaizhao.waibuyiyunxing
                ? shezhi3.Text("来源：外部已运行进程", "Source: already-running external process")
                : shezhi3.Text("来源：本地日志补读", "Source: local log import");
        DrawText(huitu3, pidwenben + "    " + laiyuan, _zhiziti3, zhiwenben2, new RectangleF(gongjukapian.Left + 18, gongjukapian.Top + 266, gongjukapian.Width - 36, 22), Align.NearCenter);

        var anniu = TokenMonitorActionRect(bianjie6);
        anniu.Offset(gongjukapian.Left - TokenMonitorStatusRect(bianjie6).Left, gongjukapian.Top - TokenMonitorStatusRect(bianjie6).Top);
        FillRound(huitu3, anniu, 18, roufenanniu);
        DrawRoundBorder(huitu3, anniu, 18, roufenbiankuang);
        var anniutubiao = new Rectangle(anniu.Left + 15, anniu.Top + 8, 24, 24);
        DrawOpenClawTokenIcon(huitu3, anniutubiao, meiguifenwenben);
        var anniuwenben = anniu;
        anniuwenben.Offset(14, 0);
        DrawText(huitu3, shezhi3.Text("补读日志", "Import Logs"), _anniuziti, meiguifenwenben, anniuwenben, Align.Center);

    }

    private static void DrawTokenSummaryDivider(Graphics huitu3, Rectangle shangquyu, Rectangle xiaquyu)
    {
        var zongxiang = (shangquyu.Bottom + xiaquyu.Top) / 2;
        var zuo = Math.Min(shangquyu.Left, xiaquyu.Left) + 18;
        var you = Math.Max(shangquyu.Right, xiaquyu.Right) - 18;
        using var bi = new Pen(Color.FromArgb(92, 210, 214, 224), 1f);
        huitu3.DrawLine(bi, zuo, zongxiang, you, zongxiang);
    }

    private void DrawTokenSummaryStrip(Graphics huitu3, Rectangle quyu, TokenMonitorSnapshot tokenkuaizhao, AppSettings shezhi3)
    {
        var zonglan = tokenkuaizhao.tongjizonglan;
        var shuzhi = new[]
        {
            FormatTokenCompact(zonglan.leijitokens),
            FormatTokenCompact(zonglan.fengzhitokens),
            FormatDurationCompact(zonglan.zuichangrenwumiao),
            FormatDays(zonglan.dangqianlianxutianshu, shezhi3),
            FormatDays(zonglan.zuichanglianxutianshu, shezhi3)
        };
        var biaoqian = new[]
        {
            shezhi3.Text("累计 Token 数", "Cumulative Tokens"),
            shezhi3.Text("峰值 Token 数", "Peak Tokens"),
            shezhi3.Text("最长任务时长", "Longest Task"),
            shezhi3.Text("当前连续天数", "Current Streak"),
            shezhi3.Text("最长连续天数", "Longest Streak")
        };

        var liekuan = quyu.Width / 5f;
        for (var suoyin = 0; suoyin < 5; suoyin++)
        {
            var hengxiang = quyu.Left + suoyin * liekuan;
            var lie = new RectangleF(hengxiang, quyu.Top + 8, liekuan, quyu.Height - 16);

            DrawText(huitu3, shuzhi[suoyin], _zhiziti4, wenben2, new RectangleF(lie.Left + 8, lie.Top + 2, lie.Width - 16, 24), Align.Center);
            DrawText(huitu3, biaoqian[suoyin], _zhiziti3, zhiwenben2, new RectangleF(lie.Left + 8, lie.Top + 28, lie.Width - 16, 22), Align.Center);
        }
    }

    private void DrawTokenHardwareStrip(Graphics huitu3, Rectangle quyu, SystemSnapshot? kuaizhao10, AppSettings shezhi3)
    {
        DrawText(huitu3, shezhi3.Text("系统占用", "System Usage"), _zhiziti, wenben2, new RectangleF(quyu.Left + 18, quyu.Top + 12, 92, 24), Align.NearCenter);

        var xianqia = kuaizhao10?.xianqia7.FirstOrDefault();
        var shuzhi = new[]
        {
            kuaizhao10 is null ? "-" : Formatters.Percent(kuaizhao10.chuliqi3.shiyonglvbaifenbi),
            xianqia is null ? "-" : Formatters.Percent(xianqia.shiyonglvbaifenbi4),
            GpuMemoryUsageText(xianqia),
            MemoryUsageText(kuaizhao10?.neicun)
        };
        var biaoqian = new[]
        {
            shezhi3.Text("CPU 占用", "CPU Usage"),
            shezhi3.Text("GPU 占用", "GPU Usage"),
            shezhi3.Text("显存占用", "VRAM Usage"),
            shezhi3.Text("内存占用", "Memory Usage")
        };

        var kaishihengxiang = quyu.Left + 122;
        var liekuan = (quyu.Right - 18 - kaishihengxiang) / 4f;
        for (var suoyin = 0; suoyin < 4; suoyin++)
        {
            var hengxiang = kaishihengxiang + suoyin * liekuan;
            var lie = new RectangleF(hengxiang, quyu.Top + 9, liekuan, quyu.Height - 18);

            DrawText(huitu3, shuzhi[suoyin], _zhiziti4, suoyin == 2 ? zise : lanse, new RectangleF(lie.Left + 8, lie.Top + 2, lie.Width - 16, 24), Align.Center);
            DrawText(huitu3, biaoqian[suoyin], _zhiziti3, zhiwenben2, new RectangleF(lie.Left + 8, lie.Top + 29, lie.Width - 16, 20), Align.Center);
        }
    }

    private static Rectangle DeepSeekBalanceRect(Rectangle bianjie6)
    {
        var yingjian = TokenHardwareRect(bianjie6);
        return new Rectangle(yingjian.Left, yingjian.Bottom + 16, yingjian.Width, 74);
    }

    private void DrawDeepSeekBalanceCard(Graphics huitu3, Rectangle quyu, AppSettings shezhi3)
    {
        DrawText(huitu3, shezhi3.Text("DeepSeek", "DeepSeek"), _zhiziti, wenben2,
            new RectangleF(quyu.Left + 18, quyu.Top + 12, 92, 24), Align.NearCenter);

        var zhuangtaiyanse = _deepseekyuexinxi.chenggong ? bohelvse : fenhongyanse;
        var shuzhi = new[]
        {
            _deepseekyuexinxi.zhuangtai,
            _deepseekyuexinxi.chenggong ? _deepseekyuexinxi.yuenwenben : "-",
            _deepseekyuexinxi.totalTokensText,
            _deepseekyuexinxi.huancunmingzhongwenben,
            _deepseekyuexinxi.weimingzhongwenben,
            _deepseekyuexinxi.huancunmingzhonglvText
        };
        var biaoqian = new[]
        {
            shezhi3.Text("状态", "Status"),
            shezhi3.Text("余额", "Balance"),
            shezhi3.Text("今日 Token", "Today"),
            shezhi3.Text("命中缓存", "Hit"),
            shezhi3.Text("未命中缓存", "Miss"),
            shezhi3.Text("命中率", "Rate")
        };
        var yansemen = new[] { zhuangtaiyanse, lanse, zise, bohelvse, fenhongyanse, bohelvse };

        var kaishi = quyu.Left + 132;
        var liekuan = (quyu.Right - 18 - kaishi) / 6f;
        for (var i = 0; i < 6; i++)
        {
            var heng = kaishi + i * liekuan;
            var lie = new RectangleF(heng, quyu.Top + 9, liekuan, quyu.Height - 18);
            DrawText(huitu3, shuzhi[i], _zhiziti4, yansemen[i], new RectangleF(lie.Left + 8, lie.Top + 2, lie.Width - 16, 24), Align.Center);
            DrawText(huitu3, biaoqian[i], _zhiziti3, zhiwenben2, new RectangleF(lie.Left + 8, lie.Top + 30, lie.Width - 16, 18), Align.Center);
        }
    }

    private void DrawTokenActivityHeatmap(Graphics huitu3, Rectangle quyu, TokenMonitorSnapshot tokenkuaizhao, AppSettings shezhi3)
    {
        DrawText(huitu3, shezhi3.Text("Token 活动", "Token Activity"), _zhiziti, wenben2, new RectangleF(quyu.Left + 18, quyu.Top + 16, quyu.Width - 36, 24), Align.NearCenter);

        DrawTokenActivityTab(huitu3, quyu, shezhi3, TokenActivityMode.Daily, _tokenhuodongmoshi2 == TokenActivityMode.Daily);
        DrawTokenActivityTab(huitu3, quyu, shezhi3, TokenActivityMode.Weekly, _tokenhuodongmoshi2 == TokenActivityMode.Weekly);

        var zidian = BuildTokenActivityMap(tokenkuaizhao);
        var zhiquyu = BuildTokenActivityValues(zidian, _tokenhuodongmoshi2);
        var (kaishi, jieshu) = TokenActivityDateRange(_tokenhuodongmoshi2);
        var jihe = TokenHeatmapGeometry(quyu);
        DateOnly? xuanfuriqi = null;
        Rectangle xuanfukuai = Rectangle.Empty;

        for (var suoyin = 0; suoyin < tokenhuodongfangkuaishu; suoyin++)
        {
            var riqi = kaishi.AddDays(suoyin);
            zhiquyu.TryGetValue(riqi, out var qiangduzhi);
            var fangkuai = TokenHeatmapCellRect(jihe, suoyin, _tokenhuodongmoshi2);
            var xuanfu = string.Equals(TokenDateKey(riqi), _tokenhuodongxuanfuriqi2, StringComparison.Ordinal);
            var zhengliexuanfu = IsWeeklyColumnHovered(kaishi, suoyin, _tokenhuodongxuanfuriqi2);
            FillRound(huitu3, fangkuai, Math.Max(2, fangkuai.Width / 3), HeatColor(qiangduzhi, _tokenhuodongmoshi2));
            if (xuanfu)
            {
                xuanfuriqi = riqi;
                xuanfukuai = fangkuai;
                if (_tokenhuodongmoshi2 != TokenActivityMode.Weekly)
                {
                    DrawTokenHeatCellBorder(huitu3, fangkuai);
                }
            }

            if (_tokenhuodongmoshi2 == TokenActivityMode.Weekly && zhengliexuanfu && qiangduzhi > 0)
            {
                DrawTokenHeatCellBorder(huitu3, fangkuai);
            }
        }

        var yuefen = new DateOnly(kaishi.Year, kaishi.Month, 1).AddMonths(1);
        while (yuefen <= jieshu)
        {
            var suoyin = Math.Clamp(yuefen.DayNumber - kaishi.DayNumber, 0, tokenhuodongfangkuaishu - 1);
            var fangkuai = TokenHeatmapCellRect(jihe, suoyin, _tokenhuodongmoshi2);
            var hengxiang = fangkuai.Left;
            DrawText(huitu3, MonthLabel(yuefen, shezhi3), _zhiziti3, zhiwenben2, new RectangleF(hengxiang - 5, quyu.Bottom - 28, 45, 18), Align.NearCenter);
            yuefen = yuefen.AddMonths(1);
        }

        if (xuanfuriqi is not null)
        {
            DrawTokenActivityTooltip(huitu3, quyu, xuanfukuai, xuanfuriqi.Value, zidian, _tokenhuodongmoshi2, shezhi3);
        }
    }

    private void DrawTokenActivityTab(Graphics huitu3, Rectangle quyu, AppSettings shezhi3, TokenActivityMode huodongmoshi, bool xuanze)
    {
        var biaoqian = huodongmoshi switch
        {
            TokenActivityMode.Weekly => shezhi3.Text("每周", "Weekly"),
            _ => shezhi3.Text("每日", "Daily")
        };
        var tab = TokenActivityTabRect(quyu, huodongmoshi);
        DrawText(huitu3, biaoqian, _zhiziti3, xuanze ? lanse : zhiwenben2, tab, Align.Center);
    }

    private void DrawTokenActivityTooltip(Graphics huitu3, Rectangle quyu, Rectangle fangkuai, DateOnly riqi, Dictionary<DateOnly, TokenDailyUsage> zidian, TokenActivityMode huodongmoshi, AppSettings shezhi3)
    {
        var wenben = FormatActivityTooltipText(zidian, riqi, huodongmoshi, shezhi3);
        var kuan = Math.Min(Math.Max(180, quyu.Width - 36), Math.Max(180, (int)Math.Ceiling(huitu3.MeasureString(wenben, _zhiziti3).Width) + 28));
        var gao = 32;
        var hengxiang = fangkuai.Right + 10;
        if (hengxiang + kuan > quyu.Right - 14) hengxiang = fangkuai.Left - kuan - 10;
        if (hengxiang < quyu.Left + 14) hengxiang = quyu.Left + 14;
        var zongxiang = fangkuai.Top - 36;
        if (zongxiang + gao > quyu.Bottom - 14) zongxiang = quyu.Bottom - gao - 14;
        if (zongxiang < quyu.Top + 46) zongxiang = quyu.Top + 46;
        var tishi = new Rectangle(hengxiang, zongxiang, kuan, gao);

        var yinying = tishi;
        yinying.Offset(0, 5);
        FillRound(huitu3, yinying, 14, Color.FromArgb(18, 72, 76, 92));
        FillRound(huitu3, tishi, 14, Color.FromArgb(246, Color.White));
        DrawRoundBorder(huitu3, tishi, 14, Color.FromArgb(224, 226, 236, 246));
        DrawText(huitu3, wenben, _zhiziti3, wenben2, new RectangleF(tishi.Left + 13, tishi.Top + 3, tishi.Width - 26, tishi.Height - 6), Align.Center);
    }

    private static void DrawTokenHeatCellBorder(Graphics huitu3, Rectangle fangkuai)
    {
        using var bi = RoundPen(Color.FromArgb(226, lanse), 2f);
        using var lujing = RoundedPath(new Rectangle(fangkuai.Left - 2, fangkuai.Top - 2, fangkuai.Width + 4, fangkuai.Height + 4), Math.Max(3, fangkuai.Width / 2));
        huitu3.DrawPath(bi, lujing);
    }

    private static Rectangle MemoryToolsCardRect(Rectangle bianjie6)
    {
        var zuo = zhikuandu + 24;
        return new Rectangle(zuo, zhigaodu + 90, Math.Max(320, bianjie6.Width - zuo - 28), 330);
    }

    private static Rectangle AndroidLinkCardRect(Rectangle bianjie6)
    {
        var peizhikapian = TokenCollectorConfigRect(bianjie6);
        return new Rectangle(peizhikapian.Left, peizhikapian.Bottom + 16, peizhikapian.Width, 268);
    }

    private static Rectangle AndroidLinkButtonRowRect(Rectangle bianjie6)
    {
        var lianjiekapian = AndroidLinkCardRect(bianjie6);
        return new Rectangle(lianjiekapian.Left + 18, lianjiekapian.Bottom - 58, lianjiekapian.Width - 36, 40);
    }

    private static Rectangle TokenCollectorConfigRect(Rectangle bianjie6)
    {
        var gongjukapian = MemoryToolsCardRect(bianjie6);
        return new Rectangle(gongjukapian.Left, gongjukapian.Bottom + 16, gongjukapian.Width, 260);
    }

    private static Rectangle TokenMonitorCardRect(Rectangle bianjie6)
    {
        return TokenMonitorStatusRect(bianjie6);
    }

    private static Rectangle TokenSummaryRect(Rectangle bianjie6)
    {
        var zuo = zhikuandu + 24;
        return new Rectangle(zuo, zhigaodu + 92, Math.Max(360, bianjie6.Width - zuo - 28), 74);
    }

    private static Rectangle TokenActivityRect(Rectangle bianjie6)
    {
        var zonglan = TokenSummaryRect(bianjie6);
        return new Rectangle(zonglan.Left, zonglan.Bottom + 24, zonglan.Width, 248);
    }

    private static Rectangle TokenHardwareRect(Rectangle bianjie6)
    {
        var huodong = TokenActivityRect(bianjie6);
        return new Rectangle(huodong.Left, huodong.Bottom + 16, huodong.Width, 74);
    }

    private static Rectangle TokenMonitorStatusRect(Rectangle bianjie6)
    {
        var deepseek = DeepSeekBalanceRect(bianjie6);
        return new Rectangle(deepseek.Left, deepseek.Bottom + 16, deepseek.Width, 376);
    }

    private int DiskCardHeight(int kuandu3, int cipanshuliang2)
    {
        var lieshu = kuandu3 >= 780 ? 2 : 1;
        var hangshu = (int)Math.Ceiling(Math.Max(1, cipanshuliang2) / (double)lieshu);
        return 72 + hangshu * 92 + 18;
    }

    private void DrawCpu(Graphics huitu4, Rectangle quyu, SystemSnapshot kuaizhao11, AppSettings shezhi4)
    {
        DrawCard(huitu4, quyu, shezhi4.Text("处理器", "CPU"), fenhongyanse);
        DrawText(huitu4, kuaizhao11.chuliqi3.xinghao2, _zhiziti2, zhiwenben, new RectangleF(quyu.Left + 18, quyu.Top + 58, quyu.Width - 36, 24), Align.NearCenter);
        var shiyonglv2 = Ease("cpu", kuaizhao11.chuliqi3.shiyonglvbaifenbi);
        DrawGauge(huitu4, new Point(quyu.Left + quyu.Width / 2, quyu.Top + 156), 54, shiyonglv2, fenhongyanse);

        var zong4 = quyu.Bottom - 68;
        var kuandu4 = (quyu.Width - 60) / 4;
        DrawStat(huitu4, new Rectangle(quyu.Left + 18, zong4, kuandu4, 52), kuaizhao11.chuliqi3.zhipinlv.ToString("0", CultureInfo.InvariantCulture), shezhi4.Text("频率", "Clock"));
        DrawStat(huitu4, new Rectangle(quyu.Left + 24 + kuandu4, zong4, kuandu4, 52), kuaizhao11.chuliqi3.hexin2.ToString(CultureInfo.InvariantCulture), shezhi4.Text("核心", "Cores"));
        DrawStat(huitu4, new Rectangle(quyu.Left + 30 + kuandu4 * 2, zong4, kuandu4, 52), Formatters.Temperature(kuaizhao11.chuliqi3.wenduzhi), "°C");
        DrawStat(huitu4, new Rectangle(quyu.Left + 36 + kuandu4 * 3, zong4, quyu.Right - (quyu.Left + 36 + kuandu4 * 3) - 18, 52), Formatters.Power(kuaizhao11.chuliqi3.gonghaowazhi, kuaizhao11.chuliqi3.gonghaogusuan), shezhi4.Text("功耗", "Power"));
    }

    private void DrawGpu(Graphics huitu5, Rectangle quyu2, SystemSnapshot kuaizhao12, AppSettings shezhi5)
    {
        DrawCard(huitu5, quyu2, shezhi5.Text("显卡", "GPU"), lanse);
        var xianqia8 = kuaizhao12.xianqia7.FirstOrDefault();
        DrawText(huitu5, xianqia8?.xinghao3 ?? shezhi5.Text("未检测到显卡", "No GPU detected"), _zhiziti2, zhiwenben, new RectangleF(quyu2.Left + 18, quyu2.Top + 58, quyu2.Width - 36, 24), Align.NearCenter);
        var shiyonglv3 = Ease("gpu", xianqia8?.shiyonglvbaifenbi4 ?? 0);
        DrawGauge(huitu5, new Point(quyu2.Left + quyu2.Width / 2, quyu2.Top + 132), 46, shiyonglv3, lanse);

        var xianqiabiaoqian = new[]
        {
            "°C", shezhi5.Text("功耗", "Power"), shezhi5.Text("核心 MHz", "Core MHz"), shezhi5.Text("显存 MHz", "Memory MHz"),
            shezhi5.Text("风扇", "Fan"), shezhi5.Text("已用", "Used"), shezhi5.Text("总显存", "VRAM"), shezhi5.Text("占用", "Load")
        };
        var xianqiazhi = new string[8];
        if (xianqia8 is null)
        {
            Array.Fill(xianqiazhi, "-");
        }
        else
        {
            xianqiazhi[0] = Formatters.Temperature(xianqia8.wenduzhi2);
            xianqiazhi[1] = Formatters.Power(xianqia8.gonghaowazhi, xianqia8.gonghaogusuan);
            xianqiazhi[2] = xianqia8.hexinpinlv > 0 ? xianqia8.hexinpinlv.ToString("0", CultureInfo.InvariantCulture) : "-";
            xianqiazhi[3] = xianqia8.neicunpinlv > 0 ? xianqia8.neicunpinlv.ToString("0", CultureInfo.InvariantCulture) : "-";
            xianqiazhi[4] = xianqia8.fengshanbaifenbi > 0 ? Formatters.Percent(xianqia8.fengshanbaifenbi) : "-";
            xianqiazhi[5] = Formatters.Size(xianqia8.zhiyiyongzhi);
            xianqiazhi[6] = Formatters.Size(xianqia8.zhizongliangzhi);
            xianqiazhi[7] = Formatters.Percent(xianqia8.shiyonglvbaifenbi4);
        }

        var zong5 = quyu2.Bottom - 114;
        var kuandu5 = (quyu2.Width - 66) / 4;
        for (var suoyin3 = 0; suoyin3 < 8; suoyin3++)
        {
            var liesuoyin = suoyin3 % 4;
            var hang4 = suoyin3 / 4;
            DrawStat(huitu5, new Rectangle(quyu2.Left + 18 + liesuoyin * (kuandu5 + 8), zong5 + hang4 * 58, kuandu5, 50), xianqiazhi[suoyin3], xianqiabiaoqian[suoyin3]);
        }
    }

    private void DrawMemory(Graphics huitu6, Rectangle quyu3, SystemSnapshot kuaizhao13, AppSettings shezhi6)
    {
        DrawCard(huitu6, quyu3, shezhi6.Text("内存", "Memory"), bohelvse);
        var shiyonglv4 = Ease("mem", kuaizhao13.neicun.shiyonglvbaifenbi2);
        var biaotou = new RectangleF(quyu3.Left + 18, quyu3.Top + 66, quyu3.Width - 36, 22);
        DrawText(huitu6, shezhi6.Text("使用率", "Usage"), _zhiziti, zhiwenben, biaotou, Align.NearCenter);
        DrawText(huitu6, shiyonglv4.ToString("0.0", CultureInfo.InvariantCulture) + "%", _zhiziti4, bohelvse, biaotou, Align.FarCenter);
        DrawProgress(huitu6, new Rectangle(quyu3.Left + 18, quyu3.Top + 94, quyu3.Width - 36, 7), shiyonglv4);

        var neicunbiaoqian = new[] { shezhi6.Text("总量", "Total"), shezhi6.Text("已用", "Used"), shezhi6.Text("可用", "Available") };
        var neicunzhi = new[] { Formatters.Size(kuaizhao13.neicun.zongliangzhi3), Formatters.Size(kuaizhao13.neicun.yiyongzhi), Formatters.Size(kuaizhao13.neicun.keyongzhi3) };
        for (var suoyin4 = 0; suoyin4 < 3; suoyin4++)
        {
            var hang5 = new RectangleF(quyu3.Left + 18, quyu3.Top + 124 + suoyin4 * 34, quyu3.Width - 36, 24);
            DrawText(huitu6, neicunbiaoqian[suoyin4], _zhiziti2, zhiwenben, hang5, Align.NearCenter);
            DrawText(huitu6, neicunzhi[suoyin4], _zhiziti4, wenben2, hang5, Align.FarCenter);
        }
    }

    private void DrawFans(Graphics huitu7, Rectangle quyu4, SystemSnapshot kuaizhao14, AppSettings shezhi7)
    {
        DrawCard(huitu7, quyu4, shezhi7.Text("风扇", "Fans"), zise);
        var zong6 = quyu4.Top + 64;
        var fengshanleixingliebiao = new[] { "cpu", "system", "gpu", "other" };
        foreach (var leixing3 in fengshanleixingliebiao)
        {
            var fengshan4 = kuaizhao14.fengshan3.Where(fengshanxiang3 => fengshanxiang3.leixing2 == leixing3).ToList();
            if (fengshan4.Count == 0) continue;

            DrawText(huitu7, FanLabel(leixing3, shezhi7), _zhiziti3, zhiwenben2, new RectangleF(quyu4.Left + 18, zong6, quyu4.Width - 36, 18), Align.NearCenter);
            zong6 += 22;
            foreach (var fengshan5 in fengshan4)
            {
                if (zong6 + 36 > quyu4.Bottom - 16) return;
                var hang6 = new Rectangle(quyu4.Left + 18, zong6, quyu4.Width - 36, 34);
                FillRound(huitu7, hang6, 8, Color.FromArgb(110, Color.White));
                DrawRoundBorder(huitu7, hang6, 8, Color.FromArgb(236, 236, 242));
                var fengshanzhi = fengshan5.fengshanzhuansu > 0 ? fengshan5.fengshanzhuansu.ToString("0", CultureInfo.InvariantCulture) + " RPM" : Formatters.Percent(fengshan5.baifenbi2);
                DrawText(huitu7, fengshan5.mingcheng9, _zhiziti2, wenben2, new RectangleF(hang6.Left + 12, hang6.Top + 2, hang6.Width - 110, hang6.Height - 4), Align.NearCenter);
                DrawText(huitu7, fengshanzhi, _zhiziti4, zise, new RectangleF(hang6.Right - 104, hang6.Top + 2, 92, hang6.Height - 4), Align.FarCenter);
                zong6 += 42;
            }
        }

        if (kuaizhao14.fengshan3.Count == 0)
        {
            DrawText(huitu7, shezhi7.Text("未检测到风扇", "No fans detected"), _zhiziti2, zhiwenben2, new RectangleF(quyu4.Left + 16, quyu4.Top + 90, quyu4.Width - 32, 24), Align.Center);
        }
    }

    private void DrawDisks(Graphics huitu8, Rectangle quyu5, SystemSnapshot kuaizhao15, AppSettings shezhi8)
    {
        DrawCard(huitu8, quyu5, shezhi8.Text("磁盘", "Disks"), bohelvse);
        var lieshu2 = quyu5.Width >= 780 ? 2 : 1;
        var cipanjiange = 14;
        var zhikuandu2 = (quyu5.Width - 36 - cipanjiange * (lieshu2 - 1)) / lieshu2;
        var zhizong = quyu5.Top + 64;

        for (var suoyin5 = 0; suoyin5 < kuaizhao15.cipan2.Count; suoyin5++)
        {
            var cipan3 = kuaizhao15.cipan2[suoyin5];
            var cipanliesuoyin = suoyin5 % lieshu2;
            var hang7 = suoyin5 / lieshu2;
            var cipanquyu = new Rectangle(quyu5.Left + 18 + cipanliesuoyin * (zhikuandu2 + cipanjiange), zhizong + hang7 * 92, zhikuandu2, 82);
            FillRound(huitu8, cipanquyu, 8, Color.FromArgb(108, Color.White));
            DrawRoundBorder(huitu8, cipanquyu, 8, Color.FromArgb(236, 236, 242));

            var mingchengquyu = new RectangleF(cipanquyu.Left + 12, cipanquyu.Top + 9, cipanquyu.Width - 24, 25);
            DrawText(huitu8, cipan3.cipanmingcheng, _zhiziti, wenben2, mingchengquyu, Align.NearCenter);
            DrawText(huitu8, cipan3.shiyonglvbaifenbi3.ToString("0.0", CultureInfo.InvariantCulture) + "%", _zhiziti4, bohelvse, mingchengquyu, Align.FarCenter);
            DrawProgress(huitu8, new Rectangle(cipanquyu.Left + 12, cipanquyu.Top + 39, cipanquyu.Width - 24, 7), Ease("disk" + suoyin5, cipan3.shiyonglvbaifenbi3));

            var xiangqing = shezhi8.zhiyingwen
                ? $"Total {Formatters.Size(cipan3.zongliangzhi4)}    Used {Formatters.Size(cipan3.yiyongzhi2)}    Free {Formatters.Size(cipan3.shengyuzhi2)}"
                : $"总量 {Formatters.Size(cipan3.zongliangzhi4)}    已用 {Formatters.Size(cipan3.yiyongzhi2)}    剩余 {Formatters.Size(cipan3.shengyuzhi2)}";
            DrawText(huitu8, xiangqing, _zhiziti3, zhiwenben, new RectangleF(cipanquyu.Left + 12, cipanquyu.Top + 55, cipanquyu.Width - 24, 20), Align.NearCenter);
        }
    }

    private void DrawNetwork(Graphics huitu9, Rectangle quyu6, SystemSnapshot kuaizhao16, AppSettings shezhi9)
    {
        DrawCard(huitu9, quyu6, shezhi9.Text("网络", "Network"), lanse);
        var zong7 = quyu6.Top + 64;
        var biaotou2 = new RectangleF(quyu6.Left + 18, zong7, quyu6.Width - 36, 22);
        DrawText(huitu9, shezhi9.Text("网卡", "Adapter"), _zhiziti3, zhiwenben2, biaotou2, Align.NearCenter);
        DrawText(huitu9, shezhi9.Text("下载        上传        总下载        总上传", "Down        Up        Total Down        Total Up"), _zhiziti3, zhiwenben2, biaotou2, Align.FarCenter);
        zong7 += 30;

        foreach (var wangluo2 in kuaizhao16.wangluo)
        {
            if (zong7 + 30 > quyu6.Bottom - 16) break;
            var hang8 = new RectangleF(quyu6.Left + 18, zong7, quyu6.Width - 36, 28);
            var you3 = $"{Formatters.Bytes(wangluo2.xiazaisudu2)}/s      {Formatters.Bytes(wangluo2.shangchuansudu2)}/s      {Formatters.Bytes(wangluo2.xiazaizijie2)}      {Formatters.Bytes(wangluo2.shangchuanzijie2)}";
            DrawText(huitu9, wangluo2.mingcheng10, _zhiziti, wenben2, hang8, Align.NearCenter);
            DrawText(huitu9, you3, _zhiziti4, wenben2, hang8, Align.FarCenter);
            zong7 += 36;
        }
    }

    private void DrawHeader(Graphics huitu10, Rectangle bianjie7, SystemSnapshot? kuaizhao17, AppSettings shezhi10, bool jiuxu2, bool shezhidakai3, bool quanping2, AppPage yemian)
    {
        var dingbu2 = new Rectangle(bianjie7.Left + zhikuandu, bianjie7.Top, bianjie7.Width - zhikuandu, zhigaodu);
        FillRound(huitu10, dingbu2, 0, Color.FromArgb(248, roufenbiaomian));
        using (var bi = new Pen(Color.FromArgb(150, hang3)))
        {
            huitu10.DrawLine(bi, bianjie7.Left + zhikuandu, zhigaodu - 1, bianjie7.Right, zhigaodu - 1);
        }

        var anniuzuo = ButtonRects(bianjie7)[WindowButton.Minimize].Left;
        var biaotizuo = zhikuandu + 24;
        DrawText(huitu10, shezhi10.Text("系统观测台", "System Observatory"), _biaotiziti, wenben2, new RectangleF(biaotizuo, 10, anniuzuo - biaotizuo - 18, 30), Align.NearCenter);

        var fubiaoti = yemian == AppPage.MemoryTools
            ? shezhi10.Text("轻量整理与维护", "Lightweight cleanup and maintenance")
            : yemian == AppPage.TokenMonitor
                ? shezhi10.Text("OpenClaw Token 图形化监控", "Graphical OpenClaw Token Monitor")
                : shezhi10.Text("正在读取硬件信息...", "Reading hardware information...");
        if (yemian == AppPage.Monitor && jiuxu2 && kuaizhao17 is not null)
        {
            var yunxingshijianwenben = Formatters.Uptime(kuaizhao17.yunxingshijianmiao, shezhi10.zhiyingwen);
            fubiaoti = shezhi10.zhiyingwen
                ? $"{kuaizhao17.diannaomingcheng} · {kuaizhao17.zhimingcheng} · {kuaizhao17.neihe} · Uptime {yunxingshijianwenben}"
                : $"{kuaizhao17.diannaomingcheng} · {kuaizhao17.zhimingcheng} · {kuaizhao17.neihe} · 运行 {yunxingshijianwenben}";
        }
        DrawText(huitu10, fubiaoti, _zhiziti3, zhiwenben2, new RectangleF(biaotizuo, 43, anniuzuo - biaotizuo - 18, 24), Align.NearCenter);
        if (kuaizhao17 is not null && kuaizhao17.zonggonghaowazhi >= 0)
        {
            DrawPowerBadge(huitu10, bianjie7, kuaizhao17, shezhi10);
        }
        DrawWindowButtons(huitu10, bianjie7, shezhi10, shezhidakai3, quanping2);
    }

    private void DrawPowerBadge(Graphics huitu10, Rectangle bianjie7, SystemSnapshot kuaizhao17, AppSettings shezhi10)
    {
        var anniuzuo = ButtonRects(bianjie7)[WindowButton.Minimize].Left;
        var wenben4 = shezhi10.Text("整机 ", "System ") + Formatters.Power(kuaizhao17.zonggonghaowazhi, kuaizhao17.zonggonghaogusuan);
        var kuandu8 = Math.Min(150, Math.Max(96, (int)huitu10.MeasureString(wenben4, _zhiziti3).Width + 30));
        var quyu19 = new Rectangle(anniuzuo - kuandu8 - 16, 23, kuandu8, 34);
        FillRound(huitu10, quyu19, 16, Color.FromArgb(238, tianlanbeijing));
        DrawRoundBorder(huitu10, quyu19, 16, Color.FromArgb(190, lanse));
        DrawText(huitu10, wenben4, _zhiziti3, lanse, quyu19, Align.Center);
    }

    private void DrawWindowButtons(Graphics huitu11, Rectangle bianjie8, AppSettings shezhi11, bool shezhidakai4, bool quanping3)
    {
        foreach (var anniupeizhi in ButtonRects(bianjie8))
        {
            var anniu4 = anniupeizhi.Key;
            if (anniu4 is WindowButton.Settings or WindowButton.MonitorHome or WindowButton.MemoryTools or WindowButton.OpenClawTokenMonitor) continue;
            var quyu7 = anniupeizhi.Value;
            var anniuxuanfu = _xuanfuanniu2 == anniu4;
            var anxia2 = _anxiaanniu2 == anniu4;
            var shezhixuanzhong = anniu4 == WindowButton.Settings && shezhidakai4;
            if (anniuxuanfu || anxia2)
            {
                var tianchong = anniu4 == WindowButton.Close
                    ? Color.FromArgb(anxia2 ? 255 : 230, 232, 17, 35)
                    : Color.FromArgb(anxia2 ? 116 : 82, 0, 0, 0);
                FillRound(huitu11, quyu7, 0, tianchong);
            }

            var tubiaoyanse = anniu4 == WindowButton.Close && anniuxuanfu ? Color.White : shezhixuanzhong ? fenhongyanse : Color.FromArgb(28, 28, 30);
            var zhongxin = new Point(quyu7.Left + quyu7.Width / 2, quyu7.Top + quyu7.Height / 2);
            if (anniu4 == WindowButton.Minimize)
            {
                using var bi2 = RoundPen(tubiaoyanse, 1.35f);
                huitu11.DrawLine(bi2, zhongxin.X - 5, zhongxin.Y + 6, zhongxin.X + 5, zhongxin.Y + 6);
            }
            else if (anniu4 == WindowButton.Fullscreen)
            {
                using var bi3 = RoundPen(tubiaoyanse, 1.35f);
                if (quanping3)
                {
                    huitu11.DrawRectangle(bi3, zhongxin.X - 2, zhongxin.Y - 6, 8, 8);
                    huitu11.DrawRectangle(bi3, zhongxin.X - 6, zhongxin.Y - 2, 8, 8);
                }
                else
                {
                    const int jiantoubianda = 6;
                    const int jiantoupianyi = 1;
                    huitu11.DrawLine(bi3, zhongxin.X - jiantoubianda, zhongxin.Y - jiantoupianyi, zhongxin.X - jiantoubianda, zhongxin.Y - jiantoubianda);
                    huitu11.DrawLine(bi3, zhongxin.X - jiantoubianda, zhongxin.Y - jiantoubianda, zhongxin.X - jiantoupianyi, zhongxin.Y - jiantoubianda);
                    huitu11.DrawLine(bi3, zhongxin.X + jiantoubianda, zhongxin.Y + jiantoupianyi, zhongxin.X + jiantoubianda, zhongxin.Y + jiantoubianda);
                    huitu11.DrawLine(bi3, zhongxin.X + jiantoupianyi, zhongxin.Y + jiantoubianda, zhongxin.X + jiantoubianda, zhongxin.Y + jiantoubianda);
                    huitu11.DrawLine(bi3, zhongxin.X + jiantoupianyi, zhongxin.Y - jiantoubianda, zhongxin.X + jiantoubianda, zhongxin.Y - jiantoubianda);
                    huitu11.DrawLine(bi3, zhongxin.X + jiantoubianda, zhongxin.Y - jiantoubianda, zhongxin.X + jiantoubianda, zhongxin.Y - jiantoupianyi);
                    huitu11.DrawLine(bi3, zhongxin.X - jiantoubianda, zhongxin.Y + jiantoupianyi, zhongxin.X - jiantoubianda, zhongxin.Y + jiantoubianda);
                    huitu11.DrawLine(bi3, zhongxin.X - jiantoubianda, zhongxin.Y + jiantoubianda, zhongxin.X - jiantoupianyi, zhongxin.Y + jiantoubianda);
                }
            }
            else if (anniu4 == WindowButton.Close)
            {
                using var bi4 = RoundPen(tubiaoyanse, 1.35f);
                huitu11.DrawLine(bi4, zhongxin.X - 5, zhongxin.Y - 5, zhongxin.X + 5, zhongxin.Y + 5);
                huitu11.DrawLine(bi4, zhongxin.X + 5, zhongxin.Y - 5, zhongxin.X - 5, zhongxin.Y + 5);
            }
        }
    }

    private Dictionary<WindowButton, Rectangle> ButtonRects(Rectangle bianjie9)
    {
        if (_huancunanniukuang is not null && bianjie9.Size == _huancunanniuchicun)
        {
            return _huancunanniukuang;
        }

        const int zhikuandu3 = 46;
        const int chuangkouanniugaodu = 42;
        var heng2 = bianjie9.Right - zhikuandu3;
        var guanbi = new Rectangle(heng2, 0, zhikuandu3, chuangkouanniugaodu);
        heng2 -= zhikuandu3;
        var quanping4 = new Rectangle(heng2, 0, zhikuandu3, chuangkouanniugaodu);
        heng2 -= zhikuandu3;
        var zuixiao = new Rectangle(heng2, 0, zhikuandu3, chuangkouanniugaodu);
        var zhuye = new Rectangle(bianjie9.Left + (zhikuandu - 42) / 2, 104, 42, 42);
        var neicungongju = new Rectangle(bianjie9.Left + (zhikuandu - 42) / 2, 156, 42, 42);
        var openclaw = new Rectangle(bianjie9.Left + (zhikuandu - 42) / 2, 208, 42, 42);
        var shezhi12 = new Rectangle(bianjie9.Left + (zhikuandu - 42) / 2, bianjie9.Bottom - 58, 42, 42);

        var jieguo = new Dictionary<WindowButton, Rectangle>
        {
            [WindowButton.Settings] = shezhi12,
            [WindowButton.MonitorHome] = zhuye,
            [WindowButton.MemoryTools] = neicungongju,
            [WindowButton.OpenClawTokenMonitor] = openclaw,
            [WindowButton.Fullscreen] = quanping4,
            [WindowButton.Minimize] = zuixiao,
            [WindowButton.Close] = guanbi
        };
        _huancunanniukuang = jieguo;
        _huancunanniuchicun = bianjie9.Size;
        return jieguo;
    }

    private void DrawLoading(Graphics huitu12, Rectangle bianjie10, AppSettings shezhi13)
    {
        var quyu8 = new RectangleF(zhikuandu + 32, zhigaodu + 48, bianjie10.Width - zhikuandu - 64, 42);
        DrawText(huitu12, shezhi13.Text("正在连接传感器，请稍候", "Connecting sensors, please wait"), _zhiziti, zhiwenben2, quyu8, Align.Center);
    }

    private void DrawSidebar(Graphics huitu13, Rectangle bianjie11, AppSettings shezhi13, bool shezhidakai5, bool neicunqinglizhong, AppPage yemian, AnimationSnapshot donghua)
    {
        var celanquyu = new Rectangle(bianjie11.Left, bianjie11.Top, zhikuandu, bianjie11.Height);
        FillRound(huitu13, celanquyu, 0, roufencelan);

        var anniujuhe = ButtonRects(bianjie11);
        var zhuyequyu = anniujuhe[WindowButton.MonitorHome];
        var neicungongjuquyu = anniujuhe[WindowButton.MemoryTools];
        var openclawquyu = anniujuhe[WindowButton.OpenClawTokenMonitor];
        var xuanzequyu = SelectedNavRect(zhuyequyu, neicungongjuquyu, openclawquyu, yemian, neicunqinglizhong, donghua);
        FillRound(huitu13, xuanzequyu, 12, roufenxuanze);
        var mubiaoyanse = yemian == AppPage.TokenMonitor
            ? zise
            : yemian == AppPage.MemoryTools || neicunqinglizhong
                ? bohelvse
                : fenhongyanse;
        var xuanzezhutiyanse = mubiaoyanse;
        if (donghua.yemianqiehuanjihuo)
        {
            var laiyuanyanse = donghua.laiyuanyemian == AppPage.TokenMonitor
                ? zise
                : donghua.laiyuanyemian == AppPage.MemoryTools
                    ? bohelvse
                    : fenhongyanse;
            xuanzezhutiyanse = Blend(laiyuanyanse, mubiaoyanse, donghua.yemianjieduan);
        }
        var xuanzezhishi = new Rectangle(bianjie11.Left + 7, xuanzequyu.Top + 10, 4, xuanzequyu.Height - 20);
        FillRound(huitu13, xuanzezhishi, 2, Color.FromArgb(190, xuanzezhutiyanse));

        var zhuyexuanfu = _xuanfuanniu2 == WindowButton.MonitorHome;
        var zhuyeanxia = _anxiaanniu2 == WindowButton.MonitorHome;
        if (zhuyexuanfu || zhuyeanxia)
        {
            var tianchong = roufenhover;
            if (zhuyeanxia) tianchong = roufenxuanze;
            FillRound(huitu13, zhuyequyu, 12, tianchong);
        }
        DrawMonitorHomeIcon(huitu13, zhuyequyu, yemian == AppPage.Monitor ? fenhongyanse : Color.FromArgb(42, 42, 46));
        if (zhuyexuanfu) DrawTooltip(huitu13, zhuyequyu, shezhi13.Text("监控主页", "Monitor"));

        var neicungongjuxuanfu = _xuanfuanniu2 == WindowButton.MemoryTools;
        var neicungongjuanxia = _anxiaanniu2 == WindowButton.MemoryTools;
        if (neicungongjuxuanfu || neicungongjuanxia)
        {
            var tianchong = roufenhover;
            if (neicungongjuanxia) tianchong = roufenxuanze;
            FillRound(huitu13, neicungongjuquyu, 12, tianchong);
        }
        DrawToolboxIcon(huitu13, neicungongjuquyu, yemian == AppPage.MemoryTools || neicunqinglizhong ? bohelvse : Color.FromArgb(42, 42, 46), neicunqinglizhong ? (float)(donghua.gongzuobomai * 18d - 9d) : 0f);
        if (neicungongjuxuanfu) DrawTooltip(huitu13, neicungongjuquyu, shezhi13.Text("工具栏", "Toolbox"));

        var openclawxuanfu = _xuanfuanniu2 == WindowButton.OpenClawTokenMonitor;
        var openclawanxia = _anxiaanniu2 == WindowButton.OpenClawTokenMonitor;
        if (openclawxuanfu || openclawanxia)
        {
            var tianchong3 = roufenhover;
            if (openclawanxia) tianchong3 = roufenxuanze;
            FillRound(huitu13, openclawquyu, 12, tianchong3);
        }
        DrawOpenClawTokenIcon(huitu13, openclawquyu, yemian == AppPage.TokenMonitor || openclawxuanfu ? zise : Color.FromArgb(42, 42, 46));
        if (openclawxuanfu) DrawTooltip(huitu13, openclawquyu, shezhi13.Text("Token监控", "Token Monitor"));

        var shezhiquyu = anniujuhe[WindowButton.Settings];
        var shezhixuanfu = _xuanfuanniu2 == WindowButton.Settings;
        var anxia3 = _anxiaanniu2 == WindowButton.Settings;
        if (shezhidakai5 || shezhixuanfu || anxia3)
        {
            var tianchong2 = shezhidakai5 ? roufenxuanze : roufenhover;
            if (anxia3) tianchong2 = Color.FromArgb(208, 211, 238);
            FillRound(huitu13, shezhiquyu, 12, tianchong2);
            if (shezhidakai5) DrawRoundBorder(huitu13, shezhiquyu, 12, Color.FromArgb(188, fenhongyanse));
        }

        var yanse = shezhidakai5 ? fenhongyanse : Color.FromArgb(42, 42, 46);
        DrawGear(huitu13, shezhiquyu.Left + shezhiquyu.Width / 2, shezhiquyu.Top + shezhiquyu.Height / 2, yanse);
    }

    private void DrawToast(Graphics huitu13, Rectangle bianjie11, string wenben4, double jieduan)
    {
        jieduan = UiAnimation.Clamp01(jieduan);
        if (jieduan <= 0.01) return;
        var kuandu8 = Math.Min(440, Math.Max(280, bianjie11.Width - zhikuandu - 72));
        var quyu19 = new Rectangle(bianjie11.Left + zhikuandu + (bianjie11.Width - zhikuandu - kuandu8) / 2, bianjie11.Bottom - 74, kuandu8, 44);
        quyu19.Offset(0, (int)((1d - jieduan) * 18d));
        var yinying3 = quyu19;
        yinying3.Offset(0, 6);
        FillRound(huitu13, yinying3, 12, Color.FromArgb((int)(22 * jieduan), 60, 64, 72));
        FillRound(huitu13, quyu19, 12, Color.FromArgb((int)(248 * jieduan), Color.White));
        DrawRoundBorder(huitu13, quyu19, 12, Color.FromArgb((int)(224 * jieduan), Color.FromArgb(228, 228, 234)));
        DrawText(huitu13, wenben4, _zhiziti2, Color.FromArgb((int)(255 * jieduan), wenben2), new RectangleF(quyu19.Left + 16, quyu19.Top + 6, quyu19.Width - 32, quyu19.Height - 12), Align.Center);
    }

    private void DrawTooltip(Graphics huitu13, Rectangle anniuquyu, string wenben4)
    {
        var quyu19 = new Rectangle(anniuquyu.Right + 8, anniuquyu.Top + 5, 92, 32);
        FillRound(huitu13, quyu19, 8, Color.FromArgb(248, Color.White));
        DrawRoundBorder(huitu13, quyu19, 8, Color.FromArgb(224, 228, 234));
        DrawText(huitu13, wenben4, _zhiziti3, wenben2, quyu19, Align.Center);
    }

    private void DrawSettingsPanel(Graphics huitu14, Rectangle bianjie12, AppSettings shezhi14, double jieduan)
    {
        jieduan = Math.Clamp(jieduan, 0, 1);
        var huadong = UiAnimation.EaseOutBack(jieduan);
        var mianban2 = SettingsPanelRect(bianjie12);
        mianban2.Offset(0, (int)((1 - jieduan) * 18));

        FillRound(huitu14, bianjie12, 0, Color.FromArgb((int)(28 * jieduan), roufenbeijing));
        var zhuangtai = huitu14.Save();
        var suofang = (float)(0.965d + 0.035d * huadong);
        var zhongxin = new PointF(mianban2.Left + mianban2.Width / 2f, mianban2.Top + mianban2.Height / 2f);
        huitu14.TranslateTransform(zhongxin.X, zhongxin.Y);
        huitu14.ScaleTransform(suofang, suofang);
        huitu14.TranslateTransform(-zhongxin.X, -zhongxin.Y);
        var yinying = mianban2;
        yinying.Offset(0, 12);
        FillRound(huitu14, yinying, 18, Color.FromArgb((int)(30 * jieduan), 63, 81, 181));
        FillRound(huitu14, mianban2, 18, Color.FromArgb((int)(224 * jieduan), roufenbiaomian));
        DrawRoundBorder(huitu14, mianban2, 18, roufenkapianbiankuang);

        DrawText(huitu14, shezhi14.Text("设置", "Settings"), _zhiziti, wenben2, new RectangleF(mianban2.Left + 24, mianban2.Top + 18, mianban2.Width - 48, 28), Align.NearCenter);
        DrawText(huitu14, shezhi14.Text("语言", "Language"), _zhiziti3, zhiwenben2, new RectangleF(mianban2.Left + 24, mianban2.Top + 56, mianban2.Width - 48, 20), Align.NearCenter);

        var yuyan3 = SettingsLanguageRect(mianban2);
        FillRound(huitu14, yuyan3, 18, Color.FromArgb(238, roufenhover));
        DrawRoundBorder(huitu14, yuyan3, 18, roufenkapianbiankuang);
        var zhongjian = yuyan3.Left + yuyan3.Width / 2;
        DrawPill(huitu14, new Rectangle(yuyan3.Left + 4, yuyan3.Top + 4, zhongjian - yuyan3.Left - 7, yuyan3.Height - 8), "中文", !shezhi14.zhiyingwen);
        DrawPill(huitu14, new Rectangle(zhongjian + 3, yuyan3.Top + 4, yuyan3.Right - zhongjian - 7, yuyan3.Height - 8), "English", shezhi14.zhiyingwen);

        DrawText(huitu14, shezhi14.Text("背景图片", "Background Image"), _zhiziti3, zhiwenben2, new RectangleF(mianban2.Left + 24, mianban2.Top + 144, mianban2.Width - 48, 20), Align.NearCenter);
        var zhuangtai3 = shezhi14.beijingtupian is null ? shezhi14.Text("未设置背景图片", "No background image") : shezhi14.Text("已启用自定义背景", "Custom background enabled");
        DrawText(huitu14, zhuangtai3, _zhiziti2, zhiwenben, new RectangleF(mianban2.Left + 24, mianban2.Top + 166, mianban2.Width - 48, 20), Align.NearCenter);

        DrawPill(huitu14, SettingsPickBackgroundRect(mianban2), shezhi14.Text("选择图片", "Choose"), false);
        DrawPill(huitu14, SettingsClearBackgroundRect(mianban2), shezhi14.Text("清除", "Clear"), false);

        DrawText(huitu14, shezhi14.Text("系统行为", "System Behavior"), _zhiziti3, zhiwenben2, new RectangleF(mianban2.Left + 24, mianban2.Top + 250, mianban2.Width - 48, 20), Align.NearCenter);
        DrawSettingToggleRow(huitu14, AutostartToggleRect(mianban2), shezhi14.Text("开机自启动", "Start With Windows"), shezhi14.Text("登录 Windows 后自动打开系统观测台", "Open System Observatory after Windows sign-in"), shezhi14.kaijiqidong);
        DrawSettingToggleRow(huitu14, CloseToTrayToggleRect(mianban2), shezhi14.Text("关闭后放入系统托盘", "Close To System Tray"), shezhi14.Text("点击关闭时隐藏窗口，托盘图标可恢复或退出", "Hide on close; tray icon can restore or exit"), shezhi14.guanbihoutuopan);

        DrawText(huitu14, shezhi14.Text("远程同步", "Remote Sync"), _zhiziti3, zhiwenben2, new RectangleF(mianban2.Left + 24, mianban2.Top + 380, mianban2.Width - 48, 20), Align.NearCenter);
        DrawSettingToggleRow(huitu14, RelaySyncToggleRect(mianban2), shezhi14.Text("允许 Android 连接", "Allow Android to connect"), shezhi14.Text("内嵌服务器，配合 MSLFrp 暴露公网", "Built-in server, use MSLFrp for public access"), shezhi14.relayEnabled);
        DrawRelayConfigButton(huitu14, RelaySyncConfigRect(mianban2), shezhi14);

        DrawText(huitu14, RelayHintText(shezhi14), _zhiziti3, zhiwenben2, new RectangleF(mianban2.Left + 24, mianban2.Bottom - 26, mianban2.Width - 48, 20), Align.NearCenter);
        huitu14.Restore(zhuangtai);
    }

    private void DrawAnimatedCard(Graphics huitu15, Rectangle quyu9, int suoyin, MonitorCard kapian, AnimationSnapshot donghua, Action<Graphics, Rectangle> huizhi)
    {
        var yidong = MoveForStagger(quyu9, donghua, suoyin);
        huizhi(huitu15, yidong);

        var jieduan = UiAnimation.Stagger(donghua.neirongjieduan, suoyin);
        if (jieduan < 0.985d)
        {
            FillRound(huitu15, yidong, 12, Color.FromArgb((int)(118 * (1d - jieduan)), Color.White));
        }

        DrawHoverHighlight(huitu15, yidong, _xuanfukapian2 == kapian);
    }

    private static Rectangle MoveForStagger(Rectangle quyu9, AnimationSnapshot donghua, int suoyin)
    {
        var jieduan = UiAnimation.Stagger(donghua.neirongjieduan, suoyin);
        quyu9.Offset(0, (int)((1d - jieduan) * 18d));
        return quyu9;
    }

    private void DrawHoverHighlight(Graphics huitu15, Rectangle quyu9, bool xuanfu)
    {
        if (!xuanfu) return;

        var neiqu = new Rectangle(quyu9.Left + 1, quyu9.Top + 1, quyu9.Width - 3, quyu9.Height - 3);
        using var lujing = RoundedPath(neiqu, 12);
        using var bi = RoundPen(Color.FromArgb(150, fenhongyanse), 1.7f);
        huitu15.DrawPath(bi, lujing);
    }

    private static Rectangle SelectedNavRect(Rectangle zhuyequyu, Rectangle neicungongjuquyu, Rectangle openclawquyu, AppPage yemian, bool neicunqinglizhong, AnimationSnapshot donghua)
    {
        static Rectangle Quyu(AppPage yemian2, Rectangle zhuye2, Rectangle neicun2, Rectangle token2)
        {
            return yemian2 == AppPage.TokenMonitor ? token2 : yemian2 == AppPage.MemoryTools ? neicun2 : zhuye2;
        }

        if (donghua.yemianqiehuanjihuo)
        {
            var laiyuan = Quyu(donghua.laiyuanyemian, zhuyequyu, neicungongjuquyu, openclawquyu);
            var mubiao = Quyu(donghua.mubiaoyemian, zhuyequyu, neicungongjuquyu, openclawquyu);
            return new Rectangle(
                mubiao.Left,
                (int)Math.Round(UiAnimation.Lerp(laiyuan.Top, mubiao.Top, donghua.yemianjieduan)),
                mubiao.Width,
                mubiao.Height);
        }

        return neicunqinglizhong ? neicungongjuquyu : Quyu(yemian, zhuyequyu, neicungongjuquyu, openclawquyu);
    }

    private static void ApplyOpenTransform(Graphics huitu15, Rectangle bianjie12, double jieduan)
    {
        jieduan = UiAnimation.Clamp01(jieduan);
        if (jieduan >= 0.999d) return;

        var suofang = (float)(0.96d + 0.04d * jieduan);
        var zhongxin = new PointF(bianjie12.Left + bianjie12.Width / 2f, bianjie12.Top + bianjie12.Height / 2f);
        huitu15.TranslateTransform(0f, (float)((1d - jieduan) * 14d));
        huitu15.TranslateTransform(zhongxin.X, zhongxin.Y);
        huitu15.ScaleTransform(suofang, suofang);
        huitu15.TranslateTransform(-zhongxin.X, -zhongxin.Y);
    }

    private static void DrawOpeningWash(Graphics huitu15, Rectangle bianjie12, double jieduan)
    {
        jieduan = UiAnimation.Clamp01(jieduan);
        if (jieduan >= 0.999d) return;
        FillRound(huitu15, bianjie12, 0, Color.FromArgb((int)(105 * (1d - jieduan)), Color.White));
    }

    private void DrawPill(Graphics huitu15, Rectangle quyu9, string biaoqian2, bool kaiqizhi)
    {
        FillRound(huitu15, quyu9, 16, kaiqizhi ? Color.FromArgb(245, roufenanniu) : Color.FromArgb(220, roufenbiaomian));
        DrawRoundBorder(huitu15, quyu9, 16, kaiqizhi ? Color.FromArgb(210, fenhongyanse) : roufenkapianbiankuang);
        DrawText(huitu15, biaoqian2, _anniuziti, kaiqizhi ? fenhongyanse : zhiwenben, quyu9, Align.Center);
    }

    private void DrawSettingToggleRow(Graphics huitu15, Rectangle quyu9, string biaoti, string miaoshu, bool kaiqi)
    {
        FillRound(huitu15, quyu9, 12, Color.FromArgb(136, roufenbiaomian));
        DrawRoundBorder(huitu15, quyu9, 12, roufenkapianbiankuang);
        DrawText(huitu15, biaoti, _zhiziti3, wenben2, new RectangleF(quyu9.Left + 12, quyu9.Top + 3, quyu9.Width - 84, 18), Align.NearCenter);
        DrawText(huitu15, miaoshu, _anniuziti, zhiwenben2, new RectangleF(quyu9.Left + 12, quyu9.Top + 21, quyu9.Width - 84, 17), Align.NearCenter);
        DrawSwitch(huitu15, new Rectangle(quyu9.Right - 62, quyu9.Top + 8, 48, 26), kaiqi);
    }

    private void DrawRelayConfigButton(Graphics huitu15, Rectangle quyu9, AppSettings shezhi14)
    {
        FillRound(huitu15, quyu9, 12, Color.FromArgb(136, roufenbiaomian));
        DrawRoundBorder(huitu15, quyu9, 12, roufenkapianbiankuang);
        var zhuangtai = string.IsNullOrWhiteSpace(shezhi14.relayUrl)
            ? shezhi14.Text("未配置公网地址", "Public URL is not set")
            : ShortText(shezhi14.relayUrl, 31);
        DrawText(huitu15, shezhi14.Text("配置地址", "Configure URL"), _zhiziti3, wenben2, new RectangleF(quyu9.Left + 12, quyu9.Top + 4, 92, quyu9.Height - 8), Align.NearCenter);
        DrawText(huitu15, zhuangtai, _anniuziti, zhiwenben2, new RectangleF(quyu9.Left + 106, quyu9.Top + 4, quyu9.Width - 118, quyu9.Height - 8), Align.FarCenter);
    }

    private static string RelayHintText(AppSettings shezhi14)
    {
        if (!shezhi14.relayEnabled) return shezhi14.Text("远程同步未开启", "Remote sync is off");
        if (string.IsNullOrWhiteSpace(shezhi14.relayUrl)) return shezhi14.Text("请先填写公网地址", "Set the public URL first");
        return shezhi14.Text("Android 可通过公网地址实时连接", "Android can connect in real time via public URL");
    }

    private static string PortStatusText(AppSettings shezhi14, EmbeddedServerState zhuangtai)
    {
        return zhuangtai switch
        {
            EmbeddedServerState.Running => shezhi14.Text("8787 已开启", "8787 open"),
            EmbeddedServerState.Failed => shezhi14.Text("启动失败", "Start failed"),
            _ => shezhi14.Text("8787 未开启", "8787 closed")
        };
    }

    private static string PortPromptText(AppSettings shezhi14, EmbeddedServerState zhuangtai, string tishi)
    {
        if (zhuangtai == EmbeddedServerState.Running)
        {
            return shezhi14.Text(
                "端口 8787 已开启；点设备密钥区域自动复制，手机填写公网地址和密钥即可连接。",
                "Port 8787 is open; click the device-key area to copy, then enter the URL and key on Android.");
        }

        if (zhuangtai == EmbeddedServerState.Failed)
        {
            var xiangqing = string.IsNullOrWhiteSpace(tishi) ? shezhi14.Text("请重新开启 Android 连接", "enable Android Link again") : ShortText(tishi, 34);
            return shezhi14.Text("端口 8787 未开启：" + xiangqing, "Port 8787 is not open: " + xiangqing);
        }

        return shezhi14.relayEnabled
            ? shezhi14.Text("端口 8787 未开启，请重新开启 Android 连接。", "Port 8787 is not open; enable Android Link again.")
            : shezhi14.Text("开启后会检查并监听 8787 端口。", "When enabled, the app checks and listens on port 8787.");
    }

    private static string ShortText(string wenben3, int changdu)
    {
        if (wenben3.Length <= changdu) return wenben3;
        return wenben3[..Math.Max(0, changdu - 3)] + "...";
    }

    private static void DrawSwitch(Graphics huitu15, Rectangle quyu9, bool kaiqi)
    {
        FillRound(huitu15, quyu9, quyu9.Height / 2, kaiqi ? roufenanniu : Color.FromArgb(220, 235, 226, 232));
        DrawRoundBorder(huitu15, quyu9, quyu9.Height / 2, kaiqi ? roufenbiankuang : roufenkapianbiankuang);

        var zhijing = quyu9.Height - 8;
        var hengxiang = kaiqi ? quyu9.Right - zhijing - 4 : quyu9.Left + 4;
        var dian = new Rectangle(hengxiang, quyu9.Top + 4, zhijing, zhijing);
        FillRound(huitu15, dian, zhijing / 2, Color.White);
        DrawRoundBorder(huitu15, dian, zhijing / 2, Color.FromArgb(34, 126, 130, 150));
    }

    private void DrawCard(Graphics huitu16, Rectangle quyu10, string biaoti, Color zhutiyanse)
    {
        var yinying2 = quyu10;
        yinying2.Offset(1, 9);
        var kapianbeijing = zhutiyanse.ToArgb() == lanse.ToArgb()
            ? Color.FromArgb(224, tianlanbeijing)
            : zhutiyanse.ToArgb() == bohelvse.ToArgb()
                ? Color.FromArgb(224, bohelvbeijing)
                : zhutiyanse.ToArgb() == zise.ToArgb()
                    ? Color.FromArgb(224, dianzibeijing)
                    : roufenkapian;
        FillRound(huitu16, yinying2, 12, Color.FromArgb(18, zhutiyanse));
        FillRound(huitu16, quyu10, 12, kapianbeijing);
        var dingbutouguang = new Rectangle(quyu10.Left + 1, quyu10.Top + 1, quyu10.Width - 2, Math.Min(58, quyu10.Height - 2));
        FillRound(huitu16, dingbutouguang, 12, Color.FromArgb(34, zhutiyanse));
        var cebianzhishi = new Rectangle(quyu10.Left + 14, quyu10.Top + 17, 4, 28);
        FillRound(huitu16, cebianzhishi, 2, Color.FromArgb(190, zhutiyanse));
        DrawRoundBorder(huitu16, quyu10, 12, Color.FromArgb(74, zhutiyanse));

        var tubiao = new Rectangle(quyu10.Left + 24, quyu10.Top + 18, 26, 26);
        FillRound(huitu16, tubiao, 7, Color.FromArgb(145, Color.White));
        using (var huabi = new SolidBrush(Color.FromArgb(38, zhutiyanse)))
        {
            huitu16.FillEllipse(huabi, tubiao.Left + 8, tubiao.Top + 8, 10, 10);
        }
        using (var bi5 = RoundPen(zhutiyanse, 1.6f))
        {
            huitu16.DrawRectangle(bi5, tubiao.Left + 8, tubiao.Top + 8, 10, 10);
            huitu16.DrawLine(bi5, tubiao.Left + 11, tubiao.Top + 12, tubiao.Right - 11, tubiao.Top + 12);
        }

        DrawText(huitu16, biaoti, _zhiziti, wenben2, new RectangleF(quyu10.Left + 60, quyu10.Top + 16, quyu10.Width - 78, 30), Align.NearCenter);
    }

    private void DrawStat(Graphics huitu17, Rectangle quyu11, string shuzhizhi, string biaoqian3)
    {
        FillRound(huitu17, quyu11, 8, Color.FromArgb(106, Color.White));
        DrawRoundBorder(huitu17, quyu11, 8, Color.FromArgb(237, 237, 243));
        DrawText(huitu17, shuzhizhi, _zhiziti4, wenben2, new RectangleF(quyu11.Left + 12, quyu11.Top + 6, quyu11.Width - 20, 20), Align.NearCenter);
        DrawText(huitu17, biaoqian3, _zhiziti3, zhiwenben2, new RectangleF(quyu11.Left + 12, quyu11.Top + 27, quyu11.Width - 20, 18), Align.NearCenter);
    }

    private void DrawTokenStat(Graphics huitu17, Rectangle quyu11, string shuzhizhi, string biaoqian3)
    {
        FillRound(huitu17, quyu11, 8, Color.FromArgb(116, Color.White));
        DrawRoundBorder(huitu17, quyu11, 8, Color.FromArgb(236, 236, 244));
        DrawText(huitu17, string.IsNullOrWhiteSpace(shuzhizhi) ? "-" : shuzhizhi, _zhiziti4, wenben2, new RectangleF(quyu11.Left + 12, quyu11.Top + 6, quyu11.Width - 20, 22), Align.NearCenter);
        DrawText(huitu17, biaoqian3, _zhiziti3, zhiwenben2, new RectangleF(quyu11.Left + 12, quyu11.Top + 30, quyu11.Width - 20, 18), Align.NearCenter);
    }

    private void DrawTokenPlainSectionTitle(Graphics huitu17, Rectangle quyu11, string biaoti)
    {
        DrawText(huitu17, biaoti, _zhiziti, wenben2, new RectangleF(quyu11.Left + 54, quyu11.Top + 16, quyu11.Width - 72, 30), Align.NearCenter);
    }

    private void DrawTokenPlainStat(Graphics huitu17, Rectangle quyu11, string shuzhizhi, string biaoqian3)
    {
        DrawText(huitu17, string.IsNullOrWhiteSpace(shuzhizhi) ? "-" : shuzhizhi, _zhiziti4, wenben2, new RectangleF(quyu11.Left + 12, quyu11.Top + 6, quyu11.Width - 20, 22), Align.NearCenter);
        DrawText(huitu17, biaoqian3, _zhiziti3, zhiwenben2, new RectangleF(quyu11.Left + 12, quyu11.Top + 30, quyu11.Width - 20, 18), Align.NearCenter);
    }

    private void DrawStatusPill(Graphics huitu17, Rectangle quyu11, string wenben4, Color yanse)
    {
        FillRound(huitu17, quyu11, 15, Color.FromArgb(38, yanse));
        DrawRoundBorder(huitu17, quyu11, 15, Color.FromArgb(96, yanse));
        using (var huabi = new SolidBrush(yanse))
        {
            huitu17.FillEllipse(huabi, quyu11.Left + 12, quyu11.Top + 11, 8, 8);
        }
        DrawText(huitu17, wenben4, _zhiziti3, yanse, new RectangleF(quyu11.Left + 26, quyu11.Top + 3, quyu11.Width - 34, quyu11.Height - 6), Align.NearCenter);
    }

    private static string TokenNumber(double shuzhizhi, string danwei)
    {
        return double.IsFinite(shuzhizhi)
            ? shuzhizhi.ToString("0.00", CultureInfo.InvariantCulture) + danwei
            : "-";
    }

    private static string GpuMemoryUsageText(GpuInfo? xianqia)
    {
        if (xianqia is null) return "-";
        if (xianqia.zhizongliangzhi > 0)
        {
            var baifenbi = Math.Clamp(xianqia.zhiyiyongzhi * 100d / xianqia.zhizongliangzhi, 0d, 100d);
            return Formatters.Percent(baifenbi) + "  " + Formatters.Size(xianqia.zhiyiyongzhi) + "/" + Formatters.Size(xianqia.zhizongliangzhi);
        }

        return xianqia.zhiyiyongzhi > 0 ? Formatters.Size(xianqia.zhiyiyongzhi) : "-";
    }

    private static string MemoryUsageText(MemInfo? neicun)
    {
        if (neicun is null) return "-";
        if (neicun.zongliangzhi3 > 0)
        {
            return Formatters.Percent(neicun.shiyonglvbaifenbi2) + "  " + Formatters.Size(neicun.yiyongzhi) + "/" + Formatters.Size(neicun.zongliangzhi3);
        }

        return neicun.yiyongzhi > 0 ? Formatters.Size(neicun.yiyongzhi) : "-";
    }

    private static Dictionary<DateOnly, TokenDailyUsage> BuildTokenActivityMap(TokenMonitorSnapshot tokenkuaizhao)
    {
        var zidian = new Dictionary<DateOnly, TokenDailyUsage>();
        var huodong = tokenkuaizhao.huodongriqi;
        if (huodong is null) return zidian;
        foreach (var dangri in huodong)
        {
            var riqi = ParseTokenDate(dangri.riqi);
            if (riqi is not null) zidian[riqi.Value] = dangri;
        }

        return zidian;
    }

    private static Dictionary<DateOnly, int> BuildTokenActivityValues(Dictionary<DateOnly, TokenDailyUsage> zidian, TokenActivityMode huodongmoshi)
    {
        if (huodongmoshi == TokenActivityMode.Weekly)
        {
            return BuildWeeklyColumnActivityValues(zidian);
        }

        var zhiquyu = new Dictionary<DateOnly, int>();
        var (kaishi, _) = TokenActivityDateRange(huodongmoshi);
        for (var suoyin = 0; suoyin < tokenhuodongfangkuaishu; suoyin++)
        {
            var riqi = kaishi.AddDays(suoyin);
            zhiquyu[riqi] = TokenAmountForDay(zidian, riqi).zongtokens;
        }

        return zhiquyu;
    }

    private static Dictionary<DateOnly, int> BuildWeeklyColumnActivityValues(Dictionary<DateOnly, TokenDailyUsage> zidian)
    {
        var zhiquyu = new Dictionary<DateOnly, int>();
        var (kaishi, _) = WeeklyTokenActivityDateRange();
        var liezongliang = new int[tokenhuodonglieshu];
        for (var lie = 0; lie < tokenhuodonglieshu; lie++)
        {
            var liekaishi = kaishi.AddDays(lie * tokenhuodonghangshu);
            liezongliang[lie] = AggregateHeatmapColumn(zidian, liekaishi).zongtokens;
        }

        var zuidazhou = Math.Max(1, liezongliang.DefaultIfEmpty(0).Max());
        for (var lie = 0; lie < tokenhuodonglieshu; lie++)
        {
            var zhouzongliang = liezongliang[lie];
            var tianchongshu = WeeklyColumnFillCount(zhouzongliang, zuidazhou);
            for (var hang = 0; hang < tokenhuodonghangshu; hang++)
            {
                var riqi = kaishi.AddDays(lie * tokenhuodonghangshu + hang);
                zhiquyu[riqi] = hang >= tokenhuodonghangshu - tianchongshu ? zhouzongliang : 0;
            }
        }

        return zhiquyu;
    }

    private static int WeeklyColumnFillCount(int zhouzongliang, int zuidazhou)
    {
        if (zhouzongliang <= 0 || zuidazhou <= 0) return 0;
        var bili = Math.Clamp(zhouzongliang / (double)zuidazhou, 0d, 1d);
        return Math.Clamp((int)Math.Round(bili * tokenhuodonghangshu, MidpointRounding.AwayFromZero), 1, tokenhuodonghangshu);
    }

    private static string FormatActivityTooltipText(Dictionary<DateOnly, TokenDailyUsage> zidian, DateOnly riqi, TokenActivityMode huodongmoshi, AppSettings shezhi17)
    {
        return huodongmoshi switch
        {
            TokenActivityMode.Weekly => shezhi17.zhiyingwen
                ? $"{FormatDateFull(ColumnStartForDate(riqi), shezhi17)} used {FormatTokenCompact(AggregateHeatmapColumn(zidian, riqi).zongtokens)} tokens this column-week"
                : $"{FormatDateFull(ColumnStartForDate(riqi), shezhi17)} 当周使用了 {FormatTokenCompact(AggregateHeatmapColumn(zidian, riqi).zongtokens)} 个 Token",
            _ => shezhi17.zhiyingwen
                ? $"{FormatDateFull(riqi, shezhi17)} used {FormatTokenCompact(TokenAmountForDay(zidian, riqi).zongtokens)} tokens that day"
                : $"{FormatDateFull(riqi, shezhi17)} 当日使用了 {FormatTokenCompact(TokenAmountForDay(zidian, riqi).zongtokens)} 个 Token"
        };
    }

    private static TokenUsageAmount AggregateHeatmapColumn(Dictionary<DateOnly, TokenDailyUsage> zidian, DateOnly riqi)
    {
        var kaishi = ColumnStartForDate(riqi);
        var shuru = 0;
        var shuchu = 0;
        for (var suoyin = 0; suoyin < tokenhuodonghangshu; suoyin++)
        {
            var dangri = TokenAmountForDay(zidian, kaishi.AddDays(suoyin));
            shuru += dangri.shurutokens;
            shuchu += dangri.shuchutokens;
        }

        return new TokenUsageAmount(shuru, shuchu);
    }

    private static TokenUsageAmount AggregateWeek(Dictionary<DateOnly, TokenDailyUsage> zidian, DateOnly riqi)
    {
        var kaishi = WeekStart(riqi);
        var shuru = 0;
        var shuchu = 0;
        for (var suoyin = 0; suoyin < 7; suoyin++)
        {
            var dangri = TokenAmountForDay(zidian, kaishi.AddDays(suoyin));
            shuru += dangri.shurutokens;
            shuchu += dangri.shuchutokens;
        }

        return new TokenUsageAmount(shuru, shuchu);
    }

    private static TokenUsageAmount TokenAmountForDay(Dictionary<DateOnly, TokenDailyUsage> zidian, DateOnly riqi)
    {
        return zidian.TryGetValue(riqi, out var dangri)
            ? new TokenUsageAmount(Math.Max(0, dangri.shurutokens), Math.Max(0, dangri.shuchutokens))
            : new TokenUsageAmount(0, 0);
    }

    private static DateOnly WeekStart(DateOnly riqi)
    {
        var pianyi = riqi.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)riqi.DayOfWeek - (int)DayOfWeek.Monday;
        return riqi.AddDays(-pianyi);
    }

    private static DateOnly ColumnStartForDate(DateOnly riqi)
    {
        var (kaishi, _) = WeeklyTokenActivityDateRange();
        var pianyi = Math.Clamp(riqi.DayNumber - kaishi.DayNumber, 0, tokenhuodongfangkuaishu - 1);
        return kaishi.AddDays(pianyi / tokenhuodonghangshu * tokenhuodonghangshu);
    }

    private static bool IsWeeklyColumnHovered(DateOnly kaishi, int suoyin, string xuanfuriqi)
    {
        var xuanfuriqi2 = ParseTokenDate(xuanfuriqi);
        if (xuanfuriqi2 is null) return false;
        var xuanfupianyi = xuanfuriqi2.Value.DayNumber - kaishi.DayNumber;
        if (xuanfupianyi < 0 || xuanfupianyi >= tokenhuodongfangkuaishu) return false;
        return xuanfupianyi / tokenhuodonghangshu == suoyin / tokenhuodonghangshu;
    }

    private static string FormatTokenPlain(int tokenshuliang)
    {
        return tokenshuliang > 0 ? tokenshuliang.ToString("N0", CultureInfo.InvariantCulture) : "-";
    }

    private static string FormatTokenCompact(int tokenshuliang)
    {
        if (tokenshuliang <= 0) return "0";
        if (tokenshuliang >= 100_000_000)
        {
            return (tokenshuliang / 100_000_000d).ToString(tokenshuliang % 100_000_000 == 0 ? "0" : "0.#", CultureInfo.InvariantCulture) + "亿";
        }

        if (tokenshuliang >= 10_000)
        {
            return (tokenshuliang / 10_000d).ToString(tokenshuliang % 10_000 == 0 ? "0" : "0.#", CultureInfo.InvariantCulture) + "万";
        }

        return tokenshuliang.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatDurationCompact(double miaoshu)
    {
        if (!double.IsFinite(miaoshu) || miaoshu <= 0) return "0秒";
        var zongmiao = Math.Max(1, (int)Math.Round(miaoshu));
        if (zongmiao < 60) return zongmiao.ToString(CultureInfo.InvariantCulture) + "秒";
        if (zongmiao < 3600)
        {
            return (zongmiao / 60).ToString(CultureInfo.InvariantCulture) + "分" + (zongmiao % 60).ToString("00", CultureInfo.InvariantCulture) + "秒";
        }

        return (zongmiao / 3600).ToString(CultureInfo.InvariantCulture) + "小时" + (zongmiao % 3600 / 60).ToString(CultureInfo.InvariantCulture) + "分";
    }

    private static string FormatDurationMaybe(double miaoshu)
    {
        return double.IsFinite(miaoshu) && miaoshu > 0 ? FormatDurationCompact(miaoshu) : "-";
    }

    private static string FormatFirstTokenLatency(double miaoshu, bool gusuan)
    {
        if (!double.IsFinite(miaoshu) || miaoshu <= 0) return "-";
        var wenben3 = FormatDurationCompact(miaoshu);
        return gusuan ? "≈" + wenben3 : wenben3;
    }

    private static string FormatDays(int tianshu, AppSettings shezhi17)
    {
        return shezhi17.zhiyingwen
            ? Math.Max(0, tianshu).ToString(CultureInfo.InvariantCulture) + " d"
            : Math.Max(0, tianshu).ToString(CultureInfo.InvariantCulture) + "天";
    }

    private static string FormatTokenAmount(TokenUsageAmount dangqianzhi, AppSettings shezhi17)
    {
        return shezhi17.zhiyingwen
            ? $"Input {FormatTokenZero(dangqianzhi.shurutokens)} · Output {FormatTokenZero(dangqianzhi.shuchutokens)} · Total {FormatTokenZero(dangqianzhi.zongtokens)}"
            : $"输入 {FormatTokenZero(dangqianzhi.shurutokens)} · 输出 {FormatTokenZero(dangqianzhi.shuchutokens)} · 总计 {FormatTokenZero(dangqianzhi.zongtokens)}";
    }

    private static string FormatTokenZero(int tokenshuliang)
    {
        return Math.Max(0, tokenshuliang).ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatDateFull(DateOnly riqi, AppSettings shezhi17)
    {
        return shezhi17.zhiyingwen
            ? riqi.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : riqi.ToString("yyyy年M月d日", CultureInfo.InvariantCulture);
    }

    private static string FormatDateShort(DateOnly riqi, AppSettings shezhi17)
    {
        return shezhi17.zhiyingwen
            ? riqi.ToString("MM-dd", CultureInfo.InvariantCulture)
            : riqi.ToString("M月d日", CultureInfo.InvariantCulture);
    }

    private static Color HeatColor(int tokenshuliang, TokenActivityMode huodongmoshi)
    {
        var dangqianzhi = Math.Max(0, tokenshuliang);
        var yuzhi = huodongmoshi == TokenActivityMode.Weekly ? tokenmeizhoureduyuzhi : tokenmeirireduyuzhi;
        for (var suoyin = yuzhi.Length - 1; suoyin >= 0; suoyin--)
        {
            if (dangqianzhi >= yuzhi[suoyin])
            {
                return tokenreduyanse[Math.Min(suoyin + 1, tokenreduyanse.Length - 1)];
            }
        }

        return tokenreduyanse[0];
    }

    private static Color Blend(Color kaishi, Color jieshu, double jieduan)
    {
        jieduan = UiAnimation.Clamp01(jieduan);
        return Color.FromArgb(
            (int)Math.Round(UiAnimation.Lerp(kaishi.A, jieshu.A, jieduan)),
            (int)Math.Round(UiAnimation.Lerp(kaishi.R, jieshu.R, jieduan)),
            (int)Math.Round(UiAnimation.Lerp(kaishi.G, jieshu.G, jieduan)),
            (int)Math.Round(UiAnimation.Lerp(kaishi.B, jieshu.B, jieduan)));
    }

    private static string MonthLabel(DateOnly riqi, AppSettings shezhi17)
    {
        return shezhi17.zhiyingwen
            ? riqi.ToString("MMM", CultureInfo.InvariantCulture)
            : riqi.Month.ToString(CultureInfo.InvariantCulture) + "月";
    }

    private static Rectangle TokenActivityTabRect(Rectangle quyu, TokenActivityMode huodongmoshi)
    {
        var kuan = quyu.Width < 620 ? 42 : 48;
        var jiange = 4;
        var zuo = quyu.Right - 18 - kuan * tokenhuodongbiaoqianshu - jiange * (tokenhuodongbiaoqianshu - 1);
        var suoyin = huodongmoshi switch
        {
            TokenActivityMode.Weekly => 1,
            _ => 0
        };
        return new Rectangle(zuo + suoyin * (kuan + jiange), quyu.Top + 15, kuan, 26);
    }

    private static (DateOnly kaishi, DateOnly jieshu) TokenActivityDateRange(TokenActivityMode huodongmoshi)
    {
        return huodongmoshi == TokenActivityMode.Weekly
            ? WeeklyTokenActivityDateRange()
            : DailyTokenActivityDateRange();
    }

    private static (DateOnly kaishi, DateOnly jieshu) DailyTokenActivityDateRange()
    {
        var jieshu = DateOnly.FromDateTime(DateTime.Now);
        return (jieshu.AddDays(-(tokenhuodongfangkuaishu - 1)), jieshu);
    }

    private static (DateOnly kaishi, DateOnly jieshu) WeeklyTokenActivityDateRange()
    {
        var benzhoukaishi = WeekStart(DateOnly.FromDateTime(DateTime.Now));
        var kaishi = benzhoukaishi.AddDays(-((tokenhuodonglieshu - 1) * tokenhuodonghangshu));
        return (kaishi, kaishi.AddDays(tokenhuodongfangkuaishu - 1));
    }

    private static (Rectangle wanggequyu, int fangkuai, int jiange, int zuo, int ding) TokenHeatmapGeometry(Rectangle quyu)
    {
        var wanggequyu = new Rectangle(quyu.Left + 18, quyu.Top + 55, quyu.Width - 36, quyu.Height - 86);
        var jiange = wanggequyu.Width >= 680 ? 4 : 3;
        var fangkuai = Math.Clamp(Math.Min((wanggequyu.Width - jiange * (tokenhuodonglieshu - 1)) / tokenhuodonglieshu, (wanggequyu.Height - jiange * (tokenhuodonghangshu - 1)) / tokenhuodonghangshu), 5, 12);
        var shijiwidth = tokenhuodonglieshu * fangkuai + (tokenhuodonglieshu - 1) * jiange;
        var shijigaodu = tokenhuodonghangshu * fangkuai + (tokenhuodonghangshu - 1) * jiange;
        var zuo = wanggequyu.Left + Math.Max(0, (wanggequyu.Width - shijiwidth) / 2);
        var ding = wanggequyu.Top + Math.Max(0, (wanggequyu.Height - shijigaodu) / 2);
        return (wanggequyu, fangkuai, jiange, zuo, ding);
    }

    private static Rectangle TokenHeatmapCellRect((Rectangle wanggequyu, int fangkuai, int jiange, int zuo, int ding) jihe, int suoyin, TokenActivityMode huodongmoshi)
    {
        return huodongmoshi == TokenActivityMode.Weekly
            ? WeeklyTokenHeatmapCellRect(jihe, suoyin)
            : DailyTokenHeatmapCellRect(jihe, suoyin);
    }

    private static Rectangle DailyTokenHeatmapCellRect((Rectangle wanggequyu, int fangkuai, int jiange, int zuo, int ding) jihe, int suoyin)
    {
        var liehao = suoyin / tokenhuodonghangshu;
        var hanghao = suoyin % tokenhuodonghangshu;
        return new Rectangle(
            jihe.zuo + liehao * (jihe.fangkuai + jihe.jiange),
            jihe.ding + hanghao * (jihe.fangkuai + jihe.jiange),
            jihe.fangkuai,
            jihe.fangkuai);
    }

    private static Rectangle WeeklyTokenHeatmapCellRect((Rectangle wanggequyu, int fangkuai, int jiange, int zuo, int ding) jihe, int suoyin)
    {
        var liehao = suoyin / tokenhuodonghangshu;
        var hanghao = suoyin % tokenhuodonghangshu;
        return new Rectangle(
            jihe.zuo + liehao * (jihe.fangkuai + jihe.jiange),
            jihe.ding + hanghao * (jihe.fangkuai + jihe.jiange),
            jihe.fangkuai,
            jihe.fangkuai);
    }

    private static string TokenDateKey(DateOnly riqi)
    {
        return riqi.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParseTokenDate(string riqi)
    {
        return DateOnly.TryParseExact(riqi, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var jieguo)
            ? jieguo
            : null;
    }

    private void DrawGauge(Graphics huitu18, Point zhongxin2, int banjing, double baifenbi3, Color yuanhuanyanse)
    {
        baifenbi3 = Math.Clamp(baifenbi3, 0, 100);
        var quyu12 = new Rectangle(zhongxin2.X - banjing, zhongxin2.Y - banjing, banjing * 2, banjing * 2);
        using (var beijingbi = RoundPen(Color.FromArgb(178, 232, 232, 239), 7.5f))
        {
            huitu18.DrawEllipse(beijingbi, quyu12);
        }
        if (baifenbi3 > 0.1)
        {
            using var bi6 = RoundPen(yuanhuanyanse, 7.5f);
            huitu18.DrawArc(bi6, quyu12, -90, (float)(baifenbi3 * 3.6));
        }

        var zhiziti = YibiaopanZiti(Math.Min(26f, banjing * 0.5f));
        var zhiquyu = new RectangleF(zhongxin2.X - banjing, zhongxin2.Y - 31, banjing * 2, 34);
        var danweiquyu = new RectangleF(zhongxin2.X - banjing, zhongxin2.Y + 5, banjing * 2, 18);
        DrawText(huitu18, baifenbi3.ToString("0", CultureInfo.InvariantCulture), zhiziti, yuanhuanyanse, zhiquyu, Align.Center);
        DrawText(huitu18, "%", _zhiziti3, zhiwenben2, danweiquyu, Align.Center);
    }

    private static Font YibiaopanZiti(float daxiao)
    {
        if (_yibiaopanziti.TryGetValue(daxiao, out var cunzhit))
        {
            return cunzhit;
        }
        var xinziti = new Font("Segoe UI", daxiao, FontStyle.Bold);
        _yibiaopanziti[daxiao] = xinziti;
        return xinziti;
    }

    private void DrawProgress(Graphics huitu19, Rectangle quyu13, double baifenbi4)
    {
        baifenbi4 = Math.Clamp(baifenbi4, 0, 100);
        FillRound(huitu19, quyu13, quyu13.Height, Color.FromArgb(237, 240, 250));
        var tianchong3 = quyu13;
        tianchong3.Width = Math.Max(quyu13.Height, (int)(quyu13.Width * baifenbi4 / 100d));
        if (tianchong3.Width <= 0) return;
        FillRound(huitu19, tianchong3, quyu13.Height, roufenjindutiao);
    }

    private void DrawBackground(Graphics huitu20, Rectangle bianjie13, AppSettings shezhi15)
    {
        using var waike = new SolidBrush(roufenbeijing);
        huitu20.FillRectangle(waike, bianjie13);
    }

    private void DrawMainSurface(Graphics huitu21, Rectangle bianjie14, AppSettings shezhi16)
    {
        var zhuyebiaomian = MainSurfaceRect(bianjie14);
        if (zhuyebiaomian.Width <= 0 || zhuyebiaomian.Height <= 0) return;

        using var lujing6 = MainSurfacePath(zhuyebiaomian);
        if (shezhi16.beijingtupian is not null)
        {
            var zhuangtai4 = huitu21.Save();
            huitu21.SetClip(lujing6);
            DrawCoverImage(huitu21, zhuyebiaomian, shezhi16.beijingtupian);
            using var fugai = new LinearGradientBrush(zhuyebiaomian, Color.FromArgb(204, 255, 255, 255), Color.FromArgb(188, 248, 250, 252), LinearGradientMode.Vertical);
            huitu21.FillRectangle(fugai, zhuyebiaomian);
            huitu21.Restore(zhuangtai4);
        }
        else
        {
            using var baitianchong = new SolidBrush(roufenbiaomian);
            huitu21.FillPath(baitianchong, lujing6);
        }

    }

    private static Rectangle MainSurfaceRect(Rectangle bianjie15)
    {
        return new Rectangle(bianjie15.Left + zhikuandu, bianjie15.Top + zhigaodu, bianjie15.Width - zhikuandu, bianjie15.Height - zhigaodu);
    }

    private static Point ScrolledPoint(Point zuobiao4, int gundongzong)
    {
        return new Point(zuobiao4.X, zuobiao4.Y + Math.Max(0, gundongzong));
    }

    private readonly record struct TokenUsageAmount(int shurutokens, int shuchutokens)
    {
        public int zongtokens => Math.Max(0, shurutokens) + Math.Max(0, shuchutokens);
    }

    private static GraphicsPath MainSurfacePath(Rectangle quyu14)
    {
        var banjing2 = Math.Min(zhubiaomianbanjing, Math.Min(quyu14.Width, quyu14.Height) / 2);
        var zhijing = banjing2 * 2;
        var lujing7 = new GraphicsPath();
        lujing7.StartFigure();
        lujing7.AddArc(quyu14.Left, quyu14.Top, zhijing, zhijing, 180, 90);
        lujing7.AddLine(quyu14.Left + banjing2, quyu14.Top, quyu14.Right, quyu14.Top);
        lujing7.AddLine(quyu14.Right, quyu14.Top, quyu14.Right, quyu14.Bottom);
        lujing7.AddLine(quyu14.Right, quyu14.Bottom, quyu14.Left, quyu14.Bottom);
        lujing7.AddLine(quyu14.Left, quyu14.Bottom, quyu14.Left, quyu14.Top + banjing2);
        lujing7.CloseFigure();
        return lujing7;
    }

    private static void DrawCoverImage(Graphics huitu22, Rectangle bianjie16, Image tupian)
    {
        var suofang = Math.Max(bianjie16.Width / (double)tupian.Width, bianjie16.Height / (double)tupian.Height);
        var kuandu6 = (int)Math.Ceiling(tupian.Width * suofang);
        var gaodu2 = (int)Math.Ceiling(tupian.Height * suofang);
        var heng3 = bianjie16.Left + (bianjie16.Width - kuandu6) / 2;
        var zong8 = bianjie16.Top + (bianjie16.Height - gaodu2) / 2;
        huitu22.DrawImage(tupian, new Rectangle(heng3, zong8, kuandu6, gaodu2));
    }

    private double Ease(string jian5, double mubiao3)
    {
        if (!_huandongzhi.TryGetValue(jian5, out var dangqian))
        {
            dangqian = mubiao3;
        }
        var xishu = 1d - Math.Exp(-_bencihuizhijiange / 65d);
        dangqian += (mubiao3 - dangqian) * xishu;
        if (Math.Abs(mubiao3 - dangqian) < 0.05) dangqian = mubiao3;
        _huandongzhi[jian5] = dangqian;
        return dangqian;
    }

    private static string FanLabel(string leixing4, AppSettings shezhi17)
    {
        return leixing4 switch
        {
            "cpu" => shezhi17.Text("CPU 风扇", "CPU Fan"),
            "gpu" => shezhi17.Text("GPU 风扇", "GPU Fan"),
            "system" => shezhi17.Text("系统风扇", "System Fan"),
            _ => shezhi17.Text("其它风扇", "Other Fans")
        };
    }

    private static Pen RoundPen(Color yanse2, float kuandu7)
    {
        return new Pen(yanse2, kuandu7)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
    }

    private static void FillRound(Graphics huitu23, Rectangle quyu15, int banjing3, Color yanse3)
    {
        using var huabi3 = new SolidBrush(yanse3);
        if (banjing3 <= 0)
        {
            huitu23.FillRectangle(huabi3, quyu15);
            return;
        }
        using var lujing8 = RoundedPath(quyu15, banjing3);
        huitu23.FillPath(huabi3, lujing8);
    }

    private static void DrawRoundBorder(Graphics huitu24, Rectangle quyu16, int banjing4, Color yanse4)
    {
        using var bi7 = new Pen(yanse4, 1f);
        var biankuangquyu = new Rectangle(quyu16.Left, quyu16.Top, quyu16.Width - 1, quyu16.Height - 1);
        using var lujing9 = RoundedPath(biankuangquyu, banjing4);
        huitu24.DrawPath(bi7, lujing9);
    }

    private static GraphicsPath RoundedPath(Rectangle quyu17, int banjing5)
    {
        var lujing10 = new GraphicsPath();
        if (banjing5 <= 0)
        {
            lujing10.AddRectangle(quyu17);
            return lujing10;
        }

        var zhijing2 = banjing5 * 2;
        lujing10.AddArc(quyu17.Left, quyu17.Top, zhijing2, zhijing2, 180, 90);
        lujing10.AddArc(quyu17.Right - zhijing2, quyu17.Top, zhijing2, zhijing2, 270, 90);
        lujing10.AddArc(quyu17.Right - zhijing2, quyu17.Bottom - zhijing2, zhijing2, zhijing2, 0, 90);
        lujing10.AddArc(quyu17.Left, quyu17.Bottom - zhijing2, zhijing2, zhijing2, 90, 90);
        lujing10.CloseFigure();
        return lujing10;
    }

    private static void DrawGear(Graphics huitu25, int zhongxinhengxiang, int zhongxinzongxiang, Color yanse5)
    {
        using var bi8 = RoundPen(yanse5, 1.5f);
        var chilunzuobiao = new PointF[24];
        for (var suoyin6 = 0; suoyin6 < chilunzuobiao.Length; suoyin6++)
        {
            var jiaodu = Math.PI * 2 * suoyin6 / chilunzuobiao.Length - Math.PI / 2;
            var banjing6 = suoyin6 % 2 == 0 ? 9.2 : 6.8;
            chilunzuobiao[suoyin6] = new PointF(zhongxinhengxiang + (float)Math.Cos(jiaodu) * (float)banjing6, zhongxinzongxiang + (float)Math.Sin(jiaodu) * (float)banjing6);
        }
        huitu25.DrawPolygon(bi8, chilunzuobiao);
        huitu25.DrawEllipse(bi8, zhongxinhengxiang - 3.4f, zhongxinzongxiang - 3.4f, 6.8f, 6.8f);
    }

    private static void DrawMonitorHomeIcon(Graphics huitu25, Rectangle quyu19, Color yanse5)
    {
        var zhongxin = new Point(quyu19.Left + quyu19.Width / 2, quyu19.Top + quyu19.Height / 2);
        using var bi = RoundPen(yanse5, 1.55f);
        var waikuang = new Rectangle(zhongxin.X - 10, zhongxin.Y - 8, 20, 15);
        huitu25.DrawRectangle(bi, waikuang);
        huitu25.DrawLine(bi, zhongxin.X - 5, zhongxin.Y + 11, zhongxin.X + 5, zhongxin.Y + 11);
        huitu25.DrawLine(bi, zhongxin.X, zhongxin.Y + 7, zhongxin.X, zhongxin.Y + 11);
        huitu25.DrawLine(bi, zhongxin.X - 6, zhongxin.Y + 1, zhongxin.X - 2, zhongxin.Y - 3);
        huitu25.DrawLine(bi, zhongxin.X - 2, zhongxin.Y - 3, zhongxin.X + 1, zhongxin.Y + 2);
        huitu25.DrawLine(bi, zhongxin.X + 1, zhongxin.Y + 2, zhongxin.X + 6, zhongxin.Y - 4);
    }

    private static void DrawMemoryCleanupIcon(Graphics huitu25, Rectangle quyu19, Color yanse5, float xuanzhuanjiaodu = 0f)
    {
        var zhongxin3 = new Point(quyu19.Left + quyu19.Width / 2, quyu19.Top + quyu19.Height / 2);
        var zhuangtai = huitu25.Save();
        if (Math.Abs(xuanzhuanjiaodu) > 0.01f)
        {
            huitu25.TranslateTransform(zhongxin3.X, zhongxin3.Y);
            huitu25.RotateTransform(xuanzhuanjiaodu);
            huitu25.TranslateTransform(-zhongxin3.X, -zhongxin3.Y);
        }

        using var bi9 = RoundPen(yanse5, 1.55f);
        var quyu20 = new Rectangle(zhongxin3.X - 9, zhongxin3.Y - 9, 18, 18);
        huitu25.DrawArc(bi9, quyu20, -72, 292);

        var jiaodu2 = 220d * Math.PI / 180d;
        var jiantou = new PointF(zhongxin3.X + (float)Math.Cos(jiaodu2) * 9f, zhongxin3.Y + (float)Math.Sin(jiaodu2) * 9f);
        huitu25.DrawLine(bi9, jiantou.X, jiantou.Y, jiantou.X + 1f, jiantou.Y - 5f);
        huitu25.DrawLine(bi9, jiantou.X, jiantou.Y, jiantou.X + 5f, jiantou.Y - 1f);
        huitu25.DrawLine(bi9, zhongxin3.X - 3, zhongxin3.Y + 1, zhongxin3.X + 3, zhongxin3.Y + 1);
        huitu25.DrawLine(bi9, zhongxin3.X, zhongxin3.Y - 2, zhongxin3.X, zhongxin3.Y + 4);
        huitu25.Restore(zhuangtai);
    }

    private static void DrawToolboxIcon(Graphics huitu25, Rectangle quyu19, Color yanse5, float xuanzhuanjiaodu = 0f)
    {
        var zhongxin3 = new Point(quyu19.Left + quyu19.Width / 2, quyu19.Top + quyu19.Height / 2);
        var zhuangtai = huitu25.Save();
        if (Math.Abs(xuanzhuanjiaodu) > 0.01f)
        {
            huitu25.TranslateTransform(zhongxin3.X, zhongxin3.Y);
            huitu25.RotateTransform(xuanzhuanjiaodu);
            huitu25.TranslateTransform(-zhongxin3.X, -zhongxin3.Y);
        }

        using var bi9 = RoundPen(yanse5, 1.55f);
        var xiangzi = new Rectangle(zhongxin3.X - 10, zhongxin3.Y - 7, 20, 15);
        using (var lujing = RoundedPath(xiangzi, 4))
        {
            huitu25.DrawPath(bi9, lujing);
        }

        huitu25.DrawLine(bi9, zhongxin3.X - 5, zhongxin3.Y - 9, zhongxin3.X + 5, zhongxin3.Y - 9);
        huitu25.DrawLine(bi9, zhongxin3.X - 5, zhongxin3.Y - 9, zhongxin3.X - 5, zhongxin3.Y - 6);
        huitu25.DrawLine(bi9, zhongxin3.X + 5, zhongxin3.Y - 9, zhongxin3.X + 5, zhongxin3.Y - 6);
        huitu25.DrawLine(bi9, zhongxin3.X - 6, zhongxin3.Y + 1, zhongxin3.X + 6, zhongxin3.Y + 1);
        using var huabi = new SolidBrush(Color.FromArgb(45, yanse5));
        huitu25.FillEllipse(huabi, zhongxin3.X - 3, zhongxin3.Y - 2, 6, 6);
        huitu25.Restore(zhuangtai);
    }

    private static void DrawOpenClawTokenIcon(Graphics huitu25, Rectangle quyu19, Color yanse5)
    {
        var zhongxin = new PointF(quyu19.Left + quyu19.Width / 2f, quyu19.Top + quyu19.Height / 2f);
        var shang = new PointF(zhongxin.X, zhongxin.Y - 10f);
        var zuo = new PointF(zhongxin.X - 10f, zhongxin.Y + 5f);
        var you = new PointF(zhongxin.X + 10f, zhongxin.Y + 5f);

        using var bi = RoundPen(yanse5, 1.55f);
        huitu25.DrawLine(bi, shang, zuo);
        huitu25.DrawLine(bi, shang, you);
        huitu25.DrawLine(bi, zuo, you);
        huitu25.DrawEllipse(bi, zhongxin.X - 5f, zhongxin.Y - 5f, 10f, 10f);

        using var huabi = new SolidBrush(Color.FromArgb(42, yanse5));
        huitu25.FillEllipse(huabi, shang.X - 3.4f, shang.Y - 3.4f, 6.8f, 6.8f);
        huitu25.FillEllipse(huabi, zuo.X - 3.4f, zuo.Y - 3.4f, 6.8f, 6.8f);
        huitu25.FillEllipse(huabi, you.X - 3.4f, you.Y - 3.4f, 6.8f, 6.8f);
        huitu25.DrawEllipse(bi, shang.X - 3.4f, shang.Y - 3.4f, 6.8f, 6.8f);
        huitu25.DrawEllipse(bi, zuo.X - 3.4f, zuo.Y - 3.4f, 6.8f, 6.8f);
        huitu25.DrawEllipse(bi, you.X - 3.4f, you.Y - 3.4f, 6.8f, 6.8f);
    }

    private static void DrawAndroidLinkIcon(Graphics huitu25, Rectangle quyu19, Color yanse5)
    {
        var zhongxin = new Point(quyu19.Left + quyu19.Width / 2, quyu19.Top + quyu19.Height / 2);
        using var bi = RoundPen(yanse5, 1.55f);
        var shouji = new Rectangle(zhongxin.X - 6, zhongxin.Y - 10, 12, 20);
        using (var lujing = RoundedPath(shouji, 4))
        {
            huitu25.DrawPath(bi, lujing);
        }

        using (var huabi = new SolidBrush(Color.FromArgb(42, yanse5)))
        {
            huitu25.FillEllipse(huabi, zhongxin.X - 1, zhongxin.Y + 6, 2, 2);
        }

        var zuolian = new Rectangle(zhongxin.X - 17, zhongxin.Y - 6, 8, 12);
        var youlian = new Rectangle(zhongxin.X + 9, zhongxin.Y - 6, 8, 12);
        huitu25.DrawArc(bi, zuolian, 300, 120);
        huitu25.DrawArc(bi, youlian, 120, 120);
        huitu25.DrawLine(bi, zhongxin.X - 10, zhongxin.Y, zhongxin.X - 6, zhongxin.Y);
        huitu25.DrawLine(bi, zhongxin.X + 6, zhongxin.Y, zhongxin.X + 10, zhongxin.Y);
    }

    private static void DrawText(Graphics huitu26, string wenben3, Font ziti, Color yanse6, RectangleF quyu18, Align duiqifangshi)
    {
        using var huabi4 = new SolidBrush(yanse6);
        var geshi = duiqifangshi is Align.Center ? _geshizhong : duiqifangshi is Align.FarCenter ? _geshiyuan : _geshijin;
        huitu26.DrawString(wenben3, ziti, huabi4, quyu18, geshi);
    }

    public void Dispose()
    {
        _biaotiziti.Dispose();
        _zhiziti.Dispose();
        _zhiziti2.Dispose();
        _zhiziti3.Dispose();
        _zhiziti4.Dispose();
        _anniuziti.Dispose();
        _geshijin.Dispose();
        _geshizhong.Dispose();
        _geshiyuan.Dispose();
        foreach (var ziti in _yibiaopanziti.Values) ziti.Dispose();
        _yibiaopanziti.Clear();
    }

    private enum Align
    {
        NearCenter,
        Center,
        FarCenter
    }
}
