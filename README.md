# How to Lua

**How to Lua** is a BepInEx framework for How to Fish that loads small, manifest-based Lua mods. It uses MoonSharp, so players do not need to install Lua separately.

Lua mods run through a deliberately restricted API. They can react to supported game events, register host commands, schedule work, send chat messages, award shared money, and save their own string data. They do not receive arbitrary C# reflection or raw Unity objects.

## Install

1. Install BepInEx 5 for How to Fish and start the game once.
2. Copy both `HowToLua.dll` and `MoonSharp.Interpreter.dll` to `How to Fish/BepInEx/plugins/HowToLua/`.
3. Create or copy Lua mod folders into `How to Fish/BepInEx/plugins/HowToLua/mods/`.
4. Start the game. Open Pause and select **Lua Mods** to see loaded mods and reload them.

The framework is installed on the host for host-side game events, money rewards, and commands. Mods with `"hostOnly": false` can load on clients too, but the initial API does not provide client-only gameplay hooks.

## Lua Mod Layout

```text
mods/
  welcome-example/
    manifest.json
    main.lua
```

`manifest.json`:

```json
{
  "id": "welcome-example",
  "name": "Welcome Example",
  "version": "1.0.0",
  "author": "Estonia",
  "entry": "main.lua",
  "hostOnly": true
}
```

`id`, `name`, and the entry file are required. IDs must be unique. `entry` defaults to `main.lua` and `hostOnly` defaults to `true`.

## Quick Example

```lua
local catches = tonumber(htf.get_data("catches", "0"))

htf.on("fish_hooked", function(fish_name)
  catches = catches + 1
  htf.set_data("catches", tostring(catches))
  htf.chat("Crew catch #" .. catches .. ": " .. fish_name)
end)

htf.command("bonus", function(args)
  htf.money(25)
  htf.chat("The crew received $25.")
end)
```

Host uses `/bonus` in game chat. Commands are intentionally host-only in this first release.

## Current API

| Function | Purpose |
| --- | --- |
| `htf.on(event, callback)` | Subscribe to `fish_hooked`, `creature_killed`, `boss_killed`, or `server_command`. |
| `htf.command(name, callback)` | Register a host chat command, used as `/name args`. |
| `htf.button(label, callback)` | Add a native button under Lua Mods > Mods / Actions. |
| `htf.after(seconds, callback)` | Run a callback once after a delay. |
| `htf.every(seconds, callback)` | Run a callback repeatedly. |
| `htf.chat(message)` | Broadcast a chat message with the mod name as its prefix. |
| `htf.money(amount)` | Award shared server money while hosting. |
| `htf.get_data(key, fallback)` | Read persistent string data for this Lua mod. |
| `htf.set_data(key, value)` | Persist a string value for this Lua mod. |
| `htf.is_host()` | Return whether the local game is hosting. |
| `htf.log(message)` | Write a message to BepInEx output. |

See the [GitHub Wiki](https://github.com/ESTONlA/How-to-lua/wiki) or [local Wiki source](wiki/Home.md) for the API documentation.

Version 0.1.0 is an early beta. The build and Lua execution tests pass; multiplayer behavior and the native menu still need an in-game test.

## Development Notes

- Reloading removes all currently registered Lua mods, commands, callbacks, and timers before reading the mod folders again.
- Errors are isolated to the Lua mod that caused them and shown in both BepInEx output and the **Lua Mods** panel.
- Data is stored by BepInEx at `BepInEx/config/HowToLua.<mod-id>.cfg`.
- The first release exposes strings and numbers to Lua. It intentionally does not expose game objects, networking internals, Harmony, file I/O, or arbitrary CLR types.
- Player events are `player_joined(name, steam_id)` and `player_left(name, steam_id)`. Steam IDs are strings to preserve precision. The host checks the roster once per second.
- Optional manifest `dependencies` is an array of exact mod IDs. Missing, failed, or cyclic dependencies block loading. Version ranges and shared Lua globals are not supported.
- Startup code and `on_load()` run when scripts load, including at the main menu. Host-only event callbacks and timers remain idle until hosting. Use `htf.is_host()` before logic requiring a session.
- Lua execution is limited to 50,000 instructions per entry/callback. Only install scripts you trust: this is an in-process runtime with no hard memory quota.
- This beta does not yet expose prefab spawning, arbitrary game hooks, custom networking, JSON data tables, or client-required mod negotiation.

## Documentation Maintenance

Update `README.md` and the affected `wiki/*.md` pages whenever the API changes. `Publish-Wiki.ps1` publishes the canonical local pages to the separate GitHub Wiki repository. Documentation is updated with code changes; there is no background auto-update service.

## Building

```powershell
dotnet build HowToLua.csproj -c Release
```

Copy the framework DLL and its MoonSharp dependency from `bin/Release` into the BepInEx plugin folder.

Run `dotnet run --project tests/SmokeTests.csproj -c Release` for Lua smoke tests. `./Build-Package.ps1` builds an installable ZIP containing both DLLs, the MoonSharp license, README, and an optional example.
