# Game Method Coverage

**4.83% direct method integration: 229 / 4,737 methods.**

This measures how much of the installed game's managed method surface the Lua gameplay bridges directly touch. It is not the percentage of features supported, executable lines covered, or methods callable freely from Lua. Some integrations only observe a method or read a property.

## Counting Rules

- Denominator: every non-constructor method declared in `Assembly-CSharp.dll`, including getters/setters, generated networking methods and any embedded libraries. Unity, FishNet and other DLLs are not included.
- Numerator: unique game methods referenced by gameplay APIs/snapshots/hooks, plus the methods patched by those hooks. Internal UI wiring and arbitrary framework infrastructure do not count.
- Methods called internally by an integrated game method are not counted automatically. Reading fields does not add methods. A method appearing in several bridges is counted once.
- This is a static analysis of the built DLL, not a multiplayer test result or a promise that every argument/behavior of an integrated method is exposed.

## Recalculate

Run `dotnet run --project tests/SmokeTests.csproj -c Release -- --coverage` after changing the bridges or updating the game. Commit the generated `coverage` files to refresh the README badge. The generator does not publish to GitHub.

Framework version: `0.3.0`.

Game DLL SHA-256: `9BFC0A0F95085D64CA13A6A1943A31B0241472DF6361BC780AEFA8AFB79A59F2`.

Framework DLL SHA-256: `342E6F9A5C0F0397388F10638B44C2F89DFD967A5C0D7E21F7863AFC17BAA648`.

## Integrated Methods

| Game method | Integration |
| --- | --- |
| `Attachments Weapon::get_Attachments()` | Direct gameplay bridge call/read |
| `BaitInfo NPCQuest::get_BaitToReceive()` | Direct gameplay bridge call/read |
| `Bird Item::get_BirdHolder()` | Direct gameplay bridge call/read |
| `Boat BoatManager::get_Boat()` | Direct gameplay bridge call/read |
| `BossType Creature::get_BossType()` | Direct gameplay bridge call/read |
| `BulletUpgrade Attachments::GetCurBulletUpgrade()` | Direct gameplay bridge call/read |
| `BulletUpgrade Attachments::GetNextBulletUpgrade(System.Byte)` | Direct gameplay bridge call/read |
| `Creature BossManager::get_Boss()` | Direct gameplay bridge call/read |
| `DeadPlayer Item::get_DeadPlayer()` | Direct gameplay bridge call/read |
| `Difficulty ServerSettings::get_Difficulty()` | Direct gameplay bridge call/read |
| `FishNet.Connection.NetworkConnection RigidbodySync::get_SyncedSimulator()` | Direct gameplay bridge call/read |
| `Fishable ItemInfoWeight::get_Fishable()` | Direct gameplay bridge call/read |
| `FishingRod Item::get_AttachedRod()` | Direct gameplay bridge call/read |
| `Item GameInfo::GetSpawnable(System.Byte)` | Direct gameplay bridge call/read |
| `Item ItemManager::SpawnNewItem(Item,UnityEngine.Vector3,UnityEngine.Quaternion)` | Direct gameplay bridge call/read |
| `Item PlayerHolding::get_HeldItem()` | Direct gameplay bridge call/read |
| `ItemType Item::get_Type()` | Direct gameplay bridge call/read |
| `NPC NPCManager::IDToNpc(System.Byte)` | Direct gameplay bridge call/read |
| `Player Boat::get_Driver()` | Direct gameplay bridge call/read |
| `Player Item::get_LastHolder()` | Direct gameplay bridge call/read |
| `Player Item::get_SyncedHolder()` | Direct gameplay bridge call/read |
| `PlayerHolding Player::get_Holding()` | Direct gameplay bridge call/read |
| `PlayerInventory Player::get_Inventory()` | Direct gameplay bridge call/read |
| `PlayerVitals Player::get_Vitals()` | Direct gameplay bridge call/read |
| `QuestType NPCQuest::get_Type()` | Direct gameplay bridge call/read |
| `RigidbodySync Item::get_RigidbodySync()` | Direct gameplay bridge call/read |
| `SaveManager/ServerSaveObject SaveManager::get_CurServerSave()` | Direct gameplay bridge call/read |
| `Server Server::get_Instance()` | Direct gameplay bridge call/read |
| `SharpnessUpgrade Melee::GetCurSharpness()` | Direct gameplay bridge call/read |
| `SharpnessUpgrade Melee::GetNextSharpnessUpgrade(System.Byte)` | Direct gameplay bridge call/read |
| `SkinPreset Boat::get_SkinPreset()` | Direct gameplay bridge call/read |
| `SkinPreset Item::get_SkinPreset()` | Direct gameplay bridge call/read |
| `System.Boolean Attachments::get_ExtendedMag()` | Direct gameplay bridge call/read |
| `System.Boolean Attachments::get_LaserSight()` | Direct gameplay bridge call/read |
| `System.Boolean BaitInfo::get_RequireReelingToCatch()` | Direct gameplay bridge call/read |
| `System.Boolean Boat::get_BoatRadarUnlocked()` | Direct gameplay bridge call/read |
| `System.Boolean Boat::get_BoatUnlocked()` | Direct gameplay bridge call/read |
| `System.Boolean BossManager::get_IsImmortal()` | Direct gameplay bridge call/read |
| `System.Boolean Creature::get_ExcludeFromJournal()` | Direct gameplay bridge call/read |
| `System.Boolean Creature::get_IgnoreDeathByWater()` | Direct gameplay bridge call/read |
| `System.Boolean Creature::get_IsDead()` | Direct gameplay bridge call/read |
| `System.Boolean Creature::get_IsDrip()` | Direct gameplay bridge call/read |
| `System.Boolean Creature::get_IsEndangered()` | Direct gameplay bridge call/read |
| `System.Boolean IslandManager::get_IsInitialized()` | Direct gameplay bridge call/read |
| `System.Boolean IslandManager::get_IsLoading()` | Direct gameplay bridge call/read |
| `System.Boolean Item::get_CanPickUp()` | Direct gameplay bridge call/read |
| `System.Boolean Item::get_HasBeenHeld()` | Direct gameplay bridge call/read |
| `System.Boolean Item::get_HasPlayerHolder()` | Direct gameplay bridge call/read |
| `System.Boolean Item::get_IgnoredByMoneyNpc()` | Direct gameplay bridge call/read |
| `System.Boolean Item::get_IgnoredBySeagulls()` | Direct gameplay bridge call/read |
| `System.Boolean Item::get_IsDestroying()` | Direct gameplay bridge call/read |
| `System.Boolean Item::get_IsInInventory()` | Direct gameplay bridge call/read |
| `System.Boolean Item::get_IsInteractable()` | Direct gameplay bridge call/read |
| `System.Boolean Item::get_IsQuestItem()` | Direct gameplay bridge call/read |
| `System.Boolean NPCManager::NpcIsHoldingItem(System.Byte)` | Direct gameplay bridge call/read |
| `System.Boolean NPCManager::get_FinalBossKilled()` | Direct gameplay bridge call/read |
| `System.Boolean NPCManager::get_GrillUnlocked()` | Direct gameplay bridge call/read |
| `System.Boolean NPCQuest::get_OnlyCreatures()` | Direct gameplay bridge call/read |
| `System.Boolean Player::get_IsAfk()` | Direct gameplay bridge call/read |
| `System.Boolean PlayerInventory::HasItemInInventory(Item)` | Direct gameplay bridge call/read |
| `System.Boolean PlayerInventory::HasPocket(System.Int32)` | Direct gameplay bridge call/read |
| `System.Boolean RigidbodySync::get_IsFloating()` | Direct gameplay bridge call/read |
| `System.Boolean RigidbodySync::get_IsStationary()` | Direct gameplay bridge call/read |
| `System.Boolean RigidbodySync::get_OnBoat()` | Direct gameplay bridge call/read |
| `System.Boolean ServerSettings::get_OneShotEnabled()` | Direct gameplay bridge call/read |
| `System.Boolean ServerSettings::get_UseFriendlyFire()` | Direct gameplay bridge call/read |
| `System.Byte Attachments::get_AmmoType()` | Direct gameplay bridge call/read |
| `System.Byte Attachments::get_BarrelAttachment()` | Direct gameplay bridge call/read |
| `System.Byte Attachments::get_Sight()` | Direct gameplay bridge call/read |
| `System.Byte Boat::get_CurSkin()` | Direct gameplay bridge call/read |
| `System.Byte Boat::get_MotorIndex()` | Direct gameplay bridge call/read |
| `System.Byte GameInfo::GetIndexOfBait(BaitInfo)` | Direct gameplay bridge call/read |
| `System.Byte Item::get_CurSkin()` | Direct gameplay bridge call/read |
| `System.Byte Item::get_ID()` | Direct gameplay bridge call/read |
| `System.Byte Melee::get_SharpnessIndex()` | Direct gameplay bridge call/read |
| `System.Byte NPC::GetServerProgression(System.Byte)` | Direct gameplay bridge call/read |
| `System.Byte NPC::get_ID()` | Direct gameplay bridge call/read |
| `System.Byte NPCQuest::get_IslandToUnlock()` | Direct gameplay bridge call/read |
| `System.Byte NPCQuest::get_TotalItems()` | Direct gameplay bridge call/read |
| `System.Byte OnlineIslandManager::get_CurIsland()` | Direct gameplay bridge call/read |
| `System.Byte OnlineIslandManager::get_MaxIslandUnlocked()` | Direct gameplay bridge call/read |
| `System.Byte PlayerInventory::get_CurBait()` | Direct gameplay bridge call/read |
| `System.Byte PlayerInventory::get_ExtraSlots()` | Direct gameplay bridge call/read |
| `System.Collections.Generic.Dictionary`2<UnityEngine.Transform,Item> ItemManager::get_Items()` | Direct gameplay bridge call/read |
| `System.Collections.Generic.IReadOnlyList`1<AttachmentInfo> GameInfo::get_AllAttachments()` | Direct gameplay bridge call/read |
| `System.Collections.Generic.IReadOnlyList`1<BaitInfo> GameInfo::get_AllBaits()` | Direct gameplay bridge call/read |
| `System.Collections.Generic.IReadOnlyList`1<ItemSkin> SkinPreset::get_Skins()` | Direct gameplay bridge call/read |
| `System.Collections.Generic.IReadOnlyList`1<NPCQuest> NPC::get_Quests()` | Direct gameplay bridge call/read |
| `System.Collections.Generic.KeyValuePair`2<System.Boolean,System.Single> WaterManager::GetWaterInfo(UnityEngine.Vector3)` | Direct gameplay bridge call/read |
| `System.Collections.Generic.List`1<Item> NPCQuest::get_QuestItems()` | Direct gameplay bridge call/read |
| `System.Collections.Generic.List`1<ItemInfoWeight> BaitInfo::get_ItemWeights()` | Direct gameplay bridge call/read |
| `System.Collections.IEnumerator IslandManager::ProcessRequest(System.Byte)` | Event/command hook |
| `System.Int32 BaitInfo::get_Cost()` | Direct gameplay bridge call/read |
| `System.Int32 BossManager::GetBossDamage(System.Int32,System.Single)` | Direct gameplay bridge call/read |
| `System.Int32 BossManager::GetBossMaxHp(System.Int32,System.Single)` | Direct gameplay bridge call/read |
| `System.Int32 BossManager::get_BossMaxHp()` | Direct gameplay bridge call/read |
| `System.Int32 BulletUpgrade::get_Cost()` | Direct gameplay bridge call/read |
| `System.Int32 BulletUpgrade::get_Damage()` | Direct gameplay bridge call/read |
| `System.Int32 Creature::get_FullnessToRestore()` | Direct gameplay bridge call/read |
| `System.Int32 Creature::get_HpToRestore()` | Direct gameplay bridge call/read |
| `System.Int32 Creature::get_MaxHp()` | Direct gameplay bridge call/read |
| `System.Int32 GameInfo::get_Seed()` | Direct gameplay bridge call/read |
| `System.Int32 Item::get_Cost()` | Direct gameplay bridge call/read |
| `System.Int32 Item::get_DefaultWorth()` | Direct gameplay bridge call/read |
| `System.Int32 Item::get_TotalWorth()` | Direct gameplay bridge call/read |
| `System.Int32 MoneyManager::get_Money()` | Direct gameplay bridge call/read |
| `System.Int32 PlayerInventory::GetExtraSlotCost(System.Byte)` | Direct gameplay bridge call/read |
| `System.Int32 PlayerVitals::get_Fullness()` | Direct gameplay bridge call/read |
| `System.Int32 PlayerVitals::get_Health()` | Direct gameplay bridge call/read |
| `System.Int32 SharpnessUpgrade::get_Cost()` | Direct gameplay bridge call/read |
| `System.Int32 SharpnessUpgrade::get_Damage()` | Direct gameplay bridge call/read |
| `System.Int32 Weapon::get_Damage()` | Direct gameplay bridge call/read |
| `System.Single BaitInfo::get_LostOnBaitChance()` | Direct gameplay bridge call/read |
| `System.Single Boat::get_VelocityMag()` | Direct gameplay bridge call/read |
| `System.Single Creature::get_BossHpMultiplier()` | Direct gameplay bridge call/read |
| `System.Single Creature::get_BossTimeInSeconds()` | Direct gameplay bridge call/read |
| `System.Single GameInfo::get_BoatProjectileForce()` | Direct gameplay bridge call/read |
| `System.Single GameInfo::get_DefaultVelDamageMulti()` | Direct gameplay bridge call/read |
| `System.Single GameInfo::get_HeadShotDamageMulti()` | Direct gameplay bridge call/read |
| `System.Single GameInfo::get_MaxAliveCreatureVel()` | Direct gameplay bridge call/read |
| `System.Single GameInfo::get_MaxItemVel()` | Direct gameplay bridge call/read |
| `System.Single GameInfo::get_PlayerDeathForceMultiFromCreature()` | Direct gameplay bridge call/read |
| `System.Single GameInfo::get_PlayerKillForce()` | Direct gameplay bridge call/read |
| `System.Single Item::get_BettingMultiplier()` | Direct gameplay bridge call/read |
| `System.Single Item::get_Buoyancy()` | Direct gameplay bridge call/read |
| `System.Single Item::get_Cookness()` | Direct gameplay bridge call/read |
| `System.Single Item::get_KillScoreMultiplier()` | Direct gameplay bridge call/read |
| `System.Single Item::get_ModelHeight()` | Direct gameplay bridge call/read |
| `System.Single Item::get_RandomizedWeight()` | Direct gameplay bridge call/read |
| `System.Single ItemInfoWeight::get_Weight()` | Direct gameplay bridge call/read |
| `System.Single ServerSettings::get_DamageMultiplier()` | Direct gameplay bridge call/read |
| `System.Single ServerSettings::get_HealthMultiplier()` | Direct gameplay bridge call/read |
| `System.Single Weapon::get_AdsFov()` | Direct gameplay bridge call/read |
| `System.Single Weapon::get_AdsSpeedDamping()` | Direct gameplay bridge call/read |
| `System.String AttachmentInfo::get_DescriptionLocalized()` | Direct gameplay bridge call/read |
| `System.String AttachmentInfo::get_NameLocalized()` | Direct gameplay bridge call/read |
| `System.String BaitInfo::get_Description()` | Direct gameplay bridge call/read |
| `System.String BaitInfo::get_NameLocalized()` | Direct gameplay bridge call/read |
| `System.String Item::GetName()` | Direct gameplay bridge call/read |
| `System.String Player::get_SteamName()` | Direct gameplay bridge call/read |
| `System.UInt32 BossManager::get_BossLeavesTick()` | Direct gameplay bridge call/read |
| `System.UInt32 BossManager::get_BossSpawnTick()` | Direct gameplay bridge call/read |
| `System.UInt32 BossManager::get_BossTotalTimeInTicks()` | Direct gameplay bridge call/read |
| `System.UInt64 Player::get_SteamID()` | Direct gameplay bridge call/read |
| `System.Void Attachments::OnAmmoChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void Attachments::OnBarrelChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void Attachments::OnLaserSightChange(System.Boolean,System.Boolean,System.Boolean)` | Event/command hook |
| `System.Void Attachments::OnMagChange(System.Boolean,System.Boolean,System.Boolean)` | Event/command hook |
| `System.Void Attachments::OnSightChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void Attachments::UpgradeBullets()` | Direct gameplay bridge call/read |
| `System.Void Boat::OnBoatRadarChange(System.Boolean,System.Boolean,System.Boolean)` | Event/command hook |
| `System.Void Boat::OnDriverChange(Player,Player,System.Boolean)` | Event/command hook |
| `System.Void Boat::OnMotorChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void Boat::OnSkinChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void Boat::ServerSetSkin(System.Byte)` | Direct gameplay bridge call/read |
| `System.Void Boat::SetMotor(System.Byte)` | Direct gameplay bridge call/read |
| `System.Void Boat::TrySetDriver(Player)` | Direct gameplay bridge call/read |
| `System.Void Boat::UnlockBoat()` | Direct gameplay bridge call/read |
| `System.Void Boat::UnlockBoatRadar()` | Direct gameplay bridge call/read |
| `System.Void BoatManager::TryMoveBoat(UnityEngine.Vector3,UnityEngine.Quaternion)` | Direct gameplay bridge call/read |
| `System.Void BossManager::InitializeBossFight(Creature)` | Event/command hook |
| `System.Void BossManager::OnBossMaxHpChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void BossManager::OnBossRemoved()` | Event/command hook |
| `System.Void BossManager::OnIsImmportalChange(System.Boolean,System.Boolean,System.Boolean)` | Event/command hook |
| `System.Void BossManager::ToggleImmortal(System.Boolean)` | Direct gameplay bridge call/read |
| `System.Void ChatManager::SendTypedMessage()` | Event/command hook |
| `System.Void Creature::OnHealthChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void Creature::ServerChangeHp(System.Int32)` | Direct gameplay bridge call/read |
| `System.Void Item::CookItem(System.Single)` | Direct gameplay bridge call/read |
| `System.Void Item::DestroyItem(System.Byte,System.Byte)` | Direct gameplay bridge call/read |
| `System.Void Item::OnAttachedRodChange(FishingRod,FishingRod,System.Boolean)` | Event/command hook |
| `System.Void Item::OnCooknessChange(System.Single,System.Single,System.Boolean)` | Event/command hook |
| `System.Void Item::OnCurSkinChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void Item::OnStartServer()` | Event/command hook |
| `System.Void Item::OnStopServer()` | Event/command hook |
| `System.Void Item::OnSyncedHolderChange(Player,Player,System.Boolean)` | Event/command hook |
| `System.Void Item::ServerSetSkin(System.Byte)` | Direct gameplay bridge call/read |
| `System.Void Item::SetKillscoreMultiplier(System.Single)` | Direct gameplay bridge call/read |
| `System.Void Item::ToggleInteractable(System.Boolean)` | Direct gameplay bridge call/read |
| `System.Void Melee::UpgradeSharpness()` | Direct gameplay bridge call/read |
| `System.Void MoneyManager::AddMoney(System.Int32,Player)` | Direct gameplay bridge call/read |
| `System.Void MoneyManager::OnChangeMoney(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void MoneyManager::RemoveMoney(System.Int32,Player)` | Direct gameplay bridge call/read |
| `System.Void MoneyManager::SellItem(Item)` | Event/command hook |
| `System.Void NPCManager::OnGrillUnlockedChange(System.Boolean,System.Boolean,System.Boolean)` | Event/command hook |
| `System.Void NPCManager::ServerOnClientSpokeToNpc(System.Byte)` | Event/command hook |
| `System.Void NPCManager::ServerSpeak(System.Byte,System.Byte,System.Byte,QuestLineType)` | Event/command hook |
| `System.Void NPCManager::UnlockGrill()` | Direct gameplay bridge call/read |
| `System.Void OnlineIslandManager::OnIslandChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void OnlineIslandManager::TpToSpecificIsland(System.Byte)` | Direct gameplay bridge call/read |
| `System.Void OnlineIslandManager::UnlockIsland(System.Byte)` | Direct gameplay bridge call/read |
| `System.Void Player::RPCTeleport(FishNet.Connection.NetworkConnection,UnityEngine.Vector3,System.Single)` | Direct gameplay bridge call/read |
| `System.Void PlayerInventory::OnCurBaitChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void PlayerInventory::OnCurSlotChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerInventory::OnInventoryAmountChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void PlayerInventory::OnItemsChange(FishNet.Object.Synchronizing.SyncDictionaryOperation,System.Byte,Item,System.Boolean)` | Event/command hook |
| `System.Void PlayerInventory::OnOwnedBaitChange(FishNet.Object.Synchronizing.SyncListOperation,System.Int32,System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerInventory::ServerBoughtBait(System.Byte)` | Direct gameplay bridge call/read |
| `System.Void PlayerInventory::ServerSetCurBait(System.Byte)` | Direct gameplay bridge call/read |
| `System.Void PlayerInventory::ServerSetSyncedCurSlot(System.Int32)` | Direct gameplay bridge call/read |
| `System.Void PlayerInventory::UnlockExtraPocket(System.Byte)` | Direct gameplay bridge call/read |
| `System.Void PlayerVitals::ApplyNewFire()` | Direct gameplay bridge call/read |
| `System.Void PlayerVitals::ApplyNewPoison()` | Direct gameplay bridge call/read |
| `System.Void PlayerVitals::Heal(System.Int32)` | Direct gameplay bridge call/read |
| `System.Void PlayerVitals::OnFireChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerVitals::OnFullnessChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerVitals::OnHealthChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerVitals::OnPoisonChange(System.Int32,System.Int32,System.Boolean)` | Event/command hook |
| `System.Void PlayerVitals::RestoreFullness(System.Int32)` | Direct gameplay bridge call/read |
| `System.Void PlayerVitals::TakeDamage(System.Int32,UnityEngine.Vector3,UnityEngine.Vector3,System.Boolean)` | Direct gameplay bridge call/read |
| `System.Void SaveManager::SaveServer(System.Boolean)` | Direct gameplay bridge call/read, Event/command hook |
| `System.Void Server::SendChatMessage(System.String,FishNet.Connection.NetworkConnection)` | Direct gameplay bridge call/read |
| `System.Void ServerSettings::OnDifficultyChange(System.Byte,System.Byte,System.Boolean)` | Event/command hook |
| `System.Void ServerSettings::SetDifficulty(Difficulty)` | Direct gameplay bridge call/read |
| `System.Void ServerSettings::ToggleFriendlyFire(System.Boolean)` | Direct gameplay bridge call/read |
| `System.Void ServerSettings::ToggleOneShot()` | Direct gameplay bridge call/read |
| `UnityEngine.Rigidbody Boat::get_HiddenPhysicsRig()` | Direct gameplay bridge call/read |
| `UnityEngine.Transform Boat::get_DriverPos()` | Direct gameplay bridge call/read |
| `UnityEngine.Transform Boat::get_VisualBoat()` | Direct gameplay bridge call/read |
| `UnityEngine.Transform NPC::get_MouthPosForItems()` | Direct gameplay bridge call/read |
| `UnityEngine.Transform Player::get_Transform()` | Direct gameplay bridge call/read |
| `UnityEngine.Transform Server::get_DynamicObjectsHolder()` | Direct gameplay bridge call/read |
| `UnityEngine.Vector2 BaitInfo::get_CatchTimeMinMax()` | Direct gameplay bridge call/read |
| `UnityEngine.Vector2 GameInfo::get_DefaultMinMaxFishVelForDamage()` | Direct gameplay bridge call/read |
| `UnityEngine.Vector3 Boat::get_AngularVelocity()` | Direct gameplay bridge call/read |
| `UnityEngine.Vector3 Boat::get_Velocity()` | Direct gameplay bridge call/read |
| `UnityEngine.Vector3 RigidbodySync::get_FakeAngularVelocity()` | Direct gameplay bridge call/read |
| `UnityEngine.Vector3 RigidbodySync::get_FakeVelocity()` | Direct gameplay bridge call/read |
| `Weapon Attachments::get_Weapon()` | Direct gameplay bridge call/read |
