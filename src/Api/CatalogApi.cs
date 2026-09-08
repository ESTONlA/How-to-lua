using System;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

namespace HowToLua;

[LuaBridge]
internal static class CatalogApi
{
    internal static void Register(Script script, Table api, LuaHost host)
    {
        var catalog = new Table(script);
        catalog.Set("items", DynValue.NewCallback((c, a) => LuaValues.From(script, GameAccess.WorldReady(host)
            ? Enumerable.Range(0, 256).Select(id => GameInfo.GetSpawnable((byte)id)).Where(i => i).Distinct().Select(i => (object)ItemSnapshots.Definition(i)).ToArray() : Array.Empty<object>())));
        catalog.Set("item", DynValue.NewCallback((c, a) =>
        {
            int id = LuaArguments.Integer(a[0], "definition ID", 0, 255);
            return LuaValues.From(script, GameAccess.WorldReady(host) ? ItemSnapshots.Definition(GameInfo.GetSpawnable((byte)id)) : null);
        }));
        catalog.Set("baits", DynValue.NewCallback((c, a) => LuaValues.From(script, GameAccess.WorldReady(host)
            ? GameInfo.AllBaits.Select((bait, id) => (object)Bait(bait, id)).ToArray() : Array.Empty<object>())));
        catalog.Set("attachments", DynValue.NewCallback((c, a) => LuaValues.From(script, GameAccess.WorldReady(host)
            ? GameInfo.AllAttachments.Select((info, id) => (object)new Dictionary<string, object>
            { ["id"] = id, ["available"] = (bool)info, ["name"] = info ? info.NameLocalized : null, ["description"] = info ? info.DescriptionLocalized : null }).ToArray() : Array.Empty<object>())));
        api.Set("catalog", DynValue.NewTable(catalog));
    }
    private static Dictionary<string, object> Bait(BaitInfo bait, int id)
    {
        if (!bait) return new() { ["id"] = id, ["available"] = false };
        return new()
        {
            ["id"] = id, ["available"] = true, ["name"] = bait.NameLocalized, ["description"] = bait.Description,
            ["cost"] = bait.Cost, ["requires_reeling"] = bait.RequireReelingToCatch, ["loss_chance"] = bait.LostOnBaitChance,
            ["catch_seconds_min"] = bait.CatchTimeMinMax.x, ["catch_seconds_max"] = bait.CatchTimeMinMax.y,
            ["weights"] = (bait.ItemWeights ?? new List<ItemInfoWeight>()).Where(w => w != null).Select(w => (object)new Dictionary<string, object>
                { ["asset_name"] = w.Fishable ? w.Fishable.name : null, ["weight"] = w.Weight }).ToArray()
        };
    }
}
