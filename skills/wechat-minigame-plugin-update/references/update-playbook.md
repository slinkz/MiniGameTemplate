# WeChat Mini Game Unity SDK Update Playbook

## Official Version Discovery

Use the official endpoint every time; plugin versions change over time.

```powershell
Invoke-WebRequest `
  -Uri "https://game.weixin.qq.com/cgi-bin/gamewxagwasmsplitwap/getunityplugininfo" `
  -UseBasicParsing |
  Select-Object -ExpandProperty Content
```

Expected shape:

```json
{
  "errcode": 0,
  "errmsg": "ok",
  "data": {
    "info": {
      "version": "202606220647",
      "url": "https://res.wx.qq.com/.../minigame.202606220647.unitypackage#0.1.34"
    }
  }
}
```

Compare against:

```powershell
Get-Content UnityProj/Packages/com.qq.weixin.minigame/Editor/WXPluginVersion.cs
Get-Content UnityProj/Packages/com.qq.weixin.minigame/package.json
```

## Download And Reconstruct UnityPackage

```powershell
$root = "$env:TEMP\wx-minigame-plugin-update"
$pkg = "$root\minigame.latest.unitypackage"
$extract = "$root\extracted"
$recon = "$root\reconstructed"

New-Item -ItemType Directory -Force -Path $root | Out-Null
Invoke-WebRequest -Uri "<url before #>" -OutFile $pkg -UseBasicParsing

Remove-Item -Recurse -Force $extract,$recon -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $extract,$recon | Out-Null
tar -xzf $pkg -C $extract

Get-ChildItem $extract -Directory | ForEach-Object {
  $path = (Get-Content (Join-Path $_.FullName "pathname") -Raw).Trim()
  $target = Join-Path $recon $path
  New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
  Copy-Item -LiteralPath (Join-Path $_.FullName "asset") -Destination $target -Force
  if (Test-Path (Join-Path $_.FullName "asset.meta")) {
    Copy-Item -LiteralPath (Join-Path $_.FullName "asset.meta") -Destination ($target + ".meta") -Force
  }
}
```

Inspect before copying:

```powershell
Get-ChildItem "$recon\Assets\WX-WASM-SDK-V2" -Force
Get-ChildItem "$recon\Assets\WX-WASM-SDK-V2\Editor" -Directory
Get-ChildItem "$recon\Assets\WX-WASM-SDK-V2\Runtime" -Directory
```

## Project-Specific Copy Rules

This project uses an embedded package:

```json
"com.qq.weixin.minigame": "file:Packages/com.qq.weixin.minigame"
```

Copy official `Assets/WX-WASM-SDK-V2` content into the embedded package path:

```powershell
$src = (Resolve-Path "$recon\Assets\WX-WASM-SDK-V2").Path
$dst = (Resolve-Path "UnityProj\Packages\com.qq.weixin.minigame").Path
Copy-Item -Recurse -Force -LiteralPath $dst -Destination "$root\backup-com.qq.weixin.minigame"
robocopy $src $dst /E /R:0 /W:0 /XF package.json /NFL /NDL /NJH /NJS /NP
if ($LASTEXITCODE -gt 7) { throw "robocopy failed: $LASTEXITCODE" }
```

Then manually set `UnityProj/Packages/com.qq.weixin.minigame/package.json` to the version after `#` in the official URL.

If `UnityProj/Assets/WebGLTemplates` exists, it can be synced from the UnityPackage `Assets/WebGLTemplates`:

```powershell
$tplSrc = (Resolve-Path "$recon\Assets\WebGLTemplates").Path
$tplDst = Resolve-Path "UnityProj\Assets\WebGLTemplates" -ErrorAction SilentlyContinue
if ($tplDst) {
  robocopy $tplSrc $tplDst.Path /E /R:0 /W:0 /NFL /NDL /NJH /NJS /NP
  if ($LASTEXITCODE -gt 7) { throw "template robocopy failed: $LASTEXITCODE" }
}
```

Do not sync a full second Runtime into `UnityProj/Assets/WX-WASM-SDK-V2/Runtime` for this repo. That creates duplicate `WxWasmSDKRuntime.asmdef` and precompiled assemblies.

## Unity DLL Locks

Unity may lock plugin DLLs during import or while the SDK is loaded. Prefer:

1. Save/focus Unity.
2. Close Unity gracefully.
3. Retry copy with `/R:0 /W:0`.
4. If the window does not close and the user has accepted it, stop the Unity process and restart after copying.

Typical lock errors:

- `ERROR 32 ... file is being used by another process`
- `ERROR 1224 ... user-mapped section open`

## Safe Mode Recovery

If Unity opens `Enter Safe Mode?`, MCP may not be available. Read:

```powershell
$log = "$env:LOCALAPPDATA\Unity\Editor\Editor.log"
Select-String -Path $log -Pattern "error CS|Multiple precompiled assemblies|Assembly with name|Compilation failed" -Context 0,2 |
  Select-Object -Last 80
```

For duplicate WeChat runtime errors, remove the accidental duplicate runtime under `Assets/WX-WASM-SDK-V2/Runtime`:

```powershell
git restore -- UnityProj/Assets/WX-WASM-SDK-V2/Runtime
git clean -fd -- UnityProj/Assets/WX-WASM-SDK-V2/Runtime
```

Use this only when the duplicate files were created by the current SDK update attempt. Never remove unrelated user changes.

## MCP Verification

After Unity reopens normally:

```text
unity_list_instances(refresh=true)
unity_editor_state(port=7890)
unity_get_compilation_errors(port=7890, severity=all, count=100)
```

Success criteria:

- `isCompiling: false`
- `count: 0`
- `entries: []`

Do not rely on `dotnet build` as a substitute in this repo; it can fail on unrelated WeChat/YooAsset project references.
