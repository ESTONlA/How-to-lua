using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MoonSharp.Interpreter;

namespace HowToLua;

[LuaBridge]
internal static class InventoryApi
{
    private static readonly FieldInfo StartingSlots = AccessTools.Field(typeof(PlayerInventory), "_startingSlots");
    private static readonly FieldInfo PocketCosts = AccessTools.Field(typeof(PlayerInventory), "_extraSlotCosts");
    private static PlayerInventory Get(LuaHost host, DynValue id)
    {
        Player player = GameAccess.Player(host, id);
        return player && player.Inventory && player.Inventory.IsServerInitialized ? player.Inventory : null;
    }
    private static int Capacity(PlayerInventory inventory) => (int)StartingSlots.GetValue(inventory) + inventory.ExtraSlots;
    internal static void Register(Script script, Table api, LuaHost host)
    {
        var inventory = new Table(script);
        inventory.Set("get", DynValue.NewCallback((c, a) =>
        {
            PlayerInventory bag = Get(host, a[0]);
            if (!bag) return DynValue.Nil;
            return LuaValues.From(script, new Dictionary<string, object>
            {
                ["capacity"] = Capacity(bag), ["extra_pockets"] = (int)bag.ExtraSlots, ["selected_slot"] = bag._syncedCurSlot.Value,
                ["bait"] = (int)bag.CurBait,
                ["slots"] = bag._items.OrderBy(pair => pair.Key).Select(pair => (object)new Dictionary<string, object>
                { ["slot"] = (int)pair.Key, ["unlocked"] = pair.Key < Capacity(bag), ["item"] = GameSnapshots.ItemInfo(pair.Value) }).ToArray(),
                ["owned_baits"] = bag._ownedBaits.Select((count, index) => (object)new Dictionary<string, object> { ["id"] = index + 1, ["count"] = count }).ToArray()
            });
        }));
        inventory.Set("contains", DynValue.NewCallback((c, a) =>
        {
            PlayerInventory bag = Get(host, a[0]);
            Item item = GameAccess.Item(host, a[1]);
            return DynValue.NewBoolean(bag && item && bag.HasItemInInventory(item));
        }));
        inventory.Set("select_slot", DynValue.NewCallback((c, a) =>
        {
            PlayerInventory bag = Get(host, a[0]);
            int slot = LuaArguments.Integer(a[1], "slot", -1, 255);
            Player player = GameAccess.Player(host, a[0]);
            if (!bag || !player || !player.Vitals || player.Vitals.Health <= 0 || slot >= Capacity(bag) || (slot >= 0 && (!bag._items.TryGetValue((byte)slot, out Item item) || !item))) return DynValue.False;
            bag.ServerSetSyncedCurSlot(slot);
            return DynValue.True;
        }));
        inventory.Set("set_bait", DynValue.NewCallback((c, a) =>
        {
            PlayerInventory bag = Get(host, a[0]);
            int id = LuaArguments.Integer(a[1], "bait ID", 0, 255);
            if (!bag || id >= GameInfo.AllBaits.Count || (id > 0 && (id > bag._ownedBaits.Count || bag._ownedBaits[id - 1] <= 0))) return DynValue.False;
            bag.ServerSetCurBait((byte)id);
            return DynValue.True;
        }));
        inventory.Set("give_bait", DynValue.NewCallback((c, a) =>
        {
            PlayerInventory bag = Get(host, a[0]);
            int id = LuaArguments.Integer(a[1], "bait ID", 1, 255);
            int count = LuaArguments.Integer(a[2], "count", 1, 100);
            if (!bag || id >= GameInfo.AllBaits.Count || id > bag._ownedBaits.Count || bag._ownedBaits[id - 1] > int.MaxValue - count) return DynValue.False;
            for (int i = 0; i < count; i++) bag.ServerBoughtBait((byte)id);
            return DynValue.True;
        }));
        inventory.Set("pocket_cost", DynValue.NewCallback((c, a) =>
        {
            PlayerInventory bag = Get(host, a[0]);
            int index = LuaArguments.Integer(a[1], "pocket", 1, 255);
            if (!bag || index > ((int[])PocketCosts.GetValue(bag)).Length) return DynValue.Nil;
            return DynValue.NewNumber(bag.GetExtraSlotCost((byte)index));
        }));
        inventory.Set("unlock_pockets", DynValue.NewCallback((c, a) =>
        {
            PlayerInventory bag = Get(host, a[0]);
            int total = LuaArguments.Integer(a[1], "extra pockets", 0, 255);
            if (!bag || total > ((int[])PocketCosts.GetValue(bag)).Length || (int)StartingSlots.GetValue(bag) + total > bag._items.Count) return DynValue.False;
            bag.UnlockExtraPocket((byte)total);
            return DynValue.NewBoolean(bag.HasPocket(total));
        }));
        api.Set("inventory", DynValue.NewTable(inventory));
    }
}
