using System;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;

namespace HowToLua;

[LuaBridge]
internal static class ItemApi
{
    internal static void Register(Script script, Table api, LuaHost host)
    {
        var items = new Table(script);
        items.Set("list", DynValue.NewCallback((c, a) =>
        {
            int offset = a.Count > 0 ? LuaArguments.Integer(a[0], "offset", 0, 1000000) : 0;
            int limit = a.Count > 1 ? LuaArguments.Integer(a[1], "limit", 1, 256) : 64;
            return LuaValues.From(script, host.IsHost ? GameAccess.Items().OrderBy(i => i.NetworkObject.ObjectId).Skip(offset).Take(limit).Select(i => (object)GameSnapshots.ItemInfo(i)).ToArray() : Array.Empty<object>());
        }));
        items.Set("get", DynValue.NewCallback((c, a) => LuaValues.From(script, ItemSnapshots.Detail(GameAccess.Item(host, a[0])))));
        items.Set("nearby", DynValue.NewCallback((c, a) =>
        {
            Vector3 position = GameAccess.Position(a, 0);
            float radius = (float)LuaArguments.Number(a[3], "radius", 0, 1000);
            return LuaValues.From(script, host.IsHost ? GameAccess.Items().Where(i => (i.transform.position - position).sqrMagnitude <= radius * radius)
                .OrderBy(i => (i.transform.position - position).sqrMagnitude).Take(256).Select(i => (object)GameSnapshots.ItemInfo(i)).ToArray() : Array.Empty<object>());
        }));
        items.Set("spawn", DynValue.NewCallback((c, a) =>
        {
            byte id = (byte)LuaArguments.Integer(a[0], "definition ID", 0, 255);
            Vector3 position = GameAccess.Position(a, 1);
            float yaw = a.Count > 4 ? (float)LuaArguments.Number(a[4], "yaw", -360, 360) : 0;
            if (!GameAccess.WorldReady(host) || !ItemManager.Instance || !ItemManager.Instance.IsServerInitialized || !Server.Instance.DynamicObjectsHolder) return DynValue.Nil;
            Item prefab = GameInfo.GetSpawnable(id);
            if (!prefab || GameAccess.Protected(prefab) || !host.AllowSpawn()) return DynValue.Nil;
            Item spawned = ItemManager.Instance.SpawnNewItem(prefab, position, Quaternion.Euler(0, yaw, 0));
            host.TrackSpawn(spawned);
            return LuaValues.From(script, GameSnapshots.ItemInfo(spawned));
        }));
        items.Set("despawn", DynValue.NewCallback((c, a) =>
        {
            Item item = GameAccess.Item(host, a[0]);
            if (!GameAccess.WorldReady(host) || !GameAccess.Loose(item) || GameAccess.Protected(item)) return DynValue.False;
            item.DestroyItem();
            return DynValue.True;
        }));
        items.Set("cook", DynValue.NewCallback((c, a) =>
        {
            Item item = GameAccess.Item(host, a[0]);
            float amount = (float)LuaArguments.Number(a[1], "amount", 0, 2);
            if (!(item is Creature) || GameAccess.Protected(item)) return DynValue.False;
            item.CookItem(amount);
            return DynValue.True;
        }));
        items.Set("set_skin", DynValue.NewCallback((c, a) =>
        {
            Item item = GameAccess.Item(host, a[0]);
            byte skin = (byte)LuaArguments.Integer(a[1], "skin", 0, 255);
            if (!item || !item.SkinPreset || skin >= item.SkinPreset.Skins.Count) return DynValue.False;
            item.ServerSetSkin(skin);
            return DynValue.True;
        }));
        items.Set("set_interactable", DynValue.NewCallback((c, a) =>
        {
            Item item = GameAccess.Item(host, a[0]);
            bool enabled = LuaArguments.Boolean(a[1], "enabled");
            if (!GameAccess.Loose(item) || GameAccess.Protected(item)) return DynValue.False;
            item.ToggleInteractable(enabled);
            return DynValue.True;
        }));
        items.Set("set_score_multiplier", DynValue.NewCallback((c, a) =>
        {
            Item item = GameAccess.Item(host, a[0]);
            float multiplier = (float)LuaArguments.Number(a[1], "multiplier", 1, 100);
            if (!item || item.KillScoreMultiplier != 1f) return DynValue.False;
            item.SetKillscoreMultiplier(multiplier);
            return DynValue.True;
        }));
        items.Set("damage", DynValue.NewCallback((c, a) =>
        {
            Creature creature = GameAccess.Item(host, a[0]) as Creature;
            int damage = LuaArguments.Integer(a[1], "damage", 0, 100000);
            if (!creature || creature._hp.Value <= 0 || creature.IsDead) return DynValue.False;
            creature.ServerChangeHp(damage);
            return DynValue.True;
        }));
        api.Set("items", DynValue.NewTable(items));
    }
}
