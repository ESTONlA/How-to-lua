using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using MoonSharp.Interpreter;
using UnityEngine;

namespace HowToLua;

internal sealed class LuaHost
{
    private readonly Dictionary<string, LuaMod> _mods = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LuaCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _foldersById = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loading = new(StringComparer.OrdinalIgnoreCase);
    internal readonly List<LuaAction> Actions = new();
    private readonly string _modsPath;
    private readonly ManualLogSource Logger;
    internal ManualLogSource FrameworkLogger => Logger;
    internal IReadOnlyDictionary<string, LuaMod> Mods => _mods;
    internal bool IsHost => Server.Instance && Server.Instance.IsServerInitialized;
    internal LuaHost(ManualLogSource logger, string modsPath)
    {
        Logger = logger;
        _modsPath = modsPath;
        Directory.CreateDirectory(modsPath);
    }
    private readonly GameEventQueue _events = new();
    private readonly HashSet<Item> _spawned = new();
    private readonly WindowBudget _spawnBudget = new();
    internal readonly WindowBudget SaveBudget = new();
    internal readonly WindowBudget TravelBudget = new();
    internal bool AllowSpawn()
    {
        _spawned.RemoveWhere(i => !i || i.IsDeinitializing || i.IsDestroying);
        return IsHost && _spawned.Count < 128 && _spawnBudget.Take(Time.unscaledTime, 8, 1);
    }
    internal void TrackSpawn(Item item) { if (item) _spawned.Add(item); }
    private bool _warnedQueue;
    internal void Dispose() { _mods.Clear(); _commands.Clear(); Actions.Clear(); _events.Clear(); }
    internal void DispatchEvents()
    {
        _events.Drain((name, args) =>
        {
            foreach (LuaMod mod in _mods.Values.ToArray())
            {
                if (!mod.Loaded || (!IsHost && name != "server_stopped") || !mod.Events.TryGetValue(name, out DynValue callback)) continue;
                try { mod.Call(callback, args.Select(value => LuaValues.From(mod.Script, value)).ToArray()); }
                catch (Exception ex) { mod.SetError("Event " + name + ": " + ex.Message); }
            }
        });
    }
    internal void ResetSessionEvents() { _events.Clear(); _warnedQueue = false; }
    internal void ReloadMods()
    {
        _events.Clear();
        _warnedQueue = false;
        _mods.Clear();
        _commands.Clear();
        Actions.Clear();
        _foldersById.Clear();
        _loading.Clear();
        Directory.CreateDirectory(_modsPath);
        foreach (string folder in Directory.GetDirectories(_modsPath).OrderBy(x => x))
        {
            try
            {
                LuaManifest manifest = ReadManifest(folder);
                if (!LuaExecution.ValidId(manifest.id)) throw new IOException("Invalid manifest id.");
                if (_foldersById.ContainsKey(manifest.id)) throw new IOException("Duplicate manifest id: " + manifest.id);
                _foldersById.Add(manifest.id, folder);
            }
            catch (Exception ex) { Logger.LogError($"Cannot read '{folder}': {ex.Message}"); }
        }
        foreach (string folder in _foldersById.Values.ToArray())
        {
            try { LoadMod(folder); }
            catch (Exception ex) { Logger.LogError($"Cannot load '{folder}': {ex.Message}"); }
        }

        Logger.LogInfo($"Loaded {_mods.Values.Count(x => x.Loaded)} Lua mod(s) from {_modsPath}.");
    }

    private void LoadMod(string folder)
    {
        if (!_loading.Add(folder)) throw new IOException("Lua dependency cycle at " + folder);
        try { LoadModCore(folder); }
        finally { _loading.Remove(folder); }
    }

    private static LuaManifest ReadManifest(string folder)
    {
        string file = Path.Combine(folder, "manifest.json");
        if (new FileInfo(file).Length > 16384) throw new IOException("Manifest exceeds 16 KiB.");
        LuaManifest manifest = new LuaManifest();
        JsonUtility.FromJsonOverwrite(File.ReadAllText(file), manifest);
        return manifest;
    }

    private void LoadModCore(string folder)
    {
        string manifestPath = Path.Combine(folder, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return;
        }

        LuaManifest manifest;
        try
        {
            if (new FileInfo(manifestPath).Length > 16384) throw new IOException("Manifest exceeds 16 KiB.");
            manifest = new LuaManifest();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(manifestPath), manifest);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to read Lua manifest '{manifestPath}': {ex.Message}");
            return;
        }

        if (manifest == null || !LuaExecution.ValidId(manifest.id) || string.IsNullOrWhiteSpace(manifest.name))
        {
            Logger.LogError($"Lua mod at '{folder}' needs a manifest id and name.");
            return;
        }

        manifest.entry = string.IsNullOrWhiteSpace(manifest.entry) ? "main.lua" : manifest.entry;
        string entryPath = LuaExecution.EntryPath(folder, manifest.entry);

        if (_mods.ContainsKey(manifest.id))
        {
            return;
        }

        foreach (string dependency in manifest.dependencies ?? Array.Empty<string>())
        {
            if (!LuaExecution.ValidId(dependency) || !_foldersById.TryGetValue(dependency, out string dependencyFolder))
                throw new IOException($"{manifest.id} requires missing mod '{dependency}'.");
            if (!_mods.ContainsKey(dependency)) LoadMod(dependencyFolder);
            if (!_mods.TryGetValue(dependency, out LuaMod required) || !required.Loaded)
                throw new IOException($"{manifest.id} requires working mod '{dependency}'.");
        }

        LuaMod mod = new LuaMod(manifest, folder, Path.Combine(Paths.ConfigPath, $"HowToLua.{SafeFileName(manifest.id)}.cfg"), this);
        _mods.Add(manifest.id, mod);
        try
        {
            mod.CreateScript(this);
            LuaExecution.Run(mod.Script, mod.Script.LoadString(File.ReadAllText(entryPath), null, manifest.id + "/" + manifest.entry));
            mod.Loaded = string.IsNullOrWhiteSpace(mod.Error);
            if (mod.Loaded)
            {
                mod.InvokeOptional("on_load");
            }

            if (mod.Loaded)
            {
                Logger.LogInfo($"Loaded Lua mod: {manifest.name} ({manifest.id}) {manifest.version}");
            }
        }
        catch (Exception ex)
        {
            mod.SetError(ex.Message);
        }
    }

    internal void RegisterEvent(LuaMod mod, string eventName, DynValue callback)
    {
        eventName = (eventName ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsCallable(callback) || !GameEventCatalog.Names.Contains(eventName))
        {
            throw new ScriptRuntimeException("htf.on requires a supported event name and a Lua function.");
        }

        mod.Events[eventName.Trim().ToLowerInvariant()] = callback;
    }

    internal void DisableMod(LuaMod mod)
    {
        foreach (string name in _commands.Where(x => x.Value.Mod == mod).Select(x => x.Key).ToArray())
            _commands.Remove(name);
        mod.Events.Clear();
        mod.Timers.Clear();
        Actions.RemoveAll(x => x.Mod == mod);
        foreach (LuaMod dependent in _mods.Values.Where(x => x.Loaded && (x.Manifest.dependencies ?? Array.Empty<string>()).Contains(mod.Manifest.id)).ToArray())
            dependent.SetError("Dependency failed: " + mod.Manifest.id);
    }

    internal void RegisterAction(LuaMod mod, string label, DynValue callback)
    {
        if (string.IsNullOrWhiteSpace(label) || label.Length > 48 || !IsCallable(callback) || Actions.Count(x => x.Mod == mod) >= 16)
            throw new ScriptRuntimeException("Button requires a label (1-48 characters) and function; maximum 16 per mod.");
        Actions.Add(new LuaAction(mod, label, callback));
    }

    internal void RegisterCommand(LuaMod mod, string name, DynValue callback)
    {
        name = (name ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsCallable(callback) || !IsValidCommand(name))
        {
            throw new ScriptRuntimeException("htf.command requires a command name and a Lua function.");
        }

        if (_commands.ContainsKey(name))
        {
            throw new ScriptRuntimeException($"Command /{name} is already registered.");
        }

        _commands.Add(name, new LuaCommand(mod, callback));
    }

    internal void AddTimer(LuaMod mod, float seconds, bool repeat, DynValue callback)
    {
        if (!IsCallable(callback) || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0.05f || seconds > 86400f || mod.Timers.Count >= 64)
        {
            throw new ScriptRuntimeException("Timer requires 0.05-86400 seconds and a Lua function; maximum 64 timers per mod.");
        }

        mod.Timers.Add(new LuaTimer(Mathf.Max(0.05f, seconds), repeat, callback));
    }

    internal void Emit(string eventName, params object[] arguments)
    {
        if (!IsHost && eventName != "server_stopped") return;
        if (!_events.Add(eventName, arguments) && !_warnedQueue)
        {
            _warnedQueue = true;
            Logger.LogWarning("Lua game event queue reached 1024 entries; excess events are dropped. Check scripts for feedback loops.");
        }
    }

    internal void TickTimers()
    {
        float now = Time.unscaledTime;
        foreach (LuaMod mod in _mods.Values.ToArray())
        {
            if (!mod.Loaded || mod.Manifest.hostOnly && !IsHost)
            {
                continue;
            }

            foreach (LuaTimer timer in mod.Timers.ToArray())
            {
                if (!mod.Loaded) break;
                if (now < timer.NextRun)
                {
                    continue;
                }

                if (timer.Repeat)
                {
                    timer.NextRun = now + timer.Interval;
                }
                else
                {
                    mod.Timers.Remove(timer);
                }
                mod.Call(timer.Callback);
            }
        }
    }

    internal bool TryRunCommand(string rawCommand)
    {
        if (!IsHost || string.IsNullOrWhiteSpace(rawCommand) || rawCommand[0] != '/')
        {
            return false;
        }

        string[] parts = rawCommand.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !_commands.TryGetValue(parts[0].ToLowerInvariant(), out LuaCommand command) || !command.Mod.Loaded)
        {
            return false;
        }

        Table args = new Table(command.Mod.Script);
        for (int i = 1; i < parts.Length; i++)
        {
            args.Set(i, DynValue.NewString(parts[i]));
        }

        command.Mod.Call(command.Callback, DynValue.NewTable(args));
        Emit("server_command", parts[0].ToLowerInvariant(), parts.Skip(1).ToArray());
        return true;
    }

    private static bool IsCallable(DynValue value)
    {
        return value != null && value.Type == DataType.Function;
    }

    private static bool IsValidCommand(string command)
    {
        return command.Length > 0 && command.Length <= 32 && command.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }

    private static string SafeFileName(string value)
    {
        return new string(value.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
    }

}
