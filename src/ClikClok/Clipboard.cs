using System.Diagnostics;

namespace ClikClok;

/// <summary>Puts text on the system clipboard with the platform's own tool (clip, pbcopy, wl-copy or xclip).</summary>
static class Clipboard
{
    public static bool TrySet(string text)
    {
        (string File, string Args)[] tools = OperatingSystem.IsWindows() ? [("clip", "")]
            : OperatingSystem.IsMacOS() ? [("pbcopy", "")]
            : [("wl-copy", ""), ("xclip", "-selection clipboard")];
        foreach (var (file, args) in tools)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo(file, args)
                {
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                })!;
                process.StandardInput.Write(text);
                process.StandardInput.Close();
                if (process.WaitForExit(5000) && process.ExitCode == 0)
                    return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // tool not installed: try the next one
            }
        }
        return false;
    }
}
