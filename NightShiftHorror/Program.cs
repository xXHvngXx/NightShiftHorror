using System.Numerics;
using System.Text;
using Raylib_cs;

const int ScreenWidth = 1280;
const int ScreenHeight = 720;
Raylib.InitWindow(ScreenWidth, ScreenHeight, "Ca Trực Đêm Của Tú - Indie Horror");
Raylib.SetExitKey(KeyboardKey.Null);
Raylib.InitAudioDevice();
Raylib.SetTargetFPS(60);
Raylib.DisableCursor();

Props.LoadFaceTextures();

string viChars =
    " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
    "ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯư" +
    "ẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹ";

int[] codepoints = viChars.Select(c => (int)c).Distinct().ToArray();
Font viFont = Raylib.LoadFontEx(@"C:\Windows\Fonts\segoeui.ttf", 26, codepoints, codepoints.Length);
Raylib.SetTextureFilter(viFont.Texture, TextureFilter.Bilinear);

Sound footstepSound = CreateFootstepSound();
Sound voiceStaticSound = CreateStaticBlipSound();
Sound phoneRingSound = CreatePhoneRingSound();
Sound knockSound = CreateKnockSound();
Sound scareSound = CreateScareSound();
Sound ambientHumSound = CreateAmbientHumSound();
Sound heartbeatSound = CreateHeartbeatSound();

Raylib.SetSoundVolume(footstepSound, 0.75f);
Raylib.SetSoundVolume(voiceStaticSound, 0.35f);
Raylib.SetSoundVolume(phoneRingSound, 0.55f);
Raylib.SetSoundVolume(knockSound, 0.85f);
Raylib.SetSoundVolume(scareSound, 0.85f);
Raylib.SetSoundVolume(ambientHumSound, 0.22f);
Raylib.SetSoundVolume(heartbeatSound, 0.65f);

const float NormalFov = 65.0f;
const float TalkNpcFov = 48.0f;

Vector3 cafeSeatPos = new(0.0f, 1.32f, -1.15f);
Vector3 playerPos = new(-1.2f, 0f, -3.5f);
float camYaw = 0f;
float camPitch = -0.05f;
float currentBobOffset = 0f;

Camera3D camera = new()
{
    Position = cafeSeatPos,
    Target = new Vector3(0.0f, 1.25f, 0.8f),
    Up = new Vector3(0.0f, 1.0f, 0.0f),
    FovY = NormalFov,
    Projection = CameraProjection.Perspective
};

List<BoundingBox> solidObjects =
[
    new(new Vector3(-3.2f, 0f, -2.6f), new Vector3(0.4f, 1.1f, -1.7f)),
    new(new Vector3(-3.2f, 0f, -4.8f), new Vector3(-2.3f, 1.1f, -1.7f)),
    new(new Vector3(1.8f, 0f, -2.5f), new Vector3(2.5f, 2.0f, 1.8f)),
    new(new Vector3(3.4f, 0f, -2.5f), new Vector3(4.1f, 2.0f, 1.8f)),
    new(new Vector3(1.5f, 0f, -4.9f), new Vector3(4.3f, 2.4f, -4.1f))
];

BoundingBox rulesPaperBox = new(new Vector3(-1.4f, 1.05f, -2.35f), new Vector3(-0.8f, 1.25f, -1.85f));
BoundingBox deskPhoneBox = new(new Vector3(-0.4f, 1.05f, -2.35f), new Vector3(0.1f, 1.35f, -1.85f));
BoundingBox frontDoorBox = new(new Vector3(-1.2f, 0.0f, 4.7f), new Vector3(1.2f, 2.5f, 5.3f));

float stepTimer = 0f;
float walkBobTimer = 0f;
const float StepInterval = 0.46f;
Random rng = new();

(string Speaker, string Text)[] cafeDialogue =
[
    ("Tú", "Haha! Trời ơi nhìn màn hình đi Dũng, nổ vàng rồi! Cuối cùng tao cũng roll trúng Mitya rồi anh em ơi, cười ha hả luôn!"),
    ("Dũng", "Xì, ông lại bịp anh em đúng không? Hôm trước mới thề sống thề chết là tích nguyên thạch nhịn roll cơ mà!"),
    ("Tú", "Thì tại nhìn Mitya cuốn quá, thèm chịu không nổi nên ngứa tay bấm kéo luôn chứ sao! May mà không lệch."),
    ("Dũng", "Cười cho đã đi rồi cuối tháng húp mì gói nhé bạn hiền. Tiền trọ tháng này ông xoay đủ chưa đấy?"),
    ("Tú", "Ờ... nhắc tới tiền trọ mới nhớ. Chắc tối nay tao phải kiếm kèo đi làm thêm ca đêm gấp thôi.")
];

string[] introMonologue =
[
    "Tiền trọ tháng này lại tăng, thêm tiền ăn uống, sinh hoạt đủ thứ... Mình không thể cứ ngửa tay xin tiền ba mẹ mãi được.",
    "May mà hôm qua lướt mạng thấy cửa hàng tiện lợi cuối khu đang tuyển người trực ca đêm từ 0 giờ đến 5 giờ sáng.",
    "Lương 500 nghìn một đêm, nhận tiền mặt ngay khi hết ca. Hơi khuya một chút nhưng ráng cày vài tuần là đủ trang trải.",
    "Tới giờ nhận ca rồi. Đọc qua tờ quy tắc đặt trên quầy thu ngân xem có gì cần lưu ý không nào..."
];

int gameScene = 0;
int cafeIndex = 0;
bool isFadingOutCafe = false;
int introIndex = 0;
float fadeAlpha = 1.0f;

bool isPaused = false;
int pauseSelection = 0;
string[] pauseOptions = ["Tiếp tục ca trực", "Chơi lại từ đầu", "Thoát trò chơi"];

int storyStep = 0;
float clockMinutes = 0f;
string currentTime = "00:00";
string currentObjective = "Đọc tờ quy tắc đặt trên quầy thu ngân";
bool isDoorLocked = false;
bool lightsOn = true;
bool noticedAnomaly = false;
float flickerTimer = 0f;
float phoneRingTimer = 0f;
float eventDelayTimer = 0f;
float heartbeatTimer = 0f;
float cameraShakeTimer = 0f;

bool npcActive = false;
Vector3 npcPos = new(0f, 0f, 11f);
Vector3 npcTarget = new(0f, 0f, 11f);
Color npcShirtColor = Color.Blue;
Color npcHairColor = new(35, 35, 40, 255);
bool npcLeaving = false;
float npcYaw = 180f, npcWalkPhase = 0f, npcSwing = 0f;
bool npcRaincoat = false, npcIsFemale = false;
FaceId npcFaceId = FaceId.None;

float npcGazeWeight = 0f;
float npcHeadYaw = 0f;
float npcHeadPitch = 0f;
float npcHeadRoll = 0f;
Vector2 npcEyeGaze = Vector2.Zero;

bool gameEnded = false;
bool isBadEnding = false;
string endingTitle = "";
string endingText = "";

bool isTalking = false;
bool isTalkingToNpc = false;
int dialogueStage = 0;
string currentSpeaker = "";
string fullText = "";
float visibleChars = 0f;
int lastSoundCharIndex = -1;
const float TypingSpeed = 36f;
string[] currentOptions = [];
int selectedOption = 0;
string activeInteractId = "";

void ApplyCameraOrientation(Vector3 eyePos)
{
    Vector3 lookDir = new(
        MathF.Sin(camYaw) * MathF.Cos(camPitch),
        MathF.Sin(camPitch),
        MathF.Cos(camYaw) * MathF.Cos(camPitch)
    );
    camera.Position = eyePos;
    camera.Target = eyePos + lookDir;
}

void UpdateFirstPersonLook(Vector3 eyePos)
{
    Vector2 mouseDelta = Raylib.GetMouseDelta();
    const float sensitivity = 0.0022f;
    camYaw -= mouseDelta.X * sensitivity;
    camPitch = Math.Clamp(camPitch - mouseDelta.Y * sensitivity, -1.42f, 1.42f);
    ApplyCameraOrientation(eyePos);
}

void StartDialogue(string speaker, string text, int stage, string[]? options = null, bool talkingToNpc = false)
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
}

float GetSpeakerPitch(string speaker) => speaker switch
{
    "Dũng" => 0.86f,
    "Ông cụ" => 0.78f,
    "Cô gái" => 1.18f,
    "Cô gái (Áo mưa vàng)" => 1.12f,
    "Người đàn ông lạ" => 0.72f,
    "Quản lý" => 0.90f,
    "Điện thoại bàn" => 0.65f,
    _ => 1.0f
};

void UpdateTypewriter(float deltaTime, float basePitch)
{
    if (visibleChars >= fullText.Length) return;

    visibleChars = Math.Min(fullText.Length, visibleChars + TypingSpeed * deltaTime);
    int currentIndex = (int)visibleChars;

    if (currentIndex > lastSoundCharIndex && currentIndex % 2 == 0)
    {
        char c = fullText[Math.Min(currentIndex, fullText.Length - 1)];
        if (!char.IsWhiteSpace(c))
        {
            Raylib.SetSoundPitch(voiceStaticSound, basePitch + (float)rng.NextDouble() * 0.1f);
            Raylib.PlaySound(voiceStaticSound);
        }
        lastSoundCharIndex = currentIndex;
    }
}

void DrawViText(string text, int x, int y, float fontSize, Color color)
{
    Raylib.DrawTextEx(viFont, text, new Vector2(x, y), fontSize, 1.0f, color);
}

List<string> WrapViText(string text, float maxWidth, float fontSize)
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

void DrawVignette(int extraDarkness = 0)
{
    int alpha = Math.Clamp(115 + extraDarkness, 0, 210);
    Color edgeColor = new(0, 0, 0, alpha);
    Color clearColor = new(0, 0, 0, 0);

    Raylib.DrawRectangleGradientV(0, 0, ScreenWidth, 110, edgeColor, clearColor);
    Raylib.DrawRectangleGradientV(0, ScreenHeight - 110, ScreenWidth, 110, clearColor, edgeColor);
    Raylib.DrawRectangleGradientH(0, 0, 140, ScreenHeight, edgeColor, clearColor);
    Raylib.DrawRectangleGradientH(ScreenWidth - 140, 0, 140, ScreenHeight, clearColor, edgeColor);
}

void DrawDialogueBox()
{
    int boxX = 110, boxW = ScreenWidth - 220;
    float textMaxW = boxW - 50;
    string currentText = fullText[..(int)visibleChars];
    List<string> wrappedMainText = WrapViText(currentText, textMaxW, 22);

    int mainTextHeight = Math.Max(1, wrappedMainText.Count) * 30;
    int optionsCount = ((int)visibleChars >= fullText.Length && dialogueStage == 1) ? currentOptions.Length : 1;
    int boxH = Math.Max(175, 55 + mainTextHeight + optionsCount * 34 + 35);
    int boxY = ScreenHeight - boxH - 25;

    if (!string.IsNullOrEmpty(currentSpeaker))
    {
        float speakerWidth = Raylib.MeasureTextEx(viFont, currentSpeaker, 21, 1.0f).X;
        int tagW = (int)speakerWidth + 36;
        int tagH = 36;
        int tagY = boxY - tagH + 1;

        Raylib.DrawRectangle(boxX, tagY, tagW, tagH, new Color(15, 15, 20, 245));
        Raylib.DrawRectangleLines(boxX, tagY, tagW, tagH, Color.Gray);

        Color speakerColor = currentSpeaker switch
        {
            "Tú" => Color.SkyBlue,
            "Dũng" => Color.Orange,
            "Cô gái (Áo mưa vàng)" => Color.Gold,
            _ => Color.Yellow
        };
        DrawViText(currentSpeaker, boxX + 18, tagY + 7, 21, speakerColor);
    }

    Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(0, 0, 0, 235));
    Raylib.DrawRectangleLines(boxX, boxY, boxW, boxH, Color.Gray);

    for (int i = 0; i < wrappedMainText.Count; i++)
    {
        DrawViText(wrappedMainText[i], boxX + 25, boxY + 20 + i * 30, 22, Color.RayWhite);
    }

    if ((int)visibleChars >= fullText.Length)
    {
        int optionsStartY = boxY + 28 + mainTextHeight;
        if (dialogueStage == 1)
        {
            for (int i = 0; i < currentOptions.Length; i++)
            {
                bool isSelected = (i == selectedOption);
                Color optionColor = isSelected ? Color.Yellow : Color.Gray;
                string prefix = isSelected ? "> " : "   ";
                DrawViText(prefix + currentOptions[i], boxX + 28, optionsStartY + i * 34, 21, optionColor);
            }
            DrawViText("[Lên/Xuống] Chọn   [Enter] Xác nhận", boxX + boxW - 315, boxY + boxH - 28, 18, Color.DarkGray);
        }
        else if (dialogueStage == 2)
        {
            DrawViText("> [Enter] Tiếp tục", boxX + 28, optionsStartY + 6, 21, Color.Yellow);
        }
    }
}

void SpawnNpc(Vector3 start, Vector3 target, Color shirtColor, Color hairColor,
              bool raincoat = false, bool isFemale = false, FaceId faceId = FaceId.None)
{
    npcPos = start;
    npcTarget = target;
    npcShirtColor = shirtColor;
    npcHairColor = hairColor;
    npcRaincoat = raincoat;
    npcIsFemale = isFemale;
    npcFaceId = faceId;
    Vector3 d = target - start;
    npcYaw = d.LengthSquared() > 0.01f ? MathF.Atan2(d.X, d.Z) * 180f / MathF.PI : 180f;
    npcLeaving = false;
    npcGazeWeight = 0f;
    npcHeadYaw = 0f;
    npcHeadPitch = 0f;
    npcHeadRoll = 0f;
    npcEyeGaze = Vector2.Zero;
    npcActive = true;
}

string FormatClock(float totalMinutes)
{
    int mins = (int)totalMinutes;
    int h = (mins / 60) % 24;
    int m = mins % 60;
    return $"{h:D2}:{m:D2}";
}

void ResetGame()
{
    camYaw = 0f;
    camPitch = -0.05f;
    currentBobOffset = 0f;
    camera.FovY = NormalFov;
    playerPos = new Vector3(-1.2f, 0f, -3.5f);
    UpdateFirstPersonLook(cafeSeatPos);

    gameScene = 0;
    cafeIndex = 0;
    isFadingOutCafe = false;
    introIndex = 0;
    fadeAlpha = 1.0f;
    isPaused = false;
    pauseSelection = 0;
    storyStep = 0;
    clockMinutes = 0f;
    currentTime = "00:00";
    currentObjective = "Đọc tờ quy tắc đặt trên quầy thu ngân";
    isDoorLocked = false;
    lightsOn = true;
    noticedAnomaly = false;
    flickerTimer = 0f;
    phoneRingTimer = 0f;
    eventDelayTimer = 0f;
    heartbeatTimer = 0f;
    cameraShakeTimer = 0f;
    npcActive = false;
    npcLeaving = false;
    npcFaceId = FaceId.None;
    npcGazeWeight = 0f;
    npcHeadYaw = 0f;
    npcHeadPitch = 0f;
    npcHeadRoll = 0f;
    npcEyeGaze = Vector2.Zero;
    gameEnded = false;
    isBadEnding = false;
    isTalking = false;
    isTalkingToNpc = false;
    dialogueStage = 0;
    Raylib.StopSound(ambientHumSound);
    Raylib.DisableCursor();
    StartDialogue(cafeDialogue[0].Speaker, cafeDialogue[0].Text, 2);
}

UpdateFirstPersonLook(cafeSeatPos);
StartDialogue(cafeDialogue[0].Speaker, cafeDialogue[0].Text, 2);

while (!Raylib.WindowShouldClose())
{
    float dt = Raylib.GetFrameTime();

    if (!gameEnded && Raylib.IsKeyPressed(KeyboardKey.Escape))
    {
        isPaused = !isPaused;
        pauseSelection = 0;
        if (isPaused) Raylib.EnableCursor();
        else
        {
            Raylib.DisableCursor();
            Raylib.GetMouseDelta();
        }
    }

    if (isPaused)
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
            }
            else if (pauseSelection == 1)
            {
                ResetGame();
            }
            else
            {
                break;
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(10, 10, 14, 255));
        DrawVignette(40);
        DrawViText("TẠM DỪNG", ScreenWidth / 2 - 90, 190, 36, Color.Yellow);

        for (int i = 0; i < pauseOptions.Length; i++)
        {
            bool sel = i == pauseSelection;
            DrawViText((sel ? "> " : "   ") + pauseOptions[i], ScreenWidth / 2 - 125, 280 + i * 44, 24, sel ? Color.RayWhite : Color.Gray);
        }

        DrawViText("[Esc] Quay lại   |   [Lên/Xuống] Chọn   |   [Enter] Xác nhận", ScreenWidth / 2 - 265, ScreenHeight - 90, 18, Color.DarkGray);
        Raylib.EndDrawing();
        continue;
    }

    if (gameScene == 0)
    {
        UpdateFirstPersonLook(cafeSeatPos);

        if (isFadingOutCafe)
        {
            fadeAlpha = Math.Min(1.0f, fadeAlpha + dt * 0.85f);
            if (fadeAlpha >= 1.0f)
            {
                gameScene = 1;
                isFadingOutCafe = false;
                fadeAlpha = 0f;
                introIndex = 0;
                StartDialogue("Tú", introMonologue[0], 2);
            }
        }
        else
        {
            if (fadeAlpha > 0f)
            {
                fadeAlpha = Math.Max(0f, fadeAlpha - dt * 0.7f);
            }

            UpdateTypewriter(dt, GetSpeakerPitch(currentSpeaker));
            bool textFinished = (int)visibleChars >= fullText.Length;

            if (!textFinished && (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.F)))
            {
                visibleChars = fullText.Length;
            }
            else if (textFinished && Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                cafeIndex++;
                if (cafeIndex < cafeDialogue.Length)
                {
                    StartDialogue(cafeDialogue[cafeIndex].Speaker, cafeDialogue[cafeIndex].Text, 2);
                }
                else
                {
                    isFadingOutCafe = true;
                }
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(28, 20, 16, 255));

        Raylib.BeginMode3D(camera);
        Props.DrawCafe((float)Raylib.GetTime(), camera.Position);
        Raylib.EndMode3D();

        DrawVignette();

        if (!isFadingOutCafe)
        {
            DrawDialogueBox();
        }
        if (fadeAlpha > 0f)
        {
            Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(0, 0, 0, (int)(fadeAlpha * 255)));
        }

        Raylib.EndDrawing();
        continue;
    }

    if (gameScene == 1)
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
                camPitch = -0.05f;
                currentBobOffset = 0f;
                camera.FovY = NormalFov;
                UpdateFirstPersonLook(new Vector3(playerPos.X, 1.7f, playerPos.Z));
                Raylib.GetMouseDelta();
                fadeAlpha = 1.0f;
                gameScene = 2;
                isTalking = false;
                isTalkingToNpc = false;
                dialogueStage = 0;
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        DrawDialogueBox();
        Raylib.EndDrawing();
        continue;
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
        DrawViText(endingTitle, 140 + shakeX, 170 + shakeY, 38, isBadEnding ? Color.Red : Color.Yellow);

        string[] paragraphs = endingText.Split('\n');
        int currentY = 250 + shakeY;
        foreach (string p in paragraphs)
        {
            var wrapped = WrapViText(p, ScreenWidth - 280, 24);
            foreach (string line in wrapped)
            {
                DrawViText(line, 140 + shakeX, currentY, 24, Color.RayWhite);
                currentY += 36;
            }
            currentY += 10;
        }

        DrawViText("Bấm [R] để chơi lại từ đầu", 140, ScreenHeight - 90, 22, Color.Gray);
        Raylib.EndDrawing();
        continue;
    }

    if (!isTalking)
    {
        float clockRate = storyStep == 7 ? 0.65f : 0.22f;
        float maxClockForStep = storyStep switch
        {
            0 => 28f,
            1 => 72f,
            2 => 118f,
            4 or 5 => 138f,
            6 => 165f,
            7 => 179f,
            8 or 9 => 295f,
            _ => 310f
        };
        if (clockMinutes < maxClockForStep)
        {
            clockMinutes = Math.Min(maxClockForStep, clockMinutes + dt * clockRate);
            currentTime = FormatClock(clockMinutes);
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
            clockMinutes = 140f;
            currentTime = FormatClock(clockMinutes);
            currentObjective = "Tiếp vị khách vừa bước vào cửa hàng";
            SpawnNpc(
                new Vector3(0f, 0f, 9.5f),
                new Vector3(-1.1f, 0f, -1.1f),
                new Color(45, 45, 55, 255),
                new Color(30, 30, 35, 255),
                faceId: FaceId.StrangeMan
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
            npcLeaving = false;

            if (storyStep == 1)
            {
                clockMinutes = 75f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "Tiếp vị khách tiếp theo vừa vào quán";
                SpawnNpc(
                    new Vector3(0f, 0f, 9.5f),
                    new Vector3(-1.1f, 0f, -1.1f),
                    new Color(215, 215, 225, 255),
                    new Color(40, 30, 25, 255),
                    isFemale: true,
                    faceId: FaceId.Girl
                );
                storyStep = 2;
            }
            else if (storyStep == 2)
            {
                clockMinutes = 120f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "Nghe điện thoại bàn đang reo trên quầy";
                flickerTimer = 1.2f;
                phoneRingTimer = 0.2f;
                Raylib.PlaySound(voiceStaticSound);
                storyStep = 4;
            }
            else if (storyStep == 6)
            {
                clockMinutes = 166f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "Ra cửa chính bấm [F] để khóa chốt trước 03:00";
                storyStep = 7;
            }
            else if (storyStep == 9)
            {
                clockMinutes = 300f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "Nhận tiền công từ quản lý và kết thúc ca trực";
                SpawnNpc(
                    new Vector3(0f, 0f, 9.5f),
                    new Vector3(-1.1f, 0f, -1.1f),
                    new Color(35, 110, 75, 255),
                    new Color(30, 30, 35, 255),
                    faceId: FaceId.Manager
                );
                storyStep = 10;
            }
        }
    }

    string interactPrompt = "";
    activeInteractId = "";

    if (!isTalking)
    {
        Vector3 oldPlayerPos = playerPos;

        Vector3 forwardFlat = new(MathF.Sin(camYaw), 0f, MathF.Cos(camYaw));
        Vector3 rightFlat = new(-MathF.Cos(camYaw), 0f, MathF.Sin(camYaw));

        Vector3 moveInput = Vector3.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) moveInput += forwardFlat;
        if (Raylib.IsKeyDown(KeyboardKey.S)) moveInput -= forwardFlat;
        if (Raylib.IsKeyDown(KeyboardKey.D)) moveInput += rightFlat;
        if (Raylib.IsKeyDown(KeyboardKey.A)) moveInput -= rightFlat;

        if (moveInput.LengthSquared() > 0.001f)
        {
            const float moveSpeed = 3.1f;
            playerPos += Vector3.Normalize(moveInput) * moveSpeed * dt;
        }

        float maxZ = isDoorLocked ? 4.4f : 11.5f;
        if (Math.Abs(playerPos.X) > 0.85f && playerPos.Z > 4.4f && oldPlayerPos.Z <= 4.4f)
        {
            playerPos.Z = 4.4f;
        }
        playerPos.X = Math.Clamp(playerPos.X, -4.4f, 4.4f);
        playerPos.Z = Math.Clamp(playerPos.Z, -4.4f, maxZ);

        foreach (var box in solidObjects)
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

        if (movedDist > 0.001f)
        {
            stepTimer += dt;
            walkBobTimer += dt * 12.0f;
            float targetBob = MathF.Sin(walkBobTimer) * 0.022f;
            currentBobOffset += (targetBob - currentBobOffset) * Math.Min(1f, dt * 16f);

            if (stepTimer >= StepInterval)
            {
                stepTimer = 0f;
                Raylib.SetSoundPitch(footstepSound, 0.92f + (float)rng.NextDouble() * 0.16f);
                Raylib.PlaySound(footstepSound);
            }
        }
        else
        {
            stepTimer = StepInterval * 0.8f;
            currentBobOffset += (0f - currentBobOffset) * Math.Min(1f, dt * 14f);
        }

        UpdateFirstPersonLook(new Vector3(playerPos.X, 1.7f + currentBobOffset, playerPos.Z));

        if (storyStep == 10 && playerPos.Z > 7.5f)
        {
            gameEnded = true;
            isBadEnding = false;
            endingTitle = "KẾT THÚC: CA TRỰC KHÔNG HỒI KẾT";
            string extraLine = noticedAnomaly
                ? "Tú nhớ lại ánh mắt cảnh báo của người đàn ông lạ lúc 02:20 và rùng mình nhận ra quán này vốn chưa từng mở cửa cho người sống."
                : "Tú bàng hoàng nhớ lại ông cụ hiền lành lúc đầu ca — hóa ra ngay từ vị khách đầu tiên, Tú đã không còn ở thế giới bình thường nữa.";

            endingText =
                "Tú cầm phong bì tiền công bước qua bên kia đường rồi quay đầu nhìn lại cửa hàng.\n" +
                "Cửa cuốn gỉ sét đóng im lìm, phủ đầy bụi bặm cùng tấm biển cũ nát: 'NGỪNG HOẠT ĐỘNG TỪ NĂM 2019'.\n" +
                extraLine + "\n" +
                "Tú run tay xé phong bì kiểm tra: bên trong chỉ có tờ hợp đồng ghi 'Nhân viên trực ca vĩnh viễn' đã ký sẵn tên Tú.";
        }

        Vector3 lookDir = Vector3.Normalize(camera.Target - camera.Position);
        Ray eyeRay = new(camera.Position, lookDir);

        RayCollision paperHit = Raylib.GetRayCollisionBox(eyeRay, rulesPaperBox);
        RayCollision phoneHit = Raylib.GetRayCollisionBox(eyeRay, deskPhoneBox);
        RayCollision doorHit = Raylib.GetRayCollisionBox(eyeRay, frontDoorBox);

        BoundingBox npcBox = new(npcPos - new Vector3(0.4f, 0f, 0.4f), npcPos + new Vector3(0.4f, 1.85f, 0.4f));
        RayCollision npcHit = npcActive ? Raylib.GetRayCollisionBox(eyeRay, npcBox) : new RayCollision { Hit = false };

        if (npcHit.Hit && npcHit.Distance <= 3.2f && !npcLeaving)
        {
            activeInteractId = "npc";
            interactPrompt = "[F] Nói chuyện";
        }
        else if (paperHit.Hit && paperHit.Distance <= 2.6f)
        {
            activeInteractId = "rules";
            interactPrompt = "[F] Đọc tờ quy tắc";
        }
        else if (phoneHit.Hit && phoneHit.Distance <= 2.6f && storyStep == 4)
        {
            activeInteractId = "phone";
            interactPrompt = "[F] Nghe điện thoại";
        }
        else if (doorHit.Hit && doorHit.Distance <= 2.8f && storyStep == 7 && !isDoorLocked)
        {
            activeInteractId = "door";
            interactPrompt = "[F] Khóa chốt cửa chính";
        }

        if (activeInteractId != "" && Raylib.IsKeyPressed(KeyboardKey.F))
        {
            if (activeInteractId == "rules")
            {
                StartDialogue(
                    "Tờ quy tắc ca đêm",
                    "1. Chào hỏi khách đàng hoàng trước khi tính tiền. 2. Tuyệt đối không bán hàng cho người mặc áo mưa VÀNG. 3. Đúng 03:00 khóa chặt cửa chính, không mở cho bất kỳ ai.",
                    2
                );
            }
            else if (activeInteractId == "phone")
            {
                StartDialogue(
                    "Điện thoại bàn",
                    "*Rè rè...* (Đầu dây bên kia không có ai nói, chỉ có tiếng bước chân cộp... cộp... vang lên khớp y hệt nhịp chân của Tú)",
                    2
                );
            }
            else if (activeInteractId == "door")
            {
                isDoorLocked = true;
                Raylib.PlaySound(knockSound);
                clockMinutes = 185f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "Có tiếng gõ cửa dồn dập! Ra kiểm tra cửa kính";
                SpawnNpc(
                    new Vector3(0f, 0f, 5.7f),
                    new Vector3(0f, 0f, 5.7f),
                    Color.Gold,
                    new Color(40, 30, 25, 255),
                    raincoat: true,
                    isFemale: true,
                    faceId: FaceId.YellowNormal
                );
                storyStep = 8;
                StartDialogue(
                    "Tú",
                    "Mình vừa gạt chốt khóa cửa thì tiếng đập cửa kính đã vang lên dồn dập: CỐC! CỐC! CỐC!",
                    2
                );
            }
            else if (activeInteractId == "npc")
            {
                if (storyStep == 1)
                {
                    StartDialogue(
                        "Ông cụ",
                        "Chào cháu, lấy cho ông hộp sữa tươi với ổ bánh mì nhé. Ông ở ngay con hẻm đối diện, tối nay khó ngủ nên đi bộ ra mua chút đồ ăn sáng.",
                        1,
                        [
                            "Dạ của ông hết 25 nghìn ạ. Ông cầm tiền thừa nhé.",
                            "Con hẻm đối diện tối lắm, ông đi bộ một mình cẩn thận xe cộ nha."
                        ],
                        talkingToNpc: true
                    );
                }
                else if (storyStep == 2)
                {
                    StartDialogue(
                        "Cô gái",
                        "Em ơi tính tiền giúp chị ly mì với chai nước suối nhé. Khu này dạo này vắng quá, nhất là từ sau vụ tai nạn ngoài ngã tư tuần trước...",
                        1,
                        [
                            "Dạ hết 20 nghìn ạ. Tuần trước ngoài ngã tư có vụ gì vậy chị?",
                            "Chị ở trọ gần đây à? Giờ này còn đi mua đồ ăn khuya."
                        ],
                        talkingToNpc: true
                    );
                }
                else if (storyStep == 6)
                {
                    StartDialogue(
                        "Người đàn ông lạ",
                        "Lấy cho tôi hộp sữa tươi với ổ bánh mì... Tôi ở ngay con hẻm đối diện, tối nay khó ngủ nên đi bộ ra mua...",
                        1,
                        [
                            "Chú... sao chú lại nói y hệt ông cụ lúc nãy?",
                            "Quán sắp đóng cửa kiểm kê rồi, chú thông cảm..."
                        ],
                        talkingToNpc: true
                    );
                }
                else if (storyStep == 8)
                {
                    StartDialogue(
                        "Cô gái (Áo mưa vàng)",
                        "Em ơi mở cửa nhanh giúp chị với! Lúc nãy chị làm rơi chùm chìa khóa phòng trọ ngay trước quầy thu ngân!",
                        1,
                        [
                            "Mở chốt cửa cho chị ấy vào tìm chìa khóa",
                            "Nói vọng qua kính: 'Trên quầy không có chìa khóa nào đâu chị'"
                        ],
                        talkingToNpc: true
                    );
                }
                else if (storyStep == 10)
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
    else
    {
        Raylib.GetMouseDelta();
        currentBobOffset += (0f - currentBobOffset) * (1f - MathF.Exp(-10f * dt));
        Vector3 eyePos = new(playerPos.X, 1.7f + currentBobOffset, playerPos.Z);

        if (isTalkingToNpc && npcActive)
        {
            Vector3 npcFacePos = npcPos + new Vector3(0f, 1.56f, 0f);
            Vector3 toFace = npcFacePos - eyePos;
            float horizDist = MathF.Sqrt(toFace.X * toFace.X + toFace.Z * toFace.Z);
            if (horizDist > 0.05f)
            {
                float targetYaw = MathF.Atan2(toFace.X, toFace.Z);
                float targetPitch = Math.Clamp(MathF.Atan2(toFace.Y, horizDist), -0.65f, 0.65f);
                float yawDiff = MathF.Atan2(MathF.Sin(targetYaw - camYaw), MathF.Cos(targetYaw - camYaw));
                float lookSmooth = 1f - MathF.Exp(-7.5f * dt);
                camYaw += yawDiff * lookSmooth;
                camPitch += (targetPitch - camPitch) * lookSmooth;
            }
        }

        ApplyCameraOrientation(eyePos);

        UpdateTypewriter(dt, GetSpeakerPitch(currentSpeaker));
        bool textFinished = (int)visibleChars >= fullText.Length;

        if (!textFinished && (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.F)))
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
                else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    if (storyStep == 1)
                    {
                        string reply = selectedOption == 0
                            ? "Cảm ơn cháu nhiều nhé. Ông gửi tiền, cháu mới đi làm ca đêm cố gắng giữ sức khỏe nha."
                            : "Ông quen đường rồi cháu ơi. Cảm ơn cháu, ông gửi tiền sữa với bánh mì nhé.";
                        StartDialogue("Ông cụ", reply, 2, talkingToNpc: true);
                    }
                    else if (storyStep == 2)
                    {
                        string reply = selectedOption == 0
                            ? "Tội nghiệp ông cụ tóc bạc ở con hẻm đối diện quán mình nè, đi bộ buổi tối bị xe tải quẹt mất tuần trước rồi, nhà đóng cửa im ỉm mấy nay..."
                            : "Ừ chị ở dãy trọ cuối đường. Mà nhắc mới nhớ, ông cụ tóc bạc ở hẻm đối diện vừa mất vì tai nạn tuần trước nên tối nay đường vắng tanh à.";
                        StartDialogue("Cô gái", reply, 2, talkingToNpc: true);
                    }
                    else if (storyStep == 6)
                    {
                        noticedAnomaly = (selectedOption == 0);
                        string reply = selectedOption == 0
                            ? "Ông cụ đó mất tuần trước rồi... Cậu còn mở cửa bán hàng thì trước 3 giờ nhớ khóa chặt chốt, đừng để thứ ngoài ngã tư lọt vào."
                            : "Không cần mua gì đâu. Nhớ khóa chặt cửa lúc 3 giờ, đừng mở cho bất kỳ ai gõ kính...";
                        StartDialogue("Người đàn ông lạ", reply, 2, talkingToNpc: true);
                    }
                    else if (storyStep == 8)
                    {
                        if (selectedOption == 0)
                        {
                            Raylib.PlaySound(scareSound);
                            cameraShakeTimer = 0.65f;
                            gameEnded = true;
                            isBadEnding = true;
                            endingTitle = "KẾT THÚC XẤU: PHÁ VỠ QUY TẮC";
                            endingText =
                                "Tú vừa gạt chốt mở cửa, toàn bộ bóng đèn trong cửa hàng nổ tung.\n" +
                                "Bóng người mặc áo mưa màu vàng lao sầm vào bóng tối...\n" +
                                "Sáng hôm sau, người ta chỉ tìm thấy chiếc balo của Tú nằm lăn lóc dưới quầy thu ngân.";
                        }
                        else
                        {
                            // Đổi ngay khuôn mặt bình thường sang mặt kinh dị lúc nụ cười tắt ngấm
                            npcFaceId = FaceId.YellowHorror;
                            flickerTimer = 0.45f;
                            StartDialogue(
                                "Cô gái (Áo mưa vàng)",
                                "Cô ta đứng khựng lại, nụ cười tắt ngấm rồi đổi sang giọng trầm khàn: 'Mắt tinh đấy... Tiếc thật, suýt nữa thì đủ người thay ca đêm nay rồi.'",
                                2,
                                talkingToNpc: true
                            );
                            storyStep = 9;
                        }
                    }
                }
            }
            else if (dialogueStage == 2)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    isTalking = false;
                    isTalkingToNpc = false;
                    dialogueStage = 0;
                    Raylib.GetMouseDelta();

                    if (storyStep == 0)
                    {
                        clockMinutes = 30f;
                        currentTime = FormatClock(clockMinutes);
                        currentObjective = "Đón vị khách đầu tiên vừa bước vào cửa hàng";
                        SpawnNpc(
                            new Vector3(0f, 0f, 9.5f),
                            new Vector3(-1.1f, 0f, -1.1f),
                            new Color(95, 115, 125, 255),
                            new Color(225, 225, 230, 255),
                            faceId: FaceId.OldMan
                        );
                        storyStep = 1;
                    }
                    else if (storyStep == 1 || storyStep == 2 || storyStep == 6 || storyStep == 9)
                    {
                        npcTarget = new Vector3(0f, 0f, 11.5f);
                        npcLeaving = true;
                    }
                    else if (storyStep == 4)
                    {
                        currentObjective = "Chờ vị khách tiếp theo...";
                        eventDelayTimer = 2.0f;
                        storyStep = 5;
                    }
                    else if (storyStep == 10)
                    {
                        isDoorLocked = false;
                        currentObjective = "Bước ra ngoài cửa hàng để ra về";
                    }
                }
            }
        }
    }

    float targetFov = (isTalking && isTalkingToNpc && npcActive) ? TalkNpcFov : NormalFov;
    float zoomSmooth = 1f - MathF.Exp(-6.0f * dt);
    camera.FovY += (targetFov - camera.FovY) * zoomSmooth;

    if (npcActive)
    {
        float targetWeight = npcLeaving ? 0f : 1f;
        npcGazeWeight += (targetWeight - npcGazeWeight) * (1f - MathF.Exp(-5.5f * dt));

        var targetGaze = Props.ComputeGazeTarget(
            npcPos + new Vector3(0f, 1.65f, 0f),
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

    Raylib.BeginDrawing();
    Color skyColor = storyStep >= 10 ? new Color(45, 58, 75, 255) : new Color(6, 7, 12, 255);
    Raylib.ClearBackground(skyColor);

    Raylib.BeginMode3D(camera);
    Props.DrawStore(lightsOn, isDoorLocked, (float)Raylib.GetTime(), currentTime, storyStep == 4 && !isTalking);
    if (npcActive)
        Props.DrawPerson(npcPos, npcYaw, npcShirtColor, npcHairColor, npcWalkPhase, npcSwing,
                         npcRaincoat, npcIsFemale, npcHeadYaw, npcHeadPitch, npcHeadRoll, npcEyeGaze, npcFaceId);
    Props.DrawStoreGlass(isDoorLocked);

    if (!isTalking && activeInteractId != "")
    {
        if (activeInteractId == "rules")
            Raylib.DrawCubeWires(new Vector3(-1.1f, 1.07f, -2.1f), 0.38f, 0.05f, 0.48f, Color.Yellow);
        else if (activeInteractId == "phone")
            Raylib.DrawCubeWires(new Vector3(-0.15f, 1.13f, -2.1f), 0.32f, 0.18f, 0.28f, Color.Yellow);
        else if (activeInteractId == "door")
            Raylib.DrawCubeWires(new Vector3(0f, 1.3f, 5.0f), 2.44f, 2.64f, 0.12f, Color.Yellow);
    }

    Raylib.EndMode3D();

    DrawVignette(lightsOn ? 0 : 65);

    Raylib.DrawRectangle(15, 15, 560, 68, new Color(0, 0, 0, 175));
    DrawViText($"Thời gian: {currentTime}   |   Cửa chính: {(isDoorLocked ? "ĐÃ KHÓA" : "ĐANG MỞ")}", 28, 22, 20, storyStep == 7 ? Color.Orange : Color.Yellow);
    DrawViText($"Nhiệm vụ: {currentObjective}", 28, 50, 19, Color.RayWhite);

    Raylib.DrawCircle(ScreenWidth / 2, ScreenHeight / 2, interactPrompt != "" ? 5 : 2, interactPrompt != "" ? Color.Yellow : Color.LightGray);
    if (interactPrompt != "" && !isTalking)
    {
        DrawViText(interactPrompt, ScreenWidth / 2 - 75, ScreenHeight / 2 + 16, 22, Color.Yellow);
    }

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

Props.UnloadFaceTextures();
Raylib.UnloadFont(viFont);
Raylib.UnloadSound(footstepSound);
Raylib.UnloadSound(voiceStaticSound);
Raylib.UnloadSound(phoneRingSound);
Raylib.UnloadSound(knockSound);
Raylib.UnloadSound(scareSound);
Raylib.UnloadSound(ambientHumSound);
Raylib.UnloadSound(heartbeatSound);
Raylib.CloseAudioDevice();
Raylib.CloseWindow();

static Sound CreateFootstepSound()
{
    const int sampleRate = 22050;
    const float duration = 0.13f;
    int sampleCount = (int)(sampleRate * duration);
    short[] samples = new short[sampleCount];
    Random rand = new(42);

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float envelope = MathF.Exp(-t * 32f);
        float freq = 115f - t * 280f;
        float thump = MathF.Sin(2f * MathF.PI * Math.Max(40f, freq) * t);
        float grit = ((float)rand.NextDouble() * 2f - 1f) * 0.25f;
        samples[i] = (short)((thump * 0.85f + grit) * envelope * short.MaxValue);
    }

    return BuildSoundFromPcm(samples, sampleRate);
}

static Sound CreateStaticBlipSound()
{
    const int sampleRate = 22050;
    const float duration = 0.032f;
    int sampleCount = (int)(sampleRate * duration);
    short[] samples = new short[sampleCount];
    Random rand = new(99);

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float envelope = 1f - (t / duration);
        float buzz = MathF.Sign(MathF.Sin(2f * MathF.PI * 190f * t)) * 0.4f;
        float staticNoise = ((float)rand.NextDouble() * 2f - 1f) * 0.6f;
        samples[i] = (short)((buzz + staticNoise) * envelope * short.MaxValue * 0.6f);
    }

    return BuildSoundFromPcm(samples, sampleRate);
}

static Sound CreatePhoneRingSound()
{
    const int sampleRate = 22050;
    const float duration = 0.45f;
    int sampleCount = (int)(sampleRate * duration);
    short[] samples = new short[sampleCount];

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float mod = MathF.Sin(2f * MathF.PI * 22f * t) > 0 ? 1f : 0.2f;
        float tone = (MathF.Sin(2f * MathF.PI * 440f * t) + MathF.Sin(2f * MathF.PI * 480f * t)) * 0.45f;
        samples[i] = (short)(tone * mod * short.MaxValue * 0.6f);
    }

    return BuildSoundFromPcm(samples, sampleRate);
}

static Sound CreateKnockSound()
{
    const int sampleRate = 22050;
    const float duration = 0.55f;
    int sampleCount = (int)(sampleRate * duration);
    short[] samples = new short[sampleCount];

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float wave = 0f;
        foreach (float hitTime in new[] { 0.0f, 0.18f, 0.36f })
        {
            if (t >= hitTime)
            {
                float dtHit = t - hitTime;
                wave += MathF.Sin(2f * MathF.PI * 140f * dtHit) * MathF.Exp(-dtHit * 38f);
            }
        }
        samples[i] = (short)(Math.Clamp(wave, -1f, 1f) * short.MaxValue * 0.9f);
    }

    return BuildSoundFromPcm(samples, sampleRate);
}

static Sound CreateScareSound()
{
    const int sampleRate = 22050;
    const float duration = 0.7f;
    int sampleCount = (int)(sampleRate * duration);
    short[] samples = new short[sampleCount];
    Random rand = new(7);

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float env = MathF.Exp(-t * 3.5f);
        float screech = MathF.Sin(2f * MathF.PI * (620f - t * 400f) * t) * 0.5f;
        float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.5f;
        samples[i] = (short)((screech + noise) * env * short.MaxValue);
    }

    return BuildSoundFromPcm(samples, sampleRate);
}

static Sound CreateAmbientHumSound()
{
    const int sampleRate = 22050;
    const float duration = 2.0f;
    int sampleCount = (int)(sampleRate * duration);
    short[] samples = new short[sampleCount];
    Random rand = new(12);

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float hum = MathF.Sin(2f * MathF.PI * 60f * t) * 0.45f + MathF.Sin(2f * MathF.PI * 120f * t) * 0.2f;
        float air = ((float)rand.NextDouble() * 2f - 1f) * 0.12f;
        samples[i] = (short)((hum + air) * short.MaxValue * 0.35f);
    }

    return BuildSoundFromPcm(samples, sampleRate);
}

static Sound CreateHeartbeatSound()
{
    const int sampleRate = 22050;
    const float duration = 0.55f;
    int sampleCount = (int)(sampleRate * duration);
    short[] samples = new short[sampleCount];

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float beat1 = MathF.Sin(2f * MathF.PI * 52f * t) * MathF.Exp(-t * 24f);
        float beat2 = t > 0.18f ? MathF.Sin(2f * MathF.PI * 48f * (t - 0.18f)) * MathF.Exp(-(t - 0.18f) * 28f) * 0.75f : 0f;
        samples[i] = (short)(Math.Clamp(beat1 + beat2, -1f, 1f) * short.MaxValue * 0.85f);
    }

    return BuildSoundFromPcm(samples, sampleRate);
}

static Sound BuildSoundFromPcm(short[] samples, int sampleRate)
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

    foreach (short s in samples)
        writer.Write(s);

    Wave wave = Raylib.LoadWaveFromMemory(".wav", ms.ToArray());
    Sound sound = Raylib.LoadSoundFromWave(wave);
    Raylib.UnloadWave(wave);
    return sound;
}