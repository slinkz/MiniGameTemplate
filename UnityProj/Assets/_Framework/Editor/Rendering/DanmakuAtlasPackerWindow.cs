using System.Collections.Generic;
using System.IO;
using MiniGameTemplate.Rendering;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.Editor.Rendering
{
    /// <summary>
    /// Atlas 打包工具——将多张独立贴图打包成一张 Atlas + 生成 AtlasMappingSO。
    /// 菜单入口：Tools/弹幕系统/Atlas 打包工具
    /// </summary>
    public class DanmakuAtlasPackerWindow : EditorWindow
    {
        private enum AtlasDomain { Bullet, VFX }

        [MenuItem("Tools/弹幕系统/Atlas 打包工具")]
        public static void ShowWindow()
        {
            GetWindow<DanmakuAtlasPackerWindow>("Atlas 打包工具");
        }

        private AtlasDomain _domain = AtlasDomain.Bullet;
        private int _maxSizeIndex = 2; // 0=512, 1=1024, 2=2048, 3=4096
        private static readonly int[] MaxSizeOptions = { 512, 1024, 2048, 4096 };
        private static readonly string[] MaxSizeLabels = { "512", "1024", "2048", "4096" };
        private int _padding = 2;
        private List<Texture2D> _sourceTextures = new();
        private string _outputDir = "Assets/_Game/Atlas/";
        private AtlasMappingSO _existingMapping;
        private Vector2 _scrollPos;

        // 预览
        private Texture2D _previewAtlas;
        private string _lastReport;

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Atlas 打包工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ADR-017：Atlas 为可逆派生产物。打包后可随时删除 AtlasMappingSO 回退到独立贴图模式。",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // 域选择
            _domain = (AtlasDomain)EditorGUILayout.EnumPopup("域（不允许混打）", _domain);

            // Atlas 最大尺寸
            _maxSizeIndex = EditorGUILayout.Popup("Atlas 最大尺寸", _maxSizeIndex, MaxSizeLabels);

            // Padding
            _padding = Mathf.Max(0, EditorGUILayout.IntField("Padding (px)", _padding));

            // 输出目录
            EditorGUILayout.BeginHorizontal();
            _outputDir = EditorGUILayout.TextField("输出目录", _outputDir);
            if (GUILayout.Button("选择...", GUILayout.Width(60)))
            {
                string folder = EditorUtility.OpenFolderPanel("选择输出目录", "Assets", "");
                if (!string.IsNullOrEmpty(folder))
                {
                    if (folder.StartsWith(Application.dataPath))
                        _outputDir = "Assets" + folder.Substring(Application.dataPath.Length);
                    else
                        EditorUtility.DisplayDialog("错误", "请选择项目 Assets 下的目录", "确定");
                }
            }
            EditorGUILayout.EndHorizontal();

            // 已有映射资产（重新打包）
            _existingMapping = (AtlasMappingSO)EditorGUILayout.ObjectField(
                "已有 AtlasMappingSO（重新打包）", _existingMapping, typeof(AtlasMappingSO), false);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("源贴图列表", EditorStyles.boldLabel);

            // 添加按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("拖入或点击添加贴图"))
            {
                int id = EditorGUIUtility.GetControlID(FocusType.Passive);
                EditorGUIUtility.ShowObjectPicker<Texture2D>(null, false, "", id);
            }
            if (GUILayout.Button("从文件夹添加", GUILayout.Width(100)))
            {
                string folder = EditorUtility.OpenFolderPanel("选择贴图文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(folder) && folder.StartsWith(Application.dataPath))
                {
                    string assetsPath = "Assets" + folder.Substring(Application.dataPath.Length);
                    string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetsPath });
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (tex != null && !_sourceTextures.Contains(tex))
                            _sourceTextures.Add(tex);
                    }
                }
            }
            if (GUILayout.Button("清空", GUILayout.Width(50)))
            {
                _sourceTextures.Clear();
            }
            EditorGUILayout.EndHorizontal();

            // 处理 ObjectPicker 结果
            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                var picked = EditorGUIUtility.GetObjectPickerObject() as Texture2D;
                if (picked != null && !_sourceTextures.Contains(picked))
                    _sourceTextures.Add(picked);
            }

            // 贴图列表（带拖拽支持）
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(200));
            int removeIndex = -1;
            for (int i = 0; i < _sourceTextures.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _sourceTextures[i] = (Texture2D)EditorGUILayout.ObjectField(
                    _sourceTextures[i], typeof(Texture2D), false);
                if (GUILayout.Button("×", GUILayout.Width(22)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeIndex >= 0)
                _sourceTextures.RemoveAt(removeIndex);
            EditorGUILayout.EndScrollView();

            // 拖拽区域
            var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "拖拽贴图到此处添加");
            HandleDragDrop(dropRect);

            EditorGUILayout.Space(4);

            // 打包按钮
            using (new EditorGUI.DisabledScope(_sourceTextures.Count == 0))
            {
                if (GUILayout.Button("🔨 打包 Atlas", GUILayout.Height(30)))
                {
                    PackAtlas();
                }
            }

            // 报告
            if (!string.IsNullOrEmpty(_lastReport))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(_lastReport, MessageType.None);
            }

            // 预览
            if (_previewAtlas != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);
                float maxW = position.width - 20;
                float ratio = (float)_previewAtlas.height / _previewAtlas.width;
                float w = Mathf.Min(maxW, _previewAtlas.width);
                float h = w * ratio;
                var previewRect = GUILayoutUtility.GetRect(w, h);
                GUI.DrawTexture(previewRect, _previewAtlas, ScaleMode.ScaleToFit);
            }
        }

        private void HandleDragDrop(Rect dropArea)
        {
            var evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is Texture2D tex && !_sourceTextures.Contains(tex))
                            _sourceTextures.Add(tex);
                    }
                }
                evt.Use();
            }
        }

        private void PackAtlas()
        {
            if (_sourceTextures.Count == 0) return;

            int maxSize = MaxSizeOptions[_maxSizeIndex];

            // 确保输出目录存在
            string fullOutputDir = Path.Combine(Application.dataPath, _outputDir.Replace("Assets/", "").Replace("Assets\\", ""));
            if (!Directory.Exists(fullOutputDir))
                Directory.CreateDirectory(fullOutputDir);

            // 确保所有源贴图可读
            var originalReadable = new Dictionary<string, bool>();
            var texArray = new Texture2D[_sourceTextures.Count];
            for (int i = 0; i < _sourceTextures.Count; i++)
            {
                var tex = _sourceTextures[i];
                texArray[i] = tex;
                string path = AssetDatabase.GetAssetPath(tex);
                if (!string.IsNullOrEmpty(path))
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null && !importer.isReadable)
                    {
                        originalReadable[path] = false;
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }
                }
            }

            try
            {
                // 打包
                var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Rect[] rects = atlas.PackTextures(texArray, _padding, maxSize);

                if (rects == null || rects.Length != texArray.Length)
                {
                    EditorUtility.DisplayDialog("打包失败", "贴图打包失败，可能超过最大尺寸限制。", "确定");
                    Object.DestroyImmediate(atlas);
                    return;
                }

                // 保存 Atlas PNG
                string domainName = _domain.ToString();
                string atlasFileName = $"{domainName}Atlas_{atlas.width}x{atlas.height}.png";
                string atlasPath = Path.Combine(_outputDir, atlasFileName);
                byte[] pngData = atlas.EncodeToPNG();
                string fullAtlasPath = Path.Combine(Application.dataPath, atlasPath.Replace("Assets/", "").Replace("Assets\\", ""));
                File.WriteAllBytes(fullAtlasPath, pngData);
                AssetDatabase.ImportAsset(atlasPath);

                // 设置 Atlas 导入参数
                var atlasImporter = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
                if (atlasImporter != null)
                {
                    atlasImporter.textureType = TextureImporterType.Default;
                    atlasImporter.npotScale = TextureImporterNPOTScale.None;
                    atlasImporter.mipmapEnabled = false;
                    atlasImporter.isReadable = false;
                    atlasImporter.filterMode = FilterMode.Bilinear;
                    atlasImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    atlasImporter.SaveAndReimport();
                }

                var atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);

                // 构建 AtlasMappingSO
                AtlasMappingSO mapping = _existingMapping;
                string mappingPath;
                if (mapping != null)
                {
                    mappingPath = AssetDatabase.GetAssetPath(mapping);
                }
                else
                {
                    mapping = CreateInstance<AtlasMappingSO>();
                    mappingPath = Path.Combine(_outputDir, $"{domainName}AtlasMapping.asset");
                    // 确保不覆盖已有文件
                    mappingPath = AssetDatabase.GenerateUniqueAssetPath(mappingPath);
                    AssetDatabase.CreateAsset(mapping, mappingPath);
                }

                Undo.RecordObject(mapping, "Pack Atlas");
                mapping.AtlasTexture = atlasTexture;
                mapping.Padding = _padding;

                var entries = new AtlasEntry[texArray.Length];
                for (int i = 0; i < texArray.Length; i++)
                {
                    string srcPath = AssetDatabase.GetAssetPath(texArray[i]);
                    entries[i] = new AtlasEntry
                    {
                        SourceTexture = texArray[i],
                        SourceGUID = !string.IsNullOrEmpty(srcPath)
                            ? AssetDatabase.AssetPathToGUID(srcPath) : "",
                        UVRect = rects[i],
                        PixelRect = new RectInt(
                            Mathf.RoundToInt(rects[i].x * atlas.width),
                            Mathf.RoundToInt(rects[i].y * atlas.height),
                            Mathf.RoundToInt(rects[i].width * atlas.width),
                            Mathf.RoundToInt(rects[i].height * atlas.height)),
                    };
                }
                mapping.Entries = entries;
                EditorUtility.SetDirty(mapping);
                AssetDatabase.SaveAssets();

                // 计算利用率
                long totalSourcePixels = 0;
                for (int i = 0; i < texArray.Length; i++)
                    totalSourcePixels += (long)texArray[i].width * texArray[i].height;
                long atlasPixels = (long)atlas.width * atlas.height;
                float utilization = atlasPixels > 0 ? (float)totalSourcePixels / atlasPixels * 100f : 0f;

                _previewAtlas = atlasTexture;
                _existingMapping = mapping;
                _lastReport = $"✅ 打包完成\n" +
                    $"域: {domainName}\n" +
                    $"源贴图: {texArray.Length} 张\n" +
                    $"Atlas 尺寸: {atlas.width}×{atlas.height}\n" +
                    $"利用率: {utilization:F1}%\n" +
                    $"Atlas: {atlasPath}\n" +
                    $"Mapping: {mappingPath}";

                Debug.Log($"[AtlasPacker] {_lastReport}");

                Object.DestroyImmediate(atlas);
            }
            finally
            {
                // 恢复源贴图的 isReadable 设置
                foreach (var kvp in originalReadable)
                {
                    var importer = AssetImporter.GetAtPath(kvp.Key) as TextureImporter;
                    if (importer != null)
                    {
                        importer.isReadable = kvp.Value;
                        importer.SaveAndReimport();
                    }
                }
            }
        }
    }
}
