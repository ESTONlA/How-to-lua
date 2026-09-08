using System;
using MoonSharp.Interpreter;

namespace HowToLua;

internal static class LuaArguments
{
    internal static int Integer(DynValue value, string name, int min, int max)
    {
        double number = Number(value, name, min, max);
        if (number != Math.Truncate(number)) throw new ScriptRuntimeException(name + " must be an integer.");
        return (int)number;
    }
    internal static bool Boolean(DynValue value, string name)
    {
        if (value.Type != DataType.Boolean) throw new ScriptRuntimeException(name + " must be true or false.");
        return value.Boolean;
    }
    internal static string NetworkId(DynValue value)
    {
        if (value.Type != DataType.String || !ushort.TryParse(value.String, out ushort id) || id.ToString() != value.String)
            throw new ScriptRuntimeException("Network ID must be a decimal string from a current item snapshot.");
        return value.String;
    }
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
