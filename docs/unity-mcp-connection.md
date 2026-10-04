# Unity MCP connection: why it drops and what to do

## If Unity isn't open or MCP isn't connected
- Compile errors are in `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
- The user runs *Test Runner → EditMode → Run All*; ask for the full message of any failure (and stack traces of runtime errors).
- Don't launch Unity from the command line while the editor is open (it locks the project).

## If Unity stops answering right after a git command
A scene git rewrote while it was open: Unity is waiting on a *"modified externally"* dialog (ping not answered, `refresh_unity` times out). Ask the user to click **Reload**. To avoid it: `docs/parallel-lanes.md` → *Git commands that change a scene* (`SceneGitGuard`).

## Findings (2026-09-29)
- The server on port 8080 was **started and owned by the Unity plugin** (its command line has `--pidfile Library\MCPForUnity\...`). Every Unity restart or *Stop/Start Server* click kills it and starts a new process.
- A new server means Claude Code's old HTTP session is dead. Claude Code doesn't reliably reconnect, so the tools disappear or hang until `/mcp` → reconnect (or a new session).
- Each start also runs `claude mcp add` for **`UnityMCP`**, so the plugin keeps re-writing a second, duplicate registration next to the project `.mcp.json` entry **`unityMCP`**. Both point at the same server, and the tool list shows every tool twice.
- Compiling, running tests and entering Play Mode reload the domain and close the Unity-side WebSocket for a few seconds. Calls made in that window fail; they recover by themselves.
- The server runs from the `uv` cache; `uv cache clean` deletes it.
- The link tested healthy at the time of writing (`/health` 200, `read_console` worked), so the drops are restarts, not a broken install.

## Fix
1. Start the server yourself so Unity can't kill it: `.\tools\unity-mcp.ps1 -Start`. It is a hidden process; it survives Unity and Claude Code.
2. Unity: *Window → MCP for Unity*. Turn **off** auto-start/stop of the local server and **off** auto-register with Claude Code (Unity won't re-add `UnityMCP`), keep the URL `http://127.0.0.1:8080`, then press *Connect/Start Session* (client only).
3. Registration (changed 2026-09-29): `.mcp.json` holds one entry named **`unity-standalone`**, which VS Code and Claude Code both read. It is named differently on purpose: whenever the plugin registers itself (a *Configure* click, or auto-register) it runs "remove any existing UnityMCP registrations from all scopes", which emptied the old `unityMCP` entry in `.mcp.json` and made VS Code say "server not found". The plugin doesn't know `unity-standalone`, so it can't remove it. Tools are now `mcp__unity-standalone__*` (allowlisted in `.claude/settings.json`). If the plugin re-adds a local `UnityMCP`, remove it: `claude mcp remove UnityMCP -s local`.
4. Keep it alive with a watchdog (installed 2026-09-29): `.\tools\unity-mcp.ps1 -Install` creates the Task Scheduler task `UnityMCPServer`. It runs at login and every minute; if nothing is listening on port 8080 it restarts the standalone server (tested: killed the server, back up within ~10 s of the next check). `-Uninstall` removes it. Every drop and restart is logged: `.\tools\unity-mcp.ps1 -Log` (file `%LOCALAPPDATA%\UnityMCP\watchdog.log`). The log shows how often the server is dying, and whether Unity was running, so the next step is evidence, not guessing.
   - The login-only task installed earlier never ran (no logout since) and was replaced by this one.
   - After a restart Claude Code's old session may still be dead: `/mcp` → reconnect, or start a new chat.
   - Unity's cached plugin (`com.coplaydev.unity-mcp@045e809…`, from Editor.log) differed from the pinned `30d2207` on 2026-09-29: restart Unity once so the pin loads.
5. Versions are pinned so an update can't break the link: server `mcpforunityserver==10.2.0` (in `tools/unity-mcp.ps1`) and the plugin commit `30d2207` (in `Packages/manifest.json`). To upgrade, change both together, then `-Uninstall`/`-Install` and restart the server.
6. When tools vanish anyway: run `.\tools\unity-mcp.ps1` to see which side is down, wait out compiles, then `/mcp` → reconnect `unityMCP`.
