# Game Method Coverage

**0.87% direct method integration: 41 / 4,737 methods.**

This measures how much of the installed game's managed method surface the Lua gameplay bridges directly touch. It is not the percentage of features supported, executable lines covered, or methods callable freely from Lua. Some integrations only observe a method or read a property.

## Counting Rules

- Denominator: every non-constructor method declared in `Assembly-CSharp.dll`, including getters/setters, generated networking methods and any embedded libraries. Unity, FishNet and other DLLs are not included.
- Numerator: unique game methods referenced by gameplay APIs/snapshots/hooks, plus the methods patched by those hooks. Internal UI wiring and arbitrary framework infrastructure do not count.
- Methods called internally by an integrated game method are not counted automatically. Reading fields does not add methods. A method appearing in several bridges is counted once.
- This is a static analysis of the built DLL, not a multiplayer test result or a promise that every argument/behavior of an integrated method is exposed.

## Recalculate

Run `dotnet run --project tests/SmokeTests.csproj -c Release -- --coverage` after changing the bridges or updating the game. Commit the generated `coverage` files to refresh the README badge. The generator does not publish to GitHub.

Framework version: `0.2.0`.

Game DLL SHA-256: `9BFC0A0F95085D64CA13A6A1943A31B0241472DF6361BC780AEFA8AFB79A59F2`.

Framework DLL SHA-256: `0359FD119BBBB2C3D9D3F1B66641D73F53094E0FE33BBF848DB7774297A5B7FD`.

## Integrated Methods

| Game method | Integration |
| --- | --- |
| `BossType Creature::get_BossType()` | Direct gameplay bridge call/read |
| `Creature BossManager::get_Boss()` | Direct gameplay bridge call/read |
| `Player Item::get_LastHolder()` | Direct gameplay bridge call/read |
| `PlayerVitals Player::get_Vitals()` | Direct gameplay bridge call/read |
| `SaveManager/ServerSaveObject SaveManager::get_CurServerSave()` | Direct gameplay bridge call/read |
| `Server Server::get_Instance()` | Direct gameplay bridge call/read |
| `System.Boolean IslandManager::get_IsInitialized()` | Direct gameplay bridge call/read |
| `System.Boolean IslandManager::get_IsLoading()` | Direct gameplay bridge call/read |
| `System.Boolean Player::get_IsAfk()` | Direct gameplay bridge call/read |
| `System.Byte Item::get_ID()` | Direct gameplay bridge call/read |
| `System.Byte OnlineIslandManager::get_CurIsland()` | Direct gameplay bridge call/read |
| `System.Collections.IEnumerator IslandManager::ProcessRequest(System.Byte)` | Event/command hook |
| `System.Int32 Item::get_TotalWorth()` | Direct gameplay bridge call/read |
| `System.Int32 MoneyManager::get_Money()` | Direct gameplay bridge call/read |
| `System.Int32 PlayerVitals::get_Fullness()` | Direct gameplay bridge call/read |
| `System.Int32 PlayerVitals::get_Health()` | Direct gameplay bridge call/read |
| `System.String Item::GetName()` | Direct gameplay bridge call/read |
| `System.String Player::get_SteamName()` | Direct gameplay bridge call/read |
| `System.UInt64 Player::get_SteamID()` | Direct gameplay bridge call/read |
| `System.Void BossManager::InitializeBossFight(Creature)` | Event/command hook |
| `System.Void BossManager::OnBossRemoved()` | Event/command hook |
| `System.Void ChatManager::SendTypedMessage()` | Event/command hook |
| `System.Void Creature::OnHealthChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void Item::OnAttachedRodChange(FishingRod,FishingRod,System.Boolean)` | Event/command hook |
| `System.Void Item::OnCooknessChange(System.Single,System.Single,System.Boolean)` | Event/command hook |
| `System.Void Item::OnCurSkinChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void Item::OnSyncedHolderChange(Player,Player,System.Boolean)` | Event/command hook |
| `System.Void MoneyManager::AddMoney(System.Int32,Player)` | Direct gameplay bridge call/read |
| `System.Void MoneyManager::OnChangeMoney(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void MoneyManager::SellItem(Item)` | Event/command hook |
| `System.Void OnlineIslandManager::OnIslandChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void Player::RPCTeleport(FishNet.Connection.NetworkConnection,UnityEngine.Vector3,System.Single)` | Direct gameplay bridge call/read |
| `System.Void PlayerVitals::Heal(System.Int32)` | Direct gameplay bridge call/read |
| `System.Void PlayerVitals::OnFireChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerVitals::OnFullnessChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerVitals::OnHealthChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerVitals::OnPoisonChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerVitals::RestoreFullness(System.Int32)` | Direct gameplay bridge call/read |
| `System.Void SaveManager::SaveServer(System.Boolean)` | Event/command hook |
| `System.Void Server::SendChatMessage(System.String,FishNet.Connection.NetworkConnection)` | Direct gameplay bridge call/read |
| `UnityEngine.Transform Player::get_Transform()` | Direct gameplay bridge call/read |
