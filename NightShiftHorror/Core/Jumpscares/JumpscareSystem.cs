using System.Numerics;
using Raylib_cs;

partial class Program
{
    public enum JumpscarePreset
    {
        CounterLunge = 0,
        WashroomMirror = 1,
        FrontDoorRush = 2,
        AisleAmbush = 3
    }

    static Sound jumpscareSound;
    static bool jumpscareSoundLoaded = false;
    static bool jumpscareSoundChecked = false;

    static bool jumpscareActive = false;
    static JumpscarePreset activeJumpscarePreset = JumpscarePreset.CounterLunge;
    static float jumpscareTimer = 0f;
    static float jumpscareDuration = 2.0f;
    static bool jumpscareAudioTriggered = false;

    static bool jsLit = true;
    static float jsGhostBright = 1f;
    static float jsScream = 0f;
    static float jsTwitch = 0.25f;
    static float jsFlash = 0f;
    static float jsAber = 0f;
    static float jsZoom = 0f;

    static RenderTexture2D jsTarget;
    static bool jsTargetLoaded = false;
    static readonly Random jsRng = new(99);

    static float JsSm(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        return x * x * (3f - 2f * x);
    }

    static int JsHash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 374761393 + b * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (int)(h & 0x7fffffff);
        }
    }

    static float JsRand(int a, int b) => (JsHash(a, b) & 0xFFFF) / 65535f;

    static Color JsColor(int r, int g, int b, float a = 255f)
        => new Color((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255),
                     (byte)Math.Clamp((int)a, 0, 255));

    static float JsImpactTime(float dur)
        => MathF.Min(jumpscareSoundLoaded ? 0.05f : 0.16f, dur * 0.35f);

    static void EnsureJumpscareTarget()
    {
        if (jsTargetLoaded && jsTarget.Id > 0) return;
        jsTarget = Raylib.LoadRenderTexture(ScreenWidth, ScreenHeight);
        if (jsTarget.Id > 0)
        {
            Raylib.SetTextureFilter(jsTarget.Texture, TextureFilter.Bilinear);
            jsTargetLoaded = true;
        }
    }

    static void EnsureJumpscareAudio()
    {
        if (jumpscareSoundChecked) return;
        jumpscareSoundChecked = true;

        string baseDir = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDir, "Assets", "Audio", "jumpscare.mp3"),
            Path.Combine(baseDir, "Assets", "Audio", "jumpscare.wav"),
            Path.Combine(baseDir, "Assets", "Audio", "jumpscare.ogg"),
            "Assets/Audio/jumpscare.mp3",
            "Assets/Audio/jumpscare.wav",
            "Assets/Audio/jumpscare.ogg"
        ];

        string found = candidates.FirstOrDefault(File.Exists) ?? "";
        if (found != "")
        {
            jumpscareSound = Raylib.LoadSound(found);
            jumpscareSoundLoaded = jumpscareSound.FrameCount > 0;
        }
    }

    public static void UnloadJumpscareAudio()
    {
        if (jumpscareSoundLoaded)
        {
            Raylib.UnloadSound(jumpscareSound);
            jumpscareSoundLoaded = false;
        }
        if (jsTargetLoaded)
        {
            Raylib.UnloadRenderTexture(jsTarget);
            jsTargetLoaded = false;
        }
        jumpscareSoundChecked = false;
    }

    static void PlayJumpscareAudioBurst()
    {
        EnsureJumpscareAudio();
        float sfxVol = Math.Clamp(homeSfxVolume, 0.25f, 1.0f);

        if (jumpscareSoundLoaded)
        {
            if (Raylib.IsSoundPlaying(jumpscareSound))
                Raylib.StopSound(jumpscareSound);

            Raylib.SetSoundVolume(jumpscareSound, sfxVol);
            Raylib.SetSoundPitch(jumpscareSound, 1.0f);
            Raylib.PlaySound(jumpscareSound);
        }
        else
        {
            Raylib.SetSoundVolume(knockSound, sfxVol);
            Raylib.SetSoundPitch(knockSound, 0.6f);
            Raylib.PlaySound(knockSound);
        }
    }

    public static void TriggerJumpscare(JumpscarePreset preset, float duration = 2.0f)
    {
        EnsureJumpscareAudio();
        activeJumpscarePreset = preset;
        jumpscareDuration = duration;
        jumpscareTimer = 0f;
        jumpscareActive = true;
        jumpscareAudioTriggered = false;
    }

    static void ComputeJumpscarePose(JumpscarePreset preset, float tNorm, bool isActing,
                                     out Vector3 camPos, out Vector3 camTarget, out Vector3 camUp, out float fov,
                                     out Vector3 ghostPos, out float ghostYaw, out float lunge)
    {
        float dur = MathF.Max(0.5f, jumpscareDuration);
        float t = Math.Clamp(tNorm, 0f, 1f) * dur;
        float ti = JsImpactTime(dur);
        float u = t - ti;
        float time = (float)Raylib.GetTime();

        Vector3 camBase;
        Vector3 ghostStart;
        float baseFov;

        switch (preset)
        {
            case JumpscarePreset.WashroomMirror:
                camBase = new Vector3(-13.15f, 1.68f, -5.65f);
                ghostStart = new Vector3(-13.15f, 0f, -3.45f);
                baseFov = 62f;
                break;

            case JumpscarePreset.FrontDoorRush:
                camBase = new Vector3(0.0f, 1.70f, 4.20f);
                ghostStart = new Vector3(0.0f, 0f, 8.20f);
                baseFov = 64f;
                break;

            case JumpscarePreset.AisleAmbush:
                camBase = new Vector3(2.05f, 1.68f, 4.30f);
                ghostStart = new Vector3(2.05f, 0f, -2.60f);
                baseFov = 62f;
                break;

            case JumpscarePreset.CounterLunge:
            default:
                camBase = new Vector3(-2.60f, 1.70f, 1.20f);
                ghostStart = new Vector3(-2.60f, 0f, -1.85f);
                baseFov = 62f;
                break;
        }

        Vector3 toCam = camBase - ghostStart;
        ghostYaw = MathF.Atan2(toCam.X, toCam.Z) * (180f / MathF.PI);

        Vector3 dir = Vector3.Normalize(new Vector3(ghostStart.X - camBase.X, 0f, ghostStart.Z - camBase.Z));
        float yawRad = ghostYaw * MathF.PI / 180f;

        float creep = Math.Clamp((u - 0.15f) / 1.2f, 0f, 1f);
        float stop = MathF.Max(0.32f, 0.35f * (1f - 0.16f * JsSm(creep)));

        Vector3 faceFull = GhostModel.GetFaceLocal(1f, 1f);
        float fx = faceFull.X * MathF.Cos(yawRad) + faceFull.Z * MathF.Sin(yawRad);
        float fz = -faceFull.X * MathF.Sin(yawRad) + faceFull.Z * MathF.Cos(yawRad);
        Vector3 ghostEnd = new(camBase.X + dir.X * stop - fx, 0f, camBase.Z + dir.Z * stop - fz);

        float s;
        if (u < 0f)
        {
            float preT = Math.Clamp(t / MathF.Max(0.01f, ti), 0f, 1f);
            s = 0.48f * JsSm(preT);
        }
        else
        {
            float r = Math.Clamp(u / 0.09f, 0f, 1f);
            float e = 1f - MathF.Pow(1f - r, 4.0f);
            s = 0.48f + (1f - 0.48f) * e;
            if (u > 0.09f) s += 0.04f * MathF.Exp(-(u - 0.09f) * 14f);
        }
        ghostPos = Vector3.Lerp(ghostStart, ghostEnd, s);
        ghostPos.Y = 0f;

        lunge = u < 0f ? 0f : JsSm(u / 0.12f);

        bool lit;
        float gb;
        if (u < 0f)
        {
            bool flick1 = t >= 0.045f && t < 0.085f;
            bool flick2 = t >= 0.105f && t < 0.145f;
            lit = flick1 || flick2;
            gb = lit ? 0.72f : 0.08f;
        }
        else if (u < 0.40f)
        {
            lit = ((int)(t * 22f) % 2) == 0;
            gb = 1f;
        }
        else
        {
            lit = JsHash((int)(t * 24f), 91) % 11 != 0;
            gb = 1f;
        }

        jsLit = lit;
        jsGhostBright = gb;
        jsFlash = u >= 0f ? 0.72f * MathF.Exp(-u * 26f) : 0f;
        jsAber = u >= 0f ? 2.2f + 12f * MathF.Exp(-u * 5.5f) : (lit ? 2.5f : 0.3f);
        jsZoom = u >= 0f ? 0.85f * MathF.Exp(-u * 9f) : 0f;
        jsScream = u >= 0f ? JsSm((u - 0.01f) / 0.08f) : 0f;
        jsTwitch = u >= 0f ? 0.30f + 0.65f * MathF.Exp(-u * 1.1f) : 0.25f;

        Vector3 shake = Vector3.Zero;
        float roll = 0f;
        if (isActing)
        {
            float amp = u < 0f
                ? 0.003f * (0.4f + JsSm(t / MathF.Max(0.01f, ti)))
                : 0.026f * MathF.Exp(-u * 3.8f) + 0.002f;

            shake = new Vector3(
                MathF.Sin(time * 54f) * amp,
                MathF.Cos(time * 63f) * amp * 0.7f,
                MathF.Sin(time * 47f) * amp * 0.4f);

            roll = u < 0f ? 0f : MathF.Sin(u * 34f) * 0.07f * MathF.Exp(-u * 5.5f) + MathF.Sin(time * 22f) * 0.007f;
        }

        float rec = u >= 0f ? MathF.Exp(-u * 8f) : 0f;
        camPos = camBase - dir * (0.10f * rec) + new Vector3(0f, -0.04f * rec, 0f) + shake;

        Vector3 liveFace = GhostModel.GetLiveFaceWorld(ghostPos, ghostYaw, time, lunge, jsTwitch);
        camTarget = liveFace + shake * 0.20f;

        camUp = Vector3.Normalize(new Vector3(MathF.Sin(roll), MathF.Cos(roll), 0f));

        fov = u < 0f
            ? baseFov - 3f * JsSm(t / MathF.Max(0.01f, ti))
            : baseFov - 4f + 14f * MathF.Exp(-u * 14f) - 6f * JsSm((u - 0.1f) / 1.8f);
    }

    static void DrawJsGrain(int count, int maxAlpha)
    {
        int W = ScreenWidth, H = ScreenHeight;
        for (int i = 0; i < count; i++)
        {
            bool light = jsRng.Next(3) != 0;
            Raylib.DrawRectangle(jsRng.Next(W), jsRng.Next(H), jsRng.Next(1, 4), jsRng.Next(1, 3),
                light ? JsColor(255, 255, 255, jsRng.Next(8, Math.Max(9, maxAlpha)))
                      : JsColor(0, 0, 0, jsRng.Next(14, Math.Max(15, maxAlpha + 10))));
        }
    }

    static void DrawJsStaticBurst(float k)
    {
        int W = ScreenWidth, H = ScreenHeight;
        int n = (int)(650f * k);
        for (int i = 0; i < n; i++)
        {
            int g = jsRng.Next(60, 255);
            Raylib.DrawRectangle(jsRng.Next(W), jsRng.Next(H), jsRng.Next(2, 14), jsRng.Next(1, 3), JsColor(g, g, g, 95f * k));
        }
        for (int b = 0; b < 3; b++)
        {
            Raylib.DrawRectangle(0, jsRng.Next(H), W, jsRng.Next(6, 36), JsColor(255, 255, 255, 24f * k));
        }
    }

    static void DrawJsLensSplatter(float u)
    {
        if (u < 0.02f) return;
        int W = ScreenWidth, H = ScreenHeight;
        float grow = Math.Clamp((u - 0.02f) / 0.10f, 0f, 1f);
        float drip = Math.Clamp((u - 0.10f) / 2.2f, 0f, 1f);

        for (int i = 0; i < 10; i++)
        {
            bool leftSide = (i % 2 == 0);
            float px = leftSide
                ? (0.03f + JsRand(i, 11) * 0.21f) * W
                : (0.76f + JsRand(i, 11) * 0.21f) * W;
            float py = (0.08f + JsRand(i, 12) * 0.78f) * H;
            float rad = (18f + JsRand(i, 13) * 44f) * grow;
            Color col = JsColor(75, 2, 8, 195f * grow);

            Raylib.DrawCircleV(new Vector2(px, py), rad, col);
            for (int k = 0; k < 4; k++)
            {
                float a = JsRand(i, 20 + k) * MathF.PI * 2f;
                float d = rad * (1.1f + JsRand(i, 30 + k) * 0.75f);
                Raylib.DrawCircleV(new Vector2(px + MathF.Cos(a) * d, py + MathF.Sin(a) * d), rad * (0.12f + JsRand(i, 40 + k) * 0.16f), col);
            }

            float len = (25f + JsRand(i, 14) * 140f) * drip;
            Raylib.DrawRectangle((int)(px - rad * 0.15f), (int)py, (int)MathF.Max(2f, rad * 0.30f), (int)len, col);
            Raylib.DrawCircleV(new Vector2(px, py + len), rad * 0.18f, col);
            Raylib.DrawCircleV(new Vector2(px - rad * 0.28f, py - rad * 0.28f), rad * 0.14f, JsColor(255, 200, 200, 34f * grow));
        }
    }

    static void DrawJumpscareScreenEffects(float tNorm, float time, bool isActing)
    {
        int W = ScreenWidth, H = ScreenHeight;
        float dur = MathF.Max(0.5f, jumpscareDuration);
        float t = Math.Clamp(tNorm, 0f, 1f) * dur;
        float ti = JsImpactTime(dur);
        float u = t - ti;

        if (u < 0f)
        {
            float p = Math.Clamp(t / MathF.Max(0.01f, ti), 0f, 1f);
            Raylib.DrawRectangle(0, 0, W, H, JsColor(0, 0, 0, 36f * p));
            DrawJsGrain((int)(100 + 220 * p), 32);
        }
        else
        {
            float bloodTint = isActing ? 46f * MathF.Exp(-u * 1.8f) + 16f : 16f;
            Raylib.DrawRectangle(0, 0, W, H, JsColor(140, 0, 10, bloodTint));

            if (isActing && u < 0.12f) DrawJsStaticBurst(1f - u / 0.12f);

            DrawJsLensSplatter(u);

            if (isActing)
            {
                float tailFade = JsSm((dur - t) / MathF.Max(0.01f, dur * 0.12f));
                int sliceCount = (int)MathF.Ceiling(7f * tailFade * MathF.Exp(-u * 0.9f));
                int seed = (int)(time * 30f);

                for (int i = 0; i < sliceCount; i++)
                {
                    int sy = JsHash(seed, i * 3 + 1) % Math.Max(1, H - 16);
                    int sh = 2 + (JsHash(seed, i * 3 + 2) % 10);
                    float a = Math.Clamp(30f * tailFade, 8f, 52f);
                    Color barCol = (i % 2 == 0) ? JsColor(255, 45, 85, a) : JsColor(60, 210, 255, a);
                    Raylib.DrawRectangle(0, sy, W, sh, barCol);
                }
            }

            DrawJsGrain(isActing ? 180 : 100, 26);
        }

        if (jsFlash > 0.01f)
            Raylib.DrawRectangle(0, 0, W, H, JsColor(255, 250, 250, 235f * jsFlash));

        float beat = u < 0f ? 0f : 0.5f + 0.5f * MathF.Sin(time * (8f + MathF.Min(u, 2f) * 3f));
        int edge = (int)(26f + 48f * beat);
        Raylib.DrawRectangleGradientV(0, 0, W, H / 5, JsColor(120, 0, 8, edge), JsColor(120, 0, 8, 0));
        Raylib.DrawRectangleGradientV(0, H - H / 5, W, H / 5, JsColor(120, 0, 8, 0), JsColor(120, 0, 8, edge));

        for (int y = 0; y < H; y += 3)
            Raylib.DrawRectangle(0, y, W, 1, JsColor(0, 0, 0, 20));

        DrawVignette(36 + (int)(62f * beat));
    }

    static void DrawJumpscareComposite(float aberration, float zoomBlur)
    {
        Texture2D tex = jsTarget.Texture;
        int W = ScreenWidth, H = ScreenHeight;
        Rectangle src = new(0, 0, tex.Width, -tex.Height);

        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawTexturePro(tex, src, new Rectangle(-aberration, -aberration * 0.25f, W, H), Vector2.Zero, 0f, JsColor(255, 0, 0));
        Raylib.DrawTexturePro(tex, src, new Rectangle(0, 0, W, H), Vector2.Zero, 0f, JsColor(0, 255, 0));
        Raylib.DrawTexturePro(tex, src, new Rectangle(aberration, aberration * 0.25f, W, H), Vector2.Zero, 0f, JsColor(0, 0, 255));
        Raylib.EndBlendMode();

        if (zoomBlur > 0.02f)
        {
            for (int k = 1; k <= 5; k++)
            {
                float sc = 1f + k * 0.015f * zoomBlur;
                Rectangle dst = new(-(W * sc - W) * 0.5f, -(H * sc - H) * 0.5f, W * sc, H * sc);
                Raylib.DrawTexturePro(tex, src, dst, Vector2.Zero, 0f, JsColor(255, 255, 255, 30f * zoomBlur));
            }
        }
    }

    static void DrawJumpscareDebris(JumpscarePreset preset, float u)
    {
        if (preset != JumpscarePreset.FrontDoorRush || u < 0f || u > 1.8f) return;

        for (int i = 0; i < 46; i++)
        {
            float r1 = JsRand(i, 1), r2 = JsRand(i, 2), r3 = JsRand(i, 3), r4 = JsRand(i, 4), r5 = JsRand(i, 5);
            Vector3 origin = new((r1 - 0.5f) * 2.3f, 0.25f + r2 * 2.1f, 7.55f);
            Vector3 vel = new((r3 - 0.5f) * 2.2f - origin.X * 0.35f, (r4 - 0.35f) * 1.6f, -(5.5f + r5 * 6.5f));
            Vector3 p = origin + vel * u + new Vector3(0f, -0.5f * 6.5f * u * u, 0f);
            if (p.Y < 0.02f) p.Y = 0.02f;

            float size = 0.05f + r2 * 0.13f;
            float spin = u * (6f + r4 * 14f) + r1 * 6.28f;
            float cs = MathF.Cos(spin), sn = MathF.Sin(spin);
            Vector3 a = new(size * cs, size * sn, 0f);
            Vector3 b = new(-size * 0.6f * sn, size * 0.9f * cs, size * 0.3f);
            Vector3 c = new(-size * cs, -size * 0.5f * sn, -size * 0.2f);

            Color col = JsColor(190, 220, 235, 150f * (1f - u / 1.8f));
            Raylib.DrawTriangle3D(p + a, p + b, p + c, col);
            Raylib.DrawTriangle3D(p + c, p + b, p + a, col);
        }
    }
}