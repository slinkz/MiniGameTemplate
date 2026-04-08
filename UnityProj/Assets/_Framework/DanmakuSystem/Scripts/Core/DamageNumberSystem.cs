using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 伤害飘字系统——环形缓冲区 (128) + 独立 Mesh 渲染。
    /// 高频飘字使用数字精灵 Mesh 合批（零 GC），低频可回退到 FairyGUI 文本。
    /// </summary>
    public class DamageNumberSystem
    {
        public const int MAX_NUMBERS = 128;

        private readonly DamageNumberData[] _buffer = new DamageNumberData[MAX_NUMBERS];
        private int _head;   // 下一个写入位置
        private int _count;  // 当前活跃数量

        // 渲染
        private Mesh _mesh;
        private Material _material;
        private DanmakuVertex[] _vertices;
        private int[] _indices;
        private int _quadCount;

        // 数字精灵图集配置（10 个数字 0-9，水平排列）
        private Texture2D _numberAtlas;
        private const float DIGIT_UV_WIDTH = 0.1f;  // 每个数字占 10% 宽度
        private const float DIGIT_SIZE = 0.3f;       // 世界单位
        private const float DIGIT_SPACING = 0.2f;    // 数字间距
        private const float FLOAT_SPEED = 1.5f;      // 上飘速度
        private const float FADE_START = 0.6f;        // 开始淡出的生命百分比

        // VertexAttributeDescriptor 缓存
        private static readonly VertexAttributeDescriptor[] VertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
        };

        public void Initialize(DanmakuRenderConfig renderConfig)
        {
            _numberAtlas = renderConfig.NumberAtlas;

            // 每个飘字最多 5 位数 × 1 Quad
            int maxQuads = MAX_NUMBERS * 5;
            int vertexCount = maxQuads * 4;
            int indexCount = maxQuads * 6;

            _vertices = new DanmakuVertex[vertexCount];
            _indices = new int[indexCount];

            // 预填充索引
            for (int q = 0; q < maxQuads; q++)
            {
                int vi = q * 4;
                int ii = q * 6;
                _indices[ii + 0] = vi + 0;
                _indices[ii + 1] = vi + 1;
                _indices[ii + 2] = vi + 2;
                _indices[ii + 3] = vi + 2;
                _indices[ii + 4] = vi + 3;
                _indices[ii + 5] = vi + 0;
            }

            _mesh = new Mesh { name = "DamageNumbers" };
            _mesh.SetVertexBufferParams(vertexCount, VertexLayout);
            _mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
            _mesh.SetIndices(_indices, MeshTopology.Triangles, 0, false);

            // 复用 Normal 材质（飘字用 Alpha Blend）
            _material = renderConfig.BulletMaterial;
        }

        /// <summary>
        /// 生成一个伤害飘字。
        /// </summary>
        public void Spawn(Vector2 position, int damage, bool isCritical = false)
        {
            ref var data = ref _buffer[_head];
            data.Position = position;
            data.Velocity = new Vector2(Random.Range(-0.3f, 0.3f), FLOAT_SPEED);
            data.Lifetime = 0.8f;
            data.Elapsed = 0;
            data.Damage = damage;
            data.DigitCount = CountDigits(damage);
            data.Flags = isCritical ? (byte)1 : (byte)0;
            data.Scale = isCritical ? 1.5f : 1f;
            data.Color = isCritical
                ? new Color32(255, 200, 50, 255)
                : new Color32(255, 255, 255, 255);

            _head = (_head + 1) % MAX_NUMBERS;
            if (_count < MAX_NUMBERS) _count++;
        }

        /// <summary>
        /// 每帧更新 + 渲染。由 DanmakuSystem.LateUpdate 调用。
        /// </summary>
        public void UpdateAndRender(float dt)
        {
            _quadCount = 0;

            for (int i = 0; i < MAX_NUMBERS; i++)
            {
                ref var data = ref _buffer[i];
                if (data.Lifetime <= 0) continue;

                data.Elapsed += dt;
                if (data.Elapsed >= data.Lifetime)
                {
                    data.Lifetime = 0;  // 标记为不活跃
                    _count--;
                    continue;
                }

                // 上飘 + 减速
                float t = data.Elapsed / data.Lifetime;
                float speedFactor = 1f - t * 0.5f;  // 逐渐减速
                data.Position += data.Velocity * speedFactor * dt;

                // 透明度淡出
                float alpha = t > FADE_START ? 1f - (t - FADE_START) / (1f - FADE_START) : 1f;

                // 逐位写入数字 Quad
                WriteNumber(data, alpha);
            }

            // 上传 + 绘制
            if (_quadCount > 0 && _material != null)
            {
                int vertexCount = _quadCount * 4;
                int indexCount = _quadCount * 6;

                _mesh.SetVertexBufferData(_vertices, 0, 0, vertexCount, 0,
                    MeshUpdateFlags.DontValidateIndices |
                    MeshUpdateFlags.DontRecalculateBounds |
                    MeshUpdateFlags.DontNotifyMeshUsers);

                _mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles),
                    MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);

                _mesh.bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 1));

                Graphics.DrawMesh(_mesh, Matrix4x4.identity, _material, 0);
            }
        }

        public void Dispose()
        {
            if (_mesh != null) Object.Destroy(_mesh);
        }

        // ──── 内部方法 ────

        private void WriteNumber(in DamageNumberData data, float alpha)
        {
            int damage = data.Damage;
            int digits = data.DigitCount;
            float totalWidth = digits * DIGIT_SPACING * data.Scale;
            float startX = data.Position.x - totalWidth * 0.5f;

            // 从高位到低位
            int divisor = 1;
            for (int d = 1; d < digits; d++) divisor *= 10;

            for (int d = 0; d < digits; d++)
            {
                int digit = (damage / divisor) % 10;
                divisor /= 10;

                float x = startX + d * DIGIT_SPACING * data.Scale;
                float halfSize = DIGIT_SIZE * 0.5f * data.Scale;

                float uvLeft = digit * DIGIT_UV_WIDTH;
                float uvRight = uvLeft + DIGIT_UV_WIDTH;

                int baseVertex = _quadCount * 4;
                if (baseVertex + 4 > _vertices.Length) return;

                byte a = (byte)(alpha * data.Color.a);
                var color = new Color32(data.Color.r, data.Color.g, data.Color.b, a);

                _vertices[baseVertex + 0] = new DanmakuVertex
                {
                    Position = new Vector3(x - halfSize, data.Position.y - halfSize, 0),
                    UV = new Vector2(uvLeft, 0),
                    Color = color,
                };
                _vertices[baseVertex + 1] = new DanmakuVertex
                {
                    Position = new Vector3(x + halfSize, data.Position.y - halfSize, 0),
                    UV = new Vector2(uvRight, 0),
                    Color = color,
                };
                _vertices[baseVertex + 2] = new DanmakuVertex
                {
                    Position = new Vector3(x + halfSize, data.Position.y + halfSize, 0),
                    UV = new Vector2(uvRight, 1),
                    Color = color,
                };
                _vertices[baseVertex + 3] = new DanmakuVertex
                {
                    Position = new Vector3(x - halfSize, data.Position.y + halfSize, 0),
                    UV = new Vector2(uvLeft, 1),
                    Color = color,
                };

                _quadCount++;
            }
        }

        private static byte CountDigits(int value)
        {
            if (value < 0) value = -value;
            if (value < 10) return 1;
            if (value < 100) return 2;
            if (value < 1000) return 3;
            if (value < 10000) return 4;
            return 5;
        }
    }
}
