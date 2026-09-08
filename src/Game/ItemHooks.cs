using HarmonyLib;

namespace HowToLua;

[LuaBridge]
internal static class ItemHooks
{
    [HarmonyPatch(typeof(Item), "OnStartServer")]
    private static class SpawnPatch
    {
        private static void Postfix(Item __instance)
        {
            GameHooks.Capture("item_spawned", () => new object[] { GameSnapshots.ItemInfo(__instance) });
        }
    }

    [HarmonyPatch(typeof(Item), "OnStopServer")]
    private static class DespawnPatch
    {
        private static void Prefix(Item __instance)
        {
            GameHooks.Capture("item_despawned", () => new object[] { GameSnapshots.ItemInfo(__instance) });
        }
    }

    [HarmonyPatch(typeof(Item), "OnSyncedHolderChange")]
    private static class HolderPatch
    {
        private static void Postfix(Item __instance, Player prev, Player next, bool asServer)
        {
            if (!asServer || prev == next) return;
            if (prev) GameHooks.Capture("item_dropped", () => new object[] { GameSnapshots.ItemInfo(__instance), GameSnapshots.PlayerInfo(prev) });
            if (next) GameHooks.Capture("item_picked_up", () => new object[] { GameSnapshots.ItemInfo(__instance), GameSnapshots.PlayerInfo(next) });
        }
    }

    [HarmonyPatch(typeof(Item), "OnCooknessChange")]
    private static class CooknessPatch
    {
        private static void Postfix(Item __instance, float prev, float next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("item_cooked", () => new object[] { GameSnapshots.ItemInfo(__instance), prev, next });
        }
    }

    [HarmonyPatch(typeof(Item), "OnCurSkinChange")]
    private static class SkinPatch
    {
        private static void Postfix(Item __instance, byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("item_skin_changed", () => new object[] { GameSnapshots.ItemInfo(__instance), (int)prev, (int)next });
        }
    }

    [HarmonyPatch(typeof(MoneyManager), "SellItem")]
    private static class SalePatch
    {
        private static void Prefix(Item item, out object[] __state)
        {
            __state = GameHooks.Snapshot(() => item && MoneyManager.Instance && MoneyManager.Instance.IsServerInitialized
                ? new object[] { GameSnapshots.ItemInfo(item), GameSnapshots.PlayerInfo(item.LastHolder) } : null);
        }
        private static void Postfix(Item item, object[] __state)
        {
            GameHooks.Publish("item_sold", __state);
            if (item is Fish) GameHooks.Publish("fish_sold", __state);
        }
    }

    [HarmonyPatch(typeof(MoneyManager), "OnChangeMoney")]
    private static class MoneyPatch
    {
        private static void Postfix(int prev, int next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("money_changed", () => new object[] { prev, next, (long)next - prev });
        }
    }
}
