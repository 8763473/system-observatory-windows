using System.Runtime.InteropServices;
using HwMonitor.Hardware;
using HwMonitor.Models;
using HwMonitor.Settings;
using HwMonitor.Utilities;

namespace HwMonitor.UI;

public sealed class MainForm : Form
{
    private const int shuaxinjiangehaomiao = 3000;
    private const int donghuajiangehaomiao = 24;
    private const int suofangbiankuangkuandu = 8;
    private const int qidongdonghuahaomiao = 850;
    private const int yemianqiehuanhaomiao = 560;
    private const int shuaxinhaomiao = 760;

    private readonly HardwareCollector _yingjiancaijiqi = new();
    private readonly MonitorRenderer _xuanranqi = new();
    private readonly System.Windows.Forms.Timer _shuaxindingshiqi = new();
    private readonly System.Windows.Forms.Timer _donghuadingshiqi = new();
    private readonly System.Windows.Forms.Timer _tokenjiankongshuaxin = new();
    private readonly System.Windows.Forms.Timer _deepseekshuaxin = new();
    private readonly CancellationTokenSource _guanbizhi = new();
    private readonly NotifyIcon _tuopan = new();
    private readonly ContextMenuStrip _tuopancaidan = new();
    private readonly AppSettings _shezhi;
    private readonly EmbeddedServer _neiqianfuwuqi = new();

    private SystemSnapshot? _kuaizhao;
    private bool _jiuxu;
    private bool _caiji;
    private double _gundongdangqian;
    private double _gundongmubiao;
    private bool _shezhidakai;
    private bool _quanping;
    private bool _neicunqinglizhong;
    private string _neicunqinglitishi = "";
    private string _neicunqinglijieguo = "";
    private DateTime _neicunqinglitishijieshu;
    private AppPage _yemian = AppPage.Monitor;
    private AppPage _qiehuanlaiyuan = AppPage.Monitor;
    private AppPage _qiehuanmubiao = AppPage.Monitor;
    private TokenMonitorSnapshot _tokenjiankongkuaizhao = OpenClawTokenMonitorLauncher.Snapshot();
    private TokenActivityMode _tokenhuodongmoshi = TokenActivityMode.Daily;
    private string _tokenhuodongxuanfuriqi = "";
    private bool _tokenrizhidaoruzhong;
    private DateTime _xiacitokenrizhidaoru = DateTime.MinValue;
    private DeepSeekBalanceInfo _deepseekyuexinxi = new();
    private Rectangle _chuangkoubianjie;
    private FormWindowState _chuangkouzhuangtai = FormWindowState.Normal;
    private double _shezhijieduan;
    private DateTime _donghuajieshu;
    private DateTime _qidongkaishi = DateTime.UtcNow;
    private DateTime _shuaxinkaishi = DateTime.MinValue;
    private DateTime _yemianqiehuankaishi = DateTime.MinValue;
    private DateTime _neicunqinglitishikaishi = DateTime.MinValue;
    private WindowButton _xuanfuanniu = WindowButton.None;
    private WindowButton _anxiaanniu = WindowButton.None;
    private MonitorCard _xuanfukapian = MonitorCard.None;
    private bool _zhengzaituichu;
    private bool _yitishituopan;
    private DateTime _shangcishubiaoshi = DateTime.MinValue;
    private DateTime _shangcidonghuashi = DateTime.MinValue;

    protected override CreateParams CreateParams
    {
        get
        {
            const int yinyingyangshi = 0x00020000;
            const int biaotiyangshi = 0x00C00000;
            const int suofangyangshi = 0x00040000;
            const int caidanyangshi = 0x00080000;
            const int zuixiaohuayangshi = 0x00020000;
            var chuangjiancanshu = base.CreateParams;
            chuangjiancanshu.ClassStyle |= yinyingyangshi;
            chuangjiancanshu.Style |= biaotiyangshi | suofangyangshi | caidanyangshi | zuixiaohuayangshi;
            return chuangjiancanshu;
        }
    }

    public MainForm()
    {
        _shezhi = AppSettings.Load();
        Text = "系统观测台";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(760, 620);
        Size = new Size(1120, 980);
        BackColor = Color.FromArgb(245, 247, 250);
        KeyPreview = true;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        _shuaxindingshiqi.Interval = shuaxinjiangehaomiao;
        _shuaxindingshiqi.Tick += async (bianliang, bianliang2) => await RefreshSnapshotAsync();
        _donghuadingshiqi.Interval = donghuajiangehaomiao;
        _donghuadingshiqi.Tick += (bianliang3, bianliang4) => TickAnimation();
        _tokenjiankongshuaxin.Interval = 3000;
        _tokenjiankongshuaxin.Tick += (bianliang5, bianliang6) => RefreshTokenMonitorSnapshot();
        _deepseekshuaxin.Interval = 5000;
        _deepseekshuaxin.Tick += async (_, _) => await RefreshDeepSeekBalanceAsync();

        InitializeTrayIcon();
    }

    protected override async void OnLoad(EventArgs shijian2)
    {
        base.OnLoad(shijian2);
        _qidongkaishi = DateTime.UtcNow;
        _shuaxindingshiqi.Start();
        _deepseekshuaxin.Start();
        StartAnimation(qidongdonghuahaomiao);
        if (_shezhi.relayEnabled)
        {
            StartEmbeddedServer();
        }
        await RefreshSnapshotAsync();
        _ = ImportTokenLogsAsync(xianshitishi: false);
    }

    protected override void OnHandleCreated(EventArgs shijian3)
    {
        base.OnHandleCreated(shijian3);
        ApplyDwmChrome();
    }

    protected override void OnPaint(PaintEventArgs shijian4)
    {
        base.OnPaint(shijian4);
        _xuanranqi.SetButtonState(_xuanfuanniu, _anxiaanniu);
        _xuanranqi.SetHoverCard(_xuanfukapian);
        _xuanranqi.SetTokenActivityState(_tokenhuodongmoshi, _tokenhuodongxuanfuriqi);
        _xuanranqi.SetEmbeddedServerState(_neiqianfuwuqi.zhuangtai, _neiqianfuwuqi.zhuangtaiwenben, _neiqianfuwuqi.suidaoURL);
        _xuanranqi.SetDeepSeekBalance(_deepseekyuexinxi);
        _xuanranqi.Draw(shijian4.Graphics, ClientRectangle, _kuaizhao, _tokenjiankongkuaizhao, _jiuxu, _shezhi, _shezhidakai, _quanping, _neicunqinglizhong, _neicunqinglitishi, _neicunqinglijieguo, _yemian, BuildAnimationSnapshot(DateTime.UtcNow));
    }

    protected override void OnResize(EventArgs shijian5)
    {
        base.OnResize(shijian5);
        ClampScroll();
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs shijian6)
    {
        base.OnMouseMove(shijian6);
        var xianzai = DateTime.UtcNow;
        if ((xianzai - _shangcishubiaoshi).TotalMilliseconds < 14) return;
        _shangcishubiaoshi = xianzai;
        var xuanfu = _xuanranqi.HitButton(ClientRectangle, shijian6.Location);
        var xuanfukapian = xuanfu == WindowButton.None && !_shezhidakai
            ? _xuanranqi.HitMonitorCard(ClientRectangle, _kuaizhao, _yemian, (int)Math.Round(_gundongdangqian), shijian6.Location)
            : MonitorCard.None;
        var tokenriqi = "";
        var tokenkejiaohu = false;
        if (_yemian == AppPage.TokenMonitor && !_shezhidakai)
        {
            var gundong = (int)Math.Round(_gundongdangqian);
            tokenriqi = _xuanranqi.HitTokenActivityDay(ClientRectangle, gundong, _tokenjiankongkuaizhao, shijian6.Location, _tokenhuodongmoshi);
            tokenkejiaohu = tokenriqi.Length > 0 || _xuanranqi.HitTokenActivityMode(ClientRectangle, gundong, shijian6.Location) is not null;
        }

        Cursor = tokenkejiaohu ? Cursors.Hand : Cursors.Default;
        if (xuanfu != _xuanfuanniu || xuanfukapian != _xuanfukapian || tokenriqi != _tokenhuodongxuanfuriqi)
        {
            _xuanfuanniu = xuanfu;
            _xuanfukapian = xuanfukapian;
            _tokenhuodongxuanfuriqi = tokenriqi;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs shijian7)
    {
        base.OnMouseLeave(shijian7);
        Cursor = Cursors.Default;
        if (_xuanfuanniu != WindowButton.None || _xuanfukapian != MonitorCard.None)
        {
            _xuanfuanniu = WindowButton.None;
            _xuanfukapian = MonitorCard.None;
            _tokenhuodongxuanfuriqi = "";
            Invalidate();
        }
        else if (_tokenhuodongxuanfuriqi.Length > 0)
        {
            _tokenhuodongxuanfuriqi = "";
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs shijian8)
    {
        base.OnMouseDown(shijian8);
        if (shijian8.Button != MouseButtons.Left) return;
        var anniu = _xuanranqi.HitButton(ClientRectangle, shijian8.Location);
        if (anniu != WindowButton.None)
        {
            _anxiaanniu = anniu;
            Capture = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs shijian9)
    {
        base.OnMouseUp(shijian9);
        if (shijian9.Button != MouseButtons.Left) return;
        var zhianxia = _anxiaanniu;
        _anxiaanniu = WindowButton.None;
        Capture = false;

        var anniu2 = _xuanranqi.HitButton(ClientRectangle, shijian9.Location);
        if (zhianxia != WindowButton.None)
        {
            if (anniu2 == zhianxia) ActivateButton(anniu2);
            Invalidate();
            return;
        }

        var caozuo = _xuanranqi.HitSettings(ClientRectangle, shijian9.Location, _shezhidakai);
        if (caozuo != SettingsAction.None)
        {
            ActivateSettings(caozuo);
            return;
        }

        if (_yemian == AppPage.MemoryTools && !_shezhidakai)
        {
            var gongjucaozuo = _xuanranqi.HitMemoryTools(ClientRectangle, (int)Math.Round(_gundongdangqian), shijian9.Location);
            if (gongjucaozuo == MemoryToolsAction.CleanMemory)
            {
                _ = CleanMemoryAsync();
                return;
            }
            if (gongjucaozuo == MemoryToolsAction.ConfigureTokenCollector)
            {
                _ = InstallTokenCollectorAsync();
                return;
            }
            if (gongjucaozuo == MemoryToolsAction.ConfigureAndroidLink)
            {
                OpenRelaySettingsDialog();
                return;
            }
            if (gongjucaozuo == MemoryToolsAction.CopyAndroidDeviceKey)
            {
                CopyAndroidDeviceKey();
                return;
            }
            if (gongjucaozuo == MemoryToolsAction.CopyAndroidLinkUrl)
            {
                CopyAndroidLinkUrl();
                return;
            }
        }

        if (_yemian == AppPage.TokenMonitor && !_shezhidakai)
        {
            var gundong = (int)Math.Round(_gundongdangqian);
            var huodongmoshi = _xuanranqi.HitTokenActivityMode(ClientRectangle, gundong, shijian9.Location);
            if (huodongmoshi is not null)
            {
                _tokenhuodongmoshi = huodongmoshi.Value;
                _tokenhuodongxuanfuriqi = _xuanranqi.HitTokenActivityDay(ClientRectangle, gundong, _tokenjiankongkuaizhao, shijian9.Location, _tokenhuodongmoshi);
                StartAnimation(260);
                Invalidate();
                return;
            }

            var tokencaozuo = _xuanranqi.HitTokenMonitor(ClientRectangle, (int)Math.Round(_gundongdangqian), shijian9.Location);
            if (tokencaozuo == TokenMonitorAction.StartOrRefresh)
            {
                _ = ImportTokenLogsAsync(xianshitishi: true);
            }
        }
    }

    protected override void OnMouseWheel(MouseEventArgs shijian10)
    {
        if (_shezhidakai)
        {
            return;
        }

        base.OnMouseWheel(shijian10);
        ScrollTo(_gundongmubiao - shijian10.Delta * 90 / 120);
    }

    protected override void OnKeyDown(KeyEventArgs shijian11)
    {
        base.OnKeyDown(shijian11);
        var yemian = Math.Max(120, ClientSize.Height - 120);
        if (shijian11.KeyCode == Keys.Down) ScrollTo(_gundongmubiao + 44);
        else if (shijian11.KeyCode == Keys.Up) ScrollTo(_gundongmubiao - 44);
        else if (shijian11.KeyCode == Keys.PageDown) ScrollTo(_gundongmubiao + yemian);
        else if (shijian11.KeyCode == Keys.PageUp) ScrollTo(_gundongmubiao - yemian);
        else if (shijian11.KeyCode == Keys.Home) ScrollTo(0);
        else if (shijian11.KeyCode == Keys.End) ScrollTo(MaxScroll());
        else if (shijian11.KeyCode == Keys.F11) ToggleFullscreen();
        else if (shijian11.KeyCode == Keys.Escape && _quanping) ExitFullscreen();
        else return;
        shijian11.Handled = true;
    }

    protected override void WndProc(ref Message xitongxiaoxi)
    {
        const int xiaoxifeikehudaxiao = 0x0083;
        const int xiaoxifeikehumingzhong = 0x0084;
        if (xitongxiaoxi.Msg == xiaoxifeikehudaxiao && xitongxiaoxi.WParam != IntPtr.Zero)
        {
            xitongxiaoxi.Result = IntPtr.Zero;
            return;
        }

        if (xitongxiaoxi.Msg == xiaoxifeikehumingzhong)
        {
            base.WndProc(ref xitongxiaoxi);
            if ((int)xitongxiaoxi.Result == 1)
            {
                var zuobiao = PointToClient(new Point((short)(xitongxiaoxi.LParam.ToInt64() & 0xFFFF), (short)((xitongxiaoxi.LParam.ToInt64() >> 16) & 0xFFFF)));
                xitongxiaoxi.Result = HitTest(zuobiao);
            }
            return;
        }

        base.WndProc(ref xitongxiaoxi);
    }

    protected override void OnFormClosing(FormClosingEventArgs shijian12)
    {
        if (!_zhengzaituichu && shijian12.CloseReason == CloseReason.UserClosing && _shezhi.guanbihoutuopan)
        {
            shijian12.Cancel = true;
            HideToTray();
            return;
        }

        _zhengzaituichu = true;
        _tuopan.Visible = false;
        base.OnFormClosing(shijian12);
    }

    protected override void Dispose(bool zhengzaishifang)
    {
        if (zhengzaishifang)
        {
            _guanbizhi.Cancel();
            _shuaxindingshiqi.Stop();
            _donghuadingshiqi.Stop();
            _tokenjiankongshuaxin.Stop();
            _deepseekshuaxin.Stop();
            OpenClawTokenMonitorLauncher.RequestStopOwnedProcess();
            _shuaxindingshiqi.Dispose();
            _donghuadingshiqi.Dispose();
            _tokenjiankongshuaxin.Dispose();
            _deepseekshuaxin.Dispose();
            _guanbizhi.Dispose();
            _tuopan.Dispose();
            _tuopancaidan.Dispose();
            _neiqianfuwuqi.Dispose();
            _xuanranqi.Dispose();
            _shezhi.Dispose();
        }
        base.Dispose(zhengzaishifang);
    }

    private async Task RefreshSnapshotAsync()
    {
        if (_caiji || _guanbizhi.IsCancellationRequested) return;
        _caiji = true;
        try
        {
            var kuaizhao7 = await Task.Run(() => _yingjiancaijiqi.Collect(), _guanbizhi.Token);
            if (_guanbizhi.IsCancellationRequested || IsDisposed) return;
            _kuaizhao = kuaizhao7;
            _jiuxu = true;
            ClampScroll();
            _shuaxinkaishi = DateTime.UtcNow;
            _neiqianfuwuqi.BroadcastSnapshot(kuaizhao7);
            StartAnimation(820);
            Invalidate();
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (!_jiuxu) Invalidate();
        }
        finally
        {
            _caiji = false;
        }
    }

    private void ActivateButton(WindowButton anniu3)
    {
        if (anniu3 == WindowButton.Close)
        {
            Close();
        }
        else if (anniu3 == WindowButton.Minimize)
        {
            WindowState = FormWindowState.Minimized;
        }
        else if (anniu3 == WindowButton.Fullscreen)
        {
            ToggleFullscreen();
        }
        else if (anniu3 == WindowButton.MonitorHome)
        {
            SwitchPage(AppPage.Monitor);
        }
        else if (anniu3 == WindowButton.MemoryTools)
        {
            SwitchPage(AppPage.MemoryTools);
        }
        else if (anniu3 == WindowButton.OpenClawTokenMonitor)
        {
            SwitchPage(AppPage.TokenMonitor);
            _ = ImportTokenLogsAsync(xianshitishi: false);
        }
        else if (anniu3 == WindowButton.Settings)
        {
            if (_shezhidakai) CloseSettings();
            else OpenSettings();
        }
    }

    private void ActivateSettings(SettingsAction caozuo2)
    {
        if (caozuo2 == SettingsAction.Close)
        {
            CloseSettings();
        }
        else if (caozuo2 == SettingsAction.ToggleLanguage)
        {
            _shezhi.yuyan = _shezhi.zhiyingwen ? AppLanguage.Chinese : AppLanguage.English;
            _shezhi.Save();
            UpdateTrayText();
            StartAnimation(260);
            Invalidate();
        }
        else if (caozuo2 == SettingsAction.PickBackground)
        {
            using var duihuakuang = new OpenFileDialog
            {
                Title = _shezhi.Text("选择背景图片", "Choose background image"),
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };
            if (duihuakuang.ShowDialog(this) == DialogResult.OK)
            {
                _shezhi.SetBackground(duihuakuang.FileName);
                StartAnimation(320);
                Invalidate();
            }
        }
        else if (caozuo2 == SettingsAction.ClearBackground)
        {
            _shezhi.ClearBackground();
            StartAnimation(260);
            Invalidate();
        }
        else if (caozuo2 == SettingsAction.ToggleAutostart)
        {
            ToggleAutostartSetting();
        }
        else if (caozuo2 == SettingsAction.ToggleCloseToTray)
        {
            _shezhi.guanbihoutuopan = !_shezhi.guanbihoutuopan;
            _shezhi.Save();
            UpdateTrayVisibility();
            ShowToast(_shezhi.guanbihoutuopan
                ? _shezhi.Text("关闭按钮将把窗口放入系统托盘", "Close now sends the window to the system tray")
                : _shezhi.Text("关闭按钮将直接退出程序", "Close now exits the app directly"), 3600);
            StartAnimation(260);
            Invalidate();
        }
        else if (caozuo2 == SettingsAction.ToggleRelaySync)
        {
            _shezhi.relayEnabled = !_shezhi.relayEnabled;
            _shezhi.Save();
            if (_shezhi.relayEnabled)
            {
                StartEmbeddedServer();
                ShowToast(_shezhi.Text("已开启远程同步", "Remote sync enabled"), 3200);
            }
            else
            {
                _neiqianfuwuqi.Stop();
                ShowToast(_shezhi.Text("已关闭远程同步", "Remote sync disabled"), 3200);
            }
            StartAnimation(260);
            Invalidate();
        }
        else if (caozuo2 == SettingsAction.ConfigureRelaySync)
        {
            OpenRelaySettingsDialog();
        }
    }

    private void StartEmbeddedServer()
    {
        if (_neiqianfuwuqi.zhuangtai == EmbeddedServerState.Running) return;
        _neiqianfuwuqi.gongwangdizhi = _shezhi.relayUrl;
        _neiqianfuwuqi.Start(8787, _shezhi.relayDeviceKey);
        if (_kuaizhao is not null)
        {
            _neiqianfuwuqi.BroadcastSnapshot(_kuaizhao);
        }
    }

    private string EnsureAndroidDeviceKey()
    {
        var miyao = _neiqianfuwuqi.EnsureDeviceKey(_shezhi.relayDeviceKey);
        if (!string.Equals(_shezhi.relayDeviceKey, miyao, StringComparison.Ordinal))
        {
            _shezhi.relayDeviceKey = miyao;
            _shezhi.Save();
        }

        return miyao;
    }

    private void CopyAndroidDeviceKey()
    {
        var miyao = EnsureAndroidDeviceKey();
        try
        {
            Clipboard.SetText(miyao);
            ShowToast(_shezhi.Text("设备密钥已复制", "Device key copied"), 2600);
        }
        catch
        {
            ShowToast(_shezhi.Text("复制失败，请稍后重试", "Copy failed, try again"), 3200);
        }
    }

    private void CopyAndroidLinkUrl()
    {
        var dizhi = _neiqianfuwuqi.androidDizhi;
        if (string.IsNullOrWhiteSpace(dizhi) || dizhi.StartsWith("http://127."))
        {
            ShowToast(_shezhi.Text("未配置公网地址", "No public URL configured"), 2600);
            return;
        }
        try
        {
            Clipboard.SetText(dizhi);
            ShowToast(_shezhi.Text("公网地址已复制", "Public URL copied"), 2600);
        }
        catch
        {
            ShowToast(_shezhi.Text("复制失败", "Copy failed"), 3200);
        }
    }

    private void OpenRelaySettingsDialog()
    {
        var dangqianmiyao = EnsureAndroidDeviceKey();
        using var duihuakuang = new Form
        {
            Text = _shezhi.Text("配置远程同步", "Configure Remote Sync"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ClientSize = new Size(480, 340),
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false
        };

        var neirong = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 6
        };
        neirong.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
        neirong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 5; i++)
        {
            neirong.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        }
        neirong.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var kaiqi = new CheckBox { Text = _shezhi.Text("开启同步", "Enable sync"), Checked = _shezhi.relayEnabled, AutoSize = true, Anchor = AnchorStyles.Left };
        var dizhi = RelayTextBox(_shezhi.relayUrl);
        var deepseekMiyao = RelayTextBox(_shezhi.deepSeekApiKey);
        deepseekMiyao.PasswordChar = '*';

        AddRelayDialogRow(neirong, 0, _shezhi.Text("同步", "Sync"), kaiqi);
        AddRelayDialogRow(neirong, 1, _shezhi.Text("公网地址", "Public URL"), dizhi);

        var miyaowenben = new Label
        {
            Text = _shezhi.Text("设备密钥：", "Device Key: ") + dangqianmiyao,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Color.FromArgb(86, 86, 96),
            Margin = new Padding(0, 8, 0, 0)
        };
        neirong.Controls.Add(miyaowenben, 0, 2);
        neirong.SetColumnSpan(miyaowenben, 2);

        AddRelayDialogRow(neirong, 3, _shezhi.Text("DS API Key", "DS API Key"), deepseekMiyao);

        var anliwenben = new Label
        {
            Text = _shezhi.Text("用 MSLFrp 把本机 8787 端口暴露到公网。DeepSeek API Key 从环境变量 DEEPSEEK_API_KEY 读取，此处可覆盖。", "Use MSLFrp to expose local port 8787. DeepSeek API Key reads from env DEEPSEEK_API_KEY; this overrides it."),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Color.FromArgb(134, 134, 144),
            Margin = new Padding(0, 6, 0, 0)
        };
        neirong.Controls.Add(anliwenben, 0, 4);
        neirong.SetColumnSpan(anliwenben, 2);

        var anniuqu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var queding = new Button { Text = _shezhi.Text("保存", "Save"), DialogResult = DialogResult.OK, Width = 86 };
        var quxiao = new Button { Text = _shezhi.Text("取消", "Cancel"), DialogResult = DialogResult.Cancel, Width = 86 };
        anniuqu.Controls.Add(queding);
        anniuqu.Controls.Add(quxiao);
        neirong.Controls.Add(anniuqu, 1, 5);

        duihuakuang.Controls.Add(neirong);
        duihuakuang.AcceptButton = queding;
        duihuakuang.CancelButton = quxiao;

        if (duihuakuang.ShowDialog(this) != DialogResult.OK) return;

        _shezhi.relayEnabled = kaiqi.Checked;
        _shezhi.relayUrl = dizhi.Text.Trim();
        _shezhi.deepSeekApiKey = deepseekMiyao.Text.Trim();
        if (string.IsNullOrWhiteSpace(_shezhi.relayDeviceKey) && _neiqianfuwuqi.miyao.Length > 0)
        {
            _shezhi.relayDeviceKey = _neiqianfuwuqi.miyao;
        }
        _shezhi.Save();

        ShowToast(_shezhi.Text("远程配置已保存", "Remote settings saved"), 3200);
        if (_shezhi.relayEnabled)
        {
            StartEmbeddedServer();
        }
        StartAnimation(260);
        Invalidate();
    }

    private static TextBox RelayTextBox(string wenben)
    {
        return new TextBox
        {
            Text = wenben,
            Dock = DockStyle.Fill,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 4, 0, 4)
        };
    }

    private static void AddRelayDialogRow(TableLayoutPanel neirong, int hang, string biaoqian, Control kongjian)
    {
        var label = new Label
        {
            Text = biaoqian,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };
        neirong.Controls.Add(label, 0, hang);
        neirong.Controls.Add(kongjian, 1, hang);
    }

    private void TickAnimation()
    {
        var xianzai = DateTime.UtcNow;
        var haomiao = _shangcidonghuashi == DateTime.MinValue
            ? donghuajiangehaomiao
            : Math.Clamp((xianzai - _shangcidonghuashi).TotalMilliseconds, 1, 120);
        _shangcidonghuashi = xianzai;

        var mubiao2 = _shezhidakai ? 1d : 0d;
        var shezhixishu = 1d - Math.Exp(-haomiao / 42d);
        _shezhijieduan += (mubiao2 - _shezhijieduan) * shezhixishu;
        if (Math.Abs(mubiao2 - _shezhijieduan) < 0.015) _shezhijieduan = mubiao2;

        var gundongxishu = 1d - Math.Exp(-haomiao / 48d);
        _gundongdangqian += (_gundongmubiao - _gundongdangqian) * gundongxishu;
        if (Math.Abs(_gundongmubiao - _gundongdangqian) < 0.35) _gundongdangqian = _gundongmubiao;

        if (PageTransitionRaw(xianzai) >= 1d)
        {
            _qiehuanlaiyuan = _yemian;
            _qiehuanmubiao = _yemian;
        }

        if (!_neicunqinglizhong && _neicunqinglitishi.Length > 0 && xianzai > _neicunqinglitishijieshu)
        {
            _neicunqinglitishi = "";
        }

        Invalidate();
        if (!HasActiveAnimation(xianzai, mubiao2))
        {
            _donghuadingshiqi.Stop();
        }
    }

    private async Task CleanMemoryAsync()
    {
        if (_neicunqinglizhong || _guanbizhi.IsCancellationRequested) return;
        _neicunqinglizhong = true;
        _neicunqinglitishi = _shezhi.Text("正在整理内存...", "Cleaning memory...");
        _neicunqinglijieguo = _neicunqinglitishi;
        _neicunqinglitishikaishi = DateTime.UtcNow;
        _neicunqinglitishijieshu = DateTime.MaxValue;
        StartAnimation(4200);
        Invalidate();

        try
        {
            var jieguo = await Task.Run(MemoryCleaner.Clean, _guanbizhi.Token);
            if (_guanbizhi.IsCancellationRequested || IsDisposed) return;
            var daxiao = Formatters.Bytes((ulong)Math.Max(0, jieguo.shifangzijie));
            _neicunqinglitishi = jieguo.chenggongshuliang > 0
                ? _shezhi.Text($"已整理 {jieguo.chenggongshuliang} 个进程，估算减少 {daxiao}", $"Cleaned {jieguo.chenggongshuliang} processes, estimated {daxiao} reduced")
                : _shezhi.Text("没有可整理的进程", "No processes could be cleaned");
            _neicunqinglijieguo = _neicunqinglitishi;
            _neicunqinglitishikaishi = DateTime.UtcNow;
            _neicunqinglitishijieshu = DateTime.UtcNow.AddMilliseconds(4000);
            StartAnimation(4200);
            await RefreshSnapshotAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            _neicunqinglitishi = _shezhi.Text("内存整理未完成", "Memory cleanup did not complete");
            _neicunqinglijieguo = _neicunqinglitishi;
            _neicunqinglitishikaishi = DateTime.UtcNow;
            _neicunqinglitishijieshu = DateTime.UtcNow.AddMilliseconds(4000);
            StartAnimation(4200);
        }
        finally
        {
            _neicunqinglizhong = false;
            Invalidate();
        }
    }

    private async Task ImportTokenLogsAsync(bool xianshitishi)
    {
        if (_guanbizhi.IsCancellationRequested) return;
        if (_tokenrizhidaoruzhong)
        {
            if (xianshitishi) ShowToast(_shezhi.Text("Token 日志正在补读中...", "Token logs are already being imported..."), 2400);
            return;
        }

        _tokenrizhidaoruzhong = true;
        if (xianshitishi) ShowToast(_shezhi.Text("正在补读 Token 日志...", "Importing Token logs..."), 2200);
        try
        {
            var jieguo = await Task.Run(OpenClawTokenMonitorLauncher.ImportUsageLogs, _guanbizhi.Token);
            if (_guanbizhi.IsCancellationRequested || IsDisposed) return;
            RefreshTokenMonitorSnapshot();
            if (xianshitishi)
            {
                var tishi = jieguo.daorushuliang > 0
                    ? _shezhi.Text($"已补读 {jieguo.daorushuliang} 条 Token 记录", $"Imported {jieguo.daorushuliang} Token records")
                    : jieguo.biaojishuliang > 0
                        ? _shezhi.Text("已建立历史日志去重基线", "Historical log baseline recorded")
                        : _shezhi.Text("暂无新的 Token 记录", "No new Token records");
                ShowToast(tishi, 4200);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (xianshitishi && !_guanbizhi.IsCancellationRequested && !IsDisposed)
            {
                ShowToast(_shezhi.Text("Token 日志补读失败", "Token log import failed"), 5200);
            }
        }
        finally
        {
            _tokenrizhidaoruzhong = false;
        }
    }

    private void ScheduleTokenAutoImport()
    {
        if (_guanbizhi.IsCancellationRequested || _tokenrizhidaoruzhong) return;
        var xianzai = DateTime.UtcNow;
        if (xianzai < _xiacitokenrizhidaoru) return;

        _xiacitokenrizhidaoru = xianzai.AddSeconds(5);
        _ = AutoImportTokenLogsAsync();
    }

    private async Task AutoImportTokenLogsAsync()
    {
        await ImportTokenLogsAsync(xianshitishi: false);
    }

    private async Task InstallTokenCollectorAsync()
    {
        if (_guanbizhi.IsCancellationRequested) return;

        ShowToast(_shezhi.Text("正在配置 Token 后台采集...", "Configuring Token background collector..."), 2800);
        try
        {
            var jieguo = await Task.Run(TokenCollectorService.InstallOrRepair, _guanbizhi.Token);
            if (_guanbizhi.IsCancellationRequested || IsDisposed) return;
            _ = ImportTokenLogsAsync(xianshitishi: false);
            RefreshTokenMonitorSnapshot();
            ShowToast(_shezhi.Text(jieguo.zhongwentishi, jieguo.yingwentishi), jieguo.chenggong ? 4800 : 6200);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (!_guanbizhi.IsCancellationRequested && !IsDisposed)
            {
                ShowToast(_shezhi.Text("Token 后台采集配置失败", "Token background collector configuration failed"), 5600);
            }
        }
    }

    private void ShowToast(string tishi, int haomiao)
    {
        if (_neicunqinglizhong) return;
        _neicunqinglitishi = tishi;
        _neicunqinglitishikaishi = DateTime.UtcNow;
        _neicunqinglitishijieshu = DateTime.UtcNow.AddMilliseconds(Math.Max(1200, haomiao));
        StartAnimation(Math.Max(1600, haomiao + 360));
        Invalidate();
    }

    private void InitializeTrayIcon()
    {
        _tuopan.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        _tuopan.DoubleClick += (_, _) => RestoreFromTray();
        _tuopancaidan.Items.Add(_shezhi.Text("显示系统观测台", "Show System Observatory"), null, (_, _) => RestoreFromTray());
        _tuopancaidan.Items.Add(new ToolStripSeparator());
        _tuopancaidan.Items.Add(_shezhi.Text("退出", "Exit"), null, (_, _) => ExitFromTray());
        _tuopan.ContextMenuStrip = _tuopancaidan;
        UpdateTrayText();
        UpdateTrayVisibility();

        if (_shezhi.kaijiqidong)
        {
            StartupManager.SetEnabled(true);
        }
    }

    private void UpdateTrayText()
    {
        _tuopan.Text = _shezhi.Text("系统观测台", "System Observatory");
        if (_tuopancaidan.Items.Count >= 3)
        {
            _tuopancaidan.Items[0].Text = _shezhi.Text("显示系统观测台", "Show System Observatory");
            _tuopancaidan.Items[2].Text = _shezhi.Text("退出", "Exit");
        }
    }

    private void UpdateTrayVisibility()
    {
        _tuopan.Visible = _shezhi.guanbihoutuopan && !_zhengzaituichu;
    }

    private void HideToTray()
    {
        _tuopan.Visible = true;
        Hide();
        ShowInTaskbar = false;
        if (_yitishituopan) return;
        _yitishituopan = true;
        _tuopan.BalloonTipTitle = _shezhi.Text("系统观测台仍在运行", "System Observatory is still running");
        _tuopan.BalloonTipText = _shezhi.Text("已放入系统托盘，双击图标可恢复窗口。", "Moved to the system tray. Double-click the icon to restore.");
        _tuopan.ShowBalloonTip(2600);
    }

    private void RestoreFromTray()
    {
        if (IsDisposed) return;
        ShowInTaskbar = true;
        Show();
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Activate();
        StartAnimation(420);
        Invalidate();
    }

    private void ExitFromTray()
    {
        _zhengzaituichu = true;
        _tuopan.Visible = false;
        Close();
    }

    private void ToggleAutostartSetting()
    {
        var kaiqi = !_shezhi.kaijiqidong;
        if (!StartupManager.SetEnabled(kaiqi))
        {
            ShowToast(_shezhi.Text("开机自启动设置失败", "Failed to update startup setting"), 4600);
            return;
        }

        _shezhi.kaijiqidong = kaiqi;
        _shezhi.Save();
        ShowToast(kaiqi
            ? _shezhi.Text("已开启开机自启动", "Start with Windows enabled")
            : _shezhi.Text("已关闭开机自启动", "Start with Windows disabled"), 3600);
        StartAnimation(260);
        Invalidate();
    }

    private void StartAnimation(int haomiao)
    {
        var jieshu = DateTime.UtcNow.AddMilliseconds(haomiao);
        if (jieshu > _donghuajieshu) _donghuajieshu = jieshu;
        if (!_donghuadingshiqi.Enabled) _donghuadingshiqi.Start();
    }

    private void OpenSettings()
    {
        _shezhidakai = true;
        _xuanfukapian = MonitorCard.None;
        StartAnimation(220);
        Invalidate();
    }

    private void CloseSettings()
    {
        _shezhidakai = false;
        StartAnimation(260);
        Invalidate();
    }

    private void ScrollTo(double zong)
    {
        var xiayige = Math.Clamp(zong, 0, MaxScroll());
        if (Math.Abs(xiayige - _gundongmubiao) < 0.5) return;
        _gundongmubiao = xiayige;
        _tokenhuodongxuanfuriqi = "";
        StartAnimation(420);
        Invalidate();
    }

    private void SwitchPage(AppPage yemian)
    {
        if (_yemian == yemian) return;
        _qiehuanlaiyuan = _yemian;
        _qiehuanmubiao = yemian;
        _yemianqiehuankaishi = DateTime.UtcNow;
        _yemian = yemian;
        _gundongdangqian = 0;
        _gundongmubiao = 0;
        _tokenhuodongxuanfuriqi = "";
        if (yemian == AppPage.TokenMonitor || yemian == AppPage.MemoryTools)
        {
            _tokenjiankongshuaxin.Start();
            RefreshTokenMonitorSnapshot();
        }
        else
        {
            _tokenjiankongshuaxin.Stop();
        }
        StartAnimation(yemianqiehuanhaomiao + 160);
        Invalidate();
    }

    private void RefreshTokenMonitorSnapshot()
    {
        _tokenjiankongkuaizhao = OpenClawTokenMonitorLauncher.Snapshot();
        if (_yemian == AppPage.TokenMonitor || _yemian == AppPage.MemoryTools)
        {
            ScheduleTokenAutoImport();
            StartAnimation(260);
            Invalidate();
        }
    }

    private async Task RefreshDeepSeekBalanceAsync()
    {
        var miyao = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? _shezhi.deepSeekApiKey ?? "";
        if (string.IsNullOrWhiteSpace(miyao)) return;
        _deepseekyuexinxi = await DeepSeekBalanceChecker.QueryAsync(miyao, _guanbizhi.Token);

        var opencode = OpenCodeLogReader.QueryDeepSeekUsage();
        if (opencode.youshuju)
        {
            var zongliang = opencode.jinrishurutokens + opencode.jinrishuchutokens + opencode.jinrituilitokens;
            _deepseekyuexinxi.totalTokensText = zongliang switch
            {
                >= 100_000_000 => $"{zongliang / 100_000_000d:F1}亿",
                >= 10_000 => $"{zongliang / 10_000d:F1}万",
                _ => zongliang.ToString("N0")
            };
            _deepseekyuexinxi.huancunmingzhonglvText = opencode.jinrihuancunmingzhonglv > 0
                ? opencode.jinrihuancunmingzhonglv.ToString("0.0") + "%"
                : "-";
            _deepseekyuexinxi.huancunmingzhongwenben = opencode.jinrihuancunmingzhongwenben;
            _deepseekyuexinxi.weimingzhongwenben = opencode.jinriweimingzhongwenben;
        }
        else
        {
            _deepseekyuexinxi.totalTokensText = "-";
            _deepseekyuexinxi.huancunmingzhonglvText = "-";
            _deepseekyuexinxi.huancunmingzhongwenben = "-";
            _deepseekyuexinxi.weimingzhongwenben = "-";
        }

        SyncOpenCodeDeepSeek();

        if (_yemian == AppPage.TokenMonitor || _yemian == AppPage.MemoryTools)
        {
            _tokenjiankongkuaizhao = OpenClawTokenMonitorLauncher.Snapshot();
            Invalidate();
        }
    }

    private static int _shangciDsJinriZongliang = -1;

    private static void SyncOpenCodeDeepSeek()
    {
        var wenjian = TokenUsageStore.DefaultPath();
        var jinri = DateOnly.FromDateTime(DateTime.Now);

        foreach (var (riqi, shuru, shuchu, tuili, huancun) in OpenCodeLogReader.QueryAllDeepSeekDays())
        {
            var zongshuru = shuru + tuili + huancun;
            var zongliang = zongshuru + shuchu;

            if (riqi == jinri)
            {
                if (_shangciDsJinriZongliang < 0)
                {
                    var jian = $"opencode-ds:{riqi:yyyy-MM-dd}";
                    TokenUsageStore.AddUsageIfNew(wenjian, jian, riqi, zongshuru, shuchu, 0, "deepseek-v4-pro", 0, DateTime.UtcNow);
                    _shangciDsJinriZongliang = zongliang;
                }
                else if (zongliang > _shangciDsJinriZongliang)
                {
                    var shengliang = zongliang - _shangciDsJinriZongliang;
                    var shengliangShuru = (int)Math.Round(zongshuru * (double)shengliang / zongliang);
                    var shengliangShuchu = shengliang - shengliangShuru;
                    TokenUsageStore.AddUsage(wenjian, riqi, shengliangShuru, shengliangShuchu);
                    _shangciDsJinriZongliang = zongliang;
                }
            }
            else
            {
                var jian = $"opencode-ds:{riqi:yyyy-MM-dd}";
                TokenUsageStore.AddUsageIfNew(wenjian, jian, riqi, zongshuru, shuchu, 0, "deepseek-v4-pro", 0, DateTime.UtcNow);
            }
        }
    }

    private void ToggleFullscreen()
    {
        if (_quanping) ExitFullscreen();
        else EnterFullscreen();
    }

    private void EnterFullscreen()
    {
        _chuangkouzhuangtai = WindowState;
        _chuangkoubianjie = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _quanping = true;
        WindowState = FormWindowState.Normal;
        Bounds = Screen.FromControl(this).Bounds;
        Invalidate();
    }

    private void ExitFullscreen()
    {
        _quanping = false;
        WindowState = FormWindowState.Normal;
        if (!_chuangkoubianjie.IsEmpty) Bounds = _chuangkoubianjie;
        if (_chuangkouzhuangtai == FormWindowState.Maximized) WindowState = FormWindowState.Maximized;
        ApplyDwmChrome();
        Invalidate();
    }

    private void ClampScroll()
    {
        var zuida = MaxScroll();
        _gundongmubiao = Math.Clamp(_gundongmubiao, 0, zuida);
        _gundongdangqian = Math.Clamp(_gundongdangqian, 0, zuida);
    }

    private int MaxScroll()
    {
        return Math.Max(0, _xuanranqi.ContentHeight(ClientRectangle, _kuaizhao, _yemian) - ClientSize.Height);
    }

    private AnimationSnapshot BuildAnimationSnapshot(DateTime xianzai)
    {
        var qidongyuanshi = UiAnimation.Clamp01((xianzai - _qidongkaishi).TotalMilliseconds / qidongdonghuahaomiao);
        var qidongjieduan = UiAnimation.EaseOutCubic(qidongyuanshi);

        var yemianyuanshi = PageTransitionRaw(xianzai);
        var yemianjieduan = UiAnimation.EaseInOutCubic(yemianyuanshi);
        var qiehuanzhong = _qiehuanlaiyuan != _qiehuanmubiao && yemianyuanshi < 1d;
        var fangxiang = qiehuanzhong ? UiAnimation.yemianfangxiang(_qiehuanlaiyuan, _qiehuanmubiao) : 0;
        var yemianpianyi = 0d;

        var shuaxinjieduan = 0d;
        if (_shuaxinkaishi != DateTime.MinValue)
        {
            var shuaxinyuanshi = UiAnimation.Clamp01((xianzai - _shuaxinkaishi).TotalMilliseconds / shuaxinhaomiao);
            shuaxinjieduan = 1d - UiAnimation.EaseOutCubic(shuaxinyuanshi);
        }

        var tishijieduan2 = tishijieduan(xianzai);
        var gongzuomaichong = _neicunqinglizhong
            ? 0.5d + Math.Sin(Math.Max(0, (xianzai - _neicunqinglitishikaishi).TotalSeconds) * 5.2d) * 0.5d
            : 0d;
        var neirongjieduan = qiehuanzhong ? UiAnimation.SequentialFadeIn(yemianjieduan) : qidongjieduan;

        return new AnimationSnapshot(
            dakaijieduan: qidongjieduan,
            shezhijieduan: _shezhijieduan,
            yemianjieduan: yemianjieduan,
            neirongjieduan: neirongjieduan,
            shuaxinjieduan: shuaxinjieduan,
            tishijieduan: tishijieduan2,
            gongzuobomai: gongzuomaichong,
            gundongpianyi: _gundongdangqian,
            yemianpianyi: yemianpianyi,
            yemianfangxiang: fangxiang,
            dangqianyemian: _yemian,
            laiyuanyemian: qiehuanzhong ? _qiehuanlaiyuan : _yemian,
            mubiaoyemian: _yemian,
            yemianqiehuanjihuo: qiehuanzhong);
    }

    private double PageTransitionRaw(DateTime xianzai)
    {
        if (_qiehuanlaiyuan == _qiehuanmubiao) return 1d;
        return UiAnimation.Clamp01((xianzai - _yemianqiehuankaishi).TotalMilliseconds / yemianqiehuanhaomiao);
    }

    private double tishijieduan(DateTime xianzai)
    {
        if (string.IsNullOrWhiteSpace(_neicunqinglitishi)) return 0d;
        var jinru = UiAnimation.EaseOutCubic((xianzai - _neicunqinglitishikaishi).TotalMilliseconds / 220d);
        if (_neicunqinglitishijieshu == DateTime.MaxValue) return jinru;

        var shengyu = (_neicunqinglitishijieshu - xianzai).TotalMilliseconds;
        if (shengyu <= 0) return 0d;
        var tuichu = UiAnimation.Clamp01(shengyu / 360d);
        return Math.Min(jinru, tuichu);
    }

    private bool HasActiveAnimation(DateTime xianzai, double shezhimubiao)
    {
        if (xianzai < _donghuajieshu) return true;
        if (Math.Abs(_shezhijieduan - shezhimubiao) >= 0.02) return true;
        if (Math.Abs(_gundongdangqian - _gundongmubiao) >= 0.5) return true;
        if (PageTransitionRaw(xianzai) < 1d) return true;
        if (UiAnimation.Clamp01((xianzai - _qidongkaishi).TotalMilliseconds / qidongdonghuahaomiao) < 1d) return true;
        if (_shuaxinkaishi != DateTime.MinValue && UiAnimation.Clamp01((xianzai - _shuaxinkaishi).TotalMilliseconds / shuaxinhaomiao) < 1d) return true;
        if (_neicunqinglizhong) return true;
        return !string.IsNullOrWhiteSpace(_neicunqinglitishi) && xianzai <= _neicunqinglitishijieshu;
    }

    private void ApplyDwmChrome()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        try
        {
            const int dwmchuangkouyuanjiao = 33;
            const int dwmbiankuangyanse = 34;
            const int zhiyansezhi = unchecked((int)0xFFFFFFFE);
            var yuanjiao = 2;
            var zhiyanse = zhiyansezhi;
            DwmSetWindowAttribute(Handle, dwmchuangkouyuanjiao, ref yuanjiao, sizeof(int));
            DwmSetWindowAttribute(Handle, dwmbiankuangyanse, ref zhiyanse, sizeof(int));
        }
        catch
        {
            
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr chuangkoujubing, int shuxing, ref int shuxingzhi, int zhidaxiao);

    private IntPtr HitTest(Point zuobiao2)
    {
        const int mingzhongkehuqu = 1;
        const int mingzhongbiaotiqu = 2;
        const int zhizuo = 10;
        const int zhiyou = 11;
        const int zhidingbu = 12;
        const int zhidingbuzuo = 13;
        const int zhidingbuyou = 14;
        const int zhidibu = 15;
        const int zhidibuzuo = 16;
        const int zhidibuyou = 17;

        if (_xuanranqi.HitButton(ClientRectangle, zuobiao2) != WindowButton.None) return (IntPtr)mingzhongkehuqu;
        if (_shezhidakai && _xuanranqi.SettingsPanelRect(ClientRectangle).Contains(zuobiao2)) return (IntPtr)mingzhongkehuqu;

        if (_quanping) return (IntPtr)mingzhongkehuqu;

        if (WindowState != FormWindowState.Maximized)
        {
            var zuo2 = zuobiao2.X < suofangbiankuangkuandu;
            var you2 = zuobiao2.X >= ClientSize.Width - suofangbiankuangkuandu;
            var dingbu = zuobiao2.Y < suofangbiankuangkuandu;
            var dibu = zuobiao2.Y >= ClientSize.Height - suofangbiankuangkuandu;
            if (zuo2 && dingbu) return (IntPtr)zhidingbuzuo;
            if (you2 && dingbu) return (IntPtr)zhidingbuyou;
            if (zuo2 && dibu) return (IntPtr)zhidibuzuo;
            if (you2 && dibu) return (IntPtr)zhidibuyou;
            if (zuo2) return (IntPtr)zhizuo;
            if (you2) return (IntPtr)zhiyou;
            if (dingbu) return (IntPtr)zhidingbu;
            if (dibu) return (IntPtr)zhidibu;
        }

        return zuobiao2.Y < MonitorRenderer.zhigaodu ? (IntPtr)mingzhongbiaotiqu : (IntPtr)mingzhongkehuqu;
    }
}
