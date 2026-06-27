using HwMonitor.Hardware;
using HwMonitor.UI;

namespace HwMonitor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] minglingcanshu)
    {
        if (minglingcanshu.Any(canshu => string.Equals(canshu, "--token-collector", StringComparison.OrdinalIgnoreCase)))
        {
            TokenCollectorService.RunCollectorLoop();
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
