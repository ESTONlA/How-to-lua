$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet build HowToLua.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $stage = Join-Path $PSScriptRoot 'release/package'
    $plugin = Join-Path $stage 'BepInEx/plugins/HowToLua'
    New-Item -ItemType Directory -Force $plugin | Out-Null
    Copy-Item bin/Release/HowToLua.dll,bin/Release/MoonSharp.Interpreter.dll -Destination $plugin -Force
    Copy-Item README.md,MoonSharp-LICENSE.txt -Destination $stage -Force
    Copy-Item examples -Destination $stage -Recurse -Force
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Test-PackagedMoonSharp.ps1 -DllPath (Join-Path $plugin 'MoonSharp.Interpreter.dll')
    if ($LASTEXITCODE -ne 0) { throw 'Packaged MoonSharp compatibility check failed.' }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath release/HowToLua-0.1.1.zip -Force
} finally { Pop-Location }
