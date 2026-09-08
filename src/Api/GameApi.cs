using MoonSharp.Interpreter;

namespace HowToLua;

internal static class GameApi
{
    internal static void Register(LuaMod mod, LuaHost host)
    {
        Table api = mod.Script.Globals.Get("htf").Table;
        PlayerApi.Register(mod.Script, api, host);
        WorldApi.Register(mod.Script, api, host);
        ItemApi.Register(mod.Script, api, host);
        CatalogApi.Register(mod.Script, api, host);
        InventoryApi.Register(mod.Script, api, host);
        CombatApi.Register(mod.Script, api, host);
        ServerApi.Register(mod.Script, api, host);
        BoatApi.Register(mod.Script, api, host);
        ProgressionApi.Register(mod.Script, api, host);
        NpcApi.Register(mod.Script, api, host);
        BossApi.Register(mod.Script, api, host);
    }
}
