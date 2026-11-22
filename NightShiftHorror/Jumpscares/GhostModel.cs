using System.Numerics;
using Raylib_cs;

public static class GhostModel
{
    static Model externalModel;
    static bool externalLoaded = false;
    static bool checkedExternal = false;

    public static float ExternalScale = 1.0f;
    public static float ExternalYawOffset = 0f;

    public static void EnsureLoaded()
    {
        if (checkedExternal) return;
        checkedExternal = true;

        string baseDir = AppContext.BaseDirectory;
        string[] paths =
        [
            Path.Combine(baseDir, "Assets", "Models", "ghost.glb"),
            Path.Combine(baseDir, "Assets", "Models", "ghost.obj"),
            "Assets/Models/ghost.glb",
            "Assets/Models/ghost.obj"
        ];

        string found = paths.FirstOrDefault(File.Exists) ?? "";
        if (found != "")
        {
            externalModel = Raylib.LoadModel(found);
            externalLoaded = externalModel.MeshCount > 0;
        }
    }

    public static void Unload()
    {
        if (externalLoaded)
        {
            Raylib.UnloadModel(externalModel);
            externalLoaded = false;
        }
        checkedExternal = false;
    }

    /// <summary>
    /// Vẽ thực thể ma tại vị trí pos (chân chạm mặt đất Y = pos.Y), quay mặt theo yawDeg.
    /// lungeProgress (0..1): mức độ lao tới / giơ tay vồ lấy người chơi.
    /// </summary>
    public static void Draw(Vector3 pos, float yawDeg, float time, float lungeProgress = 0f, float twitchIntensity = 1f)
    {
        EnsureLoaded();

        if (externalLoaded)
        {
            float twitchYaw = MathF.Sin(time * 47f) * 3.5f * twitchIntensity;
            Raylib.DrawModelEx(
                externalModel,
                pos,
                Vector3.UnitY,
                yawDeg + ExternalYawOffset + twitchYaw,
                new Vector3(ExternalScale),
                Color.White
            );
            return;
        }

        // --- DỰNG MODEL MA PROCEDURAL DỊ DẠNG & ÁM ẢNH ---
        // Chuyển động giật cục kiểu stop-motion (lượng tử hóa nhịp thời gian)
        float snapTime = MathF.Floor(time * 24f) / 24f;
        float headTwitchZ = (MathF.Sin(snapTime * 59f) * 22f + MathF.Cos(time * 31f) * 8f) * twitchIntensity + 11f;
        float headTwitchX = MathF.Sin(snapTime * 43f) * 11f * twitchIntensity;
        float headTwitchY = MathF.Cos(snapTime * 37f) * 14f * twitchIntensity;
        float bodySway = MathF.Sin(time * 21f) * 0.035f * twitchIntensity;
        float hunchForward = 10f + lungeProgress * 14f;

        Color robeVoid = new(7, 7, 10, 255);
        Color robeTatter = new(18, 17, 23, 255);
        Color skinCorpse = new(176, 182, 178, 255);
        Color skinBruise = new(96, 102, 112, 255);
        Color bonePale = new(215, 212, 198, 255);
        Color shadowAbyss = new(3, 3, 5, 255);
        Color eyeCrimson = new(255, 24, 38, 255);
        Color eyeCore = new(255, 235, 210, 255);
        Color bloodDark = new(115, 8, 16, 245);
        Color auraSmoke = new(10, 10, 14, 130);

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X + bodySway, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0f, 1f, 0f);

        // 0. Làn khói đen u ám bốc lên quanh chân thực thể
        for (int s = 0; s < 10; s++)
        {
            float ang = time * 2.4f + s * (MathF.PI * 2f / 10f);
            float rad = 0.24f + MathF.Sin(time * 5f + s) * 0.09f;
            float sy = 0.08f + ((time * 0.6f + s * 0.17f) % 0.65f);
            Raylib.DrawSphere(
                new Vector3(MathF.Cos(ang) * rad, sy, MathF.Sin(ang) * rad),
                0.11f + (s % 3) * 0.025f,
                auraSmoke
            );
        }

        // 1. Thân dưới & tà áo choàng rách rưới cao lêu nghêu (2.05m)
        Raylib.DrawCylinder(new Vector3(0f, 0.02f, -0.02f), 0.36f, 0.19f, 1.36f, 14, robeVoid);

        // Các dải vải rách tươm bay phấp phới quanh chân
        for (int i = 0; i < 9; i++)
        {
            float a = i * (MathF.PI * 2f / 9f) + MathF.Sin(time * 9f + i * 1.3f) * 0.14f;
            float rx = MathF.Cos(a) * 0.27f;
            float rz = MathF.Sin(a) * 0.27f;
            float hStrip = 0.38f + (i % 3) * 0.14f;
            Raylib.DrawCube(new Vector3(rx, hStrip * 0.5f, rz), 0.055f, hStrip, 0.025f, (i % 2 == 0) ? robeTatter : robeVoid);
        }

        // 2. Thân trên gù gập về phía trước + xương sườn lộ ra dưới lớp áo rách
        Rlgl.PushMatrix();
        Rlgl.Translatef(0f, 1.18f, 0f);
        Rlgl.Rotatef(hunchForward, 1f, 0f, 0f);

        // Lồng ngực gầy trơ xương
        Raylib.DrawCylinder(new Vector3(0f, 0.0f, 0.01f), 0.18f, 0.23f, 0.46f, 12, robeTatter);
        Raylib.DrawCube(new Vector3(0f, 0.24f, 0.03f), 0.15f, 0.34f, 0.12f, skinBruise);

        // 4 cặp xương sườn trắng bệch nhô ra trước ngực
        for (int r = 0; r < 4; r++)
        {
            float ry = 0.10f + r * 0.068f;
            float rw = 0.18f + r * 0.015f;
            Raylib.DrawCube(new Vector3(-rw * 0.45f, ry, 0.095f), rw * 0.85f, 0.020f, 0.035f, bonePale);
            Raylib.DrawCube(new Vector3(rw * 0.45f, ry, 0.095f), rw * 0.85f, 0.020f, 0.035f, bonePale);
        }

        // Hai khớp vai nhô cao bất đối xứng (vai trái cao hơn vai phải)
        Raylib.DrawSphere(new Vector3(-0.24f, 0.45f, 0.02f), 0.075f, robeVoid);
        Raylib.DrawSphere(new Vector3(0.23f, 0.39f, 0.02f), 0.072f, robeVoid);

        // 3. Cổ dài ngoằng vươn ra trước + đốt sống cổ gồ ghề
        Rlgl.PushMatrix();
        Rlgl.Translatef(0f, 0.42f, 0.04f);
        Rlgl.Rotatef(18f, 1f, 0f, 0f);
        Raylib.DrawCylinder(new Vector3(0f, 0.0f, 0f), 0.045f, 0.058f, 0.22f, 10, skinCorpse);
        // Gân cổ căng cứng
        Raylib.DrawLine3D(new Vector3(-0.025f, 0.02f, 0.04f), new Vector3(-0.018f, 0.20f, 0.035f), skinBruise);
        Raylib.DrawLine3D(new Vector3(0.025f, 0.02f, 0.04f), new Vector3(0.018f, 0.20f, 0.035f), skinBruise);

        // 4. ĐẦU & KHUÔN MẶT BIẾN DẠNG
        Rlgl.PushMatrix();
        Rlgl.Translatef(0f, 0.22f, 0.03f);
        Rlgl.Rotatef(headTwitchZ, 0f, 0f, 1f);
        Rlgl.Rotatef(headTwitchX - hunchForward * 0.6f, 1f, 0f, 0f);
        Rlgl.Rotatef(headTwitchY, 0f, 1f, 0f);

        // Hộp sọ dài hốc hác & hai gò má nhô cao
        Raylib.DrawSphere(new Vector3(0f, 0.12f, -0.01f), 0.132f, skinCorpse);
        Raylib.DrawCylinder(new Vector3(0f, -0.01f, 0.02f), 0.072f, 0.122f, 0.15f, 12, skinCorpse);
        Raylib.DrawSphere(new Vector3(-0.078f, 0.055f, 0.088f), 0.042f, skinCorpse);
        Raylib.DrawSphere(new Vector3(0.078f, 0.055f, 0.088f), 0.042f, skinCorpse);
        // Vùng má hóp tối sầm
        Raylib.DrawSphere(new Vector3(-0.068f, 0.00f, 0.078f), 0.036f, skinBruise);
        Raylib.DrawSphere(new Vector3(0.068f, 0.00f, 0.078f), 0.036f, skinBruise);

        // Mái tóc đen rũ rượi che lòa xòa hai bên và trước trán
        Raylib.DrawSphere(new Vector3(0f, 0.155f, -0.035f), 0.146f, shadowAbyss);
        for (int h = -3; h <= 3; h++)
        {
            float hx = h * 0.042f;
            float swayHair = MathF.Sin(time * 18f + h) * 0.014f;
            float len = (Math.Abs(h) >= 2) ? 0.44f : 0.16f;
            float zOff = (Math.Abs(h) >= 2) ? 0.02f : 0.118f;
            Raylib.DrawCube(new Vector3(hx + swayHair, 0.16f - len * 0.5f, zOff), 0.026f, len, 0.03f, shadowAbyss);
        }

        // Hốc mắt sâu hoắm + quầng thâm + đồng tử đỏ rực phát sáng
        Raylib.DrawSphere(new Vector3(-0.048f, 0.105f, 0.112f), 0.033f, shadowAbyss);
        Raylib.DrawSphere(new Vector3(0.048f, 0.105f, 0.112f), 0.033f, shadowAbyss);
        // Ánh đỏ phát sáng bên trong hốc mắt
        Raylib.DrawSphere(new Vector3(-0.048f, 0.105f, 0.132f), 0.014f, eyeCrimson);
        Raylib.DrawSphere(new Vector3(0.048f, 0.105f, 0.132f), 0.014f, eyeCrimson);
        Raylib.DrawSphere(new Vector3(-0.048f, 0.105f, 0.141f), 0.006f, eyeCore);
        Raylib.DrawSphere(new Vector3(0.048f, 0.105f, 0.141f), 0.006f, eyeCore);

        // Sống mũi gãy & hai lỗ mũi tối
        Raylib.DrawCube(new Vector3(0f, 0.065f, 0.132f), 0.022f, 0.055f, 0.025f, skinBruise);

        // Hàm dưới trễ sâu ngoác rộng + răng lởm chởm + vệt máu chảy cằm
        float jawDrop = 0.065f + lungeProgress * 0.055f + MathF.Abs(MathF.Sin(time * 38f)) * 0.018f;
        Raylib.DrawCylinder(new Vector3(0f, -0.02f - jawDrop * 0.45f, 0.095f), 0.052f, 0.072f, jawDrop + 0.05f, 10, skinCorpse);
        // Khoang miệng đen ngòm
        Raylib.DrawCylinder(new Vector3(0f, -0.005f - jawDrop * 0.35f, 0.116f), 0.038f, 0.048f, jawDrop, 10, shadowAbyss);

        // Hàm răng nhọn lởm chởm trên & dưới
        for (int t = -2; t <= 2; t++)
        {
            float tx = t * 0.014f;
            Raylib.DrawCube(new Vector3(tx, 0.012f, 0.136f), 0.006f, 0.016f, 0.006f, bonePale);
            Raylib.DrawCube(new Vector3(tx, -0.015f - jawDrop * 0.7f, 0.132f), 0.006f, 0.015f, 0.006f, bonePale);
        }

        // Vệt máu thẫm chảy từ hai khóe miệng xuống cằm
        Raylib.DrawCube(new Vector3(-0.036f, -0.045f - jawDrop * 0.4f, 0.132f), 0.010f, 0.075f, 0.008f, bloodDark);
        Raylib.DrawCube(new Vector3(0.036f, -0.055f - jawDrop * 0.4f, 0.132f), 0.011f, 0.090f, 0.008f, bloodDark);

        Rlgl.PopMatrix(); // Kết thúc Đầu
        Rlgl.PopMatrix(); // Kết thúc Cổ

        // 5. HAI CÁNH TAY DÀI NGOẰNG & MÓNG VUỐT SẮC NHỌN VỒ TỚI
        float armLift = 18f + lungeProgress * 82f;
        for (int side = -1; side <= 1; side += 2)
        {
            Rlgl.PushMatrix();
            float shoulderY = (side == -1) ? 0.43f : 0.38f;
            Rlgl.Translatef(side * 0.24f, shoulderY, 0.02f);

            float twitchArm = MathF.Sin(snapTime * 37f + side * 2f) * 9f * twitchIntensity;
            Rlgl.Rotatef(-armLift + twitchArm, 1f, 0f, 0f);
            Rlgl.Rotatef(side * (-10f + lungeProgress * 16f), 0f, 0f, 1f);

            // Cánh tay trên bọc ống tay áo rách
            Raylib.DrawCylinder(new Vector3(0f, -0.46f, 0f), 0.030f, 0.044f, 0.48f, 8, robeVoid);
            // Cẳng tay gầy trơ xương dài ngoằng
            Raylib.DrawCylinder(new Vector3(0f, -0.92f, 0.05f), 0.020f, 0.029f, 0.48f, 8, skinCorpse);
            Raylib.DrawCube(new Vector3(0f, -0.72f, 0.03f), 0.018f, 0.36f, 0.018f, skinBruise);

            // Bàn tay xương xẩu + 5 ngón tay dài kèm móng vuốt đen nhọn hoắt
            Vector3 handPos = new(0f, -0.95f, 0.06f);
            Raylib.DrawSphere(handPos, 0.035f, skinCorpse);

            for (int f = -2; f <= 2; f++)
            {
                float spreadX = f * 0.018f;
                float curlZ = 0.045f + (2 - Math.Abs(f)) * 0.012f;
                Vector3 knuckle = handPos + new Vector3(spreadX * 0.6f, -0.045f, curlZ * 0.5f);
                Vector3 fingerTip = handPos + new Vector3(spreadX, -0.115f, curlZ);
                Vector3 clawTip = fingerTip + new Vector3(spreadX * 0.25f, -0.045f, 0.035f);

                Raylib.DrawLine3D(handPos, knuckle, skinCorpse);
                Raylib.DrawLine3D(knuckle, fingerTip, skinCorpse);
                // Móng vuốt đen sắc nhọn vươn thẳng về ống kính
                Raylib.DrawLine3D(fingerTip, clawTip, shadowAbyss);
            }

            Rlgl.PopMatrix();
        }

        Rlgl.PopMatrix(); // Kết thúc Thân trên
        Rlgl.PopMatrix(); // Kết thúc Tổng thể
    }
}