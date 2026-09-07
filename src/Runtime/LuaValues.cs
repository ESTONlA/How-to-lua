using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace HowToLua;

internal static class LuaValues
{
    internal static DynValue From(Script script, object value)
    {
        if (value == null) return DynValue.Nil;
        if (value is string text) return DynValue.NewString(text);
        if (value is bool flag) return DynValue.NewBoolean(flag);
        if (value is DynValue scalar && (scalar.Type == DataType.Nil || scalar.Type == DataType.String || scalar.Type == DataType.Number || scalar.Type == DataType.Boolean)) return scalar;
        if (value is IDictionary<string, object> record)
        {
            var table = new Table(script);
            foreach (var pair in record) table.Set(pair.Key, From(script, pair.Value));
            return DynValue.NewTable(table);
        }
        if (value is object[] array)
        {
            var table = new Table(script);
            for (int i = 0; i < array.Length; i++) table.Set(i + 1, From(script, array[i]));
            return DynValue.NewTable(table);
        }
        if (value is int || value is byte || value is float || value is double || value is long)
            return DynValue.NewNumber(Convert.ToDouble(value));
        throw new InvalidOperationException("Unsupported Lua snapshot type: " + value.GetType().Name);
    }
}
