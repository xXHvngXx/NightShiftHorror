using System.Numerics;
using Raylib_cs;

partial class Program
{
    static int homeSelection = 0;
    static int homeSubMenu = 0; // 0: Menu chính, 1: Danh sách Episodes, 2: Credits
    static float homeOrbitAngle = 0f;
    static float homePanAngle = 0f;
    static readonly float[] menuHoverAnim = new float[5];

    static bool DrawHomeScreen(float dt)
    {
        homeOrbitAngle += dt * 0.22f;
        camera.Position = new Vector3(
            -1.2f + MathF.Sin(homeOrbitAngle) * 3.8f,
            2.35f + MathF.Sin(homeOrbitAngle * 0.65f) * 0.18f,
            14.8f + MathF.Cos(homeOrbitAngle) * 0.8f
        );
        camera.Target = new Vector3(-1.8f, 1.85f, 3.5f);
        camera.Up = Vector3.UnitY;
        camera.FovY = 48f;

        Vector2 mousePos = Raylib.GetMousePosition();

        // Tọa độ tự động co giãn theo mọi độ phân giải màn hình
        int titleX = Math.Max(72, (int)(ScreenWidth * 0.075f));
        int titleY = (int)(ScreenHeight * 0.42f);
        int menuX = ScreenWidth - Math.Max(360, (int)(ScreenWidth * 0.28f));
        int menuStartY = titleY;
        const int MenuStepY = 54;
        const int MenuHitW = 340;
        const int MenuHitH = 44;

        if (homeSubMenu == 0)
        {
            string[] mainItems = ["Episodes", "Credits", "Exit"];

            for (int i = 0; i < mainItems.Length; i++)
            {
                Rectangle hitBox = new(menuX - 16, menuStartY + i * MenuStepY - 6, MenuHitW, MenuHitH);
                bool hovered = Raylib.CheckCollisionPointRec(mousePos, hitBox);

                if (hovered)
                {
                    if (homeSelection != i)
                    {
                        homeSelection = i;
                        Raylib.SetSoundPitch(voiceStaticSound, 1.35f);
                        Raylib.PlaySound(voiceStaticSound);
                    }

                    if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        if (i == 0) { homeSubMenu = 1; homeSelection = 0; }
                        else if (i == 1) { homeSubMenu = 2; }
                        else return true;
                    }
                }

                float targetAnim = (homeSelection == i) ? 1f : 0f;
                menuHoverAnim[i] += (targetAnim - menuHoverAnim[i]) * Math.Min(1f, dt * 14f);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
            {
                homeSelection = (homeSelection + 1) % mainItems.Length;
                Raylib.SetSoundPitch(voiceStaticSound, 1.35f);
                Raylib.PlaySound(voiceStaticSound);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
            {
                homeSelection = (homeSelection - 1 + mainItems.Length) % mainItems.Length;
                Raylib.SetSoundPitch(voiceStaticSound, 1.35f);
                Raylib.PlaySound(voiceStaticSound);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                if (homeSelection == 0) { homeSubMenu = 1; homeSelection = 0; }
                else if (homeSelection == 1) { homeSubMenu = 2; }
                else return true;
            }
        }
        else if (homeSubMenu == 1)
        {
            (string Title, string Sub, bool Unlocked)[] chapters =
            [
                ("Chapter 1", "Ca Trực Đêm", true),
                ("Chapter 2", "Chuyến Xe Cuối", false),
                ("Chapter 3", "Khu Trọ Vắng", false),
                ("Back", "", true)
            ];

            for (int i = 0; i < chapters.Length; i++)
            {
                int itemY = menuStartY - 35 + i * 66;
                Rectangle hitBox = new(menuX - 16, itemY - 6, MenuHitW + 30, 56);

                if (chapters[i].Unlocked && Raylib.CheckCollisionPointRec(mousePos, hitBox))
                {
                    if (homeSelection != i)
                    {
                        homeSelection = i;
                        Raylib.SetSoundPitch(voiceStaticSound, 1.35f);
                        Raylib.PlaySound(voiceStaticSound);
                    }

                    if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        if (i == 0)
                        {
                            StartGameFromHome();
                        }
                        else if (i == 3)
                        {
                            homeSubMenu = 0;
                            homeSelection = 0;
                        }
                    }
                }

                float targetAnim = (chapters[i].Unlocked && homeSelection == i) ? 1f : 0f;
                menuHoverAnim[i] += (targetAnim - menuHoverAnim[i]) * Math.Min(1f, dt * 14f);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S) ||
                Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
            {
                homeSelection = (homeSelection == 0) ? 3 : 0;
                Raylib.SetSoundPitch(voiceStaticSound, 1.35f);
                Raylib.PlaySound(voiceStaticSound);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                if (homeSelection == 0) StartGameFromHome();
                else { homeSubMenu = 0; homeSelection = 0; }
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsMouseButtonPressed(MouseButton.Right))
            {
                homeSubMenu = 0;
                homeSelection = 0;
            }
        }
        else if (homeSubMenu == 2)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Enter) ||
                Raylib.IsMouseButtonPressed(MouseButton.Left) || Raylib.IsMouseButtonPressed(MouseButton.Right))
            {
                homeSubMenu = 0;
                homeSelection = 1;
            }
        }

        // ================= RENDER =================
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(10, 12, 18, 255));

        Raylib.BeginMode3D(camera);
        Props.DrawStore(lit: true, doorLocked: false, (float)Raylib.GetTime(), "23:48", phoneRinging: false, daylight: 0f);
        DrawStoreShelves(lit: true);
        Props.DrawStoreGlass(doorLocked: false, daylight: 0f);
        Raylib.EndMode3D();

        Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(6, 8, 14, 105));
        int gradW = (int)(ScreenWidth * 0.48f);
        Raylib.DrawRectangleGradientH(ScreenWidth - gradW, 0, gradW, ScreenHeight,
            new Color(5, 7, 12, 0), new Color(5, 7, 12, 238));
        DrawVignette(75);

        DrawViText("AN EPISODIC HORROR", titleX + 2, titleY - 26, 17, new Color(225, 228, 235, 230));

        string mainTitle = "NIGHT SHIFT HORROR";
        DrawViText(mainTitle, titleX - 2, titleY, 54, new Color(235, 45, 95, 70));
        DrawViText(mainTitle, titleX + 2, titleY + 2, 54, new Color(180, 20, 60, 90));
        DrawViText(mainTitle, titleX, titleY, 54, new Color(252, 68, 112, 255));

        string subTitle = "CA TRỰC ĐÊM";
        int titleW = (int)Raylib.MeasureTextEx(viFont, mainTitle, 54, 1f).X;
        int subW = (int)Raylib.MeasureTextEx(viFont, subTitle, 23, 1f).X;
        DrawViText(subTitle, titleX + titleW - subW, titleY + 58, 23, new Color(245, 212, 66, 255));

        DrawViText("v1.3", titleX + 2, titleY + 195, 18, new Color(200, 205, 212, 215));
        DrawViText("Một ca trực đêm tại cửa hàng cuối đường Số 9.", titleX + 2, ScreenHeight - 115, 18, new Color(215, 218, 222, 225));
        DrawViText("Đừng mở cửa sau 03:00 sáng.", titleX + 2, ScreenHeight - 88, 19, new Color(240, 240, 245, 245));

        if (homeSubMenu == 0)
        {
            string[] mainItems = ["Episodes", "Credits", "Exit"];
            for (int i = 0; i < mainItems.Length; i++)
            {
                int y = menuStartY + i * MenuStepY;
                float anim = menuHoverAnim[i];
                int offsetX = (int)(anim * 12f);

                if (anim > 0.02f)
                {
                    Raylib.DrawRectangle(menuX - 8, y + 6, 3, 24, new Color(252, 68, 112, (int)(anim * 255)));
                    DrawViText(mainItems[i], menuX + offsetX, y, 31, new Color(252, 68, 112, (int)(anim * 110)));
                }

                Color textCol = LerpCol(new Color(218, 222, 228, 235), Color.White, anim);
                DrawViText(mainItems[i], menuX + offsetX, y, 31, textCol);
            }
        }
        else if (homeSubMenu == 1)
        {
            DrawViText("SELECT EPISODE", menuX, menuStartY - 72, 16, new Color(245, 212, 66, 235));

            (string Title, string Sub, bool Unlocked)[] chapters =
            [
                ("Chapter 1", "Ca Trực Đêm", true),
                ("Chapter 2", "Chuyến Xe Cuối", false),
                ("Chapter 3", "Khu Trọ Vắng", false),
                ("Back", "", true)
            ];

            for (int i = 0; i < chapters.Length; i++)
            {
                int y = menuStartY - 35 + i * 66;
                var ch = chapters[i];

                if (!ch.Unlocked)
                {
                    DrawViText(ch.Title, menuX, y, 27, new Color(185, 192, 204, 155));
                    if (!string.IsNullOrEmpty(ch.Sub))
                    {
                        DrawViText($"{ch.Sub}  (Locked)", menuX, y + 29, 16, new Color(165, 172, 184, 130));
                    }
                }
                else
                {
                    float anim = menuHoverAnim[i];
                    int offsetX = (int)(anim * 12f);

                    if (anim > 0.02f)
                    {
                        Raylib.DrawRectangle(menuX - 8, y + 4, 3, ch.Sub != "" ? 38 : 24,
                            new Color(252, 68, 112, (int)(anim * 255)));
                        DrawViText(ch.Title, menuX + offsetX, y, 28, new Color(252, 68, 112, (int)(anim * 100)));
                    }

                    Color titleCol = LerpCol(new Color(225, 230, 236, 245), Color.White, anim);
                    DrawViText(ch.Title, menuX + offsetX, y, 28, titleCol);

                    if (!string.IsNullOrEmpty(ch.Sub))
                    {
                        Color subCol = LerpCol(new Color(220, 190, 70, 220), new Color(255, 222, 80, 255), anim);
                        DrawViText(ch.Sub, menuX + offsetX, y + 29, 17, subCol);
                    }
                }
            }
        }
        else if (homeSubMenu == 2)
        {
            DrawViText("CREDITS", menuX, menuStartY - 50, 26, new Color(252, 68, 112, 255));
            DrawViText("Game Design & Code", menuX, menuStartY, 17, new Color(245, 212, 66, 230));
            DrawViText("Minh Hưng & Tiến Dũng", menuX, menuStartY + 22, 19, Color.RayWhite);

            DrawViText("Engine & Audio", menuX, menuStartY + 62, 17, new Color(245, 212, 66, 230));
            DrawViText("C# (.NET 9.0) • Raylib-cs", menuX, menuStartY + 84, 19, Color.RayWhite);

            DrawViText("3D Assets Attribution", menuX, menuStartY + 124, 17, new Color(245, 212, 66, 230));
            DrawViText("Grocery Shelf by Palmart Productions", menuX, menuStartY + 146, 17, new Color(210, 215, 222, 230));

            DrawViText("[Click hoặc ESC để quay lại]", menuX, menuStartY + 205, 16, new Color(175, 180, 190, 220));
        }

        Raylib.EndDrawing();
        return false;
    }

    static Color LerpCol(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }

    static void StartGameFromHome()
    {
        gameScene = -1;
        homeSubMenu = 0;
        phoneNotifStep = 0;
        phoneStepTimer = 0.7f;
        phoneChoiceStage = 0;
        phoneSelectedOption = 0;
        phoneEndHoldTimer = 0f;
        phoneSentReply = "";
        fadeAlpha = 1f;
        homePanAngle = 0f;
        Raylib.DisableCursor();
        Raylib.GetMouseDelta();
    }

    static void ReturnToHome()
    {
        ResetGame();
        gameScene = -2;
        homeSubMenu = 0;
        homeSelection = 0;
        homePanAngle = 0f;
        Raylib.EnableCursor();
    }
}