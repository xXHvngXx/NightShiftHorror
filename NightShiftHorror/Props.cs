using System.Numerics;
using Raylib_cs;

public enum FaceId
{
    None,
    OldMan,
    Girl,
    StrangeMan,
    YellowNormal,
    YellowHorror,
    Manager,
    Friend1,
    Friend2,
    Friend3
}

public static partial class Props
{
    static Color Col(int r, int g, int b, int a = 255) => new((byte)r, (byte)g, (byte)b, (byte)a);

    static Color Shade(Color c, bool lit, float k = 0.16f)
        => lit ? c : new Color((byte)(c.R * k), (byte)(c.G * k), (byte)(c.B * k), c.A);

    static Color LerpCol(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }

    static void Box(float x, float y, float z, float w, float h, float l, Color c)
        => Raylib.DrawCube(new Vector3(x, y, z), w, h, l, c);

    static void TexBox(float x, float y, float z, float w, float h, float l, Texture2D tex, float tileMeters, Color tint)
        => DrawTexturedBox(new Vector3(x, y, z), new Vector3(w, h, l), tex, tileMeters, tint);

    static void Ellipsoid(float x, float y, float z, float rx, float ry, float rz, Color c, int rings = 14, int slices = 16)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(x, y, z);
        Rlgl.Scalef(rx, ry, rz);
        Raylib.DrawSphereEx(Vector3.Zero, 1f, rings, slices, c);
        Rlgl.PopMatrix();
    }

    static int Hash(int a, int b, int c)
        => ((a * 73856093) ^ (b * 19349663) ^ (c * 83492791)) & 0x7fffffff;

    static readonly Color[] Palette =
    {
        new Color((byte)220, (byte)70,  (byte)60,  (byte)255),
        new Color((byte)235, (byte)170, (byte)50,  (byte)255),
        new Color((byte)70,  (byte)150, (byte)220, (byte)255),
        new Color((byte)80,  (byte)185, (byte)110, (byte)255),
        new Color((byte)240, (byte)240, (byte)235, (byte)255),
        new Color((byte)150, (byte)90,  (byte)200, (byte)255),
        new Color((byte)230, (byte)120, (byte)160, (byte)255),
        new Color((byte)60,  (byte)70,  (byte)80,  (byte)255),
    };

    static void Leg(Vector3 pivot, float angle, float length, Color pants, Color shoe)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pivot.X, pivot.Y, pivot.Z);
        Rlgl.Rotatef(angle, 1, 0, 0);
        Raylib.DrawCube(new Vector3(0, -length / 2, 0), 0.17f, length, 0.2f, pants);
        Raylib.DrawCube(new Vector3(0, -length - 0.04f, 0.04f), 0.18f, 0.08f, 0.3f, shoe);
        Rlgl.PopMatrix();
    }

    static void Arm(Vector3 pivot, float angle, Color sleeve, Color skin)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pivot.X, pivot.Y, pivot.Z);
        Rlgl.Rotatef(angle, 1, 0, 0);
        Raylib.DrawCube(new Vector3(0, -0.19f, 0), 0.12f, 0.38f, 0.14f, sleeve);
        Raylib.DrawCube(new Vector3(0, -0.45f, 0), 0.10f, 0.14f, 0.12f, skin);
        Rlgl.PopMatrix();
    }

    public static (float HeadYaw, float HeadPitch, float HeadRoll, Vector2 EyeGaze) ComputeGazeTarget(
        Vector3 headWorldPos, float bodyYawDeg, Vector3? viewerPos, float weight = 1f)
    {
        if (!viewerPos.HasValue || weight <= 0.001f)
            return (0f, 0f, 0f, Vector2.Zero);

        Vector3 toViewer = viewerPos.Value - headWorldPos;
        float dist = toViewer.Length();
        if (dist < 0.15f || dist > 10.0f)
            return (0f, 0f, 0f, Vector2.Zero);

        float rad = bodyYawDeg * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        float localX = toViewer.X * cos - toViewer.Z * sin;
        float localZ = toViewer.X * sin + toViewer.Z * cos;
        float localY = toViewer.Y;

        if (localZ <= 0.05f)
            return (0f, 0f, 0f, Vector2.Zero);

        float horizRad = MathF.Atan2(localX, localZ);
        float vertRad = MathF.Atan2(localY, MathF.Sqrt(localX * localX + localZ * localZ));

        const float maxHorizFov = 1.186f;
        const float maxVertFov = 0.785f;

        if (MathF.Abs(horizRad) > maxHorizFov || MathF.Abs(vertRad) > maxVertFov)
            return (0f, 0f, 0f, Vector2.Zero);

        float horizDeg = horizRad * 180f / MathF.PI;
        float vertDeg = vertRad * 180f / MathF.PI;

        float headYaw = Math.Clamp(horizDeg * 0.78f, -52f, 52f) * weight;
        float headPitch = Math.Clamp(vertDeg * 0.55f, -16f, 16f) * weight;
        float headRoll = Math.Clamp(-horizDeg * 0.12f, -7.5f, 7.5f) * weight;

        float remHoriz = (horizDeg - headYaw) / 30f;
        float remVert = (vertDeg - headPitch) / 25f;
        Vector2 eyeGaze = new(
            Math.Clamp(remHoriz, -1f, 1f) * weight,
            Math.Clamp(remVert, -1f, 1f) * weight
        );

        return (headYaw, headPitch, headRoll, eyeGaze);
    }

    static void UpperBody(float by, Color shirt, Color hair, float armL, float armR, bool hood, bool isFemale,
                          float headYaw, float headPitch, float headRoll, Vector2 eyeGaze, FaceId faceId)
    {
        Color skin = faceId switch
        {
            FaceId.OldMan => Col(212, 182, 158),
            FaceId.StrangeMan => Col(198, 184, 172),
            FaceId.YellowHorror => Col(178, 182, 186),
            FaceId.Friend3 => Col(194, 152, 122),
            FaceId.Manager => Col(214, 178, 150),
            _ when isFemale => Col(232, 198, 176),
            _ => Col(222, 186, 160)
        };

        Box(0, by + 0.30f, 0, 0.50f, 0.60f, 0.28f, shirt);

        float neckR = isFemale ? 0.044f : 0.052f;
        Color collarCol = LerpCol(shirt, Color.Black, 0.22f);
        Raylib.DrawCylinder(new Vector3(0, by + 0.59f, -0.006f), neckR + 0.015f, neckR + 0.018f, 0.032f, 16, collarCol);
        Raylib.DrawCylinder(new Vector3(0, by + 0.58f, -0.008f), neckR, neckR * 1.08f, 0.16f, 16, skin);

        Arm(new Vector3(-0.32f, by + 0.56f, 0), armL, shirt, skin);
        Arm(new Vector3(0.32f, by + 0.56f, 0), armR, shirt, skin);

        float neckPivotY = by + 0.66f;
        Rlgl.PushMatrix();
        Rlgl.Translatef(0, neckPivotY, 0);
        Rlgl.Rotatef(headYaw, 0, 1, 0);
        Rlgl.Rotatef(-headPitch, 1, 0, 0);
        Rlgl.Rotatef(headRoll, 0, 0, 1);

        DrawHead(isFemale, hair, skin, faceId, eyeGaze);

        if (hood)
        {
            Box(0, 0.18f, -0.05f, 0.36f, 0.38f, 0.32f, shirt);
            Box(0, 0.37f, 0.00f, 0.38f, 0.03f, 0.38f, shirt);
        }

        Rlgl.PopMatrix();
    }

    public static void DrawPerson(Vector3 pos, float yawDeg, Color shirt, Color hair,
                                  float phase, float swing, bool raincoat = false, bool isFemale = false,
                                  float headYaw = 0f, float headPitch = 0f, float headRoll = 0f,
                                  Vector2? eyeGaze = null, FaceId faceId = FaceId.None)
    {
        FearsBodySpec spec = faceId switch
        {
            FaceId.Manager => FearsBodySpec.Manager,
            FaceId.OldMan => FearsBodySpec.OldManNpc,
            FaceId.Girl => FearsBodySpec.MaiNpc,
            FaceId.StrangeMan => FearsBodySpec.HungNpc,
            FaceId.YellowNormal => FearsBodySpec.YellowGirl,
            FaceId.YellowHorror => FearsBodySpec.YellowHorror,
            _ when raincoat => FearsBodySpec.TallLanky,
            _ when isFemale => FearsBodySpec.SlimGirl,
            _ => FearsBodySpec.AverageMan
        };

        DrawFearsCharacter(pos, yawDeg, shirt, hair, spec, phase, swing,
                           headYaw, headPitch, headRoll, eyeGaze, faceId);
    }

    public static void DrawSeatedPerson(Vector3 pos, float yawDeg, Color shirt, Color hair,
                                        Vector3? viewerPos = null, FaceId faceId = FaceId.None, bool isFemale = false)
    {
        Color pants = isFemale ? Col(42, 38, 48) : Col(34, 38, 52);
        Color shoe = Col(20, 20, 24);
        const float hip = 0.57f;

        var gaze = ComputeGazeTarget(pos + new Vector3(0, hip + 0.81f, 0), yawDeg, viewerPos, 0.92f);

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        foreach (float x in new[] { -0.11f, 0.11f })
        {
            Rlgl.PushMatrix();
            Rlgl.Translatef(x, hip, 0);
            Rlgl.Rotatef(-90, 1, 0, 0);
            Raylib.DrawCube(new Vector3(0, -0.21f, 0), 0.17f, 0.42f, 0.2f, pants);
            Rlgl.PopMatrix();

            Leg(new Vector3(x, 0.63f, 0.42f), 0, 0.55f, pants, shoe);
        }

        UpperBody(hip, shirt, hair, -65f, -65f, false, isFemale,
                  gaze.HeadYaw, gaze.HeadPitch, gaze.HeadRoll, gaze.EyeGaze, faceId);

        Rlgl.PopMatrix();
    }

    // Nhân viên phục vụ bưng khay đi chậm rãi trên lối đi
    static void DrawCafeWaiter(Vector3 pos, float yawDeg, float walkPhase, bool isFemale, Color apronColor)
    {
        Color shirt = Col(238, 236, 232);
        Color hair = isFemale ? Col(42, 28, 22) : Col(28, 28, 32);
        FearsBodySpec spec = isFemale ? FearsBodySpec.SlimGirl : FearsBodySpec.AverageMan;

        DrawFearsCharacter(pos, yawDeg, shirt, hair, spec, walkPhase, 15f);

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        // Tạp dề phục vụ ngang hông
        Box(0f, 0.72f, 0.15f, 0.42f, 0.52f, 0.03f, apronColor);

        // Khay phục vụ cầm trước ngực + 2 ly nước
        float sway = MathF.Sin(walkPhase) * 0.012f;
        Raylib.DrawCylinder(new Vector3(0.16f, 1.06f + sway, 0.34f), 0.17f, 0.16f, 0.02f, 16, Col(45, 32, 24));
        Raylib.DrawCylinder(new Vector3(0.11f, 1.08f + sway, 0.32f), 0.038f, 0.034f, 0.09f, 10, Col(242, 242, 238));
        Raylib.DrawCylinder(new Vector3(0.21f, 1.08f + sway, 0.36f), 0.036f, 0.032f, 0.11f, 10, Col(215, 165, 95));

        Rlgl.PopMatrix();
    }

    // Tính vị trí & góc xoay nhân viên đi tuần hoàn dọc theo chuỗi điểm lộ trình (closed loop)
    static (Vector3 Pos, float YawDeg, float Phase) EvalLoopWalker(Vector3[] waypoints, float time, float speed)
    {
        int n = waypoints.Length;
        float[] segLen = new float[n];
        float totalLen = 0f;
        for (int i = 0; i < n; i++)
        {
            segLen[i] = Vector3.Distance(waypoints[i], waypoints[(i + 1) % n]);
            totalLen += segLen[i];
        }

        float dist = (time * speed) % totalLen;
        for (int i = 0; i < n; i++)
        {
            if (dist <= segLen[i])
            {
                Vector3 a = waypoints[i];
                Vector3 b = waypoints[(i + 1) % n];
                float t = segLen[i] > 0.001f ? dist / segLen[i] : 0f;
                Vector3 pos = Vector3.Lerp(a, b, t);
                Vector3 dir = b - a;
                float yaw = MathF.Atan2(dir.X, dir.Z) * 180f / MathF.PI;
                return (pos, yaw, time * speed * 6.5f);
            }
            dist -= segLen[i];
        }

        return (waypoints[0], 0f, 0f);
    }

    public static void DrawChair(Vector3 pos, float yawDeg, Color wood, Color seat)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        Box(0, 0.43f, -0.04f, 0.46f, 0.05f, 0.46f, seat);
        foreach (float sx in new[] { -0.19f, 0.19f })
            foreach (float sz in new[] { -0.19f, 0.19f })
                Box(sx, 0.2f, -0.04f + sz, 0.05f, 0.4f, 0.05f, wood);
        Box(0, 0.75f, -0.27f, 0.46f, 0.5f, 0.04f, wood);

        Rlgl.PopMatrix();
    }

    // Ghế bành bo tròn bọc nỉ sang trọng (như trong ảnh mẫu)
    static void DrawTubArmchair(Vector3 pos, float yawDeg, Color fabric)
    {
        Color darkFabric = LerpCol(fabric, Color.Black, 0.28f);
        Color cushionCol = LerpCol(fabric, Color.White, 0.08f);
        Color legCol = Col(28, 20, 16);

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        // 4 chân gỗ thấp
        foreach (float lx in new[] { -0.26f, 0.26f })
            foreach (float lz in new[] { -0.26f, 0.24f })
                Box(lx, 0.07f, lz, 0.06f, 0.14f, 0.06f, legCol);

        // Thân ghế bo tròn + nệm ngồi
        Raylib.DrawCylinder(new Vector3(0f, 0.14f, -0.02f), 0.37f, 0.35f, 0.24f, 20, darkFabric);
        Raylib.DrawCylinder(new Vector3(0f, 0.38f, 0.01f), 0.33f, 0.33f, 0.10f, 20, cushionCol);

        // Lưng ghế & tay vịn vòng cung bao quanh 210 độ
        for (int i = -6; i <= 6; i++)
        {
            float ang = i * 16f;
            float rad = ang * MathF.PI / 180f;
            float bx = MathF.Sin(rad) * 0.31f;
            float bz = -MathF.Cos(rad) * 0.31f;
            float sideDrop = MathF.Abs(i) * 0.018f;
            float backH = 0.50f - sideDrop;

            Rlgl.PushMatrix();
            Rlgl.Translatef(bx, 0.34f + backH * 0.5f, bz);
            Rlgl.Rotatef(-ang, 0, 1, 0);
            Box(0f, 0f, 0f, 0.15f, backH, 0.11f, fabric);
            Rlgl.PopMatrix();
        }

        Rlgl.PopMatrix();
    }

    // Ghế sofa băng dài (xanh navy hoặc nâu trầm)
    static void DrawLoungeSofa(Vector3 pos, float yawDeg, float width, Color fabric)
    {
        Color darkFabric = LerpCol(fabric, Color.Black, 0.28f);
        Color cushionCol = LerpCol(fabric, Color.White, 0.07f);
        Color legCol = Col(25, 18, 15);
        float hw = width * 0.5f;

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        foreach (float lx in new[] { -hw + 0.12f, hw - 0.12f })
            foreach (float lz in new[] { -0.32f, 0.32f })
                Box(lx, 0.06f, lz, 0.07f, 0.12f, 0.07f, legCol);

        // Đế sofa & tựa lưng
        Box(0f, 0.24f, 0f, width, 0.24f, 0.78f, darkFabric);
        Box(0f, 0.56f, -0.30f, width, 0.46f, 0.18f, fabric);
        // Tay vịn 2 bên
        Box(-hw + 0.08f, 0.44f, 0.04f, 0.16f, 0.36f, 0.72f, fabric);
        Box(hw - 0.08f, 0.44f, 0.04f, 0.16f, 0.36f, 0.72f, fabric);

        // Các múi đệm ngồi & gối tựa lưng
        int seats = Math.Max(2, (int)MathF.Round(width / 0.68f));
        float innerW = width - 0.34f;
        float padW = (innerW / seats) - 0.02f;
        for (int s = 0; s < seats; s++)
        {
            float px = -innerW * 0.5f + (s + 0.5f) * (innerW / seats);
            Box(px, 0.41f, 0.06f, padW, 0.11f, 0.60f, cushionCol);
            Box(px, 0.60f, -0.17f, padW, 0.34f, 0.12f, cushionCol);
        }

        Rlgl.PopMatrix();
    }

    // Bàn cà phê mặt đá sáng, chân trụ đen
    static void DrawLoungeTable(float x, float z, float w, float l, Texture2D topTex)
    {
        Color blackIron = Col(22, 24, 28);
        Box(x, 0.025f, z, 0.52f, 0.05f, 0.52f, blackIron);
        Box(x, 0.36f, z, 0.10f, 0.68f, 0.10f, blackIron);
        TexBox(x, 0.72f, z, w, 0.05f, l, topTex, 1.1f, Col(242, 232, 216));

        // Hộp giấy ăn & gạt tàn nhỏ trên bàn
        Box(x + 0.10f, 0.79f, z - 0.06f, 0.09f, 0.09f, 0.09f, Col(235, 228, 215));
        Raylib.DrawCylinder(new Vector3(x - 0.12f, 0.745f, z + 0.08f), 0.045f, 0.045f, 0.025f, 12, Col(210, 210, 215));
    }

    // Cụm 3 đèn lồng chụp tròn treo giật cấp tỏa sáng vàng ấm
    static void DrumLightCluster(float x, float z)
    {
        (float dx, float dz, float y, float r, float h)[] drums =
        [
            (0.00f,  0.00f, 3.22f, 0.42f, 0.24f),
            (-0.46f, 0.34f, 3.04f, 0.38f, 0.22f),
            (0.48f, -0.28f, 2.94f, 0.36f, 0.22f)
        ];

        foreach (var d in drums)
        {
            float lx = x + d.dx;
            float lz = z + d.dz;
            Box(lx, (3.85f + d.y + d.h) * 0.5f, lz, 0.018f, 3.85f - (d.y + d.h), 0.018f, Col(18, 18, 20));
            Raylib.DrawCylinder(new Vector3(lx, d.y, lz), d.r, d.r, d.h, 22, Col(68, 56, 44));
            Raylib.DrawCylinder(new Vector3(lx, d.y - 0.006f, lz), d.r * 0.96f, d.r * 0.96f, 0.015f, 22, Col(255, 240, 198));
            Raylib.DrawCylinder(new Vector3(lx, d.y + d.h - 0.008f, lz), d.r * 1.01f, d.r * 1.01f, 0.014f, 22, Col(245, 225, 175));
            Raylib.DrawSphere(new Vector3(lx, d.y - 0.06f, lz), d.r * 0.85f, Col(255, 215, 135, 22));
        }
    }

    // Cột vuông ốp nan gỗ dọc giữa quán
    static void SlattedPillar(float x, float z, float height)
    {
        Box(x, height * 0.5f, z, 0.72f, height, 0.72f, Col(42, 24, 16));
        Color slatCol = Col(138, 66, 36);
        for (int i = -4; i <= 4; i++)
        {
            float off = i * 0.082f;
            Box(x + off, height * 0.5f, z - 0.375f, 0.042f, height, 0.035f, slatCol);
            Box(x + off, height * 0.5f, z + 0.375f, 0.042f, height, 0.035f, slatCol);
            Box(x - 0.375f, height * 0.5f, z + off, 0.035f, height, 0.042f, slatCol);
            Box(x + 0.375f, height * 0.5f, z + off, 0.035f, height, 0.042f, slatCol);
        }
    }

    // Kệ vách ngăn ô vuông gỗ bày cây xanh nhỏ
    static void CubeDividerShelf(float x, float z, float yawDeg, int cols, int rows)
    {
        Color wood = Col(125, 76, 44);
        const float cell = 0.44f;
        float totalW = cols * cell;
        float totalH = rows * cell;

        Rlgl.PushMatrix();
        Rlgl.Translatef(x, 0f, z);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        for (int r = 0; r <= rows; r++)
            Box(0f, 0.12f + r * cell, 0f, totalW + 0.04f, 0.032f, 0.34f, wood);
        for (int c = 0; c <= cols; c++)
            Box(-totalW * 0.5f + c * cell, 0.12f + totalH * 0.5f, 0f, 0.032f, totalH, 0.34f, wood);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int h = Hash(r + (int)x, c + (int)z, 31);
                if (h % 3 == 0) continue;
                float cx = -totalW * 0.5f + (c + 0.5f) * cell;
                float cy = 0.136f + r * cell;
                if (h % 2 == 0)
                {
                    Raylib.DrawCylinder(new Vector3(cx, cy, 0f), 0.07f, 0.055f, 0.11f, 8, Col(195, 165, 135));
                    Raylib.DrawSphere(new Vector3(cx, cy + 0.17f, 0f), 0.11f, Col(46, 118, 56));
                    Raylib.DrawSphere(new Vector3(cx + 0.05f, cy + 0.21f, 0.03f), 0.08f, Col(64, 142, 72));
                }
                else
                {
                    Box(cx, cy + 0.08f, 0f, 0.16f, 0.16f, 0.14f, Palette[h % Palette.Length]);
                }
            }
        }

        Rlgl.PopMatrix();
    }

    static void Plant(float x, float z, float s)
    {
        Raylib.DrawCylinder(new Vector3(x, 0, z), 0.22f * s, 0.16f * s, 0.38f * s, 10, Col(140, 82, 52));
        Raylib.DrawSphere(new Vector3(x, 0.58f * s, z), 0.26f * s, Col(42, 108, 54));
        Raylib.DrawSphere(new Vector3(x + 0.14f * s, 0.80f * s, z + 0.06f * s), 0.20f * s, Col(56, 132, 68));
        Raylib.DrawSphere(new Vector3(x - 0.12f * s, 0.85f * s, z - 0.06f * s), 0.18f * s, Col(38, 96, 48));
    }

    static void Mug(float x, float z, Color c)
    {
        Raylib.DrawCylinder(new Vector3(x, 0.745f, z), 0.05f, 0.045f, 0.09f, 12, c);
        Raylib.DrawCylinder(new Vector3(x, 0.832f, z), 0.04f, 0.04f, 0.004f, 12, Col(60, 35, 20));
        Box(x + 0.065f, 0.79f, z, 0.03f, 0.05f, 0.015f, c);
    }

    public static void DrawCafe(float time, Vector3? viewerPos = null, string speaker = "")
    {
        Texture2D floorWoodTex = GetWorldTex("Floor/Horror_Floor_03-512x512.png");
        Texture2D wallBrickTex = GetWorldTex("Brick/Horror_Brick_01-512x512.png");
        Texture2D marbleTopTex = GetWorldTex("Stone/Horror_Stone_02-512x512.png");
        Texture2D gardenStoneTex = GetWorldTex("Stone/Horror_Stone_04-512x512.png");

        // 1. Sàn gỗ tối bóng rộng rãi (19m x 18m)
        DrawTexturedFloor(new Vector3(0f, 0.002f, 1.5f), 19f, 18f, floorWoodTex, 1.8f, Col(145, 112, 86));

        // 2. Trần cao màu trầm + hệ dầm gỗ & 6 cụm đèn lồng chụp tròn ấm áp
        Box(0f, 3.90f, 1.5f, 19f, 0.20f, 18f, Col(24, 21, 19));
        for (float bz = -4.5f; bz <= 8.5f; bz += 4.2f)
            Box(0f, 3.68f, bz, 19f, 0.24f, 0.34f, Col(34, 26, 20));
        for (float bx = -6.0f; bx <= 6.0f; bx += 6.0f)
            Box(bx, 3.68f, 1.5f, 0.34f, 0.24f, 18f, Col(34, 26, 20));

        DrumLightCluster(0.0f, 0.2f);
        DrumLightCluster(-4.2f, 1.8f);
        DrumLightCluster(4.2f, 1.8f);
        DrumLightCluster(-0.8f, 4.8f);
        DrumLightCluster(4.0f, 5.4f);
        DrumLightCluster(-4.8f, 6.2f);

        // 3. Tường bao quanh sảnh cà phê
        Color wallTint = Col(140, 112, 92);
        TexBox(0f, 1.95f, -7.5f, 19f, 3.9f, 0.25f, wallBrickTex, 2.0f, wallTint);
        TexBox(-9.5f, 1.95f, 1.5f, 0.25f, 3.9f, 18f, wallBrickTex, 2.0f, wallTint);
        TexBox(9.5f, 1.95f, 1.5f, 0.25f, 3.9f, 18f, wallBrickTex, 2.0f, wallTint);

        // Vách kính phía sau nhìn ra sân vườn cây xanh (Z = 10.2)
        Box(0f, 3.45f, 10.2f, 19f, 0.9f, 0.25f, Col(30, 24, 20));
        for (float gx = -9.0f; gx <= 9.0f; gx += 3.0f)
            Box(gx, 1.5f, 10.2f, 0.12f, 3.0f, 0.16f, Col(32, 26, 22));
        Box(0f, 1.5f, 10.2f, 18.8f, 3.0f, 0.04f, Col(140, 185, 165, 38));

        // Khu vườn ngoài kính + 2 trụ đèn đứng phát sáng vàng cam hai bên lối cửa
        DrawTexturedFloor(new Vector3(0f, -0.02f, 12.8f), 22f, 5.5f, gardenStoneTex, 2.0f, Col(45, 58, 48));
        for (float px = -8.2f; px <= 8.2f; px += 1.6f)
            Plant(px, 11.4f + (MathF.Abs(px) % 1.2f), 1.45f);

        foreach (float lx in new[] { -1.45f, 2.55f })
        {
            Box(lx, 0.45f, 9.75f, 0.34f, 0.90f, 0.34f, Col(55, 36, 24));
            Raylib.DrawCylinder(new Vector3(lx, 0.92f, 9.75f), 0.14f, 0.14f, 0.56f, 14, Col(255, 195, 105));
            Raylib.DrawSphere(new Vector3(lx, 1.22f, 9.75f), 0.22f, Col(255, 205, 115, 55));
        }

        // 4. Quầy bar pha chế sáng đèn ở góc trái phía xa
        Box(-7.4f, 1.95f, 8.2f, 4.2f, 3.9f, 3.6f, Col(245, 228, 195));
        TexBox(-7.1f, 0.52f, 6.4f, 4.4f, 1.04f, 0.72f, floorWoodTex, 1.2f, Col(185, 125, 78));
        TexBox(-7.1f, 1.06f, 6.4f, 4.5f, 0.06f, 0.80f, marbleTopTex, 1.2f, Col(245, 238, 225));
        Plant(-8.5f, 5.8f, 1.35f);

        // 5. Cột ốp nan gỗ dọc trung tâm & hệ kệ ô vuông bày cây xanh phân khu (chừa lối đi rộng >= 1.8m)
        SlattedPillar(-2.1f, 3.7f, 3.8f);
        CubeDividerShelf(-5.4f, 5.1f, 0f, 5, 5);
        CubeDividerShelf(4.4f, 6.8f, -10f, 5, 5);
        CubeDividerShelf(7.4f, 3.2f, -90f, 4, 5);

        // 6. Các bộ bàn ghế lounge bố trí giãn cách thoáng đãng + NPC khách ngồi trò chuyện
        Color brownArmchair = Col(108, 72, 46);
        Color tanArmchair = Col(148, 108, 74);
        Color navySofa = Col(36, 54, 86);
        Color darkBrownSofa = Col(78, 52, 36);

        // --- Cụm 1: Sofa xanh navy bên phải (X = 4.5, Z = 1.6) - cách bàn Tú hơn 3m ---
        DrawLoungeTable(4.5f, 1.6f, 1.35f, 0.85f, marbleTopTex);
        DrawLoungeSofa(new Vector3(4.5f, 0f, 0.50f), 0f, 2.15f, navySofa);
        DrawLoungeSofa(new Vector3(4.5f, 0f, 2.70f), 180f, 2.15f, navySofa);
        Mug(4.25f, 1.45f, Col(235, 235, 230));
        Mug(4.80f, 1.75f, Col(190, 100, 80));

        Vector3 g1Pos = new(3.95f, 0f, 0.58f);
        Vector3 g2Pos = new(5.05f, 0f, 0.58f);
        Vector3 g3Pos = new(4.45f, 0f, 2.62f);
        DrawSeatedPerson(g1Pos, 0f, Col(175, 130, 95), Col(35, 28, 25), g3Pos + new Vector3(0, 1.38f, 0), isFemale: true);
        DrawSeatedPerson(g2Pos, 0f, Col(55, 75, 110), Col(28, 28, 32), g3Pos + new Vector3(0, 1.38f, 0), isFemale: false);
        DrawSeatedPerson(g3Pos, 180f, Col(145, 70, 75), Col(45, 32, 26), g1Pos + new Vector3(0, 1.38f, 0), isFemale: true);

        // --- Cụm 2: Sofa xanh navy góc sau bên phải (X = 5.4, Z = 4.9) ---
        DrawLoungeTable(5.4f, 4.9f, 1.25f, 0.80f, marbleTopTex);
        DrawLoungeSofa(new Vector3(5.4f, 0f, 3.90f), 0f, 1.95f, navySofa);
        DrawLoungeSofa(new Vector3(5.4f, 0f, 5.85f), 180f, 1.95f, navySofa);
        DrawSeatedPerson(new Vector3(5.00f, 0f, 3.98f), 0f, Col(85, 115, 95), Col(30, 30, 34), new Vector3(5.4f, 1.38f, 5.75f));

        // --- Cụm 3: Sofa & ghế bành nâu bên trái (X = -5.5, Z = 1.4) ---
        DrawLoungeTable(-5.5f, 1.4f, 1.25f, 0.85f, marbleTopTex);
        DrawLoungeSofa(new Vector3(-5.5f, 0f, 2.50f), 180f, 2.05f, darkBrownSofa);
        DrawTubArmchair(new Vector3(-6.05f, 0f, 0.35f), 20f, brownArmchair);
        DrawTubArmchair(new Vector3(-4.85f, 0f, 0.35f), -20f, tanArmchair);
        Mug(-5.30f, 1.30f, Col(235, 235, 230));

        Vector3 g4Pos = new(-5.95f, 0f, 2.42f);
        Vector3 g5Pos = new(-5.00f, 0f, 2.42f);
        Vector3 g6Pos = new(-6.05f, 0f, 0.35f);
        DrawSeatedPerson(g4Pos, 180f, Col(115, 85, 65), Col(32, 28, 26), g6Pos + new Vector3(0, 1.38f, 0), isFemale: false);
        DrawSeatedPerson(g5Pos, 180f, Col(185, 165, 140), Col(48, 34, 26), g6Pos + new Vector3(0, 1.38f, 0), isFemale: true);
        DrawSeatedPerson(g6Pos, 20f, Col(68, 92, 118), Col(28, 28, 32), g4Pos + new Vector3(0, 1.38f, 0), isFemale: false);

        // --- Cụm 4: Bàn ghế bành khu vườn phía sau (X = 0.5, Z = 6.9) ---
        DrawLoungeTable(0.5f, 6.9f, 1.15f, 0.85f, marbleTopTex);
        DrawTubArmchair(new Vector3(-0.45f, 0f, 6.9f), 90f, tanArmchair);
        DrawTubArmchair(new Vector3(1.45f, 0f, 6.9f), -90f, brownArmchair);
        DrawSeatedPerson(new Vector3(-0.45f, 0f, 6.9f), 90f, Col(135, 95, 115), Col(38, 28, 24), new Vector3(1.45f, 1.38f, 6.9f), isFemale: true);
        DrawSeatedPerson(new Vector3(1.45f, 0f, 6.9f), -90f, Col(75, 85, 95), Col(30, 30, 34), new Vector3(-0.45f, 1.38f, 6.9f), isFemale: false);

        // --- Cụm 5 & 6: Hai bộ bàn ghế ở khu vực phía trước tạo chiều sâu ---
        DrawLoungeTable(4.8f, -3.1f, 1.15f, 0.80f, marbleTopTex);
        DrawTubArmchair(new Vector3(3.90f, 0f, -3.1f), 90f, brownArmchair);
        DrawTubArmchair(new Vector3(5.70f, 0f, -3.1f), -90f, tanArmchair);
        DrawSeatedPerson(new Vector3(5.70f, 0f, -3.1f), -90f, Col(95, 125, 105), Col(32, 30, 28), new Vector3(3.90f, 1.38f, -3.1f));

        DrawLoungeTable(-4.8f, -3.1f, 1.15f, 0.80f, marbleTopTex);
        DrawTubArmchair(new Vector3(-5.70f, 0f, -3.1f), 90f, tanArmchair);
        DrawTubArmchair(new Vector3(-3.90f, 0f, -3.1f), -90f, brownArmchair);
        DrawSeatedPerson(new Vector3(-5.70f, 0f, -3.1f), 90f, Col(160, 110, 75), Col(40, 30, 25), new Vector3(-3.90f, 1.38f, -3.1f), isFemale: true);

        // 7. Hai nhân viên phục vụ bưng khay đi chậm rãi trên hành lang rộng, không va chạm vật thể
        Vector3[] waiterLoop1 =
        [
            new Vector3(-3.35f, 0f,  5.00f),
            new Vector3(-3.35f, 0f, -1.10f),
            new Vector3( 2.35f, 0f, -1.10f),
            new Vector3( 2.35f, 0f,  4.75f),
            new Vector3(-0.80f, 0f,  4.75f)
        ];
        var w1 = EvalLoopWalker(waiterLoop1, time, 0.72f);
        DrawCafeWaiter(w1.Pos, w1.YawDeg, w1.Phase, isFemale: true, Col(92, 56, 36));

        Vector3[] waiterLoop2 =
        [
            new Vector3(-2.60f, 0f, -4.85f),
            new Vector3( 2.65f, 0f, -4.85f),
            new Vector3( 2.65f, 0f, -1.75f),
            new Vector3(-2.60f, 0f, -1.75f)
        ];
        var w2 = EvalLoopWalker(waiterLoop2, time + 6.5f, 0.64f);
        DrawCafeWaiter(w2.Pos, w2.YawDeg, w2.Phase, isFemale: false, Col(38, 42, 48));

        // 8. Bàn trung tâm của nhóm Tú - Dũng - Hưng - Đen (giữ nguyên tọa độ chuẩn để khớp góc camera!)
        DrawLoungeTable(0f, 0f, 1.65f, 1.20f, marbleTopTex);

        // Đồ uống & điện thoại trên bàn nhóm Tú
        Box(0, 0.752f, -0.35f, 0.10f, 0.016f, 0.19f, Col(240, 215, 110));
        Box(0, 0.761f, -0.35f, 0.085f, 0.002f, 0.17f, Col(70, 100, 150));
        Mug(0.24f, 0.28f, Col(235, 235, 230));
        Mug(-0.52f, 0.08f, Col(180, 90, 70));
        Raylib.DrawCylinder(new Vector3(-0.26f, 0.745f, -0.25f), 0.045f, 0.04f, 0.13f, 12, Col(110, 75, 50));
        Box(-0.25f, 0.895f, -0.25f, 0.008f, 0.18f, 0.008f, Col(220, 60, 60));
        Raylib.DrawCylinder(new Vector3(0.36f, 0.745f, -0.15f), 0.14f, 0.14f, 0.012f, 16, Col(235, 235, 235));
        Box(0.36f, 0.785f, -0.15f, 0.12f, 0.05f, 0.09f, Col(240, 200, 130));
        Box(0.36f, 0.820f, -0.15f, 0.12f, 0.02f, 0.09f, Col(110, 60, 40));

        // Ghế bành sang trọng cho Tú, Dũng, Hưng, Đen
        DrawTubArmchair(new Vector3(0f, 0f, -1.15f), 0f, brownArmchair);
        DrawTubArmchair(new Vector3(0f, 0f, 1.05f), 180f, brownArmchair);
        DrawTubArmchair(new Vector3(-1.25f, 0f, 0.05f), 90f, tanArmchair);
        DrawTubArmchair(new Vector3(1.25f, 0f, 0.05f), -90f, tanArmchair);

        float b0 = 0.012f * MathF.Sin(time * 2.4f);
        float b1 = 0.012f * MathF.Sin(time * 2.0f + 1.7f);
        float b2 = 0.012f * MathF.Sin(time * 2.8f + 3.1f);

        Vector3 dungPos = new(0, b0, 1.05f);
        Vector3 hungPos = new(-1.25f, b1, 0.05f);
        Vector3 denPos = new(1.25f, b2, 0.05f);

        Vector3 dungHead = dungPos + new Vector3(0, 1.38f, 0);
        Vector3 hungHead = hungPos + new Vector3(0, 1.38f, 0);
        Vector3 denHead = denPos + new Vector3(0, 1.38f, 0);

        Vector3? dungLook = speaker switch
        {
            "Hưng" => hungHead,
            "Đen" => denHead,
            _ => viewerPos
        };
        Vector3? hungLook = speaker switch
        {
            "Dũng" => dungHead,
            "Đen" => denHead,
            _ => viewerPos
        };
        Vector3? denLook = speaker switch
        {
            "Dũng" => dungHead,
            "Hưng" => hungHead,
            _ => viewerPos
        };

        DrawSeatedPerson(dungPos, 180, Col(195, 85, 45), Col(30, 28, 32), dungLook, FaceId.Friend1);
        DrawSeatedPerson(hungPos, 90, Col(55, 115, 165), Col(45, 35, 30), hungLook, FaceId.Friend2);
        DrawSeatedPerson(denPos, -90, Col(65, 145, 95), Col(35, 30, 28), denLook, FaceId.Friend3);

        // Ly thủy tinh trong suốt đặt sau cùng để alpha blending chuẩn
        Raylib.DrawCylinder(new Vector3(-0.26f, 0.745f, -0.25f), 0.05f, 0.045f, 0.14f, 12, Col(220, 240, 250, 60));
    }

    static void Product(float x, float y, float z, int hs, bool lit)
    {
        Color c = Shade(Palette[hs % Palette.Length], lit, 0.3f);
        Color white = Shade(Col(240, 240, 240), lit, 0.3f);
        switch ((hs / 8) % 4)
        {
            case 0:
                float h = 0.18f + ((hs / 64) % 3) * 0.03f;
                Box(x, y + h / 2, z, 0.16f, h, 0.15f, c);
                Box(x, y + h * 0.55f, z, 0.162f, 0.05f, 0.152f, white);
                break;
            case 1:
                Raylib.DrawCylinder(new Vector3(x, y, z), 0.04f, 0.04f, 0.13f, 8, c);
                break;
            case 2:
                Raylib.DrawCylinder(new Vector3(x, y, z), 0.035f, 0.035f, 0.2f, 8, c);
                Box(x, y + 0.22f, z, 0.03f, 0.04f, 0.03f, white);
                break;
            default:
                Box(x, y + 0.11f, z, 0.15f, 0.22f, 0.07f, c);
                break;
        }
    }

    static void Gondola(float cx, float cz, float len, bool lit, int seed)
    {
        Texture2D metalTex = GetWorldTex("Metal/Horror_Metal_10-512x512.png");
        Color frame = Shade(Col(140, 145, 155), lit, 0.32f);
        Color board = Shade(Col(215, 218, 225), lit, 0.28f);
        Color lip = Shade(Col(245, 195, 50), lit, 0.3f);
        Color header = seed == 0 ? Col(215, 55, 45) : Col(235, 130, 40);

        TexBox(cx, 0.06f, cz, 0.58f, 0.12f, len, metalTex, 1.0f, frame);
        TexBox(cx, 0.92f, cz, 0.05f, 1.84f, len, metalTex, 1.2f, frame);
        Box(cx, 1.80f, cz, 0.58f, 0.10f, len, Shade(header, lit, 0.3f));
        TexBox(cx, 0.92f, cz - len / 2, 0.58f, 1.84f, 0.05f, metalTex, 1.2f, frame);
        TexBox(cx, 0.92f, cz + len / 2, 0.58f, 1.84f, 0.05f, metalTex, 1.2f, frame);

        for (int l = 0; l < 4; l++)
        {
            float by = 0.28f + l * 0.38f;
            for (int sd = -1; sd <= 1; sd += 2)
            {
                TexBox(cx + sd * 0.15f, by, cz, 0.26f, 0.03f, len, metalTex, 1.0f, board);
                Box(cx + sd * 0.278f, by + 0.04f, cz, 0.012f, 0.07f, len, lip);

                int n = (int)((len - 0.3f) / 0.21f);
                for (int i = 0; i <= n; i++)
                {
                    int hs = Hash(l + seed * 7, sd + 2, i);
                    if (hs % 9 == 0) continue;
                    Product(cx + sd * 0.16f, by + 0.015f, cz - len / 2 + 0.17f + i * 0.21f, hs, lit);
                }
            }
        }
    }

    static void RollingSnackRack(float cx, float cz, float yawDeg, bool lit)
    {
        Color blackMetal = Shade(Col(24, 26, 30), lit, 0.35f);
        Rlgl.PushMatrix();
        Rlgl.Translatef(cx, 0f, cz);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        foreach (float wx in new[] { -0.34f, 0.34f })
            foreach (float wz in new[] { -0.22f, 0.22f })
                Raylib.DrawCylinder(new Vector3(wx, 0f, wz), 0.045f, 0.045f, 0.06f, 8, blackMetal);

        Box(0f, 0.86f, 0f, 0.04f, 1.60f, 0.52f, blackMetal);
        Box(0f, 0.86f, -0.26f, 0.78f, 1.60f, 0.04f, blackMetal);
        Box(0f, 0.86f, 0.26f, 0.78f, 1.60f, 0.04f, blackMetal);
        Box(0f, 1.68f, 0f, 0.80f, 0.08f, 0.56f, Shade(Col(215, 45, 45), lit, 0.35f));

        for (int shelf = 0; shelf < 5; shelf++)
        {
            float sy = 0.22f + shelf * 0.29f;
            Box(0f, sy, 0f, 0.76f, 0.025f, 0.50f, blackMetal);
            for (int sd = -1; sd <= 1; sd += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    Color bagCol = Shade(Palette[(shelf * 2 + i + (sd > 0 ? 1 : 0)) % Palette.Length], lit, 0.35f);
                    float bx = -0.27f + i * 0.18f;
                    Box(bx, sy + 0.12f, sd * 0.13f, 0.14f, 0.20f, 0.12f, bagCol);
                    Box(bx, sy + 0.13f, sd * 0.192f, 0.10f, 0.06f, 0.004f, Shade(Col(250, 245, 220), lit, 0.35f));
                }
            }
        }

        Rlgl.PopMatrix();
    }

    static void LeftWoodIslandCounter(bool lit)
    {
        Texture2D woodTex = GetWorldTex("Floor/Horror_Floor_03-512x512.png");
        Texture2D stoneTopTex = GetWorldTex("Stone/Horror_Stone_02-512x512.png");

        Color blackFrame = Shade(Col(22, 24, 28), lit, 0.35f);
        Color warmWood = Shade(Col(235, 185, 130), lit, 0.34f);
        Color counterTop = Shade(Col(235, 232, 225), lit, 0.32f);
        Color steelGray = Shade(Col(145, 150, 158), lit, 0.35f);

        Box(-4.35f, 0.48f, 0.5f, 1.30f, 0.96f, 3.60f, blackFrame);
        TexBox(-4.35f, 0.99f, 0.5f, 1.38f, 0.06f, 3.68f, stoneTopTex, 1.2f, counterTop);

        for (int p = 0; p < 5; p++)
        {
            float pz = -0.85f + p * 0.68f;
            TexBox(-3.69f, 0.48f, pz, 0.02f, 0.78f, 0.58f, woodTex, 0.9f, warmWood);
        }

        Box(-3.68f, 0.62f, -0.25f, 0.03f, 0.36f, 0.54f, steelGray);
        Box(-3.66f, 0.64f, -0.25f, 0.01f, 0.24f, 0.36f, Col(25, 28, 34));

        float caseZ = 1.65f;
        Box(-4.25f, 1.66f, caseZ, 0.76f, 0.08f, 0.96f, blackFrame);
        foreach (float dx in new[] { -0.35f, 0.35f })
            foreach (float dz in new[] { -0.45f, 0.45f })
                Box(-4.25f + dx, 1.34f, caseZ + dz, 0.035f, 0.64f, 0.035f, blackFrame);

        for (int t = 0; t < 2; t++)
        {
            float ty = 1.18f + t * 0.22f;
            Box(-4.25f, ty, caseZ, 0.68f, 0.02f, 0.88f, steelGray);
            for (int b = -2; b <= 2; b++)
                Raylib.DrawSphere(new Vector3(-4.25f, ty + 0.045f, caseZ + b * 0.16f), 0.055f,
                    Shade(Col(228, 165, 78), lit, 0.35f));
        }

        for (int c = 0; c < 4; c++)
            Raylib.DrawCylinder(new Vector3(-4.20f, 1.02f, 0.75f - c * 0.18f), 0.045f, 0.04f, 0.16f, 10,
                Shade(Col(240, 240, 235), lit, 0.35f));
        Box(-4.35f, 1.32f, -0.45f, 0.56f, 0.60f, 0.90f, blackFrame);
    }

    static void LeftBeverageFountainWall(bool lit)
    {
        Texture2D woodTex = GetWorldTex("Floor/Horror_Floor_03-512x512.png");
        Texture2D stoneTopTex = GetWorldTex("Stone/Horror_Stone_02-512x512.png");

        Color blackBase = Shade(Col(22, 24, 28), lit, 0.35f);
        Color warmWood = Shade(Col(235, 185, 130), lit, 0.34f);
        Color counterTop = Shade(Col(235, 232, 225), lit, 0.32f);

        Box(-6.85f, 0.48f, -0.2f, 1.15f, 0.96f, 6.2f, blackBase);
        TexBox(-6.85f, 0.99f, -0.2f, 1.22f, 0.06f, 6.3f, stoneTopTex, 1.2f, counterTop);
        for (int p = 0; p < 8; p++)
            TexBox(-6.26f, 0.48f, -2.75f + p * 0.72f, 0.02f, 0.78f, 0.62f, woodTex, 0.9f, warmWood);

        for (int m = 0; m < 4; m++)
        {
            float mz = -2.2f + m * 1.35f;
            Box(-6.75f, 1.44f, mz, 0.64f, 0.84f, 1.05f, Shade(Col(35, 38, 44), lit, 0.4f));
            Box(-6.41f, 1.44f, mz, 0.03f, 0.52f, 0.88f, lit ? Palette[(m + 2) % Palette.Length] : Col(35, 45, 65));
        }

        Color[] bannerColors =
        [
            Col(32, 95, 215),
            Col(235, 145, 42),
            Col(38, 120, 225),
            Col(48, 155, 78)
        ];
        for (int b = 0; b < 4; b++)
        {
            float bz = -2.2f + b * 1.48f;
            Box(-7.36f, 2.52f, bz, 0.05f, 1.05f, 1.40f, Shade(bannerColors[b], lit, 0.35f));
            Box(-7.33f, 2.52f, bz, 0.01f, 0.38f, 0.78f, Shade(Col(248, 246, 238), lit, 0.35f));
        }
    }

    static void BeverageCoolerUnit(float cx, float cz, float yawDeg, bool lit, bool isRedCocaCola = false)
    {
        Texture2D metalTex = GetWorldTex("Metal/Horror_Metal_10-512x512.png");
        Rlgl.PushMatrix();
        Rlgl.Translatef(cx, 0f, cz);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        float front = 0.35f;
        Color silver = Shade(Col(215, 220, 228), lit, 0.35f);
        Color bodyCol = isRedCocaCola ? Shade(Col(225, 48, 52), lit, 0.38f) : Shade(Col(110, 120, 132), lit, 0.4f);

        TexBox(0f, 1.25f, 0f, 2.5f, 2.5f, 0.7f, metalTex, 1.4f, bodyCol);
        Box(0f, 1.25f, front - 0.005f, 2.25f, 2.08f, 0.02f, lit ? Col(125, 198, 238) : Col(28, 48, 64));
        Box(0f, 2.36f, front + 0.04f, 2.25f, 0.06f, 0.04f, lit ? Col(235, 248, 255) : Col(60, 90, 110));
        Box(0f, 2.62f, front + 0.02f, 2.48f, 0.10f, 0.06f, lit ? Col(45, 105, 255) : Col(18, 35, 85));

        for (int l = 0; l < 4; l++)
        {
            float y = 0.45f + l * 0.5f;
            Box(0f, y, front + 0.06f, 2.25f, 0.03f, 0.22f, silver);
            for (int i = 0; i < 14; i++)
            {
                float x = -0.98f + i * 0.15f;
                Color c = Shade(Palette[Hash(l, i, 5) % 8], lit, 0.5f);
                Box(x, y + 0.155f, front + 0.07f, 0.07f, 0.26f, 0.07f, c);
                Box(x, y + 0.305f, front + 0.07f, 0.04f, 0.03f, 0.04f, Shade(Col(240, 240, 240), lit, 0.5f));
            }
        }

        foreach (float dx in new[] { -1.11f, -0.37f, 0.37f, 1.11f })
            Box(dx, 1.25f, front + 0.13f, 0.04f, 2.08f, 0.04f, silver);

        Rlgl.PopMatrix();
    }

    static void MiniBlueCooler(float cx, float cz, bool lit)
    {
        Texture2D metalTex = GetWorldTex("Metal/Horror_Metal_10-512x512.png");
        Color blueBody = Shade(Col(45, 125, 235), lit, 0.35f);
        TexBox(cx, 0.65f, cz, 0.72f, 1.30f, 0.68f, metalTex, 1.0f, blueBody);
        Box(cx, 0.68f, cz + 0.345f, 0.60f, 1.05f, 0.015f, lit ? Col(140, 210, 245) : Col(35, 55, 75));
        for (int s = 0; s < 3; s++)
        {
            float sy = 0.32f + s * 0.32f;
            for (int b = -2; b <= 2; b++)
                Raylib.DrawCylinder(new Vector3(cx + b * 0.10f, sy, cz + 0.26f), 0.03f, 0.03f, 0.16f, 8,
                    Shade(Palette[(s + b + 4) % Palette.Length], lit, 0.45f));
        }
    }

    static void WallRack(float cx, float wallZ, bool lit, bool hasWelcomeSign = false)
    {
        Texture2D woodTex = GetWorldTex("Floor/Horror_Floor_03-512x512.png");
        float w = 2.8f;
        Color wood = Shade(Col(175, 135, 100), lit, 0.32f);
        Box(cx, 1.45f, wallZ + 0.12f, w, 1.6f, 0.02f, Shade(Col(35, 32, 38), lit, 0.4f));

        if (hasWelcomeSign)
        {
            Box(cx, 2.58f, wallZ + 0.14f, 2.2f, 0.34f, 0.04f, Shade(Col(218, 92, 38), lit, 0.35f));
            Box(cx, 2.58f, wallZ + 0.165f, 1.5f, 0.10f, 0.01f, Shade(Col(250, 246, 238), lit, 0.35f));
        }

        for (int l = 0; l < 4; l++)
        {
            float y = 0.82f + l * 0.40f;
            TexBox(cx, y, wallZ + 0.22f, w, 0.03f, 0.24f, woodTex, 1.2f, wood);
            for (int i = 0; i < 13; i++)
            {
                int hs = Hash(l, i, 77);
                if (hs % 7 == 0) continue;
                Box(cx - w / 2 + 0.15f + i * 0.2f, y + 0.1f, wallZ + 0.22f, 0.15f, 0.17f + (hs % 3) * 0.015f, 0.14f,
                    Shade(Palette[hs % 8], lit, 0.3f));
            }
        }
    }

    static void Hand(float x, float y, float z, float angCW, float len, float w, Color c)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(x, y, z);
        Rlgl.Rotatef(-angCW, 0, 0, 1);
        Raylib.DrawCube(new Vector3(0, len / 2, 0), w, len, 0.012f, c);
        Rlgl.PopMatrix();
    }

    static void WallClock(float x, float y, float z, string t, bool lit)
    {
        int h = 0, m = 0;
        string[] p = t.Split(':');
        if (p.Length == 2) { int.TryParse(p[0], out h); int.TryParse(p[1], out m); }
        float hourAng = ((h % 12) + m / 60f) * 30f;
        float minAng = m * 6f;

        Rlgl.PushMatrix();
        Rlgl.Translatef(x, y, z);
        Rlgl.Rotatef(90, 1, 0, 0);
        Raylib.DrawCylinder(new Vector3(0, -0.01f, 0), 0.26f, 0.26f, 0.04f, 24, Shade(Col(30, 30, 34), lit, 0.4f));
        Raylib.DrawCylinder(new Vector3(0, 0, 0), 0.23f, 0.23f, 0.045f, 24, Shade(Col(235, 235, 225), lit, 0.3f));
        Rlgl.PopMatrix();

        Color hand = Shade(Col(20, 20, 24), lit, 0.5f);
        Hand(x, y, z + 0.05f, hourAng, 0.12f, 0.025f, hand);
        Hand(x, y, z + 0.052f, minAng, 0.19f, 0.016f, hand);
    }

    static void CeilingLight(float x, float z, bool lit)
    {
        Box(x, 3.665f, z, 1.9f, 0.07f, 0.52f, Shade(Col(120, 122, 128), lit, 0.4f));
        if (lit) Box(x, 3.63f, z, 1.8f, 0.02f, 0.42f, Col(252, 252, 248));
    }

    static void StreetLamp(float x, float z)
    {
        Texture2D metalTex = GetWorldTex("Metal/Horror_Metal_10-512x512.png");
        Color metal = Col(95, 100, 108);
        Raylib.DrawCylinder(new Vector3(x, 0, z), 0.06f, 0.08f, 3.4f, 8, Col(40, 42, 46));
        TexBox(x - 0.4f, 3.4f, z, 0.9f, 0.06f, 0.08f, metalTex, 0.8f, metal);
        Box(x - 0.8f, 3.36f, z, 0.4f, 0.08f, 0.22f, Col(255, 225, 150));

        Box(x, 1.55f, z - 0.082f, 0.24f, 0.32f, 0.01f, Col(240, 225, 165));
        Box(x, 1.66f, z - 0.088f, 0.18f, 0.03f, 0.004f, Col(190, 40, 40));
        Box(x, 1.54f, z - 0.088f, 0.16f, 0.14f, 0.004f, Col(45, 45, 50));
    }

    public static void DrawStore(bool lit, bool doorLocked, float time, string clockTime, bool phoneRinging, float daylight = 0f)
    {
        const float minX = -7.5f, maxX = 7.5f;
        const float minZ = -6.5f, maxZ = 7.5f;
        const float storeW = maxX - minX;
        const float storeL = maxZ - minZ;

        Texture2D storeFloorTex = GetWorldTex("Floor/Horror_Floor_12-512x512.png");
        Texture2D innerWallTex = GetWorldTex("Wall/Horror_Wall_01-512x512.png");
        Texture2D facadeBrickTex = GetWorldTex("Brick/Horror_Brick_11-512x512.png");
        Texture2D ceilingTex = GetWorldTex("Wall/Horror_Wall_04-512x512.png");
        Texture2D roadTex = GetWorldTex("Stone/Horror_Stone_01-512x512.png");
        Texture2D sidewalkTex = GetWorldTex("Stone/Horror_Stone_04-512x512.png");
        Texture2D counterWood = GetWorldTex("Floor/Horror_Floor_03-512x512.png");
        Texture2D counterStone = GetWorldTex("Stone/Horror_Stone_02-512x512.png");
        Texture2D metalTex = GetWorldTex("Metal/Horror_Metal_10-512x512.png");

        Color floorTint = Shade(Col(235, 228, 218), lit, 0.24f);
        Color innerWallTint = Shade(Col(225, 214, 198), lit, 0.22f);
        Color facadeTint = Shade(Col(215, 205, 192), lit, 0.24f);
        Color trimDark = Shade(Col(55, 58, 66), lit, 0.3f);

        DrawTexturedFloor(new Vector3(0, 0.002f, 0.5f), storeW, storeL, storeFloorTex, 1.5f, floorTint);
        Box(0, 0.012f, maxZ - 0.65f, 2.1f, 0.02f, 0.95f, Shade(Col(32, 34, 38), lit, 0.3f));

        Color roadTint = LerpCol(Col(85, 90, 102), Col(185, 195, 212), daylight);
        Color sidewalkTint = LerpCol(Col(135, 140, 150), Col(220, 225, 235), daylight);
        DrawTexturedFloor(new Vector3(0, -0.02f, 12.5f), 28f, 12f, roadTex, 2.6f, roadTint);
        TexBox(0, -0.005f, 8.7f, 28f, 0.03f, 2.4f, sidewalkTex, 1.8f, sidewalkTint);
        TexBox(0, 0.005f, 9.95f, 28f, 0.05f, 0.12f, sidewalkTex, 1.0f, LerpCol(Col(160, 162, 170), Col(230, 232, 240), daylight));
        for (int x = -12; x <= 12; x += 3) Box(x, -0.015f, 13.0f, 1.4f, 0.006f, 0.12f, Col(200, 190, 120));
        StreetLamp(-2.1f, 9.1f);

        TexBox(0, 3.8f, 0.5f, storeW, 0.2f, storeL, ceilingTex, 2.2f, Shade(Col(235, 235, 240), lit, 0.20f));
        TexBox(0, 3.48f, -1.2f, storeW - 0.2f, 0.36f, 0.24f, counterWood, 1.5f, Shade(Col(220, 175, 115), lit, 0.26f));
        Box(-0.35f, 3.16f, 0.6f, 0.04f, 0.28f, 0.04f, Col(28, 30, 34));
        Box(-0.35f, 2.94f, 0.6f, 0.44f, 0.28f, 0.06f, Shade(Col(24, 26, 30), lit, 0.4f));
        Box(-0.35f, 2.94f, 0.635f, 0.38f, 0.22f, 0.008f, lit ? Col(115, 175, 225) : Col(25, 45, 65));

        foreach (float lx in new[] { -4.6f, -1.5f, 1.6f, 4.7f })
            foreach (float lz in new[] { -4.2f, -1.2f, 1.8f, 4.8f })
                CeilingLight(lx, lz, lit);

        TexBox(0, 1.9f, minZ, storeW, 3.8f, 0.2f, innerWallTex, 2.2f, innerWallTint);
        TexBox(minX, 1.9f, 0.5f, 0.2f, 3.8f, storeL, innerWallTex, 2.2f, innerWallTint);
        TexBox(maxX, 1.9f, 0.5f, 0.2f, 3.8f, storeL, innerWallTex, 2.2f, innerWallTint);

        TexBox(-4.35f, 1.9f, maxZ, 6.3f, 3.8f, 0.2f, facadeBrickTex, 1.8f, facadeTint);
        TexBox(4.35f, 1.9f, maxZ, 6.3f, 3.8f, 0.2f, facadeBrickTex, 1.8f, facadeTint);
        TexBox(0f, 3.2f, maxZ, 2.4f, 1.2f, 0.2f, facadeBrickTex, 1.8f, facadeTint);

        Box(0, 0.07f, minZ + 0.11f, storeW - 0.2f, 0.14f, 0.02f, trimDark);
        Box(minX + 0.11f, 0.07f, 0.5f, 0.02f, 0.14f, storeL - 0.2f, trimDark);
        Box(maxX - 0.11f, 0.07f, 0.5f, 0.02f, 0.14f, storeL - 0.2f, trimDark);

        Color alu = Shade(Col(205, 210, 220), lit, 0.35f);
        TexBox(-1.2f, 1.3f, maxZ, 0.10f, 2.6f, 0.14f, metalTex, 1.0f, alu);
        TexBox(1.2f, 1.3f, maxZ, 0.10f, 2.6f, 0.14f, metalTex, 1.0f, alu);
        TexBox(0, 0.06f, maxZ, 2.4f, 0.12f, 0.14f, metalTex, 1.0f, alu);
        TexBox(0, 2.6f, maxZ, 2.4f, 0.10f, 0.14f, metalTex, 1.0f, alu);
        TexBox(0, 1.3f, maxZ, 0.06f, 2.6f, 0.10f, metalTex, 1.0f, alu);
        foreach (float hx in new[] { -0.2f, 0.2f })
        {
            Box(hx, 1.1f, maxZ + 0.08f, 0.04f, 0.6f, 0.04f, alu);
            Box(hx, 1.1f, maxZ - 0.08f, 0.04f, 0.6f, 0.04f, alu);
        }
        Box(0, 1.15f, maxZ - 0.07f, 0.10f, 0.04f, 0.06f, doorLocked ? Col(210, 50, 50) : Col(70, 200, 90));

        Box(0.65f, 2.0f, maxZ - 0.09f, 0.48f, 0.24f, 0.015f, Col(205, 38, 38));
        Box(0.65f, 2.0f, maxZ - 0.102f, 0.32f, 0.04f, 0.008f, Col(245, 245, 245));
        Box(0.65f, 2.0f, maxZ + 0.09f, 0.48f, 0.24f, 0.015f, Col(32, 165, 82));
        Box(0.65f, 2.0f, maxZ + 0.102f, 0.32f, 0.04f, 0.008f, Col(245, 245, 245));

        Box(0f, 3.15f, maxZ - 0.115f, 2.25f, 0.56f, 0.02f, Col(195, 36, 36));
        Box(0f, 3.15f, maxZ - 0.128f, 1.75f, 0.12f, 0.008f, Col(250, 235, 210));
        Box(0f, 3.15f, maxZ + 0.115f, 2.25f, 0.56f, 0.02f, Col(28, 155, 78));
        Box(0f, 3.15f, maxZ + 0.128f, 1.75f, 0.12f, 0.008f, Col(250, 235, 210));

        Color cWood = Shade(Col(180, 140, 105), lit, 0.32f);
        Color cTop = Shade(Col(240, 236, 228), lit, 0.30f);
        Color green = Shade(Col(30, 140, 80), lit, 0.3f);
        TexBox(-1.4f, 0.48f, -2.15f, 3.4f, 0.96f, 0.75f, counterWood, 1.2f, cWood);
        TexBox(-1.4f, 1.01f, -2.15f, 3.48f, 0.06f, 0.83f, counterStone, 1.2f, cTop);
        Box(-1.4f, 0.6f, -1.77f, 3.0f, 0.07f, 0.02f, green);
        TexBox(-2.75f, 0.48f, -3.55f, 0.75f, 0.96f, 3.5f, counterWood, 1.2f, cWood);
        TexBox(-2.75f, 1.01f, -3.55f, 0.83f, 0.06f, 3.58f, counterStone, 1.2f, cTop);
        Box(-2.37f, 0.6f, -3.55f, 0.02f, 0.07f, 3.5f, green);

        Box(-1.1f, 1.045f, -2.1f, 0.35f, 0.008f, 0.45f, Shade(Col(245, 245, 240), lit, 0.3f));
        for (int k = 0; k < 5; k++)
            Box(-1.1f, 1.051f, -2.27f + k * 0.075f, 0.26f - (k % 2) * 0.05f, 0.002f, 0.018f, Shade(Col(90, 90, 95), lit, 0.3f));

        Box(-0.62f, 1.055f, -2.12f, 0.28f, 0.026f, 0.36f, Shade(Col(38, 68, 108), lit, 0.35f));
        Box(-0.62f, 1.070f, -2.12f, 0.22f, 0.003f, 0.12f, Shade(Col(235, 230, 210), lit, 0.35f));

        Box(-0.15f, 1.075f, -2.1f, 0.28f, 0.07f, 0.25f, Shade(Col(30, 30, 35), lit, 0.5f));
        Box(-0.15f, 1.125f, -2.1f, 0.26f, 0.035f, 0.07f, Shade(Col(22, 22, 26), lit, 0.5f));
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                Box(-0.22f + c * 0.04f, 1.113f, -2.04f + r * 0.035f, 0.025f, 0.008f, 0.02f, Shade(Col(120, 120, 130), lit, 0.5f));
        bool blink = phoneRinging && MathF.Sin(time * 14f) > 0;
        Box(-0.05f, 1.112f, -2.2f, 0.03f, 0.015f, 0.03f, blink ? Col(255, 50, 40) : Col(80, 20, 20));

        Box(-2.0f, 1.16f, -2.2f, 0.42f, 0.24f, 0.36f, Shade(Col(50, 52, 58), lit, 0.5f));
        Box(-2.0f, 1.34f, -2.3f, 0.30f, 0.14f, 0.04f, lit ? Col(120, 230, 160) : Col(40, 80, 60));
        Box(-2.0f, 1.285f, -2.12f, 0.30f, 0.01f, 0.12f, Shade(Col(90, 92, 100), lit, 0.5f));

        Box(-2.75f, 1.39f, -4.15f, 0.45f, 0.70f, 0.45f, Shade(Col(40, 42, 46), lit, 0.5f));
        Box(-2.75f, 1.55f, -3.92f, 0.30f, 0.14f, 0.02f, lit ? Col(240, 120, 60) : Col(60, 30, 20));
        Raylib.DrawCylinder(new Vector3(-2.75f, 1.04f, -3.8f), 0.04f, 0.035f, 0.07f, 8, Shade(Col(235, 235, 230), lit, 0.4f));
        Raylib.DrawSphere(new Vector3(-2.85f, 1.12f, -3.1f), 0.07f, Shade(Col(225, 170, 90), lit, 0.4f));
        Raylib.DrawSphere(new Vector3(-2.68f, 1.12f, -3.08f), 0.07f, Shade(Col(225, 170, 90), lit, 0.4f));
        Raylib.DrawSphere(new Vector3(-2.77f, 1.12f, -2.92f), 0.07f, Shade(Col(235, 190, 110), lit, 0.4f));

        WallRack(-1.3f, minZ, lit, hasWelcomeSign: true);
        WallRack(-4.5f, minZ, lit, hasWelcomeSign: false);
        WallRack(2.1f, minZ, lit, hasWelcomeSign: false);
        WallClock(-1.3f, 3.12f, minZ + 0.12f, clockTime, lit);

        LeftBeverageFountainWall(lit);
        LeftWoodIslandCounter(lit);

        MiniBlueCooler(0.85f, -2.25f, lit);

        RollingSnackRack(2.25f, 1.65f, -15f, lit);

        Gondola(3.85f, -0.8f, 4.4f, lit, 0);
        Gondola(5.35f, -0.8f, 4.4f, lit, 1);

        BeverageCoolerUnit(maxX - 0.45f, 4.6f, -90f, lit, isRedCocaCola: true);
        BeverageCoolerUnit(maxX - 0.45f, 1.8f, -90f, lit, isRedCocaCola: false);
        BeverageCoolerUnit(maxX - 0.45f, -1.0f, -90f, lit, isRedCocaCola: false);
        BeverageCoolerUnit(maxX - 0.45f, -3.8f, -90f, lit, isRedCocaCola: false);

        Box(maxX - 0.5f, 3.65f, minZ + 0.5f, 0.2f, 0.1f, 0.2f, Col(30, 30, 34));
        Raylib.DrawSphere(new Vector3(maxX - 0.5f, 3.58f, minZ + 0.5f), 0.11f, Shade(Col(20, 20, 24), lit, 0.5f));
        if (MathF.Sin(time * 3f) > 0.4f)
            Raylib.DrawSphere(new Vector3(maxX - 0.5f, 3.52f, minZ + 0.6f), 0.025f, Col(255, 40, 30));
    }

    public static void DrawStoreGlass(bool doorLocked, float daylight = 0f)
    {
        const float maxZ = 7.5f;

        Raylib.DrawCube(new Vector3(0f, 1.3f, maxZ), 2.4f, 2.6f, 0.06f,
            doorLocked ? Col(90, 130, 160, 120) : Col(140, 190, 220, 65));

        Raylib.DrawCube(new Vector3(-4.25f, 1.34f, 1.65f), 0.72f, 0.60f, 0.92f, Col(235, 220, 185, 48));

        Raylib.DrawCube(new Vector3(-2.75f, 1.22f, -3.0f), 0.5f, 0.36f, 0.5f, Col(200, 230, 240, 50));

        byte lampAlpha = (byte)Math.Max(0, (int)((1f - daylight) * 35f));
        if (lampAlpha > 0)
        {
            Raylib.DrawSphere(new Vector3(-2.9f, 3.2f, 9.1f), 0.35f, Col(255, 225, 150, lampAlpha));
            Raylib.DrawCylinder(new Vector3(-2.9f, 0.012f, 9.1f), 1.4f, 1.4f, 0.002f, 24, Col(255, 225, 150, (byte)(lampAlpha * 0.8f)));
        }
    }
}