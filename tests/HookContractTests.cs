using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

internal static class HookContractTests
{
    internal static void Run(string repo, Action<bool, string> check)
    {
        const string gamePath = @"C:\Program Files (x86)\Steam\steamapps\common\How to Fish\How to Fish\How to Fish_Data\Managed\Assembly-CSharp.dll";
        using var game = AssemblyDefinition.ReadAssembly(gamePath);
        using var plugin = AssemblyDefinition.ReadAssembly(Path.Combine(repo, "bin", "Release", "HowToLua.dll"));
        int patches = 0;
        foreach (var type in AllTypes(plugin.MainModule.Types))
        {
            var attribute = type.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch");
            if (attribute == null) continue;
            var targetType = (TypeReference)attribute.ConstructorArguments[0].Value;
            string methodName = (string)attribute.ConstructorArguments[1].Value;
            var originalType = game.MainModule.GetType(targetType.FullName);
            var candidates = originalType?.Methods.Where(m => m.Name == methodName).ToArray();
            check(candidates?.Length == 1, "patch target exists unambiguously: " + targetType.Name + "." + methodName);
            var original = candidates[0];
            foreach (var method in type.Methods.Where(m => m.Name == "Prefix" || m.Name == "Postfix"))
            {
                foreach (var parameter in method.Parameters)
                {
                    if (parameter.Name == "__state") continue;
                    TypeReference expected;
                    if (parameter.Name == "__instance") expected = originalType;
                    else if (parameter.Name == "__result") expected = original.ReturnType;
                    else if (parameter.Name.StartsWith("___"))
                        expected = originalType.Fields.SingleOrDefault(f => f.Name == parameter.Name.Substring(3))?.FieldType;
                    else expected = original.Parameters.SingleOrDefault(p => p.Name == parameter.Name)?.ParameterType;
                    var actual = parameter.ParameterType is ByReferenceType byRef ? byRef.ElementType : parameter.ParameterType;
                    check(expected != null && expected.FullName == actual.FullName,
                        type.Name + "." + method.Name + " injection " + parameter.Name);
                }
            }
            patches++;
        }
        check(patches == 17, "all 17 game patch classes checked against installed assembly");
        check(plugin.MainModule.GetType("HowToLua.HowToLuaPlugin").NestedTypes.Count == 0, "bootstrap no longer contains runtime, UI or hook classes");
    }

    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> roots)
    {
        foreach (var type in roots)
        {
            yield return type;
            foreach (var child in AllTypes(type.NestedTypes)) yield return child;
        }
    }
}
