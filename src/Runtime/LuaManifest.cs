using System;

namespace HowToLua;

[Serializable]
internal sealed class LuaManifest
{
    public string id = string.Empty;
    public string name = string.Empty;
    public string version = "0.1.0";
    public string author = "Unknown";
    public string entry = "main.lua";
    public bool hostOnly = true;
    public string[] dependencies = Array.Empty<string>();
}
