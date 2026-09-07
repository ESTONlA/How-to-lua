$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet build HowToLua.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet run --project tests/SmokeTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Framework tests failed.' }
    $version = [System.Reflection.AssemblyName]::GetAssemblyName((Join-Path $PSScriptRoot 'bin/Release/HowToLua.dll')).Version.ToString(3)
    $stage = Join-Path $PSScriptRoot "release/package-$version"
    $plugin = Join-Path $stage 'BepInEx/plugins/HowToLua'
    New-Item -ItemType Directory -Force $plugin | Out-Null
    Copy-Item bin/Release/HowToLua.dll,bin/Release/MoonSharp.Interpreter.dll -Destination $plugin -Force
    Copy-Item README.md,MoonSharp-LICENSE.txt -Destination $stage -Force
    Copy-Item examples -Destination $stage -Recurse -Force
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Test-PackagedMoonSharp.ps1 -DllPath (Join-Path $plugin 'MoonSharp.Interpreter.dll')
    if ($LASTEXITCODE -ne 0) { throw 'Packaged MoonSharp compatibility check failed.' }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath "release/HowToLua-$version.zip" -Force
} finally { Pop-Location }
