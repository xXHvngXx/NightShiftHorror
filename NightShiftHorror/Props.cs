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
        Color skin = Col(222, 186, 160);
        Box(0, by + 0.30f, 0, 0.50f, 0.60f, 0.28f, shirt);
        Ellipsoid(0, by + 0.63f, 0, 0.062f, 0.06f, 0.062f, skin, 8, 10);

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
        Color pants = raincoat ? shirt : Col(34, 38, 52);
        Color shoe = Col(20, 20, 24);
        float s = MathF.Sin(phase) * swing;

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        Leg(new Vector3(-0.11f, 0.84f, 0), s, 0.76f, pants, shoe);
        Leg(new Vector3(0.11f, 0.84f, 0), -s, 0.76f, pants, shoe);
        UpperBody(0.84f, shirt, hair, -s * 0.8f, s * 0.8f, raincoat, isFemale,
                  headYaw, headPitch, headRoll, eyeGaze ?? Vector2.Zero, faceId);
        if (raincoat) Box(0, 0.58f, 0, 0.58f, 0.62f, 0.34f, shirt);

        Rlgl.PopMatrix();
    }

    public static void DrawSeatedPerson(Vector3 pos, float yawDeg, Color shirt, Color hair,
                                        Vector3? viewerPos = null, FaceId faceId = FaceId.None)
    {
        Color pants = Col(34, 38, 52);
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

        UpperBody(hip, shirt, hair, -65f, -65f, false, false,
                  gaze.HeadYaw, gaze.HeadPitch, gaze.HeadRoll, gaze.EyeGaze, faceId);

        Rlgl.PopMatrix();
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

    static void Plant(float x, float z, float s)
    {
        Raylib.DrawCylinder(new Vector3(x, 0, z), 0.2f * s, 0.15f * s, 0.35f * s, 10, Col(150, 82, 55));
        Raylib.DrawSphere(new Vector3(x, 0.55f * s, z), 0.22f * s, Col(52, 120, 65));
        Raylib.DrawSphere(new Vector3(x + 0.12f * s, 0.75f * s, z + 0.05f * s), 0.16f * s, Col(66, 140, 78));
        Raylib.DrawSphere(new Vector3(x - 0.10f * s, 0.80f * s, z - 0.05f * s), 0.15f * s, Col(45, 105, 58));
    }

    static void Pendant(float x, float z, float s)
    {
        float top = 2.6f + 0.28f * s;
        Box(x, (3.2f + top) / 2, z, 0.02f, 3.2f - top, 0.02f, Col(15, 15, 15));
        Raylib.DrawCylinder(new Vector3(x, 2.6f, z), 0.10f * s, 0.36f * s, 0.28f * s, 16, Col(42, 32, 26));
        Raylib.DrawSphere(new Vector3(x, 2.58f, z), 0.09f * s, Col(255, 220, 150));
    }

    static void Mug(float x, float z, Color c)
    {
        Raylib.DrawCylinder(new Vector3(x, 0.78f, z), 0.05f, 0.045f, 0.09f, 12, c);
        Raylib.DrawCylinder(new Vector3(x, 0.866f, z), 0.04f, 0.04f, 0.004f, 12, Col(60, 35, 20));
        Box(x + 0.065f, 0.825f, z, 0.03f, 0.05f, 0.015f, c);
    }

    public static void DrawCafe(float time, Vector3? viewerPos = null, string speaker = "")
    {
        Color wood = Col(95, 62, 40);
        Color woodDark = Col(40, 30, 25);

        Raylib.DrawPlane(Vector3.Zero, new Vector2(9, 9), Col(30, 20, 14));
        for (int i = -4; i <= 4; i++)
        {
            int h = Hash(i, 3, 9) % 16;
            Box(i, 0.005f, 0, 0.97f, 0.02f, 9f, Col(70 + h, 48 + h / 2, 34 + h / 3));
        }

        Box(0, 3.3f, 0, 9f, 0.2f, 9f, Col(48, 34, 26));
        for (int i = -3; i <= 3; i += 2) Box(i, 3.15f, 0, 0.18f, 0.14f, 9f, Col(34, 24, 18));

        Color wall = Col(115, 88, 65);
        Box(0, 1.65f, -4.5f, 9f, 3.3f, 0.2f, wall);
        Box(-4.5f, 1.65f, 0, 0.2f, 3.3f, 9f, wall);
        Box(4.5f, 1.65f, 0, 0.2f, 3.3f, 9f, wall);
        Box(0, 1.65f, 4.5f, 9f, 3.3f, 0.2f, wall);
        Color panel = Col(78, 54, 38);
        Box(0, 0.5f, 4.38f, 9f, 1f, 0.04f, panel);
        Box(4.38f, 0.5f, 0, 0.04f, 1f, 9f, panel);
        Box(0, 1.02f, 4.37f, 9f, 0.05f, 0.07f, woodDark);
        Box(4.37f, 1.02f, 0, 0.07f, 0.05f, 9f, woodDark);

        Box(0, 2.0f, 4.375f, 2.3f, 0.98f, 0.05f, woodDark);
        Box(0, 2.0f, 4.34f, 2.2f, 0.90f, 0.04f, Col(24, 28, 26));
        Color chalk = Col(225, 225, 210);
        for (int k = 0; k < 5; k++)
        {
            Box(-0.5f, 2.30f - k * 0.15f, 4.315f, 0.8f - (k % 3) * 0.15f, 0.025f, 0.005f, chalk);
            Box(0.7f, 2.30f - k * 0.15f, 4.315f, 0.25f, 0.025f, 0.005f, chalk);
        }

        Box(-2.8f, 1.6f, 4.25f, 1.6f, 0.05f, 0.3f, wood);
        for (int i = 0; i < 5; i++)
            Raylib.DrawCylinder(new Vector3(-3.4f + i * 0.3f, 1.625f, 4.25f), 0.06f, 0.05f, 0.1f, 10, Palette[i]);
        Box(-2.8f, 2.05f, 4.25f, 1.6f, 0.05f, 0.3f, wood);
        for (int i = 0; i < 4; i++)
            Box(-3.35f + i * 0.35f, 2.15f, 4.25f, 0.2f, 0.15f, 0.15f, Palette[(i + 3) % 8]);

        Box(-4.39f, 1.55f, 0.2f, 0.02f, 1.8f, 3.6f, Col(18, 26, 48));
        Raylib.DrawSphere(new Vector3(-4.37f, 2.05f, -0.7f), 0.12f, Col(255, 214, 140));
        Color frame = Col(50, 36, 28);
        Box(-4.34f, 0.65f, 0.2f, 0.06f, 0.06f, 3.7f, frame);
        Box(-4.34f, 2.45f, 0.2f, 0.06f, 0.06f, 3.7f, frame);
        Box(-4.34f, 1.55f, 0.2f, 0.05f, 0.04f, 3.6f, frame);
        foreach (float z in new[] { -1.6f, 0.2f, 2.0f })
            Box(-4.34f, 1.55f, z, 0.06f, 1.86f, 0.06f, frame);
        Box(-4.25f, 0.62f, 0.2f, 0.25f, 0.04f, 3.8f, wood);
        Plant(-4.2f, 1.4f, 0.5f);

        Plant(-3.8f, 3.8f, 1.1f);
        Plant(3.8f, 3.8f, 1.2f);

        Raylib.DrawCylinder(new Vector3(-3.1f, 0.72f, 3.0f), 0.4f, 0.4f, 0.05f, 16, wood);
        Box(-3.1f, 0.36f, 3.0f, 0.08f, 0.72f, 0.08f, woodDark);
        DrawChair(new Vector3(-2.45f, 0, 3.0f), -90, woodDark, Col(120, 60, 50));
        DrawChair(new Vector3(-3.75f, 0, 3.0f), 90, woodDark, Col(120, 60, 50));

        Pendant(0, 0, 1.0f);
        Pendant(-2.6f, 1.5f, 0.8f);
        Pendant(2.6f, 1.5f, 0.8f);

        Box(0, 0.74f, 0, 1.8f, 0.08f, 1.3f, wood);
        Box(0, 0.70f, 0, 1.7f, 0.03f, 1.2f, Col(70, 46, 30));
        foreach (float lx in new[] { -0.75f, 0.75f })
            foreach (float lz in new[] { -0.5f, 0.5f })
                Box(lx, 0.37f, lz, 0.08f, 0.74f, 0.08f, woodDark);

        Box(0, 0.789f, -0.35f, 0.10f, 0.016f, 0.19f, Col(240, 215, 110));
        Box(0, 0.798f, -0.35f, 0.085f, 0.002f, 0.17f, Col(70, 100, 150));
        Mug(0.22f, 0.28f, Col(235, 235, 230));
        Mug(-0.55f, 0.05f, Col(180, 90, 70));
        Raylib.DrawCylinder(new Vector3(-0.25f, 0.78f, -0.25f), 0.045f, 0.04f, 0.13f, 12, Col(110, 75, 50));
        Box(-0.24f, 0.93f, -0.25f, 0.008f, 0.18f, 0.008f, Col(220, 60, 60));
        Raylib.DrawCylinder(new Vector3(0.35f, 0.78f, -0.15f), 0.14f, 0.14f, 0.012f, 16, Col(235, 235, 235));
        Box(0.35f, 0.82f, -0.15f, 0.12f, 0.05f, 0.09f, Col(240, 200, 130));
        Box(0.35f, 0.855f, -0.15f, 0.12f, 0.02f, 0.09f, Col(110, 60, 40));
        Raylib.DrawCylinder(new Vector3(0.0f, 0.78f, 0.25f), 0.03f, 0.035f, 0.1f, 8, Col(190, 215, 225));
        Raylib.DrawSphere(new Vector3(0.0f, 0.91f, 0.25f), 0.04f, Col(235, 120, 150));
        Raylib.DrawSphere(new Vector3(0.04f, 0.88f, 0.26f), 0.03f, Col(250, 220, 90));

        Color chairWood = Col(55, 38, 28);
        Color chairSeat = Col(135, 70, 55);
        DrawChair(new Vector3(0, 0, 1.05f), 180, chairWood, chairSeat);
        DrawChair(new Vector3(-1.25f, 0, 0.05f), 90, chairWood, chairSeat);
        DrawChair(new Vector3(1.25f, 0, 0.05f), -90, chairWood, chairSeat);

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

        Box(-4.33f, 1.55f, 0.2f, 0.04f, 1.8f, 3.6f, Col(170, 210, 230, 45));
        Raylib.DrawCylinder(new Vector3(-0.25f, 0.78f, -0.25f), 0.05f, 0.045f, 0.14f, 12, Col(220, 240, 250, 60));
        Raylib.DrawSphere(new Vector3(0, 2.58f, 0), 0.30f, Col(255, 200, 120, 28));
        Raylib.DrawSphere(new Vector3(-2.6f, 2.58f, 1.5f), 0.24f, Col(255, 200, 120, 24));
        Raylib.DrawSphere(new Vector3(2.6f, 2.58f, 1.5f), 0.24f, Col(255, 200, 120, 24));
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
        Color frame = Shade(Col(32, 34, 40), lit, 0.3f);
        Color board = Shade(Col(195, 198, 205), lit, 0.25f);
        Color lip = Shade(Col(245, 195, 50), lit, 0.3f);
        Color header = seed == 0 ? Col(215, 55, 45) : Col(235, 130, 40);

        Box(cx, 0.06f, cz, 0.58f, 0.12f, len, frame);
        Box(cx, 0.92f, cz, 0.05f, 1.84f, len, frame);
        Box(cx, 1.80f, cz, 0.58f, 0.10f, len, Shade(header, lit, 0.3f));
        Box(cx, 0.92f, cz - len / 2, 0.58f, 1.84f, 0.05f, frame);
        Box(cx, 0.92f, cz + len / 2, 0.58f, 1.84f, 0.05f, frame);

        for (int l = 0; l < 4; l++)
        {
            float by = 0.28f + l * 0.38f;
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Box(cx + sd * 0.15f, by, cz, 0.26f, 0.03f, len, board);
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
        Color blackFrame = Shade(Col(22, 24, 28), lit, 0.35f);
        Color warmWood = Shade(Col(162, 102, 56), lit, 0.32f);
        Color counterTop = Shade(Col(215, 212, 205), lit, 0.32f);
        Color steelGray = Shade(Col(145, 150, 158), lit, 0.35f);

        Box(-4.35f, 0.48f, 0.5f, 1.30f, 0.96f, 3.60f, blackFrame);
        Box(-4.35f, 0.99f, 0.5f, 1.38f, 0.06f, 3.68f, counterTop);

        for (int p = 0; p < 5; p++)
        {
            float pz = -0.85f + p * 0.68f;
            Box(-3.69f, 0.48f, pz, 0.02f, 0.78f, 0.58f, warmWood);
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
        Color blackBase = Shade(Col(22, 24, 28), lit, 0.35f);
        Color warmWood = Shade(Col(162, 102, 56), lit, 0.32f);
        Color counterTop = Shade(Col(215, 212, 205), lit, 0.32f);

        Box(-6.85f, 0.48f, -0.2f, 1.15f, 0.96f, 6.2f, blackBase);
        Box(-6.85f, 0.99f, -0.2f, 1.22f, 0.06f, 6.3f, counterTop);
        for (int p = 0; p < 8; p++)
            Box(-6.26f, 0.48f, -2.75f + p * 0.72f, 0.02f, 0.78f, 0.62f, warmWood);

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
        Rlgl.PushMatrix();
        Rlgl.Translatef(cx, 0f, cz);
        Rlgl.Rotatef(yawDeg, 0, 1, 0);

        float front = 0.35f;
        Color silver = Shade(Col(185, 190, 198), lit, 0.35f);
        Color bodyCol = isRedCocaCola ? Shade(Col(198, 28, 32), lit, 0.38f) : Shade(Col(28, 34, 42), lit, 0.4f);

        Box(0f, 1.25f, 0f, 2.5f, 2.5f, 0.7f, bodyCol);
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
        Color blueBody = Shade(Col(22, 88, 190), lit, 0.35f);
        Box(cx, 0.65f, cz, 0.72f, 1.30f, 0.68f, blueBody);
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
        float w = 2.8f;
        Color wood = Shade(Col(70, 50, 38), lit, 0.3f);
        Box(cx, 1.45f, wallZ + 0.12f, w, 1.6f, 0.02f, Shade(Col(35, 32, 38), lit, 0.4f));

        if (hasWelcomeSign)
        {
            Box(cx, 2.58f, wallZ + 0.14f, 2.2f, 0.34f, 0.04f, Shade(Col(218, 92, 38), lit, 0.35f));
            Box(cx, 2.58f, wallZ + 0.165f, 1.5f, 0.10f, 0.01f, Shade(Col(250, 246, 238), lit, 0.35f));
        }

        for (int l = 0; l < 4; l++)
        {
            float y = 0.82f + l * 0.40f;
            Box(cx, y, wallZ + 0.22f, w, 0.03f, 0.24f, wood);
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
        Color metal = Col(40, 42, 46);
        Raylib.DrawCylinder(new Vector3(x, 0, z), 0.06f, 0.08f, 3.4f, 8, metal);
        Box(x - 0.4f, 3.4f, z, 0.9f, 0.06f, 0.08f, metal);
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

        Color wallColor = Shade(Col(198, 184, 166), lit, 0.22f);
        Color trimDark = Shade(Col(55, 58, 66), lit, 0.3f);

        Raylib.DrawPlane(new Vector3(0, 0, 0.5f), new Vector2(storeW, storeL), Shade(Col(150, 142, 132), lit, 0.28f));
        for (int i = -7; i <= 7; i++)
            for (int j = -6; j <= 7; j++)
            {
                Color t = (((i + j) & 1) == 0) ? Col(218, 206, 192) : Col(205, 192, 176);
                Box(i, 0.006f, j, 0.96f, 0.012f, 0.96f, Shade(t, lit, 0.16f));
            }
        Box(0, 0.012f, maxZ - 0.65f, 2.1f, 0.02f, 0.95f, Shade(Col(32, 34, 38), lit, 0.3f));

        Raylib.DrawPlane(new Vector3(0, -0.02f, 12.5f), new Vector2(28, 12), LerpCol(Col(24, 25, 30), Col(72, 78, 90), daylight));
        Box(0, -0.005f, 8.7f, 28f, 0.03f, 2.4f, LerpCol(Col(70, 72, 78), Col(135, 140, 148), daylight));
        Box(0, 0.005f, 9.95f, 28f, 0.05f, 0.12f, LerpCol(Col(110, 110, 115), Col(175, 178, 185), daylight));
        for (int x = -12; x <= 12; x += 3) Box(x, -0.015f, 13.0f, 1.4f, 0.006f, 0.12f, Col(200, 190, 120));
        StreetLamp(-2.1f, 9.1f);

        Box(0, 3.8f, 0.5f, storeW, 0.2f, storeL, Shade(Col(228, 228, 232), lit, 0.20f));
        Box(0, 3.48f, -1.2f, storeW - 0.2f, 0.36f, 0.24f, Shade(Col(168, 122, 68), lit, 0.26f));
        Box(-0.35f, 3.16f, 0.6f, 0.04f, 0.28f, 0.04f, Col(28, 30, 34));
        Box(-0.35f, 2.94f, 0.6f, 0.44f, 0.28f, 0.06f, Shade(Col(24, 26, 30), lit, 0.4f));
        Box(-0.35f, 2.94f, 0.635f, 0.38f, 0.22f, 0.008f, lit ? Col(115, 175, 225) : Col(25, 45, 65));

        foreach (float lx in new[] { -4.6f, -1.5f, 1.6f, 4.7f })
            foreach (float lz in new[] { -4.2f, -1.2f, 1.8f, 4.8f })
                CeilingLight(lx, lz, lit);

        Box(0, 1.9f, minZ, storeW, 3.8f, 0.2f, wallColor);
        Box(minX, 1.9f, 0.5f, 0.2f, 3.8f, storeL, wallColor);
        Box(maxX, 1.9f, 0.5f, 0.2f, 3.8f, storeL, wallColor);
        Box(-4.35f, 1.9f, maxZ, 6.3f, 3.8f, 0.2f, wallColor);
        Box(4.35f, 1.9f, maxZ, 6.3f, 3.8f, 0.2f, wallColor);
        Box(0f, 3.2f, maxZ, 2.4f, 1.2f, 0.2f, wallColor);

        Box(0, 0.07f, minZ + 0.11f, storeW - 0.2f, 0.14f, 0.02f, trimDark);
        Box(minX + 0.11f, 0.07f, 0.5f, 0.02f, 0.14f, storeL - 0.2f, trimDark);
        Box(maxX - 0.11f, 0.07f, 0.5f, 0.02f, 0.14f, storeL - 0.2f, trimDark);

        Color alu = Shade(Col(170, 175, 185), lit, 0.35f);
        Box(-1.2f, 1.3f, maxZ, 0.10f, 2.6f, 0.14f, alu);
        Box(1.2f, 1.3f, maxZ, 0.10f, 2.6f, 0.14f, alu);
        Box(0, 0.06f, maxZ, 2.4f, 0.12f, 0.14f, alu);
        Box(0, 2.6f, maxZ, 2.4f, 0.10f, 0.14f, alu);
        Box(0, 1.3f, maxZ, 0.06f, 2.6f, 0.10f, alu);
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

        Color cWood = Shade(Col(78, 54, 40), lit, 0.3f);
        Color cTop = Shade(Col(228, 224, 216), lit, 0.3f);
        Color green = Shade(Col(30, 140, 80), lit, 0.3f);
        Box(-1.4f, 0.48f, -2.15f, 3.4f, 0.96f, 0.75f, cWood);
        Box(-1.4f, 1.01f, -2.15f, 3.48f, 0.06f, 0.83f, cTop);
        Box(-1.4f, 0.6f, -1.77f, 3.0f, 0.07f, 0.02f, green);
        Box(-2.75f, 0.48f, -3.55f, 0.75f, 0.96f, 3.5f, cWood);
        Box(-2.75f, 1.01f, -3.55f, 0.83f, 0.06f, 3.58f, cTop);
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