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
        /// <summary>
        /// 打包期间为 true，告诉 TextureImportEnforcer 跳过 isReadable 强制还原。
        /// PackTextures 需要源贴图可读，打包完成后会自行恢复。
        /// </summary>
        internal static bool IsPackingInProgress { get; private set; }

        private enum AtlasDomain { Bullet, VFX }

        [MenuItem("Tools/MiniGame Template/Danmaku/Atlas Packer")]
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
        private Vector2 _windowScrollPos;
        private bool _previewFoldout = true;
        private const float PREVIEW_MAX_HEIGHT = 300f;

        private void OnGUI()
        {
            // 整个窗口包在一个 ScrollView 中，避免内容溢出时挤压上方区域
            _windowScrollPos = EditorGUILayout.BeginScrollView(_windowScrollPos);

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
            EditorGUI.BeginChangeCheck();
            _existingMapping = (AtlasMappingSO)EditorGUILayout.ObjectField(
                "已有 AtlasMappingSO（重新打包）", _existingMapping, typeof(AtlasMappingSO), false);
            if (EditorGUI.EndChangeCheck() && _existingMapping != null)
            {
                RestoreFromMapping(_existingMapping);
            }

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

            // 贴图列表——高度自适应：少于 10 张时完全展开，超过时限高滚动
            float listHeight = _sourceTextures.Count * EditorGUIUtility.singleLineHeight * 1.2f;
            float maxListHeight = EditorGUIUtility.singleLineHeight * 12f; // ≈12 行
            bool needsScroll = listHeight > maxListHeight;

            if (needsScroll)
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(maxListHeight));

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

            if (needsScroll)
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

            // 预览（可折叠 + 限制最大高度）
            if (_previewAtlas != null)
            {
                EditorGUILayout.Space(4);
                _previewFoldout = EditorGUILayout.Foldout(_previewFoldout, "预览", true, EditorStyles.foldoutHeader);
                if (_previewFoldout)
                {
                    float maxW = position.width - 40;
                    float ratio = (float)_previewAtlas.height / _previewAtlas.width;
                    float w = Mathf.Min(maxW, _previewAtlas.width);
                    float h = Mathf.Min(w * ratio, PREVIEW_MAX_HEIGHT);
                    w = h / ratio; // 保持宽高比
                    var previewRect = GUILayoutUtility.GetRect(w, h);
                    GUI.DrawTexture(previewRect, _previewAtlas, ScaleMode.ScaleToFit);
                }
            }

            EditorGUILayout.EndScrollView();
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

        /// <summary>
        /// 从已有 AtlasMappingSO 还原工具状态：源贴图列表、padding、输出目录、预览。
        /// </summary>
        private void RestoreFromMapping(AtlasMappingSO mapping)
        {
            // 还原源贴图列表
            _sourceTextures.Clear();
            if (mapping.Entries != null)
            {
                foreach (var entry in mapping.Entries)
                {
                    if (entry.SourceTexture != null)
                        _sourceTextures.Add(entry.SourceTexture);
                }
            }

            // 还原 padding
            _padding = mapping.Padding;

            // 还原输出目录——从 mapping 资产路径推断
            string mappingPath = AssetDatabase.GetAssetPath(mapping);
            if (!string.IsNullOrEmpty(mappingPath))
            {
                _outputDir = Path.GetDirectoryName(mappingPath).Replace('\\', '/') + "/";
            }

            // 还原预览
            _previewAtlas = mapping.AtlasTexture;

            // 还原域——从资产名或路径推断
            string mappingName = mapping.name.ToLowerInvariant();
            if (mappingName.Contains("vfx"))
                _domain = AtlasDomain.VFX;
            else
                _domain = AtlasDomain.Bullet;

            // 计算利用率
            string sizeInfo = "";
            string utilInfo = "";
            if (mapping.AtlasTexture != null)
            {
                int aw = mapping.AtlasTexture.width;
                int ah = mapping.AtlasTexture.height;
                sizeInfo = $"Atlas 尺寸: {aw}×{ah}\n";

                long totalSrc = 0;
                foreach (var entry in mapping.Entries)
                {
                    if (entry.SourceTexture != null)
                        totalSrc += (long)entry.SourceTexture.width * entry.SourceTexture.height;
                }
                long atlasPixels = (long)aw * ah;
                float util = atlasPixels > 0 ? (float)totalSrc / atlasPixels * 100f : 0f;
                utilInfo = $"利用率: {util:F1}%\n";
            }

            string mappingAssetPath = AssetDatabase.GetAssetPath(mapping);
            _lastReport = $"📂 已从 {mapping.name} 还原\n" +
                $"源贴图: {_sourceTextures.Count} 张\n" +
                sizeInfo + utilInfo +
                $"Mapping: {mappingAssetPath}";
        }

        private void PackAtlas()
        {
            if (_sourceTextures.Count == 0) return;

            int maxSize = MaxSizeOptions[_maxSizeIndex];

            // 确保输出目录存在
            string fullOutputDir = Path.Combine(Application.dataPath, _outputDir.Replace("Assets/", "").Replace("Assets\\", ""));
            if (!Directory.Exists(fullOutputDir))
                Directory.CreateDirectory(fullOutputDir);

            // 设置打包标志——通知 TextureImportEnforcer 跳过 isReadable 强制
            IsPackingInProgress = true;

            // 确保所有源贴图可读——必须先全部设置 isReadable，再统一 reimport，
            // 最后重新加载引用，否则像素数据拿到的仍是不可读的旧对象。
            var originalReadable = new Dictionary<string, bool>();
            var texPaths = new string[_sourceTextures.Count];
            for (int i = 0; i < _sourceTextures.Count; i++)
            {
                string path = AssetDatabase.GetAssetPath(_sourceTextures[i]);
                texPaths[i] = path;
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

            // 重新导入后必须重新加载贴图对象，旧引用的像素缓冲可能已失效
            var texArray = new Texture2D[_sourceTextures.Count];
            for (int i = 0; i < _sourceTextures.Count; i++)
            {
                if (!string.IsNullOrEmpty(texPaths[i]))
                    texArray[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(texPaths[i]);
                else
                    texArray[i] = _sourceTextures[i];
            }

            try
            {
                // 检测是否所有子图同尺寸——同尺寸时用网格排列（支持序列帧），
                // 混合尺寸时用 PackTextures 自动布局 + Y 翻转。
                bool allSameSize = true;
                int firstW = texArray[0].width, firstH = texArray[0].height;
                for (int i = 1; i < texArray.Length; i++)
                {
                    if (texArray[i].width != firstW || texArray[i].height != firstH)
                    {
                        allSameSize = false;
                        break;
                    }
                }

                Texture2D atlas;
                Rect[] rects;

                if (allSameSize)
                {
                    // ========== 同尺寸：手动网格排列，从左上角开始 ==========
                    PackResult result = PackGrid(texArray, firstW, firstH, _padding, maxSize);
                    if (result == null) return; // PackGrid 内部已显示错误对话框
                    atlas = result.Atlas;
                    rects = result.UVRects;
                }
                else
                {
                    // ========== 混合尺寸：PackTextures + 整图 Y 翻转 ==========
                    PackResult result = PackMixed(texArray, _padding, maxSize);
                    if (result == null) return;
                    atlas = result.Atlas;
                    rects = result.UVRects;
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

                string layoutMode = allSameSize ? "网格排列（左上角起）" : "自动排列";
                _previewAtlas = atlasTexture;
                _existingMapping = mapping;
                _lastReport = $"✅ 打包完成\n" +
                    $"域: {domainName}\n" +
                    $"排列: {layoutMode}\n" +
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
                // 清除打包标志
                IsPackingInProgress = false;

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

        /// <summary>
        /// 打包结果——Atlas 贴图 + 每张子图的 UV Rect。
        /// </summary>
        private class PackResult
        {
            public Texture2D Atlas;
            public Rect[] UVRects;
        }

        /// <summary>
        /// 同尺寸子图：手动网格排列，从左上角开始，逐行从左到右。
        /// 输出的 PNG 在图片查看器中看到的顺序就是 frame 0 在左上角。
        /// UV 坐标已正确换算（PNG 图片空间 Y 轴翻转为 UV 空间）。
        /// </summary>
        private PackResult PackGrid(Texture2D[] textures, int cellW, int cellH, int padding, int maxSize)
        {
            int count = textures.Length;
            int cellPaddedW = cellW + padding;
            int cellPaddedH = cellH + padding;

            // 计算最优列数——尽量接近正方形
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / cols);

            int atlasW = cols * cellPaddedW - padding; // 最后一列不需要右侧 padding
            int atlasH = rows * cellPaddedH - padding; // 最后一行不需要底部 padding

            // 向上对齐到 4 的倍数（纹理压缩友好）
            atlasW = ((atlasW + 3) / 4) * 4;
            atlasH = ((atlasH + 3) / 4) * 4;

            if (atlasW > maxSize || atlasH > maxSize)
            {
                EditorUtility.DisplayDialog("打包失败",
                    $"网格排列需要 {atlasW}×{atlasH}，超过最大尺寸 {maxSize}。\n" +
                    $"请减少贴图数量或增大最大尺寸。", "确定");
                return null;
            }

            var atlas = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false);
            // 清空为透明
            var clearPixels = new Color[atlasW * atlasH];
            atlas.SetPixels(clearPixels);

            var uvRects = new Rect[count];

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;

                // 图片空间坐标（左上角原点）：子图 i 的左上角
                int imgX = col * cellPaddedW;
                int imgY = row * cellPaddedH;

                // Unity 纹理坐标（左下角原点）：SetPixels 需要左下角的 (x, y)
                // 图片空间 Y → 纹理空间 Y 的转换：texY = atlasH - imgY - cellH
                int texX = imgX;
                int texY = atlasH - imgY - cellH;

                atlas.SetPixels(texX, texY, cellW, cellH, textures[i].GetPixels());

                // UV Rect（归一化，左下角原点）
                uvRects[i] = new Rect(
                    (float)texX / atlasW,
                    (float)texY / atlasH,
                    (float)cellW / atlasW,
                    (float)cellH / atlasH);
            }

            atlas.Apply();
            return new PackResult { Atlas = atlas, UVRects = uvRects };
        }

        /// <summary>
        /// 混合尺寸子图：使用 PackTextures 自动布局，保持原始排列不做翻转。
        /// 混合尺寸场景下每个子图通过独立 UVRect 寻址，不用于序列帧播放。
        /// </summary>
        private PackResult PackMixed(Texture2D[] textures, int padding, int maxSize)
        {
            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Rect[] rects = atlas.PackTextures(textures, padding, maxSize);

            if (rects == null || rects.Length != textures.Length)
            {
                EditorUtility.DisplayDialog("打包失败", "贴图打包失败，可能超过最大尺寸限制。", "确定");
                Object.DestroyImmediate(atlas);
                return null;
            }

            // 安全校验：PackTextures 在源贴图不可读时不报错，只返回 2×2 白点
            if (atlas.width <= 2 && atlas.height <= 2 && textures.Length > 0)
            {
                EditorUtility.DisplayDialog("打包失败",
                    "Atlas 输出为 2×2，源贴图像素数据不可读。\n" +
                    "请检查源贴图的 Read/Write Enabled 设置。", "确定");
                Object.DestroyImmediate(atlas);
                return null;
            }

            return new PackResult { Atlas = atlas, UVRects = rects };
        }
    }
}
