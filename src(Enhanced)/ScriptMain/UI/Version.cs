using GTA;
using GTA.Native;
using System.Diagnostics;

public class ModVersionChecker : Script
{
    private readonly string modVersion = "1.5.0";
    private readonly int requiredBuild = 889; // Example build number
    private bool hasChecked = false;

    public ModVersionChecker()
    {
        Tick += OnTick;
        Interval = 1000;
    }

    private void OnTick(object sender, System.EventArgs e)
    {
        if (hasChecked) return;

        ShowNotification("~g~TornadoV~b~ Enhanced~g~ Loaded!~w~ Version: " + modVersion);

        // Get GTA5.exe file version
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(exePath);

        string fileVersion = versionInfo.FileVersion;
        string[] parts = fileVersion.Split('.');
        int gameBuild = int.Parse(parts[2]);

        if (gameBuild < requiredBuild)
        {
            ShowNotification("~r~Your GTA V build (" + gameBuild + ") is LOWER than required (" + requiredBuild + ")");
        }
        else if (gameBuild > requiredBuild)
        {
            ShowNotification("~y~Your GTA V build (" + gameBuild + ") is HIGHER than this mod was built for (" + requiredBuild + ")");
        }

        hasChecked = true;
    }

    private void ShowNotification(string message)
    {
        Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, message);
        Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
    }
}
