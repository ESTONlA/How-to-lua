using HarmonyLib;

namespace HowToLua;

[LuaBridge]
internal static class InventoryHooks
{
    [HarmonyPatch(typeof(PlayerInventory), "OnItemsChange")]
    private static class SlotsPatch
    {
        private static void Postfix(Player ____player, FishNet.Object.Synchronizing.SyncDictionaryOperation op, byte index, Item item, bool asServer)
        {
            if (asServer)
                GameHooks.Capture("inventory_changed", () => new object[] { GameSnapshots.PlayerInfo(____player), op.ToString(), (int)index, GameSnapshots.ItemInfo(item) });
        }
    }

    [HarmonyPatch(typeof(PlayerInventory), "OnCurSlotChange")]
    private static class SelectionPatch
    {
        private static void Postfix(Player ____player, int prev, int next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("inventory_slot_changed", () => new object[] { GameSnapshots.PlayerInfo(____player), prev, next });
        }
    }

    [HarmonyPatch(typeof(PlayerInventory), "OnInventoryAmountChange")]
    private static class PocketsPatch
    {
        private static void Postfix(Player ____player, byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("inventory_pockets_changed", () => new object[] { GameSnapshots.PlayerInfo(____player), (int)prev, (int)next });
        }
    }

    [HarmonyPatch(typeof(PlayerInventory), "OnCurBaitChange")]
    private static class BaitPatch
    {
        private static void Postfix(Player ____player, byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("inventory_bait_changed", () => new object[] { GameSnapshots.PlayerInfo(____player), (int)prev, (int)next });
        }
    }

    [HarmonyPatch(typeof(PlayerInventory), "OnOwnedBaitChange")]
    private static class StockPatch
    {
        private static void Postfix(Player ____player, FishNet.Object.Synchronizing.SyncListOperation op, int index, int oldAmount, int newAmount, bool asServer)
        {
            if (asServer)
                GameHooks.Capture("inventory_bait_stock_changed", () => new object[] { GameSnapshots.PlayerInfo(____player), op.ToString(), index + 1, oldAmount, newAmount });
        }
    }
}

