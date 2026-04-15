using System;
using MiniGameTemplate.Rendering;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.Editor.Rendering
{
    /// <summary>
    /// 子图选择器弹出窗口——在贴图上可视化显示子区域，点击选择后回写 UVRect。
    /// </summary>
    public class AtlasSubSpritePopup : EditorWindow
    {
        private Texture2D _texture;
        private AtlasMappingSO _atlasMapping;
        private int _columns;
        private int _rows;
        private Rect _currentUVRect;
        private Action<Rect> _onSelected;
        private Vector2 _scrollPos;
        private bool _useAtlasEntries;

        /// <summary>
        /// 打开子图选择器窗口。
        /// </summary>
        /// <param name="texture">要显示的贴图（Atlas 或 SourceTexture）</param>
        /// <param name="atlasMapping">Atlas 映射（可为 null）</param>
        /// <param name="columns">SpriteSheet 列数（无 Atlas 时用于网格绘制）</param>
        /// <param name="rows">SpriteSheet 行数</param>
        /// <param name="currentUV">当前 UV 区域（用于高亮）</param>
        /// <param name="onSelected">选择回调</param>
        public static void Show(Texture2D texture, AtlasMappingSO atlasMapping,
            int columns, int rows, Rect currentUV, Action<Rect> onSelected)
        {
            var window = CreateInstance<AtlasSubSpritePopup>();
            window.titleContent = new GUIContent("选择子图");
            window._texture = texture;
            window._atlasMapping = atlasMapping;
            window._columns = Mathf.Max(1, columns);
            window._rows = Mathf.Max(1, rows);
            window._currentUVRect = currentUV;
            window._onSelected = onSelected;
            window._useAtlasEntries = atlasMapping != null && atlasMapping.Entries != null
                && atlasMapping.Entries.Length > 0;
            window.ShowUtility();
            window.minSize = new Vector2(300, 350);
        }

        private void OnGUI()
        {
            if (_texture == null)
            {
                EditorGUILayout.HelpBox("贴图为空", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("点击子图区域选择 UVRect", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"贴图: {_texture.name} ({_texture.width}×{_texture.height})");

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // 计算绘制区域
            float maxW = position.width - 20;
            float ratio = (float)_texture.height / _texture.width;
            float drawW = Mathf.Min(maxW, _texture.width);
            float drawH = drawW * ratio;

            var texRect = GUILayoutUtility.GetRect(drawW, drawH);
            GUI.DrawTexture(texRect, _texture, ScaleMode.ScaleToFit);

            // 实际绘制区域（ScaleToFit 可能不占满）
            float actualW = texRect.width;
            float actualH = actualW * ratio;
            if (actualH > texRect.height)
            {
                actualH = texRect.height;
                actualW = actualH / ratio;
            }
            float offsetX = texRect.x + (texRect.width - actualW) * 0.5f;
            float offsetY = texRect.y + (texRect.height - actualH) * 0.5f;
            var actualRect = new Rect(offsetX, offsetY, actualW, actualH);

            // 绘制子图区域
            if (_useAtlasEntries)
            {
                DrawAtlasEntries(actualRect);
            }
            else
            {
                DrawGridCells(actualRect);
            }

            // 高亮当前选中区域
            DrawHighlight(actualRect, _currentUVRect, Color.green, 3f);

            // 点击检测
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                && actualRect.Contains(Event.current.mousePosition))
            {
                Vector2 localPos = Event.current.mousePosition - actualRect.position;
                float normalizedX = localPos.x / actualRect.width;
                float normalizedY = 1f - (localPos.y / actualRect.height); // GUI Y 翻转

                Rect selectedUV = FindClickedUV(normalizedX, normalizedY);
                _currentUVRect = selectedUV;
                _onSelected?.Invoke(selectedUV);
                Event.current.Use();
                Close();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"当前 UVRect: {_currentUVRect}", EditorStyles.miniLabel);
        }

        private void DrawAtlasEntries(Rect drawRect)
        {
            if (_atlasMapping == null) return;

            for (int i = 0; i < _atlasMapping.Entries.Length; i++)
            {
                var entry = _atlasMapping.Entries[i];
                DrawUVOutline(drawRect, entry.UVRect, new Color(1f, 0.8f, 0f, 0.8f), 1f);

                // 标签
                var labelRect = UVToScreenRect(drawRect, entry.UVRect);
                string label = entry.SourceTexture != null ? entry.SourceTexture.name : $"[{i}]";
                GUI.Label(new Rect(labelRect.x + 2, labelRect.y + 2, labelRect.width, 16), label,
                    EditorStyles.miniLabel);
            }
        }

        private void DrawGridCells(Rect drawRect)
        {
            float cellW = 1f / _columns;
            float cellH = 1f / _rows;

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    var cellUV = new Rect(col * cellW, row * cellH, cellW, cellH);
                    DrawUVOutline(drawRect, cellUV, new Color(0.5f, 0.5f, 1f, 0.5f), 1f);
                }
            }
        }

        private void DrawHighlight(Rect drawRect, Rect uvRect, Color color, float thickness)
        {
            DrawUVOutline(drawRect, uvRect, color, thickness);

            // 半透明填充
            var screenRect = UVToScreenRect(drawRect, uvRect);
            var fillColor = new Color(color.r, color.g, color.b, 0.15f);
            EditorGUI.DrawRect(screenRect, fillColor);
        }

        private void DrawUVOutline(Rect drawRect, Rect uvRect, Color color, float thickness)
        {
            var screenRect = UVToScreenRect(drawRect, uvRect);
            // Top
            EditorGUI.DrawRect(new Rect(screenRect.x, screenRect.y, screenRect.width, thickness), color);
            // Bottom
            EditorGUI.DrawRect(new Rect(screenRect.x, screenRect.yMax - thickness, screenRect.width, thickness), color);
            // Left
            EditorGUI.DrawRect(new Rect(screenRect.x, screenRect.y, thickness, screenRect.height), color);
            // Right
            EditorGUI.DrawRect(new Rect(screenRect.xMax - thickness, screenRect.y, thickness, screenRect.height), color);
        }

        private Rect UVToScreenRect(Rect drawRect, Rect uvRect)
        {
            // UV (0,0) = bottom-left, GUI (0,0) = top-left
            float x = drawRect.x + uvRect.x * drawRect.width;
            float y = drawRect.y + (1f - uvRect.y - uvRect.height) * drawRect.height;
            float w = uvRect.width * drawRect.width;
            float h = uvRect.height * drawRect.height;
            return new Rect(x, y, w, h);
        }

        private Rect FindClickedUV(float normalizedX, float normalizedY)
        {
            if (_useAtlasEntries && _atlasMapping != null)
            {
                // 查找点击了哪个 Entry
                for (int i = 0; i < _atlasMapping.Entries.Length; i++)
                {
                    var uv = _atlasMapping.Entries[i].UVRect;
                    if (normalizedX >= uv.x && normalizedX <= uv.x + uv.width
                        && normalizedY >= uv.y && normalizedY <= uv.y + uv.height)
                    {
                        return uv;
                    }
                }
            }

            // Grid 模式：按列行计算
            int col = Mathf.Clamp((int)(normalizedX * _columns), 0, _columns - 1);
            int row = Mathf.Clamp((int)(normalizedY * _rows), 0, _rows - 1);
            float cellW = 1f / _columns;
            float cellH = 1f / _rows;
            return new Rect(col * cellW, row * cellH, cellW, cellH);
        }
    }
}
