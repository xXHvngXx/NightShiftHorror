using System.Numerics;
using Raylib_cs;

partial class Program
{
    static readonly BoundingBox backyardMotorbikeBox = new(
        new Vector3(3.75f, 0f, -16.45f),
        new Vector3(4.65f, 1.35f, -14.45f)
    );

    static readonly BoundingBox backyardAlleyGateBox = new(
        new Vector3(7.55f, 0f, -18.80f),
        new Vector3(9.65f, 2.50f, -18.40f)
    );

    static int pendingJumpscareEnding = -1;

    static bool IsConfirmKeyPressed()
        => Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter);

    static void ApplyCameraOrientation(Vector3 eyePos)
    {
        Vector3 lookDir = new(
            MathF.Sin(camYaw) * MathF.Cos(camPitch),
            MathF.Sin(camPitch),
            MathF.Cos(camYaw) * MathF.Cos(camPitch)
        );
        camera.Position = eyePos;
        camera.Target = eyePos + lookDir;
        camera.Up = Vector3.UnitY;
    }

    static void SmoothLookAtPoint(Vector3 eyePos, Vector3 targetPos, float dt, float speed = 6.5f)
    {
        Vector3 toTarget = targetPos - eyePos;
        float horizDist = MathF.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z);
        if (horizDist <= 0.05f) return;

        float targetYaw = MathF.Atan2(toTarget.X, toTarget.Z);
        float targetPitch = Math.Clamp(MathF.Atan2(toTarget.Y, horizDist), -0.65f, 0.65f);
        float yawDiff = MathF.Atan2(MathF.Sin(targetYaw - camYaw), MathF.Cos(targetYaw - camYaw));
        float lookSmooth = 1f - MathF.Exp(-speed * dt);
        camYaw += yawDiff * lookSmooth;
        camPitch += (targetPitch - camPitch) * lookSmooth;
    }

    static void UpdateFirstPersonLook(Vector3 eyePos)
    {
        Vector2 mouseDelta = Raylib.GetMouseDelta();
        const float sensitivity = 0.0022f;
        camYaw -= mouseDelta.X * sensitivity;
        camPitch = Math.Clamp(camPitch - mouseDelta.Y * sensitivity, -1.42f, 1.42f);
        ApplyCameraOrientation(eyePos);
    }

    static void StartDialogue(string speaker, string text, int stage, string[]? options = null,
                              bool talkingToNpc = false, string mode = "", bool leaveAfter = false)
    {
        currentSpeaker = speaker;
        fullText = text;
        visibleChars = 0f;
        lastSoundCharIndex = -1;
        dialogueStage = stage;
        currentOptions = options ?? [];
        selectedOption = 0;
        isTalking = true;
        isTalkingToNpc = talkingToNpc;
        subChoiceMode = mode;
        npcShouldLeaveAfterDialogue = leaveAfter;
        PlayDialogueVoice(speaker, text);
    }

    static void UpdateTypewriter(float deltaTime, float basePitch)
    {
        if (visibleChars >= fullText.Length) return;

        visibleChars = Math.Min(fullText.Length, visibleChars + TypingSpeed * deltaTime);
        int currentIndex = (int)visibleChars;

        if (currentIndex > lastSoundCharIndex && currentIndex % 2 == 0)
        {
            char c = fullText[Math.Min(currentIndex, fullText.Length - 1)];
            if (!hasActiveVoiceClip && !char.IsWhiteSpace(c))
            {
                Raylib.SetSoundPitch(voiceStaticSound, basePitch + (float)rng.NextDouble() * 0.1f);
                Raylib.PlaySound(voiceStaticSound);
            }
            lastSoundCharIndex = currentIndex;
        }
    }

    static void SpawnNpc(Vector3 start, Vector3 target, Color shirtColor, Color hairColor,
                         FearsBodySpec spec, FaceId faceId = FaceId.None)
    {
        npcIsGhostModel = false;
        npcPos = start;
        npcTarget = target;
        npcShirtColor = shirtColor;
        npcHairColor = hairColor;
        npcSpec = spec;
        npcFaceId = faceId;
        Vector3 d = target - start;
        npcYaw = d.LengthSquared() > 0.01f ? MathF.Atan2(d.X, d.Z) * 180f / MathF.PI : 180f;
        npcLeaving = false;
        npcShouldLeaveAfterDialogue = false;
        npcGazeWeight = 0f;
        npcHeadYaw = 0f;
        npcHeadPitch = 0f;
        npcHeadRoll = 0f;
        npcEyeGaze = Vector2.Zero;
        npcActive = true;
    }

    static void SpawnGlassGhost(Vector3 pos, float yawDeg = 180f)
    {
        GhostModel.EnsureLoaded();
        npcIsGhostModel = true;
        npcPos = pos;
        npcTarget = pos;
        npcYaw = yawDeg;
        npcLeaving = false;
        npcShouldLeaveAfterDialogue = false;
        npcSwing = 0f;
        npcGazeWeight = 0f;
        npcHeadYaw = 0f;
        npcHeadPitch = 0f;
        npcHeadRoll = 0f;
        npcEyeGaze = Vector2.Zero;
        npcActive = true;
    }

    static void TriggerStoryJumpscare(JumpscarePreset preset, int endingIdAfter)
    {
        EnsureJumpscareTarget();
        StopDialogueVoice();
        isTalking = false;
        isTalkingToNpc = false;
        dialogueStage = 0;
        subChoiceMode = "";
        npcActive = false;
        npcIsGhostModel = false;
        pendingJumpscareEnding = endingIdAfter;
        TriggerJumpscare(preset, 2.0f);
    }

    static void UpdateAndDrawStoryJumpscare(float dt)
    {
        float time = (float)Raylib.GetTime();
        EnsureJumpscareTarget();

        if (!jumpscareAudioTriggered)
        {
            PlayJumpscareAudioBurst();
            jumpscareAudioTriggered = true;
        }

        jumpscareTimer += dt;
        float dur = MathF.Max(0.5f, jumpscareDuration);
        float tNorm = Math.Clamp(jumpscareTimer / dur, 0f, 1f);
        float u = tNorm * dur - JsImpactTime(dur);

        ComputeJumpscarePose(activeJumpscarePreset, tNorm, true,
            out Vector3 camPos, out Vector3 camTarget, out Vector3 camUp, out float fov,
            out Vector3 ghostPos, out float ghostYaw, out float lunge);

        camera.Position = camPos;
        camera.Target = camTarget;
        camera.Up = camUp;
        camera.FovY = fov;

        bool strobeLit = jsLit;

        Raylib.BeginTextureMode(jsTarget);
        Raylib.ClearBackground(strobeLit ? JsColor(24, 27, 34) : JsColor(6, 7, 11));

        Raylib.BeginMode3D(camera);
        Props.DrawStore(lit: strobeLit, doorLocked: false, time, currentTime, phoneRinging: false, daylight: 0f);
        DrawStoreShelves(lit: strobeLit);
        Props.DrawWashroom(strobeLit, time, wcDoorOpen, wcStallDoorsOpen, -1, 0f, false, 0, 0f);
        Props.DrawStoreGlass(doorLocked: false, daylight: 0f);

        DrawJumpscareDebris(activeJumpscarePreset, u);
        GhostModel.Draw(ghostPos, ghostYaw, time, lunge,
            twitchIntensity: jsTwitch, screamAmount: jsScream, brightness: jsGhostBright);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        DrawJumpscareComposite(jsAber, jsZoom);
        DrawJumpscareScreenEffects(tNorm, time, true);

        Raylib.EndDrawing();

        if (jumpscareTimer >= jumpscareDuration)
        {
            jumpscareActive = false;
            int endId = pendingJumpscareEnding > 0 ? pendingJumpscareEnding : 1;
            pendingJumpscareEnding = -1;
            TriggerEnding(endId);
        }
    }

    static void UpdateAndDrawStoreGameplay(float dt)
    {
        if (jumpscareActive)
        {
            UpdateAndDrawStoryJumpscare(dt);
            return;
        }
        

        if (!gameEnded && !Raylib.IsSoundPlaying(ambientHumSound))
        {
            Raylib.PlaySound(ambientHumSound);
        }

        if (fadeAlpha > 0f)
        {
            fadeAlpha = Math.Max(0f, fadeAlpha - dt * 0.75f);
        }

        if (gameEnded)
        {
            UpdateAndDrawEndingScreen(dt);
            return;
        }

        bool anyCutscene = isWorkCutscene || isDawnCutscene || isReturnHomeCutscene || IsWcBusy;

        if (!isTalking && !anyCutscene)
        {
            float clockRate = storyStep switch
            {
                -1 => 0.35f,
                0 => 2.15f,
                7 => 0.65f,
                _ => 0.45f
            };

            float maxClockForStep = storyStep switch
            {
                -1 => 1138f,
                0 => 1470f,
                1 => 1515f,
                2 => 1558f,
                4 or 5 => 1580f,
                6 => 1605f,
                7 => 1619f,
                8 or 9 => 1735f,
                _ => 1750f
            };

            if (clockMinutes < maxClockForStep)
            {
                clockMinutes = Math.Min(maxClockForStep, clockMinutes + dt * clockRate);
                currentTime = FormatClock(clockMinutes);
            }

            if (storyStep == 0 && clockMinutes >= 1440f && currentObjective.Contains("Ca tối"))
            {
                currentObjective = "Đã quá 00:00 khuya - Phố vắng tanh, chuẩn bị đón khách đêm";
            }

            // Sau giờ hành chính/ca tối (00:00), các NPC sự kiện chính tiến thẳng tới quầy tương tác rồi rời đi
            if (!npcActive && IsShopperIdleOrLeaving())
            {
                if (storyStep == 0 && clockMinutes >= 1470f)
                {
                    currentTime = FormatClock(1470f);
                    currentObjective = "Tiếp vị khách đầu tiên lúc khuya vừa bước vào quầy (00:30)";
                    SpawnNpc(
                        new Vector3(0f, 0f, 12.0f),
                        new Vector3(-1.1f, 0f, -1.1f),
                        new Color(95, 115, 125, 255),
                        new Color(225, 225, 230, 255),
                        FearsBodySpec.OldManNpc,
                        FaceId.OldMan
                    );
                    storyStep = 1;
                }
                else if (storyStep == 1 && clockMinutes >= 1515f)
                {
                    currentTime = FormatClock(1515f);
                    currentObjective = "Tiếp vị khách tiếp theo vừa tiến tới quầy (01:15)";
                    SpawnNpc(
                        new Vector3(0f, 0f, 12.0f),
                        new Vector3(-1.1f, 0f, -1.1f),
                        new Color(215, 215, 225, 255),
                        new Color(40, 30, 25, 255),
                        FearsBodySpec.MaiNpc,
                        FaceId.Girl
                    );
                    storyStep = 2;
                }
                else if (storyStep == 2 && clockMinutes >= 1558f)
                {
                    clockMinutes = 1560f;
                    currentTime = FormatClock(clockMinutes);
                    currentObjective = "Nghe điện thoại bàn đang reo trên quầy (02:00)";
                    flickerTimer = 1.2f;
                    phoneRingTimer = 0.2f;
                    Raylib.PlaySound(voiceStaticSound);
                    storyStep = 4;
                }
            }

            // Nếu đã gửi vị trí, lúc 02:54 thực thể ma 3D lấp ló ngoài cửa kính trước giờ chốt cửa
            if (storyStep == 7 && !isDoorLocked && sharedLocation && !spoofAlertShown && clockMinutes >= 1614f)
            {
                spoofAlertShown = true;
                ShowTopNotificationBanner(
                    "Zalo",
                    "Dũng",
                    "Ê, tao chạy ngang đường Số 9 ghé qua chỗ mày nè, ra mở cửa tao coi.",
                    new Color(24, 119, 242, 255),
                    4.5f
                );
                SpawnGlassGhost(new Vector3(0.45f, 0f, 8.95f), 180f);
                StartDialogue(
                    "Tin nhắn (Dũng)",
                    "Điện thoại Tú rung lên tin nhắn từ một tài khoản lạ sao chép y hệt ảnh đại diện Dũng: 'Ê, tao chạy ngang đường Số 9 ghé qua chỗ mày nè, ra mở cửa tao coi.' (Ngoài cửa kính lấp ló một bóng ma đứng khuất trong sương)",
                    2
                );
            }
        }

        if (storyStep == 7 && !isDoorLocked)
        {
            heartbeatTimer -= dt;
            if (heartbeatTimer <= 0f)
            {
                Raylib.PlaySound(heartbeatSound);
                heartbeatTimer = 1.05f;
            }
        }

        if (flickerTimer > 0f)
        {
            flickerTimer -= dt;
            lightsOn = ((int)(flickerTimer * 18f) % 2 == 0);
            if (flickerTimer <= 0f)
            {
                lightsOn = true;
            }
        }

        if (storyStep == 4 && !isTalking)
        {
            phoneRingTimer -= dt;
            if (phoneRingTimer <= 0f)
            {
                Raylib.PlaySound(phoneRingSound);
                phoneRingTimer = 2.6f;
            }
        }

        if (storyStep == 5 && eventDelayTimer > 0f)
        {
            eventDelayTimer -= dt;
            if (eventDelayTimer <= 0f)
            {
                clockMinutes = 1580f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "Tiếp vị khách vừa bước tới quầy (02:20)";
                SpawnNpc(
                    new Vector3(0f, 0f, 12.0f),
                    new Vector3(-1.1f, 0f, -1.1f),
                    new Color(45, 45, 55, 255),
                    new Color(30, 30, 35, 255),
                    FearsBodySpec.HungNpc,
                    FaceId.StrangeMan
                );
                storyStep = 6;
            }
        }

        if (npcActive)
        {
            float speed = npcLeaving ? 2.4f : 2.1f;
            Vector3 dir = npcTarget - npcPos;
            float dist = dir.Length();
            npcSwing = dist > 0.05f ? 28f : 0f;

            if (dist > 0.05f)
            {
                npcPos += Vector3.Normalize(dir) * Math.Min(dist, speed * dt);
                npcYaw = MathF.Atan2(dir.X, dir.Z) * 180f / MathF.PI;
                npcWalkPhase += dt * 9f;
            }
            else if (npcLeaving)
            {
                npcActive = false;
                npcIsGhostModel = false;
                npcLeaving = false;

                if (storyStep == -1)
                {
                    clockMinutes = 1140f;
                    currentTime = "19:00";
                    currentObjective = "Đọc tờ quy tắc & Sổ giao ca trên quầy thu ngân";
                    storyStep = 0;
                }
                else if (storyStep == 1)
                {
                    currentObjective = "Trực quầy & kiểm tra kệ hàng (00:30 - 01:15)";
                }
                else if (storyStep == 2)
                {
                    currentObjective = "Trực quầy & kiểm tra kệ hàng trước 02:00";
                }
                else if (storyStep == 6)
                {
                    clockMinutes = 1606f;
                    currentTime = FormatClock(clockMinutes);
                    currentObjective = "Ra cửa chính bấm [F] để khóa chốt trước 03:00";
                    storyStep = 7;
                }
                else if (storyStep == 9)
                {
                    StartDawnCutscene();
                }
            }
        }

        activeInteractId = "";

        if (isWorkCutscene)
        {
            Raylib.GetMouseDelta();
            UpdateWorkCutscene(dt);
        }
        else if (isDawnCutscene)
        {
            Raylib.GetMouseDelta();
            UpdateDawnCutscene(dt);
        }
        else if (isReturnHomeCutscene)
        {
            Raylib.GetMouseDelta();
            UpdateReturnHomeCutscene(dt);
        }
        else if (IsWcBusy)
        {
            Raylib.GetMouseDelta();
            UpdateWcActions(dt);
        }
        else if (!isTalking)
        {
            Vector3 oldPlayerPos = playerPos;

            Vector3 forwardFlat = new(MathF.Sin(camYaw), 0f, MathF.Cos(camYaw));
            Vector3 rightFlat = new(-MathF.Cos(camYaw), 0f, MathF.Sin(camYaw));

            Vector3 moveInput = Vector3.Zero;
            if (Raylib.IsKeyDown(KeyboardKey.W)) moveInput += forwardFlat;
            if (Raylib.IsKeyDown(KeyboardKey.S)) moveInput -= forwardFlat;
            if (Raylib.IsKeyDown(KeyboardKey.D)) moveInput += rightFlat;
            if (Raylib.IsKeyDown(KeyboardKey.A)) moveInput -= rightFlat;

            bool isSprinting = moveInput.LengthSquared() > 0.001f &&
                               (Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift));

            if (moveInput.LengthSquared() > 0.001f)
            {
                float moveSpeed = isSprinting ? 4.55f : 3.1f;
                playerPos += Vector3.Normalize(moveInput) * moveSpeed * dt;
            }

            float maxZ = isDoorLocked ? 6.9f : 13.5f;
            if (Math.Abs(playerPos.X) > 0.85f && playerPos.Z > 6.9f && oldPlayerPos.Z <= 6.9f)
            {
                playerPos.Z = 6.9f;
            }

            bool inWcArea = playerPos.X < -10.25f ||
                            (playerPos.X < -9.75f && wcDoorOpen && playerPos.Z >= -6.45f && playerPos.Z <= -5.55f);

            if (inWcArea)
            {
                playerPos.Z = Math.Clamp(playerPos.Z, -6.50f, -1.75f);
                ApplyWcCollisionAndBounds();
            }
            else
            {
                playerPos.X = Math.Clamp(playerPos.X, -9.8f, 10.6f);
                playerPos.Z = Math.Clamp(playerPos.Z, -18.0f, maxZ);

                if (playerPos.Z > -7.1f && playerPos.X > 9.8f)
                {
                    playerPos.X = 9.8f;
                }
            }

            if (playerPos.Z < -11.0f && playerPos.X < 1.95f)
            {
                playerPos.X = 1.95f;
            }

            BoundingBox[] architecturalWalls =
            [
                new BoundingBox(new Vector3(-10.5f, 0f, -7.45f), new Vector3(8.38f, 3.8f, -6.95f)),
                new BoundingBox(new Vector3(9.62f, 0f, -7.45f), new Vector3(10.5f, 3.8f, -6.95f)),
                new BoundingBox(new Vector3(6.00f, 0f, -11.4f), new Vector3(6.40f, 3.8f, -7.00f)),
                new BoundingBox(new Vector3(1.45f, 0f, -11.45f), new Vector3(8.48f, 3.8f, -10.95f)),
                new BoundingBox(new Vector3(9.52f, 0f, -11.45f), new Vector3(11.15f, 3.8f, -10.95f)),
                new BoundingBox(new Vector3(3.75f, 0f, -16.45f), new Vector3(4.65f, 1.35f, -14.45f)),
                new BoundingBox(new Vector3(2.45f, 0f, -17.35f), new Vector3(2.65f, 2.6f, -17.15f)),
                new BoundingBox(new Vector3(5.75f, 0f, -17.35f), new Vector3(5.95f, 2.6f, -17.15f)),
                new BoundingBox(new Vector3(2.45f, 0f, -14.15f), new Vector3(2.65f, 2.6f, -13.95f)),
                new BoundingBox(new Vector3(5.75f, 0f, -14.15f), new Vector3(5.95f, 2.6f, -13.95f)),
                new BoundingBox(new Vector3(9.95f, 0f, -15.95f), new Vector3(10.80f, 1.2f, -12.20f))
            ];

            foreach (var wall in architecturalWalls)
            {
                float cx = Math.Clamp(playerPos.X, wall.Min.X, wall.Max.X);
                float cz = Math.Clamp(playerPos.Z, wall.Min.Z, wall.Max.Z);
                float dx = playerPos.X - cx;
                float dz = playerPos.Z - cz;
                float dSq = dx * dx + dz * dz;
                const float pRad = 0.42f;
                if (dSq < pRad * pRad)
                {
                    float d = MathF.Sqrt(dSq);
                    if (d > 0.0001f)
                    {
                        playerPos.X += (dx / d) * (pRad - d);
                        playerPos.Z += (dz / d) * (pRad - d);
                    }
                }
            }

            var storeColliders = solidObjects
                .Where(b => b.Min.X > -3.4f && b.Max.X < 1.5f && b.Min.Z > -6.0f && b.Max.Z < 0.0f)
                .Concat(customModelColliders);

            foreach (var box in storeColliders)
            {
                float closestX = Math.Clamp(playerPos.X, box.Min.X, box.Max.X);
                float closestZ = Math.Clamp(playerPos.Z, box.Min.Z, box.Max.Z);
                float dx = playerPos.X - closestX;
                float dz = playerPos.Z - closestZ;
                float distSq = dx * dx + dz * dz;
                const float radius = 0.42f;
                if (distSq < radius * radius)
                {
                    float d = MathF.Sqrt(distSq);
                    if (d > 0.0001f)
                    {
                        float push = radius - d;
                        playerPos.X += (dx / d) * push;
                        playerPos.Z += (dz / d) * push;
                    }
                }
            }

            float movedDist = Vector2.Distance(
                new Vector2(oldPlayerPos.X, oldPlayerPos.Z),
                new Vector2(playerPos.X, playerPos.Z)
            );

            float currentStepInterval = isSprinting ? 0.33f : StepInterval;

            if (movedDist > 0.001f)
            {
                stepTimer += dt;
                float bobFreq = isSprinting ? 16.5f : 12.0f;
                float bobAmp = isSprinting ? 0.032f : 0.022f;
                walkBobTimer += dt * bobFreq;
                float targetBob = MathF.Sin(walkBobTimer) * bobAmp;
                currentBobOffset += (targetBob - currentBobOffset) * Math.Min(1f, dt * 16f);

                if (stepTimer >= currentStepInterval)
                {
                    stepTimer = 0f;
                    Raylib.SetSoundPitch(footstepSound, (isSprinting ? 1.02f : 0.92f) + (float)rng.NextDouble() * 0.16f);
                    Raylib.PlaySound(footstepSound);
                }
            }
            else
            {
                stepTimer = currentStepInterval * 0.8f;
                currentBobOffset += (0f - currentBobOffset) * Math.Min(1f, dt * 14f);
            }

            UpdateFirstPersonLook(new Vector3(playerPos.X, 1.7f + currentBobOffset, playerPos.Z));

            // Nếu người chơi mở cửa hoặc bước ra ngoài đường trong lúc chưa chốt cửa
            if (storyStep >= 1 && storyStep <= 7 && !isDoorLocked && playerPos.Z > 10.7f)
            {
                if (spoofAlertShown)
                    TriggerStoryJumpscare(JumpscarePreset.FrontDoorRush, 1);
                else
                    TriggerEnding(3);
                return;
            }

            if (storyStep == 10 && playerPos.Z > 10.0f)
            {
                TriggerEnding(2);
                return;
            }

            UpdateShiftWork(dt);
            Vector3 lookDir = Vector3.Normalize(camera.Target - camera.Position);
            Ray eyeRay = new(camera.Position, lookDir);

            RayCollision paperHit = Raylib.GetRayCollisionBox(eyeRay, rulesPaperBox);
            RayCollision logbookHit = Raylib.GetRayCollisionBox(eyeRay, logbookBox);
            RayCollision phoneHit = Raylib.GetRayCollisionBox(eyeRay, deskPhoneBox);
            RayCollision doorHit = Raylib.GetRayCollisionBox(eyeRay, frontDoorBox);
            RayCollision posterHit = Raylib.GetRayCollisionBox(eyeRay, posterBox);
            RayCollision bikeHit = Raylib.GetRayCollisionBox(eyeRay, backyardMotorbikeBox);
            RayCollision gateHit = Raylib.GetRayCollisionBox(eyeRay, backyardAlleyGateBox);

            float npcHeight = npcIsGhostModel ? 1.88f : 1.85f * npcSpec.HeightScale;
            BoundingBox npcBox = new(npcPos - new Vector3(0.45f, 0f, 0.45f), npcPos + new Vector3(0.45f, npcHeight, 0.45f));
            RayCollision npcHit = npcActive ? Raylib.GetRayCollisionBox(eyeRay, npcBox) : new RayCollision { Hit = false };

            if (skullPromptKey == "")
            {
                TryDetectWcInteraction(eyeRay);
            }

            if (skullPromptKey == "")
            {
                float maxNpcDist = npcIsGhostModel ? 3.8f : 3.2f;
                if (npcHit.Hit && npcHit.Distance <= maxNpcDist && !npcLeaving && storyStep != 7 && !IsRayBlockedByWall(eyeRay, npcHit.Distance))
                {
                    activeInteractId = "npc";
                    skullPromptKey = "F";
                    skullPromptText = npcIsGhostModel ? "NHÌN RA NGOÀI CỬA KÍNH" : "NÓI CHUYỆN";
                }
                else if (posterHit.Hit && posterHit.Distance <= 3.0f && !IsRayBlockedByWall(eyeRay, posterHit.Distance))
                {
                    activeInteractId = "poster";
                    skullPromptKey = "F";
                    skullPromptText = "XEM TỜ ÁP PHÍCH TRÊN CỘT ĐÈN";
                }
                else if (paperHit.Hit && paperHit.Distance <= 2.6f && !IsRayBlockedByWall(eyeRay, paperHit.Distance))
                {
                    activeInteractId = "rules";
                    skullPromptKey = "F";
                    skullPromptText = "ĐỌC TỜ QUY TẮC";
                }
                else if (logbookHit.Hit && logbookHit.Distance <= 2.6f && !IsRayBlockedByWall(eyeRay, logbookHit.Distance))
                {
                    activeInteractId = "logbook";
                    skullPromptKey = "F";
                    skullPromptText = "LẬT XEM SỔ GIAO CA";
                }
                else if (phoneHit.Hit && phoneHit.Distance <= 2.6f && storyStep == 4 && !IsRayBlockedByWall(eyeRay, phoneHit.Distance))
                {
                    activeInteractId = "phone";
                    skullPromptKey = "F";
                    skullPromptText = "NGHE ĐIỆN THOẠI";
                }
                else if (doorHit.Hit && doorHit.Distance <= 2.8f && storyStep == 7 && !isDoorLocked && !IsRayBlockedByWall(eyeRay, doorHit.Distance))
                {
                    activeInteractId = "door";
                    skullPromptKey = "F";
                    skullPromptText = "KHÓA CHỐT CỬA CHÍNH";
                }
                else if (bikeHit.Hit && bikeHit.Distance <= 3.0f && !IsRayBlockedByWall(eyeRay, bikeHit.Distance))
                {
                    activeInteractId = "motorbike";
                    skullPromptKey = "F";
                    skullPromptText = IsMorningAfterShift
                        ? "LÊN XE CHẠY VỀ NHÀ"
                        : "KIỂM TRA XE MÁY CỦA TÚ";
                }
                else if (gateHit.Hit && gateHit.Distance <= 3.0f && !IsRayBlockedByWall(eyeRay, gateHit.Distance))
                {
                    activeInteractId = "alley_gate";
                    skullPromptKey = "F";
                    skullPromptText = "KIỂM TRA CỔNG SẮT RA HẺM";
                }
            }

            if (activeInteractId != "" && Raylib.IsKeyPressed(KeyboardKey.F))
            {
                HandleStoreInteraction();
            }
        }
        else
        {
            HandleStoreDialogueUpdate(dt);
        }

        float targetFov = anyCutscene ? 57.0f : ((isTalking && isTalkingToNpc && npcActive) ? TalkNpcFov : NormalFov);
        float zoomSmooth = 1f - MathF.Exp(-6.0f * dt);
        camera.FovY += (targetFov - camera.FovY) * zoomSmooth;

        if (npcActive && !npcIsGhostModel)
        {
            float targetWeight = (npcLeaving || anyCutscene) ? 0f : 1f;
            npcGazeWeight += (targetWeight - npcGazeWeight) * (1f - MathF.Exp(-5.5f * dt));

            var targetGaze = Props.ComputeGazeTarget(
                npcPos + new Vector3(0f, 1.62f * npcSpec.HeightScale, 0f),
                npcYaw,
                camera.Position,
                npcGazeWeight
            );

            float headSmooth = 1f - MathF.Exp(-7.0f * dt);
            npcHeadYaw += (targetGaze.HeadYaw - npcHeadYaw) * headSmooth;
            npcHeadPitch += (targetGaze.HeadPitch - npcHeadPitch) * headSmooth;
            npcHeadRoll += (targetGaze.HeadRoll - npcHeadRoll) * headSmooth;
            npcEyeGaze = Vector2.Lerp(npcEyeGaze, targetGaze.EyeGaze, headSmooth);
        }

        DrawStoreFrame(dt, anyCutscene);
    }

    static void HandleStoreInteraction()
    {
        if (TryExecuteWcInteraction()) return;

        if (activeInteractId == "motorbike")
        {
            if (IsMorningAfterShift)
            {
                StartDialogue(
                    "Tú",
                    "Trời đã sáng rõ, ca đêm cũng khép lại bình an. Mình tra chìa khóa, nổ máy xe chạy thẳng về nhà nghỉ ngơi thôi.",
                    2,
                    mode: "return_home_ride"
                );
            }
            else
            {
                StartDialogue(
                    "Tú",
                    "Chiếc xe số màu đỏ của mình dựng gọn dưới mái che sân sau. Chìa khóa vẫn cất trong túi quần, chỉ mong trực yên ổn tới 5 giờ sáng để nổ máy chạy thẳng về nhà.",
                    2
                );
            }
        }
        else if (activeInteractId == "alley_gate")
        {
            StartDialogue(
                "Tú",
                "Cổng sắt thông ra con hẻm nhỏ phía sau cửa hàng đã được móc khóa xích chắc chắn. Qua khe song sắt chỉ thấy con hẻm tối om hắt ánh đèn vàng vọt.",
                2
            );
        }
        else if (activeInteractId == "poster")
        {
            readPoster = true;
            StartDialogue(
                "Áp phích ngã tư",
                "THÔNG BÁO TÌM NHÂN CHỨNG: Khoảng 02:30 rạng sáng tuần trước, tại ngã tư cuối đường Số 9 xảy ra vụ tai nạn giao thông nghiêm trọng làm một cụ ông 72 tuổi tử vong tại chỗ...",
                2
            );
        }
        else if (activeInteractId == "rules")
        {
            StartDialogue(
                "Tờ quy tắc ca trực",
                "1. Chào hỏi khách đàng hoàng trước khi tính tiền. 2. Tuyệt đối không bán hàng cho ■■■■■■. 3. Sau 00:00 khuya, đúng 03:00 khóa chặt cửa chính, không mở cho bất kỳ ai.",
                1,
                [
                    "Lật mép giấy xem dòng chữ viết tay bên lề",
                    "Đặt tờ quy tắc xuống"
                ],
                mode: "rules_margin"
            );
        }
        else if (activeInteractId == "logbook")
        {
            readLogbook = true;
            StartDialogue(
                "Sổ giao ca",
                "Trang cuối ghi nét bút run rẩy của ca trước (Trần Văn Hùng): 'Đừng tin quản lý. Bài đăng tuyển dụng trên mạng là mồi nhử — hễ ai gửi định vị đường Số 9 cho bạn bè, thứ ở ngã tư sẽ đọc lén được tên người đó và giả danh gọi mở cửa lúc 3 giờ sáng...'",
                2
            );
        }
        else if (activeInteractId == "phone")
        {
            StartDialogue(
                "Điện thoại bàn",
                "*Rè rè...* (Đầu dây bên kia không có ai nói, chỉ có tiếng bước chân cộp... cộp... vang lên đều đặn trong tiếng mưa rào)",
                2,
                mode: "desk_phone"
            );
        }
        else if (activeInteractId == "door")
        {
            isDoorLocked = true;
            Raylib.PlaySound(knockSound);
            flickerTimer = 0.65f;
            clockMinutes = 1620f;
            currentTime = FormatClock(clockMinutes);
            currentObjective = "03:00 - Có bóng ma áp sát ngoài cửa kính! Ra kiểm tra cửa chính";
            SpawnGlassGhost(new Vector3(0f, 0f, 8.15f), 180f);
            storyStep = 8;
            StartDialogue(
                "Tú",
                "Mình vừa gạt chốt khóa cửa đúng 03:00 thì một bóng người bê bết máu lao sầm tới áp sát mặt kính, đập dồn dập: CỐC! CỐC! CỐC!",
                2
            );
        }
        else if (activeInteractId == "npc")
        {
            if (storyStep == -1)
            {
                StartDialogue(
                    "Quản lý",
                    "Tới đúng 7 giờ tối rồi hả Tú? Buổi tối khách ra vào hơi đông, em cứ đứng quầy tính tiền, châm kệ với lau dọn cho sạch. Qua nửa đêm vắng khách thì nhớ đọc kỹ tờ quy tắc và điều số 3 nha!",
                    2,
                    talkingToNpc: true,
                    leaveAfter: true
                );
            }
            else if (storyStep == 1)
            {
                StartDialogue(
                    "Ông cụ",
                    "Chào cháu, lấy cho ông hộp sữa tươi với ổ bánh mì nhé. Ông ở ngay con hẻm đối diện, tối nay khó ngủ nên đi bộ ra mua chút đồ ăn sáng.",
                    1,
                    [
                        "Dạ chào ông, của ông hết 25 nghìn ạ. Ông cầm tiền thừa nhé.",
                        "Con hẻm đối diện tối lắm, ông đi bộ một mình cẩn thận xe cộ nha."
                    ],
                    talkingToNpc: true
                );
            }
            else if (storyStep == 2)
            {
                StartDialogue(
                    "Chị Mai",
                    "Em ơi tính tiền giúp chị ly mì với chai nước suối nhé. Khu này dạo này vắng quá, nhất là từ sau vụ tai nạn ngoài ngã tư tuần trước...",
                    1,
                    [
                        "Dạ chào chị, hết 20 nghìn ạ. Tuần trước ngoài ngã tư có vụ gì vậy chị?",
                        "Tính tiền xong rồi đó chị. Khuya rồi chị về cẩn thận."
                    ],
                    talkingToNpc: true
                );
            }
            else if (storyStep == 6)
            {
                StartDialogue(
                    "Anh Hùng",
                    "(Trên ngực áo sơ mi của anh ta vẫn đeo bảng tên nhân viên ca trước: TRẦN VĂN HÙNG) Lấy cho tôi hộp sữa tươi với ổ bánh mì... Tôi ở ngay con hẻm đối diện...",
                    1,
                    [
                        "Anh là Trần Văn Hùng viết trong sổ giao ca? Sao anh nói y hệt ông cụ lúc nãy?",
                        "Quán sắp đóng cửa kiểm kê rồi, anh thông cảm..."
                    ],
                    talkingToNpc: true
                );
            }
            else if (storyStep == 8)
            {
                if (sharedLocation)
                {
                    StartDialogue(
                        "Bóng ma ngoài kính",
                        "Thực thể ngoài cửa kính nghiêng đầu gãy gập, từ cổ họng phát ra giọng nhại y hệt thằng Dũng: 'Ê Tú! Tao Dũng nè, mở cửa nhanh đi ngoài này lạnh teo rồi!'",
                        1,
                        [
                            "Mở chốt cửa cho 'Dũng' vào",
                            "Rút điện thoại gọi thẳng cho số thằng Dũng kiểm tra",
                            "Lùi lại, nhất quyết không mở cửa"
                        ],
                        talkingToNpc: true,
                        mode: "0300_shared"
                    );
                }
                else
                {
                    StartDialogue(
                        "Bóng ma ngoài kính",
                        "Thực thể áp sát mặt kính lạnh toát, cất giọng khàn đục: 'Em ơi mở cửa nhanh giúp với... Lúc nãy làm rơi chùm chìa khóa ngay trước quầy thu ngân!'",
                        1,
                        [
                            "Mở chốt cửa cho vào tìm chìa khóa",
                            "Trên quầy không có chìa khóa nào cả, không mở cửa"
                        ],
                        talkingToNpc: true,
                        mode: "0300_normal"
                    );
                }
            }
            else if (storyStep == 10)
            {
                if (recordingOn)
                {
                    StartDialogue(
                        "Quản lý",
                        "Làm tốt lắm Tú, đây là phong bì 500 nghìn tiền công đêm nay. Em ký xác nhận vào tờ giấy này rồi tối mai 7 giờ tối lại qua trực tiếp nhé.",
                        1,
                        [
                            "Cầm tiền và ký tên vào tờ giấy",
                            "Từ chối ký, giơ điện thoại đang ghi âm lên vạch trần gã Quản lý"
                        ],
                        talkingToNpc: true,
                        mode: "0500_ending"
                    );
                }
                else
                {
                    StartDialogue(
                        "Quản lý",
                        "Làm tốt lắm Tú, đây là phong bì 500 nghìn tiền công đêm nay của em. Giờ em về nghỉ ngơi đi, tối mai 7 giờ tối lại qua trực tiếp nhé.",
                        2,
                        talkingToNpc: true
                    );
                }
            }
        }
    }

    static void HandleStoreDialogueUpdate(float dt)
    {
        Raylib.GetMouseDelta();
        currentBobOffset += (0f - currentBobOffset) * (1f - MathF.Exp(-10f * dt));
        Vector3 eyePos = new(playerPos.X, 1.7f + currentBobOffset, playerPos.Z);

        if (isTalkingToNpc && npcActive)
        {
            Vector3 npcFacePos = npcIsGhostModel
                ? GhostModel.GetLiveFaceWorld(npcPos, npcYaw, (float)Raylib.GetTime(), storyStep >= 9 ? 0.35f : 0.08f, 0.5f)
                : npcPos + new Vector3(0f, 1.56f * npcSpec.HeightScale, 0f);
            SmoothLookAtPoint(eyePos, npcFacePos, dt, 7.5f);
        }

        ApplyCameraOrientation(eyePos);

        UpdateTypewriter(dt, GetSpeakerPitch(currentSpeaker));
        bool textFinished = (int)visibleChars >= fullText.Length;

        if (!textFinished && (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.F) || IsConfirmKeyPressed()))
        {
            visibleChars = fullText.Length;
        }
        else if (textFinished)
        {
            if (dialogueStage == 1 && currentOptions.Length > 0)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
                {
                    selectedOption = (selectedOption + 1) % currentOptions.Length;
                    Raylib.SetSoundPitch(voiceStaticSound, 1.3f);
                    Raylib.PlaySound(voiceStaticSound);
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
                {
                    selectedOption = (selectedOption - 1 + currentOptions.Length) % currentOptions.Length;
                    Raylib.SetSoundPitch(voiceStaticSound, 1.3f);
                    Raylib.PlaySound(voiceStaticSound);
                }
                else if (IsConfirmKeyPressed())
                {
                    if (subChoiceMode == "record_choice")
                    {
                        recordingOn = (selectedOption == 0);
                        subChoiceMode = "";
                        isTalking = false;
                        StopDialogueVoice();
                        dialogueStage = 0;
                        Raylib.GetMouseDelta();
                    }
                    else if (subChoiceMode == "rude_shopper")
                    {
                        subChoiceMode = "";
                        isTalking = false;
                        StopDialogueVoice();
                        dialogueStage = 0;
                        Raylib.GetMouseDelta();
                        ResolveRudeShopperChoice(selectedOption == 0);
                    }
                    else if (subChoiceMode == "rules_margin")
                    {
                        subChoiceMode = "";
                        if (selectedOption == 0)
                        {
                            readMargin = true;
                            StartDialogue(
                                "Chữ viết tay bên lề",
                                "Dòng chữ chì mờ mờ ghi ở mép dưới: 'Sóng điện thoại trong tiệm này nối thẳng ra ngã tư. Nó đọc lén được tin nhắn gửi vị trí — 3 giờ sáng nó sẽ nhại đúng giọng người bạn vừa nhắn...'",
                                2,
                                mode: "rules_done"
                            );
                        }
                        else
                        {
                            isTalking = false;
                            StopDialogueVoice();
                            dialogueStage = 0;
                            Raylib.GetMouseDelta();

                            if (storyStep == 0)
                            {
                                currentObjective = "Ca tối (19:00 - 00:00): Đón khách, quét tính tiền & sắp xếp kệ";
                            }
                        }
                    }
                    else if (storyStep == 1)
                    {
                        if (selectedOption == 0) greetedCount++;
                        string reply = selectedOption == 0
                            ? "Cảm ơn cháu lễ phép nhé. Ông gửi tiền, cháu mới đi làm ca đêm cố gắng giữ sức khỏe nha."
                            : "Ông quen đường rồi cháu ơi. Cảm ơn cháu, ông gửi tiền sữa với bánh mì nhé.";
                        StartDialogue("Ông cụ", reply, 2, talkingToNpc: true, leaveAfter: true);
                    }
                    else if (storyStep == 2)
                    {
                        if (selectedOption == 0)
                        {
                            greetedCount++;
                            talkedMai = true;
                            StartDialogue(
                                "Chị Mai",
                                "Tội nghiệp ông cụ tóc bạc ở con hẻm đối diện quán mình nè, đi bộ buổi tối bị xe tải quẹt mất tuần trước rồi, nhà đóng cửa im ỉm mấy nay...",
                                2,
                                talkingToNpc: true,
                                leaveAfter: true
                            );
                        }
                        else
                        {
                            StartDialogue(
                                "Chị Mai",
                                "Ừ, cảm ơn em nha. Chị về trước đây, trực đêm cẩn thận đó.",
                                2,
                                talkingToNpc: true,
                                leaveAfter: true
                            );
                        }
                    }
                    else if (storyStep == 6)
                    {
                        noticedAnomaly = (selectedOption == 0);
                        nameTag = (selectedOption == 0);
                        string reply = selectedOption == 0
                            ? "Ông cụ đó mất tuần trước rồi... Cậu còn mở cửa bán hàng thì trước 3 giờ nhớ khóa chặt chốt, đừng để thứ ngoài ngã tư lọt vào."
                            : "Không cần mua gì đâu. Nhớ khóa chặt cửa lúc 3 giờ, đừng mở cho bất kỳ ai gõ kính...";
                        StartDialogue("Anh Hùng", reply, 2, talkingToNpc: true, leaveAfter: true);
                    }
                    else if (storyStep == 8)
                    {
                        if (subChoiceMode == "0300_shared")
                        {
                            if (selectedOption == 0)
                            {
                                isDoorLocked = false;
                                TriggerStoryJumpscare(JumpscarePreset.FrontDoorRush, 1);
                                return;
                            }
                            else if (selectedOption == 1)
                            {
                                subChoiceMode = "";
                                calledRealDung = true;
                                flickerTimer = 0.75f;
                                storyStep = 9;
                                StartDialogue(
                                    "Điện thoại (Dũng)",
                                    "Đầu dây bên kia giọng Dũng ngái ngủ: 'Alo? Tao nhắn hỏi thăm hồi 10 giờ không thấy rep nên ngủ luôn rồi, qua đường Số 9 hồi nào!' Ngoài kính, bóng ma đứng khựng lại, giật cổ rồi gầm gừ: 'Mắt tinh tú đấy... Tiếc thật.'",
                                    2,
                                    talkingToNpc: true,
                                    leaveAfter: true
                                );
                            }
                            else
                            {
                                subChoiceMode = "";
                                flickerTimer = 0.65f;
                                storyStep = 9;
                                StartDialogue(
                                    "Bóng ma ngoài kính",
                                    "Nó đứng khựng lại sau lớp kính, giọng thằng Dũng biến mất, đổi sang tiếng gầm khàn đặc: 'Mắt tinh tú đấy... Tiếc thật.'",
                                    2,
                                    talkingToNpc: true,
                                    leaveAfter: true
                                );
                            }
                        }
                        else
                        {
                            if (selectedOption == 0)
                            {
                                isDoorLocked = false;
                                TriggerStoryJumpscare(JumpscarePreset.FrontDoorRush, 1);
                                return;
                            }
                            else
                            {
                                subChoiceMode = "";
                                flickerTimer = 0.65f;
                                storyStep = 9;
                                StartDialogue(
                                    "Bóng ma ngoài kính",
                                    "Nó đứng khựng lại sau lớp kính, cổ giật mạnh rồi cất tiếng gầm khàn đặc: 'Mắt tinh tú đấy... Tiếc thật.'",
                                    2,
                                    talkingToNpc: true,
                                    leaveAfter: true
                                );
                            }
                        }
                    }
                    else if (storyStep == 10 && subChoiceMode == "0500_ending")
                    {
                        subChoiceMode = "";
                        if (selectedOption == 0)
                        {
                            isTalking = false;
                            StopDialogueVoice();
                            isTalkingToNpc = false;
                            dialogueStage = 0;
                            isDoorLocked = false;
                            currentObjective = "Ra sân sau lấy xe máy hoặc bước ra cửa chính để về nhà";
                            Raylib.GetMouseDelta();
                        }
                        else
                        {
                            if (GetClueCount() >= 3 && greetedCount >= 1)
                                TriggerEnding(5);
                            else
                                TriggerEnding(4);
                            return;
                        }
                    }
                    else
                    {
                        subChoiceMode = "";
                        isTalking = false;
                        StopDialogueVoice();
                        isTalkingToNpc = false;
                        dialogueStage = 0;
                        Raylib.GetMouseDelta();
                    }
                }
            }
            else if (dialogueStage == 2 || currentOptions.Length == 0)
            {
                if (IsConfirmKeyPressed() || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.F))
                {
                    string finishedMode = subChoiceMode;
                    bool shouldNpcLeave = npcShouldLeaveAfterDialogue;

                    subChoiceMode = "";
                    npcShouldLeaveAfterDialogue = false;
                    isTalking = false;
                    StopDialogueVoice();
                    isTalkingToNpc = false;
                    dialogueStage = 0;
                    Raylib.GetMouseDelta();

                    if (storyStep == 0 && finishedMode == "rules_done")
                    {
                        currentObjective = "Ca tối (19:00 - 00:00): Đón khách, quét tính tiền & sắp xếp kệ";
                    }
                    else if (finishedMode == "return_home_ride")
                    {
                        StartReturnHomeCutscene();
                    }
                    else if (shouldNpcLeave && npcActive)
                    {
                        npcTarget = npcIsGhostModel ? new Vector3(0f, 0f, 15.5f) : new Vector3(0f, 0f, 14.0f);
                        npcLeaving = true;
                    }
                    else if (storyStep == 4 && finishedMode == "desk_phone")
                    {
                        currentObjective = "Chờ vị khách tiếp theo...";
                        eventDelayTimer = 3.5f;
                        storyStep = 5;
                    }
                    else if (storyStep == 10)
                    {
                        isDoorLocked = false;
                        currentObjective = "Ra sân sau lấy xe máy hoặc bước ra cửa chính để về nhà";
                    }
                }
            }
        }
    }

    static void DrawStoreFrame(float dt, bool anyCutscene)
    {
        float currentDaylight = isDawnCutscene ? dawnProgress : (storyStep >= 10 ? 1.0f : 0.0f);
        Props.UpdateWashroomMirror(camera, playerPos, camYaw, lightsOn, (float)Raylib.GetTime(),
                                   wcDoorOpen, wcStallDoorsOpen, activeSinkIndex, washHandsTimer,
                                   isPeeing, activePeeStall, peeTimer);

        Raylib.BeginDrawing();
        Color nightSky = new(10, 12, 18, 255);
        Color dawnSky = new(135, 168, 205, 255);
        Color skyColor = LerpColor(nightSky, dawnSky, currentDaylight);

        RenderFoggyScene(() =>
        {
            Props.DrawStore(lightsOn, isDoorLocked, (float)Raylib.GetTime(), currentTime, storyStep == 4 && !isTalking, currentDaylight);
            DrawStoreShelves(lightsOn);
            DrawShiftWork3D(lightsOn);
            DrawShopper3D();
            Props.DrawWashroom(lightsOn, (float)Raylib.GetTime(), wcDoorOpen, wcStallDoorsOpen,
                               activeSinkIndex, washHandsTimer, isPeeing, activePeeStall, peeTimer);
            Props.DrawFirstPersonWashHands(camera, washHandsTimer);

            if (npcActive)
            {
                if (npcIsGhostModel)
                {
                    float tNow = (float)Raylib.GetTime();
                    bool ghostRevealed = storyStep >= 9;
                    GhostModel.Draw(
                        npcPos,
                        npcYaw,
                        tNow,
                        lungeProgress: ghostRevealed ? 0.35f : 0.08f,
                        twitchIntensity: ghostRevealed ? 1.15f : 0.35f,
                        screamAmount: ghostRevealed ? 0.65f : 0.05f,
                        brightness: lightsOn ? 0.92f : 0.32f
                    );
                }
                else
                {
                    Props.DrawFearsCharacter(npcPos, npcYaw, npcShirtColor, npcHairColor, npcSpec, npcWalkPhase, npcSwing,
                                             npcHeadYaw, npcHeadPitch, npcHeadRoll, npcEyeGaze, npcFaceId);
                }
            }
            Props.DrawStoreGlass(isDoorLocked, currentDaylight);
        }, skyColor, LerpColor(new Color(24, 0, 0, 255), new Color(14, 0, 0, 255), currentDaylight).R * 0.1f);

        DrawVignette(lightsOn ? (int)(10 * (1f - currentDaylight)) : 75);

        if (isWorkCutscene)
        {
            int barH = 74;
            Raylib.DrawRectangle(0, 0, ScreenWidth, barH, Color.Black);
            Raylib.DrawRectangle(0, ScreenHeight - barH, ScreenWidth, barH, Color.Black);

            string caption = workCutsceneTimer < 2.6f
                ? "18:46 - Quầy đảo bánh ngọt & trạm nước tự phục vụ sáng đèn đón khách tối"
                : (workCutsceneTimer < 5.2f
                    ? "18:47 - Ánh đèn huỳnh quang hắt xuống các dãy kệ hàng chuẩn bị vào ca"
                    : "18:48 - Quản lý đang đứng chờ bàn giao ca tại quầy thu ngân");
            float capW = Raylib.MeasureTextEx(viFont, caption, 22, 1.0f).X;
            DrawViText(caption, (int)((ScreenWidth - capW) / 2), ScreenHeight - 50, 22, Color.RayWhite);
        }
        else if (isDawnCutscene)
        {
            int barH = 74;
            Raylib.DrawRectangle(0, 0, ScreenWidth, barH, Color.Black);
            Raylib.DrawRectangle(0, ScreenHeight - barH, ScreenWidth, barH, Color.Black);

            string caption = dawnCutsceneTimer < 2.8f
                ? $"{currentTime} - Bầu trời dần hửng sáng... Có người đang tiến tới cửa hàng"
                : $"{currentTime} - Quản lý bước vào tiệm để kết thúc ca trực";
            float capW = Raylib.MeasureTextEx(viFont, caption, 22, 1.0f).X;
            DrawViText(caption, (int)((ScreenWidth - capW) / 2), ScreenHeight - 50, 22, Color.RayWhite);
        }
        else if (isReturnHomeCutscene)
        {
            int barH = 74;
            Raylib.DrawRectangle(0, 0, ScreenWidth, barH, Color.Black);
            Raylib.DrawRectangle(0, ScreenHeight - barH, ScreenWidth, barH, Color.Black);

            string caption = returnHomeCutsceneTimer < 2.5f
                ? $"{currentTime} - Tú nổ máy xe số quen thuộc sau một đêm trực dài..."
                : $"{currentTime} - Ánh nắng sớm len qua con phố, Tú chạy xe về nhà an toàn.";
            float capW = Raylib.MeasureTextEx(viFont, caption, 22, 1.0f).X;
            DrawViText(caption, (int)((ScreenWidth - capW) / 2), ScreenHeight - 50, 22, Color.RayWhite);
        }
        else
        {
            Raylib.DrawRectangle(15, 15, 620, 68, new Color(0, 0, 0, 175));
            DrawViText($"Thời gian: {currentTime}   |   Cửa chính: {(isDoorLocked ? "ĐÃ KHÓA" : "ĐANG MỞ")}   |   Manh mối: {GetClueCount()}/5", 28, 22, 19, storyStep == 7 ? Color.Orange : Color.Yellow);
            DrawViText($"Nhiệm vụ: {currentObjective}", 28, 50, 19, Color.RayWhite);

            if (recordingOn)
            {
                Raylib.DrawRectangle(ScreenWidth - 135, 18, 118, 34, new Color(0, 0, 0, 180));
                if (MathF.Sin((float)Raylib.GetTime() * 5.0f) > 0f)
                {
                    Raylib.DrawCircle(ScreenWidth - 114, 35, 6, Color.Red);
                }
                DrawViText("REC", ScreenWidth - 98, 25, 19, Color.RayWhite);
            }

            if (!isTalking)
            {
                DrawShopperSpeechBubbleHUD();
            }

            bool hasAnyPrompt = skullPromptKey != "";
            Raylib.DrawCircle(ScreenWidth / 2, ScreenHeight / 2, hasAnyPrompt ? 5 : 2, hasAnyPrompt ? Color.Yellow : Color.LightGray);
            if (skullPromptKey != "" && !isTalking)
            {
                DrawSkullmugKeyPrompt();
            }
        }

        if (isTalking)
        {
            DrawDialogueBox();
        }

        DrawWcHudOverlay();
        UpdateAndDrawTopNotificationBanner(dt);

        if (fadeAlpha > 0f)
        {
            Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(0, 0, 0, (int)(fadeAlpha * 255)));
        }

        Raylib.EndDrawing();
    }

    static void HandleEndingDevShortcuts()
    {
        if (isPaused || jumpscareActive) return;

        bool shiftHeld = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
        if (!shiftHeld) return;

        bool pressF6 = Raylib.IsKeyPressed(KeyboardKey.F6);
        bool pressF7 = Raylib.IsKeyPressed(KeyboardKey.F7);
        bool pressF8 = Raylib.IsKeyPressed(KeyboardKey.F8);
        if (!pressF6 && !pressF7 && !pressF8) return;

        // Dọn sạch trạng thái hiện tại để tua băng tới mốc thời gian bí mật
        Raylib.StopSound(cafeAmbience);
        Raylib.StopSound(motorbikeEngineSound);
        StopDialogueVoice();

        gameEnded = false;
        isBadEnding = false;
        fadeAlpha = 0f;
        isCafeCutscene = false;
        isFadingOutCafe = false;
        isWorkCutscene = false;
        isDawnCutscene = false;
        isReturnHomeCutscene = false;

        isTalking = false;
        isTalkingToNpc = false;
        dialogueStage = 0;
        subChoiceMode = "";
        npcShouldLeaveAfterDialogue = false;
        ResetShopperState();

        gameScene = 2;
        Raylib.DisableCursor();
        Raylib.GetMouseDelta();

        // Tiếng rè tua băng VHS
        Raylib.SetSoundPitch(voiceStaticSound, 0.55f);
        Raylib.PlaySound(voiceStaticSound);

        if (pressF6)
        {
            isDoorLocked = true;
            lightsOn = true;
            clockMinutes = 1620f;
            currentTime = "03:00";
            sharedLocation = true;
            spoofAlertShown = true;
            recordingOn = true;
            readPoster = readLogbook = readMargin = nameTag = talkedMai = true;
            greetedCount = 2;

            playerPos = new Vector3(0f, 0f, 5.15f);
            camYaw = 0f;
            camPitch = -0.04f;
            currentBobOffset = 0f;
            camera.FovY = NormalFov;
            ApplyCameraOrientation(new Vector3(playerPos.X, 1.7f, playerPos.Z));

            SpawnGlassGhost(new Vector3(0f, 0f, 8.15f), 180f);
            storyStep = 8;
            currentObjective = "03:00 - Thứ ngoài cửa kính đang chờ câu trả lời...";
            ShowTopNotificationBanner(
                "VHS // EASTER EGG",
                "Tua Băng Tới 03:00",
                "Ký ức bị khóa đã mở. Tiến lại cửa kính và bấm [F] để đối mặt.",
                new Color(220, 38, 38, 255),
                4.2f
            );
        }
        else
        {
            bool fullClues = pressF7;
            npcActive = false;
            npcIsGhostModel = false;

            recordingOn = true;
            readPoster = true;
            readLogbook = fullClues;
            readMargin = fullClues;
            nameTag = fullClues;
            talkedMai = fullClues;
            greetedCount = fullClues ? 2 : 0;
            calledRealDung = fullClues;

            StartDawnCutscene();
            ShowTopNotificationBanner(
                "VHS // EASTER EGG",
                fullClues ? "Tua Băng Tới 05:00 (Băng Đầy Đủ 5/5)" : "Tua Băng Tới 05:00 (Băng Nhiễu 1/5)",
                "Cuốn băng đã tua tới lúc rạng sáng. Hãy đưa ra lựa chọn cuối cùng.",
                new Color(234, 179, 8, 255),
                4.5f
            );
        }
    }
}