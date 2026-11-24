using System.Numerics;
using Raylib_cs;

/// <summary>
/// Thực thể ma: Nạp model 3D ngoài (horror_ghost_character_-_blood_stained_spirit.glb hoặc ghost.glb|obj),
/// vẽ 2 mặt (Double-Sided) chống xuyên thấu, tự chuẩn hóa chiều cao, căn chân chạm đất và phủ hiệu ứng kinh dị.
/// </summary>
public static class GhostModel
{
    static Model externalModel;
    static bool externalLoaded = false;
    static bool checkedExternal = false;

    static float autoBaseScale = 1.0f;
    static Vector3 autoPivotOffset = Vector3.Zero;
    static float normalizedHeight = 1.88f;

    /// <summary>Hệ số phóng to/thu nhỏ bổ sung cho model .glb (1.0 = chuẩn 1.88m).</summary>
    public static float ExternalScale = 1.0f;

    /// <summary>Góc xoay bù hướng mặt của model .glb (90f đưa mặt hướng thẳng ra ống kính; bấm Y trong F9 để xoay thử).</summary>
    public static float ExternalYawOffset = 270f;

    /// <summary>Tỷ lệ chiều cao khuôn mặt trên model .glb (0.90 = đúng tầm mắt/khuôn mặt).</summary>
    public static float ExternalFaceHeightRatio = 0.90f;

    /// <summary>Độ nhô về phía trước của khuôn mặt tính từ tâm trục đứng (mét).</summary>
    public static float ExternalFaceForwardOffset = 0.14f;

    /// <summary>Bật/tắt ánh mắt đỏ phát sáng trên khuôn mặt model .glb (bấm G trong F9 để bật/tắt).</summary>
    public static bool ExternalEyeGlow = false;

    /// <summary>Hệ số duỗi tay khi vồ cho bản procedural.</summary>
    public static float ReachScale = 0.92f;

    public static bool IsUsingExternalModel
    {
        get
        {
            EnsureLoaded();
            return externalLoaded;
        }
    }

    public static void EnsureLoaded()
    {
        if (checkedExternal) return;
        checkedExternal = true;

        string baseDir = AppContext.BaseDirectory;
        string[] fileNames =
        [
            "horror_ghost_character_-_blood_stained_spirit.glb",
            "ghost.glb",
            "ghost.obj"
        ];

        string found = "";
        foreach (string name in fileNames)
        {
            string p1 = Path.Combine(baseDir, "Assets", "Models", name);
            if (File.Exists(p1)) { found = p1; break; }

            string p2 = Path.Combine("Assets", "Models", name);
            if (File.Exists(p2)) { found = p2; break; }
        }

        if (found != "")
        {
            externalModel = Raylib.LoadModel(found);
            externalLoaded = externalModel.MeshCount > 0;

            if (externalLoaded)
            {
                NormalizeExternalBounds();
            }
        }
    }

    static void NormalizeExternalBounds()
    {
        BoundingBox box = Raylib.GetModelBoundingBox(externalModel);
        Vector3 size = box.Max - box.Min;
        float rawHeight = MathF.Max(0.001f, size.Y);

        const float targetHeight = 1.88f;
        autoBaseScale = targetHeight / rawHeight;
        normalizedHeight = targetHeight;

        float centerX = (box.Min.X + box.Max.X) * 0.5f;
        float centerZ = (box.Min.Z + box.Max.Z) * 0.5f;
        autoPivotOffset = new Vector3(-centerX * autoBaseScale, -box.Min.Y * autoBaseScale, -centerZ * autoBaseScale);
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

    static float gBright = 1f;
    static float gAlpha = 1f;
    static Vector3 gTint = Vector3.One;
    static readonly Vector3 LightDir = Vector3.Normalize(new Vector3(0.15f, 0.25f, 1f));
    static readonly float[] FingerSeg = [0.078f, 0.062f, 0.050f];

    static float Sm(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        return x * x * (3f - 2f * x);
    }

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    static float HashF(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 374761393 + b * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0xFFFF) / 65535f;
        }
    }

    static Color C(int r, int g, int b, int a = 255)
        => new Color(
            (byte)Math.Clamp((int)(r * gBright * gTint.X), 0, 255),
            (byte)Math.Clamp((int)(g * gBright * gTint.Y), 0, 255),
            (byte)Math.Clamp((int)(b * gBright * gTint.Z), 0, 255),
            (byte)Math.Clamp((int)(a * gAlpha), 0, 255));

    static Color E(int r, int g, int b, int a = 255)
        => new Color(
            (byte)Math.Clamp((int)(r * gTint.X), 0, 255),
            (byte)Math.Clamp((int)(g * gTint.Y), 0, 255),
            (byte)Math.Clamp((int)(b * gTint.Z), 0, 255),
            (byte)Math.Clamp((int)(a * gAlpha), 0, 255));

    static Color LerpC(Color a, Color b, float t)
        => new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t));

    static Color Shade(Color c, Vector3 n, float amt)
    {
        float k = Math.Clamp(Vector3.Dot(n, LightDir) * 0.5f + 0.5f, 0f, 1f);
        float f = (1f - amt) + amt * k;
        return new Color((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f), c.A);
    }

    static void Reserve(int verts) => Rlgl.CheckRenderBatchLimit(verts);

    static void Vert(Vector3 p, Color c)
    {
        Rlgl.Color4ub(c.R, c.G, c.B, c.A);
        Rlgl.Vertex3f(p.X, p.Y, p.Z);
    }

    static Vector3 RotX(Vector3 v, float deg)
    {
        float r = deg * MathF.PI / 180f, c = MathF.Cos(r), s = MathF.Sin(r);
        return new Vector3(v.X, v.Y * c - v.Z * s, v.Y * s + v.Z * c);
    }

    static Vector3 RotY(Vector3 v, float deg)
    {
        float r = deg * MathF.PI / 180f, c = MathF.Cos(r), s = MathF.Sin(r);
        return new Vector3(v.X * c + v.Z * s, v.Y, -v.X * s + v.Z * c);
    }

    static Vector3 RotZ(Vector3 v, float deg)
    {
        float r = deg * MathF.PI / 180f, c = MathF.Cos(r), s = MathF.Sin(r);
        return new Vector3(v.X * c - v.Y * s, v.X * s + v.Y * c, v.Z);
    }

    struct Pose
    {
        public float Hunch, NeckPitch, HeadPitch, HeadRoll, HeadYaw;
    }

    static float ComputeBodySway(float time, float twitch)
        => MathF.Sin(time * 19f) * 0.014f * twitch;

    static Pose BuildPose(float lunge, float reveal, float time, float twitch, bool useTwitch)
    {
        float rev = Sm(reveal);
        Pose k;
        k.Hunch = 8f + lunge * 14f;
        k.NeckPitch = 14f + lunge * 10f;
        k.HeadPitch = Lerp(36f, -18f, rev);
        k.HeadRoll = Lerp(-10f, -18f, rev);
        k.HeadYaw = 0f;

        if (useTwitch)
        {
            float snap = MathF.Floor(time * 22f) / 22f;
            float amt = twitch * (0.25f + 0.55f * rev);
            k.HeadRoll += (MathF.Sin(snap * 59f) * 6.5f + MathF.Cos(time * 31f) * 2.5f) * amt;
            k.HeadPitch += MathF.Sin(snap * 43f) * 3.8f * amt;
            k.HeadYaw = MathF.Sin(snap * 37f) * 5.5f * amt;
        }
        return k;
    }

    static Vector3 HeadToGhost(Pose k, Vector3 p)
    {
        p = RotZ(p, k.HeadRoll);
        p = RotX(p, k.HeadPitch);
        p = RotY(p, k.HeadYaw);
        p += new Vector3(0f, 0.26f, 0f);
        p = RotX(p, k.NeckPitch);
        p += new Vector3(0f, 0.60f, 0.02f);
        p = RotX(p, k.Hunch);
        p += new Vector3(0f, 1.0f, 0f);
        return p;
    }

    public static Vector3 GetFaceLocal(float lunge, float reveal = -1f)
    {
        EnsureLoaded();
        lunge = Math.Clamp(lunge, 0f, 1f);
        if (reveal < 0f) reveal = lunge;

        if (externalLoaded)
        {
            float h = normalizedHeight * ExternalScale * ExternalFaceHeightRatio;
            float fwd = ExternalFaceForwardOffset * ExternalScale;
            return new Vector3(0f, h, fwd);
        }

        Pose k = BuildPose(lunge, reveal, 0f, 0f, false);
        return HeadToGhost(k, new Vector3(0f, 0.065f, 0.125f) * 1.12f);
    }

    public static Vector3 GetFaceWorld(Vector3 pos, float yawDeg, float lunge, float reveal = -1f)
    {
        Vector3 l = GetFaceLocal(lunge, reveal);
        float r = yawDeg * MathF.PI / 180f;
        return pos + new Vector3(
            l.X * MathF.Cos(r) + l.Z * MathF.Sin(r),
            l.Y,
            -l.X * MathF.Sin(r) + l.Z * MathF.Cos(r));
    }

    public static Vector3 GetLiveFaceWorld(Vector3 pos, float yawDeg, float time, float lunge, float twitchIntensity = 1f)
    {
        EnsureLoaded();
        lunge = Math.Clamp(lunge, 0f, 1f);
        float twitch = Math.Clamp(twitchIntensity, 0f, 2f);
        float sway = ComputeBodySway(time, twitch);
        float r = yawDeg * MathF.PI / 180f;

        if (externalLoaded)
        {
            Vector3 lExt = GetFaceLocal(lunge, lunge);
            return new Vector3(pos.X + sway, pos.Y, pos.Z) + new Vector3(
                lExt.X * MathF.Cos(r) + lExt.Z * MathF.Sin(r),
                lExt.Y,
                -lExt.X * MathF.Sin(r) + lExt.Z * MathF.Cos(r));
        }

        Pose k = BuildPose(lunge, lunge, time, twitch, true);
        Vector3 l = HeadToGhost(k, new Vector3(0f, 0.065f, 0.125f) * 1.12f);

        return new Vector3(pos.X + sway, pos.Y, pos.Z) + new Vector3(
            l.X * MathF.Cos(r) + l.Z * MathF.Sin(r),
            l.Y,
            -l.X * MathF.Sin(r) + l.Z * MathF.Cos(r));
    }

    // =====================================================================
    //  VẼ MODEL NGOÀI (.GLB) - DOUBLE-SIDED CHỐNG XUYÊN THẤU + HỌA TIẾT KINH DỊ
    // =====================================================================
    static void DrawExternalGhost(Vector3 pos, float yawDeg, float time, float lunge,
                                  float twitch, float scream, float bright)
    {
        float snap = MathF.Floor(time * 22f) / 22f;
        float sway = ComputeBodySway(time, twitch);
        float twitchYaw = MathF.Sin(snap * 47f) * (1.8f + 3.5f * lunge) * twitch;
        float twitchRoll = MathF.Sin(snap * 59f) * (1.2f + 2.5f * lunge) * twitch;
        float finalScale = autoBaseScale * ExternalScale;

        // Tắt Backface Culling để hiển thị đủ 2 mặt polygon của model .glb, tránh bị xuyên thấu rỗng ruột
        Rlgl.DisableBackfaceCulling();

        // 1. Vẽ model .glb chính với màu sắc & độ sáng đồng bộ đèn chớp
        float b = MathF.Max(0.12f, bright);
        Color mainTint = new Color(
            (byte)Math.Clamp((int)(255f * b), 28, 255),
            (byte)Math.Clamp((int)(248f * b), 26, 255),
            (byte)Math.Clamp((int)(255f * b), 32, 255),
            (byte)255
        );

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X + sway, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg + ExternalYawOffset + twitchYaw, 0f, 1f, 0f);
        Rlgl.Rotatef(twitchRoll, 0f, 0f, 1f);
        Rlgl.Translatef(autoPivotOffset.X * ExternalScale, autoPivotOffset.Y * ExternalScale, autoPivotOffset.Z * ExternalScale);
        Raylib.DrawModelEx(externalModel, Vector3.Zero, Vector3.UnitY, 0f, new Vector3(finalScale), mainTint);
        Rlgl.PopMatrix();

        // 2. Bóng mờ giật hình đỏ/xanh (chỉ bật khi đang hét đỉnh điểm để không che mặt lúc tiếp cận)
        float afterimage = twitch * scream;
        if (afterimage > 0.65f && bright > 0.25f)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                float sgn = pass == 0 ? 1f : -1f;
                Color ghostTint = pass == 0
                    ? new Color((byte)255, (byte)40, (byte)60, (byte)(36 * afterimage))
                    : new Color((byte)50, (byte)205, (byte)255, (byte)(36 * afterimage));

                Rlgl.PushMatrix();
                Rlgl.Translatef(pos.X + sway + 0.025f * sgn, pos.Y + 0.008f * sgn, pos.Z - 0.015f);
                Rlgl.Rotatef(yawDeg + ExternalYawOffset + twitchYaw + 2.5f * sgn, 0f, 1f, 0f);
                Rlgl.Rotatef(twitchRoll + 1.8f * sgn, 0f, 0f, 1f);
                Rlgl.Translatef(autoPivotOffset.X * ExternalScale, autoPivotOffset.Y * ExternalScale, autoPivotOffset.Z * ExternalScale);
                Raylib.DrawModelEx(externalModel, Vector3.Zero, Vector3.UnitY, 0f, new Vector3(finalScale), ghostTint);
                Rlgl.PopMatrix();
            }
        }

        // Bật lại Backface Culling cho phần còn lại của game
        Rlgl.EnableBackfaceCulling();

        // 3. Khói đen quẩn dưới chân
        gBright = MathF.Max(0.05f, bright);
        gAlpha = 1f;
        gTint = Vector3.One;

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X + sway, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0f, 1f, 0f);
        DrawGroundAndSmoke(time);
        Rlgl.PopMatrix();

        // 4. Tùy chọn ánh mắt đỏ (bấm G trong F9 để bật/tắt)
        if (ExternalEyeGlow)
        {
            Vector3 faceWorld = GetLiveFaceWorld(pos, yawDeg, time, lunge, twitch);
            float r = yawDeg * MathF.PI / 180f;
            Vector3 rightVec = new(MathF.Cos(r), 0f, -MathF.Sin(r));
            Vector3 fwdVec = new(MathF.Sin(r), 0f, MathF.Cos(r));

            float eyeSep = 0.032f * ExternalScale;
            Vector3 eyeL = faceWorld - rightVec * eyeSep + fwdVec * 0.015f;
            Vector3 eyeR = faceWorld + rightVec * eyeSep + fwdVec * 0.015f;

            float gl = (bright < 0.2f ? 1.0f : 0.55f + 0.45f * scream) * (0.85f + 0.15f * MathF.Sin(time * 23f));
            Raylib.BeginBlendMode(BlendMode.Additive);
            Raylib.DrawSphereEx(eyeL, 0.008f, 6, 8, E(255, 60, 50, (int)(200f * gl)));
            Raylib.DrawSphereEx(eyeR, 0.008f, 6, 8, E(255, 60, 50, (int)(200f * gl)));
            Raylib.EndBlendMode();
        }
    }

    // =====================================================================
    //  FALLBACK PROCEDURAL (Giữ nguyên bên dưới)
    // =====================================================================
    static void ShadedBlob(Vector3 c, Vector3 r, Color col, int rings = 8, int slices = 12, float shade = 0.45f)
    {
        Reserve(rings * slices * 8 + 16);
        Rlgl.Begin(DrawMode.Quads);
        for (int i = 0; i < rings; i++)
        {
            float lat0 = MathF.PI * ((float)i / rings - 0.5f);
            float lat1 = MathF.PI * ((float)(i + 1) / rings - 0.5f);
            float cy0 = MathF.Cos(lat0), sy0 = MathF.Sin(lat0);
            float cy1 = MathF.Cos(lat1), sy1 = MathF.Sin(lat1);

            for (int j = 0; j < slices; j++)
            {
                float lon0 = MathF.PI * 2f * j / slices;
                float lon1 = MathF.PI * 2f * (j + 1) / slices;
                float sx0 = MathF.Sin(lon0), cx0 = MathF.Cos(lon0);
                float sx1 = MathF.Sin(lon1), cx1 = MathF.Cos(lon1);

                Vector3 v00 = new(cy0 * sx0, sy0, cy0 * cx0);
                Vector3 v01 = new(cy0 * sx1, sy0, cy0 * cx1);
                Vector3 v11 = new(cy1 * sx1, sy1, cy1 * cx1);
                Vector3 v10 = new(cy1 * sx0, sy1, cy1 * cx0);

                Vector3 p00 = c + new Vector3(r.X * v00.X, r.Y * v00.Y, r.Z * v00.Z);
                Vector3 p01 = c + new Vector3(r.X * v01.X, r.Y * v01.Y, r.Z * v01.Z);
                Vector3 p11 = c + new Vector3(r.X * v11.X, r.Y * v11.Y, r.Z * v11.Z);
                Vector3 p10 = c + new Vector3(r.X * v10.X, r.Y * v10.Y, r.Z * v10.Z);

                Color c00 = Shade(col, Vector3.Normalize(new Vector3(v00.X / r.X, v00.Y / r.Y, v00.Z / r.Z)), shade);
                Color c01 = Shade(col, Vector3.Normalize(new Vector3(v01.X / r.X, v01.Y / r.Y, v01.Z / r.Z)), shade);
                Color c11 = Shade(col, Vector3.Normalize(new Vector3(v11.X / r.X, v11.Y / r.Y, v11.Z / r.Z)), shade);
                Color c10 = Shade(col, Vector3.Normalize(new Vector3(v10.X / r.X, v10.Y / r.Y, v10.Z / r.Z)), shade);

                Vert(p00, c00); Vert(p01, c01); Vert(p11, c11); Vert(p10, c10);
                Vert(p10, c10); Vert(p11, c11); Vert(p01, c01); Vert(p00, c00);
            }
        }
        Rlgl.End();
    }

    static void Blob(Vector3 c, Vector3 r, Color col, int rings = 6, int slices = 9)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(c.X, c.Y, c.Z);
        Rlgl.Scalef(r.X, r.Y, r.Z);
        Raylib.DrawSphereEx(Vector3.Zero, 1f, rings, slices, col);
        Rlgl.PopMatrix();
    }

    static void Tube(Vector3 a, Vector3 b, float ra, float rb, Color col, int sides = 10, float shade = 0.5f)
    {
        Vector3 axis = b - a;
        float len = axis.Length();
        if (len < 1e-4f) return;
        axis /= len;

        Vector3 helper = MathF.Abs(axis.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX;
        Vector3 u = Vector3.Normalize(Vector3.Cross(axis, helper));
        Vector3 v = Vector3.Cross(axis, u);

        Reserve(sides * 8 + 16);
        Rlgl.Begin(DrawMode.Quads);
        for (int i = 0; i < sides; i++)
        {
            float a0 = MathF.PI * 2f * i / sides;
            float a1 = MathF.PI * 2f * (i + 1) / sides;
            Vector3 d0 = u * MathF.Cos(a0) + v * MathF.Sin(a0);
            Vector3 d1 = u * MathF.Cos(a1) + v * MathF.Sin(a1);
            Color c0 = Shade(col, d0, shade);
            Color c1 = Shade(col, d1, shade);

            Vector3 p00 = a + d0 * ra, p01 = a + d1 * ra;
            Vector3 p11 = b + d1 * rb, p10 = b + d0 * rb;

            Vert(p00, c0); Vert(p01, c1); Vert(p11, c1); Vert(p10, c0);
            Vert(p10, c0); Vert(p11, c1); Vert(p01, c1); Vert(p00, c0);
        }
        Rlgl.End();
    }

    static void Strand(Vector3 root, float length, int segs, float width, float swayAmp, float swaySpeed,
                       float phase, float time, Vector3 drift, Color rootCol, Color tipCol)
    {
        Reserve(segs * 8 + 16);
        Rlgl.Begin(DrawMode.Quads);

        Vector3 prev = root;
        for (int i = 1; i <= segs; i++)
        {
            float t0 = (i - 1) / (float)segs;
            float t = i / (float)segs;

            Vector3 p = root + new Vector3(0f, -length * t, 0f) + drift * MathF.Min(1f, t * 3.5f);
            p.X += MathF.Sin(time * swaySpeed + phase + t * 5f) * swayAmp * t;
            p.Z += MathF.Cos(time * swaySpeed * 0.8f + phase * 1.7f + t * 4f) * swayAmp * 0.6f * t;

            float w0 = width * (1f - 0.8f * t0) * 0.5f;
            float w1 = width * (1f - 0.8f * t) * 0.5f;
            Color c0 = LerpC(rootCol, tipCol, t0);
            Color c1 = LerpC(rootCol, tipCol, t);

            Vector3 a0 = prev - new Vector3(w0, 0f, 0f), a1 = prev + new Vector3(w0, 0f, 0f);
            Vector3 b0 = p - new Vector3(w1, 0f, 0f), b1 = p + new Vector3(w1, 0f, 0f);

            Vert(a0, c0); Vert(a1, c0); Vert(b1, c1); Vert(b0, c1);
            Vert(b0, c1); Vert(b1, c1); Vert(a1, c0); Vert(a0, c0);

            prev = p;
        }
        Rlgl.End();
    }

    public static void Draw(Vector3 pos, float yawDeg, float time, float lungeProgress = 0f,
                            float twitchIntensity = 1f, float screamAmount = -1f, float brightness = 1f)
    {
        EnsureLoaded();

        float lunge = Math.Clamp(lungeProgress, 0f, 1f);
        float bright = Math.Clamp(brightness, 0f, 1f);
        float scream = screamAmount < 0f ? Sm((lunge - 0.25f) / 0.45f) : Math.Clamp(screamAmount, 0f, 1f);
        float twitch = Math.Clamp(twitchIntensity, 0f, 2f);

        if (externalLoaded)
        {
            DrawExternalGhost(pos, yawDeg, time, lunge, twitch, scream, bright);
            return;
        }

        float snap = MathF.Floor(time * 22f) / 22f;
        float sway = ComputeBodySway(time, twitch);

        gBright = MathF.Max(0.035f, bright);
        gAlpha = 1f;
        gTint = Vector3.One;

        Pose k = BuildPose(lunge, lunge, time, twitch, true);

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X + sway, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0f, 1f, 0f);

        DrawGroundAndSmoke(time);
        DrawGown(time);

        Rlgl.PushMatrix();
        Rlgl.Translatef(0f, 1.0f, 0f);
        Rlgl.Rotatef(k.Hunch, 1f, 0f, 0f);

        DrawTorso();
        DrawArm(+1, lunge, time, snap, twitch);
        DrawArm(-1, lunge, time, snap, twitch);

        Rlgl.PushMatrix();
        Rlgl.Translatef(0f, 0.60f, 0.02f);
        Rlgl.Rotatef(k.NeckPitch, 1f, 0f, 0f);
        DrawNeck();

        Rlgl.PushMatrix();
        Rlgl.Translatef(0f, 0.26f, 0f);

        float afterimage = twitch * scream;
        if (afterimage > 0.25f)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                float sgn = pass == 0 ? 1f : -1f;
                gTint = pass == 0 ? new Vector3(1f, 0.22f, 0.32f) : new Vector3(0.28f, 0.82f, 1f);
                gAlpha = 0.16f * MathF.Min(1f, afterimage);

                Rlgl.PushMatrix();
                Rlgl.Translatef(0.028f * sgn, 0.008f * sgn, -0.008f);
                Rlgl.Rotatef(k.HeadYaw + 4f * sgn, 0f, 1f, 0f);
                Rlgl.Rotatef(k.HeadPitch, 1f, 0f, 0f);
                Rlgl.Rotatef(k.HeadRoll + 5f * sgn, 0f, 0f, 1f);
                DrawHead(time, scream, lunge, snap, twitch);
                Rlgl.PopMatrix();
            }
            gTint = Vector3.One;
            gAlpha = 1f;
        }

        Rlgl.Rotatef(k.HeadYaw, 0f, 1f, 0f);
        Rlgl.Rotatef(k.HeadPitch, 1f, 0f, 0f);
        Rlgl.Rotatef(k.HeadRoll, 0f, 0f, 1f);
        DrawHead(time, scream, lunge, snap, twitch);

        Rlgl.PopMatrix();
        Rlgl.PopMatrix();
        Rlgl.PopMatrix();

        DrawHair(k, time, lunge, twitch, scream);

        Rlgl.PopMatrix();

        gBright = 1f;
        gAlpha = 1f;
        gTint = Vector3.One;
    }

    static void DrawGroundAndSmoke(float time)
    {
        Raylib.DrawCylinder(new Vector3(0f, 0.004f, 0f), 0.62f, 0.62f, 0.002f, 20, E(0, 0, 0, 120));

        for (int s = 0; s < 9; s++)
        {
            float ang = time * 1.9f + s * (MathF.PI * 2f / 9f);
            float rad = 0.28f + MathF.Sin(time * 4f + s * 1.7f) * 0.10f;
            float sy = 0.06f + ((time * 0.55f + s * 0.19f) % 0.85f);
            float fade = 1f - sy / 0.95f;
            Blob(new Vector3(MathF.Cos(ang) * rad, sy, MathF.Sin(ang) * rad),
                 new Vector3(0.14f, 0.11f, 0.14f), E(8, 8, 12, (int)(120f * fade)), 6, 8);
        }
    }

    static void DrawGown(float time)
    {
        Color gown = C(15, 14, 19);
        Color gownLight = C(28, 26, 34);

        Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, 1.02f, 0f), 0.345f, 0.17f, gown, 18, 0.55f);

        for (int i = 0; i < 16; i++)
        {
            float a = i * (MathF.PI * 2f / 16f) + (i % 3) * 0.05f;
            float y0 = 0.46f + (i % 4) * 0.09f;
            float rr = 0.345f - (0.175f * y0 / 1.02f) + 0.018f;
            Vector3 root = new(MathF.Cos(a) * rr, y0, MathF.Sin(a) * rr);
            float len = 0.30f + (i % 5) * 0.07f;
            Strand(root, len, 5, 0.075f, 0.025f, 7f + i % 3, i * 1.9f, time,
                new Vector3(MathF.Cos(a) * 0.05f, 0f, MathF.Sin(a) * 0.05f),
                gownLight, C(8, 7, 10, 0));
        }
    }

    static void DrawTorso()
    {
        Color gown = C(17, 16, 22);
        Color skin = C(184, 189, 184);
        Color skinDark = C(104, 110, 118);
        Color bone = C(222, 218, 204);

        Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.30f, 0f), 0.145f, 0.118f, gown, 14, 0.55f);
        Tube(new Vector3(0f, 0.30f, 0f), new Vector3(0f, 0.60f, 0.01f), 0.118f, 0.150f, gown, 14, 0.55f);

        ShadedBlob(new Vector3(0f, 0.44f, 0.110f), new Vector3(0.085f, 0.17f, 0.045f), skin, 6, 10, 0.55f);
        for (int r = 0; r < 5; r++)
        {
            float ry = 0.33f + r * 0.058f;
            float rw = 0.070f + (r < 2 ? r * 0.010f : (4 - r) * 0.008f);
            Raylib.DrawCube(new Vector3(-rw * 0.55f, ry, 0.148f), rw * 0.95f, 0.013f, 0.014f, bone);
            Raylib.DrawCube(new Vector3(rw * 0.55f, ry, 0.148f), rw * 0.95f, 0.013f, 0.014f, bone);
            Raylib.DrawCube(new Vector3(0f, ry - 0.015f, 0.147f), 0.12f, 0.006f, 0.008f, skinDark);
        }
        Raylib.DrawCube(new Vector3(0f, 0.44f, 0.150f), 0.012f, 0.19f, 0.012f, bone);

        Blob(new Vector3(-0.235f, 0.585f, 0.010f), new Vector3(0.075f, 0.065f, 0.075f), gown);
        Blob(new Vector3(0.230f, 0.525f, 0.010f), new Vector3(0.072f, 0.062f, 0.072f), gown);
        Tube(new Vector3(-0.19f, 0.60f, 0.05f), new Vector3(-0.02f, 0.625f, 0.07f), 0.011f, 0.011f, skin, 6, 0.5f);
        Tube(new Vector3(0.19f, 0.55f, 0.05f), new Vector3(0.02f, 0.625f, 0.07f), 0.011f, 0.011f, skin, 6, 0.5f);
    }

    static void DrawNeck()
    {
        Color skin = C(180, 185, 180);
        Color dark = C(100, 106, 114);
        Color bruise = C(66, 62, 84);

        Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.27f, 0f), 0.054f, 0.040f, skin, 10, 0.55f);
        Tube(new Vector3(0f, 0.012f, 0f), new Vector3(0f, 0.050f, 0f), 0.058f, 0.056f, bruise, 10, 0.5f);
        Raylib.DrawLine3D(new Vector3(-0.026f, 0.02f, 0.040f), new Vector3(-0.016f, 0.25f, 0.030f), dark);
        Raylib.DrawLine3D(new Vector3(0.026f, 0.02f, 0.040f), new Vector3(0.016f, 0.25f, 0.030f), dark);
    }

    static void DrawArm(int side, float lunge, float time, float snap, float twitch)
    {
        bool right = side > 0;
        Color sleeve = C(18, 17, 23);
        Color skin = C(184, 189, 184);
        Color skinDark = C(100, 106, 114);
        Color cloth = C(38, 34, 42);

        float reach = ReachScale;
        float lift = 8f + lunge * (right ? 42f : 34f) * reach;
        float elbow = 10f + lunge * (right ? 34f : 28f) * reach;
        float tw = MathF.Sin(snap * 37f + side * 2f) * 7f * twitch;
        float spread = side * (6f + lunge * (right ? 14f : 18f));
        const float L1 = 0.40f, L2 = 0.43f;

        Rlgl.PushMatrix();
        Rlgl.Translatef(side * 0.235f, right ? 0.525f : 0.585f, 0.015f);
        Rlgl.Rotatef(-lift + tw, 1f, 0f, 0f);
        Rlgl.Rotatef(spread, 0f, 0f, 1f);

        Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, -L1, 0f), 0.046f, 0.036f, sleeve, 10, 0.55f);
        Blob(new Vector3(0f, -L1, 0f), new Vector3(0.040f, 0.040f, 0.040f), skinDark, 6, 8);

        Rlgl.Translatef(0f, -L1, 0f);
        Rlgl.Rotatef(-elbow, 1f, 0f, 0f);

        Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, -L2, 0f), 0.034f, 0.025f, skin, 10, 0.55f);
        Raylib.DrawLine3D(new Vector3(0.012f, -0.03f, 0.026f), new Vector3(0.008f, -L2 * 0.92f, 0.020f), skinDark);
        Raylib.DrawLine3D(new Vector3(-0.012f, -0.03f, 0.026f), new Vector3(-0.008f, -L2 * 0.92f, 0.020f), skinDark);
        Tube(new Vector3(0f, -L2 * 0.70f, 0f), new Vector3(0f, -L2 * 0.80f, 0f), 0.030f, 0.029f, cloth, 8, 0.5f);

        Rlgl.Translatef(0f, -L2, 0f);
        DrawHand(side, lunge, time);

        Rlgl.PopMatrix();
    }

    static void DrawHand(int side, float lunge, float time)
    {
        Color skin = C(186, 191, 186);
        Color nail = C(6, 6, 8);

        Blob(new Vector3(0f, -0.040f, 0f), new Vector3(0.040f, 0.052f, 0.017f), skin, 6, 9);

        float curlBase = 16f + lunge * 26f;
        for (int f = -2; f <= 2; f++)
        {
            float fs = f == 0 ? 1.18f : (Math.Abs(f) == 1 ? 1.05f : 0.88f);
            float spreadDeg = f * (6f + lunge * 9f);
            float wave = MathF.Sin(time * 13f + f * 1.3f + side) * 10f * (0.35f + lunge);

            Rlgl.PushMatrix();
            Rlgl.Translatef(f * 0.0155f, -0.086f, 0f);
            Rlgl.Rotatef(spreadDeg, 0f, 0f, 1f);

            for (int j = 0; j < 3; j++)
            {
                Rlgl.Rotatef((curlBase + wave) * (0.65f + 0.35f * j), 1f, 0f, 0f);
                float len = FingerSeg[j] * fs;
                Raylib.DrawCylinderEx(Vector3.Zero, new Vector3(0f, -len, 0f), 0.0085f - j * 0.0016f, 0.0075f - j * 0.0016f, 5, skin);
                Rlgl.Translatef(0f, -len, 0f);
            }

            Raylib.DrawCylinderEx(new Vector3(0f, 0.004f, 0f), new Vector3(0f, -0.058f, 0f), 0.0050f, 0f, 5, nail);
            Rlgl.PopMatrix();
        }
    }

    static void DrawHead(float time, float scream, float rev, float snap, float twitch)
    {
        Color skin = C(208, 212, 206);
        Color skinMid = C(164, 170, 166);
        Color skinDark = C(98, 104, 114);
        Color bruise = C(66, 62, 88);
        Color black = C(3, 3, 5);
        Color blood = C(115, 8, 16);
        Color lip = C(112, 58, 66);
        Color tooth = C(228, 214, 168);
        Color toothDark = C(158, 134, 88);
        Color gum = C(64, 10, 18);
        Color tongue = C(92, 26, 44);
        Color sclera = C(246, 240, 228);
        Color vein = C(175, 20, 30);
        Color skinVein = C(74, 82, 126);

        float open = Sm(scream);
        float jawAng = 5f + open * 50f + MathF.Abs(MathF.Sin(time * 38f)) * 5f * open;
        float gap = MathF.Sin(jawAng * MathF.PI / 180f) * 0.12f;

        Rlgl.PushMatrix();
        Rlgl.Scalef(1.12f, 1.12f + open * 0.08f, 1.12f);

        ShadedBlob(new Vector3(0f, 0.150f, -0.005f), new Vector3(0.116f, 0.148f, 0.128f), skin, 9, 14, 0.42f);
        ShadedBlob(new Vector3(0f, 0.085f, 0.030f), new Vector3(0.095f, 0.085f, 0.090f), skin, 7, 12, 0.42f);
        ShadedBlob(new Vector3(-0.072f, 0.068f, 0.082f), new Vector3(0.036f, 0.032f, 0.032f), skin, 6, 9, 0.42f);
        ShadedBlob(new Vector3(0.072f, 0.068f, 0.082f), new Vector3(0.036f, 0.032f, 0.032f), skin, 6, 9, 0.42f);
        Blob(new Vector3(-0.078f, 0.010f, 0.062f), new Vector3(0.030f, 0.052f, 0.040f), skinDark);
        Blob(new Vector3(0.078f, 0.010f, 0.062f), new Vector3(0.030f, 0.052f, 0.040f), skinDark);
        Blob(new Vector3(0f, 0.140f, 0.104f), new Vector3(0.100f, 0.014f, 0.030f), skinMid);

        Blob(new Vector3(-0.048f, 0.104f, 0.096f), new Vector3(0.052f, 0.058f, 0.022f), bruise);
        Blob(new Vector3(-0.048f, 0.105f, 0.100f), new Vector3(0.040f, 0.048f, 0.024f), black);
        Blob(new Vector3(-0.048f, 0.107f, 0.104f), new Vector3(0.030f, 0.034f, 0.021f), sclera, 7, 10);
        Blob(new Vector3(-0.048f, 0.107f, 0.1245f), new Vector3(0.0075f * (1f - 0.4f * open), 0.0075f * (1f - 0.4f * open), 0.004f), black, 5, 6);
        for (int v = 0; v < 6; v++)
        {
            float ang = v * 1.05f + 0.3f;
            float cx = MathF.Cos(ang), cy = MathF.Sin(ang);
            Raylib.DrawLine3D(
                new Vector3(-0.048f + cx * 0.011f, 0.107f + cy * 0.012f, 0.1235f),
                new Vector3(-0.048f + cx * 0.019f, 0.107f + cy * 0.021f, 0.1145f),
                vein);
        }

        Blob(new Vector3(0.048f, 0.102f, 0.094f), new Vector3(0.054f, 0.060f, 0.022f), bruise);
        Blob(new Vector3(0.048f, 0.105f, 0.098f), new Vector3(0.043f, 0.052f, 0.026f), black);

        Raylib.DrawCube(new Vector3(0f, 0.085f, 0.118f), 0.014f, 0.050f, 0.012f, skinMid);
        Blob(new Vector3(-0.012f, 0.052f, 0.118f), new Vector3(0.0065f, 0.015f, 0.008f), black, 5, 6);
        Blob(new Vector3(0.012f, 0.052f, 0.118f), new Vector3(0.0065f, 0.015f, 0.008f), black, 5, 6);

        Blob(new Vector3(0f, 0.016f, 0.092f), new Vector3(0.055f, 0.014f, 0.024f), lip);
        Blob(new Vector3(0f, -0.020f - gap * 0.35f, 0.055f), new Vector3(0.052f, 0.045f + gap * 0.5f, 0.050f), black, 7, 10);

        for (int t = -7; t <= 7; t++)
        {
            float x = t * 0.0078f;
            float z = 0.100f - MathF.Abs(t) * 0.0045f;
            float len = 0.013f + (Math.Abs(t * 7) % 4) * 0.0045f + open * 0.006f;
            Raylib.DrawCylinderEx(new Vector3(x, 0.012f, z), new Vector3(x, 0.012f - len, z), 0.0034f, 0f, 5, (t % 2 == 0) ? tooth : toothDark);
        }

        for (int s = -1; s <= 1; s += 2)
        {
            Blob(new Vector3(s * 0.058f, 0.012f - gap * 0.45f, 0.085f), new Vector3(0.014f, 0.012f + gap * 0.4f, 0.014f), blood, 5, 6);
            Vector3 m0 = new(s * 0.060f, 0.012f - gap * 0.4f, 0.088f);
            Vector3 m1 = new(s * 0.098f, 0.030f, 0.062f);
            Vector3 m2 = new(s * 0.112f, 0.062f, 0.020f);
            Raylib.DrawLine3D(m0, m1, blood);
            Raylib.DrawLine3D(m1, m2, blood);
            for (int st = 0; st < 4; st++)
            {
                Vector3 sp = Vector3.Lerp(m0, m1, (st + 0.5f) / 4f);
                Raylib.DrawLine3D(sp + new Vector3(0f, 0.008f, 0f), sp - new Vector3(0f, 0.008f, 0f), C(30, 22, 24));
            }
        }

        Rlgl.PushMatrix();
        Rlgl.Translatef(0f, 0.034f, 0.012f);
        Rlgl.Rotatef(jawAng, 1f, 0f, 0f);
        Rlgl.Translatef(0f, -0.010f * open, 0.012f * open);

        ShadedBlob(new Vector3(0f, -0.070f, 0.070f), new Vector3(0.058f, 0.044f, 0.050f), skin, 7, 12, 0.42f);
        ShadedBlob(new Vector3(-0.056f, -0.040f, 0.030f), new Vector3(0.024f, 0.048f, 0.050f), skinMid, 6, 8, 0.42f);
        ShadedBlob(new Vector3(0.056f, -0.040f, 0.030f), new Vector3(0.024f, 0.048f, 0.050f), skinMid, 6, 8, 0.42f);
        Blob(new Vector3(0f, -0.030f, 0.098f), new Vector3(0.050f, 0.010f, 0.018f), lip);
        Blob(new Vector3(0f, -0.034f, 0.070f), new Vector3(0.046f, 0.008f, 0.030f), gum);

        for (int t = -6; t <= 6; t++)
        {
            float x = t * 0.0082f;
            float z = 0.104f - MathF.Abs(t) * 0.0050f;
            float len = 0.012f + (Math.Abs(t * 5) % 3) * 0.005f + open * 0.005f;
            Raylib.DrawCylinderEx(new Vector3(x, -0.036f, z), new Vector3(x, -0.036f + len, z), 0.0034f, 0f, 5, (t % 2 == 0) ? toothDark : tooth);
        }

        float tl = Sm((open - 0.1f) / 0.6f);
        if (tl > 0.02f)
        {
            Vector3 tp = new(0f, -0.034f, 0.060f);
            for (int s = 0; s < 6; s++)
            {
                float seg = 0.020f + 0.020f * tl;
                Vector3 next = tp + new Vector3(MathF.Sin(time * 11f + s * 0.9f) * 0.010f * tl, -seg * 0.9f, 0.012f + s * 0.004f);
                Raylib.DrawCylinderEx(tp, next, 0.014f - s * 0.0015f, 0.013f - s * 0.0016f, 6, tongue);
                tp = next;
            }
        }

        float drool = 0.05f + 0.13f * open + 0.02f * MathF.Sin(time * 5f);
        Raylib.DrawCylinderEx(new Vector3(-0.030f, -0.030f, 0.096f), new Vector3(-0.030f, -0.030f - drool, 0.098f), 0.0040f, 0.0015f, 4, C(40, 4, 8));
        Raylib.DrawCylinderEx(new Vector3(0.026f, -0.030f, 0.096f), new Vector3(0.026f, -0.030f - drool * 1.2f, 0.098f), 0.0045f, 0.0015f, 4, C(40, 4, 8));
        Rlgl.PopMatrix();

        for (int v = 0; v < 3; v++)
        {
            float x0 = (v - 1) * 0.045f;
            Vector3 prev = default;
            for (int j = 0; j < 4; j++)
            {
                float x = x0 + MathF.Sin(j * 1.3f + v) * 0.012f;
                float y = 0.215f - j * 0.022f;
                float inside = 1f - (x / 0.116f) * (x / 0.116f) - ((y - 0.15f) / 0.148f) * ((y - 0.15f) / 0.148f);
                float z = -0.005f + 0.128f * MathF.Sqrt(MathF.Max(0.05f, inside)) + 0.002f;
                Vector3 p = new(x, y, z);
                if (j > 0) Raylib.DrawLine3D(prev, p, skinVein);
                prev = p;
            }
        }
        Raylib.DrawLine3D(new Vector3(0.060f, 0.230f, 0.085f), new Vector3(0.085f, 0.190f, 0.090f), skinDark);
        Raylib.DrawLine3D(new Vector3(0.085f, 0.190f, 0.090f), new Vector3(0.078f, 0.160f, 0.105f), skinDark);

        float tear = 0.06f + 0.05f * open + 0.015f * MathF.Sin(time * 3f);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 a = new(s * 0.048f, 0.082f, 0.112f);
            Vector3 b = new(s * 0.052f, 0.082f - tear, 0.108f);
            Raylib.DrawCylinderEx(a, b, 0.0055f, 0.0018f, 4, C(22, 2, 6));
            float bead = (time * 0.35f + s * 0.3f) % 1f;
            Raylib.DrawSphereEx(b + new Vector3(0f, -bead * 0.06f, 0f), 0.0065f * (1f - bead * 0.5f), 4, 5, C(22, 2, 6));
        }

        Raylib.BeginBlendMode(BlendMode.Additive);
        float gl = 0.65f + 0.35f * scream + 0.15f * MathF.Sin(time * 23f);
        Vector3 eR = new(0.048f, 0.105f, 0.118f);
        Raylib.DrawSphereEx(eR, 0.012f, 6, 8, E(255, 70, 60, (int)(235f * gl)));
        Raylib.DrawSphereEx(eR, 0.026f, 6, 8, E(255, 20, 30, (int)(95f * gl)));
        Raylib.DrawSphereEx(eR, 0.046f, 6, 8, E(200, 0, 10, (int)(32f * gl)));
        Raylib.DrawSphereEx(new Vector3(-0.048f, 0.107f, 0.128f), 0.010f, 6, 8, E(255, 255, 255, (int)(55f * gl)));
        Raylib.EndBlendMode();

        Rlgl.PopMatrix();
    }

    static void DrawHair(Pose k, float time, float rev, float twitch, float scream)
    {
        Color rootC = C(5, 5, 7);
        Color tipC = C(11, 11, 15, 0);
        float part = Sm(rev);
        float agit = 0.45f + twitch * 0.7f + scream * 0.6f;

        const int front = 32;
        for (int i = 0; i < front; i++)
        {
            float u = ((i + 0.5f) / front) * 2f - 1f;
            float az = u * 1.0f;
            float el = 1.0f - 0.18f * MathF.Abs(u);
            Vector3 pl = new(
                0.118f * MathF.Cos(el) * MathF.Sin(az),
                0.150f + 0.150f * MathF.Sin(el),
                0.130f * MathF.Cos(el) * MathF.Cos(az));
            Vector3 root = HeadToGhost(k, pl * 1.12f);

            float sgn = u < 0f ? -1f : 1f;
            float len = 0.80f + 0.45f * HashF(i, 3);
            Vector3 drift = new(
                sgn * (0.02f + 0.22f * part * (0.45f + MathF.Abs(u))),
                0f,
                0.085f * (1f - part) - 0.035f * part);
            Strand(root, len, 8, 0.025f, 0.016f * agit, 6f + (i % 4), i * 1.37f, time, drift, rootC, tipC);
        }

        const int back = 90;
        for (int i = 0; i < back; i++)
        {
            float az = 0.7f + (i / (float)back) * (MathF.PI * 2f - 1.4f);
            float el = 0.15f + 0.95f * HashF(i, 7);
            Vector3 pl = new(
                0.118f * MathF.Cos(el) * MathF.Sin(az),
                0.150f + 0.150f * MathF.Sin(el),
                0.130f * MathF.Cos(el) * MathF.Cos(az));
            Vector3 root = HeadToGhost(k, pl * 1.12f);

            float len = 0.90f + 0.60f * HashF(i, 11);
            Vector3 drift = new(MathF.Sin(az) * 0.045f, 0f, MathF.Cos(az) * 0.02f - 0.015f * part);
            Strand(root, len, 8, 0.030f, 0.022f * agit, 5f + (i % 5), i * 0.93f, time, drift, rootC, tipC);
        }
    }
}