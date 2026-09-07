using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using MoonSharp.Interpreter;

namespace HowToLua;

internal sealed class LuaMod
{
    internal readonly ConfigFile Data;
    private readonly LuaHost _host;
    internal readonly Dictionary<string, DynValue> Events = new(StringComparer.OrdinalIgnoreCase);
    internal readonly List<LuaTimer> Timers = new();
    internal readonly LuaManifest Manifest;
    internal readonly string Folder;
    internal Script Script;
    internal bool Loaded;
    internal string Error;

    internal LuaMod(LuaManifest manifest, string folder, string dataPath, LuaHost host)
    {
        Manifest = manifest;
        Folder = folder;
        _host = host;
        Data = new ConfigFile(dataPath, true);
    }

    internal void CreateScript(LuaHost framework)
    {
        Script = LuaExecution.CreateScript();
        CoreApi.Register(this, framework);
    }

    internal void InvokeOptional(string name)
    {
        DynValue callback = Script.Globals.Get(name);
        if (callback != null && (callback.Type == DataType.Function || callback.Type == DataType.ClrFunction))
        {
            Call(callback);
        }
    }

    internal void Call(DynValue callback, params DynValue[] args)
    {
        try
        {
            LuaExecution.Run(Script, callback, args);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    internal void SetError(string message)
    {
        Error = message;
        Loaded = false;
        _host.DisableMod(this);
        _host.FrameworkLogger.LogError("Lua mod '" + Manifest.id + "': " + message);
    }

    internal static string SafeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || !value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            throw new ScriptRuntimeException("Data keys require 1-64 letters, digits, underscores or hyphens.");
        return value;
    }
}
