using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using System;
using System.Drawing;
using System.Windows.Forms;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.Config;
using TornadoScript.ScriptMain.Script;

public class TornadoMenu : Script
{
    private readonly ObjectPool pool;
    private readonly NativeMenu menu;
    private readonly NativeMenu settingsPage;

    // Store default values read from INI
    private bool movementEnabled;
    private bool reverseRotation;
    private bool multiVortexEnabled;
    private bool cloudTopEnabled;
    private bool cloudTopDebrisEnabled;
    private bool surfaceDetectionEnabled;
    private bool useInternalPool;
    private bool notifications;
    private bool spawnInStorm;

    // New: customizable menu toggle key & whether keybinds are enabled
    private Keys toggleKey = Keys.F5;
    private bool keybindsEnabled = true;

    public TornadoMenu()
    {
        pool = new ObjectPool();

        // Root menu
        menu = new NativeMenu("TornadoV", "Control Panel");
        menu.BannerText.Font = GTA.UI.Font.Monospace;
        menu.Banner.Color = Color.FromArgb(184, 162, 57);
        pool.Add(menu);

        // Settings submenu
        settingsPage = new NativeMenu("Tornado V", "Settings");
        settingsPage.BannerText.Font = GTA.UI.Font.Monospace;
        settingsPage.Banner.Color = Color.FromArgb(184, 162, 57);
        pool.Add(settingsPage);

        // Read all default values from INI (including the new menu toggle key)
        LoadIniValues();

        // Notify what toggle key is loaded (only if notifications enabled)
        if (notifications)
        {
            //ShowNotification($"Menu toggle key set to {toggleKey}");
        }

        // Spawn Tornado button
        var spawnItem = new NativeItem("Spawn Tornado");
        spawnItem.Activated += (s, e) => SpawnTornado();
        menu.Add(spawnItem);

        // Despawn Tornado button
        var despawnItem = new NativeItem("Despawn Tornado");
        despawnItem.Activated += (s, e) => DespawnTornado();
        menu.Add(despawnItem);

        // Add Settings submenu
        menu.AddSubMenu(settingsPage);

        // Add checkboxes individually
        AddMovementCheckbox();
        //AddReverseRotationCheckbox();
        //AddMultiVortexCheckbox();
        AddCloudTopCheckbox();
        AddCloudTopDebrisCheckbox();
        AddSurfaceDetectionCheckbox();
        AddUseInternalPoolCheckbox();
        AddNotificationsCheckbox();
        AddSpawnInStormCheckbox();

        // Tick & KeyDown events
        Tick += OnTick;
        KeyDown += OnKeyDown;
    }

    private void LoadIniValues()
    {
        // Read main settings
        movementEnabled = IniHelper.GetValue("Vortex", "MovementEnabled", true);
        reverseRotation = IniHelper.GetValue("Vortex", "ReverseRotation", false);
        multiVortexEnabled = IniHelper.GetValue("VortexAdvanced", "MultiVortexEnabled", false);
        cloudTopEnabled = IniHelper.GetValue("VortexAdvanced", "CloudTopEnabled", false);
        cloudTopDebrisEnabled = IniHelper.GetValue("VortexAdvanced", "CloudTopDebrisEnabled", false);
        surfaceDetectionEnabled = IniHelper.GetValue("VortexAdvanced", "SurfaceDetectionEnabled", true);
        useInternalPool = IniHelper.GetValue("VortexAdvanced", "UseInternalPool", true);
        notifications = IniHelper.GetValue("Other", "Notifications", true);
        spawnInStorm = IniHelper.GetValue("Other", "SpawnInStorm", true);

        // New: Read keybinds section
        keybindsEnabled = IniHelper.GetValue("KeyBinds", "KeybindsEnabled", true);

        // Read the menu toggle key (new INI key: ToggleMenu). Default to F5.
        string keyString = IniHelper.GetValue<string>("KeyBinds", "ToggleMenu", "F5")?.Trim() ?? "F5";
        if (keybindsEnabled)
        {
            if (Enum.TryParse<Keys>(keyString, true, out var parsedKey))
            {
                toggleKey = parsedKey;
            }
            else
            {
                toggleKey = Keys.F5;
            }

        }
        else
        {
            toggleKey = Keys.F5;
        }
    }

    private void AddMovementCheckbox()
    {
        var checkbox = new NativeCheckboxItem("Movement Enabled", movementEnabled);
        checkbox.CheckboxChanged += (s, e) =>
        {
            movementEnabled = checkbox.Checked;
            IniHelper.WriteValue("Vortex", "MovementEnabled", movementEnabled.ToString());
            ScriptThread.SetVar("vortexMovementEnabled", movementEnabled);
        };
        settingsPage.Add(checkbox);
    }

    private void AddReverseRotationCheckbox()
    {
        var checkbox = new NativeCheckboxItem("Reverse Rotation", reverseRotation);
        checkbox.CheckboxChanged += (s, e) =>
        {
            reverseRotation = checkbox.Checked;
            IniHelper.WriteValue("Vortex", "ReverseRotation", reverseRotation.ToString());
            ScriptThread.SetVar("vortexReverseRotation", reverseRotation);
        };
        settingsPage.Add(checkbox);
    }

    private void AddCloudTopCheckbox()
    {
        var checkbox = new NativeCheckboxItem("Cloud Top Enabled", cloudTopEnabled);
        checkbox.CheckboxChanged += (s, e) =>
        {
            cloudTopEnabled = checkbox.Checked;
            IniHelper.WriteValue("VortexAdvanced", "CloudTopEnabled", cloudTopEnabled.ToString());
            ScriptThread.SetVar("vortexEnableCloudTopParticle", cloudTopEnabled);
        };
        settingsPage.Add(checkbox);
    }

    private void AddCloudTopDebrisCheckbox()
    {
        var checkbox = new NativeCheckboxItem("Cloud Top Debris", cloudTopDebrisEnabled);
        checkbox.CheckboxChanged += (s, e) =>
        {
            cloudTopDebrisEnabled = checkbox.Checked;
            IniHelper.WriteValue("VortexAdvanced", "CloudTopDebrisEnabled", cloudTopDebrisEnabled.ToString());
            ScriptThread.SetVar("vortexEnableCloudTopParticleDebris", cloudTopDebrisEnabled);
        };
        settingsPage.Add(checkbox);
    }

    private void AddSurfaceDetectionCheckbox()
    {
        var checkbox = new NativeCheckboxItem("Surface Detection Enabled", surfaceDetectionEnabled);
        checkbox.CheckboxChanged += (s, e) =>
        {
            surfaceDetectionEnabled = checkbox.Checked;
            IniHelper.WriteValue("VortexAdvanced", "SurfaceDetectionEnabled", surfaceDetectionEnabled.ToString());
            ScriptThread.SetVar("vortexEnableSurfaceDetection", surfaceDetectionEnabled);
        };
        settingsPage.Add(checkbox);
    }

    private void AddUseInternalPoolCheckbox()
    {
        var checkbox = new NativeCheckboxItem("Use Internal Pool", useInternalPool);
        checkbox.CheckboxChanged += (s, e) =>
        {
            useInternalPool = checkbox.Checked;
            IniHelper.WriteValue("VortexAdvanced", "UseInternalPool", useInternalPool.ToString());
            ScriptThread.SetVar("vortexUseEntityPool", useInternalPool);
        };
        settingsPage.Add(checkbox);
    }

    private void AddNotificationsCheckbox()
    {
        var checkbox = new NativeCheckboxItem("Notifications", notifications);
        checkbox.CheckboxChanged += (s, e) =>
        {
            notifications = checkbox.Checked;
            IniHelper.WriteValue("Other", "Notifications", notifications.ToString());
            ScriptThread.SetVar("notifications", notifications);
        };
        settingsPage.Add(checkbox);
    }

    private void AddSpawnInStormCheckbox()
    {
        var checkbox = new NativeCheckboxItem("Spawn In Storm", spawnInStorm);
        checkbox.CheckboxChanged += (s, e) =>
        {
            spawnInStorm = checkbox.Checked;
            IniHelper.WriteValue("Other", "SpawnInStorm", spawnInStorm.ToString());
            ScriptThread.SetVar("spawnInStorm", spawnInStorm);
        };
        settingsPage.Add(checkbox);
    }

    private void OnTick(object sender, EventArgs e) => pool.Process();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // Use the configurable ToggleMenu key instead of a hard-coded F5
        if (e.KeyCode == toggleKey)
        {
            menu.Visible = !menu.Visible;
            settingsPage.Visible = false;
        }
    }

    private void SpawnTornado()
    {
        if (MainScript.Factory.ActiveVortexCount > 0 && !MainScript.GetVar<bool>("multiVortex"))
            return;

        Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE, 0f, 0f, 0f, 1000000f);
        Function.Call(Hash.SET_WIND, 70.0f);

        var position = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 180f;
        MainScript.Factory.CreateVortex(position);
    }

    private void DespawnTornado()
    {
        MainScript.Factory.RemoveAll();
        ShowNotification("All tornadoes despawned!");
    }

    private void ShowNotification(string text)
    {
        Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
        Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
    }
}