using System;
using System.IO;
using System.Collections.Generic;
using HowToLua;
using MoonSharp.Interpreter;

int passed = 0;
void Check(bool value, string name)
{
    if (!value) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}
void Reject(Action action, string name)
{
    bool rejected = false;
    try { action(); } catch { rejected = true; }
    Check(rejected, name);
}

Script script = LuaExecution.CreateScript();
var references = typeof(Script).Assembly.GetReferencedAssemblies();
Check(Array.Exists(references, x => x.Name == "mscorlib") &&
      !Array.Exists(references, x => x.Name == "System.Collections" || x.Name == "System.Runtime"),
      "MoonSharp uses classic framework references compatible with Unity Mono");
LuaExecution.Run(script, script.LoadString("answer = tonumber('42'); words = tostring(answer)"));
Check(script.Globals.Get("answer").Number == 42, "basic Lua execution");
foreach (string global in new[] { "io", "os", "debug", "require", "loadfile", "dofile", "luanet", "coroutine" })
    Check(script.Globals.Get(global).IsNil(), global + " not exposed");
Reject(() => LuaExecution.Run(script, script.LoadString("while true do end")), "infinite startup loop terminated");
Script other = LuaExecution.CreateScript();
LuaExecution.Run(other, other.LoadString("function broken() while true do end end"));
Reject(() => LuaExecution.Run(other, other.Globals.Get("broken")), "infinite callback terminated");
Reject(() => other.LoadString("function !"), "syntax error reported");
LuaExecution.Run(other, other.LoadString("healthy = 1 + 2"));
Check(other.Globals.Get("healthy").Number == 3, "another script still executes after failures");
Check(LuaExecution.ValidId("welcome-example") && !LuaExecution.ValidId("../bad") && !LuaExecution.ValidId("Bad.ID"), "manifest ID validation");

string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
string folder = Path.Combine(repo, "examples", "welcome");
Check(File.Exists(LuaExecution.EntryPath(folder, "main.lua")), "local Lua entry accepted");
Reject(() => LuaExecution.EntryPath(folder, "../welcome-other/main.lua"), "sibling prefix escape rejected");
Reject(() => LuaExecution.EntryPath(folder, Path.Combine(folder, "main.lua")), "absolute entry rejected");

Script sample = LuaExecution.CreateScript();
var handlers = new Dictionary<string, DynValue>();
var commands = new Dictionary<string, DynValue>();
var data = new Dictionary<string, string>();
int money = 0;
DynValue action = DynValue.Nil;
Table api = new Table(sample);
api["on"] = DynValue.NewCallback((ctx, a) => { handlers[a[0].String] = a[1]; return DynValue.Nil; });
api["command"] = DynValue.NewCallback((ctx, a) => { commands[a[0].String] = a[1]; return DynValue.Nil; });
api["get_data"] = DynValue.NewCallback((ctx, a) => DynValue.NewString(data.TryGetValue(a[0].String, out string v) ? v : a[1].String));
api["set_data"] = DynValue.NewCallback((ctx, a) => { data[a[0].String] = a[1].String; return DynValue.Nil; });
api["chat"] = DynValue.NewCallback((ctx, a) => DynValue.Nil);
api["log"] = DynValue.NewCallback((ctx, a) => DynValue.Nil);
api["money"] = DynValue.NewCallback((ctx, a) => { money += (int)a[0].Number; return DynValue.Nil; });
api["button"] = DynValue.NewCallback((ctx, a) => { action = a[1]; return DynValue.Nil; });
sample.Globals["htf"] = api;
LuaExecution.Run(sample, sample.LoadString(File.ReadAllText(Path.Combine(folder, "main.lua"))));
LuaExecution.Run(sample, handlers["fish_hooked"], DynValue.NewString("Test fish"));
Check(data["catches"] == "1", "bundled example handles event and persists counter");
LuaExecution.Run(sample, commands["bonus"], DynValue.NewTable(new Table(sample)));
Check(money == 25, "bundled example command awards configured amount");
LuaExecution.Run(sample, action);
Check(action.Type == DataType.Function, "bundled example registers callable native action");
EventTests.Run(Check, Reject);
HookContractTests.Run(repo, Check);
CrewExampleTests.Run(repo, Check);
string eventDocs = File.ReadAllText(Path.Combine(repo, "wiki", "Events.md"));
foreach (string name in GameEventCatalog.Names)
    Check(eventDocs.Contains("| `" + name + "` |"), "wiki documents event: " + name);
Console.WriteLine($"{passed} checks passed.");
