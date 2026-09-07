using MoonSharp.Interpreter;

namespace HowToLua;

internal static class GameApi
{
    internal static void Register(LuaMod mod, LuaHost host)
    {
        Table api = mod.Script.Globals.Get("htf").Table;
        PlayerApi.Register(mod.Script, api, host);
        WorldApi.Register(mod.Script, api, host);
    }
}
