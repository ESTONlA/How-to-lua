# How to Lua
# [Changelog](CHANGELOG.md) | [License](LICENSE)
**How to Lua** is a BepInEx framework for How to Fish that loads small, manifest-based Lua mods. It uses MoonSharp, so players do not need to install Lua separately.

Lua mods run through a deliberately restricted API. They can react to 27 game events, query players and world state, heal/feed/teleport living players, register host commands and native buttons, schedule work, send chat, award shared money, and save their own string data. They receive plain snapshot tables, not arbitrary C# reflection or raw Unity objects.

## Install

1. Install BepInEx 5 for How to Fish and start the game once.
2. Copy both `HowToLua.dll` and `MoonSharp.Interpreter.dll` to `How to Fish/BepInEx/plugins/HowToLua/`.
3. Create or copy Lua mod folders into `How to Fish/BepInEx/plugins/HowToLua/mods/`.
4. Start the game. Open Pause and select **Lua Mods** to see loaded mods and reload them.

The host installs the framework and scripts for gameplay events and actions. Other players receive the game's normal network updates and do not need Lua for these features. Mods with `"hostOnly": false` can run local timers and buttons on clients, but cannot execute host actions or receive host gameplay events.

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

The host uses `/bonus` in game chat. Commands are intentionally host-only.

## Current API

| Function | Purpose |
| --- | --- |
| `htf.on(event, callback)` | Subscribe to one of 27 events; see [Events](wiki/Events.md). |
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
| `htf.players.list()` / `get(steam_id)` | Get player snapshots, including health, fullness and position. |
| `htf.players.heal(steam_id, amount)` / `feed(steam_id, amount)` | Restore living players' health or fullness on the host. |
| `htf.players.teleport(steam_id, x, y, z, yaw)` | Send a living player a native teleport; yaw is optional. |
| `htf.world.info()` / `spawn_position()` / `boss()` | Read session, spawn and current boss snapshots. |
| `htf.economy.balance()` | Read the authoritative shared balance on the host. |

See the [GitHub Wiki](https://github.com/ESTONlA/How-to-lua/wiki) or [local Wiki source](wiki/Home.md) for the API documentation.

Version **0.2.0** is an early beta. Automated Lua, event-queue and installed-game patch-contract checks pass. The new hooks and player actions still need in-game multiplayer testing.

### 0.2.0 Hook Expansion

- 21 additional events: player vitals/death/revival, item pickup/drop/sale/cooking/skins, money changes, boss spawn/despawn, server lifecycle/save requests, and island changes/load completion.
- Player/world APIs with validated host-only actions. Steam IDs stay strings to avoid precision loss.
- Independent snapshot tables for each mod; queued callbacks with a 1024-event capacity and 128-event per-frame dispatch limit.
- Separate `Runtime`, `Api`, `Game`, and `UI` code modules. No gameplay logic in the plugin bootstrap.
- Optional `examples/crew-tools`: sale logging, saved per-player death counts, heal/feed crew buttons, `/luacrew`, and `/luareturn steam_id`.

The six existing events keep their original leading arguments. Some receive optional extra snapshot arguments. Events are now queued, not synchronous; they cannot cancel the underlying game action. Keep both DLLs together and fully restart after upgrading. Existing scripts and configuration do not need replacing.

### 0.1.1 Compatibility Fix

The package now uses MoonSharp's `net40-client` binary, fixing the `System.Collections, Version=4.0.10.0` / `Table:m_Values` load error from 0.1.0. Replace both DLLs and fully restart the game. Reload Lua Mods cannot replace an already loaded interpreter assembly. Keep your Lua scripts and saved configuration files.

## Development Notes

- Reloading removes all currently registered Lua mods, commands, callbacks, and timers before reading the mod folders again.
- Errors are isolated to the Lua mod that caused them and shown in both BepInEx output and the **Lua Mods** panel.
- Data is stored by BepInEx at `BepInEx/config/HowToLua.<mod-id>.cfg`.
- The API exposes primitives and plain tables. It intentionally does not expose game objects, networking internals, Harmony, file I/O, or arbitrary CLR types.
- Player events are `player_joined(name, steam_id)` and `player_left(name, steam_id)`. Steam IDs are strings to preserve precision. The host checks the roster once per second.
- Optional manifest `dependencies` is an array of exact mod IDs. Missing, failed, or cyclic dependencies block loading. Version ranges and shared Lua globals are not supported.
- Startup code and `on_load()` run when scripts load, including at the main menu. Gameplay callbacks and host-only timers wait for hosting. The `server_stopped` notification is delivered after hosting ends. Use `htf.is_host()` before logic requiring a session.
- Lua execution is limited to 50,000 instructions per entry/callback. Only install scripts you trust: this is an in-process runtime with no hard memory quota.
- This beta does not yet expose prefab spawning, arbitrary game hooks, custom networking, JSON data tables, or client-required mod negotiation.

## Documentation Maintenance

Update `README.md` and the affected `wiki/*.md` pages whenever the API changes. `Publish-Wiki.ps1` publishes the canonical local pages to the separate GitHub Wiki repository. Documentation is updated with code changes; there is no background auto-update service.

## Building

```powershell
dotnet build HowToLua.csproj -c Release
```

Copy the framework DLL and its MoonSharp dependency from `bin/Release` into the BepInEx plugin folder.

Run `dotnet run --project tests/SmokeTests.csproj -c Release` for Lua, snapshot, argument validation, event queue, coroutine completion, example and installed-game Harmony target tests. `./Build-Package.ps1` runs these checks and builds `release/HowToLua-0.2.0.zip` containing both DLLs, the MoonSharp license, README, and optional examples.

See [Architecture](wiki/Architecture.md) for where to extend hooks and APIs. Build paths currently reference this machine's Steam install; adjust the project references and test game path for a different installation.

`MoonSharp.Reference.props` selects `lib/net40-client/MoonSharp.Interpreter.dll` for both the plugin and tests. Keep this explicit reference: NuGet's automatically selected `netstandard1.6` build requires facade assemblies absent from the game. Packaging also runs a fresh Windows PowerShell check against the staged DLL to verify its references and execute Lua table/callback code under .NET Framework.
