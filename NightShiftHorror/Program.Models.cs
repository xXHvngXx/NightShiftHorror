using System.Numerics;
using Raylib_cs;

partial class Program
{
    sealed class CustomModelEntry
    {
        public required Model Model;
        public float AutoScale;
        public float BaseOffsetY;
        public Vector3 ScaledSize;
    }

    static readonly Dictionary<string, CustomModelEntry> customModels = new(StringComparer.OrdinalIgnoreCase);
    static readonly List<BoundingBox> customModelColliders = [];
    static Shader modelLitShader;
    static int modelLightLevelLoc = -1;
    static bool hasModelLitShader = false;

    const string ModelVertShader = """
        #version 330
        in vec3 vertexPosition;
        in vec2 vertexTexCoord;
        in vec3 vertexNormal;
        uniform mat4 mvp;
        uniform mat4 matModel;
        out vec3 fragNormal;
        void main() {
            fragNormal = normalize(vec3(matModel * vec4(vertexNormal, 0.0)));
            gl_Position = mvp * vec4(vertexPosition, 1.0);
        }
        """;

    const string ModelFragShader = """
        #version 330
        in vec3 fragNormal;
        uniform vec4 colDiffuse;
        uniform float lightLevel;
        out vec4 finalColor;
        void main() {
            vec3 lightDir = normalize(vec3(0.35, 0.85, 0.45));
            float diff = max(dot(normalize(fragNormal), lightDir), 0.0);
            float shade = (0.54 + 0.46 * diff) * lightLevel;
            vec3 charcoal = vec3(0.28, 0.30, 0.34);
            vec3 baseCol = mix(charcoal, max(colDiffuse.rgb, charcoal), 0.35);
            finalColor = vec4(baseCol * shade, colDiffuse.a);
        }
        """;

    static void LoadCustomModels()
    {
        if (!hasModelLitShader)
        {
            modelLitShader = Raylib.LoadShaderFromMemory(ModelVertShader, ModelFragShader);
            modelLightLevelLoc = Raylib.GetShaderLocation(modelLitShader, "lightLevel");
            hasModelLitShader = true;
        }

        string modelsDir = Path.Combine("Assets", "Models");
        Directory.CreateDirectory(modelsDir);

        RegisterCustomModel("shelf", Path.Combine(modelsDir, "grocery_shelf.glb"), targetHeight: 1.85f);
        RebuildStoreShelfColliders();
    }

    static void RegisterCustomModel(string key, string filePath, float targetHeight)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[MODEL ERROR] Khong tim thay file: {Path.GetFullPath(filePath)}");
            return;
        }

        Model model = Raylib.LoadModel(filePath);
        if (model.MeshCount == 0)
        {
            Console.WriteLine($"[MODEL ERROR] File '{filePath}' khong co mesh hop le!");
            return;
        }

        BoundingBox bounds = Raylib.GetModelBoundingBox(model);
        float rawHeight = Math.Max(0.001f, bounds.Max.Y - bounds.Min.Y);
        float scale = targetHeight / rawHeight;
        float bottomOffsetY = -bounds.Min.Y * scale;
        Vector3 scaledSize = (bounds.Max - bounds.Min) * scale;

        customModels[key] = new CustomModelEntry
        {
            Model = model,
            AutoScale = scale,
            BaseOffsetY = bottomOffsetY,
            ScaledSize = scaledSize
        };

        Console.WriteLine($"[MODEL OK] Da nap '{key}' ({scaledSize.X:F2}m x {scaledSize.Y:F2}m x {scaledSize.Z:F2}m)");
    }

    // Bố trí 6 kệ 3D thoáng rộng: 4 kệ thành 2 dãy bên phải + 2 kệ ngang góc trước bên trái
    static (Vector3 Pos, float Yaw, int Seed)[] GetStoreShelfPlacements()
    {
        float halfSpacingZ = 1.15f;
        if (customModels.TryGetValue("shelf", out var entry))
        {
            float shelfLength = Math.Max(entry.ScaledSize.X, entry.ScaledSize.Z);
            halfSpacingZ = shelfLength * 0.5f + 0.06f;
        }

        float rightCenterZ = 0.4f; // Nằm gọn từ Z = -2.0 đến Z = +2.8, chừa sảnh rộng trước cửa và quầy

        return
        [
            // Cánh phải - Dãy 1 (X = 4.2, cách quầy thu ngân gần 4m)
            (new Vector3(4.2f, 0f, rightCenterZ - halfSpacingZ), -90f, 1),
            (new Vector3(4.2f, 0f, rightCenterZ + halfSpacingZ), -90f, 2),

            // Cánh phải - Dãy 2 (X = 6.9, cách dãy 1 2.7m và cách tủ lạnh tường phải >2m)
            (new Vector3(6.9f, 0f, rightCenterZ - halfSpacingZ), -90f, 3),
            (new Vector3(6.9f, 0f, rightCenterZ + halfSpacingZ), -90f, 4),

            // Cánh trái - 2 kệ đặt ngang ở khu vực phía trước, tách rời hoàn toàn khỏi quầy đảo gỗ
            (new Vector3(-7.3f, 0f, 4.4f), 0f, 5),
            (new Vector3(-3.9f, 0f, 4.4f), 0f, 6)
        ];
    }

    static void RebuildStoreShelfColliders()
    {
        customModelColliders.Clear();

        if (customModels.ContainsKey("shelf"))
        {
            foreach (var item in GetStoreShelfPlacements())
            {
                customModelColliders.Add(GetCustomModelBounds("shelf", item.Pos, item.Yaw));
            }
        }

        // Khung va chạm chuẩn xác cho các quầy/tủ ở hai cánh mới (X = ±10.5m)
        customModelColliders.Add(new BoundingBox(new Vector3(-10.45f, 0f, -3.40f), new Vector3(-9.20f, 2.2f, 3.00f))); // Trạm nước tường trái
        customModelColliders.Add(new BoundingBox(new Vector3(-8.05f, 0f, -1.35f), new Vector3(-6.65f, 2.0f, 2.35f)));  // Quầy đảo bánh trái
        customModelColliders.Add(new BoundingBox(new Vector3(9.65f, 0f, -5.10f), new Vector3(10.45f, 2.6f, 5.90f)));  // Dãy tủ lạnh tường phải
        customModelColliders.Add(new BoundingBox(new Vector3(1.25f, 0f, 4.45f), new Vector3(2.15f, 1.8f, 5.35f)));   // Kệ snack gần cửa
    }

    static void DrawStoreShelves(bool lit)
    {
        if (!customModels.ContainsKey("shelf")) return;

        foreach (var item in GetStoreShelfPlacements())
        {
            DrawShelfWithProducts(item.Pos, item.Yaw, lit, item.Seed);
        }
    }

    static void DrawCustomModel(string key, Vector3 position, float rotationYDegrees = 0f, bool lit = true, float extraScale = 1.0f)
    {
        if (!customModels.TryGetValue(key, out var entry)) return;

        float finalScale = entry.AutoScale * extraScale;
        Vector3 finalPos = new(position.X, position.Y + entry.BaseOffsetY * extraScale, position.Z);

        if (hasModelLitShader)
        {
            float lightVal = lit ? 1.0f : 0.25f;
            Raylib.SetShaderValue(modelLitShader, modelLightLevelLoc, lightVal, ShaderUniformDataType.Float);
            Raylib.BeginShaderMode(modelLitShader);
        }

        Raylib.DrawModelEx(
            entry.Model,
            finalPos,
            Vector3.UnitY,
            rotationYDegrees,
            new Vector3(finalScale, finalScale, finalScale),
            Color.White
        );

        if (hasModelLitShader) Raylib.EndShaderMode();
    }

    static void DrawShelfWithProducts(Vector3 position, float rotationYDegrees, bool lit, int seed)
    {
        DrawCustomModel("shelf", position, rotationYDegrees, lit);

        if (!customModels.TryGetValue("shelf", out var entry)) return;

        bool alongX = entry.ScaledSize.X >= entry.ScaledSize.Z;
        float shelfLength = (alongX ? entry.ScaledSize.X : entry.ScaledSize.Z) * 0.84f;
        float shelfDepth = (alongX ? entry.ScaledSize.Z : entry.ScaledSize.X) * 0.62f;
        float shelfHeight = entry.ScaledSize.Y;

        float rad = rotationYDegrees * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        Color[] itemColors =
        [
            new(185, 55, 55, 255),
            new(45, 125, 175, 255),
            new(210, 165, 50, 255),
            new(65, 150, 85, 255),
            new(215, 215, 220, 255),
            new(150, 80, 165, 255)
        ];

        float[] tierHeights = [shelfHeight * 0.14f, shelfHeight * 0.36f, shelfHeight * 0.58f, shelfHeight * 0.80f];
        int cols = Math.Max(4, (int)(shelfLength / 0.22f));
        float shadeK = lit ? 1.0f : 0.28f;

        for (int t = 0; t < tierHeights.Length; t++)
        {
            float yBase = position.Y + tierHeights[t];
            for (int side = -1; side <= 1; side += 2)
            {
                float localDepth = side * (shelfDepth * 0.26f);
                for (int c = 0; c < cols; c++)
                {
                    int hash = (seed * 31 + t * 17 + c * 13 + (side + 2) * 7) & 0x7FFFFFFF;
                    if (hash % 5 == 0) continue;

                    float u = (c + 0.5f) / cols - 0.5f;
                    float localAlong = u * shelfLength;

                    float lx = alongX ? localAlong : localDepth;
                    float lz = alongX ? localDepth : localAlong;

                    float wx = position.X + lx * cos + lz * sin;
                    float wz = position.Z - lx * sin + lz * cos;

                    float itemW = 0.13f + (hash % 3) * 0.02f;
                    float itemH = 0.15f + (hash % 4) * 0.03f;
                    Color raw = itemColors[hash % itemColors.Length];
                    Color col = new((byte)(raw.R * shadeK), (byte)(raw.G * shadeK), (byte)(raw.B * shadeK), (byte)255);

                    if (hash % 3 == 0)
                        Raylib.DrawCylinder(new Vector3(wx, yBase, wz), itemW * 0.42f, itemW * 0.42f, itemH, 10, col);
                    else
                        Raylib.DrawCube(new Vector3(wx, yBase + itemH * 0.5f, wz), itemW, itemH, itemW * 0.85f, col);
                }
            }
        }
    }

    static BoundingBox GetCustomModelBounds(string key, Vector3 position, float rotationYDegrees = 0f, float extraScale = 1.0f)
    {
        if (!customModels.TryGetValue(key, out var entry))
            return new BoundingBox(position, position);

        Vector3 size = entry.ScaledSize * extraScale;
        float rad = rotationYDegrees * MathF.PI / 180f;
        float absCos = MathF.Abs(MathF.Cos(rad));
        float absSin = MathF.Abs(MathF.Sin(rad));

        float worldWidthX = size.X * absCos + size.Z * absSin;
        float worldDepthZ = size.X * absSin + size.Z * absCos;

        return new BoundingBox(
            new Vector3(position.X - worldWidthX * 0.5f, position.Y, position.Z - worldDepthZ * 0.5f),
            new Vector3(position.X + worldWidthX * 0.5f, position.Y + size.Y, position.Z + worldDepthZ * 0.5f)
        );
    }

    static void UnloadCustomModels()
    {
        foreach (var entry in customModels.Values)
        {
            Raylib.UnloadModel(entry.Model);
        }
        customModels.Clear();
        customModelColliders.Clear();

        if (hasModelLitShader)
        {
            Raylib.UnloadShader(modelLitShader);
            hasModelLitShader = false;
        }
    }
}