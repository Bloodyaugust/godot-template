#!/usr/bin/env pwsh
# Run the Hurl agentic scenarios. Boots a FRESH game instance per .hurl file
# (each file is self-contained: starts at the main scene, ends by quitting the
# game), and GUARANTEES teardown even when a file fails partway. See
# tests/hurl/README.md.
#
#   ./run-tests.ps1                       # all *.hurl, headless
#   ./run-tests.ps1 -Windowed             # watch it run
#   ./run-tests.ps1 smoke.hurl            # a single file
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

$failed = 0
foreach ($f in $Files) {
    Write-Host "=== $([IO.Path]::GetFileName($f)) ==="
    $gargs = @('--path', $root)
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

if ($failed -gt 0) { Write-Host "`nFAILED: $failed file(s)"; exit 1 }
Write-Host "`nAll Hurl tests passed"
exit 0
