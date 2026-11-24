using System.Numerics;
using Raylib_cs;

partial class Program
{
    const float BikeRoadEndZ = 1550.0f;
    const float BikeMaxSpeed = 69.3f;

    // ===== Kích thước cabin xe (toạ độ cục bộ: +X = bên TRÁI người lái, +Z = phía trước) =====
    const float BikeEyeHeight = 1.36f;     // độ cao mắt người lái
    const float BikeBarZ = 0.62f;          // trục cổ xe cách mắt bao xa về phía trước
    const float BikeBarY = 1.075f;         // độ cao ghi đông
    const float BikeMaxSteerRad = 0.42f;   // góc bẻ lái tối đa (rad)

    static Vector3 bikePos = new(2.2f, 0f, 8f);
    static float bikeYaw = 0f;
    static float bikeSpeed = 0f;
    static float bikeLean = 0f;
    static float bikeSteerAngle = 0f;
    static float bikeThrottleAnim = 0f;   // 0..1: tay ga xoay khi giữ ga
    static float bikeBrakeAnim = 0f;      // 0..1: tay phanh bóp khi phanh
    static RenderTexture2D leftMirrorTarget;
    static bool bikeHeadlightOn = true;
    static bool bikeIntroTipShown = false;
    static bool bikeFadingToNext = false;

    static RenderTexture2D rightMirrorTarget;
    static bool mirrorsInitialized = false;

    // Playlist nhạc khi chạy xe
    static List<string> bikePlaylist = [];
    static int bikeTrackIndex = 0;
    static Music bikeCurrentMusic;
    static bool bikeMusicLoaded = false;
    static bool bikeMusicEnabled = true;
    static float bikeDiscAngle = 0f;

    // Dữ liệu bầu trời (sinh một lần)
    static bool skyInitialized = false;
    static Vector3[] skyStarDirs = [];
    static float[] skyStarSize = [];
    static byte[] skyStarBase = [];
    static float[] skyStarPhase = [];
    static Color[] skyStarTint = [];

    static readonly (string Label, Color SignBg, Color TextCol)[] vnShopSigns =
    [
        ("TẠP HÓA CÔ BA", new Color(185, 35, 35, 255), Color.Yellow),
        ("CƠM TẤM ĐÊM", new Color(25, 95, 165, 255), Color.White),
        ("TIỆM THUỐC TÂY", new Color(22, 130, 68, 255), Color.White),
        ("PHỞ BÒ GIA TRUYỀN", new Color(165, 95, 20, 255), Color.Yellow),
        ("SỬA XE - VÁ VỎ", new Color(55, 65, 80, 255), Color.Orange),
        ("TRÀ CHANH - ĂN VẶT", new Color(120, 45, 145, 255), Color.RayWhite)
    ];

    static readonly Color[] parkedBikeColors =
    [
        new Color(170, 35, 40, 255),
        new Color(35, 60, 120, 255),
        new Color(210, 210, 215, 255),
        new Color(30, 30, 34, 255),
        new Color(190, 150, 40, 255)
    ];

    static void EnsureMirrorTextures()
    {
        if (mirrorsInitialized) return;
        leftMirrorTarget = Raylib.LoadRenderTexture(220, 128);
        rightMirrorTarget = Raylib.LoadRenderTexture(220, 128);
        Raylib.SetTextureFilter(leftMirrorTarget.Texture, TextureFilter.Bilinear);
        Raylib.SetTextureFilter(rightMirrorTarget.Texture, TextureFilter.Bilinear);
        mirrorsInitialized = true;
    }

    static void InitBikePlaylist()
    {
        StopAndUnloadBikeMusic();
        string musicDir = Path.Combine("Assets", "Playlist");
        Directory.CreateDirectory(musicDir);

        bikePlaylist = Directory.GetFiles(musicDir, "*.mp3")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        bikeTrackIndex = 0;
        bikeMusicEnabled = true;
        bikeDiscAngle = 0f;

        if (bikePlaylist.Count > 0)
        {
            LoadAndPlayBikeTrack(0);
        }
    }

    static void LoadAndPlayBikeTrack(int index)
    {
        if (bikePlaylist.Count == 0) return;

        if (bikeMusicLoaded)
        {
            Raylib.StopMusicStream(bikeCurrentMusic);
            Raylib.UnloadMusicStream(bikeCurrentMusic);
            bikeMusicLoaded = false;
        }

        bikeTrackIndex = ((index % bikePlaylist.Count) + bikePlaylist.Count) % bikePlaylist.Count;
        bikeCurrentMusic = Raylib.LoadMusicStream(bikePlaylist[bikeTrackIndex]);
        bikeCurrentMusic.Looping = false;
        Raylib.SetMusicVolume(bikeCurrentMusic, 0.48f);
        bikeMusicLoaded = true;

        if (bikeMusicEnabled)
        {
            Raylib.PlayMusicStream(bikeCurrentMusic);
        }
    }

    static void UpdateBikeMusic(float dt)
    {
        if (bikePlaylist.Count == 0 || !bikeMusicLoaded) return;

        if (bikeMusicEnabled)
        {
            Raylib.UpdateMusicStream(bikeCurrentMusic);
            bikeDiscAngle = (bikeDiscAngle + 95f * dt) % 360f;

            float totalLen = Raylib.GetMusicTimeLength(bikeCurrentMusic);
            float played = Raylib.GetMusicTimePlayed(bikeCurrentMusic);
            if (totalLen > 0.5f && played >= totalLen - 0.15f)
            {
                LoadAndPlayBikeTrack(bikeTrackIndex + 1);
            }
        }
    }

    static void StopAndUnloadBikeMusic()
    {
        if (bikeMusicLoaded)
        {
            Raylib.StopMusicStream(bikeCurrentMusic);
            Raylib.UnloadMusicStream(bikeCurrentMusic);
            bikeMusicLoaded = false;
        }
    }

    static void StartMotorbikeScene()
    {
        EnsureMirrorTextures();
        InitBikePlaylist();

        bikePos = new Vector3(2.2f, 0f, 8f);
        bikeYaw = 0f;
        bikeSpeed = 0f;
        bikeLean = 0f;
        bikeSteerAngle = 0f;
        bikeThrottleAnim = 0f;
        bikeBrakeAnim = 0f;
        bikeHeadlightOn = true;
        bikeIntroTipShown = false;
        bikeFadingToNext = false;
        camYaw = 0f;
        camPitch = -0.04f;
        fadeAlpha = 1.0f;
        gameScene = 3;

        StartDialogue(
            "Tú",
            "Trời tối sương bắt đầu xuống rồi... Chạy xe về phòng trọ nghỉ một lát rồi 11 rưỡi qua đường Số 9 nhận ca. (Bấm [W/S] Ga/Phanh, [A/D] Lái, [Shift] Nẹt ga/Thu FOV, [L] Đèn pha, [Space] Còi)",
            2
        );
    }

    static void UpdateAndDrawMotorbikeScene(float dt)
    {
        EnsureMirrorTextures();
        UpdateBikeMusic(dt);

        if (!Raylib.IsSoundPlaying(motorbikeEngineSound))
        {
            Raylib.PlaySound(motorbikeEngineSound);
        }

        float speedRatio = Math.Clamp(bikeSpeed / BikeMaxSpeed, 0f, 1f);
        Raylib.SetSoundPitch(motorbikeEngineSound, 0.72f + speedRatio * 1.45f);
        Raylib.SetSoundVolume(motorbikeEngineSound, 0.28f + speedRatio * 0.25f);

        // Điều khiển playlist nhạc bất cứ lúc nào trong cảnh chạy xe
        if (bikePlaylist.Count > 0)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.One))
            {
                bikeMusicEnabled = true;
                LoadAndPlayBikeTrack(bikeTrackIndex - 1);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Two))
            {
                bikeMusicEnabled = !bikeMusicEnabled;
                if (bikeMusicLoaded)
                {
                    if (bikeMusicEnabled) Raylib.ResumeMusicStream(bikeCurrentMusic);
                    else Raylib.PauseMusicStream(bikeCurrentMusic);
                }
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Three))
            {
                bikeMusicEnabled = true;
                LoadAndPlayBikeTrack(bikeTrackIndex + 1);
            }
        }

        if (fadeAlpha > 0f && !bikeFadingToNext)
        {
            fadeAlpha = Math.Max(0f, fadeAlpha - dt * 0.8f);
        }

        float throttleTarget = 0f;
        float brakeTarget = 0f;
        bool shiftHeld = false;

        if (bikeFadingToNext)
        {
            fadeAlpha = Math.Min(1.0f, fadeAlpha + dt * 0.9f);
            if (fadeAlpha >= 1.0f)
            {
                Raylib.StopSound(motorbikeEngineSound);
                StopAndUnloadBikeMusic();
                gameScene = 1;
                bikeFadingToNext = false;
                fadeAlpha = 0f;
                introIndex = 0;
                camera.FovY = NormalFov;
                camera.Up = new Vector3(0f, 1f, 0f);
                StartDialogue("Tú", introMonologue[0], 2);
                return;
            }
        }
        else if (isTalking)
        {
            Raylib.GetMouseDelta();
            UpdateTypewriter(dt, GetSpeakerPitch(currentSpeaker));
            bool textFinished = (int)visibleChars >= fullText.Length;

            if (!textFinished && (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.F)))
            {
                visibleChars = fullText.Length;
            }
            else if (textFinished && Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                isTalking = false;
                StopDialogueVoice();
                dialogueStage = 0;
                Raylib.GetMouseDelta();
            }
        }
        else
        {
            bool throttle = Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up);
            bool brake = Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down);
            shiftHeld = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
            bool boost = throttle && shiftHeld;

            throttleTarget = throttle ? (boost ? 1.0f : 0.7f) : 0f;
            brakeTarget = brake ? 1f : 0f;

            if (throttle)
            {
                float accel = boost ? 22.0f : 16.0f;
                bikeSpeed = Math.Min(BikeMaxSpeed, bikeSpeed + accel * dt);
            }
            else if (brake)
                bikeSpeed = Math.Max(0f, bikeSpeed - 22.0f * dt);
            else
                bikeSpeed = Math.Max(0f, bikeSpeed - 4.5f * dt);

            float steerInput = 0f;
            if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) steerInput += 1f;
            if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) steerInput -= 1f;

            bikeSteerAngle += (steerInput - bikeSteerAngle) * (1f - MathF.Exp(-9f * dt));

            if (bikeSpeed > 0.25f)
            {
                float turnRate = 1.35f * Math.Clamp(bikeSpeed / 8.0f, 0.35f, 1.0f);
                bikeYaw += bikeSteerAngle * turnRate * dt;
                bikeYaw = Math.Clamp(bikeYaw, -0.68f, 0.68f);
            }

            float targetLean = -bikeSteerAngle * Math.Clamp(bikeSpeed / 12.0f, 0f, 1f) * 0.24f;
            bikeLean += (targetLean - bikeLean) * (1f - MathF.Exp(-8f * dt));

            if (Raylib.IsKeyPressed(KeyboardKey.L))
            {
                bikeHeadlightOn = !bikeHeadlightOn;
                Raylib.SetSoundPitch(voiceStaticSound, 1.4f);
                Raylib.PlaySound(voiceStaticSound);
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                Raylib.PlaySound(motorbikeHornSound);
            }

            Vector3 forward = new(MathF.Sin(bikeYaw), 0f, MathF.Cos(bikeYaw));
            bikePos += forward * bikeSpeed * dt;
            bikePos.X = Math.Clamp(bikePos.X, -4.6f, 4.6f);
            bikePos.Z = Math.Max(4.0f, bikePos.Z);

            float signZ = BikeRoadEndZ * 0.5f;
            if (!bikeIntroTipShown && bikePos.Z >= signZ - 4f)
            {
                bikeIntroTipShown = true;
                StartDialogue(
                    "Tú",
                    "(Biển chỉ dẫn bên phải ghi 'Đường Số 9 -> 1.2 km'. Sương ngoài đó dày đặc hơn hẳn khu này...)",
                    2
                );
            }

            if (bikePos.Z >= BikeRoadEndZ)
            {
                bikeFadingToNext = true;
            }
        }

        // Tay ga xoay / tay phanh bóp mượt theo phím
        bikeThrottleAnim += (throttleTarget - bikeThrottleAnim) * (1f - MathF.Exp(-10f * dt));
        bikeBrakeAnim += (brakeTarget - bikeBrakeAnim) * (1f - MathF.Exp(-14f * dt));

        GetMirrorWorldPositions(bikePos, bikeYaw, bikeSteerAngle, out Vector3 leftMirrorPos, out Vector3 rightMirrorPos);

        Color nightStreetFog = new(12, 15, 24, 255);
        // Gương trái ở bên +X (trái người lái) nghiêng ra ngoài về bên trái, gương phải ngược lại
        RenderRearviewMirrorTexture(leftMirrorTarget, leftMirrorPos, bikeYaw + MathF.PI - 0.22f, bikeLean, nightStreetFog);
        RenderRearviewMirrorTexture(rightMirrorTarget, rightMirrorPos, bikeYaw + MathF.PI + 0.22f, bikeLean, nightStreetFog);

        Vector2 mouseDelta = Raylib.GetMouseDelta();
        camYaw = Math.Clamp(camYaw - mouseDelta.X * 0.0018f, -0.85f, 0.85f);
        camPitch = Math.Clamp(camPitch - mouseDelta.Y * 0.0018f, -0.45f, 0.45f);

        float engineVib = MathF.Sin((float)Raylib.GetTime() * (28f + bikeSpeed * 2.2f)) * (0.003f + speedRatio * 0.006f);
        Vector3 eyePos = new(bikePos.X, BikeEyeHeight + engineVib, bikePos.Z);
        float totalYaw = bikeYaw + camYaw;

        camera.Position = eyePos;
        camera.Target = eyePos + new Vector3(
            MathF.Sin(totalYaw) * MathF.Cos(camPitch),
            MathF.Sin(camPitch),
            MathF.Cos(totalYaw) * MathF.Cos(camPitch)
        );
        camera.Up = new Vector3(MathF.Sin(bikeLean), MathF.Cos(bikeLean), 0f);
        float shiftFovAdjust = shiftHeld ? 15.0f : 0f; // dương = mở rộng FOV ra xa khi nẹt ga/boost
        float targetFov = Math.Clamp(NormalFov + speedRatio * 10.0f + shiftFovAdjust, 45.0f, 105.0f);
        camera.FovY += (targetFov - camera.FovY) * (1f - MathF.Exp(-8.5f * dt));

        Raylib.BeginDrawing();

        RenderFoggyScene(() =>
        {
            DrawVietnameseStreet(bikePos, bikeYaw, bikeSteerAngle, bikeHeadlightOn, drawBikeHandlebar: true, eyePos: camera.Position);
        }, nightStreetFog, 2.1f);

        DrawMirrorOverlay(leftMirrorTarget, leftMirrorPos, isLeft: true);
        DrawMirrorOverlay(rightMirrorTarget, rightMirrorPos, isLeft: false);

        DrawVignette(bikeHeadlightOn ? 15 : 65);

        // HUD góc trái: Tốc độ & khoảng cách
        int kmh = (int)(bikeSpeed * 3.6f);
        float distLeft = Math.Max(0f, BikeRoadEndZ - bikePos.Z);
        Raylib.DrawRectangle(18, 18, 365, 68, new Color(8, 10, 15, 190));
        Raylib.DrawRectangleLines(18, 18, 365, 68, new Color(75, 85, 105, 255));
        DrawViText($"Tốc độ: {kmh} km/h   |   Đèn pha: {(bikeHeadlightOn ? "BẬT" : "TẮT")}", 32, 26, 19, bikeHeadlightOn ? Color.Yellow : Color.Gray);
        DrawViText($"Cách hẻm trọ: {distLeft:F0} m   ([L] Đèn | [Space] Còi)", 32, 52, 18, Color.RayWhite);

        // Thanh phát nhạc trên giữa màn hình
        DrawTopCenterMusicBar();

        if (isTalking)
        {
            DrawDialogueBox();
        }

        if (fadeAlpha > 0f)
        {
            Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(0, 0, 0, (int)(fadeAlpha * 255)));
        }

        Raylib.EndDrawing();
    }

    static void DrawTopCenterMusicBar()
    {
        const int barW = 460;
        const int barH = 68;
        int barX = (ScreenWidth - barW) / 2;
        const int barY = 18;

        Raylib.DrawRectangleRounded(new Rectangle(barX, barY, barW, barH), 0.25f, 10, new Color(10, 13, 20, 205));
        Raylib.DrawRectangleRoundedLines(new Rectangle(barX, barY, barW, barH), 0.25f, 10, new Color(85, 100, 125, 255));

        // Vẽ đĩa nhạc than xoay tròn bên trái thanh
        Vector2 discCenter = new(barX + 36, barY + barH * 0.5f);
        const float outerR = 23f;
        Raylib.DrawCircleV(discCenter, outerR, new Color(22, 24, 28, 255));
        Raylib.DrawCircleLines((int)discCenter.X, (int)discCenter.Y, outerR, new Color(110, 120, 135, 255));
        Raylib.DrawCircleLines((int)discCenter.X, (int)discCenter.Y, 16f, new Color(55, 60, 70, 200));

        // Vạch phản quang xoay theo bikeDiscAngle để thấy rõ chuyển động quay
        float rad = bikeDiscAngle * (MathF.PI / 180f);
        Vector2 grooveDir = new(MathF.Cos(rad), MathF.Sin(rad));
        Raylib.DrawLineEx(discCenter - grooveDir * 19f, discCenter - grooveDir * 9f, 2.2f, new Color(190, 210, 235, 190));
        Raylib.DrawLineEx(discCenter + grooveDir * 9f, discCenter + grooveDir * 19f, 2.2f, new Color(190, 210, 235, 190));

        // Nhãn tâm đĩa & lỗ trục
        Color labelCol = (bikePlaylist.Count > 0 && bikeMusicEnabled)
            ? new Color(215, 75, 55, 255)
            : new Color(85, 90, 100, 255);
        Raylib.DrawCircleV(discCenter, 8f, labelCol);
        Raylib.DrawCircleV(discCenter, 2.5f, new Color(12, 14, 18, 255));

        int textX = barX + 72;
        if (bikePlaylist.Count == 0)
        {
            DrawViText("Chưa có nhạc (.mp3)", textX, barY + 11, 18, Color.LightGray);
            DrawViText("Bỏ file .mp3 vào thư mục Assets/Playlist", textX, barY + 37, 16, Color.Gray);
            return;
        }

        string rawName = Path.GetFileNameWithoutExtension(bikePlaylist[bikeTrackIndex]);
        if (rawName.Length > 30) rawName = rawName[..27] + "...";

        string statusIcon = bikeMusicEnabled ? "▶" : "❚❚";
        string trackHeader = $"{statusIcon} [{bikeTrackIndex + 1}/{bikePlaylist.Count}] {rawName}";
        DrawViText(trackHeader, textX, barY + 9, 18, bikeMusicEnabled ? Color.SkyBlue : Color.Gray);

        // Thanh tiến trình nhỏ của bài hát
        if (bikeMusicLoaded)
        {
            float totalLen = Raylib.GetMusicTimeLength(bikeCurrentMusic);
            float played = Raylib.GetMusicTimePlayed(bikeCurrentMusic);
            float pct = totalLen > 0.01f ? Math.Clamp(played / totalLen, 0f, 1f) : 0f;
            int progW = barW - 88;
            Raylib.DrawRectangle(textX, barY + 33, progW, 4, new Color(40, 48, 62, 255));
            Raylib.DrawRectangle(textX, barY + 33, (int)(progW * pct), 4, bikeMusicEnabled ? Color.SkyBlue : Color.Gray);
        }

        // Hướng dẫn phím gọn: số phím + chức năng
        string toggleLabel = bikeMusicEnabled ? "Tắt" : "Bật";
        DrawViText($"[1] Trước    [2] {toggleLabel}    [3] Kế", textX, barY + 42, 16, Color.RayWhite);
    }

    // =====================================================================
    //  HÌNH HỌC CABIN XE
    //  Hệ toạ độ xe: +X = bên TRÁI người lái, +Y = lên, +Z = phía trước.
    //  "Khung tay lái" (steered) quay quanh trục cổ xe đặt tại Z = BikeBarZ.
    // =====================================================================

    // Đổi 1 điểm trong khung tay lái -> toạ độ thế giới (dùng cho vị trí gương chiếu hậu)
    static Vector3 BikeSteeredToWorld(Vector3 riderPos, float yaw, float steer, Vector3 s)
    {
        float a = steer * BikeMaxSteerRad;
        float ca = MathF.Cos(a), sa = MathF.Sin(a);
        float lx = s.X * ca + s.Z * sa;
        float lz = BikeBarZ + (-s.X * sa + s.Z * ca);

        Vector3 fwd = new(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        Vector3 left = new(MathF.Cos(yaw), 0f, -MathF.Sin(yaw));
        return new Vector3(riderPos.X, 0f, riderPos.Z) + left * lx + fwd * lz + new Vector3(0f, s.Y, 0f);
    }

    static void GetMirrorWorldPositions(Vector3 riderPos, float yaw, float steer, out Vector3 leftMirror, out Vector3 rightMirror)
    {
        leftMirror = BikeSteeredToWorld(riderPos, yaw, steer, new Vector3(0.52f, 1.29f, -0.03f));
        rightMirror = BikeSteeredToWorld(riderPos, yaw, steer, new Vector3(-0.52f, 1.29f, -0.03f));
    }

    static void DrawBikeCockpit(Vector3 riderPos, float yaw, float steer, bool headlightOn)
    {
        float steerDeg = steer * BikeMaxSteerRad * (180f / MathF.PI);

        Color body = new(168, 34, 40, 255);
        Color bodyDark = new(118, 22, 28, 255);
        Color plastic = new(24, 26, 30, 255);
        Color chrome = new(176, 181, 190, 255);
        Color rubber = new(14, 14, 16, 255);
        Color glove = new(34, 36, 42, 255);
        Color sleeve = new(48, 58, 80, 255);
        Color jeans = new(46, 64, 100, 255);
        Color shoe = new(20, 20, 24, 255);
        Color tyre = new(18, 18, 20, 255);

        float throttleTwist = bikeThrottleAnim * 0.85f;
        float brakePull = bikeBrakeAnim;

        Rlgl.PushMatrix();
        Rlgl.Translatef(riderPos.X, 0f, riderPos.Z);
        Rlgl.Rotatef(yaw * (180f / MathF.PI), 0f, 1f, 0f);

        // ================= PHẦN CỐ ĐỊNH (không quay theo tay lái) =================
        // Yên xe
        Raylib.DrawCube(new Vector3(0f, 0.74f, -0.30f), 0.34f, 0.10f, 0.75f, plastic);
        // Sàn để chân
        Raylib.DrawCube(new Vector3(0f, 0.30f, 0.38f), 0.56f, 0.05f, 0.74f, plastic);
        // Yếm (tấm che chân phía trước) + ốp hai bên
        Raylib.DrawCube(new Vector3(0f, 0.64f, 0.70f), 0.52f, 0.68f, 0.06f, body);
        Raylib.DrawCube(new Vector3(0.28f, 0.52f, 0.52f), 0.04f, 0.46f, 0.42f, bodyDark);
        Raylib.DrawCube(new Vector3(-0.28f, 0.52f, 0.52f), 0.04f, 0.46f, 0.42f, bodyDark);

        // Chân người lái (thấy khi cúi nhìn xuống)
        foreach (int s in new[] { 1, -1 })
        {
            Raylib.DrawCylinderEx(new Vector3(0.17f * s, 0.84f, -0.28f), new Vector3(0.20f * s, 0.82f, 0.30f), 0.095f, 0.075f, 10, jeans);
            Raylib.DrawCylinderEx(new Vector3(0.20f * s, 0.82f, 0.30f), new Vector3(0.22f * s, 0.40f, 0.42f), 0.075f, 0.055f, 10, jeans);
            Raylib.DrawCube(new Vector3(0.22f * s, 0.33f, 0.50f), 0.11f, 0.09f, 0.30f, shoe);
        }

        // ================= KHUNG TAY LÁI (quay quanh cổ xe) =================
        Rlgl.PushMatrix();
        Rlgl.Translatef(0f, 0f, BikeBarZ);
        Rlgl.Rotatef(steerDeg, 0f, 1f, 0f);

        // --- Cổ xe: ống cổ + ốp cổ phần dưới ---
        Raylib.DrawCylinderEx(new Vector3(0f, 1.00f, 0.06f), new Vector3(0f, 0.50f, 0.20f), 0.085f, 0.070f, 12, body);
        Raylib.DrawCylinderEx(new Vector3(0f, 1.02f, 0.22f), new Vector3(0f, 0.62f, 0.30f), 0.130f, 0.090f, 12, bodyDark);

        // --- Phuộc trước + bánh trước ---
        Vector3 axle = new(0f, 0.205f, 0.42f);
        foreach (int s in new[] { 1, -1 })
        {
            Raylib.DrawCylinderEx(new Vector3(0.07f * s, 0.96f, 0.06f), new Vector3(0.07f * s, 0.205f, 0.42f), 0.017f, 0.015f, 8, chrome);
        }
        Raylib.DrawCylinderEx(axle + new Vector3(-0.05f, 0f, 0f), axle + new Vector3(0.05f, 0f, 0f), 0.205f, 0.205f, 18, tyre);
        Raylib.DrawCylinderEx(axle + new Vector3(-0.054f, 0f, 0f), axle + new Vector3(0.054f, 0f, 0f), 0.135f, 0.135f, 16, new Color(150, 155, 166, 255));
        Raylib.DrawCylinderEx(axle + new Vector3(-0.058f, 0f, 0f), axle + new Vector3(0.058f, 0f, 0f), 0.040f, 0.040f, 10, new Color(96, 100, 110, 255));
        float wheelAng = riderPos.Z / 0.205f;
        for (int k = 0; k < 5; k++)
        {
            float ang = wheelAng + k * (MathF.PI * 2f / 5f);
            Vector3 rimPt = axle + new Vector3(0f, MathF.Cos(ang) * 0.135f, MathF.Sin(ang) * 0.135f);
            Raylib.DrawCylinderEx(axle + new Vector3(0.056f, 0f, 0f), rimPt + new Vector3(0.056f, 0f, 0f), 0.006f, 0.006f, 4, new Color(205, 208, 215, 255));
        }
        // Chắn bùn trước
        Raylib.DrawCube(new Vector3(0f, 0.455f, 0.42f), 0.13f, 0.02f, 0.34f, body);
        Raylib.DrawCube(new Vector3(0f, 0.405f, 0.62f), 0.13f, 0.02f, 0.12f, body);
        Raylib.DrawCube(new Vector3(0f, 0.405f, 0.22f), 0.13f, 0.02f, 0.12f, body);

        // --- Ốp đầu + đèn pha ---
        Raylib.DrawCube(new Vector3(0f, 1.03f, 0.04f), 0.34f, 0.08f, 0.26f, plastic);
        Raylib.DrawCube(new Vector3(0f, 1.04f, 0.28f), 0.40f, 0.14f, 0.28f, body);
        Color lens = headlightOn ? new Color(255, 248, 215, 255) : new Color(70, 76, 84, 255);
        Raylib.DrawCube(new Vector3(0f, 1.04f, 0.43f), 0.26f, 0.10f, 0.03f, lens);

        // --- Cụm đồng hồ ---
        Raylib.DrawCube(new Vector3(0f, 1.095f, 0.05f), 0.30f, 0.05f, 0.18f, new Color(16, 18, 22, 255));
        Color faceCol = headlightOn ? new Color(18, 46, 56, 255) : new Color(12, 26, 30, 255);
        Raylib.DrawCylinder(new Vector3(0f, 1.12f, 0.05f), 0.072f, 0.072f, 0.006f, 20, faceCol);
        Color tickCol = headlightOn ? new Color(80, 205, 235, 255) : new Color(40, 100, 115, 255);
        for (int t = 0; t <= 9; t++)
        {
            float tp = (45f + 270f * (t / 9f)) * (MathF.PI / 180f);
            Raylib.DrawCube(new Vector3(MathF.Sin(tp) * 0.060f, 1.1275f, 0.05f - MathF.Cos(tp) * 0.060f), 0.006f, 0.004f, 0.006f, tickCol);
        }
        float needleAng = (45f + 270f * Math.Clamp(bikeSpeed / BikeMaxSpeed, 0f, 1f)) * (MathF.PI / 180f);
        Vector3 needleStart = new(0f, 1.131f, 0.05f);
        Vector3 needleEnd = needleStart + new Vector3(MathF.Sin(needleAng) * 0.062f, 0f, -MathF.Cos(needleAng) * 0.062f);
        Raylib.DrawCylinderEx(needleStart, needleEnd, 0.004f, 0.002f, 4, new Color(255, 70, 50, 255));
        Raylib.DrawCube(new Vector3(0f, 1.130f, 0.05f), 0.014f, 0.006f, 0.014f, new Color(30, 30, 34, 255));
        // Đèn báo
        Raylib.DrawCube(new Vector3(0.115f, 1.123f, 0.10f), 0.020f, 0.006f, 0.020f, headlightOn ? new Color(70, 130, 255, 255) : new Color(25, 35, 60, 255));
        Raylib.DrawCube(new Vector3(-0.115f, 1.123f, 0.10f), 0.020f, 0.006f, 0.020f, new Color(255, 170, 40, 255));
        Raylib.DrawCube(new Vector3(0.115f, 1.123f, 0.00f), 0.020f, 0.006f, 0.020f, new Color(60, 220, 110, 255));

        // --- Ghi đông, tay ga, tay phanh, bao tay, cẳng tay ---
        foreach (int s in new[] { 1, -1 })
        {
            // ống ghi đông (vuốt nhẹ về sau)
            Raylib.DrawCylinderEx(new Vector3(0f, BikeBarY, 0f), new Vector3(0.31f * s, BikeBarY, -0.07f), 0.013f, 0.012f, 8, chrome);

            // hộp công tắc: trái = xi-nhan (vàng), phải = đề/ngắt máy (đỏ)
            Raylib.DrawCube(new Vector3(0.285f * s, 1.095f, -0.050f), 0.075f, 0.050f, 0.085f, plastic);
            Raylib.DrawCube(new Vector3(0.285f * s, 1.123f, -0.065f), 0.025f, 0.008f, 0.020f, s > 0 ? new Color(255, 170, 40, 255) : new Color(200, 30, 30, 255));

            // tay nắm cao su
            Raylib.DrawCylinderEx(new Vector3(0.32f * s, BikeBarY, -0.073f), new Vector3(0.47f * s, BikeBarY, -0.115f), 0.0225f, 0.0225f, 10, rubber);
            Raylib.DrawSphereEx(new Vector3(0.47f * s, BikeBarY, -0.115f), 0.018f, 6, 6, chrome);

            // ---- TAY GA (bên phải người lái, s = -1): vòng bạc + vạch đánh dấu xoay theo ga ----
            if (s < 0)
            {
                Raylib.DrawCylinderEx(new Vector3(0.325f * s, BikeBarY, -0.075f), new Vector3(0.345f * s, BikeBarY, -0.082f), 0.0265f, 0.0265f, 10, new Color(210, 212, 218, 255));
                float rr = 0.0245f;
                Vector3 mark = new(0.44f * s, BikeBarY + MathF.Cos(throttleTwist) * rr, -0.105f - MathF.Sin(throttleTwist) * rr);
                Raylib.DrawCube(mark, 0.05f, 0.008f, 0.008f, new Color(235, 235, 240, 255));
            }

            // ---- BAO TAY + BÀN TAY ----
            Raylib.DrawCylinderEx(new Vector3(0.355f * s, BikeBarY, -0.082f), new Vector3(0.425f * s, BikeBarY, -0.100f), 0.0315f, 0.0315f, 10, glove);
            Raylib.DrawSphereEx(new Vector3(0.385f * s, 1.103f, -0.128f), 0.040f, 8, 8, glove);
            Raylib.DrawCylinderEx(new Vector3(0.36f * s, 1.10f, -0.11f), new Vector3(0.34f * s, 1.105f, -0.04f), 0.011f, 0.009f, 6, glove);
            // cổ tay + cẳng tay áo khoác
            Raylib.DrawCylinderEx(new Vector3(0.385f * s, 1.108f, -0.14f), new Vector3(0.36f * s, 1.09f, -0.24f), 0.038f, 0.042f, 10, glove);
            Raylib.DrawCylinderEx(new Vector3(0.36f * s, 1.09f, -0.24f), new Vector3(0.31f * s, 1.00f, -0.42f), 0.043f, 0.050f, 10, sleeve);

            // ---- TAY PHANH: bóp về phía tay nắm khi phanh ----
            Vector3 pivot = new(0.345f * s, 1.098f, -0.055f);
            Vector3 tip = new(0.392f * s, 1.108f - 0.012f * brakePull, 0.115f - 0.085f * brakePull);
            Raylib.DrawCylinderEx(pivot, tip, 0.008f, 0.006f, 6, chrome);
            Raylib.DrawSphereEx(tip, 0.011f, 6, 6, chrome);

            // ---- CHÂN GƯƠNG (gương hiển thị bằng overlay 2D tại cùng vị trí) ----
            Raylib.DrawCylinderEx(new Vector3(0.30f * s, 1.10f, -0.06f), new Vector3(0.52f * s, 1.29f, -0.03f), 0.010f, 0.010f, 6, chrome);
        }

        Rlgl.PopMatrix(); // hết khung tay lái
        Rlgl.PopMatrix(); // hết xe
    }

    // =====================================================================
    //  GƯƠNG CHIẾU HẬU
    // =====================================================================

    static void RenderRearviewMirrorTexture(RenderTexture2D target, Vector3 mirrorPos, float lookBackYaw, float lean, Color fogTint)
    {
        Camera3D mirrorCam = new()
        {
            Position = mirrorPos,
            Target = mirrorPos + new Vector3(MathF.Sin(lookBackYaw), -0.05f, MathF.Cos(lookBackYaw)),
            Up = new Vector3(MathF.Sin(-lean * 0.5f), MathF.Cos(-lean * 0.5f), 0f),
            FovY = 64.0f,
            Projection = CameraProjection.Perspective
        };

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(fogTint);
        Raylib.BeginMode3D(mirrorCam);
        DrawVietnameseStreet(bikePos, bikeYaw, bikeSteerAngle, bikeHeadlightOn, drawBikeHandlebar: false, eyePos: mirrorPos);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
    }

    static void DrawMirrorOverlay(RenderTexture2D target, Vector3 mirrorWorldPos, bool isLeft)
    {
        Vector3 camForward = Vector3.Normalize(camera.Target - camera.Position);
        if (Vector3.Dot(mirrorWorldPos - camera.Position, camForward) <= 0.25f) return;

        Vector2 screenPos = Raylib.GetWorldToScreen(mirrorWorldPos, camera);
        const float mirrorW = 168f;
        const float mirrorH = 98f;

        float x = screenPos.X - mirrorW * 0.5f;
        float y = screenPos.Y - mirrorH * 0.5f;

        if (x + mirrorW < -20 || x > ScreenWidth + 20 || y + mirrorH < -20 || y > ScreenHeight + 20) return;

        Raylib.DrawRectangleRounded(new Rectangle(x - 6, y - 6, mirrorW + 12, mirrorH + 12), 0.24f, 8, new Color(24, 26, 32, 255));
        Raylib.DrawRectangleRoundedLines(new Rectangle(x - 6, y - 6, mirrorW + 12, mirrorH + 12), 0.24f, 8, new Color(95, 102, 115, 255));

        Rectangle srcRect = new(0, 0, -target.Texture.Width, -target.Texture.Height);
        Rectangle dstRect = new(x, y, mirrorW, mirrorH);
        Raylib.DrawTexturePro(target.Texture, srcRect, dstRect, Vector2.Zero, 0f, Color.White);

        Raylib.DrawRectangle((int)x, (int)y, (int)mirrorW, (int)mirrorH, new Color(180, 210, 240, 18));
        Raylib.DrawRectangleLines((int)x, (int)y, (int)mirrorW, (int)mirrorH, new Color(145, 155, 170, 200));

        string tag = isLeft ? "L" : "R";
        DrawViText(tag, (int)(isLeft ? x + 8 : x + mirrorW - 20), (int)(y + mirrorH - 22), 14, new Color(220, 225, 235, 140));
    }

    // =====================================================================
    //  TIỆN ÍCH
    // =====================================================================

    static int StreetHash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 374761393 + b * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (int)(h & 0x7fffffff);
        }
    }

    static Color Dim(Color c, float k) => new Color((int)(c.R * k), (int)(c.G * k), (int)(c.B * k), (int)c.A);

    static Color PickWindowColor(int v)
    {
        return (v & 3) switch
        {
            0 => new Color(245, 205, 115, 255),   // đèn vàng
            1 => new Color(25, 30, 38, 255),      // tối
            2 => new Color(120, 165, 225, 255),   // ánh TV xanh
            _ => new Color(150, 112, 66, 255)     // đèn ngủ mờ
        };
    }

    static float FrontX(int side, float d) => side * (9.88f - d);

    // =====================================================================
    //  BẦU TRỜI ĐÊM: quầng sáng chân trời, sao nhấp nháy, trăng, mây
    // =====================================================================

    static void EnsureNightSky()
    {
        if (skyInitialized) return;

        const int count = 280;
        skyStarDirs = new Vector3[count];
        skyStarSize = new float[count];
        skyStarBase = new byte[count];
        skyStarPhase = new float[count];
        skyStarTint = new Color[count];

        Random r = new(20260);
        float sinMin = MathF.Sin(4f * MathF.PI / 180f);

        for (int i = 0; i < count; i++)
        {
            float az = (float)(r.NextDouble() * Math.PI * 2.0);
            float sinEl = sinMin + (1f - sinMin) * (float)r.NextDouble();
            float cosEl = MathF.Sqrt(1f - sinEl * sinEl);
            skyStarDirs[i] = new Vector3(cosEl * MathF.Sin(az), sinEl, cosEl * MathF.Cos(az));

            bool bright = r.NextDouble() > 0.93;
            skyStarSize[i] = bright ? 2.0f + (float)r.NextDouble() * 0.8f : 0.8f + (float)r.NextDouble() * 0.8f;
            skyStarBase[i] = (byte)(120 + r.Next(0, 136));
            skyStarPhase[i] = (float)(r.NextDouble() * Math.PI * 2.0);

            int tint = r.Next(0, 10);
            skyStarTint[i] = tint switch
            {
                < 6 => new Color(235, 240, 255, 255),
                < 8 => new Color(180, 205, 255, 255),
                8 => new Color(255, 236, 190, 255),
                _ => new Color(255, 200, 170, 255)
            };
        }

        skyInitialized = true;
    }

    // Vòng sương ấm (ô nhiễm ánh sáng thành phố) ở chân trời, vẽ bằng quad có vertex color
    static void DrawHorizonGlow(Vector3 eye)
    {
        const int seg = 40;
        const float r = 620f;
        const float yLow = -8f;
        const float yHigh = 170f;

        Rlgl.Begin(DrawMode.Quads);
        for (int i = 0; i < seg; i++)
        {
            float a0 = i / (float)seg * MathF.PI * 2f;
            float a1 = (i + 1) / (float)seg * MathF.PI * 2f;
            float x0 = eye.X + MathF.Sin(a0) * r, z0 = eye.Z + MathF.Cos(a0) * r;
            float x1 = eye.X + MathF.Sin(a1) * r, z1 = eye.Z + MathF.Cos(a1) * r;

            // mặt trong
            Rlgl.Color4ub(100, 74, 52, 120); Rlgl.Vertex3f(x0, yLow, z0);
            Rlgl.Color4ub(100, 74, 52, 120); Rlgl.Vertex3f(x1, yLow, z1);
            Rlgl.Color4ub(60, 80, 130, 0); Rlgl.Vertex3f(x1, yHigh, z1);
            Rlgl.Color4ub(60, 80, 130, 0); Rlgl.Vertex3f(x0, yHigh, z0);

            // mặt ngoài (đảo chiều để không bị loại bỏ mặt sau)
            Rlgl.Color4ub(60, 80, 130, 0); Rlgl.Vertex3f(x0, yHigh, z0);
            Rlgl.Color4ub(60, 80, 130, 0); Rlgl.Vertex3f(x1, yHigh, z1);
            Rlgl.Color4ub(100, 74, 52, 120); Rlgl.Vertex3f(x1, yLow, z1);
            Rlgl.Color4ub(100, 74, 52, 120); Rlgl.Vertex3f(x0, yLow, z0);
        }
        Rlgl.End();
    }

    static void DrawNightClouds(Vector3 eye, float time)
    {
        const int cloudCount = 10;
        for (int i = 0; i < cloudCount; i++)
        {
            float az = i * (MathF.PI * 2f / cloudCount) + MathF.Sin(i * 3.1f) * 0.18f + time * (0.004f + 0.0015f * (i % 3));
            float el = (9f + (i * 7) % 21) * (MathF.PI / 180f);
            const float dist = 360f;

            Vector3 c = eye + new Vector3(
                MathF.Cos(el) * MathF.Sin(az),
                MathF.Sin(el),
                MathF.Cos(el) * MathF.Cos(az)) * dist;

            float w = 38f + (i % 3) * 16f;
            Color col = new(30, 36, 54, 72);

            for (int k = 0; k < 3; k++)
            {
                float ox = (k - 1) * w * 0.55f;
                float sx = k == 1 ? w : w * 0.65f;
                Rlgl.PushMatrix();
                Rlgl.Translatef(c.X + ox, c.Y + (k == 1 ? 1.5f : 0f), c.Z);
                Rlgl.Scalef(sx, 4.5f, 12f);
                Raylib.DrawSphereEx(Vector3.Zero, 1f, 10, 12, col);
                Rlgl.PopMatrix();
            }
        }
    }

    static void DrawNightSky(Vector3 eye)
    {
        EnsureNightSky();
        float time = (float)Raylib.GetTime();

        DrawHorizonGlow(eye);

        // --- Sao nhấp nháy ---
        const float starR = 400f;
        for (int i = 0; i < skyStarDirs.Length; i++)
        {
            float tw = 0.72f + 0.28f * MathF.Sin(time * (1.3f + (i % 5) * 0.55f) + skyStarPhase[i]);
            int a = (int)(skyStarBase[i] * tw);
            Color t = skyStarTint[i];
            float sz = skyStarSize[i];
            Raylib.DrawCube(eye + skyStarDirs[i] * starR, sz, sz, sz, new Color(t.R, t.G, t.B, a));
        }

        // --- Trăng ---
        Vector3 moonDir = Vector3.Normalize(new Vector3(0.32f, 0.40f, 1.0f));
        Vector3 moonC = eye + moonDir * 430f;
        const float moonR = 14f;
        Raylib.DrawSphereEx(moonC, moonR, 28, 28, new Color(238, 236, 214, 255));

        Vector3 toEye = -moonDir;
        Vector3 t1 = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, toEye));
        Vector3 t2 = Vector3.Cross(toEye, t1);
        (float cx, float cy, float cr)[] craters =
        [
            (-0.35f, 0.25f, 0.20f),
            (0.30f, 0.40f, 0.14f),
            (0.15f, -0.30f, 0.24f),
            (-0.40f, -0.20f, 0.12f),
            (0.50f, -0.05f, 0.10f)
        ];
        foreach (var cr in craters)
        {
            float depth = MathF.Sqrt(MathF.Max(0f, 1f - cr.cx * cr.cx - cr.cy * cr.cy));
            Vector3 p = moonC + (t1 * cr.cx + t2 * cr.cy + toEye * depth) * moonR;
            Raylib.DrawSphereEx(p, cr.cr * moonR, 10, 10, new Color(196, 194, 176, 255));
        }

        // Quầng sáng quanh trăng (vẽ sau trăng, từ nhỏ đến lớn)
        Raylib.DrawSphereEx(moonC, moonR * 1.55f, 20, 20, new Color(215, 225, 255, 16));
        Raylib.DrawSphereEx(moonC, moonR * 2.3f, 20, 20, new Color(190, 205, 255, 10));
        Raylib.DrawSphereEx(moonC, moonR * 3.3f, 20, 20, new Color(170, 190, 245, 6));

        // --- Mây trôi chậm ---
        DrawNightClouds(eye, time);
    }

    // =====================================================================
    //  VẬT THỂ ĐƯỜNG PHỐ
    // =====================================================================

    static void DrawStreetTree(float x, float z, int seed)
    {
        const float baseY = 0.17f;
        float away = x >= 0f ? 1f : -1f;
        int g = seed % 3;

        Raylib.DrawCube(new Vector3(x, baseY + 0.08f, z), 1.0f, 0.16f, 1.0f, new Color(92, 92, 96, 255)); // bồn cây
        Raylib.DrawCylinder(new Vector3(x, baseY, z), 0.13f, 0.19f, 2.6f, 8, new Color(60, 44, 32, 255));

        Raylib.DrawSphereEx(new Vector3(x, 3.0f, z), 1.15f, 8, 10, new Color(20, 56 + g * 6, 34, 255));
        Raylib.DrawSphereEx(new Vector3(x + 0.45f * away, 3.55f, z - 0.3f), 0.85f, 8, 10, new Color(26, 66 + g * 4, 40, 255));
        Raylib.DrawSphereEx(new Vector3(x - 0.15f * away, 3.35f, z + 0.5f), 0.80f, 8, 10, new Color(18, 50 + g * 5, 30, 255));
    }

    static void DrawParkedScooter(Vector3 pos, float yawDeg, Color bodyCol)
    {
        Color dark = new(20, 20, 24, 255);
        Color chrome = new(170, 175, 184, 255);

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X, pos.Y, pos.Z);
        Rlgl.Rotatef(yawDeg, 0f, 1f, 0f);

        // bánh trước + sau (trục dọc theo X)
        Raylib.DrawCylinderEx(new Vector3(-0.05f, 0.19f, 0.62f), new Vector3(0.05f, 0.19f, 0.62f), 0.19f, 0.19f, 12, dark);
        Raylib.DrawCylinderEx(new Vector3(-0.06f, 0.19f, -0.55f), new Vector3(0.06f, 0.19f, -0.55f), 0.19f, 0.19f, 12, dark);
        // thân, sàn, yên
        Raylib.DrawCube(new Vector3(0f, 0.42f, -0.05f), 0.30f, 0.30f, 0.95f, bodyCol);
        Raylib.DrawCube(new Vector3(0f, 0.28f, 0.30f), 0.34f, 0.04f, 0.50f, dark);
        Raylib.DrawCube(new Vector3(0f, 0.66f, -0.38f), 0.30f, 0.10f, 0.55f, dark);
        // yếm, đầu, ghi đông
        Raylib.DrawCube(new Vector3(0f, 0.62f, 0.50f), 0.34f, 0.55f, 0.07f, bodyCol);
        Raylib.DrawCube(new Vector3(0f, 0.95f, 0.55f), 0.22f, 0.14f, 0.18f, bodyCol);
        Raylib.DrawCube(new Vector3(0f, 1.02f, 0.50f), 0.62f, 0.04f, 0.05f, dark);
        Raylib.DrawCube(new Vector3(0f, 0.95f, 0.65f), 0.14f, 0.09f, 0.03f, new Color(220, 215, 190, 255));
        // gương
        Raylib.DrawCylinderEx(new Vector3(0.28f, 1.03f, 0.50f), new Vector3(0.30f, 1.22f, 0.46f), 0.008f, 0.008f, 5, chrome);
        Raylib.DrawCylinderEx(new Vector3(-0.28f, 1.03f, 0.50f), new Vector3(-0.30f, 1.22f, 0.46f), 0.008f, 0.008f, 5, chrome);
        // đèn hậu
        Raylib.DrawCube(new Vector3(0f, 0.62f, -0.68f), 0.18f, 0.06f, 0.04f, new Color(220, 30, 30, 255));

        Rlgl.PopMatrix();
    }

    static void DrawStreetFoodCart(float x, float z, int seed)
    {
        Color umbrella = (seed % 2 == 0) ? new Color(200, 48, 44, 255) : new Color(225, 170, 40, 255);
        Color metal = new(150, 154, 160, 255);

        // thùng xe đẩy + mặt bàn + bánh
        Raylib.DrawCube(new Vector3(x, 0.17f + 0.40f, z), 0.74f, 0.45f, 1.30f, new Color(190, 195, 200, 255));
        Raylib.DrawCube(new Vector3(x, 0.17f + 0.65f, z), 0.80f, 0.06f, 1.40f, metal);
        Raylib.DrawCylinderEx(new Vector3(x - 0.43f, 0.35f, z + 0.5f), new Vector3(x - 0.37f, 0.35f, z + 0.5f), 0.18f, 0.18f, 10, new Color(16, 16, 18, 255));
        Raylib.DrawCylinderEx(new Vector3(x + 0.37f, 0.35f, z + 0.5f), new Vector3(x + 0.43f, 0.35f, z + 0.5f), 0.18f, 0.18f, 10, new Color(16, 16, 18, 255));

        // nồi nước lèo + hơi
        Raylib.DrawCylinder(new Vector3(x + 0.12f, 0.88f, z - 0.15f), 0.20f, 0.22f, 0.26f, 12, new Color(200, 204, 210, 255));
        Raylib.DrawSphere(new Vector3(x + 0.12f, 1.28f, z - 0.15f), 0.13f, new Color(230, 230, 238, 38));
        Raylib.DrawSphere(new Vector3(x + 0.15f, 1.55f, z - 0.12f), 0.17f, new Color(230, 230, 238, 24));

        // dù che
        Raylib.DrawCylinder(new Vector3(x, 0.82f, z - 0.45f), 0.025f, 0.025f, 1.40f, 6, metal);
        Raylib.DrawCylinder(new Vector3(x, 2.05f, z - 0.45f), 0.05f, 1.25f, 0.38f, 12, umbrella);

        // đèn treo + quầng sáng + vệt sáng trên vỉa hè
        Raylib.DrawSphere(new Vector3(x, 1.85f, z + 0.10f), 0.09f, new Color(255, 196, 110, 255));
        Raylib.DrawSphere(new Vector3(x, 1.85f, z + 0.10f), 0.40f, new Color(255, 196, 110, 36));
        Raylib.DrawCube(new Vector3(x, 0.176f, z), 2.4f, 0.006f, 2.6f, new Color(255, 196, 110, (int)26));
    }

    static void DrawBusStop(float z)
    {
        const float x = -7.1f;   // bên phải chiều xe chạy
        Color metal = new(80, 86, 94, 255);

        Raylib.DrawCube(new Vector3(x - 1.0f, 1.4f, z - 1.7f), 0.08f, 2.4f, 0.08f, metal);
        Raylib.DrawCube(new Vector3(x - 1.0f, 1.4f, z + 1.7f), 0.08f, 2.4f, 0.08f, metal);
        Raylib.DrawCube(new Vector3(x + 1.0f, 1.4f, z - 1.7f), 0.08f, 2.4f, 0.08f, metal);
        Raylib.DrawCube(new Vector3(x + 1.0f, 1.4f, z + 1.7f), 0.08f, 2.4f, 0.08f, metal);
        Raylib.DrawCube(new Vector3(x, 2.65f, z), 2.4f, 0.08f, 3.8f, new Color(60, 66, 76, 255));
        Raylib.DrawCube(new Vector3(x - 1.15f, 1.35f, z), 0.06f, 1.6f, 2.4f, new Color(210, 230, 215, 255));   // bảng quảng cáo sáng
        Raylib.DrawCube(new Vector3(x - 0.70f, 0.65f, z), 0.40f, 0.08f, 2.2f, new Color(120, 90, 60, 255));      // ghế chờ
        Raylib.DrawCube(new Vector3(x, 0.176f, z), 2.4f, 0.006f, 3.8f, new Color(210, 235, 220, (int)36));            // vệt sáng

        // cột biển trạm
        Raylib.DrawCylinder(new Vector3(-5.9f, 0.17f, z + 2.6f), 0.04f, 0.04f, 2.4f, 6, metal);
        Raylib.DrawCube(new Vector3(-5.9f, 2.7f, z + 2.6f), 0.45f, 0.45f, 0.04f, new Color(25, 95, 170, 255));
        Raylib.DrawCube(new Vector3(-5.9f, 2.7f, z + 2.57f), 0.26f, 0.20f, 0.02f, Color.White);
    }

    // =====================================================================
    //  ĐÈN PHA: vệt sáng loe dần trên mặt đường + chùm sáng mỏng trong sương
    // =====================================================================

    static readonly float[] BeamRingT = [0f, 0.12f, 0.35f, 0.65f, 1f];

    static void BeamVert(Vector3 p, float alpha, byte r, byte g, byte b)
    {
        Rlgl.Color4ub(r, g, b, (byte)Math.Clamp((int)alpha, 0, 255));
        Rlgl.Vertex3f(p.X, p.Y, p.Z);
    }

    static float BeamConeAlpha(float t)
    {
        const float peak = 11f;   // độ đậm tối đa của chùm sáng trong sương (càng nhỏ càng kín đáo)
        if (t < 0.2f) return peak * (t / 0.2f);
        return peak * MathF.Pow(1f - (t - 0.2f) / 0.8f, 1.4f);
    }

    static void DrawHeadlightBeam(Vector3 riderPos, float yaw, float steer)
    {
        // đèn pha quay theo tay lái một phần
        float aim = yaw + steer * BikeMaxSteerRad * 0.6f;
        Vector3 dir = new(MathF.Sin(aim), 0f, MathF.Cos(aim));
        Vector3 side = new(MathF.Cos(aim), 0f, -MathF.Sin(aim));
        Vector3 lamp = BikeSteeredToWorld(riderPos, yaw, steer, new Vector3(0f, 1.04f, 0.43f));

        const byte cr = 255, cg = 244, cb = 208;   // trắng ngà ấm

        // ---------- 1) Vệt sáng trên mặt đường: hình thang loe dần, sáng giữa - mờ hai mép - tắt dần theo khoảng cách ----------
        const int slices = 10;
        const float d0 = 1.8f, d1 = 24f;    // bắt đầu / kết thúc (m, tính từ đèn)
        const float w0 = 0.55f, w1 = 5.2f;  // nửa bề rộng ở đầu / cuối => góc loe
        const float peak = 46f;             // độ sáng tối đa của vệt trên đường (0..255)
        const float gy = 0.016f;

        Vector3 o = new(lamp.X, gy, lamp.Z);

        Rlgl.Begin(DrawMode.Quads);
        for (int i = 0; i < slices; i++)
        {
            float ta = i / (float)slices;
            float tb = (i + 1) / (float)slices;

            float aa = peak * MathF.Pow(1f - ta, 1.7f) * MathF.Min(1f, 0.4f + ta * 6f);
            float ab = peak * MathF.Pow(1f - tb, 1.7f) * MathF.Min(1f, 0.4f + tb * 6f);

            Vector3 ca = o + dir * (d0 + (d1 - d0) * ta);
            Vector3 cb2 = o + dir * (d0 + (d1 - d0) * tb);
            float wa = w0 + (w1 - w0) * ta;
            float wb = w0 + (w1 - w0) * tb;

            Vector3 la = ca + side * wa, lb = cb2 + side * wb;
            Vector3 ra = ca - side * wa, rb = cb2 - side * wb;

            // nửa trái (hai chiều quay để không bị cull)
            BeamVert(ca, aa, cr, cg, cb); BeamVert(cb2, ab, cr, cg, cb); BeamVert(lb, 0f, cr, cg, cb); BeamVert(la, 0f, cr, cg, cb);
            BeamVert(la, 0f, cr, cg, cb); BeamVert(lb, 0f, cr, cg, cb); BeamVert(cb2, ab, cr, cg, cb); BeamVert(ca, aa, cr, cg, cb);
            // nửa phải
            BeamVert(ca, aa, cr, cg, cb); BeamVert(ra, 0f, cr, cg, cb); BeamVert(rb, 0f, cr, cg, cb); BeamVert(cb2, ab, cr, cg, cb);
            BeamVert(cb2, ab, cr, cg, cb); BeamVert(rb, 0f, cr, cg, cb); BeamVert(ra, 0f, cr, cg, cb); BeamVert(ca, aa, cr, cg, cb);
        }
        Rlgl.End();

        // ---------- 2) Chùm sáng hình nón mỏng trong sương (rất nhạt, gần đèn gần như trong suốt nên không chói) ----------
        Vector3 apex = lamp + dir * 0.35f;
        Vector3 axisEnd = new(lamp.X + dir.X * 22f, 0.35f, lamp.Z + dir.Z * 22f);
        Vector3 axis = Vector3.Normalize(axisEnd - apex);
        float len = Vector3.Distance(apex, axisEnd);
        Vector3 r1 = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, axis));
        Vector3 r2 = Vector3.Cross(axis, r1);

        const int seg = 16;
        Rlgl.Begin(DrawMode.Quads);
        for (int k = 0; k < BeamRingT.Length - 1; k++)
        {
            float t0 = BeamRingT[k], t1 = BeamRingT[k + 1];
            float rad0 = 0.04f + 2.6f * t0, rad1 = 0.04f + 2.6f * t1;
            float al0 = BeamConeAlpha(t0), al1 = BeamConeAlpha(t1);
            Vector3 c0 = apex + axis * (len * t0);
            Vector3 c1 = apex + axis * (len * t1);

            for (int sIdx = 0; sIdx < seg; sIdx++)
            {
                float th0 = sIdx / (float)seg * MathF.PI * 2f;
                float th1 = (sIdx + 1) / (float)seg * MathF.PI * 2f;
                Vector3 p00 = c0 + (r1 * MathF.Cos(th0) + r2 * MathF.Sin(th0)) * rad0;
                Vector3 p01 = c0 + (r1 * MathF.Cos(th1) + r2 * MathF.Sin(th1)) * rad0;
                Vector3 p10 = c1 + (r1 * MathF.Cos(th0) + r2 * MathF.Sin(th0)) * rad1;
                Vector3 p11 = c1 + (r1 * MathF.Cos(th1) + r2 * MathF.Sin(th1)) * rad1;

                BeamVert(p00, al0, cr, cg, cb); BeamVert(p01, al0, cr, cg, cb); BeamVert(p11, al1, cr, cg, cb); BeamVert(p10, al1, cr, cg, cb);
                BeamVert(p10, al1, cr, cg, cb); BeamVert(p11, al1, cr, cg, cb); BeamVert(p01, al0, cr, cg, cb); BeamVert(p00, al0, cr, cg, cb);
            }
        }
        Rlgl.End();
    }

    // =====================================================================
    //  ĐƯỜNG PHỐ
    // =====================================================================

    static void DrawVietnameseStreet(Vector3 riderPos, float yaw, float steer, bool headlightOn, bool drawBikeHandlebar, Vector3? eyePos = null)
    {
        bool rearView = !drawBikeHandlebar;   // pass vẽ cho gương chiếu hậu: chỉ cần vẽ phía sau để nhẹ hơn
        Vector3 eye = eyePos ?? new Vector3(riderPos.X, BikeEyeHeight, riderPos.Z);

        DrawNightSky(eye);

        float roadLength = BikeRoadEndZ + 45f;
        const float RoadWidth = 10.6f;

        float minVisibleZ = rearView ? Math.Max(0f, riderPos.Z - 140f) : Math.Max(0f, riderPos.Z - 35f);
        float maxVisibleZ = Math.Min(roadLength, rearView ? riderPos.Z + 25f : riderPos.Z + 175f);
        float visLen = Math.Max(1f, maxVisibleZ - minVisibleZ);
        float visMid = (minVisibleZ + maxVisibleZ) * 0.5f;

        Raylib.DrawCube(new Vector3(0f, -0.05f, roadLength * 0.5f), RoadWidth, 0.1f, roadLength, new Color(26, 28, 34, 255));
        Raylib.DrawCube(new Vector3(-7.6f, 0.06f, roadLength * 0.5f), 4.6f, 0.22f, roadLength, new Color(68, 66, 64, 255));
        Raylib.DrawCube(new Vector3(7.6f, 0.06f, roadLength * 0.5f), 4.6f, 0.22f, roadLength, new Color(68, 66, 64, 255));

        float startCurbZ = MathF.Floor(minVisibleZ / 4.0f) * 4.0f;
        for (float z = startCurbZ; z < maxVisibleZ; z += 4.0f)
        {
            bool isRed = ((int)(z / 4.0f) % 2) == 0;
            Color curbCol = isRed ? new Color(175, 38, 38, 255) : new Color(215, 215, 215, 255);
            Raylib.DrawCube(new Vector3(-5.25f, 0.08f, z + 2f), 0.22f, 0.24f, 3.95f, curbCol);
            Raylib.DrawCube(new Vector3(5.25f, 0.08f, z + 2f), 0.22f, 0.24f, 3.95f, curbCol);

            if (((int)(z / 4.0f) % 2) == 0)
            {
                Raylib.DrawCube(new Vector3(0f, 0.005f, z + 1.5f), 0.16f, 0.01f, 2.2f, new Color(225, 195, 70, 220));
            }
        }

        // ---------- Chi tiết mặt đường ----------
        // Vạch trắng ven đường
        Color edgeLine = new(205, 205, 200, 190);
        Raylib.DrawCube(new Vector3(-4.85f, 0.006f, visMid), 0.11f, 0.01f, visLen, edgeLine);
        Raylib.DrawCube(new Vector3(4.85f, 0.006f, visMid), 0.11f, 0.01f, visLen, edgeLine);

        // Vạch qua đường (zebra) + vạch dừng + biển báo người đi bộ
        const float crosswalkSpacing = 160f;
        int cwFirst = Math.Max(0, (int)MathF.Floor((minVisibleZ - 44f) / crosswalkSpacing));
        for (int cwk = cwFirst; ; cwk++)
        {
            float cz = cwk * crosswalkSpacing + 40f;
            if (cz > maxVisibleZ || cz > BikeRoadEndZ - 20f) break;
            if (cz < minVisibleZ - 4f) continue;

            for (float cx = -4.3f; cx <= 4.31f; cx += 0.95f)
            {
                Raylib.DrawCube(new Vector3(cx, 0.008f, cz), 0.5f, 0.01f, 3.0f, new Color(220, 220, 215, 205));
            }
            Raylib.DrawCube(new Vector3(0f, 0.008f, cz - 2.4f), 10.0f, 0.01f, 0.28f, new Color(220, 220, 215, 205));

            Raylib.DrawCylinder(new Vector3(-5.75f, 0.17f, cz - 2.2f), 0.035f, 0.035f, 2.2f, 6, new Color(110, 114, 120, 255));
            Raylib.DrawCube(new Vector3(-5.75f, 2.55f, cz - 2.2f), 0.5f, 0.5f, 0.04f, new Color(245, 205, 45, 255));
            Raylib.DrawCube(new Vector3(-5.75f, 2.55f, cz - 2.23f), 0.22f, 0.26f, 0.02f, new Color(25, 25, 28, 255));
        }

        // Nắp cống
        float mhStart = MathF.Floor(minVisibleZ / 57f) * 57f + 20f;
        for (float mz = mhStart; mz < maxVisibleZ; mz += 57f)
        {
            if (mz < minVisibleZ) continue;
            float mx = (((int)(mz / 57f)) % 2 == 0) ? 1.7f : -1.9f;
            Raylib.DrawCylinder(new Vector3(mx, 0.002f, mz), 0.36f, 0.36f, 0.01f, 14, new Color(40, 42, 48, 255));
            Raylib.DrawCylinder(new Vector3(mx, 0.003f, mz), 0.26f, 0.26f, 0.01f, 14, new Color(52, 54, 60, 255));
        }

        // Vũng nước mưa
        float pdStart = MathF.Floor(minVisibleZ / 43f) * 43f;
        for (float pz = pdStart; pz < maxVisibleZ; pz += 43f)
        {
            int pi = (int)(pz / 43f);
            if (pz < minVisibleZ || StreetHash(pi, 5) % 3 != 0) continue;
            float px = ((StreetHash(pi, 9) % 80) - 40) * 0.1f;
            float pr = 0.8f + (StreetHash(pi, 13) % 8) * 0.1f;
            Rlgl.PushMatrix();
            Rlgl.Translatef(px, 0.004f, pz);
            Rlgl.Scalef(1f, 1f, 2.2f);
            Raylib.DrawCylinder(Vector3.Zero, pr, pr, 0.006f, 14, new Color(70, 92, 130, 95));
            Rlgl.PopMatrix();
        }

        // ---------- Nhà phố hai bên ----------
        Color[] wallPalette =
        [
            new(148, 142, 125, 255),
            new(125, 138, 145, 255),
            new(155, 132, 118, 255),
            new(135, 145, 130, 255)
        ];

        float houseLimitZ = BikeRoadEndZ + 20f;
        int startHouseIdx = Math.Max(0, (int)((minVisibleZ - 6f) / 6.5f));
        for (int idx = startHouseIdx; ; idx++)
        {
            float z = 6f + idx * 6.5f;
            if (z > maxVisibleZ || z > houseLimitZ) break;

            for (int side = -1; side <= 1; side += 2)
            {
                int seed = idx * 14 + (side > 0 ? 3 : 1);
                float h = 6.2f + (seed % 3) * 2.6f;
                float xCenter = side * 12.6f;
                Color wallCol = wallPalette[seed % wallPalette.Length];

                int hv = StreetHash(idx, side * 7 + 1);
                int variant = hv % 6;                    // 0 minimart, 1 cửa cuốn, 2 quán ăn, 3 tiệm thuốc, 4 tạp hóa, 5 cửa cuốn
                bool closed = variant == 1 || variant == 5;
                var shop = vnShopSigns[(hv >> 4) % vnShopSigns.Length];

                Raylib.DrawCube(new Vector3(xCenter, h * 0.5f, z), 5.4f, h, 6.35f, wallCol);

                // mái + phụ kiện sân thượng
                Color roofCol = new((byte)(wallCol.R * 3 / 4), (byte)(wallCol.G * 3 / 4), (byte)(wallCol.B * 3 / 4), (byte)255);
                Raylib.DrawCube(new Vector3(xCenter, h + 0.15f, z), 5.6f, 0.3f, 6.45f, roofCol);
                if ((hv >> 5) % 3 == 0)
                {
                    Raylib.DrawCylinder(new Vector3(xCenter, h + 0.3f, z + 1.2f), 0.55f, 0.55f, 1.1f, 10, new Color(185, 190, 195, 255));
                    Raylib.DrawCylinder(new Vector3(xCenter, h + 1.4f, z + 1.2f), 0.10f, 0.50f, 0.18f, 10, new Color(150, 154, 160, 255));
                }
                if ((hv >> 8) % 2 == 0)
                {
                    Vector3 antBase = new(xCenter - side * 1.0f, h + 0.3f, z - 1.4f);
                    Raylib.DrawCylinderEx(antBase, antBase + new Vector3(0f, 2.8f, 0f), 0.025f, 0.018f, 5, new Color(40, 42, 46, 255));
                    Raylib.DrawCube(antBase + new Vector3(0f, 2.3f, 0f), 0.02f, 0.02f, 1.3f, new Color(40, 42, 46, 255));
                    Raylib.DrawCube(antBase + new Vector3(0f, 1.8f, 0f), 0.02f, 0.02f, 0.9f, new Color(40, 42, 46, 255));
                }

                float frontX = side * 9.88f;
                Raylib.DrawCube(new Vector3(frontX, 1.45f, z), 0.12f, 2.9f, 3.8f, new Color(58, 66, 74, 255));

                Raylib.DrawCube(new Vector3(side * 9.55f, 3.6f, z), 0.75f, 0.18f, 4.4f, new Color(90, 92, 98, 255));
                Raylib.DrawCube(new Vector3(side * 9.22f, 4.05f, z), 0.08f, 0.85f, 4.4f, new Color(40, 42, 46, 255));

                // cửa sổ các tầng
                bool windowLit = (seed % 3) != 0;
                Color winCol = windowLit ? new Color(245, 205, 115, 255) : new Color(25, 30, 38, 255);
                Raylib.DrawCube(new Vector3(frontX, 4.5f, z), 0.10f, 1.35f, 2.1f, winCol);
                if (h > 8.0f)
                    Raylib.DrawCube(new Vector3(frontX, 7.35f, z), 0.10f, 1.25f, 2.0f, PickWindowColor(hv >> 3));
                if (h > 10.4f)
                    Raylib.DrawCube(new Vector3(frontX, 10.0f, z), 0.10f, 1.25f, 2.0f, PickWindowColor(hv >> 6));

                // dàn nóng điều hòa
                if ((hv >> 7) % 2 == 0)
                {
                    Raylib.DrawCube(new Vector3(side * 9.72f, 5.8f, z + 1.3f), 0.5f, 0.42f, 0.7f, new Color(205, 208, 210, 255));
                    Raylib.DrawCube(new Vector3(side * 9.47f, 5.8f, z + 1.3f), 0.04f, 0.30f, 0.55f, new Color(70, 74, 80, 255));
                }

                // ---- Bảng hiệu ngang ----
                Color signBg = closed ? Dim(shop.SignBg, 0.35f) : shop.SignBg;
                Color signTx = closed ? Dim(shop.TextCol, 0.35f) : shop.TextCol;
                Raylib.DrawCube(new Vector3(side * 9.65f, 3.15f, z), 0.25f, 0.75f, 4.6f, signBg);
                for (int li = 0; li < 7; li++)
                {
                    float lw = 0.16f + ((hv >> (li + 2)) & 3) * 0.07f;
                    float lz = z - 1.8f + li * 0.6f;
                    Raylib.DrawCube(new Vector3(side * 9.50f, 3.15f, lz), 0.03f, 0.26f, lw, signTx);
                }

                // ---- Mặt tiền theo từng loại cửa hàng ----
                if (closed)
                {
                    // cửa cuốn kim loại
                    Raylib.DrawCube(new Vector3(FrontX(side, 0.07f), 1.45f, z), 0.07f, 2.9f, 3.8f, new Color(138, 142, 148, 255));
                    for (float sy = 0.25f; sy < 2.85f; sy += 0.3f)
                        Raylib.DrawCube(new Vector3(FrontX(side, 0.105f), sy, z), 0.02f, 0.03f, 3.8f, new Color(98, 102, 108, 255));
                }
                else
                {
                    Color glow = variant switch
                    {
                        0 => new Color(210, 235, 255, 255),   // minimart đèn LED trắng lạnh
                        2 => new Color(255, 226, 160, 255),   // quán ăn
                        3 => new Color(195, 240, 210, 255),   // tiệm thuốc
                        _ => new Color(255, 192, 112, 255)    // tạp hóa
                    };
                    Raylib.DrawCube(new Vector3(FrontX(side, 0.07f), 1.35f, z), 0.04f, 2.45f, 3.4f, glow);
                    // quầy hàng
                    Raylib.DrawCube(new Vector3(FrontX(side, 0.22f), 0.55f, z), 0.30f, 0.76f, 3.2f, new Color(62, 44, 30, 255));
                    // vệt sáng hắt ra vỉa hè
                    Raylib.DrawCube(new Vector3(side * 8.7f, 0.176f, z), 2.4f, 0.006f, 3.8f, new Color(glow.R, glow.G, glow.B, (byte)26));

                    if (variant == 2)
                    {
                        // bàn ghế nhựa + đèn lồng
                        float tx = FrontX(side, 1.0f);
                        Raylib.DrawCube(new Vector3(tx, 0.62f, z), 0.9f, 0.04f, 0.7f, new Color(200, 205, 210, 255));
                        for (int st = 0; st < 4; st++)
                        {
                            float sx = tx + ((st % 2 == 0) ? -0.62f : 0.62f);
                            float sz = z + ((st < 2) ? -0.55f : 0.55f);
                            Color stoolCol = ((hv >> st) & 1) == 0 ? new Color(200, 40, 40, 255) : new Color(50, 90, 170, 255);
                            Raylib.DrawCube(new Vector3(sx, 0.31f, sz), 0.30f, 0.28f, 0.30f, stoolCol);
                        }
                        Raylib.DrawSphere(new Vector3(tx, 2.15f, z), 0.14f, new Color(255, 170, 90, 255));
                        Raylib.DrawSphere(new Vector3(tx, 2.15f, z), 0.5f, new Color(255, 170, 90, 40));
                    }
                    else if (variant == 3)
                    {
                        // biển chữ thập xanh treo vuông góc mặt tiền
                        float bz = z + 1.95f;
                        Raylib.DrawCube(new Vector3(FrontX(side, 0.45f), 2.5f, bz), 0.75f, 0.75f, 0.10f, new Color(30, 200, 110, 255));
                        Raylib.DrawCube(new Vector3(FrontX(side, 0.45f), 2.5f, bz - 0.06f), 0.50f, 0.14f, 0.03f, Color.White);
                        Raylib.DrawCube(new Vector3(FrontX(side, 0.45f), 2.5f, bz - 0.06f), 0.14f, 0.50f, 0.03f, Color.White);
                    }
                    else if (variant == 4)
                    {
                        // đồ ăn vặt treo + tủ kem
                        Color[] snack =
                        [
                            new(220, 50, 50, 255), new(240, 200, 40, 255),
                            new(50, 150, 220, 255), new(60, 180, 90, 255)
                        ];
                        for (int sn = 0; sn < 8; sn++)
                        {
                            Raylib.DrawCube(new Vector3(FrontX(side, 0.14f), 2.35f - (sn % 2) * 0.2f, z - 1.4f + sn * 0.4f), 0.04f, 0.42f, 0.14f, snack[sn % snack.Length]);
                        }
                        Raylib.DrawCube(new Vector3(FrontX(side, 0.8f), 0.62f, z + 1.2f), 0.6f, 0.9f, 0.9f, new Color(210, 225, 235, 255));
                        Raylib.DrawCube(new Vector3(FrontX(side, 0.8f), 1.09f, z + 1.2f), 0.5f, 0.04f, 0.8f, new Color(150, 200, 240, 255));
                    }
                    else
                    {
                        // minimart: biển blade "24H" nhỏ
                        Raylib.DrawCube(new Vector3(FrontX(side, 0.40f), 2.55f, z + 1.95f), 0.55f, 1.0f, 0.10f, shop.SignBg);
                        Raylib.DrawCube(new Vector3(FrontX(side, 0.40f), 2.55f, z + 1.89f), 0.30f, 0.50f, 0.03f, shop.TextCol);
                    }
                }

                // Xe máy dựng trên vỉa hè
                if ((hv >> 11) % 3 == 0)
                {
                    float yawJit = -90f * side + (((hv >> 13) % 25) - 12);
                    DrawParkedScooter(
                        new Vector3(side * 7.35f, 0.17f, z - 0.9f + ((hv >> 15) % 3) * 0.5f),
                        yawJit,
                        parkedBikeColors[(hv >> 17) % parkedBikeColors.Length]);
                    if ((hv >> 20) % 3 == 0)
                    {
                        DrawParkedScooter(
                            new Vector3(side * 7.45f, 0.17f, z + 1.5f),
                            yawJit + 6f,
                            parkedBikeColors[(hv >> 22) % parkedBikeColors.Length]);
                    }
                }

                // Xe đẩy đồ ăn đêm
                if (!closed && (hv >> 24) % 8 == 0)
                {
                    DrawStreetFoodCart(side * 8.0f, z + 2.4f, hv);
                }
            }
        }

        // ---------- Cây xanh, thùng rác, trạm xe buýt ----------
        float poleLimitZ = BikeRoadEndZ + 15f;
        float startTreeZ = MathF.Floor(minVisibleZ / 24f) * 24f;
        for (float tz = startTreeZ; tz < maxVisibleZ && tz < poleLimitZ; tz += 24f)
        {
            if (tz < 6f || tz < minVisibleZ) continue;
            int tk = (int)(tz / 24f);
            DrawStreetTree(-6.7f, tz, StreetHash(tk, 1));
            DrawStreetTree(6.7f, tz, StreetHash(tk, 2));
        }

        float binStart = MathF.Floor(minVisibleZ / 48f) * 48f + 31f;
        for (float bz = binStart; bz < maxVisibleZ && bz < poleLimitZ; bz += 48f)
        {
            if (bz < minVisibleZ) continue;
            float bx = (((int)(bz / 48f)) % 2 == 0) ? 6.1f : -6.1f;
            Raylib.DrawCylinder(new Vector3(bx, 0.17f, bz), 0.27f, 0.25f, 0.85f, 10, new Color(34, 88, 56, 255));
            Raylib.DrawCylinder(new Vector3(bx, 1.02f, bz), 0.30f, 0.30f, 0.06f, 10, new Color(22, 58, 38, 255));
        }

        int busFirst = Math.Max(0, (int)MathF.Floor((minVisibleZ - 110f) / 280f));
        for (int bk = busFirst; ; bk++)
        {
            float bsz = bk * 280f + 100f;
            if (bsz > maxVisibleZ || bsz > BikeRoadEndZ - 30f) break;
            if (bsz < minVisibleZ - 4f) continue;
            DrawBusStop(bsz);
        }

        // ---------- Cột đèn + dây điện ----------
        float startPoleZ = Math.Max(12f, MathF.Floor(minVisibleZ / 24f) * 24f + 12f);
        for (float z = startPoleZ; z < maxVisibleZ && z < poleLimitZ; z += 24f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float poleX = side * 5.65f;
                Raylib.DrawCylinder(new Vector3(poleX, 0f, z), 0.14f, 0.18f, 6.8f, 8, new Color(115, 118, 122, 255));

                Vector3 armStart = new(poleX, 6.2f, z);
                Vector3 armEnd = new(poleX - side * 1.65f, 6.55f, z);
                Raylib.DrawCylinderEx(armStart, armEnd, 0.05f, 0.04f, 6, new Color(80, 85, 90, 255));

                Raylib.DrawSphere(armEnd, 0.16f, new Color(255, 225, 145, 255));
                Raylib.DrawSphere(armEnd, 0.55f, new Color(255, 225, 145, 26));                                  // quầng sáng quanh bóng
                Raylib.DrawCylinder(new Vector3(armEnd.X, 0.005f, z), 2.8f, 2.8f, 0.005f, 16, new Color(255, 210, 115, 24));
                Raylib.DrawCylinder(new Vector3(armEnd.X, 0f, z), 0.15f, 2.6f, 6.4f, 14, new Color(255, 225, 150, 12)); // chùm sáng trong sương

                if (z + 24f < poleLimitZ)
                {
                    Vector3 p1 = new(poleX, 5.4f, z);
                    Vector3 pMid = new(poleX, 4.75f, z + 12f);
                    Vector3 p2 = new(poleX, 5.4f, z + 24f);
                    Raylib.DrawLine3D(p1, pMid, new Color(15, 15, 18, 255));
                    Raylib.DrawLine3D(pMid, p2, new Color(15, 15, 18, 255));

                    Vector3 p1b = new(poleX, 5.05f, z);
                    Vector3 pMidb = new(poleX, 4.35f, z + 12f);
                    Vector3 p2b = new(poleX, 5.05f, z + 24f);
                    Raylib.DrawLine3D(p1b, pMidb, new Color(15, 15, 18, 255));
                    Raylib.DrawLine3D(pMidb, p2b, new Color(15, 15, 18, 255));
                }
            }
        }

        // ---------- Biển chỉ dẫn & cổng đích ----------
        float signZ = BikeRoadEndZ * 0.5f;
        if (signZ >= minVisibleZ && signZ <= maxVisibleZ)
        {
            Raylib.DrawCylinder(new Vector3(5.45f, 0f, signZ), 0.06f, 0.06f, 2.8f, 8, Color.LightGray);
            Raylib.DrawCube(new Vector3(4.95f, 2.55f, signZ), 1.35f, 0.45f, 0.08f, new Color(18, 78, 145, 255));
            for (int bi = 0; bi < 5; bi++)
            {
                Raylib.DrawCube(new Vector3(4.95f - 0.45f + bi * 0.22f, 2.55f, signZ - 0.05f), 0.12f, 0.20f, 0.02f, Color.White);
            }
        }

        if (BikeRoadEndZ >= minVisibleZ && BikeRoadEndZ <= maxVisibleZ)
        {
            Raylib.DrawCube(new Vector3(5.6f, 1.9f, BikeRoadEndZ), 0.45f, 3.8f, 0.45f, new Color(195, 190, 175, 255));
            Raylib.DrawCube(new Vector3(8.8f, 1.9f, BikeRoadEndZ), 0.45f, 3.8f, 0.45f, new Color(195, 190, 175, 255));
            Raylib.DrawCube(new Vector3(7.2f, 3.65f, BikeRoadEndZ), 3.7f, 0.55f, 0.55f, new Color(165, 42, 42, 255));
            Raylib.DrawCylinder(new Vector3(2.2f, 0.01f, BikeRoadEndZ), 2.2f, 2.2f, 0.01f, 20, new Color(90, 210, 140, 65));
        }

        // ---------- Cabin xe (tay lái, tay ga, cổ xe...) ----------
        Vector3 fwd = new(MathF.Sin(yaw), 0f, MathF.Cos(yaw));

        if (drawBikeHandlebar)
        {
            DrawBikeCockpit(riderPos, yaw, steer, headlightOn);
        }

        if (headlightOn && !rearView)
        {
            DrawHeadlightBeam(riderPos, yaw, steer);
        }
    }
}