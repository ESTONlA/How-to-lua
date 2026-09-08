using System;
using System.IO;
using System.Collections.Generic;
using HowToLua;
using MoonSharp.Interpreter;

internal static class WorldExampleTests
{
    internal static void Run(string repo, Action<bool, string> check)
    {
        Script script = LuaExecution.CreateScript();
        var events = new Dictionary<string, DynValue>();
        var commands = new Dictionary<string, DynValue>();
        var buttons = new Dictionary<string, DynValue>();
        var logs = new List<string>();
        int spawns = 0;
        bool allowSpawn = true, hasPlayer = true, saveAccepted = false;
        string chat = null;
        var api = new Table(script);
        api.Set("on", DynValue.NewCallback((c, a) => { events[a[0].String] = a[1]; return DynValue.Nil; }));
        api.Set("command", DynValue.NewCallback((c, a) => { commands[a[0].String] = a[1]; return DynValue.Nil; }));
        api.Set("button", DynValue.NewCallback((c, a) => { buttons[a[0].String] = a[1]; return DynValue.Nil; }));
        api.Set("log", DynValue.NewCallback((c, a) => { logs.Add(a[0].String); return DynValue.Nil; }));
        api.Set("chat", DynValue.NewCallback((c, a) => { chat = a[0].String; return DynValue.Nil; }));
        Table Group(string name) { var table = new Table(script); api.Set(name, DynValue.NewTable(table)); return table; }
        var definition = new Dictionary<string, object> { ["id"] = 4, ["name"] = "Test", ["spawn_allowed"] = true };
        var catalog = Group("catalog");
        catalog.Set("item", DynValue.NewCallback((c, a) => { definition["spawn_allowed"] = allowSpawn; return LuaValues.From(script, definition); }));
        catalog.Set("items", DynValue.NewCallback((c, a) => LuaValues.From(script, new object[] { definition })));
        Group("players").Set("local_player", DynValue.NewCallback((c, a) => LuaValues.From(script, hasPlayer ? new Dictionary<string, object>
        { ["position"] = new Dictionary<string, object> { ["x"] = 10, ["y"] = 20, ["z"] = 30 } } : null)));
        Group("items").Set("spawn", DynValue.NewCallback((c, a) =>
        {
            check(a[0].Number == 4 && a[1].Number == 13 && a[2].Number == 21 && a[3].Number == 30, "world example spawns at host-relative coordinates");
            spawns++;
            return LuaValues.From(script, new Dictionary<string, object> { ["name"] = "Test", ["network_id"] = "42" });
        }));
        Group("server").Set("save", DynValue.NewCallback((c, a) => DynValue.NewBoolean(saveAccepted)));
        Group("boat").Set("info", DynValue.NewCallback((c, a) => LuaValues.From(script,
            new Dictionary<string, object> { ["motor"] = 2, ["radar_unlocked"] = true })));
        script.Globals.Set("htf", DynValue.NewTable(api));
        LuaExecution.Run(script, script.LoadString(File.ReadAllText(Path.Combine(repo, "examples/world-tools/main.lua"))));
        check(spawns == 0, "world example startup makes no gameplay changes");
        foreach (string name in events.Keys) check(GameEventCatalog.Names.Contains(name), "world example subscribes to supported event: " + name);
        void Spawn(params object[] args) => LuaExecution.Run(script, commands["luaspawn"], LuaValues.From(script, args));
        Spawn(); Spawn("hello"); Spawn("4.5"); Spawn("256"); Spawn("-1");
        check(spawns == 0, "world spawn command rejects missing, fractional and invalid IDs");
        allowSpawn = false;
        Spawn("4");
        check(spawns == 0, "world example refuses protected definitions");
        allowSpawn = true;
        hasPlayer = false;
        Spawn("4");
        check(spawns == 0, "world example handles unavailable host player");
        hasPlayer = true;
        Spawn("4");
        check(spawns == 1 && logs.Contains("Spawned Test [42]"), "world spawn command reports spawned network ID");
        LuaExecution.Run(script, buttons["Save world"]);
        check(logs.Contains("World not ready or save cooldown active"), "world save button reports refused request");
        saveAccepted = true;
        LuaExecution.Run(script, buttons["Save world"]);
        check(logs.Contains("World save requested. Check the game log for disk errors."), "world save button does not promise disk persistence");
        LuaExecution.Run(script, buttons["Show boat status"]);
        check(chat == "Boat motor: 2 | Radar: true", "world example reads boat snapshot");
        LuaExecution.Run(script, commands["luacatalog"]);
        check(logs.Contains("4: Test | Spawn allowed: true"), "world catalog command lists prefab IDs");
        LuaExecution.Run(script, events["boat_motor_changed"], DynValue.NewNumber(1), DynValue.NewNumber(2));
        LuaExecution.Run(script, events["npc_quest_progressed"], DynValue.NewNumber(1), DynValue.NewNumber(0), DynValue.NewNumber(3), DynValue.NewString("ItemReceived"));
        check(logs.Contains("Boat motor changed: 1 -> 2") && logs.Contains("NPC 1 quest 0: 3 (ItemReceived)"), "world example handles boat and quest events");
    }
}
