using System.Numerics;
using Raylib_cs;

partial class Program
{
    static int phoneNotifStep = 0;
    static float phoneStepTimer = 0.7f;
    static int phoneChoiceStage = 0;
    static int phoneSelectedOption = 0;
    static float phoneEndHoldTimer = 0f;
    static string phoneSentReply = "";

    static void DrawPhoneIntroScene(float dt)
    {
        if (!Raylib.IsSoundPlaying(ambientHumSound))
        {
            Raylib.PlaySound(ambientHumSound);
        }

        if (phoneChoiceStage == 0)
        {
            phoneStepTimer -= dt;
            if (phoneStepTimer <= 0f || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                if (phoneNotifStep < initialPhoneMessages.Length)
                {
                    phoneNotifStep++;
                    Raylib.PlaySound(phoneVibrateSound);
                    phoneStepTimer = 1.45f;
                }
                else
                {
                    phoneChoiceStage = 1;
                }
            }
        }
        else if (phoneChoiceStage == 1)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S) ||
                Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
            {
                phoneSelectedOption = 1 - phoneSelectedOption;
                Raylib.SetSoundPitch(voiceStaticSound, 1.25f);
                Raylib.PlaySound(voiceStaticSound);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                if (phoneSelectedOption == 0)
                {
                    lieToMom = true;
                    phoneSentReply = "Dạ con ăn rồi mẹ. Mẹ giữ tiền đi, con còn.";
                }
                else
                {
                    lieToMom = false;
                    phoneSentReply = "Dạ mẹ. Con cảm ơn mẹ.";
                }
                Raylib.SetSoundPitch(voiceStaticSound, 1.45f);
                Raylib.PlaySound(voiceStaticSound);
                phoneChoiceStage = 2;
                phoneEndHoldTimer = 2.0f;
            }
        }
        else if (phoneChoiceStage == 2)
        {
            phoneEndHoldTimer -= dt;
            if (phoneEndHoldTimer <= 0f)
            {
                Raylib.StopSound(ambientHumSound);
                gameScene = 0;
                cafeIndex = 0;
                cafeSubStep = 0;
                fadeAlpha = 1.0f;
                isCafeCutscene = true;
                cafeCutsceneTimer = 0f;
                isTalking = false;
                StopDialogueVoice();
                return;
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(8, 9, 13, 255));
        DrawVignette(55);

        int phoneW = 430;
        int phoneH = 650;
        int phoneX = (ScreenWidth - phoneW) / 2;
        int phoneY = (ScreenHeight - phoneH) / 2;

        Raylib.DrawRectangle(phoneX - 8, phoneY - 8, phoneW + 16, phoneH + 16, new Color(26, 28, 34, 255));
        Raylib.DrawRectangleLines(phoneX - 8, phoneY - 8, phoneW + 16, phoneH + 16, new Color(75, 80, 92, 255));
        Raylib.DrawRectangle(phoneX, phoneY, phoneW, phoneH, new Color(15, 17, 22, 255));

        DrawViText("17:42", phoneX + 22, phoneY + 14, 18, Color.RayWhite);
        DrawViText("4G  |  Pin 38%", phoneX + phoneW - 135, phoneY + 14, 17, new Color(220, 120, 95, 255));
        Raylib.DrawLine(phoneX + 16, phoneY + 42, phoneX + phoneW - 16, phoneY + 42, new Color(45, 48, 58, 255));

        int cardY = phoneY + 56;
        for (int i = 0; i < phoneNotifStep; i++)
        {
            var msg = initialPhoneMessages[i];
            List<string> lines = WrapViText(msg.Body, phoneW - 64, 18);
            int cardH = 38 + lines.Count * 22;

            Color borderCol = i == 1 ? new Color(190, 75, 75, 255) : new Color(65, 70, 82, 255);
            Raylib.DrawRectangle(phoneX + 18, cardY, phoneW - 36, cardH, new Color(26, 29, 37, 245));
            Raylib.DrawRectangleLines(phoneX + 18, cardY, phoneW - 36, cardH, borderCol);

            Color titleCol = i == 1 ? new Color(245, 110, 110, 255) : (i == 2 ? Color.SkyBlue : Color.RayWhite);
            DrawViText($"{msg.App} • {msg.Sender}", phoneX + 30, cardY + 8, 17, titleCol);
            DrawViText(msg.Time, phoneX + phoneW - 68, cardY + 8, 16, Color.Gray);

            for (int l = 0; l < lines.Count; l++)
            {
                Color bodyCol = i == 1 ? Color.Yellow : Color.RayWhite;
                DrawViText(lines[l], phoneX + 30, cardY + 30 + l * 22, 18, bodyCol);
            }

            cardY += cardH + 10;
        }

        if (phoneChoiceStage == 1)
        {
            int replyBoxY = phoneY + phoneH - 155;
            Raylib.DrawRectangle(phoneX + 18, replyBoxY, phoneW - 36, 136, new Color(20, 23, 30, 255));
            Raylib.DrawRectangleLines(phoneX + 18, replyBoxY, phoneW - 36, 136, Color.Gray);
            DrawViText("Trả lời tin nhắn Mẹ:", phoneX + 30, replyBoxY + 10, 18, Color.RayWhite);

            string[] replies =
            [
                "Dạ con ăn rồi mẹ. Mẹ giữ tiền đi, con còn.",
                "Dạ mẹ. Con cảm ơn mẹ."
            ];

            for (int i = 0; i < replies.Length; i++)
            {
                bool sel = i == phoneSelectedOption;
                Color col = sel ? Color.Yellow : new Color(185, 160, 70, 255);
                DrawViText((sel ? "> " : "  ") + replies[i], phoneX + 28, replyBoxY + 42 + i * 32, 18, col);
            }
            DrawViText("[Lên/Xuống] Chọn   [Enter] Gửi", phoneX + 30, replyBoxY + 108, 16, Color.Gray);
        }
        else if (phoneChoiceStage == 2)
        {
            int sentBoxY = phoneY + phoneH - 115;
            Raylib.DrawRectangle(phoneX + 46, sentBoxY, phoneW - 64, 52, new Color(32, 74, 128, 255));
            DrawViText("Bạn: " + phoneSentReply, phoneX + 58, sentBoxY + 15, 18, Color.RayWhite);
            DrawViText("Số dư khả dụng: 187.400đ", phoneX + 115, phoneY + phoneH - 42, 19, Color.Yellow);
        }
        else
        {
            DrawViText("[Space / Enter] Xem tiếp", phoneX + 125, phoneY + phoneH - 34, 17, Color.Gray);
        }

        Raylib.EndDrawing();
    }

    static void UpdateAndDrawCafeScene(float dt)
    {
        if (cafeSubStep < 2 && !Raylib.IsSoundPlaying(cafeAmbience))
        {
            Raylib.PlaySound(cafeAmbience);
        }

        if (fadeAlpha > 0f && !isFadingOutCafe)
        {
            fadeAlpha = Math.Max(0f, fadeAlpha - dt * 0.7f);
        }

        if (isCafeCutscene)
        {
            Raylib.GetMouseDelta();
            UpdateCafeCutscene(dt);
        }
        else
        {
            float targetCafeFov = NormalFov;
            if (cafeSubStep == 0 && !isFadingOutCafe)
            {
                if (currentSpeaker == "Dũng")
                {
                    Raylib.GetMouseDelta();
                    SmoothLookAtPoint(cafeSeatPos, dungHeadPos, dt, 6.8f);
                    ApplyCameraOrientation(cafeSeatPos);
                    targetCafeFov = TalkNpcFov;
                }
                else if (currentSpeaker == "Đen")
                {
                    Raylib.GetMouseDelta();
                    SmoothLookAtPoint(cafeSeatPos, denHeadPos, dt, 6.8f);
                    ApplyCameraOrientation(cafeSeatPos);
                    targetCafeFov = TalkNpcFov;
                }
                else if (currentSpeaker == "Hưng")
                {
                    Raylib.GetMouseDelta();
                    SmoothLookAtPoint(cafeSeatPos, hungHeadPos, dt, 6.8f);
                    ApplyCameraOrientation(cafeSeatPos);
                    targetCafeFov = TalkNpcFov;
                }
                else
                {
                    UpdateFirstPersonLook(cafeSeatPos);
                    targetCafeFov = 72.0f;
                }
            }
            else
            {
                UpdateFirstPersonLook(cafeSeatPos);
            }

            camera.FovY += (targetCafeFov - camera.FovY) * (1f - MathF.Exp(-6.0f * dt));

            if (isFadingOutCafe)
            {
                fadeAlpha = Math.Min(1.0f, fadeAlpha + dt * 0.85f);
                if (fadeAlpha >= 1.0f)
                {
                    Raylib.StopSound(cafeAmbience);
                    isFadingOutCafe = false;
                    StartMotorbikeScene();
                }
            }
            else
            {
                UpdateTypewriter(dt, GetSpeakerPitch(currentSpeaker));
                bool textFinished = (int)visibleChars >= fullText.Length;

                if (!textFinished && (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.F)))
                {
                    visibleChars = fullText.Length;
                }
                else if (textFinished)
                {
                    if (cafeSubStep == 0 && Raylib.IsKeyPressed(KeyboardKey.Enter))
                    {
                        cafeIndex++;
                        if (cafeIndex < cafeDialogue.Length)
                        {
                            StartDialogue(cafeDialogue[cafeIndex].Speaker, cafeDialogue[cafeIndex].Text, 2);
                        }
                        else
                        {
                            cafeSubStep = 1;
                            StartDialogue(
                                "Tú",
                                "Trước khi rời quán cà phê, mình có nên gửi định vị quán tiện lợi cho Dũng và Đen không?",
                                1,
                                [
                                    "Gửi định vị quán cho Dũng và Đen",
                                    "Gửi làm del gì?"
                                ]
                            );
                        }
                    }
                    else if (cafeSubStep == 1)
                    {
                        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S) ||
                            Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
                        {
                            selectedOption = 1 - selectedOption;
                            Raylib.SetSoundPitch(voiceStaticSound, 1.3f);
                            Raylib.PlaySound(voiceStaticSound);
                        }
                        else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                        {
                            sharedLocation = (selectedOption == 0);
                            if (sharedLocation)
                            {
                                cafeSubStep = 2;
                                Raylib.SetSoundPitch(voiceStaticSound, 0.72f);
                                Raylib.PlaySound(voiceStaticSound);
                                StartDialogue(
                                    "Tú",
                                    "(Vừa bấm gửi ghim vị trí đường Số 9 vào nhóm chat, màn hình điện thoại bỗng nháy sọc nhiễu và tự động báo 'Đã xem' ngay lập tức, dù Dũng vẫn đang cắm cúi lướt điện thoại đối diện chưa hề mở tin nhắn...)",
                                    2
                                );
                            }
                            else
                            {
                                cafeSubStep = 3;
                                Raylib.StopSound(cafeAmbience);
                                Raylib.PlaySound(phoneRingSound);
                                StartDialogue(
                                    "Quản lý",
                                    "Alo? À, sinh viên hả? Được, được. Mười một giờ rưỡi em tới nghen, anh dặn kỹ rồi giao ca luôn. Không cần mang giấy tờ gì đâu.",
                                    2
                                );
                            }
                        }
                    }
                    else if (cafeSubStep == 2 && Raylib.IsKeyPressed(KeyboardKey.Enter))
                    {
                        cafeSubStep = 3;
                        Raylib.StopSound(cafeAmbience);
                        Raylib.PlaySound(phoneRingSound);
                        StartDialogue(
                            "Quản lý",
                            "Alo? À, sinh viên hả? Được, được. Mười một giờ rưỡi em tới nghen, anh dặn kỹ rồi giao ca luôn. Không cần mang giấy tờ gì đâu.",
                            2
                        );
                    }
                    else if (cafeSubStep == 3 && Raylib.IsKeyPressed(KeyboardKey.Enter))
                    {
                        isFadingOutCafe = true;
                    }
                }
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(28, 20, 16, 255));

        if (cafeSubStep < 3)
        {
            RenderFoggyScene(() =>
            {
                float cafeTime = (float)Raylib.GetTime();
                Props.DrawCafe(cafeTime, camera.Position, isCafeCutscene ? "" : currentSpeaker);
            }, new Color(36, 26, 18, 255), 2.0f);
        }
        else
        {
            Raylib.ClearBackground(new Color(10, 11, 15, 255));
        }

        DrawVignette();

        if (isCafeCutscene)
        {
            int barH = 74;
            Raylib.DrawRectangle(0, 0, ScreenWidth, barH, Color.Black);
            Raylib.DrawRectangle(0, ScreenHeight - barH, ScreenWidth, barH, Color.Black);

            string caption = cafeCutsceneTimer < 3.0f
                ? "18:05 - Quán cà phê lounge quen thuộc chiều thứ Sáu"
                : (cafeCutsceneTimer < 5.8f
                    ? "Không gian ấm ánh đèn vàng giữa tiếng trò chuyện râm ran của khách"
                    : "Bàn góc sảnh - Hội bạn thân đang tụ tập cuối tuần");
            float capW = Raylib.MeasureTextEx(viFont, caption, 22, 1.0f).X;
            DrawViText(caption, (int)((ScreenWidth - capW) / 2), ScreenHeight - 50, 22, Color.RayWhite);
        }
        else if (!isFadingOutCafe)
        {
            DrawDialogueBox();
        }

        if (fadeAlpha > 0f)
        {
            Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(0, 0, 0, (int)(fadeAlpha * 255)));
        }

        Raylib.EndDrawing();
    }

    static void UpdateAndDrawIntroMonologueScene(float dt)
    {
        UpdateTypewriter(dt, GetSpeakerPitch("Tú"));
        bool textFinished = (int)visibleChars >= fullText.Length;

        if (!textFinished && (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.F)))
        {
            visibleChars = fullText.Length;
        }
        else if (textFinished && Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            introIndex++;
            if (introIndex < introMonologue.Length)
            {
                StartDialogue("Tú", introMonologue[introIndex], 2);
            }
            else
            {
                playerPos = new Vector3(-1.2f, 0f, -3.5f);
                camYaw = 0f;
                camPitch = -0.02f;
                currentBobOffset = 0f;
                camera.FovY = NormalFov;
                UpdateFirstPersonLook(new Vector3(playerPos.X, 1.7f, playerPos.Z));
                Raylib.GetMouseDelta();
                fadeAlpha = 1.0f;
                gameScene = 2;
                storyStep = -1;
                clockMinutes = 1428f;
                currentTime = "23:48";
                currentObjective = "Nói chuyện với Quản lý ở quầy thu ngân (có thể xem áp phích ngoài cột đèn)";
                SpawnNpc(
                    new Vector3(-1.1f, 0f, -1.1f),
                    new Vector3(-1.1f, 0f, -1.1f),
                    new Color(35, 110, 75, 255),
                    new Color(30, 30, 35, 255),
                    FearsBodySpec.Manager,
                    FaceId.Manager
                );

                isWorkCutscene = true;
                workCutsceneTimer = 0f;
                isTalking = false;
                StopDialogueVoice();
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        DrawDialogueBox();
        Raylib.EndDrawing();
    }

    static void UpdateCafeCutscene(float dt)
    {
        const float TotalDuration = 8.5f;
        cafeCutsceneTimer += dt;

        Vector3 camPos;
        Vector3 camLook;

        if (cafeCutsceneTimer < 3.0f)
        {
            float p = Math.Clamp(cafeCutsceneTimer / 3.0f, 0f, 1f);
            float s = p * p * (3f - 2f * p);
            camPos = Vector3.Lerp(new Vector3(2.8f, 2.35f, 8.2f), new Vector3(3.4f, 2.15f, 3.8f), s);
            camLook = Vector3.Lerp(new Vector3(-1.2f, 1.55f, 1.5f), new Vector3(2.5f, 1.25f, 1.2f), s);
        }
        else if (cafeCutsceneTimer < 5.8f)
        {
            float p = Math.Clamp((cafeCutsceneTimer - 3.0f) / 2.8f, 0f, 1f);
            float s = p * p * (3f - 2f * p);
            camPos = Vector3.Lerp(new Vector3(3.4f, 2.15f, 3.8f), new Vector3(-2.6f, 2.00f, 1.6f), s);
            camLook = Vector3.Lerp(new Vector3(-3.5f, 1.35f, 2.8f), new Vector3(-0.6f, 1.25f, 0.2f), s);
        }
        else
        {
            float p = Math.Clamp((cafeCutsceneTimer - 5.8f) / 2.7f, 0f, 1f);
            float s = p * p * (3f - 2f * p);
            camPos = Vector3.Lerp(new Vector3(-2.6f, 2.00f, 1.6f), cafeSeatPos, s);
            camLook = Vector3.Lerp(new Vector3(-0.6f, 1.25f, 0.2f), dungHeadPos, s);
        }

        camera.Position = camPos;
        camera.Target = camLook;
        camera.FovY += (66.0f - camera.FovY) * (1f - MathF.Exp(-5.0f * dt));

        if (cafeCutsceneTimer >= TotalDuration)
        {
            isCafeCutscene = false;
            Vector3 toDung = dungHeadPos - cafeSeatPos;
            camYaw = MathF.Atan2(toDung.X, toDung.Z);
            camPitch = -0.02f;
            ApplyCameraOrientation(cafeSeatPos);
            Raylib.GetMouseDelta();
            StartDialogue(cafeDialogue[0].Speaker, cafeDialogue[0].Text, 2);
        }
    }

    static void UpdateWorkCutscene(float dt)
    {
        const float TotalDuration = 7.5f;
        workCutsceneTimer += dt;

        Vector3 camPos;
        Vector3 camLook;

        if (workCutsceneTimer < 2.6f)
        {
            float p = Math.Clamp(workCutsceneTimer / 2.6f, 0f, 1f);
            float s = p * p * (3f - 2f * p);
            camPos = Vector3.Lerp(new Vector3(-0.3f, 2.25f, 6.2f), new Vector3(-1.4f, 2.10f, 2.6f), s);
            camLook = Vector3.Lerp(new Vector3(-3.8f, 1.45f, 1.5f), new Vector3(-5.8f, 1.65f, -0.5f), s);
        }
        else if (workCutsceneTimer < 5.2f)
        {
            float p = Math.Clamp((workCutsceneTimer - 2.6f) / 2.6f, 0f, 1f);
            float s = p * p * (3f - 2f * p);
            camPos = Vector3.Lerp(new Vector3(-1.4f, 2.10f, 2.6f), new Vector3(0.4f, 2.05f, 1.2f), s);
            camLook = Vector3.Lerp(new Vector3(2.4f, 1.40f, 1.8f), new Vector3(5.6f, 1.45f, -1.2f), s);
        }
        else
        {
            float p = Math.Clamp((workCutsceneTimer - 5.2f) / 2.3f, 0f, 1f);
            float s = p * p * (3f - 2f * p);
            Vector3 endEye = new(playerPos.X, 1.7f, playerPos.Z);
            Vector3 mgrHead = new(-1.1f, 1.56f, -1.1f);
            camPos = Vector3.Lerp(new Vector3(0.4f, 2.05f, 1.2f), endEye, s);
            camLook = Vector3.Lerp(new Vector3(1.2f, 1.45f, -1.5f), mgrHead, s);
        }

        camera.Position = camPos;
        camera.Target = camLook;
        camera.FovY += (58.0f - camera.FovY) * (1f - MathF.Exp(-5.0f * dt));

        if (workCutsceneTimer >= TotalDuration)
        {
            isWorkCutscene = false;
            camera.FovY = NormalFov;

            Vector3 eyePos = new(playerPos.X, 1.7f, playerPos.Z);
            Vector3 toMgr = new Vector3(-1.1f, 1.56f, -1.1f) - eyePos;
            camYaw = MathF.Atan2(toMgr.X, toMgr.Z);
            camPitch = -0.04f;
            ApplyCameraOrientation(eyePos);
            Raylib.GetMouseDelta();

            StartDialogue(
                "Tú",
                "Lời thằng Hưng dặn lúc nãy cũng có lý... Mình nên bật ghi âm trên điện thoại để phòng thân trước khi nhận ca.",
                1,
                [
                    "Bật ghi âm điện thoại",
                    "Thôi, sợ del gì"
                ],
                mode: "record_choice"
            );
        }
    }

    static void StartDawnCutscene()
    {
        isDawnCutscene = true;
        dawnCutsceneTimer = 0f;
        dawnProgress = 0f;
        isTalking = false;
        StopDialogueVoice();
        isTalkingToNpc = false;
        isDoorLocked = false;
        lightsOn = true;
        currentObjective = "05:00 - Trời đã sáng, Quản lý đang bước vào cửa hàng";

        playerPos = new Vector3(-1.2f, 0f, -3.35f);
        camYaw = 0f;
        camPitch = -0.02f;

        SpawnNpc(
            new Vector3(0.0f, 0f, 12.2f),
            new Vector3(-1.1f, 0f, -1.1f),
            new Color(35, 110, 75, 255),
            new Color(30, 30, 35, 255),
            FearsBodySpec.Manager,
            FaceId.Manager
        );
        storyStep = 10;
    }

    static void UpdateDawnCutscene(float dt)
    {
        const float TotalDuration = 7.6f;
        dawnCutsceneTimer += dt;
        float t = Math.Clamp(dawnCutsceneTimer / TotalDuration, 0f, 1f);
        dawnProgress = t;

        clockMinutes = 1625f + (1740f - 1625f) * Math.Clamp(dawnCutsceneTimer / 4.4f, 0f, 1f);
        currentTime = FormatClock(clockMinutes);

        Vector3 camPos;
        Vector3 camLook;

        if (dawnCutsceneTimer < 2.8f)
        {
            float p = Math.Clamp(dawnCutsceneTimer / 2.8f, 0f, 1f);
            float smoothP = p * p * (3f - 2f * p);
            camPos = Vector3.Lerp(new Vector3(1.55f, 2.20f, 10.2f), new Vector3(1.10f, 2.00f, 8.75f), smoothP);
            camLook = Vector3.Lerp(new Vector3(0.15f, 2.15f, 7.60f), new Vector3(0.0f, 1.65f, 7.5f), smoothP);
        }
        else if (dawnCutsceneTimer < 5.9f)
        {
            float p = Math.Clamp((dawnCutsceneTimer - 2.8f) / 3.1f, 0f, 1f);
            float smoothP = p * p * (3f - 2f * p);
            camPos = Vector3.Lerp(new Vector3(1.15f, 2.05f, 4.65f), new Vector3(0.85f, 1.85f, 1.65f), smoothP);
            Vector3 managerFocus = new(npcPos.X, 1.55f, npcPos.Z);
            Vector3 insideSignFocus = new(0.25f, 2.30f, 7.40f);
            camLook = Vector3.Lerp(insideSignFocus, managerFocus, Math.Clamp(p * 1.35f, 0f, 1f));
        }
        else
        {
            float p = Math.Clamp((dawnCutsceneTimer - 5.9f) / 1.7f, 0f, 1f);
            float smoothP = p * p * (3f - 2f * p);
            Vector3 eyeEnd = new(playerPos.X, 1.7f, playerPos.Z);
            Vector3 lookEnd = new(-1.1f, 1.56f, -1.1f);
            camPos = Vector3.Lerp(new Vector3(0.85f, 1.85f, 1.65f), eyeEnd, smoothP);
            camLook = Vector3.Lerp(new Vector3(npcPos.X, 1.55f, npcPos.Z), lookEnd, smoothP);
        }

        camera.Position = camPos;
        camera.Target = camLook;
        camera.FovY += (56.0f - camera.FovY) * (1f - MathF.Exp(-5.0f * dt));

        if (dawnCutsceneTimer >= TotalDuration)
        {
            isDawnCutscene = false;
            dawnProgress = 1f;
            clockMinutes = 1740f;
            currentTime = "05:00";
            camera.FovY = NormalFov;

            Vector3 eyePos = new(playerPos.X, 1.7f, playerPos.Z);
            Vector3 toMgr = new Vector3(-1.1f, 1.56f, -1.1f) - eyePos;
            camYaw = MathF.Atan2(toMgr.X, toMgr.Z);
            camPitch = -0.04f;
            ApplyCameraOrientation(eyePos);
            Raylib.GetMouseDelta();

            currentObjective = "Nói chuyện với Quản lý để kết thúc ca trực";
            if (recordingOn)
            {
                StartDialogue(
                    "Quản lý",
                    "Làm tốt lắm Tú, đây là phong bì 500 nghìn tiền công đêm nay. Em ký xác nhận vào tờ giấy này rồi tối mai 12 giờ lại qua trực tiếp nhé.",
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
                    "Làm tốt lắm Tú, đây là phong bì 500 nghìn tiền công đêm nay của em. Giờ em về nghỉ ngơi đi, tối mai 12 giờ lại qua trực tiếp nhé.",
                    2,
                    talkingToNpc: true
                );
            }
        }
    }
}