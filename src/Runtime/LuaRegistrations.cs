using MoonSharp.Interpreter;
using UnityEngine;

namespace HowToLua;

internal sealed class LuaAction
{
    internal readonly LuaMod Mod;
    internal readonly string Label;
    internal readonly DynValue Callback;
    internal LuaAction(LuaMod mod, string label, DynValue callback) { Mod = mod; Label = label; Callback = callback; }
}

internal sealed class LuaTimer
{
    internal readonly float Interval;
    internal readonly bool Repeat;
    internal readonly DynValue Callback;
    internal float NextRun;

    internal LuaTimer(float interval, bool repeat, DynValue callback)
    {
        Interval = interval;
        Repeat = repeat;
        Callback = callback;
        NextRun = Time.unscaledTime + interval;
    }
}

internal sealed class LuaCommand
{
    internal readonly LuaMod Mod;
    internal readonly DynValue Callback;

    internal LuaCommand(LuaMod mod, DynValue callback)
    {
        Mod = mod;
        Callback = callback;
    }
}
