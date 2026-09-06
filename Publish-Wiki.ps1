$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if (!(Test-Path .wiki-publish/.git)) {
        git clone https://github.com/ESTONlA/How-to-lua.wiki.git .wiki-publish
        if ($LASTEXITCODE -ne 0) { throw 'Enable the GitHub Wiki and create its Home page first.' }
    }
    git -C .wiki-publish pull --ff-only origin master
    if ($LASTEXITCODE -ne 0) { throw 'Wiki pull failed; resolve its changes before publishing.' }
    Copy-Item wiki/*.md -Destination .wiki-publish -Force
    $wikiPages = @(Get-ChildItem wiki -Filter '*.md' -File | Select-Object -ExpandProperty Name)
    git -C .wiki-publish add -- $wikiPages
    if ($LASTEXITCODE -ne 0) { throw 'Wiki staging failed.' }
    git -C .wiki-publish diff --cached --quiet
    if ($LASTEXITCODE -eq 1) {
        git -C .wiki-publish commit -m 'Document How to Lua 0.1 API and installation'
        if ($LASTEXITCODE -ne 0) { throw 'Wiki commit failed.' }
    }
    git -C .wiki-publish push origin HEAD:master
    if ($LASTEXITCODE -ne 0) { throw 'Wiki push failed.' }
} finally { Pop-Location }
