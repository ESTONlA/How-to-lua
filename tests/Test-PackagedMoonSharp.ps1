param([Parameter(Mandatory = $true)][string]$DllPath)
$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path -LiteralPath $DllPath).Path
$assembly = [Reflection.Assembly]::LoadFrom($resolved)
if ($assembly.Location -ne $resolved) { throw 'Run this check in a fresh PowerShell process.' }
$references = @($assembly.GetReferencedAssemblies() | ForEach-Object { $_.Name })
if ($references -notcontains 'mscorlib' -or $references -contains 'System.Collections' -or $references -contains 'System.Runtime') {
    throw 'Wrong MoonSharp binary: package the net40-client build.'
}
$scriptEngine = [MoonSharp.Interpreter.Script]::new([MoonSharp.Interpreter.CoreModules]::Preset_HardSandbox)
$result = $scriptEngine.DoString('local values = {40, 2}; local function sum(t) return t[1] + t[2] end; return sum(values)')
if ($result.Number -ne 42) { throw 'Packaged MoonSharp table/callback execution failed.' }
Write-Output 'PASS: packaged MoonSharp creates tables and runs Lua under the Windows .NET Framework runtime.'
