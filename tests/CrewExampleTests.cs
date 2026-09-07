using System;
using System.Collections.Generic;
using System.IO;
using HowToLua;
using MoonSharp.Interpreter;

internal static class CrewExampleTests
{
    internal static void Run(string repo, Action<bool, string> check)
    {
        Script script = LuaExecution.CreateScript();
        var events = new Dictionary<string, DynValue>();
        var commands = new Dictionary<string, DynValue>();
        var buttons = new Dictionary<string, DynValue>();
        var data = new Dictionary<string, string>();
        var logs = new List<string>();
        string chat = null;
        int heals = 0, feeds = 0, teleports = 0;
        var player = new Dictionary<string, object> { ["name"] = "Host", ["steam_id"] = "76561198000000001" };
        var api = new Table(script);
        api.Set("on", DynValue.NewCallback((c, a) => { events[a[0].String] = a[1]; return DynValue.Nil; }));
        api.Set("command", DynValue.NewCallback((c, a) => { commands[a[0].String] = a[1]; return DynValue.Nil; }));
        api.Set("button", DynValue.NewCallback((c, a) => { buttons[a[0].String] = a[1]; return DynValue.Nil; }));
        api.Set("log", DynValue.NewCallback((c, a) => { logs.Add(a[0].String); return DynValue.Nil; }));
        api.Set("chat", DynValue.NewCallback((c, a) => { chat = a[0].String; return DynValue.Nil; }));
        api.Set("get_data", DynValue.NewCallback((c, a) => DynValue.NewString(data.TryGetValue(a[0].String, out var value) ? value : a[1].String)));
        api.Set("set_data", DynValue.NewCallback((c, a) => { data[a[0].String] = a[1].String; return DynValue.Nil; }));
        var players = new Table(script);
        players.Set("list", DynValue.NewCallback((c, a) => LuaValues.From(script, new object[] { player, new Dictionary<string, object> { ["steam_id"] = "0" } })));
        players.Set("heal", DynValue.NewCallback((c, a) => { heals++; return DynValue.True; }));
        players.Set("feed", DynValue.NewCallback((c, a) => { feeds++; return DynValue.True; }));
        players.Set("teleport", DynValue.NewCallback((c, a) =>
        {
            check(a[0].String == "76561198000000001" && a[1].Number == 10 && a[2].Number == 20 && a[3].Number == 30, "crew return sends exact ID and spawn coordinates");
            teleports++;
            return DynValue.True;
        }));
        var world = new Table(script);
        world.Set("spawn_position", DynValue.NewCallback((c, a) => LuaValues.From(script, new Dictionary<string, object> { ["x"] = 10, ["y"] = 20, ["z"] = 30 })));
        var economy = new Table(script);
        economy.Set("balance", DynValue.NewCallback((c, a) => DynValue.NewNumber(125)));
        api.Set("players", DynValue.NewTable(players));
        api.Set("world", DynValue.NewTable(world));
        api.Set("economy", DynValue.NewTable(economy));
        script.Globals.Set("htf", DynValue.NewTable(api));
        LuaExecution.Run(script, script.LoadString(File.ReadAllText(Path.Combine(repo, "examples/crew-tools/main.lua"))));
        foreach (string name in events.Keys) check(GameEventCatalog.Names.Contains(name), "crew example registers supported event: " + name);
        LuaExecution.Run(script, events["fish_sold"], LuaValues.From(script, new Dictionary<string, object> { ["name"] = "Test fish", ["worth"] = 50 }), DynValue.Nil);
        check(logs.Contains("Unknown sold Test fish for $50"), "sale example handles missing seller");
        LuaExecution.Run(script, events["player_died"], LuaValues.From(script, player));
        LuaExecution.Run(script, events["player_died"], DynValue.Nil);
        check(data["deaths_76561198000000001"] == "1", "death example stores per-player string count and handles nil player");
        LuaExecution.Run(script, buttons["Heal crew"]);
        LuaExecution.Run(script, buttons["Feed crew"]);
        check(heals == 1 && feeds == 1, "crew buttons skip zero-ID players");
        LuaExecution.Run(script, commands["luacrew"]);
        check(chat == "Crew: 2 | Balance: $125", "crew status command combines query results");
        LuaExecution.Run(script, commands["luareturn"], LuaValues.From(script, Array.Empty<object>()));
        LuaExecution.Run(script, commands["luareturn"], LuaValues.From(script, new object[] { "99999999999999999999" }));
        check(teleports == 0, "return command rejects missing or unmatched IDs without an API error");
        LuaExecution.Run(script, commands["luareturn"], LuaValues.From(script, new object[] { "76561198000000001" }));
        check(teleports == 1, "crew return command invokes teleport once");
    }
}
