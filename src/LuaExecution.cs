using System;
using System.IO;
using System.Text.RegularExpressions;
using MoonSharp.Interpreter;

namespace HowToLua;

internal static class LuaExecution
{
    internal static Script CreateScript() => new Script(CoreModules.Preset_HardSandbox);

    internal static void Run(Script script, DynValue function, params DynValue[] args)
    {
        if (function.Type != DataType.Function)
            throw new ScriptRuntimeException("Expected a Lua function.");
        // A fresh, bounded coroutine prevents ordinary Lua loops blocking Unity's main thread.
        Coroutine coroutine = script.CreateCoroutine(function).Coroutine;
        coroutine.AutoYieldCounter = 50000;
        coroutine.Resume(args);
        if (coroutine.State != CoroutineState.Dead)
            throw new ScriptRuntimeException("Lua instruction limit exceeded (50000 per callback).");
    }

    internal static bool ValidId(string id) => id != null && Regex.IsMatch(id, "^[a-z0-9][a-z0-9_-]{0,63}$");

    internal static string EntryPath(string folder, string entry)
    {
        string root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string path = Path.GetFullPath(Path.Combine(root, entry));
        if (Path.IsPathRooted(entry) || !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetExtension(path), ".lua", StringComparison.OrdinalIgnoreCase))
            throw new IOException("Entry must be a .lua file inside the mod folder.");
        for (string part = path; part != null; part = Path.GetDirectoryName(part))
        {
            if ((File.GetAttributes(part) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked mod files and folders are not supported.");
        }
        if (new FileInfo(path).Length > 262144)
            throw new IOException("Lua entry file exceeds 256 KiB.");
        return path;
    }
}
