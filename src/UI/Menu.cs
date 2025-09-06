using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using System.Windows.Forms;
using System.Drawing;
using TornadoScript.ScriptMain.Script;

public class TornadoMenu : Script
{
    private readonly ObjectPool pool;
    private readonly NativeMenu menu;
    private readonly NativeMenu settingsPage;

    public TornadoMenu()
    {
        pool = new ObjectPool();

        // Root menu
        menu = new NativeMenu("TornadoV", "Control Panel");
        menu.BannerText.Font = GTA.UI.Font.Pricedown;
        menu.Banner.Color = Color.FromArgb(0, 128, 0);
        pool.Add(menu);

        // Blank settings submenu
        settingsPage = new NativeMenu("Settings", "Settings");
        settingsPage.BannerText.Font = GTA.UI.Font.Pricedown;
        settingsPage.Banner.Color = Color.FromArgb(0, 128, 0);
        pool.Add(settingsPage);

        

        // Spawn Tornado button
        var spawnItem = new NativeItem("Spawn Tornado");
        spawnItem.Activated += (s, e) => SpawnTornado();
        menu.Add(spawnItem);

        // Despawn Tornado button
        var despawnItem = new NativeItem("Despawn Tornado");
        despawnItem.Activated += (s, e) => DespawnTornado();
        menu.Add(despawnItem);
        
        // Add Settings submenu to root menu
        var settingsItem = menu.AddSubMenu(settingsPage);

        var comingItem = new NativeItem("COMING SOON!");
        settingsPage.Add(comingItem);

        // Tick & KeyDown events
        Tick += OnTick;
        KeyDown += OnKeyDown;
    }

    private void OnTick(object sender, System.EventArgs e)
    {
        pool.Process();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            menu.Visible = !menu.Visible; // toggle menu only with F5
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
