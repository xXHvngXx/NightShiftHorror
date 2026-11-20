using System.Numerics;
using Raylib_cs;

partial class Program
{
    sealed class PhoneMessageItem
    {
        public string Sender { get; set; } = "";
        public string Time { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsMe { get; set; }
    }

    sealed class PhoneChatThread
    {
        public string Id { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string AppName { get; set; } = "Messages";
        public Color BadgeColor { get; set; } = new(10, 132, 255, 255);
        public bool Unread { get; set; }
        public List<PhoneMessageItem> Messages { get; set; } = [];
    }

    static readonly List<PhoneChatThread> phoneChatThreads = [];
    static string activeChatThreadId = "mom";

    static int phoneNotifStep = 0;
    static float phoneStepTimer = 0.85f;
    static int phoneChoiceStage = 0;
    static int phoneSelectedOption = 0;
    static float phoneEndHoldTimer = 0f;
    static string phoneSentReply = "";
    static bool phonePostReplySpawned = false;

    static string bannerApp = "";
    static string bannerSender = "";
    static string bannerBody = "";
    static Color bannerBadgeColor = new(34, 197, 94, 255);
    static float bannerTimer = 0f;
    static float bannerSlide = 0f;

    static void InitPhoneThreads()
    {
        phoneChatThreads.Clear();
        phoneChatThreads.Add(new PhoneChatThread
        {
            Id = "mom",
            ContactName = "Mẹ",
            AppName = "Messages",
            BadgeColor = new Color(52, 199, 89, 255),
            Unread = false,
            Messages = []
        });
        activeChatThreadId = "mom";
    }

    static void AppendThreadMessage(string threadId, string contactName, string appName, Color badgeColor,
                                    string sender, string time, string body, bool isMe, bool showBanner, float bannerDuration = 5.0f)
    {
        var thread = phoneChatThreads.Find(t => t.Id == threadId);
        if (thread == null)
        {
            thread = new PhoneChatThread
            {
                Id = threadId,
                ContactName = contactName,
                AppName = appName,
                BadgeColor = badgeColor,
                Unread = true
            };
            phoneChatThreads.Add(thread);
        }

        thread.Messages.Add(new PhoneMessageItem
        {
            Sender = sender,
            Time = time,
            Body = body,
            IsMe = isMe
        });

        bool isOtherThread = threadId != activeChatThreadId;
        if (isOtherThread)
        {
            thread.Unread = true;
            if (showBanner)
            {
                ShowTopNotificationBanner(appName, sender, body, badgeColor, bannerDuration);
            }
        }
        else
        {
            thread.Unread = false;
            if (!isMe)
            {
                Raylib.SetSoundPitch(voiceStaticSound, 1.55f);
                Raylib.PlaySound(voiceStaticSound);
            }
        }
    }

    static void ShowTopNotificationBanner(string app, string sender, string body, Color badgeColor, float duration = 5.0f)
    {
        bannerApp = app;
        bannerSender = sender;
        bannerBody = body;
        bannerBadgeColor = badgeColor;
        bannerTimer = duration;
        Raylib.PlaySound(notificationSound);
    }

    static void UpdateAndDrawTopNotificationBanner(float dt)
    {
        if (bannerTimer > 0f)
        {
            bannerTimer -= dt;
            bannerSlide += (1f - bannerSlide) * Math.Min(1f, dt * 5.2f);
        }
        else
        {
            bannerSlide += (0f - bannerSlide) * Math.Min(1f, dt * 4.5f);
        }

        if (bannerSlide <= 0.01f) return;

        int bannerW = Math.Min(540, ScreenWidth - 48);
        List<string> lines = WrapViText(bannerBody, bannerW - 96, 17);
        int bannerH = 44 + Math.Max(1, lines.Count) * 21;
        int bannerX = (ScreenWidth - bannerW) / 2;
        int bannerY = (int)(-bannerH - 16 + bannerSlide * (bannerH + 34));

        Rectangle shadowRect = new(bannerX + 2, bannerY + 6, bannerW, bannerH);
        Raylib.DrawRectangleRounded(shadowRect, 0.28f, 12, new Color(0, 0, 0, 125));

        Rectangle cardRect = new(bannerX, bannerY, bannerW, bannerH);
        Raylib.DrawRectangleRounded(cardRect, 0.28f, 12, new Color(26, 28, 36, 244));
        Raylib.DrawRectangleRoundedLinesEx(cardRect, 0.28f, 12, 1.5f, new Color(92, 98, 114, 210));

        int iconSize = 40;
        int iconX = bannerX + 14;
        int iconY = bannerY + (bannerH - iconSize) / 2;
        Rectangle iconRect = new(iconX, iconY, iconSize, iconSize);
        Raylib.DrawRectangleRounded(iconRect, 0.32f, 8, bannerBadgeColor);

        string badgeChar = bannerApp.Length > 0 ? bannerApp[..1].ToUpper() : "M";
        int charW = (int)Raylib.MeasureTextEx(viFont, badgeChar, 21, 1f).X;
        DrawViText(badgeChar, iconX + (iconSize - charW) / 2, iconY + 9, 21, Color.White);

        int textX = iconX + iconSize + 14;
        DrawViText($"{bannerSender}  •  {bannerApp}", textX, bannerY + 11, 17, new Color(245, 247, 252, 255));

        string timeTag = "bây giờ";
        int timeW = (int)Raylib.MeasureTextEx(viFont, timeTag, 15, 1f).X;
        DrawViText(timeTag, bannerX + bannerW - timeW - 16, bannerY + 12, 15, new Color(165, 172, 185, 220));

        for (int i = 0; i < lines.Count; i++)
        {
            DrawViText(lines[i], textX, bannerY + 34 + i * 21, 17, new Color(210, 215, 226, 240));
        }
    }

    static void DrawPhoneIntroScene(float dt)
    {
        if (!Raylib.IsSoundPlaying(ambientHumSound))
        {
            Raylib.PlaySound(ambientHumSound);
        }

        if (phoneNotifStep == 0 && phoneChoiceStage == 0 && phoneChatThreads.Count == 0)
        {
            InitPhoneThreads();
            phonePostReplySpawned = false;
            bannerTimer = 0f;
            bannerSlide = 0f;
        }

        string[] momMessages =
        [
            "Con ăn cơm chưa?",
            "Mẹ chuyển 500.000 rồi đó, đừng có nhịn nghen.",
            "Tháng này hơi khó khăn, con tiết kiệm chút."
        ];

        if (phoneChoiceStage == 0)
        {
            phoneStepTimer -= dt;
            if (phoneStepTimer <= 0f || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                if (phoneNotifStep < momMessages.Length)
                {
                    phoneNotifStep++;
                    phoneStepTimer = 2.15f;

                    AppendThreadMessage(
                        "mom", "Mẹ", "Messages", new Color(52, 199, 89, 255),
                        "Mẹ", "17:41", momMessages[phoneNotifStep - 1], false, false
                    );

                    if (phoneNotifStep == 2)
                    {
                        AppendThreadMessage(
                            "bank", "Biến động số dư", "Ngân hàng", new Color(220, 68, 68, 255),
                            "Biến động số dư", "17:41", "Tài khoản +500.000đ. Số dư hiện tại: 187.400đ.",
                            false, true, 4.8f
                        );
                    }
                    else if (phoneNotifStep == 3)
                    {
                        AppendThreadMessage(
                            "colan", "Cô Lan (Chủ trọ)", "Zalo", new Color(24, 119, 242, 255),
                            "Cô Lan (Chủ trọ)", "17:41", "Tú ơi, tháng này tiền trọ 2 triệu 8 nghen. Trước mùng 5 hen.",
                            false, true, 5.0f
                        );
                    }
                }
                else
                {
                    phoneChoiceStage = 1;
                }
            }
        }
        else if (phoneChoiceStage == 1)
        {
            string[] replies =
            [
                "Dạ con ăn rồi mẹ. Mẹ giữ tiền đi, con còn.",
                "Dạ mẹ. Con cảm ơn mẹ."
            ];

            int winW = 470;
            int winH = Math.Min(700, ScreenHeight - 130);
            int winX = (ScreenWidth - winW) / 2;
            int winY = (ScreenHeight - winH) / 2 + 18;
            int replyAreaY = winY + winH - 166;
            Vector2 mousePos = Raylib.GetMousePosition();

            for (int i = 0; i < replies.Length; i++)
            {
                Rectangle optRect = new(winX + 20, replyAreaY + 34 + i * 44, winW - 40, 38);
                if (Raylib.CheckCollisionPointRec(mousePos, optRect))
                {
                    if (phoneSelectedOption != i)
                    {
                        phoneSelectedOption = i;
                        Raylib.SetSoundPitch(voiceStaticSound, 1.25f);
                        Raylib.PlaySound(voiceStaticSound);
                    }
                    if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        lieToMom = (i == 0);
                        phoneSentReply = replies[i];
                        AppendThreadMessage("mom", "Mẹ", "Messages", new Color(52, 199, 89, 255), "Bạn", "17:42", phoneSentReply, true, false);
                        Raylib.SetSoundPitch(voiceStaticSound, 1.45f);
                        Raylib.PlaySound(voiceStaticSound);
                        phoneChoiceStage = 2;
                        phoneEndHoldTimer = 4.6f;
                    }
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S) ||
                Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
            {
                phoneSelectedOption = 1 - phoneSelectedOption;
                Raylib.SetSoundPitch(voiceStaticSound, 1.25f);
                Raylib.PlaySound(voiceStaticSound);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                lieToMom = (phoneSelectedOption == 0);
                phoneSentReply = replies[phoneSelectedOption];
                AppendThreadMessage("mom", "Mẹ", "Messages", new Color(52, 199, 89, 255), "Bạn", "17:42", phoneSentReply, true, false);
                Raylib.SetSoundPitch(voiceStaticSound, 1.45f);
                Raylib.PlaySound(voiceStaticSound);
                phoneChoiceStage = 2;
                phoneEndHoldTimer = 4.6f;
            }
        }
        else if (phoneChoiceStage == 2)
        {
            phoneEndHoldTimer -= dt;
            if (!phonePostReplySpawned && phoneEndHoldTimer <= 3.4f)
            {
                phonePostReplySpawned = true;
                string momAck = lieToMom ? "Ừ ráng ăn uống đầy đủ nha con." : "Ừ ngoan, ráng giữ sức khỏe nha con.";
                AppendThreadMessage("mom", "Mẹ", "Messages", new Color(52, 199, 89, 255), "Mẹ", "17:42", momAck, false, false);
                AppendThreadMessage(
                    "cafe_group", "Hội Cà Phê Chiều Thứ Sáu", "Zalo", new Color(24, 119, 242, 255),
                    "Dũng", "17:42", "Ê Tú, đi cà phê hông? Đen bao.",
                    false, true, 4.5f
                );
            }

            if (phoneEndHoldTimer <= 0f)
            {
                bannerTimer = 0f;
                bannerSlide = 0f;
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
        Raylib.ClearBackground(new Color(10, 12, 17, 255));
        DrawVignette(55);

        int w = 470;
        int h = Math.Min(700, ScreenHeight - 130);
        int x = (ScreenWidth - w) / 2;
        int y = (ScreenHeight - h) / 2 + 18;
        int topBarH = 52;
        int bottomBarH = 54;

        Rectangle outerShadow = new(x + 4, y + 10, w, h);
        Raylib.DrawRectangleRounded(outerShadow, 0.045f, 12, new Color(0, 0, 0, 140));

        Rectangle winRect = new(x, y, w, h);
        Raylib.DrawRectangleRounded(winRect, 0.045f, 12, new Color(252, 252, 254, 255));

        Rectangle headerRect = new(x, y, w, topBarH);
        Raylib.DrawRectangleRounded(headerRect, 0.045f, 12, new Color(240, 241, 245, 255));
        Raylib.DrawRectangle(x, y + topBarH - 12, w, 12, new Color(240, 241, 245, 255));
        Raylib.DrawLine(x, y + topBarH, x + w, y + topBarH, new Color(216, 218, 224, 255));

        Raylib.DrawCircle(x + 22, y + topBarH / 2, 6, new Color(255, 95, 86, 255));
        Raylib.DrawCircle(x + 42, y + topBarH / 2, 6, new Color(255, 189, 46, 255));
        Raylib.DrawCircle(x + 62, y + topBarH / 2, 6, new Color(39, 201, 63, 255));

        string headerTitle = "Messages  ▸  Mẹ";
        int headerW = (int)Raylib.MeasureTextEx(viFont, headerTitle, 20, 1f).X;
        DrawViText(headerTitle, x + (w - headerW) / 2, y + 16, 20, new Color(45, 48, 56, 255));

        string timeLabel = "Hôm nay 17:41";
        int timeLabelW = (int)Raylib.MeasureTextEx(viFont, timeLabel, 15, 1f).X;
        DrawViText(timeLabel, x + (w - timeLabelW) / 2, y + topBarH + 14, 15, new Color(142, 146, 156, 255));

        int msgY = y + topBarH + 42;
        int maxBubbleW = w - 135;

        for (int i = 0; i < phoneNotifStep; i++)
        {
            List<string> lines = WrapViText(momMessages[i], maxBubbleW - 28, 19);
            int textW = 0;
            foreach (string line in lines)
            {
                textW = Math.Max(textW, (int)Raylib.MeasureTextEx(viFont, line, 19, 1f).X);
            }

            int bubbleW = Math.Max(74, textW + 30);
            int bubbleH = 18 + lines.Count * 24;
            Rectangle bubbleRect = new(x + 22, msgY, bubbleW, bubbleH);
            Raylib.DrawRectangleRounded(bubbleRect, 0.36f, 10, new Color(230, 231, 236, 255));

            for (int l = 0; l < lines.Count; l++)
            {
                DrawViText(lines[l], x + 37, msgY + 9 + l * 24, 19, new Color(25, 27, 32, 255));
            }

            msgY += bubbleH + 10;
        }

        if (phoneChoiceStage == 0 && phoneNotifStep < momMessages.Length)
        {
            Rectangle typingRect = new(x + 22, msgY, 72, 36);
            Raylib.DrawRectangleRounded(typingRect, 0.45f, 10, new Color(230, 231, 236, 255));
            float t = (float)Raylib.GetTime() * 6f;
            for (int d = 0; d < 3; d++)
            {
                float wave = MathF.Sin(t - d * 0.9f);
                byte alpha = (byte)(125 + Math.Max(0f, wave) * 115);
                int dotY = msgY + 18 - (int)(Math.Max(0f, wave) * 3f);
                Raylib.DrawCircle(x + 42 + d * 15, dotY, 4, new Color((byte)110, (byte)114, (byte)124, alpha));
            }
        }

        if (phoneChoiceStage == 2)
        {
            msgY += 6;
            List<string> sentLines = WrapViText(phoneSentReply, maxBubbleW - 28, 19);
            int sentTextW = 0;
            foreach (string line in sentLines)
            {
                sentTextW = Math.Max(sentTextW, (int)Raylib.MeasureTextEx(viFont, line, 19, 1f).X);
            }

            int sentW = Math.Max(84, sentTextW + 30);
            int sentH = 18 + sentLines.Count * 24;
            int sentX = x + w - 22 - sentW;
            Rectangle sentRect = new(sentX, msgY, sentW, sentH);
            Raylib.DrawRectangleRounded(sentRect, 0.36f, 10, new Color(10, 132, 255, 255));

            for (int l = 0; l < sentLines.Count; l++)
            {
                DrawViText(sentLines[l], sentX + 15, msgY + 9 + l * 24, 19, Color.White);
            }
            msgY += sentH + 6;
            string delivered = "Đã gửi";
            int delivW = (int)Raylib.MeasureTextEx(viFont, delivered, 14, 1f).X;
            DrawViText(delivered, x + w - 26 - delivW, msgY, 14, new Color(142, 146, 156, 255));

            if (phonePostReplySpawned)
            {
                msgY += 24;
                string momAck = lieToMom ? "Ừ ráng ăn uống đầy đủ nha con." : "Ừ ngoan, ráng giữ sức khỏe nha con.";
                int ackW = (int)Raylib.MeasureTextEx(viFont, momAck, 19, 1f).X + 30;
                Rectangle ackRect = new(x + 22, msgY, ackW, 42);
                Raylib.DrawRectangleRounded(ackRect, 0.36f, 10, new Color(230, 231, 236, 255));
                DrawViText(momAck, x + 37, msgY + 11, 19, new Color(25, 27, 32, 255));
            }
        }

        if (phoneChoiceStage == 1)
        {
            int replyAreaY = y + h - 166;
            Raylib.DrawRectangle(x + 12, replyAreaY, w - 24, 152, new Color(243, 244, 248, 255));
            Raylib.DrawLine(x + 12, replyAreaY, x + w - 12, replyAreaY, new Color(216, 218, 224, 255));
            DrawViText("Chọn câu trả lời:", x + 24, replyAreaY + 8, 16, new Color(115, 120, 130, 255));

            string[] replies =
            [
                "Dạ con ăn rồi mẹ. Mẹ giữ tiền đi, con còn.",
                "Dạ mẹ. Con cảm ơn mẹ."
            ];

            for (int i = 0; i < replies.Length; i++)
            {
                bool sel = i == phoneSelectedOption;
                Rectangle optRect = new(x + 20, replyAreaY + 34 + i * 44, w - 40, 38);
                Color bg = sel ? new Color(10, 132, 255, 255) : new Color(228, 230, 236, 255);
                Color fg = sel ? Color.White : new Color(32, 35, 42, 255);
                Raylib.DrawRectangleRounded(optRect, 0.35f, 8, bg);
                DrawViText(replies[i], x + 34, replyAreaY + 43 + i * 44, 18, fg);
            }

            DrawViText("[Lên/Xuống] hoặc Click để chọn  •  [Enter] Gửi", x + 24, y + h - 24, 15, new Color(130, 134, 144, 255));
        }
        else
        {
            int footerY = y + h - bottomBarH;
            Raylib.DrawLine(x, footerY, x + w, footerY, new Color(218, 220, 226, 255));
            Rectangle inputBox = new(x + 18, footerY + 10, w - 36, 34);
            Raylib.DrawRectangleRounded(inputBox, 0.5f, 8, Color.White);
            Raylib.DrawRectangleRoundedLinesEx(inputBox, 0.5f, 8, 1.2f, new Color(205, 208, 216, 255));
            string placeholder = phoneChoiceStage == 0 ? "Message   (Nhấn Space / Enter để tua nhanh)" : "Message";
            DrawViText(placeholder, x + 34, footerY + 18, 17, new Color(160, 164, 172, 255));
        }

        Raylib.DrawRectangleRoundedLinesEx(winRect, 0.045f, 12, 1.5f, new Color(185, 188, 198, 255));

        UpdateAndDrawTopNotificationBanner(dt);

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