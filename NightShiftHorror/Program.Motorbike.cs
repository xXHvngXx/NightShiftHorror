using System.Numerics;
using Raylib_cs;

partial class Program
{
    const float BikeRoadEndZ = 1250.0f;
    const float BikeMaxSpeed = 22.0f;

    static Vector3 bikePos = new(2.2f, 0f, 8f);
    static float bikeYaw = 0f;
    static float bikeSpeed = 0f;
    static float bikeLean = 0f;
    static float bikeSteerAngle = 0f;
    static bool bikeHeadlightOn = true;
    static bool bikeIntroTipShown = false;
    static bool bikeFadingToNext = false;

    static RenderTexture2D leftMirrorTarget;
    static RenderTexture2D rightMirrorTarget;
    static bool mirrorsInitialized = false;

    // Playlist nhạc khi chạy xe
    static List<string> bikePlaylist = [];
    static int bikeTrackIndex = 0;
    static Music bikeCurrentMusic;
    static bool bikeMusicLoaded = false;
    static bool bikeMusicEnabled = true;
    static float bikeDiscAngle = 0f;

    static readonly (string Label, Color SignBg, Color TextCol)[] vnShopSigns =
    [
        ("TẠP HÓA CÔ BA", new Color(185, 35, 35, 255), Color.Yellow),
        ("CƠM TẤM ĐÊM", new Color(25, 95, 165, 255), Color.White),
        ("TIỆM THUỐC TÂY", new Color(22, 130, 68, 255), Color.White),
        ("PHỞ BÒ GIA TRUYỀN", new Color(165, 95, 20, 255), Color.Yellow),
        ("SỬA XE - VÁ VỎ", new Color(55, 65, 80, 255), Color.Orange),
        ("TRÀ CHANH - ĂN VẶT", new Color(120, 45, 145, 255), Color.RayWhite)
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
        bikeHeadlightOn = true;
        bikeIntroTipShown = false;
        bikeFadingToNext = false;
        camYaw = 0f;
        camPitch = -0.04f;
        fadeAlpha = 1.0f;
        gameScene = 3;

        StartDialogue(
            "Tú",
            "Trời tối sương bắt đầu xuống rồi... Chạy xe về phòng trọ nghỉ một lát rồi 11 rưỡi qua đường Số 9 nhận ca. (Bấm [W/S] Ga/Phanh, [A/D] Lái, [L] Đèn pha, [Space] Còi)",
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
        Raylib.SetSoundPitch(motorbikeEngineSound, 0.72f + speedRatio * 1.15f);
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

            if (throttle)
                bikeSpeed = Math.Min(BikeMaxSpeed, bikeSpeed + 11.5f * dt);
            else if (brake)
                bikeSpeed = Math.Max(0f, bikeSpeed - 18.0f * dt);
            else
                bikeSpeed = Math.Max(0f, bikeSpeed - 4.2f * dt);

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

        GetMirrorWorldPositions(bikePos, bikeYaw, bikeSteerAngle, out Vector3 leftMirrorPos, out Vector3 rightMirrorPos);

        Color nightStreetFog = new(12, 15, 24, 255);
        RenderRearviewMirrorTexture(leftMirrorTarget, leftMirrorPos, bikeYaw + MathF.PI + 0.22f, bikeLean, nightStreetFog);
        RenderRearviewMirrorTexture(rightMirrorTarget, rightMirrorPos, bikeYaw + MathF.PI - 0.22f, bikeLean, nightStreetFog);

        Vector2 mouseDelta = Raylib.GetMouseDelta();
        camYaw = Math.Clamp(camYaw - mouseDelta.X * 0.0018f, -0.85f, 0.85f);
        camPitch = Math.Clamp(camPitch - mouseDelta.Y * 0.0018f, -0.45f, 0.45f);

        float engineVib = MathF.Sin((float)Raylib.GetTime() * (28f + bikeSpeed * 2.2f)) * (0.003f + speedRatio * 0.006f);
        Vector3 eyePos = new(bikePos.X, 1.42f + engineVib, bikePos.Z);
        float totalYaw = bikeYaw + camYaw;

        camera.Position = eyePos;
        camera.Target = eyePos + new Vector3(
            MathF.Sin(totalYaw) * MathF.Cos(camPitch),
            MathF.Sin(camPitch),
            MathF.Cos(totalYaw) * MathF.Cos(camPitch)
        );
        camera.Up = new Vector3(MathF.Sin(bikeLean), MathF.Cos(bikeLean), 0f);
        camera.FovY += ((NormalFov + speedRatio * 9.0f) - camera.FovY) * (1f - MathF.Exp(-5f * dt));

        Raylib.BeginDrawing();

        RenderFoggyScene(() =>
        {
            DrawVietnameseStreet(bikePos, bikeYaw, bikeSteerAngle, bikeHeadlightOn, drawBikeHandlebar: true);
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

    static void GetMirrorWorldPositions(Vector3 riderPos, float yaw, float steer, out Vector3 leftMirror, out Vector3 rightMirror)
    {
        Vector3 fwd = new(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        Vector3 right = new(MathF.Cos(yaw), 0f, -MathF.Sin(yaw));

        Vector3 headCenter = riderPos + fwd * 0.72f + new Vector3(0f, 0.96f, 0f);
        Vector3 leftGrip = headCenter - right * 0.36f - fwd * (0.08f - steer * 0.06f);
        Vector3 rightGrip = headCenter + right * 0.36f - fwd * (0.08f + steer * 0.06f);

        leftMirror = leftGrip - right * 0.11f + new Vector3(0f, 0.24f, 0f);
        rightMirror = rightGrip + right * 0.11f + new Vector3(0f, 0.24f, 0f);
    }

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
        DrawVietnameseStreet(bikePos, bikeYaw, bikeSteerAngle, bikeHeadlightOn, drawBikeHandlebar: false);
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

    static void DrawVietnameseStreet(Vector3 riderPos, float yaw, float steer, bool headlightOn, bool drawBikeHandlebar)
    {
        float roadLength = BikeRoadEndZ + 45f;
        const float RoadWidth = 10.6f;

        float minVisibleZ = Math.Max(0f, riderPos.Z - 35f);
        float maxVisibleZ = Math.Min(roadLength, riderPos.Z + 175f);

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

                Raylib.DrawCube(new Vector3(xCenter, h * 0.5f, z), 5.4f, h, 6.35f, wallCol);

                float frontX = side * 9.88f;
                Raylib.DrawCube(new Vector3(frontX, 1.45f, z), 0.12f, 2.9f, 3.8f, new Color(58, 66, 74, 255));

                Raylib.DrawCube(new Vector3(side * 9.55f, 3.6f, z), 0.75f, 0.18f, 4.4f, new Color(90, 92, 98, 255));
                Raylib.DrawCube(new Vector3(side * 9.22f, 4.05f, z), 0.08f, 0.85f, 4.4f, new Color(40, 42, 46, 255));

                bool windowLit = (seed % 3) != 0;
                Color winCol = windowLit ? new Color(245, 205, 115, 255) : new Color(25, 30, 38, 255);
                Raylib.DrawCube(new Vector3(frontX, 4.5f, z), 0.10f, 1.35f, 2.1f, winCol);

                if (seed % 2 == 0)
                {
                    var sign = vnShopSigns[seed % vnShopSigns.Length];
                    Raylib.DrawCube(new Vector3(side * 9.65f, 3.15f, z), 0.25f, 0.75f, 4.6f, sign.SignBg);
                }
            }
        }

        float poleLimitZ = BikeRoadEndZ + 15f;
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
                Raylib.DrawCylinder(new Vector3(armEnd.X, 0.005f, z), 2.8f, 2.8f, 0.005f, 16, new Color(255, 210, 115, 24));

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

        float signZ = BikeRoadEndZ * 0.5f;
        if (signZ >= minVisibleZ && signZ <= maxVisibleZ)
        {
            Raylib.DrawCylinder(new Vector3(5.45f, 0f, signZ), 0.06f, 0.06f, 2.8f, 8, Color.LightGray);
            Raylib.DrawCube(new Vector3(4.95f, 2.55f, signZ), 1.35f, 0.45f, 0.08f, new Color(18, 78, 145, 255));
        }

        if (BikeRoadEndZ >= minVisibleZ && BikeRoadEndZ <= maxVisibleZ)
        {
            Raylib.DrawCube(new Vector3(5.6f, 1.9f, BikeRoadEndZ), 0.45f, 3.8f, 0.45f, new Color(195, 190, 175, 255));
            Raylib.DrawCube(new Vector3(8.8f, 1.9f, BikeRoadEndZ), 0.45f, 3.8f, 0.45f, new Color(195, 190, 175, 255));
            Raylib.DrawCube(new Vector3(7.2f, 3.65f, BikeRoadEndZ), 3.7f, 0.55f, 0.55f, new Color(165, 42, 42, 255));
            Raylib.DrawCylinder(new Vector3(2.2f, 0.01f, BikeRoadEndZ), 2.2f, 2.2f, 0.01f, 20, new Color(90, 210, 140, 65));
        }

        Vector3 fwd = new(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        Vector3 right = new(MathF.Cos(yaw), 0f, -MathF.Sin(yaw));

        if (drawBikeHandlebar)
        {
            Vector3 headCenter = riderPos + fwd * 0.72f + new Vector3(0f, 0.96f, 0f);
            Raylib.DrawCube(headCenter, 0.34f, 0.16f, 0.24f, new Color(25, 28, 34, 255));
            Raylib.DrawCube(headCenter - fwd * 0.08f + new Vector3(0f, 0.08f, 0f), 0.18f, 0.03f, 0.12f, new Color(45, 175, 210, 255));

            Vector3 leftGrip = headCenter - right * 0.36f - fwd * (0.08f - steer * 0.06f);
            Vector3 rightGrip = headCenter + right * 0.36f - fwd * (0.08f + steer * 0.06f);
            Raylib.DrawCylinderEx(headCenter, leftGrip, 0.025f, 0.028f, 8, new Color(35, 35, 40, 255));
            Raylib.DrawCylinderEx(headCenter, rightGrip, 0.025f, 0.028f, 8, new Color(35, 35, 40, 255));

            GetMirrorWorldPositions(riderPos, yaw, steer, out Vector3 leftMirrorPos, out Vector3 rightMirrorPos);
            Raylib.DrawCylinderEx(leftGrip, leftMirrorPos - new Vector3(0f, 0.05f, 0f), 0.012f, 0.012f, 6, Color.DarkGray);
            Raylib.DrawCylinderEx(rightGrip, rightMirrorPos - new Vector3(0f, 0.05f, 0f), 0.012f, 0.012f, 6, Color.DarkGray);
        }

        if (headlightOn)
        {
            Vector3 beamNear = riderPos + fwd * 3.5f + new Vector3(0f, 0.012f, 0f);
            Vector3 beamFar = riderPos + fwd * 14.0f + new Vector3(0f, 0.012f, 0f);
            Raylib.DrawCylinder(beamNear, 1.35f, 1.35f, 0.008f, 16, new Color(255, 248, 210, 48));
            Raylib.DrawCylinder(beamFar, 2.75f, 2.75f, 0.008f, 18, new Color(255, 245, 195, 28));
        }
    }
}