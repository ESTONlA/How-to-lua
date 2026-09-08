# How to Lua

[![Game method integration](coverage/method-coverage.svg)](coverage/README.md)
[![Lua functions: 79](https://img.shields.io/badge/Lua_functions-79-16a34a?style=flat-square)](wiki/Gameplay-API.md)
[![Game events: 50](https://img.shields.io/badge/game_events-50-0891b2?style=flat-square)](wiki/Events.md)
[![Hook targets: 39](https://img.shields.io/badge/hook_targets-39-6366f1?style=flat-square)](wiki/Architecture.md)

# [Changelog](CHANGELOG.md) | [License](LICENSE)
**How to Lua** is a BepInEx framework for How to Fish that loads small, manifest-based Lua mods. It uses MoonSharp, so players do not need to install Lua separately.

Lua mods run through a deliberately restricted API. Version **0.3.0** exposes **79 functions and 50 events** for players, items, inventories, combat, boats, bosses, NPC quests, islands and server rules, alongside commands, native buttons, timers and persistent string data. Scripts receive plain snapshot tables, not arbitrary C# reflection or raw Unity objects.

## Game Coverage

The badge measures **direct game-method coverage**: unique methods called/read or hooked by the Lua gameplay bridges, divided by the non-constructor methods declared in the installed game's `Assembly-CSharp.dll`.

The generated [coverage report](coverage/README.md) lists the exact count, percentage and every integrated method. Against the measured game build, integration increased from **41/4,737 (0.87%)** in 0.2.0 to **229/4,737 (4.83%)** in 0.3.0. The framework has **39 Harmony hook targets**; some supply multiple events, while lifecycle events also come from polling.

| Game system | Available to Lua |
| --- | --- |
| Players | Join/leave, vitals, death/revival, lookup, healing, feeding, teleporting, damage and status effects. |
| Fishing and bosses | Hook/release, creature health/deaths, boss lifecycle, immortality and crew-scaled health/damage queries. |
| Items and catalog | Prefab definitions, bounded spawning, nearby/list queries, loose-item removal, cooking, skins and score bonuses. |
| Inventory and combat | Slots, held items, bait stocks, pocket unlocks, weapon stats, attachment events and native upgrades. |
| Boats | Status, driver events/ejection, motor upgrades, skins, radar and return-to-spawn. |
| Economy and server | Balance changes, affordability, give/spend, save requests, difficulty, friendly fire and one-shot rules. |
| World and NPCs | Island travel/unlocks, load events, water queries, NPC locations, quest progress and grill unlocks. |

This counts actual methods, not documentation. Property accessors and generated networking methods are included in the denominator; Unity/FishNet DLLs are not. Reading a property or observing a hook counts as direct integration, not unrestricted Lua access to that method. Internal calls made by the game are not automatically marked supported. Field reads and native-menu wiring do not increase the method count.

It is **not feature-completion, line coverage or a runtime-test percentage**. Each system is only partially exposed. Arbitrary object editing, boss spawning, custom networking and unrestricted game-method calls are not implemented.

Recalculate from the current game and framework DLLs after adding support:

```powershell
dotnet run --project tests/SmokeTests.csproj -c Release -- --coverage
```

This rebuilds the framework and regenerates the local badge, method list and JSON report in `coverage/`. Commit those files with the code changes to update the GitHub README badge; it does not update itself from a running game.

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
| `htf.on(event, callback)` | Subscribe to one of 50 events; see [Events](wiki/Events.md). |
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

The table above covers the original 20 functions. The [Gameplay API](wiki/Gameplay-API.md) documents all **59 additions** with argument bounds, return values and snapshot fields, including `htf.items`, `catalog`, `inventory`, `combat`, `server`, `boat`, `npcs`, `bosses`, and extensions to players/world/economy.

Version **0.3.0** is an early beta. **405 automated checks pass**, including Lua examples, event queues, all 39 installed-game patch targets, private-field contracts and API documentation checks. These are not a substitute for in-game multiplayer testing; new gameplay actions and remote replication remain unverified in a live session.

### 0.3.0 Gameplay Expansion

- 59 additional functions in separate domain modules; 23 additional events for inventory, equipment, boats, NPC quests, item lifecycle and difficulty.
- Host-only native gameplay mutations with validated IDs and bounds. Network object IDs and Steam IDs remain strings.
- Item spawning: at most 8 attempts per second and 128 live framework-spawned items across scripts. Bosses, player bodies and quest-marked prefabs are blocked. Cleanup refuses held, stored, rod-attached, bird-held or protected items.
- World-save requests have a shared 5-second cooldown; island travel has a shared 2-second cooldown and refuses during loading. Native disk completion and remote-client receipt are not guaranteed by a true return value.
- Optional `examples/world-tools`: `/luacatalog`, `/luaspawn definition_id`, **Show boat status**, **Save world**, and boat/quest event logging. No automatic gameplay changes on load. Copy it into `mods` to enable it; test on a spare save.
- Native grants such as bait, pockets and upgrades do not charge money automatically. Scripts can pair them with the economy API.

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
- Bounded item-prefab spawning is supported, but arbitrary game hooks, custom networking, JSON data tables and client-required mod negotiation are not. Lua's instruction budget does not meter the native cost of API calls; avoid polling large catalogs every frame.

## Documentation Maintenance

Update `README.md` and the affected `wiki/*.md` pages whenever the API changes. `Publish-Wiki.ps1` publishes the canonical local pages to the separate GitHub Wiki repository. Documentation is updated with code changes; there is no background auto-update service.

## Building

```powershell
dotnet build HowToLua.csproj -c Release
```

Copy the framework DLL and its MoonSharp dependency from `bin/Release` into the BepInEx plugin folder.

Run `dotnet run --project tests/SmokeTests.csproj -c Release` for Lua, snapshot, argument validation, event queue, coroutine completion, example and installed-game contract tests. `./Build-Package.ps1` runs these checks, regenerates coverage and builds `release/HowToLua-0.3.0.zip` containing both DLLs, licenses, README, changelog, coverage, local wiki pages and optional examples.

See [Architecture](wiki/Architecture.md) for where to extend hooks and APIs. Build paths currently reference this machine's Steam install; adjust the project references and test game path for a different installation.

`MoonSharp.Reference.props` selects `lib/net40-client/MoonSharp.Interpreter.dll` for both the plugin and tests. Keep this explicit reference: NuGet's automatically selected `netstandard1.6` build requires facade assemblies absent from the game. Packaging also runs a fresh Windows PowerShell check against the staged DLL to verify its references and execute Lua table/callback code under .NET Framework.
