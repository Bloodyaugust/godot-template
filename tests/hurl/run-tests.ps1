#!/usr/bin/env pwsh
# Run the Hurl agentic scenarios. Boots a FRESH game instance per .hurl file
# (each file is self-contained: starts at the main scene, ends by quitting the
# game), and GUARANTEES teardown even when a file fails partway. See
# tests/hurl/README.md.
#
#   ./run-tests.ps1                       # all *.hurl, headless
#   ./run-tests.ps1 -Windowed             # watch it run
#   ./run-tests.ps1 -Files smoke.hurl     # a single file (bare positional
#                                         # misbinds to -Port under PS 5.1)
#   ./run-tests.ps1 -Port 9090            # non-default REST port
param(
    [Parameter(ValueFromRemainingArguments = $true)] [string[]]$Files,
    [int]$Port = 8080,
    [switch]$Windowed
)

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path "$here/../..").Path
$hostPort = "127.0.0.1:$Port"

# Resolve hurl (PATH, or the default winget install location on Windows).
$cmd = Get-Command hurl -ErrorAction SilentlyContinue
if ($cmd) { $hurl = $cmd.Source }
elseif (Test-Path "C:\Program Files\Hurl\hurl.exe") { $hurl = "C:\Program Files\Hurl\hurl.exe" }
else {
    Write-Host "hurl not found on PATH. Install it (see tests/hurl/README.md), e.g.:"
    Write-Host "  winget install --id Orange-OpenSource.Hurl"
    exit 127
}

if (-not $Files -or $Files.Count -eq 0) {
    $Files = Get-ChildItem "$here/*.hurl" | ForEach-Object { $_.FullName }
} else {
    $Files = $Files | ForEach-Object { if (Test-Path $_) { (Resolve-Path $_).Path } else { Join-Path $here $_ } }
}

# Pin the player profile to memory-only on every instance we boot: test runs
# must never touch the developer's real user://profile.json (display-settings
# mutations included — display_input.hurl).
$env:GODOT_PROFILE_MEMORY = '1'

$failed = 0
foreach ($f in $Files) {
    Write-Host "=== $([IO.Path]::GetFileName($f)) ==="
    # Boot scene: the configured main scene unless the file carries a
    # "# boot_scene:" directive in its header. "default" means no scene
    # argument — the configured main scene; any other value is passed to
    # godot-mono as the scene path (e.g. res://scenes/foo.tscn), so files
    # exercising a non-default scene never have to walk the menu flow.
    $scene = 'default'
    $directive = Select-String -Path $f -Pattern '^#\s*boot_scene:\s*(\S+)' | Select-Object -First 1
    if ($directive) { $scene = $directive.Matches[0].Groups[1].Value }
    $gargs = @('--path', $root)
    if ($scene -ne 'default') { $gargs += $scene }
    if (-not $Windowed) { $gargs += '--headless' }
    $proc = Start-Process godot-mono -ArgumentList $gargs -PassThru
    try {
        & $hurl --test --jobs 1 --variable host=$hostPort --error-format long $f
        if ($LASTEXITCODE -ne 0) { $failed++ }
    } finally {
        # The file quits itself on success; force teardown on any failure path.
        try { Invoke-RestMethod "http://$hostPort/quit" -TimeoutSec 3 | Out-Null } catch {}
        Start-Sleep -Milliseconds 800
        $up = $true
        try { Invoke-WebRequest "http://$hostPort/status" -TimeoutSec 2 -UseBasicParsing | Out-Null } catch { $up = $false }
        # Only ever kill the specific PID we launched — never godot-mono by name.
        if ($up -and -not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
    }
}

Remove-Item Env:GODOT_PROFILE_MEMORY -ErrorAction SilentlyContinue

if ($failed -gt 0) { Write-Host "`nFAILED: $failed file(s)"; exit 1 }
Write-Host "`nAll Hurl tests passed"
exit 0
