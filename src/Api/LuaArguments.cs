using System;
using MoonSharp.Interpreter;

namespace HowToLua;

internal static class LuaArguments
{
    internal static double Number(DynValue value, string name, double min, double max)
    {
        if (value.Type != DataType.Number || double.IsNaN(value.Number) || double.IsInfinity(value.Number) || value.Number < min || value.Number > max)
            throw new ScriptRuntimeException(name + " must be a finite number between " + min + " and " + max + ".");
        return value.Number;
    }

    internal static string SteamId(DynValue value)
    {
        // Steam IDs exceed Lua's exact integer range. Never accept them as doubles.
        if (value.Type != DataType.String || !ulong.TryParse(value.String, out ulong id) || id == 0 || id.ToString() != value.String)
            throw new ScriptRuntimeException("Steam ID must be a nonzero decimal string, not a number.");
        return value.String;
    }
}
