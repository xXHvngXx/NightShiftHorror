using System.Numerics;
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

    static Sound jumpscareSound;
    static bool jumpscareSoundLoaded = false;
    static bool jumpscareSoundChecked = false;

    static bool jumpscareActive = false;
    static JumpscarePreset activeJumpscarePreset = JumpscarePreset.CounterLunge;
    static float jumpscareTimer = 0f;
    static float jumpscareDuration = 2.0f;
    static bool jumpscareAudioTriggered = false;

    // Trạng thái phòng Dev Sandbox
    static bool jumpscareDevSandbox = false;
    static int devSelectedPreset = 0;
    static float devStopDistance = 0.96f; // Khoảng cách vừa đủ để thấy rõ cả đầu, ngực và tay ma
    static bool devLightsOn = true;       // Mặc định bật ánh sáng mờ để dễ quan sát khi test
    static bool devVhsGlitch = true;

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
        jumpscareSoundChecked = false;
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

    public static void EnterJumpscareDevMode()
    {
        EnsureJumpscareAudio();
        jumpscareDevSandbox = true;
        devSelectedPreset = 0;
        Raylib.EnableCursor();
        TriggerJumpscare((JumpscarePreset)devSelectedPreset);
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
            Raylib.SetSoundVolume(voiceStaticSound, sfxVol);
            Raylib.SetSoundPitch(voiceStaticSound, 2.35f);
            Raylib.PlaySound(voiceStaticSound);

            Raylib.SetSoundVolume(footstepSound, sfxVol);
            Raylib.SetSoundPitch(footstepSound, 0.42f);
            Raylib.PlaySound(footstepSound);
        }
    }

    static void ComputeJumpscarePose(JumpscarePreset preset, float tNorm, bool isActing,
                                     out Vector3 camPos, out Vector3 camTarget, out Vector3 camUp, out float fov,
                                     out Vector3 ghostPos, out float ghostYaw, out float lunge)
    {
        // Lao vào trong ~0.65s để mắt kịp nhìn rõ dáng ma, sau đó giữ khung hình cho người chơi quan sát
        float moveT = Math.Clamp(tNorm / 0.35f, 0f, 1f);
        float rush = 1f - MathF.Pow(1f - moveT, 2.2f);
        lunge = Math.Clamp(tNorm / 0.45f, 0f, 1f);

        float time = (float)Raylib.GetTime();

        Vector3 shake = Vector3.Zero;
        float rollAngle = 0f;

        if (isActing)
        {
            // Rung nhẹ và có kiểm soát để không làm nhòe hình con ma
            float shakeEnvelope = MathF.Exp(-tNorm * 3.2f) * SmoothStep01((1f - tNorm) / 0.12f);
            float shakeAmp = 0.024f * shakeEnvelope;

            shake = new Vector3(
                MathF.Sin(time * 54f) * shakeAmp,
                MathF.Cos(time * 63f) * shakeAmp * 0.7f,
                0f
            );

            rollAngle = MathF.Sin(time * 24f) * 0.045f * shakeEnvelope;
        }

        camUp = Vector3.Normalize(new Vector3(MathF.Sin(rollAngle), MathF.Cos(rollAngle), 0f));

        switch (preset)
        {
            case JumpscarePreset.WashroomMirror:
                camPos = new Vector3(-13.15f, 1.68f, -5.65f) + shake;
                Vector3 ghostStartWc = new(-13.15f, 0f, -3.45f);
                Vector3 ghostEndWc = new(-13.15f, 0f, -5.65f + devStopDistance);
                ghostPos = Vector3.Lerp(ghostStartWc, ghostEndWc, rush);
                ghostYaw = 180f;
                camTarget = new Vector3(ghostPos.X, 1.48f, ghostPos.Z);
                fov = 60f - rush * 6f;
                break;

            case JumpscarePreset.FrontDoorRush:
                camPos = new Vector3(0.0f, 1.70f, 4.20f) + shake;
                Vector3 ghostStartDoor = new(0.0f, 0f, 8.20f);
                Vector3 ghostEndDoor = new(0.0f, 0f, 4.20f + devStopDistance);
                ghostPos = Vector3.Lerp(ghostStartDoor, ghostEndDoor, rush);
                ghostYaw = 180f;
                camTarget = new Vector3(ghostPos.X, 1.48f, ghostPos.Z);
                fov = 62f - rush * 7f;
                break;

            case JumpscarePreset.AisleAmbush:
                const float mainCorridorX = 2.05f;
                camPos = new Vector3(mainCorridorX, 1.68f, 4.30f) + shake;
                Vector3 ghostStartCorridor = new(mainCorridorX, 0f, -2.60f);
                Vector3 ghostEndCorridor = new(mainCorridorX, 0f, 4.30f - devStopDistance);
                ghostPos = Vector3.Lerp(ghostStartCorridor, ghostEndCorridor, rush);
                ghostYaw = 0f;
                camTarget = new Vector3(ghostPos.X, 1.48f, ghostPos.Z);
                fov = 60f - rush * 6f;
                break;

            case JumpscarePreset.CounterLunge:
            default:
                camPos = new Vector3(-2.60f, 1.70f, 1.20f) + shake;
                Vector3 ghostStartCounter = new(-2.60f, 0f, -1.85f);
                Vector3 ghostEndCounter = new(-2.60f, 0f, 1.20f - devStopDistance);
                ghostPos = Vector3.Lerp(ghostStartCounter, ghostEndCounter, rush);
                ghostYaw = 0f;
                camTarget = new Vector3(ghostPos.X, 1.48f, ghostPos.Z);
                fov = 60f - rush * 6f;
                break;
        }
    }

    static void DrawJumpscareScreenEffects(float tNorm, float time, bool isActing)
    {
        int W = ScreenWidth, H = ScreenHeight;

        // Chớp nhẹ thoáng qua ở đầu cú lao để không che mất con ma
        if (isActing && tNorm < 0.08f)
        {
            float p = 1f - (tNorm / 0.08f);
            byte flashA = (byte)(p * 85f);
            Raylib.DrawRectangle(0, 0, W, H, new Color((byte)210, (byte)20, (byte)35, flashA));
        }

        // Nhiễu VHS mảnh trong 2.0s biểu diễn, hết 2.0s thì dừng hẳn
        if (devVhsGlitch && isActing)
        {
            float tailFade = SmoothStep01((1f - tNorm) / 0.12f);
            int sliceCount = (int)MathF.Ceiling(4f * tailFade);
            int seed = (int)(time * 30f);

            for (int i = 0; i < sliceCount; i++)
            {
                int sy = HomeHash(seed, i * 3 + 1) % Math.Max(1, H - 16);
                int sh = 2 + (HomeHash(seed, i * 3 + 2) % 8);
                byte a = (byte)Math.Clamp((int)(28f * tailFade), 6, 45);
                Color barCol = (i % 2 == 0)
                    ? new Color((byte)255, (byte)45, (byte)85, a)
                    : new Color((byte)60, (byte)210, (byte)255, a);
                Raylib.DrawRectangle(0, sy, W, sh, barCol);
            }
        }

        DrawVignette(115);
    }

    static void UpdateAndDrawJumpscareSandbox(float dt)
    {
        float time = (float)Raylib.GetTime();

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

        if (Raylib.IsKeyDown(KeyboardKey.Up))
            devStopDistance = Math.Clamp(devStopDistance - dt * 0.50f, 0.55f, 1.80f);
        if (Raylib.IsKeyDown(KeyboardKey.Down))
            devStopDistance = Math.Clamp(devStopDistance + dt * 0.50f, 0.55f, 1.80f);

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            if (jumpscareSoundLoaded && Raylib.IsSoundPlaying(jumpscareSound))
                Raylib.StopSound(jumpscareSound);
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
            jumpscareTimer += dt;
            if (jumpscareTimer >= jumpscareDuration)
            {
                jumpscareTimer = jumpscareDuration;
                jumpscareActive = false; // Hết 2.0s: ngừng rung lắc
            }
        }

        float tNorm = Math.Clamp(jumpscareTimer / Math.Max(0.01f, jumpscareDuration), 0f, 1f);
        ComputeJumpscarePose((JumpscarePreset)devSelectedPreset, tNorm, jumpscareActive,
            out Vector3 camPos, out Vector3 camTarget, out Vector3 camUp, out float fov,
            out Vector3 ghostPos, out float ghostYaw, out float lunge);

        camera.Position = camPos;
        camera.Target = camTarget;
        camera.Up = camUp;
        camera.FovY = fov;

        // Nháy nhẹ lúc đầu nhưng vẫn đủ sáng để nhận diện hình dáng con ma
        bool strobeLit = devLightsOn || (jumpscareActive && tNorm < 0.35f && ((int)(time * 24f) % 2 == 0));

        Raylib.BeginDrawing();
        Raylib.ClearBackground(strobeLit ? new Color(24, 27, 34, 255) : new Color(6, 7, 11, 255));

        Raylib.BeginMode3D(camera);
        Props.DrawStore(lit: strobeLit, doorLocked: true, time, "03:17", phoneRinging: false, daylight: 0f);
        DrawStoreShelves(lit: strobeLit);
        Props.DrawWashroom(strobeLit, time, true, [true, false, false], -1, 0f, false, 0, 0f);
        Props.DrawStoreGlass(doorLocked: true, daylight: 0f);

        GhostModel.Draw(ghostPos, ghostYaw, time, lunge, twitchIntensity: jumpscareActive ? 0.55f : 0.10f);
        Raylib.EndMode3D();

        DrawJumpscareScreenEffects(tNorm, time, jumpscareActive);

        // --- BẢNG HUD DEV CĂN CHỈNH ---
        Raylib.DrawRectangle(16, 16, 525, 210, new Color(8, 10, 16, 215));
        Raylib.DrawRectangleLines(16, 16, 525, 210, new Color(255, 65, 110, 255));

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
            ? new Color(255, 95, 120, 255)
            : new Color(120, 235, 255, 255);

        DrawViText($"JUMPSCARE DEV (F9) - {stateTag}", 28, 24, 19, stateCol);
        DrawViText($"Kịch bản: {presetNames[devSelectedPreset]}", 28, 52, 18, Color.White);
        DrawViText($"Khoảng cách dừng (Up/Down): {devStopDistance:0.00}m | Đèn (L): {(devLightsOn ? "BẬT" : "TẮT")} | VHS (V): {(devVhsGlitch ? "BẬT" : "TẮT")}", 28, 78, 16, new Color(190, 220, 255, 255));
        DrawViText("[1..4] Đổi góc  |  [SPACE] Phát lại 2.0s  |  [T] Test tiếng hù", 28, 108, 17, new Color(130, 245, 165, 255));
        DrawViText("[ESC] Quay về Home", 28, 134, 16, new Color(200, 205, 215, 255));

        float barW = 497f;
        Raylib.DrawRectangle(28, 186, (int)barW, 14, new Color(30, 34, 44, 255));
        Raylib.DrawRectangle(28, 186, (int)(barW * tNorm), 14, jumpscareActive ? new Color(255, 52, 128, 255) : new Color(90, 210, 255, 255));

        Raylib.EndDrawing();
    }
}