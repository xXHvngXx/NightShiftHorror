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

    static bool UpdateAndDrawPauseMenu()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            pauseSelection = (pauseSelection + 1) % pauseOptions.Length;
            Raylib.SetSoundPitch(voiceStaticSound, 1.3f);
            Raylib.PlaySound(voiceStaticSound);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            pauseSelection = (pauseSelection - 1 + pauseOptions.Length) % pauseOptions.Length;
            Raylib.SetSoundPitch(voiceStaticSound, 1.3f);
            Raylib.PlaySound(voiceStaticSound);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            if (pauseSelection == 0)
            {
                isPaused = false;
                Raylib.DisableCursor();
                Raylib.GetMouseDelta();
                if (gameScene == 0 && cafeSubStep < 2) Raylib.ResumeSound(cafeAmbience);
            }
            else if (pauseSelection == 1)
            {
                ResetGame();
            }
            else
            {
                return true;
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(10, 10, 14, 255));
        DrawVignette(40);
        DrawViText("TẠM DỪNG", ScreenWidth / 2 - 90, 190, 36, Color.Yellow);

        for (int i = 0; i < pauseOptions.Length; i++)
        {
            bool sel = i == pauseSelection;
            DrawViText((sel ? "> " : "   ") + pauseOptions[i], ScreenWidth / 2 - 125, 280 + i * 44, 24, sel ? Color.Yellow : Color.RayWhite);
        }

        DrawViText("[Esc] Quay lại   |   [Lên/Xuống] Chọn   |   [Enter] Xác nhận", ScreenWidth / 2 - 265, ScreenHeight - 90, 18, Color.Gray);
        Raylib.EndDrawing();
        return false;
    }

    static void UpdateAndDrawEndingScreen(float dt)
    {
        Raylib.StopSound(ambientHumSound);
        if (cameraShakeTimer > 0f) cameraShakeTimer = Math.Max(0f, cameraShakeTimer - dt);

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            ResetGame();
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

        DrawViText("Bấm [R] để chơi lại từ đầu", 140, ScreenHeight - 90, 22, Color.Gray);
        Raylib.EndDrawing();
    }
}