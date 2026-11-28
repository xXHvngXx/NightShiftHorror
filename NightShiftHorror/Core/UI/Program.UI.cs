using System.Numerics;
using Raylib_cs;

partial class Program
{
    static Color LerpColor(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }

    static void RenderFoggyScene(Action drawWorld, Color fogTint, float blurSpread = 2.2f)
    {
        Raylib.BeginTextureMode(sceneTarget);
        Raylib.ClearBackground(fogTint);
        Raylib.BeginMode3D(camera);
        drawWorld();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        Rectangle srcRect = new(0, 0, sceneTarget.Texture.Width, -sceneTarget.Texture.Height);
        Rectangle dstRect = new(0, 0, ScreenWidth, ScreenHeight);

        Raylib.DrawTexturePro(sceneTarget.Texture, srcRect, dstRect, Vector2.Zero, 0f, Color.White);

        Color hazeTint = new((byte)255, (byte)255, (byte)255, (byte)34);
        Raylib.DrawTexturePro(sceneTarget.Texture, srcRect, new Rectangle(-blurSpread, 0, ScreenWidth, ScreenHeight), Vector2.Zero, 0f, hazeTint);
        Raylib.DrawTexturePro(sceneTarget.Texture, srcRect, new Rectangle(blurSpread, 0, ScreenWidth, ScreenHeight), Vector2.Zero, 0f, hazeTint);
        Raylib.DrawTexturePro(sceneTarget.Texture, srcRect, new Rectangle(0, -blurSpread * 0.75f, ScreenWidth, ScreenHeight), Vector2.Zero, 0f, hazeTint);
        Raylib.DrawTexturePro(sceneTarget.Texture, srcRect, new Rectangle(0, blurSpread * 0.75f, ScreenWidth, ScreenHeight), Vector2.Zero, 0f, hazeTint);

        Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(fogTint.R, fogTint.G, fogTint.B, (byte)42));

        for (int y = 0; y < ScreenHeight; y += 4)
        {
            Raylib.DrawRectangle(0, y, ScreenWidth, 1, new Color(0, 0, 0, 14));
        }
    }

    static void DrawViText(string text, int x, int y, float fontSize, Color color)
    {
        Raylib.DrawTextEx(viFont, text, new Vector2(x, y), fontSize, 1.0f, color);
    }

    static List<string> WrapViText(string text, float maxWidth, float fontSize)
    {
        List<string> lines = [];
        if (string.IsNullOrEmpty(text)) return lines;

        string[] words = text.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            float lineWidth = Raylib.MeasureTextEx(viFont, testLine, fontSize, 1.0f).X;

            if (lineWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    static string FitViTextSingleLine(string text, float maxWidth, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (Raylib.MeasureTextEx(viFont, text, fontSize, 1.0f).X <= maxWidth) return text;

        string trimmed = text;
        while (trimmed.Length > 1 && Raylib.MeasureTextEx(viFont, trimmed + "...", fontSize, 1.0f).X > maxWidth)
        {
            trimmed = trimmed[..^1];
        }
        return trimmed + "...";
    }

    static void DrawVignette(int extraDarkness = 0)
    {
        int alpha = Math.Clamp(120 + extraDarkness, 0, 215);
        Color edgeColor = new(0, 0, 0, alpha);
        Color clearColor = new(0, 0, 0, 0);

        Raylib.DrawRectangleGradientV(0, 0, ScreenWidth, 115, edgeColor, clearColor);
        Raylib.DrawRectangleGradientV(0, ScreenHeight - 115, ScreenWidth, 115, clearColor, edgeColor);
        Raylib.DrawRectangleGradientH(0, 0, 150, ScreenHeight, edgeColor, clearColor);
        Raylib.DrawRectangleGradientH(ScreenWidth - 150, 0, 150, ScreenHeight, clearColor, edgeColor);
    }

    static void DrawDialogueBox()
    {
        int boxX = 110, boxW = ScreenWidth - 220;
        float textMaxW = boxW - 55;
        int charsShown = (int)visibleChars;
        bool textFinished = charsShown >= fullText.Length;
        string currentText = fullText[..charsShown];
        List<string> wrappedMainText = WrapViText(currentText, textMaxW, 22);

        int lineCount = Math.Max(1, wrappedMainText.Count);
        int mainTextHeight = lineCount * 30;
        int optionsCount = (textFinished && dialogueStage == 1) ? currentOptions.Length : 1;
        int boxH = Math.Max(175, 55 + mainTextHeight + optionsCount * 34 + 35);
        int boxY = ScreenHeight - boxH - 25;

        if (!string.IsNullOrEmpty(currentSpeaker))
        {
            float speakerWidth = Raylib.MeasureTextEx(viFont, currentSpeaker, 21, 1.0f).X;
            int tagW = (int)speakerWidth + 36;
            int tagH = 36;
            int tagY = boxY - tagH + 1;

            Raylib.DrawRectangle(boxX, tagY, tagW, tagH, new Color(12, 12, 16, 245));
            Raylib.DrawRectangleLines(boxX, tagY, tagW, tagH, new Color(110, 110, 120, 255));

            Color speakerColor = currentSpeaker switch
            {
                "Tú" => Color.SkyBlue,
                "Dũng" or "Điện thoại (Dũng)" or "Tin nhắn (Dũng)" => Color.Orange,
                "Đen" => new Color(130, 220, 155, 255),
                "Hưng" => new Color(95, 225, 210, 255),
                "Cô gái (Áo mưa vàng)" => Color.Gold,
                "Bóng ma ngoài kính" => new Color(248, 113, 113, 255),
                "Sổ giao ca" or "Áp phích ngã tư" => new Color(240, 205, 115, 255),
                _ => Color.RayWhite
            };
            DrawViText(currentSpeaker, boxX + 18, tagY + 7, 21, speakerColor);
        }

        Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(6, 6, 9, 238));
        Raylib.DrawRectangleLines(boxX, boxY, boxW, boxH, new Color(110, 110, 120, 255));

        for (int i = 0; i < wrappedMainText.Count; i++)
        {
            DrawViText(wrappedMainText[i], boxX + 25, boxY + 20 + i * 30, 22, Color.RayWhite);
        }

        int lastLineIdx = wrappedMainText.Count > 0 ? wrappedMainText.Count - 1 : 0;
        string lastLineText = wrappedMainText.Count > 0 ? wrappedMainText[lastLineIdx] : "";
        float lastLineWidth = Raylib.MeasureTextEx(viFont, lastLineText, 22, 1.0f).X;
        int cursorX = boxX + 25 + (int)lastLineWidth + 4;
        int cursorY = boxY + 20 + lastLineIdx * 30 + 3;

        bool showCursor = !textFinished || (MathF.Sin((float)Raylib.GetTime() * 7.5f) > 0f);
        if (showCursor)
        {
            Raylib.DrawRectangle(cursorX, cursorY, 11, 20, Color.RayWhite);
        }

        if (textFinished)
        {
            int optionsStartY = boxY + 28 + mainTextHeight;
            if (dialogueStage == 1)
            {
                for (int i = 0; i < currentOptions.Length; i++)
                {
                    bool isSelected = (i == selectedOption);
                    Color optionColor = isSelected ? Color.Yellow : new Color(195, 170, 75, 255);
                    string prefix = isSelected ? "> " : "   ";
                    DrawViText(prefix + currentOptions[i], boxX + 28, optionsStartY + i * 34, 21, optionColor);
                }
                DrawViText("[Lên/Xuống] Chọn   [Enter] Xác nhận", boxX + boxW - 315, boxY + boxH - 28, 18, Color.Gray);
            }
            else if (dialogueStage == 2)
            {
                DrawViText("> [Enter] Tiếp tục", boxX + 28, optionsStartY + 6, 21, Color.Yellow);
            }
        }
    }

    static float pauseFadeAnim = 0f;
    static float masterVolumeLevel = 0.85f;
    static bool pauseJustOpened = false;

    static void TogglePauseMenu(bool pause)
    {
        isPaused = pause;
        pauseSelection = 0;

        if (isPaused)
        {
            pauseFadeAnim = 0f;
            pauseJustOpened = true;
            Raylib.EnableCursor();
            Raylib.PauseSound(cafeAmbience);
            Raylib.PauseSound(motorbikeEngineSound);
            Raylib.PauseSound(ambientHumSound);
            Raylib.SetSoundPitch(voiceStaticSound, 0.85f);
            Raylib.PlaySound(voiceStaticSound);
        }
        else
        {
            pauseJustOpened = false;
            if (gameScene >= 0)
            {
                Raylib.DisableCursor();
                Raylib.GetMouseDelta();
            }
            if (gameScene == 0 && cafeSubStep < 2) Raylib.ResumeSound(cafeAmbience);
            if (gameScene == 1) Raylib.ResumeSound(motorbikeEngineSound);
            if (gameScene == -1 || gameScene == 2) Raylib.ResumeSound(ambientHumSound);

            Raylib.SetSoundPitch(voiceStaticSound, 1.15f);
            Raylib.PlaySound(voiceStaticSound);
        }
    }

    static bool UpdateAndDrawPauseMenu()
    {
        float dt = Math.Min(Raylib.GetFrameTime(), 0.05f);
        pauseFadeAnim = Math.Min(1f, pauseFadeAnim + dt * 8.0f);

        if (!pauseJustOpened && Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            TogglePauseMenu(false);
            return false;
        }
        pauseJustOpened = false;

        string[] menuItems =
        [
            "TIẾP TỤC CA TRỰC",
            "CHƠI LẠI TỪ ĐẦU",
            "VỀ MÀN HÌNH CHÍNH",
            "THOÁT GAME"
        ];

        int panelW = Math.Min(560, ScreenWidth - 80);
        int panelH = 540;
        int panelX = (ScreenWidth - panelW) / 2;
        int panelY = (ScreenHeight - panelH) / 2;
        Vector2 mouse = Raylib.GetMousePosition();

        // Điều hướng bàn phím
        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            pauseSelection = (pauseSelection + 1) % menuItems.Length;
            Raylib.SetSoundPitch(voiceStaticSound, 1.25f);
            Raylib.PlaySound(voiceStaticSound);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            pauseSelection = (pauseSelection - 1 + menuItems.Length) % menuItems.Length;
            Raylib.SetSoundPitch(voiceStaticSound, 1.25f);
            Raylib.PlaySound(voiceStaticSound);
        }

        // Chỉnh âm lượng nhanh bằng A/D hoặc Trái/Phải
        if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A))
        {
            masterVolumeLevel = Math.Max(0f, MathF.Round((masterVolumeLevel - 0.1f) * 10f) / 10f);
            Raylib.SetMasterVolume(masterVolumeLevel);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D))
        {
            masterVolumeLevel = Math.Min(1f, MathF.Round((masterVolumeLevel + 0.1f) * 10f) / 10f);
            Raylib.SetMasterVolume(masterVolumeLevel);
        }

        int btnStartY = panelY + 215;
        int btnH = 48;
        int btnGap = 12;
        int btnX = panelX + 36;
        int btnW = panelW - 72;

        bool triggerAction = Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space);

        for (int i = 0; i < menuItems.Length; i++)
        {
            Rectangle btnRect = new(btnX, btnStartY + i * (btnH + btnGap), btnW, btnH);
            if (Raylib.CheckCollisionPointRec(mouse, btnRect))
            {
                if (pauseSelection != i)
                {
                    pauseSelection = i;
                    Raylib.SetSoundPitch(voiceStaticSound, 1.25f);
                    Raylib.PlaySound(voiceStaticSound);
                }
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    triggerAction = true;
                }
            }
        }

        // Kéo thanh trượt âm lượng bằng chuột
        int sliderY = btnStartY + menuItems.Length * (btnH + btnGap) + 22;
        Rectangle sliderHitRect = new(btnX + 150, sliderY - 8, btnW - 210, 26);
        if (Raylib.IsMouseButtonDown(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, sliderHitRect))
        {
            masterVolumeLevel = Math.Clamp((mouse.X - sliderHitRect.X) / sliderHitRect.Width, 0f, 1f);
            Raylib.SetMasterVolume(masterVolumeLevel);
        }

        if (triggerAction)
        {
            if (pauseSelection == 0)
            {
                TogglePauseMenu(false);
                return false;
            }
            else if (pauseSelection == 1)
            {
                isPaused = false;
                ResetGame();
                Raylib.EnableCursor();
                return false;
            }
            else if (pauseSelection == 2)
            {
                isPaused = false;
                ReturnToHome();
                return false;
            }
            else if (pauseSelection == 3)
            {
                return true;
            }
        }

        // ================= VẼ GIAO DIỆN PAUSE =================
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(8, 9, 13, 255));

        byte bgAlpha = (byte)(215 * pauseFadeAnim);
        Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color((byte)6, (byte)7, (byte)11, bgAlpha));

        for (int y = 0; y < ScreenHeight; y += 4)
        {
            Raylib.DrawLine(0, y, ScreenWidth, y, new Color(255, 255, 255, 6));
        }

        Rectangle shadowRect = new(panelX + 6, panelY + 10, panelW, panelH);
        Raylib.DrawRectangleRounded(shadowRect, 0.03f, 12, new Color(0, 0, 0, 180));

        Rectangle cardRect = new(panelX, panelY, panelW, panelH);
        Raylib.DrawRectangleRounded(cardRect, 0.03f, 12, new Color(18, 20, 27, 248));
        Raylib.DrawRectangleRoundedLinesEx(cardRect, 0.03f, 12, 1.5f, new Color(75, 82, 98, 220));

        bool blink = MathF.Sin((float)Raylib.GetTime() * 4.5f) > 0f;
        Raylib.DrawCircle(panelX + 36, panelY + 34, 5.5f, blink ? new Color(235, 52, 52, 255) : new Color(110, 25, 25, 255));
        DrawViText("PAUSED  //  THUẬN PHÁT 24H", panelX + 50, panelY + 25, 16, new Color(155, 162, 178, 230));

        string title = "TẠM DỪNG CA TRỰC";
        int titleW = (int)Raylib.MeasureTextEx(viFont, title, 30, 1.2f).X;
        DrawViText(title, panelX + (panelW - titleW) / 2, panelY + 54, 30, Color.RayWhite);

        // Bảng trạng thái ca trực hiện tại
        Rectangle infoRect = new(btnX, panelY + 98, btnW, 96);
        Raylib.DrawRectangleRounded(infoRect, 0.08f, 8, new Color(26, 29, 38, 255));
        Raylib.DrawRectangleRoundedLinesEx(infoRect, 0.08f, 8, 1f, new Color(55, 60, 74, 200));

        string sceneLabel = gameScene switch
        {
            -1 => "17:15  •  Tin nhắn trên điện thoại",
            0 => "17:40  •  Quán cà phê chiều thứ Sáu",
            1 => "18:20  •  Trên đường tới cuối đường Số 9",
            3 => "18:45  •  Trước cửa tiệm Thuận Phát 24H",
            _ => $"{currentTime}  •  Cửa hàng tiện lợi Thuận Phát 24H"
        };
        DrawViText(sceneLabel, btnX + 16, panelY + 108, 17, new Color(234, 179, 8, 255));

        string rawObjText = gameScene == 2 ? $"Mục tiêu: {currentObjective}" : "Chương 1: Ca Đêm Đường Số 9";
        string objText = FitViTextSingleLine(rawObjText, btnW - 32, 16);
        DrawViText(objText, btnX + 16, panelY + 134, 16, new Color(210, 215, 226, 235));

        string recStatus = recordingOn ? "BẬT" : "TẮT";
        Color recColor = recordingOn ? new Color(74, 222, 128, 255) : new Color(248, 113, 113, 255);
        DrawViText($"Ghi âm: {recStatus}", btnX + 16, panelY + 162, 16, recColor);

        string clueStatus = $"Manh mối: {GetClueCount()}/5   •   Cửa chính: {(isDoorLocked ? "ĐÃ KHÓA" : "MỞ")}";
        int clueW = (int)Raylib.MeasureTextEx(viFont, clueStatus, 16, 1f).X;
        DrawViText(clueStatus, btnX + btnW - clueW - 16, panelY + 162, 16, new Color(165, 174, 192, 235));

        for (int i = 0; i < menuItems.Length; i++)
        {
            bool selected = (i == pauseSelection);
            bool isQuitBtn = (i == menuItems.Length - 1);
            Rectangle r = new(btnX, btnStartY + i * (btnH + btnGap), btnW, btnH);

            Color fillCol = selected
                ? (isQuitBtn ? new Color(185, 38, 38, 255) : new Color(235, 238, 245, 255))
                : new Color(28, 31, 41, 245);

            Color borderCol = selected
                ? (isQuitBtn ? new Color(248, 113, 113, 255) : Color.White)
                : new Color(62, 68, 84, 210);

            Color textCol = selected
                ? (isQuitBtn ? Color.White : new Color(15, 17, 22, 255))
                : new Color(220, 225, 235, 240);

            Raylib.DrawRectangleRounded(r, 0.16f, 8, fillCol);
            Raylib.DrawRectangleRoundedLinesEx(r, 0.16f, 8, 1.4f, borderCol);

            if (selected)
            {
                DrawViText("▸", btnX + 18, (int)r.Y + 13, 20, textCol);
            }

            int itemW = (int)Raylib.MeasureTextEx(viFont, menuItems[i], 20, 1.1f).X;
            DrawViText(menuItems[i], btnX + (btnW - itemW) / 2, (int)r.Y + 13, 20, textCol);
        }

        // Thanh trượt âm lượng tổng
        DrawViText("Âm lượng tổng:", btnX + 4, sliderY - 3, 17, new Color(185, 192, 208, 240));
        int trackX = btnX + 150;
        int trackW = btnW - 210;
        Raylib.DrawRectangleRounded(new Rectangle(trackX, sliderY + 3, trackW, 6), 0.5f, 4, new Color(48, 53, 66, 255));
        Raylib.DrawRectangleRounded(new Rectangle(trackX, sliderY + 3, (int)(trackW * masterVolumeLevel), 6), 0.5f, 4, new Color(234, 179, 8, 255));
        Raylib.DrawCircle((int)(trackX + trackW * masterVolumeLevel), sliderY + 6, 7.5f, Color.RayWhite);

        string volPct = $"{(int)MathF.Round(masterVolumeLevel * 100f)}%";
        DrawViText(volPct, trackX + trackW + 14, sliderY - 3, 17, Color.RayWhite);

        string hint = "[W/S] Di chuyển  •  [A/D] Âm lượng  •  [Enter / Click] Chọn  •  [Esc] Tiếp tục";
        int hintW = (int)Raylib.MeasureTextEx(viFont, hint, 14, 1f).X;
        DrawViText(hint, panelX + (panelW - hintW) / 2, panelY + panelH - 28, 14, new Color(130, 138, 155, 220));

        DrawVignette(65);
        Raylib.EndDrawing();
        return false;
    }

    static void UpdateAndDrawEndingScreen(float dt)
    {
        Raylib.StopSound(ambientHumSound);
        if (cameraShakeTimer > 0f) cameraShakeTimer = Math.Max(0f, cameraShakeTimer - dt);

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) ||
            Raylib.IsKeyPressed(KeyboardKey.Space) ||
            Raylib.IsKeyPressed(KeyboardKey.R))
        {
            ReturnToHome();
        }

        int shakeX = cameraShakeTimer > 0f ? rng.Next(-10, 11) : 0;
        int shakeY = cameraShakeTimer > 0f ? rng.Next(-10, 11) : 0;

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        DrawViText(endingTitle, 140 + shakeX, 160 + shakeY, 38, isBadEnding ? Color.Red : Color.Yellow);

        string[] paragraphs = endingText.Split('\n');
        int currentY = 240 + shakeY;
        foreach (string p in paragraphs)
        {
            var wrapped = WrapViText(p, ScreenWidth - 280, 23);
            foreach (string line in wrapped)
            {
                DrawViText(line, 140 + shakeX, currentY, 23, Color.RayWhite);
                currentY += 35;
            }
            currentY += 10;
        }

        DrawViText("Nhấn [Enter/Space/R] để quay lại màn hình chính", 140, ScreenHeight - 90, 22, Color.Gray);
        Raylib.EndDrawing();
    }
}