using System.Collections.Generic;

namespace HowToLua;

[LuaBridge]
internal static class ItemSnapshots
{
    internal static Dictionary<string, object> Definition(Item item)
    {
        if (!item) return null;
        return new()
        {
            ["id"] = (int)item.ID, ["name"] = GameSnapshots.Name(item), ["prefab_name"] = item.name,
            ["type"] = item.Type.ToString(), ["default_worth"] = item.DefaultWorth, ["cost"] = item.Cost,
            ["is_fish"] = item is Fish, ["is_weapon"] = item is Weapon,
            ["buoyancy"] = item.Buoyancy, ["model_height"] = item.ModelHeight,
            ["ignored_by_birds"] = item.IgnoredBySeagulls, ["ignored_by_shop"] = item.IgnoredByMoneyNpc,
            ["spawn_allowed"] = !GameAccess.Protected(item), ["skin_count"] = item.SkinPreset ? item.SkinPreset.Skins.Count : 0,
            ["creature"] = CreatureDefinition(item as Creature)
        };
    }
    internal static Dictionary<string, object> CreatureDefinition(Creature creature)
    {
        if (!creature) return null;
        return new()
        {
            ["max_health"] = creature.MaxHp, ["endangered"] = creature.IsEndangered,
            ["boss_type"] = creature.BossType.ToString(), ["boss_health_multiplier"] = creature.BossHpMultiplier,
            ["boss_duration"] = creature.BossTimeInSeconds, ["fullness_restored"] = creature.FullnessToRestore,
            ["health_restored"] = creature.HpToRestore, ["ignores_water_death"] = creature.IgnoreDeathByWater,
            ["excluded_from_journal"] = creature.ExcludeFromJournal
        };
    }
    internal static Dictionary<string, object> Detail(Item item)
    {
        if (!item) return null;
        var value = GameSnapshots.ItemInfo(item);
        value["definition"] = Definition(item);
        value["holder_id"] = item.SyncedHolder ? item.SyncedHolder.SteamID.ToString() : null;
        value["last_holder_id"] = item.LastHolder ? item.LastHolder.SteamID.ToString() : null;
        value["in_inventory"] = item.IsInInventory;
        value["can_pick_up"] = item.CanPickUp;
        value["interactable"] = item.IsInteractable;
        value["skin"] = (int)item.CurSkin;
        value["cookness"] = item.Cookness;
        value["weight"] = item.RandomizedWeight;
        value["killscore_multiplier"] = item.KillScoreMultiplier;
        value["betting_multiplier"] = item.BettingMultiplier;
        value["has_been_held"] = item.HasBeenHeld;
        value["attached_rod_id"] = item.AttachedRod ? item.AttachedRod.NetworkObject.ObjectId.ToString() : null;
        value["rotation"] = GameSnapshots.Position(item.transform.eulerAngles);
        if (item.RigidbodySync)
        {
            RigidbodySync sync = item.RigidbodySync;
            value["physics"] = new Dictionary<string, object>
            {
                ["velocity"] = GameSnapshots.Position(sync.FakeVelocity), ["angular_velocity"] = GameSnapshots.Position(sync.FakeAngularVelocity),
                ["floating"] = sync.IsFloating, ["on_boat"] = sync.OnBoat, ["stationary"] = sync.IsStationary,
                ["simulator_id"] = sync.SyncedSimulator?.ClientId.ToString()
            };
        }
        if (item is Creature creature)
        {
            value["health"] = creature._hp.Value;
            value["dead"] = creature._hp.Value <= 0;
            value["drip"] = creature.IsDrip;
        }
        return value;
    }
}
