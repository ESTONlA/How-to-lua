using System;
using System.Collections;
using System.Collections.Generic;
using HowToLua;
using MoonSharp.Interpreter;

internal static class EventTests
{
    internal static void Run(Action<bool, string> check, Action<Action, string> reject)
    {
        var script = LuaExecution.CreateScript();
        var second = LuaExecution.CreateScript();
        var snapshot = new Dictionary<string, object>
        {
            ["name"] = "Player", ["steam_id"] = "76561198000000001", ["health"] = 75,
            ["position"] = new Dictionary<string, object> { ["x"] = 10f, ["y"] = 2f, ["z"] = 3f }
        };
        var firstValue = LuaValues.From(script, snapshot);
        var secondValue = LuaValues.From(second, snapshot);
        firstValue.Table.Get("position").Table.Set("x", DynValue.NewNumber(999));
        check(secondValue.Table.Get("position").Table.Get("x").Number == 10, "nested event snapshots isolated across mods");
        check(secondValue.Table.Get("steam_id").String == "76561198000000001", "Steam ID retains full precision");
        var array = LuaValues.From(script, new object[] { snapshot, "text", null, true }).Table;
        check(array.Get(1).Type == DataType.Table && array.Get(2).String == "text" && array.Get(3).IsNil() && array.Get(4).Boolean, "snapshot arrays preserve Lua indexing including nil holes");
        reject(() => LuaValues.From(script, new object()), "raw CLR objects rejected");
        reject(() => LuaValues.From(script, firstValue), "cross-script Lua tables rejected");
        check(LuaArguments.SteamId(DynValue.NewString("76561198000000001")) == "76561198000000001", "string Steam ID accepted");
        reject(() => LuaArguments.SteamId(DynValue.NewNumber(76561198000000001d)), "numeric Steam ID rejected");
        reject(() => LuaArguments.SteamId(DynValue.NewString("0")), "ambiguous zero Steam ID rejected");
        reject(() => LuaArguments.SteamId(DynValue.NewString(" 123")), "noncanonical Steam ID rejected");
        check(LuaArguments.Number(DynValue.NewNumber(100), "amount", 0, 100) == 100, "numeric upper bound inclusive");
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, -1, 101 })
            reject(() => LuaArguments.Number(DynValue.NewNumber(invalid), "amount", 0, 100), "invalid amount rejected: " + invalid);
        reject(() => LuaArguments.Number(DynValue.NewString("50"), "amount", 0, 100), "numeric arguments require a number");

        var queue = new GameEventQueue();
        queue.Add("first", Array.Empty<object>());
        int called = 0;
        queue.Drain((name, args) => { called++; queue.Add("nested", args); });
        check(called == 1, "reentrant events defer until next drain");
        queue.Drain((name, args) => { called++; check(name == "nested", "deferred event preserves order"); });
        check(called == 2, "deferred event delivered once");
        for (int i = 0; i < 1024; i++) queue.Add("flood", Array.Empty<object>());
        check(!queue.Add("overflow", Array.Empty<object>()), "queue bounded at 1024 events");
        called = 0;
        queue.Drain((name, args) => called++);
        check(called == 128, "per-frame dispatch bounded at 128 events");
        queue.Clear();
        queue.Drain((name, args) => called++);
        check(called == 128, "reload/session reset drops stale events");

        bool disposed = false, completed = false;
        IEnumerator Routine()
        {
            try { yield return "first"; yield return "second"; }
            finally { disposed = true; }
        }
        IEnumerator observed = ObservedRoutine.Wrap(Routine(), () => completed = true);
        check(observed.MoveNext() && (string)observed.Current == "first" && !completed, "island wrapper preserves first yield");
        check(observed.MoveNext() && (string)observed.Current == "second" && !completed, "completion waits for final native yield");
        check(!observed.MoveNext() && disposed && completed, "natural completion notifies and disposes");
        disposed = completed = false;
        observed = ObservedRoutine.Wrap(Routine(), () => completed = true);
        observed.MoveNext();
        ((IDisposable)observed).Dispose();
        check(disposed && !completed, "cancelled island routine does not report completion");
        check(GameEventCatalog.Names.Count == 27, "27 supported event names");
    }
}
