<#
  Diagnose and repair the Claude Code <-> Unity MCP link.
  Usage (PowerShell, from the project root):
    .\tools\unity-mcp.ps1            # check only
    .\tools\unity-mcp.ps1 -Start     # also start a standalone server if none is running
    .\tools\unity-mcp.ps1 -Install   # once: watchdog task (at login, then every minute: restart the server if it is down)
    .\tools\unity-mcp.ps1 -Uninstall # remove that task
    .\tools\unity-mcp.ps1 -Log       # show the watchdog log (when the server went down and was restarted)
  The server version is pinned to match the Unity plugin pinned in Packages/manifest.json;
  bump both together (see docs/unity-mcp-connection.md).
  Why: the Unity plugin owns the HTTP server and restarts it (toggle, Unity restart), which
  invalidates Claude Code's session. A standalone server outlives Unity and Claude Code, and the
  watchdog brings it back within a minute if anything kills it. See docs/unity-mcp-connection.md.
#>
param([switch]$Start, [switch]$Install, [switch]$Uninstall, [switch]$Watch, [switch]$Log, [int]$Port = 8080)

$ServerVersion = "10.2.0"   # must match the plugin version in Packages/manifest.json
$TaskName = "UnityMCPServer"
$url = "http://127.0.0.1:$Port"
$uvx = (Get-Command uvx).Source
$serverArgs = "--from mcpforunityserver==$ServerVersion mcp-for-unity --transport http --http-url $url"
$LogFile = Join-Path $env:LOCALAPPDATA "UnityMCP\watchdog.log"   # outside the repo on purpose

function Get-Health { try { Invoke-RestMethod "$url/health" -TimeoutSec 3 } catch { $null } }
function Write-WatchLog($text) {
    New-Item -ItemType Directory -Force (Split-Path $LogFile) | Out-Null
    Add-Content -Path $LogFile -Value ("{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $text)
}
function Start-Server {
    # WMI creates the process outside our process tree, so it outlives the shell that ran this script.
    Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = "`"$uvx`" $serverArgs" } | Out-Null
}

if ($Log) {
    if (Test-Path $LogFile) { Get-Content $LogFile -Tail 60 } else { Write-Host "No log yet: $LogFile" }
    return
}
if ($Uninstall) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue
    Write-Host "Removed task $TaskName"
    return
}
if ($Install) {
    # conhost --headless: no console window flashes each minute. Current user only, no admin needed.
    $me = $env:USERNAME
    $script = (Resolve-Path $PSCommandPath).Path
    $action = New-ScheduledTaskAction -Execute "conhost.exe" `
        -Argument "--headless powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$script`" -Watch"
    $logon = New-ScheduledTaskTrigger -AtLogOn -User $me
    $every = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 1)
    $settings = New-ScheduledTaskSettingsSet -Hidden -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
        -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 2) -StartWhenAvailable
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger @($logon, $every) -Settings $settings -Force | Out-Null
    Write-Host "Installed watchdog task $TaskName (at login and every minute; log: $LogFile)"
    return
}
if ($Watch) {
    # Runs from Task Scheduler every minute. Silent and cheap when the server is healthy.
    if (Get-Health) { return }
    $unity = if (Get-Process Unity -ErrorAction SilentlyContinue) { "Unity running" } else { "Unity NOT running" }
    $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listener) {
        # Something holds the port but /health fails: hung server. Log it and leave it (never kill blindly).
        Write-WatchLog "DOWN: port $Port held by PID $($listener.OwningProcess) but /health failed ($unity); not restarting"
        return
    }
    Write-WatchLog "DOWN: nothing listening on port $Port ($unity); restarting"
    Start-Server
    Start-Sleep 8
    Write-WatchLog $(if (Get-Health) { "UP: server restarted" } else { "FAILED: server did not answer after restart" })
    return
}

Write-Host "== Unity editor"
$unity = Get-Process Unity -ErrorAction SilentlyContinue
if ($unity) { Write-Host "running (PID $($unity.Id -join ','))" } else { Write-Host "NOT running - the bridge cannot connect" }

Write-Host "== Server on port $Port"
$h = Get-Health
if ($h) { Write-Host "healthy: $($h.message) v$($h.version)" } else { Write-Host "no response" }
$listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($listener) {
    $p = Get-CimInstance Win32_Process -Filter "ProcessId=$($listener.OwningProcess)"
    $owner = if ($p.CommandLine -match '--pidfile') { "started by Unity (dies/restarts with the plugin)" } else { "standalone (survives Unity)" }
    Write-Host "PID $($listener.OwningProcess): $owner, running since $($p.CreationDate)"
}

Write-Host "== Watchdog task"
$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($task) { Write-Host "$TaskName installed ($($task.State)); last events: .\tools\unity-mcp.ps1 -Log" } else { Write-Host "$TaskName NOT installed: run -Install" }

Write-Host "== Claude Code registrations (want exactly one, pointing at $url/mcp)"
claude mcp list 2>&1 | Select-String -Pattern 'unity' -CaseSensitive:$false

if ($Start -and -not $h) {
    Write-Host "== Starting standalone server"
    Start-Server
    Start-Sleep 6
    if (Get-Health) { Write-Host "server up" } else { Write-Host "server did not answer; run uvx by hand to see the error" }
}
