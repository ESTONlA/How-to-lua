using System.IO;
using BepInEx;
using HarmonyLib;

namespace HowToLua;

[BepInPlugin("estonia.howtofish.howtolua", "How to Lua", PluginVersion)]
[BepInProcess("How to Fish.exe")]
public sealed class HowToLuaPlugin : BaseUnityPlugin
{
    internal const string PluginVersion = "0.3.0";
    private LuaHost _host;
    private LuaMenu _menu;
    private GameHooks _hooks;
    private Harmony _harmony;
    private void Awake()
    {
        string modsPath = Path.Combine(Path.GetDirectoryName(Info.Location) ?? Paths.PluginPath, "mods");
        _host = new LuaHost(Logger, modsPath);
        _menu = new LuaMenu(_host);
        _hooks = new GameHooks(_host);
        _harmony = new Harmony("estonia.howtofish.howtolua");
        _harmony.PatchAll(typeof(HowToLuaPlugin).Assembly);
        if (Config.Bind("General", "AutoLoadMods", true, "Loads valid Lua mod folders at startup.").Value)
            _host.ReloadMods();
        Logger.LogInfo($"How to Lua {PluginVersion} by Estonia. Lua mods: {modsPath}");
    }
    private void Update()
    {
        _hooks?.Tick();
        _host?.TickTimers();
        _host?.DispatchEvents();
        _menu?.Tick();
    }
    private void OnDestroy()
    {
        _hooks?.Dispose();
        _harmony?.UnpatchSelf();
        _menu?.Dispose();
        _host?.Dispose();
    }
}
