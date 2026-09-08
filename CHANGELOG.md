# Changelog

Notable changes to How to Lua, newest first.

## 0.3.0

### Added

- 59 Lua functions, increasing the public API from 20 to 79, in separate item, catalog, inventory, combat, server, boat, progression, NPC and boss modules.
- Item definition catalogs, paginated/nearby instance queries, bounded native spawning, loose-item removal, cooking, skins, interaction state, damage and score bonuses.
- Inventory snapshots, slot selection, bait selection/grants, pocket costs/unlocks, held-item queries, weapon/melee stats and upgrades.
- Player damage, poison and fire actions; authoritative poison/fire snapshot fields; host-local player lookup.
- Boat status, unlocks, radar, motors, skins, driver ejection and return-to-spawn.
- Server difficulty, friendly fire, one-shot settings, save requests, shared-money affordability/give/spend.
- Island lists, travel/unlocks, water queries, game-rule snapshots, NPC locations/quest progress, grill unlocks and boss scaling/immortality controls.
- 23 events, increasing the catalog from 27 to 50: item spawn/despawn, fish release, five inventory changes, four boat changes, five equipment changes, difficulty, boss max health/immortality, NPC talk/quest progress and grill unlock.
- Optional World Tools example with `/luacatalog`, `/luaspawn definition_id`, boat status/save buttons and boat/quest logging.
- Full Gameplay API wiki reference, updated installation/examples/troubleshooting and compiled API/private-field contract tests.

### Safety and Compatibility

- Spawning is shared-budgeted to 8 attempts/second and 128 live tracked items; Lua reload does not reset the live-item limit. Bosses, player bodies and quest-marked prefabs are excluded.
- Loose-item cleanup refuses protected items and anything held, stored or attached to a rod/bird. Upgrade and skin indices are checked before native calls.
- Save requests have a shared 5-second cooldown; travel requests have a shared 2-second cooldown and require a loaded world.
- Steam IDs and instance network IDs use canonical decimal strings. Definition and inventory indices remain bounded integer numbers.
- Detailed creature health reads the authoritative synchronized value rather than the game's client-cached property.
- Original APIs/event arguments, native pause-menu styling and classic MoonSharp interpreter compatibility are preserved.
- Gameplay bridge classes declare a marker attribute so new domains participate in the measured coverage report. Native-menu wiring is still excluded.

### Coverage and Verification

- Direct game-method integration increased from 41/4,737 (0.87%) to 229/4,737 (4.83%) against the installed game build. This measures direct calls/reads and hook targets, not feature completeness or runtime test coverage.
- 405 automated checks pass, including all 39 Harmony targets, parameter injections, four private-field dependencies, 79 compiled callback registrations, all 59 new API documentation entries and all 50 documented events.
- Packaging regenerates coverage and includes changelog/local wiki pages alongside optional examples. The staged interpreter is checked under Windows .NET Framework.
- In-game multiplayer behavior, remote replication and persistence still require live testing. A successful native request does not promise disk completion or remote receipt. Test gameplay-changing scripts on a spare save.

### Upgrade

Replace both DLLs and fully restart How to Fish. Preserve existing Lua scripts and configuration. Examples are optional; install World Tools separately to enable its commands/buttons. Bait, pocket, boat and weapon upgrade APIs are grants, not shop purchases: scripts must charge money separately when desired.

## 0.2.0

### Added

- 21 additional game events, bringing the total to 27:
  - Player health, death, revival, fullness, poison and fire changes.
  - Item pickup, drop, sale, cooking and skin changes, plus fish sales.
  - Creature health changes and boss spawn/despawn notifications.
  - Shared-money changes.
  - Server start/stop and save-request notifications.
  - Island selection changes and completed island loading.
- Player APIs: `htf.players.list()`, `get()`, `heal()`, `feed()` and `teleport()`.
- World APIs: `htf.world.info()`, `spawn_position()` and `boss()`.
- Shared-balance query: `htf.economy.balance()`.
- Plain player, item and world snapshots, with independent nested tables for each Lua listener.
- Optional Crew Tools example with sale logging, persistent per-player death counts, heal/feed buttons, `/luacrew` and `/luareturn steam_id`.
- Automated tests for snapshots, argument validation, event delivery, coroutine completion, examples and installed-game Harmony patch contracts.
- Wiki documentation for the new APIs, event payloads and framework architecture.

### Changed

- Split the plugin into separate `Runtime`, `Api`, `Game` and `UI` modules. The bootstrap now handles startup, updates and teardown.
- Game events are queued instead of invoking Lua inside native game callbacks. Callback-generated events wait for a later Update.
- Event delivery is limited to 128 events per Update, with a queue capacity of 1024. Overflow drops new events and logs a warning.
- Existing events retain their original leading arguments; selected events now include an additional snapshot argument.
- Reloading clears pending events. Ending a host session discards pending gameplay events before delivering `server_stopped`.
- Release packaging runs the expanded tests and derives the ZIP version from the built assembly.

### Correctness and Compatibility

- Sale notifications use the actual sale method, not vanilla's misleading `OnItemSold` event, which also fires on unrelated balance changes.
- Host balance queries and money-grant limits read the authoritative synchronized balance.
- Steam IDs remain strings to preserve precision. Player actions validate input bounds and require host authority and a living, available player.
- Island-load completion is observed after the native coroutine finishes, not when its loading flag first clears.
- `server_save_requested` reports an attempted save, not guaranteed disk persistence.
- Existing native pause-menu styling and the MoonSharp compatibility fix are preserved.

### Verification

- Release build completed without warnings or errors.
- 152 automated checks passed, including validation of 17 Harmony patch targets against the installed game assembly.
- Packaged MoonSharp passed its Windows .NET Framework execution check.
- New hooks and player actions still require in-game multiplayer testing.

### Upgrade Notes

Replace both framework DLLs and fully restart the game. Keep existing Lua scripts and configuration files. The original event arguments remain compatible, but callbacks are now asynchronous notifications and cannot cancel the underlying game action.

## 0.1.1

### Fixed

- Fixed Lua mods failing to load with `System.Collections, Version=4.0.10.0` or `MoonSharp.Interpreter.Table:m_Values` errors.
- Selected MoonSharp's classic `net40-client` binary instead of its incompatible automatically selected `netstandard1.6` asset.
- Prevented release-staging copies from overriding the intended interpreter reference during builds.

### Added

- Shared MoonSharp reference configuration for the plugin and smoke tests.
- A packaged-interpreter check that creates tables and runs Lua callbacks under Windows .NET Framework before ZIP creation.
- Installation and troubleshooting guidance for updating both DLLs without deleting scripts or saved data.

## 0.1.0

### Initial Beta

- Manifest-based Lua mod loading and reloading through BepInEx, using MoonSharp.
- Separate restricted Lua states, per-mod error handling and a 50,000-instruction budget per entry/callback.
- Native Pause > Lua Mods panel with mod status and registered action buttons.
- Host chat commands, delayed/repeating callbacks, chat broadcasts and shared-money rewards.
- Persistent per-mod string data and dependencies declared by exact mod ID.
- Fish-hook, creature-death, boss-death, command and player join/leave events.
- Optional Welcome Example and initial README/wiki documentation.
