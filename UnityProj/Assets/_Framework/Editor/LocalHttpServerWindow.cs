#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Local HTTP file server for WeChat Mini Game development.
    /// Wraps "npx http-server" to serve StreamingAssets from the
    /// WeChat export directory, with one-click start/stop.
    ///
    /// **Architecture**: MiniGameConfig.ProjectConf.CDN is the Single Source of Truth
    /// for CDN addresses. This tool can both READ and WRITE the CDN value, providing
    /// one-click switching between local dev and remote production environments.
    ///
    /// Saves ~2 min/session of terminal juggling + eliminates manual CDN URL entry.
    ///
    /// Access via: Tools → MiniGame Template → Dev Server
    ///
    /// CHANGELOG:
    /// 2026-05-17  Feature: One-click CDN environment switch (local dev ↔ remote). Auto-prompt on server start. WriteMiniGameConfigCdn support.
    /// 2026-05-17  Refactor: Remove AssetConfig CDN references (CDN now read from DATA_CDN at runtime).
    /// 2026-04-28  Refactor: Dev Server is now a follower, not the source. Reads CDN from configs, verifies consistency. Removed SyncServerUrlToConfigs write-back logic.
    /// 2026-04-27  Fix: IsPortInUse now checks IPAddress.Any (0.0.0.0) in addition to Loopback — prevents silent conflict with WeChat DevTools static server.
    /// 2026-04-27  Fix: wrap npx.cmd with "cmd /c" so the process stays alive for http-server's lifetime (npx.cmd batch exits immediately).
    /// 2026-04-27  Add: manual Node.js directory config with fallback to auto-detect.
    /// 2026-04-27  Fix: probe well-known Node.js install paths (Program Files, nvm, scoop) + inject PATH into child process.
    /// 2026-04-27  Initial version — start/stop http-server, auto-detect root dir, health check.
    /// </summary>
    public class LocalHttpServerWindow : EditorWindow
    {
        // ──────────────── Constants ────────────────
        private const string MENU_PATH = "Tools/MiniGame Template/Dev Server";
        private const int MENU_PRIORITY = 450;
        private const string LOG_PREFIX = "[DevServer]";
        private const string PREF_PORT = "MiniGame_DevServer_Port";
        private const string PREF_ROOT_DIR = "MiniGame_DevServer_RootDir";
        private const string PREF_NODEJS_DIR = "MiniGame_DevServer_NodejsDir";
        private const string PREF_WX_EXPORT_ROOT = "MiniGame_WXExportRoot"; // shared with BuildModeSwitch
        private const string PREF_CDN_REMOTE = "MiniGame_CDN_Remote";

        private const int DEFAULT_PORT = 8001;
        private const string DEFAULT_CDN_REMOTE = "https://cloud1-d4gagblcpd52c5630-1429432433.tcloudbaseapp.com";

        // ──────────────── State ────────────────
        private static Process _serverProcess;
        private static bool _isRunning;
        private static string _serverUrl;
        private static int _lastExitCode;

        [SerializeField] private int _port = DEFAULT_PORT;
        [SerializeField] private string _rootDir = "";
        [SerializeField] private string _nodejsDir = ""; // Manual Node.js install directory (contains npx.cmd)

        private string _statusMessage = "";
        private MessageType _statusType = MessageType.Info;
        private Vector2 _logScrollPos;
        private string _logBuffer = "";
        private bool _autoScrollLog = true;
        private bool _portConflict; // shows "kill occupying process" button
        private string _consistencyCheckResult = ""; // CDN consistency check result for UI display
        [SerializeField] private string _cdnRemote = "";  // Remote production CDN preset

        // ──────────────── Menu Entry ────────────────

        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalHttpServerWindow>("Dev Server");
            window.minSize = new Vector2(380, 320);
        }

        // ──────────────── Lifecycle ────────────────

        private void OnEnable()
        {
            _port = EditorPrefs.GetInt(PREF_PORT, DEFAULT_PORT);
            _rootDir = EditorPrefs.GetString(PREF_ROOT_DIR, "");
            _nodejsDir = EditorPrefs.GetString(PREF_NODEJS_DIR, "");
            _cdnRemote = EditorPrefs.GetString(PREF_CDN_REMOTE, DEFAULT_CDN_REMOTE);

            // Auto-detect root dir from WX export root if not set
            if (string.IsNullOrEmpty(_rootDir))
            {
                string wxRoot = EditorPrefs.GetString(PREF_WX_EXPORT_ROOT, "");
                if (!string.IsNullOrEmpty(wxRoot))
                {
                    string webglDir = Path.Combine(wxRoot, "webgl");
                    if (Directory.Exists(webglDir))
                        _rootDir = webglDir;
                }
            }

            RefreshStatus();
        }

        private void OnDisable()
        {
            EditorPrefs.SetInt(PREF_PORT, _port);
            if (!string.IsNullOrEmpty(_rootDir))
                EditorPrefs.SetString(PREF_ROOT_DIR, _rootDir);
            EditorPrefs.SetString(PREF_NODEJS_DIR, _nodejsDir ?? "");
            EditorPrefs.SetString(PREF_CDN_REMOTE, _cdnRemote ?? DEFAULT_CDN_REMOTE);
        }

        // ──────────────── GUI ────────────────

        private void OnGUI()
        {
            GUILayout.Label("本地开发服务器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "为微信开发者工具提供本地 HTTP 文件服务。\n" +
                "服务 StreamingAssets 目录中的 YooAsset Bundle 文件。",
                MessageType.Info);

            GUILayout.Space(4);

            // ── Config Section ──
            DrawConfigSection();

            GUILayout.Space(4);

            // ── Control Buttons ──
            DrawControlButtons();

            GUILayout.Space(4);

            // ── Status ──
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }

            GUILayout.Space(4);

            // ── Server URL (copyable) ──
            if (_isRunning && !string.IsNullOrEmpty(_serverUrl))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField("服务器地址", _serverUrl);
                if (GUILayout.Button("复制", GUILayout.Width(50)))
                {
                    GUIUtility.systemCopyBuffer = _serverUrl;
                    Debug.Log($"{LOG_PREFIX} URL 已复制到剪贴板: {_serverUrl}");
                }
                EditorGUILayout.EndHorizontal();

                // Show the full YooAsset URL
                string yooUrl = _serverUrl + "/StreamingAssets/yoo";
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField("YooAsset URL", yooUrl);
                if (GUILayout.Button("复制", GUILayout.Width(50)))
                {
                    GUIUtility.systemCopyBuffer = yooUrl;
                    Debug.Log($"{LOG_PREFIX} YooAsset URL 已复制到剪贴板: {yooUrl}");
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(4);

            // ── CDN Consistency Check ──
            DrawConsistencySection();

            GUILayout.Space(4);

            // ── Log Output ──
            DrawLogSection();
        }

        private void DrawConfigSection()
        {
            // Node.js directory (manual override)
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _nodejsDir = EditorGUILayout.TextField("Node.js 目录", _nodejsDir);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PREF_NODEJS_DIR, _nodejsDir);
            }

            if (GUILayout.Button("选择...", GUILayout.Width(55)))
            {
                string initial = !string.IsNullOrEmpty(_nodejsDir) && Directory.Exists(_nodejsDir)
                    ? _nodejsDir
                    : "C:\\Program Files\\nodejs";
                string selected = EditorUtility.OpenFolderPanel(
                    "选择 Node.js 安装目录（包含 npx.cmd 的文件夹）", initial, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    _nodejsDir = selected;
                    EditorPrefs.SetString(PREF_NODEJS_DIR, _nodejsDir);
                }
            }

            if (GUILayout.Button("清除", GUILayout.Width(40)))
            {
                _nodejsDir = "";
                EditorPrefs.SetString(PREF_NODEJS_DIR, "");
            }

            EditorGUILayout.EndHorizontal();

            // Validate Node.js dir
            if (!string.IsNullOrEmpty(_nodejsDir))
            {
                string npxFile = Application.platform == RuntimePlatform.WindowsEditor
                    ? Path.Combine(_nodejsDir, "npx.cmd")
                    : Path.Combine(_nodejsDir, "npx");
                if (!File.Exists(npxFile))
                {
                    EditorGUILayout.HelpBox($"⚠️ 未在该目录找到 npx，请确认路径正确。", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未配置 Node.js 目录，将自动探测常见安装路径。", MessageType.None);
            }

            // Port
            EditorGUI.BeginChangeCheck();
            _port = EditorGUILayout.IntField("端口", _port);
            if (EditorGUI.EndChangeCheck())
            {
                _port = Mathf.Clamp(_port, 1024, 65535);
                EditorPrefs.SetInt(PREF_PORT, _port);
            }

            // Root directory
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _rootDir = EditorGUILayout.TextField("服务器根目录", _rootDir);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PREF_ROOT_DIR, _rootDir);
            }

            if (GUILayout.Button("选择...", GUILayout.Width(55)))
            {
                string selected = EditorUtility.OpenFolderPanel(
                    "选择服务根目录（通常是 minigame/ 目录）",
                    string.IsNullOrEmpty(_rootDir) ? Application.dataPath : _rootDir,
                    "");

                if (!string.IsNullOrEmpty(selected))
                {
                    _rootDir = selected;
                    EditorPrefs.SetString(PREF_ROOT_DIR, _rootDir);
                }
            }

            if (GUILayout.Button("自动", GUILayout.Width(40)))
            {
                AutoDetectRootDir();
            }

            EditorGUILayout.EndHorizontal();

            // Validation
            if (!string.IsNullOrEmpty(_rootDir) && !Directory.Exists(_rootDir))
            {
                EditorGUILayout.HelpBox("⚠️ 指定的目录不存在！请先完成微信小游戏导出。", MessageType.Warning);
            }
        }

        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // Start button
            using (new EditorGUI.DisabledScope(_isRunning || string.IsNullOrEmpty(_rootDir)))
            {
                var startStyle = new GUIStyle(GUI.skin.button);
                if (GUILayout.Button("▶ 启动服务器", GUILayout.Height(28)))
                {
                    StartServer();
                }
            }

            // Stop button
            using (new EditorGUI.DisabledScope(!_isRunning))
            {
                if (GUILayout.Button("■ 停止服务器", GUILayout.Height(28)))
                {
                    StopServer();
                }
            }

            EditorGUILayout.EndHorizontal();

            // Health check
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!_isRunning))
            {
                if (GUILayout.Button("🔍 健康检查"))
                {
                    RunHealthCheck();
                }
            }

            // Refresh status
            if (GUILayout.Button("刷新状态", GUILayout.Width(70)))
            {
                RefreshStatus();
            }
            EditorGUILayout.EndHorizontal();

            // Port conflict resolution
            if (_portConflict || (!_isRunning && IsPortInUse(_port)))
            {
                _portConflict = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"端口 {_port} 被残留进程占用", MessageType.Warning);
                if (GUILayout.Button("强制释放端口", GUILayout.Width(100), GUILayout.Height(38)))
                {
                    KillProcessOnPort(_port);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawConsistencySection()
        {
            EditorGUILayout.LabelField("CDN 环境切换", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "一键切换微信转换面板的 CDN 地址（MiniGameConfig.ProjectConf.CDN）。\n" +
                "切换后需重新导出才能生效。",
                MessageType.None);

            // ── Current CDN Display ──
            string currentCdn = ReadMiniGameConfigCdn();
            string displayCdn = string.IsNullOrEmpty(currentCdn) ? "(空)" : currentCdn;

            // Determine which environment is active
            string localCdn = GetLocalCdnUrl();
            string remoteCdn = !string.IsNullOrEmpty(_cdnRemote) ? _cdnRemote : DEFAULT_CDN_REMOTE;
            string activeEnv = "❓ 自定义";
            if (!string.IsNullOrEmpty(currentCdn))
            {
                if (NormalizeCdn(currentCdn) == NormalizeCdn(localCdn))
                    activeEnv = "🏠 本地调试";
                else if (NormalizeCdn(currentCdn) == NormalizeCdn(remoteCdn))
                    activeEnv = "☁️ 远程真机";
            }
            else
            {
                activeEnv = "❌ 未配置";
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前环境", activeEnv, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("CDN 地址", displayCdn);
            if (GUILayout.Button("复制", GUILayout.Width(50)))
            {
                if (!string.IsNullOrEmpty(currentCdn))
                {
                    GUIUtility.systemCopyBuffer = currentCdn;
                    Debug.Log($"{LOG_PREFIX} CDN 地址已复制: {currentCdn}");
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // ── One-Click Switch Buttons ──
            EditorGUILayout.BeginHorizontal();

            bool isLocal = !string.IsNullOrEmpty(currentCdn) && NormalizeCdn(currentCdn) == NormalizeCdn(localCdn);
            bool isRemote = !string.IsNullOrEmpty(currentCdn) && NormalizeCdn(currentCdn) == NormalizeCdn(remoteCdn);

            using (new EditorGUI.DisabledScope(isLocal))
            {
                if (GUILayout.Button($"🏠 本地调试\n{TruncateUrl(localCdn, 35)}", GUILayout.Height(36)))
                {
                    SwitchCdn(localCdn, "本地调试");
                }
            }

            using (new EditorGUI.DisabledScope(isRemote))
            {
                if (GUILayout.Button($"☁️ 远程真机\n{TruncateUrl(remoteCdn, 35)}", GUILayout.Height(36)))
                {
                    SwitchCdn(remoteCdn, "远程真机");
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // ── Preset Config (foldout) ──
            EditorGUI.BeginChangeCheck();
            _cdnRemote = EditorGUILayout.TextField("远程 CDN 地址", _cdnRemote);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PREF_CDN_REMOTE, _cdnRemote);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("本地 CDN 地址", $"自动 = http://{{本机IP}}:{_port}");
            EditorGUILayout.EndHorizontal();

            // ── Result Message ──
            if (!string.IsNullOrEmpty(_consistencyCheckResult))
            {
                bool isError = _consistencyCheckResult.Contains("❌");
                bool isWarning = _consistencyCheckResult.Contains("⚠️");
                var msgType = isError ? MessageType.Error : (isWarning ? MessageType.Warning : MessageType.Info);
                EditorGUILayout.HelpBox(_consistencyCheckResult, msgType);
            }
        }

        /// <summary>
        /// Get the local CDN URL based on current machine IP and configured port.
        /// </summary>
        private string GetLocalCdnUrl()
        {
            return $"http://{GetLocalIP()}:{_port}";
        }

        /// <summary>
        /// Truncate a URL for display in buttons.
        /// </summary>
        private static string TruncateUrl(string url, int maxLen)
        {
            if (string.IsNullOrEmpty(url)) return "(空)";
            if (url.Length <= maxLen) return url;
            return url.Substring(0, maxLen - 3) + "...";
        }

        /// <summary>
        /// Switch CDN to the given URL and report result.
        /// </summary>
        private void SwitchCdn(string newCdnUrl, string envName)
        {
            if (string.IsNullOrEmpty(newCdnUrl))
            {
                _consistencyCheckResult = "❌ CDN 地址为空，无法切换！";
                return;
            }

            bool success = WriteMiniGameConfigCdn(newCdnUrl);
            if (success)
            {
                _consistencyCheckResult = $"✅ 已切换到 [{envName}]: {newCdnUrl}\n" +
                    "请重新「转换小游戏」导出以生效。";
                AppendLog($"CDN 已切换 → [{envName}] {newCdnUrl}");
                Debug.Log($"{LOG_PREFIX} CDN 已切换 → [{envName}] {newCdnUrl}");
            }
            else
            {
                _consistencyCheckResult = "❌ 切换失败！未找到 MiniGameConfig 资产。";
                Debug.LogError($"{LOG_PREFIX} CDN 切换失败: 未找到 MiniGameConfig");
            }

            Repaint();
        }

        // ──────────────── CDN Config Write ────────────────

        /// <summary>
        /// Write a new CDN value to MiniGameConfig.ProjectConf.CDN.
        /// Uses SerializedObject to support Undo and proper asset marking.
        /// Returns true if successful.
        /// </summary>
        private static bool WriteMiniGameConfigCdn(string newCdnUrl)
        {
            // MiniGameConfig is stored in WX-WASM-SDK-V2 plugin
            string[] guids = AssetDatabase.FindAssets("MiniGameConfig t:ScriptableObject");
            if (guids.Length == 0)
            {
                // Fallback: search by known path
                string knownPath = "Assets/WX-WASM-SDK-V2/Editor/MiniGameConfig.asset";
                if (File.Exists(Path.Combine(Application.dataPath, "../", knownPath)))
                {
                    guids = new[] { AssetDatabase.AssetPathToGUID(knownPath) };
                }
            }

            if (guids.Length == 0) return false;

            foreach (string guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var projConfProp = so.FindProperty("ProjectConf");
                if (projConfProp == null) continue;

                var cdnProp = projConfProp.FindPropertyRelative("CDN");
                if (cdnProp == null) continue;

                // Trim trailing slash for consistency
                string cleanUrl = newCdnUrl.TrimEnd('/');
                cdnProp.stringValue = cleanUrl;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                Debug.Log($"[DevServer] MiniGameConfig.ProjectConf.CDN → {cleanUrl}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Read MiniGameConfig.ProjectConf.CDN from the WeChat SDK config. (Read-only)
        /// </summary>
        private static string ReadMiniGameConfigCdn()
        {
            // MiniGameConfig is stored in WX-WASM-SDK-V2 plugin
            string[] guids = AssetDatabase.FindAssets("MiniGameConfig t:ScriptableObject");
            if (guids.Length == 0)
            {
                // Fallback: search by known path
                string knownPath = "Assets/WX-WASM-SDK-V2/Editor/MiniGameConfig.asset";
                if (File.Exists(Path.Combine(Application.dataPath, "../", knownPath)))
                {
                    guids = new[] { AssetDatabase.AssetPathToGUID(knownPath) };
                }
            }

            if (guids.Length == 0) return null;

            foreach (string guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var projConfProp = so.FindProperty("ProjectConf");
                if (projConfProp == null) continue;

                var cdnProp = projConfProp.FindPropertyRelative("CDN");
                if (cdnProp != null) return cdnProp.stringValue;
            }

            return null;
        }

        /// <summary>
        /// Normalize CDN URL for comparison: trim trailing slashes, lowercase scheme+host.
        /// </summary>
        private static string NormalizeCdn(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            return url.TrimEnd('/').ToLowerInvariant();
        }

        private void DrawLogSection()
        {
            EditorGUILayout.LabelField("服务器日志", EditorStyles.boldLabel);
            _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos, GUILayout.Height(100));
            EditorGUILayout.TextArea(_logBuffer, EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空日志", GUILayout.Width(70)))
            {
                _logBuffer = "";
            }
            GUILayout.FlexibleSpace();
            var statusDot = _isRunning ? "🟢 运行中" : "🔴 已停止";
            GUILayout.Label(statusDot, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ──────────────── Server Control ────────────────

        private void StartServer()
        {
            if (_isRunning)
            {
                SetStatus("服务器已在运行中", MessageType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_rootDir) || !Directory.Exists(_rootDir))
            {
                SetStatus("服务根目录无效或不存在！", MessageType.Error);
                return;
            }

            // Check if port is already in use
            if (IsPortInUse(_port))
            {
                int occupyingPid = FindProcessOnPort(_port);
                string pidInfo = occupyingPid > 0 ? $" (PID: {occupyingPid})" : "";
                SetStatus($"端口 {_port} 已被占用{pidInfo}！请先停止占用进程或更换端口。", MessageType.Error);
                AppendLog($"端口 {_port} 已被占用{pidInfo}，无法启动新服务器。");
                _portConflict = true;
                return;
            }
            _portConflict = false;

            try
            {
                string npxPath = FindNpxPath(_nodejsDir);
                if (string.IsNullOrEmpty(npxPath))
                {
                    SetStatus("找不到 npx 命令！请确保已安装 Node.js 并添加到 PATH。", MessageType.Error);
                    return;
                }

                // IMPORTANT: npx.cmd is a batch wrapper that exits immediately after
                // spawning node.exe as a child process. If we launch npx.cmd directly,
                // our Process.Exited event fires instantly even though http-server is
                // still running. Wrapping with "cmd /c" keeps the cmd process alive
                // until the entire command pipeline (including node) finishes.
                ProcessStartInfo startInfo;
                string npxDir = Path.GetDirectoryName(npxPath);

                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    // Use cmd /c to keep process alive for the lifetime of http-server
                    string cmdArgs = $"/c \"\"{npxPath}\" --yes http-server \"{_rootDir}\" -p {_port} --cors -a 0.0.0.0\"";
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = cmdArgs,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = _rootDir,
                    };
                }
                else
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = npxPath,
                        Arguments = $"--yes http-server \"{_rootDir}\" -p {_port} --cors -a 0.0.0.0",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = _rootDir,
                    };
                }

                // Inject Node.js directory into PATH so npx/node can be found
                if (!string.IsNullOrEmpty(npxDir) && Directory.Exists(npxDir))
                {
                    string envPath = startInfo.EnvironmentVariables["PATH"] ?? "";
                    if (!envPath.Contains(npxDir))
                        startInfo.EnvironmentVariables["PATH"] = npxDir + ";" + envPath;
                }

                _serverProcess = new Process { StartInfo = startInfo };
                _serverProcess.OutputDataReceived += OnServerOutput;
                _serverProcess.ErrorDataReceived += OnServerOutput;
                _serverProcess.EnableRaisingEvents = true;
                _serverProcess.Exited += OnServerExited;

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                _isRunning = true;
                _serverUrl = $"http://{GetLocalIP()}:{_port}";

                SetStatus($"服务器已启动: {_serverUrl}", MessageType.Info);
                AppendLog($"启动服务器: {_serverUrl}");
                AppendLog($"根目录: {_rootDir}");
                AppendLog($"PID: {_serverProcess.Id}");

                Debug.Log($"{LOG_PREFIX} 服务器已启动 → {_serverUrl} (PID: {_serverProcess.Id})");

                // Auto-check: if CDN doesn't match the dev server, offer to switch
                string currentCdn = ReadMiniGameConfigCdn();
                string localCdn = GetLocalCdnUrl();
                if (!string.IsNullOrEmpty(currentCdn) && NormalizeCdn(currentCdn) == NormalizeCdn(localCdn))
                {
                    _consistencyCheckResult = "✅ CDN 已指向本机 Dev Server。";
                }
                else
                {
                    bool autoSwitch = EditorUtility.DisplayDialog(
                        "Dev Server 已启动",
                        $"CDN 当前为:\n{(string.IsNullOrEmpty(currentCdn) ? "(空)" : currentCdn)}\n\n" +
                        $"Dev Server 地址为:\n{localCdn}\n\n" +
                        "是否自动切换 CDN 到 Dev Server？\n" +
                        "（切换后需重新「转换小游戏」导出）",
                        "切换", "不切换");

                    if (autoSwitch)
                    {
                        SwitchCdn(localCdn, "本地调试");
                    }
                    else
                    {
                        _consistencyCheckResult = $"⚠️ CDN ({currentCdn}) 与 Dev Server ({localCdn}) 不一致。\n" +
                            "如需使用本地资源调试，请点击「🏠 本地调试」按钮切换。";
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus($"启动失败: {ex.Message}", MessageType.Error);
                Debug.LogError($"{LOG_PREFIX} 启动失败: {ex}");
                _isRunning = false;
            }
        }

        private void StopServer()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    // Kill the process tree (http-server may spawn child processes)
                    KillProcessTree(_serverProcess.Id);
                    AppendLog("服务器已停止");
                    Debug.Log($"{LOG_PREFIX} 服务器已停止");
                }
                catch (Exception ex)
                {
                    AppendLog($"停止时出错: {ex.Message}");
                    Debug.LogWarning($"{LOG_PREFIX} 停止时出错: {ex.Message}");
                }
            }

            _serverProcess = null;
            _isRunning = false;
            _serverUrl = "";
            SetStatus("服务器已停止", MessageType.Info);
        }

        private void RunHealthCheck()
        {
            if (!_isRunning)
            {
                SetStatus("服务器未运行", MessageType.Warning);
                return;
            }

            string testUrl = $"http://127.0.0.1:{_port}/StreamingAssets/yoo/DefaultPackage/DefaultPackage.version";
            AppendLog($"健康检查: {testUrl}");

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(testUrl);
                request.Timeout = 3000;
                request.Method = "GET";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    string result = $"✅ HTTP {(int)response.StatusCode} — {response.ContentLength} bytes";
                    AppendLog(result);
                    SetStatus($"健康检查通过: HTTP {(int)response.StatusCode}", MessageType.Info);
                    Debug.Log($"{LOG_PREFIX} {result}");
                }
            }
            catch (WebException webEx)
            {
                if (webEx.Response is HttpWebResponse errResp)
                {
                    string result = $"⚠️ HTTP {(int)errResp.StatusCode}";
                    AppendLog(result);
                    SetStatus($"健康检查: {result}", MessageType.Warning);
                }
                else
                {
                    string result = $"❌ 连接失败: {webEx.Message}";
                    AppendLog(result);
                    SetStatus(result, MessageType.Error);

                    // Server might have died
                    if (_serverProcess == null || _serverProcess.HasExited)
                    {
                        _isRunning = false;
                        _serverUrl = "";
                        AppendLog("服务器进程已退出");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ 检查异常: {ex.Message}");
                SetStatus($"健康检查失败: {ex.Message}", MessageType.Error);
            }
        }

        // ──────────────── Helpers ────────────────

        private void RefreshStatus()
        {
            // Check if process is still alive
            if (_serverProcess != null && _serverProcess.HasExited)
            {
                _serverProcess = null;
                _isRunning = false;
                _serverUrl = "";
            }

            if (_isRunning)
            {
                SetStatus($"服务器运行中: {_serverUrl}", MessageType.Info);
            }
            else if (IsPortInUse(_port))
            {
                SetStatus($"端口 {_port} 被外部进程占用（可能是上次启动的服务器）", MessageType.Warning);
            }
            else
            {
                SetStatus("服务器未运行", MessageType.Info);
            }
        }

        private void AutoDetectRootDir()
        {
            // Try WX export root from EditorPrefs (shared with BuildModeSwitch)
            string wxRoot = EditorPrefs.GetString(PREF_WX_EXPORT_ROOT, "");
            if (!string.IsNullOrEmpty(wxRoot))
            {
                string minigameDir = Path.Combine(wxRoot, "webgl");
                if (Directory.Exists(minigameDir))
                {
                    _rootDir = minigameDir;
                    EditorPrefs.SetString(PREF_ROOT_DIR, _rootDir);
                    SetStatus($"自动检测到: {_rootDir}", MessageType.Info);
                    return;
                }
            }

            // Fallback: try common relative paths
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            string[] candidates =
            {
                Path.Combine(projectRoot, "output", "webgl"),
                Path.Combine(projectRoot, "Build", "webgl"),
            };

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    _rootDir = candidate;
                    EditorPrefs.SetString(PREF_ROOT_DIR, _rootDir);
                    SetStatus($"自动检测到: {_rootDir}", MessageType.Info);
                    return;
                }
            }

            SetStatus("未能自动检测到 minigame 目录，请手动选择", MessageType.Warning);
        }

        /// <summary>
        /// Locate npx executable.
        /// Priority: 1) user-configured nodejsDir  2) PATH  3) well-known install directories.
        /// </summary>
        private static string FindNpxPath(string manualNodejsDir)
        {
            // 1. Build a list of candidate file names / absolute paths
            var candidates = new System.Collections.Generic.List<string>();

            // ── Priority 1: user-configured directory ──
            if (!string.IsNullOrEmpty(manualNodejsDir) && Directory.Exists(manualNodejsDir))
            {
                string npxInManual = Application.platform == RuntimePlatform.WindowsEditor
                    ? Path.Combine(manualNodejsDir, "npx.cmd")
                    : Path.Combine(manualNodejsDir, "npx");
                if (File.Exists(npxInManual))
                    candidates.Add(npxInManual);
            }

            // ── Priority 2 & 3: PATH then well-known locations ──
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Try bare command (works when PATH is correct)
                candidates.Add("npx.cmd");
                candidates.Add("npx");

                // Then probe common Windows install locations
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                string[] knownDirs =
                {
                    Path.Combine(programFiles, "nodejs"),
                    Path.Combine(programFiles.Replace(" (x86)", ""), "nodejs"), // 64-bit path
                    Path.Combine(localAppData, "Programs", "nodejs"),           // nvm-windows style
                    Path.Combine(userProfile, ".nvm", "current"),               // nvm-windows symlink
                    Path.Combine(userProfile, "scoop", "shims"),                // scoop
                };

                foreach (string dir in knownDirs)
                {
                    string abs = Path.Combine(dir, "npx.cmd");
                    if (File.Exists(abs))
                        candidates.Add(abs);
                }
            }
            else
            {
                candidates.Add("npx");
                // macOS / Linux common paths
                string[] unixDirs = { "/usr/local/bin", "/opt/homebrew/bin", "/usr/bin" };
                foreach (string dir in unixDirs)
                {
                    string abs = Path.Combine(dir, "npx");
                    if (File.Exists(abs))
                        candidates.Add(abs);
                }
            }

            // 2. Try each candidate
            foreach (string candidate in candidates)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = candidate,
                        Arguments = "--version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    };
                    // Ensure the candidate's directory is in PATH for child process
                    string dir = Path.GetDirectoryName(candidate);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        string currentPath = psi.EnvironmentVariables["PATH"] ?? "";
                        if (!currentPath.Contains(dir))
                            psi.EnvironmentVariables["PATH"] = dir + ";" + currentPath;
                    }

                    using (var p = Process.Start(psi))
                    {
                        p.WaitForExit(3000);
                        if (p.ExitCode == 0)
                            return candidate;
                    }
                }
                catch
                {
                    // Not found or not executable, try next
                }
            }

            return null;
        }

        private static string GetLocalIP()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    // Connect to a public IP to determine the local network interface
                    socket.Connect("8.8.8.8", 65530);
                    var endpoint = socket.LocalEndPoint as IPEndPoint;
                    return endpoint?.Address.ToString() ?? "127.0.0.1";
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// Check if a port is in use by attempting to bind on BOTH 0.0.0.0 and 127.0.0.1.
        /// We must check IPAddress.Any (0.0.0.0) because http-server binds on 0.0.0.0,
        /// and another process (e.g. WeChat DevTools static server) may also bind 0.0.0.0
        /// while leaving 127.0.0.1 apparently free — causing a false negative.
        /// </summary>
        private static bool IsPortInUse(int port)
        {
            // Check 0.0.0.0 first (catches processes like wechatdevtools binding on Any)
            if (IsPortInUseOn(IPAddress.Any, port))
                return true;
            // Also check loopback in case something binds only on 127.0.0.1
            if (IsPortInUseOn(IPAddress.Loopback, port))
                return true;
            return false;
        }

        private static bool IsPortInUseOn(IPAddress address, int port)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(address, port);
                listener.Start();
                listener.Stop();
                return false;
            }
            catch (SocketException)
            {
                return true;
            }
            finally
            {
                try { listener?.Stop(); } catch { /* already stopped */ }
            }
        }

        private static void KillProcessTree(int pid)
        {
            // On Windows, use taskkill /T to kill process tree
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/PID {pid} /T /F",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit(5000);
                }
            }
            else
            {
                // Unix: kill the process group
                try
                {
                    Process.GetProcessById(pid).Kill();
                }
                catch
                {
                    // Already exited
                }
            }
        }

        /// <summary>
        /// Find the PID of the process listening on the given port.
        /// Returns -1 if not found.
        /// </summary>
        private static int FindProcessOnPort(int port)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor) return -1;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                };
                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(5000);

                    string searchPattern = $":{port} ";
                    foreach (string line in output.Split('\n'))
                    {
                        if (line.Contains(searchPattern) && line.Contains("LISTENING"))
                        {
                            string trimmed = line.Trim();
                            string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5 && int.TryParse(parts[parts.Length - 1], out int pid))
                                return pid;
                        }
                    }
                }
            }
            catch { /* ignore */ }

            return -1;
        }

        /// <summary>
        /// Kill whatever process is listening on the given port and refresh UI.
        /// </summary>
        private void KillProcessOnPort(int port)
        {
            int pid = FindProcessOnPort(port);
            if (pid <= 0)
            {
                AppendLog($"未找到占用端口 {port} 的进程");
                _portConflict = false;
                RefreshStatus();
                return;
            }

            try
            {
                string procName = "unknown";
                try { procName = Process.GetProcessById(pid).ProcessName; } catch { }

                AppendLog($"正在终止进程 {procName} (PID: {pid})...");
                KillProcessTree(pid);

                // Wait a moment for port to be freed
                System.Threading.Thread.Sleep(500);

                if (IsPortInUse(port))
                {
                    AppendLog($"端口 {port} 仍被占用，可能需要等待几秒");
                    SetStatus($"端口 {port} 仍被占用，请稍后重试", MessageType.Warning);
                }
                else
                {
                    AppendLog($"✅ 端口 {port} 已释放");
                    SetStatus($"端口 {port} 已释放，可以启动服务器了", MessageType.Info);
                    _portConflict = false;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"终止进程失败: {ex.Message}");
                SetStatus($"终止进程失败: {ex.Message}", MessageType.Error);
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }

        private void AppendLog(string line)
        {
            _logBuffer += $"[{DateTime.Now:HH:mm:ss}] {line}\n";

            // Keep log buffer manageable
            if (_logBuffer.Length > 8000)
            {
                int cutIndex = _logBuffer.IndexOf('\n', _logBuffer.Length - 6000);
                if (cutIndex > 0)
                    _logBuffer = "...(已截断)...\n" + _logBuffer.Substring(cutIndex + 1);
            }

            Repaint();
        }

        // ──────────────── Async Callbacks ────────────────

        private void OnServerOutput(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            // Marshal back to main thread
            EditorApplication.delayCall += () =>
            {
                AppendLog(e.Data);
            };
        }

        private void OnServerExited(object sender, EventArgs e)
        {
            // Capture exit code on the callback thread (Process may be disposed by the time delayCall runs)
            int exitCode = -1;
            try
            {
                if (sender is Process proc)
                    exitCode = proc.ExitCode;
            }
            catch { /* process already disposed */ }

            EditorApplication.delayCall += () =>
            {
                _lastExitCode = exitCode;
                _isRunning = false;
                _serverUrl = "";

                if (exitCode == 0)
                {
                    AppendLog("服务器进程正常退出");
                    SetStatus("服务器已停止", MessageType.Info);
                }
                else
                {
                    AppendLog($"⚠️ 服务器进程异常退出 (exit code: {exitCode})");
                    if (IsPortInUse(_port))
                    {
                        AppendLog($"端口 {_port} 仍被占用——可能是 EADDRINUSE（端口冲突）");
                        SetStatus($"启动失败：端口 {_port} 被占用！请点击\"强制释放端口\"后重试。", MessageType.Error);
                        _portConflict = true;
                    }
                    else
                    {
                        SetStatus($"服务器异常退出 (exit code: {exitCode})，请查看日志", MessageType.Error);
                    }
                }

                Repaint();
            };
        }

        // ──────────────── Domain Reload Safety ────────────────

        /// <summary>
        /// When Unity domain reloads (recompile), we lose the Process reference.
        /// The server continues running in the background — this is intentional,
        /// as the user may be iterating on code while testing in WeChat DevTools.
        /// On window re-open, "Refresh Status" + port check will detect it.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            // Process reference is lost after domain reload
            _serverProcess = null;
            _isRunning = false;
            _serverUrl = "";
        }
    }
}
#endif
