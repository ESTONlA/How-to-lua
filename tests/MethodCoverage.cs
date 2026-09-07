using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mono.Cecil;

internal static class MethodCoverage
{
    // Only gameplay bridges count, not native-menu wiring or framework infrastructure.
    private static readonly HashSet<string> BridgeTypes = new(StringComparer.Ordinal)
    {
        "CoreApi", "PlayerApi", "WorldApi", "GameSnapshots", "GameHooks",
        "PlayerHooks", "ItemHooks", "WorldHooks"
    };

    internal static void Generate(string repo)
    {
        const string gamePath = @"C:\Program Files (x86)\Steam\steamapps\common\How to Fish\How to Fish\How to Fish_Data\Managed\Assembly-CSharp.dll";
        string pluginPath = Path.Combine(repo, "bin", "Release", "HowToLua.dll");
        using var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(gamePath));
        resolver.AddSearchDirectory(Path.Combine(Path.GetDirectoryName(gamePath), "../../BepInEx/core"));
        using var game = AssemblyDefinition.ReadAssembly(gamePath, new ReaderParameters { AssemblyResolver = resolver });
        using var plugin = AssemblyDefinition.ReadAssembly(pluginPath, new ReaderParameters { AssemblyResolver = resolver });
        var methods = AllTypes(game.MainModule.Types).SelectMany(t => t.Methods)
            .Where(m => !m.IsConstructor).ToDictionary(m => m.FullName, StringComparer.Ordinal);
        var integrations = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        void Record(MethodDefinition method, string reason)
        {
            if (method == null || method.Module.Assembly.Name.Name != game.Name.Name || !methods.ContainsKey(method.FullName)) return;
            if (!integrations.TryGetValue(method.FullName, out var reasons))
                integrations[method.FullName] = reasons = new SortedSet<string>(StringComparer.Ordinal);
            reasons.Add(reason);
        }

        foreach (var type in AllTypes(plugin.MainModule.Types))
        {
            var root = type;
            while (root.DeclaringType != null) root = root.DeclaringType;
            if (root.Namespace != "HowToLua" || !BridgeTypes.Contains(root.Name)) continue;

            foreach (var attribute in type.CustomAttributes.Where(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch"))
            {
                var targetType = (TypeReference)attribute.ConstructorArguments[0].Value;
                string targetName = (string)attribute.ConstructorArguments[1].Value;
                var original = game.MainModule.GetType(targetType.FullName).Methods.Single(m => m.Name == targetName);
                Record(original, "Event/command hook");
            }

            foreach (var method in type.Methods.Where(m => m.HasBody))
            {
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.Operand is not MethodReference reference || reference.DeclaringType.Scope.Name != game.Name.Name) continue;
                    Record(reference.Resolve(), "Direct gameplay bridge call/read");
                }
            }
        }

        int total = methods.Count;
        int covered = integrations.Count;
        decimal percent = total == 0 ? 0 : 100m * covered / total;
        string percentage = percent.ToString("0.00", CultureInfo.InvariantCulture);
        string output = Path.Combine(repo, "coverage");
        Directory.CreateDirectory(output);
        string gameHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(gamePath)));
        string pluginHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(pluginPath)));
        var report = new
        {
            metric = "Direct game-method integration",
            frameworkVersion = plugin.Name.Version.ToString(3),
            gameAssembly = game.Name.Name,
            gameAssemblySha256 = gameHash,
            frameworkAssemblySha256 = pluginHash,
            integratedMethods = covered,
            totalMethods = total,
            percentage = Math.Round(percent, 4),
            rules = "Unique direct calls/reads and Harmony targets in gameplay bridge classes divided by all non-constructor methods declared in Assembly-CSharp.dll. Includes accessors, generated methods and embedded libraries; excludes other assemblies. No transitive calls, UI wiring, field reads or line coverage.",
            methods = integrations.Select(pair => new { signature = pair.Key, reasons = pair.Value.ToArray() }).ToArray()
        };
        File.WriteAllText(Path.Combine(output, "method-coverage.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");

        string label = "Game method integration";
        string value = percentage + "%";
        File.WriteAllText(Path.Combine(output, "method-coverage.svg"),
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"250\" height=\"24\" role=\"img\" aria-label=\"{label}: {value}\"><title>{label}: {value}</title><rect width=\"180\" height=\"24\" fill=\"#34383f\"/><rect x=\"180\" width=\"70\" height=\"24\" fill=\"#087f8c\"/><g fill=\"#fff\" text-anchor=\"middle\" font-family=\"Verdana,Arial,sans-serif\" font-size=\"11\"><text x=\"90\" y=\"16\">{label}</text><text x=\"215\" y=\"16\">{value}</text></g></svg>\n");
        var markdown = new StringBuilder();
        markdown.AppendLine("# Game Method Coverage\n");
        markdown.AppendLine($"**{percentage}% direct method integration: {covered.ToString("N0", CultureInfo.InvariantCulture)} / {total.ToString("N0", CultureInfo.InvariantCulture)} methods.**\n");
        markdown.AppendLine("This measures how much of the installed game's managed method surface the Lua gameplay bridges directly touch. It is not the percentage of features supported, executable lines covered, or methods callable freely from Lua. Some integrations only observe a method or read a property.\n");
        markdown.AppendLine("## Counting Rules\n");
        markdown.AppendLine("- Denominator: every non-constructor method declared in `Assembly-CSharp.dll`, including getters/setters, generated networking methods and any embedded libraries. Unity, FishNet and other DLLs are not included.");
        markdown.AppendLine("- Numerator: unique game methods referenced by gameplay APIs/snapshots/hooks, plus the methods patched by those hooks. Internal UI wiring and arbitrary framework infrastructure do not count.");
        markdown.AppendLine("- Methods called internally by an integrated game method are not counted automatically. Reading fields does not add methods. A method appearing in several bridges is counted once.");
        markdown.AppendLine("- This is a static analysis of the built DLL, not a multiplayer test result or a promise that every argument/behavior of an integrated method is exposed.\n");
        markdown.AppendLine("## Recalculate\n");
        markdown.AppendLine("Run `dotnet run --project tests/SmokeTests.csproj -c Release -- --coverage` after changing the bridges or updating the game. Commit the generated `coverage` files to refresh the README badge. The generator does not publish to GitHub.\n");
        markdown.AppendLine($"Framework version: `{plugin.Name.Version.ToString(3)}`.\n");
        markdown.AppendLine($"Game DLL SHA-256: `{gameHash}`.\n");
        markdown.AppendLine($"Framework DLL SHA-256: `{pluginHash}`.\n");
        markdown.AppendLine("## Integrated Methods\n");
        markdown.AppendLine("| Game method | Integration |\n| --- | --- |");
        foreach (var pair in integrations)
            markdown.AppendLine("| `" + pair.Key + "` | " + string.Join(", ", pair.Value) + " |");
        File.WriteAllText(Path.Combine(output, "README.md"), markdown.ToString());
        Console.WriteLine($"Direct game-method integration: {covered}/{total} = {percentage}%");
        Console.WriteLine($"Harmony targets: {integrations.Count(p => p.Value.Contains("Event/command hook"))}; direct calls/reads: {integrations.Count(p => p.Value.Contains("Direct gameplay bridge call/read"))}");
        Console.WriteLine("Generated coverage/method-coverage.svg, method-coverage.json and README.md.");
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
