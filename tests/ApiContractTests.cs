using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

internal static class ApiContractTests
{
    internal static void Run(string repo, Action<bool, string> check)
    {
        using var plugin = AssemblyDefinition.ReadAssembly(Path.Combine(repo, "bin/Release/HowToLua.dll"));
        using var game = AssemblyDefinition.ReadAssembly(@"C:\Program Files (x86)\Steam\steamapps\common\How to Fish\How to Fish\How to Fish_Data\Managed\Assembly-CSharp.dll");
        var modules = new (string Name, int Count)[]
        {
            ("CoreApi", 11), ("PlayerApi", 6), ("WorldApi", 4), ("ItemApi", 10),
            ("CatalogApi", 4), ("InventoryApi", 7), ("CombatApi", 8), ("ServerApi", 8),
            ("BoatApi", 7), ("ProgressionApi", 5), ("NpcApi", 5), ("BossApi", 4)
        };
        var register = plugin.MainModule.GetType("HowToLua.GameApi").Methods.Single(m => m.Name == "Register");
        int total = 0;
        foreach (var module in modules)
        {
            var type = plugin.MainModule.GetType("HowToLua." + module.Name);
            var method = type.Methods.Single(m => m.Name == "Register");
            int count = method.Body.Instructions.Count(i => i.Operand is MethodReference call &&
                call.DeclaringType.FullName == "MoonSharp.Interpreter.DynValue" && call.Name == "NewCallback");
            check(count == module.Count, module.Name + " registers " + module.Count + " Lua callbacks");
            check(type.CustomAttributes.Any(a => a.AttributeType.FullName == "HowToLua.LuaBridgeAttribute"), module.Name + " included in measured gameplay bridges");
            if (module.Name != "CoreApi")
                check(register.Body.Instructions.Any(i => i.Operand is MethodReference call && call.DeclaringType.FullName == type.FullName && call.Name == "Register"), module.Name + " wired into GameApi");
            total += count;
        }
        check(total == 79, "79 compiled Lua callback registrations (not a runtime gameplay test)");
        foreach (var field in new (string Type, string Name, string FieldType, bool Static)[]
        {
            ("PlayerInventory", "_startingSlots", "System.Int32", false),
            ("PlayerInventory", "_extraSlotCosts", "System.Int32[]", false),
            ("Boat", "_motors", "System.Collections.Generic.List`1<BoatMotor>", false),
            ("NPCManager", "_idToNpc", "System.Collections.Generic.Dictionary`2<System.Byte,NPC>", true)
        })
        {
            var actual = game.MainModule.GetType(field.Type)?.Fields.SingleOrDefault(f => f.Name == field.Name);
            check(actual != null && actual.FieldType.FullName == field.FieldType && actual.IsStatic == field.Static,
                "native private-field contract: " + field.Type + "." + field.Name);
        }
        string docs = File.ReadAllText(Path.Combine(repo, "wiki/Gameplay-API.md"));
        foreach (var group in new (string Namespace, string Methods)[]
        {
            ("players", "local_player damage poison ignite held_item"),
            ("items", "list get nearby spawn despawn cook set_skin set_interactable set_score_multiplier damage"),
            ("catalog", "items item baits attachments"),
            ("inventory", "get contains select_slot set_bait give_bait pocket_cost unlock_pockets"),
            ("combat", "weapon melee upgrade_bullets sharpen"),
            ("server", "settings set_friendly_fire set_one_shot set_difficulty save"),
            ("economy", "can_afford give spend"),
            ("boat", "info unlock unlock_radar upgrade_motor set_skin eject_driver return_to_spawn"),
            ("world", "islands travel unlock_island water_at rules"),
            ("npcs", "list get quests progression unlock_grill"),
            ("bosses", "info set_immortal scaled_health scaled_damage")
        })
            foreach (string method in group.Methods.Split(' '))
                check(docs.Contains("htf." + group.Namespace + "." + method + "("), "wiki documents API: " + group.Namespace + "." + method);
    }
}
