using System.Numerics;
using System.Text;
using Raylib_cs;

partial class Program
{
    public enum JumpscarePreset
    {
        CounterLunge = 0,    // Lao thẳng từ sau quầy thu ngân
        WashroomMirror = 1,  // Xuất hiện sau lưng ở bồn rửa nhà vệ sinh
        FrontDoorRush = 2,   // Xông thẳng qua cửa kính chính
        AisleAmbush = 3      // Lao dọc hành lang giữa cửa hàng
    }

    // ----- Âm thanh -----
    static Sound jumpscareSound;                 // file ngoài (nếu có)
    static bool jumpscareSoundLoaded = false;
    static bool jumpscareSoundChecked = false;
    static Sound jumpscareProcSound;             // tiếng hù tự tổng hợp (khi không có file ngoài)
    static bool jumpscareProcLoaded = false;
    const float JsProcImpact = 0.16f;            // rút ngắn pha chờ để ma lao vào nhanh hơn

    // ----- Trạng thái jumpscare -----
    static bool jumpscareActive = false;
    static JumpscarePreset activeJumpscarePreset = JumpscarePreset.CounterLunge;
    static float jumpscareTimer = 0f;
    static float jumpscareDuration = 2.0f;
    static bool jumpscareAudioTriggered = false;

    // ----- Dev Sandbox -----
    static bool jumpscareDevSandbox = false;
    static int devSelectedPreset = 0;
    static float devStopDistance = 0.35f;   // khoảng cách sát ống kính để mặt ma ghé sát màn hình
    static bool devLightsOn = false;        // false = bản hù thật (tối đen, chớp đèn). Bật (L) để soi chi tiết model
    static bool devVhsGlitch = true;
    static bool jsSlowMo = false;
    static bool devShowHud = true;

    // ----- Kênh phụ: ComputeJumpscarePose cập nhật mỗi khung, nơi vẽ đọc lại -----
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

    // =====================================================================
    //  TIỆN ÍCH
    // =====================================================================
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
        => MathF.Min(jumpscareSoundLoaded ? 0.05f : JsProcImpact, dur * 0.35f);

    static void EnsureJumpscareTarget()
    {
        if (jsTargetLoaded) return;
        jsTarget = Raylib.LoadRenderTexture(ScreenWidth, ScreenHeight);
        Raylib.SetTextureFilter(jsTarget.Texture, TextureFilter.Bilinear);
        jsTargetLoaded = true;
    }

    // =====================================================================
    //  ÂM THANH
    // =====================================================================
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

        if (!jumpscareSoundLoaded)
        {
            jumpscareProcSound = JsBuildScareSound();
            jumpscareProcLoaded = jumpscareProcSound.FrameCount > 0;
        }
    }

    public static void UnloadJumpscareAudio()
    {
        if (jumpscareSoundLoaded)
        {
            Raylib.UnloadSound(jumpscareSound);
            jumpscareSoundLoaded = false;
        }
        if (jumpscareProcLoaded)
        {
            Raylib.UnloadSound(jumpscareProcSound);
            jumpscareProcLoaded = false;
        }
        if (jsTargetLoaded)
        {
            Raylib.UnloadRenderTexture(jsTarget);
            jsTargetLoaded = false;
        }
        jumpscareSoundChecked = false;
    }

    static float JsThump(float t, float t0)
        => t < t0 ? 0f : MathF.Sin(2f * MathF.PI * 58f * (t - t0)) * MathF.Exp(-(t - t0) * 30f) * 0.5f;

    static Sound JsBuildScareSound()
    {
        const int sr = 22050;
        const float total = 3.0f;
        int n = (int)(sr * total);
        float[] buf = new float[n];
        Random rnd = new(1313);

        float ti = JsProcImpact;
        float dt = 1f / sr;
        float phRise = 0f, phBoom = 0f, phA = 0f, phB = 0f, phC = 0f, phG = 0f;
        float lpA = 0f, lpB = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i * dt;
            float s = 0f;
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
            lpA += (white - lpA) * 0.08f;
            lpB += (white - lpB) * 0.35f;

            if (t < ti)
            {
                float p = Math.Clamp(t / Math.Max(0.01f, ti - 0.03f), 0f, 1f);
                float gate = t < ti - 0.04f ? 1f : MathF.Max(0f, 1f - (t - (ti - 0.04f)) / 0.012f);
                float f = 70f + 520f * p * p;
                phRise += 2f * MathF.PI * f * dt;
                s += MathF.Sin(phRise) * 0.12f * p * p;
                s += lpA * 0.45f * p * p;
                s += JsThump(t, 0.025f) + JsThump(t, 0.095f);
                s *= gate;
            }
            else
            {
                float u = t - ti;

                float fb = 30f + 70f * MathF.Exp(-u * 9f);
                phBoom += 2f * MathF.PI * fb * dt;
                s += MathF.Sin(phBoom) * MathF.Exp(-u * 2.4f) * 1.05f;

                s += white * MathF.Exp(-u * 24f) * 0.55f + lpA * MathF.Exp(-u * 4.5f) * 0.9f;

                float rise = JsSm(u / 0.35f);
                float fall = JsSm((u - 0.9f) / 1.3f);
                float f0 = 780f + 560f * rise - 220f * fall + MathF.Sin(u * 2f * MathF.PI * 6.5f) * 26f;
                phA += 2f * MathF.PI * f0 * dt;
                phB += 2f * MathF.PI * f0 * 1.4142f * dt;
                phC += 2f * MathF.PI * f0 * 1.0595f * dt;
                float wave = MathF.Sin(phA + 2.2f * MathF.Sin(phA * 0.5f)) + 0.7f * MathF.Sin(phB)
                           + 0.8f * MathF.Sin(phC) + 0.25f * MathF.Sin(phA * 2f);
                float env = (1f - MathF.Exp(-u * 350f)) * (u < 1.1f ? 1f : MathF.Exp(-(u - 1.1f) * 3.0f));
                s += wave * 0.20f * env;
                s += lpB * (0.5f + 0.5f * MathF.Sin(phA)) * 0.22f * env;

                phG += 2f * MathF.PI * 62f * dt;
                float growlAm = 0.55f + 0.45f * MathF.Sin(u * 2f * MathF.PI * 24f);
                float saw = MathF.Sin(phG) + 0.5f * MathF.Sin(phG * 2f) + 0.33f * MathF.Sin(phG * 3f) + 0.25f * MathF.Sin(phG * 4f);
                s += saw * growlAm * 0.22f * (u < 1.6f ? 1f : MathF.Exp(-(u - 1.6f) * 3f)) * (1f - MathF.Exp(-u * 60f));

                if (u > 0.55f)
                {
                    float te = MathF.Exp(-(u - 0.55f) * 0.9f) * JsSm((u - 0.55f) / 0.05f);
                    s += MathF.Sin(2f * MathF.PI * 4200f * t) * 0.04f * te;
                    s += MathF.Sin(2f * MathF.PI * 5600f * t) * 0.02f * te;
                }
            }

            buf[i] = s;
        }

        short[] pcm = new short[n];
        int fadeN = (int)(sr * 0.15f);
        for (int i = 0; i < n; i++)
        {
            float v = MathF.Tanh(buf[i] * 1.4f) * 0.92f;
            if (i > n - fadeN) v *= (n - i) / (float)fadeN;
            pcm[i] = (short)(v * short.MaxValue);
        }

        return JsBuildSound(pcm, sr);
    }

    static Sound JsBuildSound(short[] samples, int sampleRate)
    {
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        int dataBytes = samples.Length * 2;
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataBytes);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataBytes);
        foreach (short s in samples) writer.Write(s);

        Wave wave = Raylib.LoadWaveFromMemory(".wav", ms.ToArray());
        Sound sound = Raylib.LoadSoundFromWave(wave);
        Raylib.UnloadWave(wave);
        return sound;
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
        else if (jumpscareProcLoaded)
        {
            if (Raylib.IsSoundPlaying(jumpscareProcSound))
                Raylib.StopSound(jumpscareProcSound);

            Raylib.SetSoundVolume(jumpscareProcSound, sfxVol);
            Raylib.SetSoundPitch(jumpscareProcSound, 1.0f);
            Raylib.PlaySound(jumpscareProcSound);
        }
        else
        {
            Raylib.SetSoundVolume(voiceStaticSound, sfxVol);
            Raylib.SetSoundPitch(voiceStaticSound, 2.35f);
            Raylib.PlaySound(voiceStaticSound);

            Raylib.SetSoundVolume(footstepSound, sfxVol);
            Raylib.SetSoundPitch(footstepSound, 0.42f);
            Raylib.PlaySound(footstepSound);
        }
    }

    // =====================================================================
    //  ĐIỀU KHIỂN
    // =====================================================================
    public static void TriggerJumpscare(JumpscarePreset preset, float duration = 2.0f)
    {
        EnsureJumpscareAudio();
        activeJumpscarePreset = preset;
        jumpscareDuration = duration;
        jumpscareTimer = 0f;
        jumpscareActive = true;
        jumpscareAudioTriggered = false;
    }

    public static void EnterJumpscareDevMode()
    {
        EnsureJumpscareAudio();
        EnsureJumpscareTarget();
        jumpscareDevSandbox = true;
        devSelectedPreset = 0;
        Raylib.EnableCursor();
        TriggerJumpscare((JumpscarePreset)devSelectedPreset);
    }

    // =====================================================================
    //  DÀN CẢNH (Face-Lock Framing: Lao nhanh áp sát ống kính, mặt luôn chính diện)
    // =====================================================================
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

        // Tự động tính góc để mặt ma luôn quay chính diện về phía camera ở tất cả các spot
        Vector3 toCam = camBase - ghostStart;
        ghostYaw = MathF.Atan2(toCam.X, toCam.Z) * (180f / MathF.PI);

        Vector3 dir = Vector3.Normalize(new Vector3(ghostStart.X - camBase.X, 0f, ghostStart.Z - camBase.Z));
        float yawRad = ghostYaw * MathF.PI / 180f;

        // Cho phép áp sát ống kính tới 0.42m và nhích thêm vào sát màn hình trong lúc hù
        float creep = Math.Clamp((u - 0.15f) / 1.2f, 0f, 1f);
        float stop = MathF.Max(0.32f, devStopDistance * (1f - 0.16f * JsSm(creep)));

        Vector3 faceFull = GhostModel.GetFaceLocal(1f, 1f);
        float fx = faceFull.X * MathF.Cos(yawRad) + faceFull.Z * MathF.Sin(yawRad);
        float fz = -faceFull.X * MathF.Sin(yawRad) + faceFull.Z * MathF.Cos(yawRad);
        Vector3 ghostEnd = new(camBase.X + dir.X * stop - fx, 0f, camBase.Z + dir.Z * stop - fz);

        // ---- Tiến trình lao tới cực nhanh ----
        float s;
        if (u < 0f)
        {
            float preT = Math.Clamp(t / MathF.Max(0.01f, ti), 0f, 1f);
            s = 0.48f * JsSm(preT);
        }
        else
        {
            // Phóng vọt từ giữa phòng vào sát màn hình chỉ trong 0.09 giây
            float r = Math.Clamp(u / 0.09f, 0f, 1f);
            float e = 1f - MathF.Pow(1f - r, 4.0f);
            s = 0.48f + (1f - 0.48f) * e;
            if (u > 0.09f) s += 0.04f * MathF.Exp(-(u - 0.09f) * 14f);
        }
        ghostPos = Vector3.Lerp(ghostStart, ghostEnd, s);
        ghostPos.Y = 0f;

        lunge = u < 0f ? 0f : JsSm(u / 0.12f);

        // ---- Ánh sáng & độ sáng của ma ----
        bool lit;
        float gb;
        if (devLightsOn)
        {
            lit = true;
            gb = 1f;
        }
        else if (u < 0f)
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
        jsAber = u >= 0f ? 2.2f + 12f * MathF.Exp(-u * 5.5f) : (lit && !devLightsOn ? 2.5f : 0.3f);
        jsZoom = u >= 0f ? 0.85f * MathF.Exp(-u * 9f) : 0f;
        jsScream = u >= 0f ? JsSm((u - 0.01f) / 0.08f) : 0f;
        jsTwitch = u >= 0f ? 0.30f + 0.65f * MathF.Exp(-u * 1.1f) : 0.25f;

        // ---- Camera: Khóa tâm nhìn vào khuôn mặt ma ----
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

    // =====================================================================
    //  HIỆU ỨNG MÀN HÌNH
    // =====================================================================
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

            if (devVhsGlitch && isActing)
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

    // =====================================================================
    //  DEV SANDBOX
    // =====================================================================
    static void UpdateAndDrawJumpscareSandbox(float dt)
    {
        float time = (float)Raylib.GetTime();
        EnsureJumpscareTarget();

        if (Raylib.IsKeyPressed(KeyboardKey.One)) { devSelectedPreset = 0; TriggerJumpscare((JumpscarePreset)0); }
        if (Raylib.IsKeyPressed(KeyboardKey.Two)) { devSelectedPreset = 1; TriggerJumpscare((JumpscarePreset)1); }
        if (Raylib.IsKeyPressed(KeyboardKey.Three)) { devSelectedPreset = 2; TriggerJumpscare((JumpscarePreset)2); }
        if (Raylib.IsKeyPressed(KeyboardKey.Four)) { devSelectedPreset = 3; TriggerJumpscare((JumpscarePreset)3); }

        if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.R))
            TriggerJumpscare((JumpscarePreset)devSelectedPreset);

        if (Raylib.IsKeyPressed(KeyboardKey.T))
            PlayJumpscareAudioBurst();

        if (Raylib.IsKeyPressed(KeyboardKey.L))
            devLightsOn = !devLightsOn;

        if (Raylib.IsKeyPressed(KeyboardKey.V))
            devVhsGlitch = !devVhsGlitch;

        if (Raylib.IsKeyPressed(KeyboardKey.X))
            jsSlowMo = !jsSlowMo;

        if (Raylib.IsKeyPressed(KeyboardKey.H))
            devShowHud = !devShowHud;

        if (Raylib.IsKeyPressed(KeyboardKey.Y))
            GhostModel.ExternalYawOffset = (GhostModel.ExternalYawOffset + 90f) % 360f;
        if (Raylib.IsKeyPressed(KeyboardKey.G))
            GhostModel.ExternalEyeGlow = !GhostModel.ExternalEyeGlow;
        if (Raylib.IsKeyDown(KeyboardKey.LeftBracket))
            GhostModel.ExternalFaceHeightRatio = Math.Clamp(GhostModel.ExternalFaceHeightRatio - dt * 0.15f, 0.50f, 0.98f);
        if (Raylib.IsKeyDown(KeyboardKey.RightBracket))
            GhostModel.ExternalFaceHeightRatio = Math.Clamp(GhostModel.ExternalFaceHeightRatio + dt * 0.15f, 0.50f, 0.98f);

        if (Raylib.IsKeyDown(KeyboardKey.Up))
            devStopDistance = Math.Clamp(devStopDistance - dt * 0.50f, 0.42f, 2.00f);
        if (Raylib.IsKeyDown(KeyboardKey.Down))
            devStopDistance = Math.Clamp(devStopDistance + dt * 0.50f, 0.42f, 2.00f);

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            if (jumpscareSoundLoaded && Raylib.IsSoundPlaying(jumpscareSound))
                Raylib.StopSound(jumpscareSound);
            if (jumpscareProcLoaded && Raylib.IsSoundPlaying(jumpscareProcSound))
                Raylib.StopSound(jumpscareProcSound);
            jumpscareDevSandbox = false;
            jumpscareActive = false;
            ReturnToHome();
            return;
        }

        if (jumpscareActive)
        {
            if (!jumpscareAudioTriggered)
            {
                PlayJumpscareAudioBurst();
                jumpscareAudioTriggered = true;
            }
            jumpscareTimer += jsSlowMo ? dt * 0.25f : dt;
            if (jumpscareTimer >= jumpscareDuration)
            {
                jumpscareTimer = jumpscareDuration;
                jumpscareActive = false;
            }
        }

        float dur = MathF.Max(0.5f, jumpscareDuration);
        float tNorm = Math.Clamp(jumpscareTimer / dur, 0f, 1f);
        float u = tNorm * dur - JsImpactTime(dur);

        ComputeJumpscarePose((JumpscarePreset)devSelectedPreset, tNorm, jumpscareActive,
            out Vector3 camPos, out Vector3 camTarget, out Vector3 camUp, out float fov,
            out Vector3 ghostPos, out float ghostYaw, out float lunge);

        camera.Position = camPos;
        camera.Target = camTarget;
        camera.Up = camUp;
        camera.FovY = fov;

        bool strobeLit = jsLit;

        // ---- Cảnh 3D vào render texture ----
        Raylib.BeginTextureMode(jsTarget);
        Raylib.ClearBackground(strobeLit ? JsColor(24, 27, 34) : JsColor(6, 7, 11));

        Raylib.BeginMode3D(camera);
        Props.DrawStore(lit: strobeLit, doorLocked: true, time, "03:17", phoneRinging: false, daylight: 0f);
        DrawStoreShelves(lit: strobeLit);
        Props.DrawWashroom(strobeLit, time, true, [true, false, false], -1, 0f, false, 0, 0f);
        Props.DrawStoreGlass(doorLocked: true, daylight: 0f);

        DrawJumpscareDebris((JumpscarePreset)devSelectedPreset, u);
        GhostModel.Draw(ghostPos, ghostYaw, time, lunge,
            twitchIntensity: jsTwitch, screamAmount: jsScream, brightness: jsGhostBright);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        // ---- Ghép + hiệu ứng ----
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        DrawJumpscareComposite(jsAber, jsZoom);
        DrawJumpscareScreenEffects(tNorm, time, jumpscareActive);

        // --- BẢNG HUD DEV (Bấm H để ẩn/hiện khi muốn ngắm toàn màn hình) ---
        if (devShowHud)
        {
            Raylib.DrawRectangle(16, 16, 560, 246, new Color((byte)8, (byte)10, (byte)16, (byte)215));
            Raylib.DrawRectangleLines(16, 16, 560, 246, new Color((byte)255, (byte)65, (byte)110, (byte)255));

            string[] presetNames =
            [
                "1: Quầy thu ngân (Counter Lunge)",
                "2: Nhà vệ sinh (Washroom Behind)",
                "3: Cửa kính chính (Front Door Rush)",
                "4: Hành lang giữa tiệm (Main Corridor)"
            ];

            string stateTag = jumpscareActive
                ? $"ĐANG HÙ ({jumpscareTimer:0.00}s / {jumpscareDuration:0.0}s)"
                : "ĐÃ NGẮT RUNG";
            Color stateCol = jumpscareActive
                ? new Color((byte)255, (byte)95, (byte)120, (byte)255)
                : new Color((byte)120, (byte)235, (byte)255, (byte)255);

            string phase = u < 0f ? "RÌNH RẬP (tối, chỉ thấy đôi mắt)" : (u < 0.5f ? "VA CHẠM" : "ÁM ẢNH");

            DrawViText($"JUMPSCARE DEV (F9) - {stateTag}", 28, 24, 19, stateCol);
            DrawViText($"Kịch bản: {presetNames[devSelectedPreset]}", 28, 52, 18, Color.White);
            DrawViText($"Giai đoạn: {phase}{(jsSlowMo ? "   [CHẬM 4x]" : "")}", 28, 78, 17, new Color((byte)255, (byte)210, (byte)120, (byte)255));
            DrawViText($"Mặt-ống kính (Up/Down): {devStopDistance:0.00}m | Đèn (L): {(devLightsOn ? "BẬT" : "TẮT")} | VHS (V): {(devVhsGlitch ? "BẬT" : "TẮT")}", 28, 104, 16, new Color((byte)190, (byte)220, (byte)255, (byte)255));
            DrawViText($"GLB Góc (Y): {GhostModel.ExternalYawOffset:0}° | Tầm mặt ([ / ]): {GhostModel.ExternalFaceHeightRatio:0.00} | Mắt đỏ (G): {(GhostModel.ExternalEyeGlow ? "BẬT" : "TẮT")}", 28, 130, 16, new Color((byte)255, (byte)170, (byte)190, (byte)255));

            DrawViText("[1..4] Đổi góc | [SPACE] Phát lại | [X] Chậm 4x | [H] Ẩn HUD", 28, 158, 17, new Color((byte)130, (byte)245, (byte)165, (byte)255));
            DrawViText("[T] Tiếng hù | [ESC] Quay về Home", 28, 184, 16, new Color((byte)200, (byte)205, (byte)215, (byte)255));

            float barW = 532f;
            Raylib.DrawRectangle(28, 224, (int)barW, 14, new Color((byte)30, (byte)34, (byte)44, (byte)255));
            Raylib.DrawRectangle(28, 224, (int)(barW * tNorm), 14,
                jumpscareActive ? new Color((byte)255, (byte)52, (byte)128, (byte)255) : new Color((byte)90, (byte)210, (byte)255, (byte)255));
        }

        Raylib.EndDrawing();
    }
}