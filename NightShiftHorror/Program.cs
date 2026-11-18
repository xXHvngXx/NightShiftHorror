using System.Numerics;
using System.Text;
using Raylib_cs;

const int ScreenWidth = 1280;
const int ScreenHeight = 720;
Raylib.InitWindow(ScreenWidth, ScreenHeight, "Ca Trực Đêm - Night Shift Horror");
DisableIme();
Raylib.SetExitKey(KeyboardKey.Null);
Raylib.InitAudioDevice();
Raylib.SetTargetFPS(60);
Raylib.DisableCursor();

RenderTexture2D sceneTarget = Raylib.LoadRenderTexture(ScreenWidth, ScreenHeight);
Raylib.SetTextureFilter(sceneTarget.Texture, TextureFilter.Bilinear);

Props.LoadFaceTextures();

string viChars =
    " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~•" +
    "ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯư" +
    "ẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹ";

int[] codepoints = viChars.Select(c => (int)c).Distinct().ToArray();
Font viFont = Raylib.LoadFontEx(@"C:\Windows\Fonts\segoeui.ttf", 26, codepoints, codepoints.Length);
Raylib.SetTextureFilter(viFont.Texture, TextureFilter.Bilinear);

Sound cafeAmbience = CreateCafeAmbienceSound();
Sound footstepSound = CreateFootstepSound();
Sound voiceStaticSound = CreateStaticBlipSound();
Sound phoneRingSound = CreatePhoneRingSound();
Sound phoneVibrateSound = CreatePhoneVibrateSound();
Sound knockSound = CreateKnockSound();
Sound scareSound = CreateScareSound();
Sound ambientHumSound = CreateAmbientHumSound();
Sound heartbeatSound = CreateHeartbeatSound();

Raylib.SetSoundVolume(footstepSound, 0.75f);
Raylib.SetSoundVolume(voiceStaticSound, 0.35f);
Raylib.SetSoundVolume(phoneRingSound, 0.55f);
Raylib.SetSoundVolume(phoneVibrateSound, 0.45f);
Raylib.SetSoundVolume(knockSound, 0.85f);
Raylib.SetSoundVolume(scareSound, 0.85f);
Raylib.SetSoundVolume(ambientHumSound, 0.22f);
Raylib.SetSoundVolume(heartbeatSound, 0.65f);

const float NormalFov = 65.0f;
const float TalkNpcFov = 48.0f;

Vector3 cafeSeatPos = new(0.0f, 1.32f, -1.15f);
Vector3 dungHeadPos = new(0.0f, 1.28f, 0.85f);
Vector3 denHeadPos = new(0.95f, 1.28f, -0.15f);
Vector3 hungHeadPos = new(-0.95f, 1.28f, -0.15f);

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

// Vùng va chạm đồng bộ mặt bằng mới: hành lang giữa (X: -3.4..+1.6) hoàn toàn thông thoáng!
List<BoundingBox> solidObjects =
[
    // Quầy thu ngân chữ L & tủ mát mini xanh bên cạnh
    new(new Vector3(-3.2f, 0f, -2.6f), new Vector3(1.25f, 1.2f, -1.7f)),
    new(new Vector3(-3.2f, 0f, -5.4f), new Vector3(-2.3f, 1.1f, -1.7f)),
    // Quầy đảo ốp gỗ & tủ bánh ngọt ở cánh trái
    new(new Vector3(-5.05f, 0f, -1.35f), new Vector3(-3.65f, 1.7f, 2.35f)),
    // Trạm nước Polar Pop / cà phê dọc tường trái
    new(new Vector3(-7.45f, 0f, -3.35f), new Vector3(-6.20f, 1.8f, 2.95f)),
    // Kệ snack lưới đen có bánh xe ở cánh phải
    new(new Vector3(1.82f, 0f, 1.25f), new Vector3(2.68f, 1.7f, 2.05f)),
    // 2 dãy kệ hàng cánh phải
    new(new Vector3(3.52f, 0f, -3.05f), new Vector3(4.18f, 1.85f, 1.45f)),
    new(new Vector3(5.02f, 0f, -3.05f), new Vector3(5.68f, 1.85f, 1.45f)),
    // Dãy tủ nước Coca-Cola & tủ mát tường phải
    new(new Vector3(6.65f, 0f, -5.1f), new Vector3(7.45f, 2.5f, 5.9f))
];

BoundingBox rulesPaperBox = new(new Vector3(-1.35f, 1.02f, -2.36f), new Vector3(-0.88f, 1.15f, -1.84f));
BoundingBox logbookBox = new(new Vector3(-0.80f, 1.02f, -2.35f), new Vector3(-0.44f, 1.15f, -1.90f));
BoundingBox deskPhoneBox = new(new Vector3(-0.35f, 1.05f, -2.35f), new Vector3(0.08f, 1.35f, -1.85f));
BoundingBox frontDoorBox = new(new Vector3(-1.2f, 0.0f, 7.2f), new Vector3(1.2f, 2.5f, 7.8f));
BoundingBox posterBox = new(new Vector3(-2.35f, 1.20f, 8.85f), new Vector3(-1.85f, 1.90f, 9.30f));

float stepTimer = 0f;
float walkBobTimer = 0f;
const float StepInterval = 0.46f;
Random rng = new();

bool lieToMom = false;
bool readPoster = false;
bool readLogbook = false;
bool readMargin = false;
bool nameTag = false;
bool talkedMai = false;
bool recordingOn = false;
bool sharedLocation = false;
bool spoofAlertShown = false;
int greetedCount = 0;

int GetClueCount()
{
    int count = 0;
    if (readPoster) count++;
    if (readLogbook) count++;
    if (readMargin) count++;
    if (nameTag) count++;
    if (talkedMai) count++;
    return count;
}

(string App, string Sender, string Time, string Body)[] initialPhoneMessages =
[
    ("Zalo", "Cô Lan (Chủ trọ)", "17:38", "Tú ơi, tháng này tiền trọ 2 triệu 8 nghen. Cô tăng 300 nghìn, điện nước cô tính riêng. Trước mùng 5 hen."),
    ("Ngân hàng", "Biến động số dư", "17:40", "Tài khoản +500.000đ. Số dư hiện tại: 187.400đ."),
    ("Zalo", "Mẹ", "17:41", "Con ăn cơm chưa? Mẹ chuyển 500.000 rồi đó, đừng có nhịn nghen. Tháng này hơi khó khăn, con tiết kiệm chút."),
    ("Zalo", "Hội Cà Phê Chiều Thứ Sáu", "17:42", "Dũng: Ê Tú, đi cà phê hông? Đen bao.")
];

int phoneNotifStep = 0;
float phoneStepTimer = 0.7f;
int phoneChoiceStage = 0;
int phoneSelectedOption = 0;
float phoneEndHoldTimer = 0f;
string phoneSentReply = "";

(string Speaker, string Text)[] cafeDialogue =
[
    ("Tú", "Mấy Nigg* arghhhhh kakaakaa tao trúng Mitya rồi!"),
    ("Dũng", "Bịp thủ, mày không nhịn được hay gì?"),
    ("Tú", "Thì... lỡ tay. Tao tham roll đó giờ."),
    ("Hưng", "Lỡ tay là bao nhiêu?"),
    ("Tú", "Ba trăm hai tám."),
    ("Đen", "Ba trăm hai tám nghìn là tiền cơm mười ngày của mày đó Tú."),
    ("Tú", "Ừ thì... ra Mitya rồi mà, coi như hòa vốn."),
    ("Dũng", "Hòa cái đầu mày. Rồi tiền trọ tính sao?"),
    ("Tú", "Cô Lan tăng ba trăm. Hai triệu tám. Mẹ tao mới gửi năm trăm."),
    ("Hưng", "Ừ... mà thôi."),
    ("Đen", "Quây, tao thấy trên group có đăng cái này nè. Cửa hàng tiện lợi cuối đường Số 9 tuyển trực ca đêm. Mười hai giờ tới năm giờ. Năm trăm nghìn một đêm, tiền mặt."),
    ("Dũng", "Năm trăm một đêm hả? Nghe lỏ vậy."),
    ("Đen", "Thì ca khuya mà. Hồi đó tao định đi, mà bả hông cho."),
    ("Hưng", "Chỗ đó hả... Hồi lớp mười mẹ tao cấm đi ngang. Hình như nó đóng cửa lâu lắm rồi mà. Giờ mở lại hả?"),
    ("Đen", "Chắc mở lại. Có số điện thoại nè."),
    ("Tú", "...Cho tao xin số."),
    ("Dũng", "Thiệt luôn? Mày tính đi làm đêm thiệt hả?"),
    ("Tú", "Làm vài bữa thôi. Đủ tiền trọ là nghỉ. Dư thì thẻ tháng, trân châu."),
    ("Hưng", "Đi thì nhớ bật ghi âm điện thoại lên. Làm chỗ lạ, mà không có hợp đồng gì hết, lỡ có chuyện del ai làm chứng."),
    ("Dũng", "Vậy mười giờ tao nhắn hỏi thăm. Del rep là tao qua xách đít mày về.")
];

string[] introMonologue =
[
    "23:45 - Đêm sương dày đặc ở cuối đường Số 9. Phía trước là cửa hàng tiện lợi Thuận Phát 24H rộng lớn sáng trưng ánh đèn huỳnh quang.",
    "Trên cột đèn sát vỉa hè có dán một tờ thông báo cũ đã ố vàng. Bên trong tiệm, nhân viên ca tối đang tất bật kiểm hàng trước giờ bàn giao.",
    "Vào nhận bàn giao ca thôi. Nhớ bật ghi âm điện thoại cho chắc ăn..."
];

int gameScene = -1;
int cafeIndex = 0;
int cafeSubStep = 0;
bool isFadingOutCafe = false;
int introIndex = 0;
float fadeAlpha = 1.0f;

bool isPaused = false;
int pauseSelection = 0;
string[] pauseOptions = ["Tiếp tục ca trực", "Chơi lại từ đầu", "Thoát trò chơi"];

int storyStep = -1;
float clockMinutes = 1428f;
string currentTime = "23:48";
string currentObjective = "Nói chuyện với Quản lý để nhận bàn giao ca";
bool isDoorLocked = false;
bool lightsOn = true;
bool noticedAnomaly = false;
float flickerTimer = 0f;
float phoneRingTimer = 0f;
float eventDelayTimer = 0f;
float heartbeatTimer = 0f;
float cameraShakeTimer = 0f;
int endingType = 0;

bool isWorkCutscene = false;
float workCutsceneTimer = 0f;

bool isDawnCutscene = false;
float dawnCutsceneTimer = 0f;
float dawnProgress = 0f;

bool npcActive = false;
Vector3 npcPos = new(0f, 0f, 13.5f);
Vector3 npcTarget = new(0f, 0f, 13.5f);
Color npcShirtColor = Color.Blue;
Color npcHairColor = new(35, 35, 40, 255);
bool npcLeaving = false;
bool npcShouldLeaveAfterDialogue = false;
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
const float TypingSpeed = 38f;
string[] currentOptions = [];
int selectedOption = 0;
string activeInteractId = "";
string subChoiceMode = "";

Color LerpColor(Color a, Color b, float t)
{
    t = Math.Clamp(t, 0f, 1f);
    return new Color(
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t),
        (byte)(a.A + (b.A - a.A) * t)
    );
}

void RenderFoggyScene(Action drawWorld, Color fogTint, float blurSpread = 2.2f)
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

void SmoothLookAtPoint(Vector3 eyePos, Vector3 targetPos, float dt, float speed = 6.5f)
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

void UpdateFirstPersonLook(Vector3 eyePos)
{
    Vector2 mouseDelta = Raylib.GetMouseDelta();
    const float sensitivity = 0.0022f;
    camYaw -= mouseDelta.X * sensitivity;
    camPitch = Math.Clamp(camPitch - mouseDelta.Y * sensitivity, -1.42f, 1.42f);
    ApplyCameraOrientation(eyePos);
}

void StartDialogue(string speaker, string text, int stage, string[]? options = null,
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
}

float GetSpeakerPitch(string speaker) => speaker switch
{
    "Dũng" => 0.86f,
    "Đen" => 0.94f,
    "Hưng" => 0.98f,
    "Ông cụ" => 0.78f,
    "Chị Mai" => 1.18f,
    "Cô gái (Áo mưa vàng)" => 1.12f,
    "Anh Hùng" => 0.72f,
    "Quản lý" => 0.90f,
    "Điện thoại bàn" => 0.65f,
    "Điện thoại (Dũng)" => 0.86f,
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
    int alpha = Math.Clamp(120 + extraDarkness, 0, 215);
    Color edgeColor = new(0, 0, 0, alpha);
    Color clearColor = new(0, 0, 0, 0);

    Raylib.DrawRectangleGradientV(0, 0, ScreenWidth, 115, edgeColor, clearColor);
    Raylib.DrawRectangleGradientV(0, ScreenHeight - 115, ScreenWidth, 115, clearColor, edgeColor);
    Raylib.DrawRectangleGradientH(0, 0, 150, ScreenHeight, edgeColor, clearColor);
    Raylib.DrawRectangleGradientH(ScreenWidth - 150, 0, 150, ScreenHeight, clearColor, edgeColor);
}

void DrawDialogueBox()
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

void DrawPhoneIntroScene(float dt)
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
            StartDialogue(cafeDialogue[0].Speaker, cafeDialogue[0].Text, 2);
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
    npcShouldLeaveAfterDialogue = false;
    npcGazeWeight = 0f;
    npcHeadYaw = 0f;
    npcHeadPitch = 0f;
    npcHeadRoll = 0f;
    npcEyeGaze = Vector2.Zero;
    npcActive = true;
}

// Cutscene mở đầu lướt dọc hành lang trung tâm thoáng rộng của cửa hàng
void UpdateWorkCutscene(float dt)
{
    const float TotalDuration = 7.5f;
    workCutsceneTimer += dt;

    Vector3 camPos;
    Vector3 camLook;

    if (workCutsceneTimer < 2.6f)
    {
        // Góc 1: Từ lối vào rộng lướt qua quầy đảo gỗ, tủ bánh ngọt & trạm nước Polar Pop bên trái
        float p = Math.Clamp(workCutsceneTimer / 2.6f, 0f, 1f);
        float s = p * p * (3f - 2f * p);
        camPos = Vector3.Lerp(new Vector3(-0.3f, 2.25f, 6.2f), new Vector3(-1.4f, 2.10f, 2.6f), s);
        camLook = Vector3.Lerp(new Vector3(-3.8f, 1.45f, 1.5f), new Vector3(-5.8f, 1.65f, -0.5f), s);
    }
    else if (workCutsceneTimer < 5.2f)
    {
        // Góc 2: Lướt ngang qua hành lang rộng sang cánh phải (kệ lưới snack, tủ Coca-Cola & dãy tủ nước)
        float p = Math.Clamp((workCutsceneTimer - 2.6f) / 2.6f, 0f, 1f);
        float s = p * p * (3f - 2f * p);
        camPos = Vector3.Lerp(new Vector3(-1.4f, 2.10f, 2.6f), new Vector3(0.4f, 2.05f, 1.2f), s);
        camLook = Vector3.Lerp(new Vector3(2.4f, 1.40f, 1.8f), new Vector3(5.6f, 1.45f, -1.2f), s);
    }
    else
    {
        // Góc 3: Đáp về góc nhìn thứ nhất của Tú sau quầy thu ngân
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

void StartDawnCutscene()
{
    isDawnCutscene = true;
    dawnCutsceneTimer = 0f;
    dawnProgress = 0f;
    isTalking = false;
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
        faceId: FaceId.Manager
    );
    storyStep = 10;
}

void UpdateDawnCutscene(float dt)
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
        if (recordingOn && GetClueCount() >= 3)
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

string FormatClock(float totalMinutes)
{
    int mins = (int)totalMinutes;
    int h = (mins / 60) % 24;
    int m = mins % 60;
    return $"{h:D2}:{m:D2}";
}

void TriggerEnding(int type)
{
    endingType = type;
    gameEnded = true;
    isTalking = false;
    isTalkingToNpc = false;

    if (type == 1)
    {
        Raylib.PlaySound(scareSound);
        lightsOn = false;
        cameraShakeTimer = 0.65f;
        isBadEnding = true;
        endingTitle = "ENDING 1: BAD ENDING";
        endingText =
            "Tú vừa gạt chốt mở cửa, toàn bộ bóng đèn trong cửa hàng nổ tung.\n" +
            "Bóng người mặc áo mưa màu vàng lao sầm vào bóng tối với nụ cười rách toạc mang tai...\n" +
            "Sáng hôm sau, người ta chỉ tìm thấy chiếc balo cùng chiếc điện thoại nứt vỡ của Tú nằm lăn lóc dưới quầy thu ngân.";
    }
    else if (type == 2)
    {
        isBadEnding = false;
        endingTitle = "ENDING 2: CA TRỰC KHÔNG HỒI KẾT";
        string extraLine = noticedAnomaly
            ? "Tú nhớ lại ánh mắt cảnh báo của anh Hùng lúc 02:20 và rùng mình nhận ra quán này vốn chưa từng mở cửa cho người sống."
            : "Tú bàng hoàng nhớ lại ông cụ hiền lành lúc đầu ca — hóa ra ngay từ vị khách đầu tiên, Tú đã không còn ở thế giới bình thường nữa.";

        endingText =
            "Tú cầm phong bì tiền công bước qua bên kia đường rồi quay đầu nhìn lại cửa hàng.\n" +
            "Cửa cuốn gỉ sét đóng im lìm, phủ đầy bụi bặm cùng tấm biển cũ nát: 'THUẬN PHÁT 24H - NGỪNG HOẠT ĐỘNG TỪ NĂM 2019'.\n" +
            extraLine + "\n" +
            "Tú run tay xé phong bì kiểm tra: bên trong không có tờ 500 nghìn nào, chỉ có bản hợp đồng 'Nhân viên trực ca vĩnh viễn' đã ký sẵn tên Tú.";
    }
    else if (type == 3)
    {
        isBadEnding = false;
        endingTitle = "ENDING 3: BỎ CA GIỮA ĐÊM";
        endingText =
            "Cảm nhận sự bất thường rợn gáy bủa vây khắp căn tiệm, Tú quyết định bỏ ngang ca trực, đẩy cửa kính chạy thẳng về dãy trọ trong đêm sương.\n" +
            "Tú giữ được mạng sống, nhưng đành chấp nhận khất tiền trọ cô Lan thêm một tháng nữa.\n" +
            "Ba tuần sau, tại quán cà phê quen thuộc, Dũng dán mắt vào màn hình điện thoại: 'Ê tụi bay, trên group có chỗ tuyển trực ca đêm 500k một đêm nè... Cho em xin số ạ!'";
    }
    else if (type == 4)
    {
        isBadEnding = false;
        endingTitle = "TRUE ENDING: CUỐN BĂNG GHI ÂM LÚC 05:00";
        endingText =
            $"Với {GetClueCount()}/5 manh mối ghép lại cùng đoạn ghi âm xuyên đêm trong điện thoại, Tú gạt phăng tờ hợp đồng của gã Quản lý và lao thẳng ra khỏi cửa hàng khi trời vừa hửng sáng.\n" +
            "Bản ghi âm với những giọng nói trùng khớp và tiếng đập kính lúc 03:00 được Tú nộp thẳng lên công an phường, lật tẩy đường dây dụ dỗ sinh viên nghèo vào thế mạng ở cuối đường Số 9.\n" +
            "Tú không nhận được 500 nghìn tiền mặt đêm đó, nhưng khi gọi điện về báo bình an cho mẹ, cậu biết mình đã giữ lại được thứ quý giá nhất.";
    }
}

void ResetGame()
{
    camYaw = 0f;
    camPitch = -0.05f;
    currentBobOffset = 0f;
    camera.FovY = NormalFov;
    playerPos = new Vector3(-1.2f, 0f, -3.5f);
    UpdateFirstPersonLook(cafeSeatPos);

    lieToMom = false;
    readPoster = false;
    readLogbook = false;
    readMargin = false;
    nameTag = false;
    talkedMai = false;
    recordingOn = false;
    sharedLocation = false;
    spoofAlertShown = false;
    greetedCount = 0;

    phoneNotifStep = 0;
    phoneStepTimer = 0.7f;
    phoneChoiceStage = 0;
    phoneSelectedOption = 0;
    phoneEndHoldTimer = 0f;
    phoneSentReply = "";

    gameScene = -1;
    cafeIndex = 0;
    cafeSubStep = 0;
    isFadingOutCafe = false;
    introIndex = 0;
    fadeAlpha = 1.0f;
    isPaused = false;
    pauseSelection = 0;
    storyStep = -1;
    clockMinutes = 1428f;
    currentTime = "23:48";
    currentObjective = "Nói chuyện với Quản lý để nhận bàn giao ca";
    isDoorLocked = false;
    lightsOn = true;
    noticedAnomaly = false;
    flickerTimer = 0f;
    phoneRingTimer = 0f;
    eventDelayTimer = 0f;
    heartbeatTimer = 0f;
    cameraShakeTimer = 0f;
    endingType = 0;
    isWorkCutscene = false;
    workCutsceneTimer = 0f;
    isDawnCutscene = false;
    dawnCutsceneTimer = 0f;
    dawnProgress = 0f;
    npcActive = false;
    npcLeaving = false;
    npcShouldLeaveAfterDialogue = false;
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
    subChoiceMode = "";
    Raylib.StopSound(cafeAmbience);
    Raylib.StopSound(ambientHumSound);
    Raylib.DisableCursor();
}

UpdateFirstPersonLook(cafeSeatPos);

while (!Raylib.WindowShouldClose())
{
    float dt = Raylib.GetFrameTime();

    if (!gameEnded && Raylib.IsKeyPressed(KeyboardKey.Escape))
    {
        isPaused = !isPaused;
        pauseSelection = 0;
        if (isPaused)
        {
            Raylib.EnableCursor();
            if (Raylib.IsSoundPlaying(cafeAmbience)) Raylib.PauseSound(cafeAmbience);
        }
        else
        {
            Raylib.DisableCursor();
            Raylib.GetMouseDelta();
            if (gameScene == 0 && cafeSubStep < 2) Raylib.ResumeSound(cafeAmbience);
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
                if (gameScene == 0 && cafeSubStep < 2) Raylib.ResumeSound(cafeAmbience);
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
            DrawViText((sel ? "> " : "   ") + pauseOptions[i], ScreenWidth / 2 - 125, 280 + i * 44, 24, sel ? Color.Yellow : Color.RayWhite);
        }

        DrawViText("[Esc] Quay lại   |   [Lên/Xuống] Chọn   |   [Enter] Xác nhận", ScreenWidth / 2 - 265, ScreenHeight - 90, 18, Color.Gray);
        Raylib.EndDrawing();
        continue;
    }

    if (gameScene == -1)
    {
        DrawPhoneIntroScene(dt);
        continue;
    }

    if (gameScene == 0)
    {
        if (cafeSubStep < 2 && !Raylib.IsSoundPlaying(cafeAmbience))
        {
            Raylib.PlaySound(cafeAmbience);
        }

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
                targetCafeFov = 62.0f;
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
                gameScene = 1;
                isFadingOutCafe = false;
                fadeAlpha = 0f;
                introIndex = 0;
                camera.FovY = NormalFov;
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
                        cafeSubStep = 2;
                        Raylib.StopSound(cafeAmbience);
                        Raylib.PlaySound(phoneRingSound);
                        StartDialogue(
                            "Quản lý",
                            "Alo? À, sinh viên hả? Được, được. Mười một giờ rưỡi em tới nghen, anh dặn kỹ rồi giao ca luôn. Không cần mang giấy tờ gì đâu.",
                            2
                        );
                    }
                }
                else if (cafeSubStep == 2 && Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    isFadingOutCafe = true;
                }
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(28, 20, 16, 255));

        if (cafeSubStep < 2)
        {
            RenderFoggyScene(() =>
            {
                float cafeTime = (float)Raylib.GetTime();
                Props.DrawCafe(cafeTime, camera.Position, currentSpeaker);
            }, new Color(36, 26, 18, 255), 2.0f);
        }
        else
        {
            Raylib.ClearBackground(new Color(10, 11, 15, 255));
        }

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
                    faceId: FaceId.Manager
                );

                isWorkCutscene = true;
                workCutsceneTimer = 0f;
                isTalking = false;
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
        continue;
    }

    bool anyCutscene = isWorkCutscene || isDawnCutscene;

    if (!isTalking && !anyCutscene)
    {
        float clockRate = storyStep == 7 ? 0.65f : 0.35f;
        float maxClockForStep = storyStep switch
        {
            -1 => 1438f,
            0 => 1468f,
            1 => 1512f,
            2 => 1558f,
            4 or 5 => 1578f,
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

        if (storyStep == 7 && !isDoorLocked && sharedLocation && !spoofAlertShown && clockMinutes >= 1614f)
        {
            spoofAlertShown = true;
            Raylib.PlaySound(phoneVibrateSound);
            StartDialogue(
                "Tin nhắn (Dũng)",
                "Điện thoại Tú rung lên tin nhắn từ tài khoản Dũng: 'Ê, tao chạy ngang đường Số 9 ghé qua chỗ mày nè, mở cửa tao coi.'",
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
            currentObjective = "Tiếp vị khách vừa bước vào cửa hàng (02:20)";
            SpawnNpc(
                new Vector3(0f, 0f, 12.0f),
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

            if (storyStep == -1)
            {
                clockMinutes = 1440f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "Đọc tờ quy tắc & sổ giao ca trên quầy thu ngân";
                storyStep = 0;
            }
            else if (storyStep == 1)
            {
                clockMinutes = 1515f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "Tiếp vị khách tiếp theo vừa vào cửa hàng (01:15)";
                SpawnNpc(
                    new Vector3(0f, 0f, 12.0f),
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
                clockMinutes = 1560f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "Nghe điện thoại bàn đang reo trên quầy (02:00)";
                flickerTimer = 1.2f;
                phoneRingTimer = 0.2f;
                Raylib.PlaySound(voiceStaticSound);
                storyStep = 4;
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

    string interactPrompt = "";
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
        playerPos.X = Math.Clamp(playerPos.X, -6.9f, 6.9f);
        playerPos.Z = Math.Clamp(playerPos.Z, -5.9f, maxZ);

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

        if (storyStep >= 1 && storyStep <= 7 && !isDoorLocked && playerPos.Z > 10.7f)
        {
            TriggerEnding(3);
            continue;
        }

        if (storyStep == 10 && playerPos.Z > 10.0f)
        {
            TriggerEnding(2);
            continue;
        }

        Vector3 lookDir = Vector3.Normalize(camera.Target - camera.Position);
        Ray eyeRay = new(camera.Position, lookDir);

        RayCollision paperHit = Raylib.GetRayCollisionBox(eyeRay, rulesPaperBox);
        RayCollision logbookHit = Raylib.GetRayCollisionBox(eyeRay, logbookBox);
        RayCollision phoneHit = Raylib.GetRayCollisionBox(eyeRay, deskPhoneBox);
        RayCollision doorHit = Raylib.GetRayCollisionBox(eyeRay, frontDoorBox);
        RayCollision posterHit = Raylib.GetRayCollisionBox(eyeRay, posterBox);

        BoundingBox npcBox = new(npcPos - new Vector3(0.4f, 0f, 0.4f), npcPos + new Vector3(0.4f, 1.85f, 0.4f));
        RayCollision npcHit = npcActive ? Raylib.GetRayCollisionBox(eyeRay, npcBox) : new RayCollision { Hit = false };

        if (npcHit.Hit && npcHit.Distance <= 3.2f && !npcLeaving)
        {
            activeInteractId = "npc";
            interactPrompt = "[F] Nói chuyện";
        }
        else if (posterHit.Hit && posterHit.Distance <= 3.0f)
        {
            activeInteractId = "poster";
            interactPrompt = "[F] Xem tờ áp phích trên cột đèn";
        }
        else if (paperHit.Hit && paperHit.Distance <= 2.6f)
        {
            activeInteractId = "rules";
            interactPrompt = "[F] Đọc tờ quy tắc";
        }
        else if (logbookHit.Hit && logbookHit.Distance <= 2.6f)
        {
            activeInteractId = "logbook";
            interactPrompt = "[F] Lật xem Sổ giao ca";
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
            if (activeInteractId == "poster")
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
                    "Tờ quy tắc ca đêm",
                    "1. Chào hỏi khách đàng hoàng trước khi tính tiền. 2. Tuyệt đối không bán hàng cho ■■■■■■. 3. Đúng 03:00 khóa chặt cửa chính, không mở cho bất kỳ ai.",
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
                    "Trang cuối ghi nét bút run rẩy của ca trước (Trần Văn Hùng): 'Đừng tin quản lý. Mấy người bước vô sau 12 giờ không phải khách. Nếu lỡ gửi vị trí cho ai thì 3 giờ sáng đừng nghe lời nó gọi ngoài kính...'",
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
                clockMinutes = 1620f;
                currentTime = FormatClock(clockMinutes);
                currentObjective = "03:00 - Có tiếng đập cửa kính! Ra kiểm tra cửa chính";
                SpawnNpc(
                    new Vector3(0f, 0f, 8.2f),
                    new Vector3(0f, 0f, 8.2f),
                    Color.Gold,
                    new Color(40, 30, 25, 255),
                    raincoat: true,
                    isFemale: true,
                    faceId: FaceId.YellowNormal
                );
                storyStep = 8;
                StartDialogue(
                    "Tú",
                    "Mình vừa gạt chốt khóa cửa đúng 03:00 thì tiếng đập cửa kính vang lên dồn dập: CỐC! CỐC! CỐC!",
                    2
                );
            }
            else if (activeInteractId == "npc")
            {
                if (storyStep == -1)
                {
                    StartDialogue(
                        "Quản lý",
                        "Tới rồi hả Tú? Ca đêm nhàn lắm, em cứ ngồi quầy đọc kỹ tờ quy tắc rồi trực tới 5 giờ sáng anh quay lại đưa tiền mặt. Nhớ kỹ điều số 3 nha!",
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
                            "Dạ của ông hết 25 nghìn ạ. Ông cầm tiền thừa nhé.",
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
                            "Dạ hết 20 nghìn ạ. Tuần trước ngoài ngã tư có vụ gì vậy chị?",
                            "Chị ở trọ gần đây à? Giờ này còn đi mua đồ ăn khuya."
                        ],
                        talkingToNpc: true
                    );
                }
                else if (storyStep == 6)
                {
                    StartDialogue(
                        "Anh Hùng",
                        "(Trên ngực áo sơ mi của anh ta vẫn đeo bảng tên nhân viên ca đêm: TRẦN VĂN HÙNG) Lấy cho tôi hộp sữa tươi với ổ bánh mì... Tôi ở ngay con hẻm đối diện...",
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
                            "Cô gái (Áo mưa vàng)",
                            "Bóng áo mưa vàng áp sát mặt kính, nhưng từ cổ họng cô ta lại phát ra giọng hệt thằng Dũng: 'Ê Tú! Tao Dũng nè, mở cửa nhanh đi ngoài này lạnh teo rồi!'",
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
                            "Cô gái (Áo mưa vàng)",
                            "Em ơi mở cửa nhanh giúp chị với! Lúc nãy chị làm rơi chùm chìa khóa phòng trọ ngay trước quầy thu ngân!",
                            1,
                            [
                                "Mở chốt cửa cho chị ấy vào tìm chìa khóa",
                                "Trên quầy không có chìa khóa nào đâu chị"
                            ],
                            talkingToNpc: true,
                            mode: "0300_normal"
                        );
                    }
                }
                else if (storyStep == 10)
                {
                    if (recordingOn && GetClueCount() >= 3)
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
    }
    else
    {
        Raylib.GetMouseDelta();
        currentBobOffset += (0f - currentBobOffset) * (1f - MathF.Exp(-10f * dt));
        Vector3 eyePos = new(playerPos.X, 1.7f + currentBobOffset, playerPos.Z);

        if (isTalkingToNpc && npcActive)
        {
            Vector3 npcFacePos = npcPos + new Vector3(0f, 1.56f, 0f);
            SmoothLookAtPoint(eyePos, npcFacePos, dt, 7.5f);
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
                    if (subChoiceMode == "record_choice")
                    {
                        recordingOn = (selectedOption == 0);
                        subChoiceMode = "";
                        isTalking = false;
                        dialogueStage = 0;
                        Raylib.GetMouseDelta();
                    }
                    else if (subChoiceMode == "rules_margin")
                    {
                        subChoiceMode = "";
                        if (selectedOption == 0)
                        {
                            readMargin = true;
                            StartDialogue(
                                "Chữ viết tay bên lề",
                                "Dòng chữ chì mờ mờ ghi ở mép dưới: 'Đừng bao giờ gửi vị trí quán này cho bạn bè. Nó nghe được tên ai là 3 giờ sáng nó sẽ giả giọng người đó...'",
                                2,
                                mode: "rules_done"
                            );
                        }
                        else
                        {
                            isTalking = false;
                            dialogueStage = 0;
                            Raylib.GetMouseDelta();

                            if (storyStep == 0)
                            {
                                clockMinutes = 1470f;
                                currentTime = FormatClock(clockMinutes);
                                currentObjective = "Đón vị khách đầu tiên vừa bước vào cửa hàng (00:30)";
                                SpawnNpc(
                                    new Vector3(0f, 0f, 12.0f),
                                    new Vector3(-1.1f, 0f, -1.1f),
                                    new Color(95, 115, 125, 255),
                                    new Color(225, 225, 230, 255),
                                    faceId: FaceId.OldMan
                                );
                                storyStep = 1;
                            }
                        }
                    }
                    else if (storyStep == 1)
                    {
                        greetedCount++;
                        string reply = selectedOption == 0
                            ? "Cảm ơn cháu nhiều nhé. Ông gửi tiền, cháu mới đi làm ca đêm cố gắng giữ sức khỏe nha."
                            : "Ông quen đường rồi cháu ơi. Cảm ơn cháu, ông gửi tiền sữa với bánh mì nhé.";
                        StartDialogue("Ông cụ", reply, 2, talkingToNpc: true, leaveAfter: true);
                    }
                    else if (storyStep == 2)
                    {
                        greetedCount++;
                        talkedMai = true;
                        string reply = selectedOption == 0
                            ? "Tội nghiệp ông cụ tóc bạc ở con hẻm đối diện quán mình nè, đi bộ buổi tối bị xe tải quẹt mất tuần trước rồi, nhà đóng cửa im ỉm mấy nay..."
                            : "Ừ chị ở dãy trọ cuối đường. Mà nhắc mới nhớ, ông cụ tóc bạc ở hẻm đối diện vừa mất vì tai nạn tuần trước nên tối nay đường vắng tanh à.";
                        StartDialogue("Chị Mai", reply, 2, talkingToNpc: true, leaveAfter: true);
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
                                TriggerEnding(1);
                                continue;
                            }
                            else if (selectedOption == 1)
                            {
                                subChoiceMode = "";
                                npcFaceId = FaceId.YellowHorror;
                                flickerTimer = 0.55f;
                                storyStep = 9;
                                StartDialogue(
                                    "Điện thoại (Dũng)",
                                    "Đầu dây bên kia giọng Dũng ngái ngủ: 'Alo? Tao đang nằm đắp chăn ở nhà trọ mà, qua hồi nào!' Ngoài kính, cô gái áo mưa vàng đứng khựng lại, nụ cười tắt ngấm rồi gầm gừ: 'Mắt tinh tú đấy... Tiếc thật.'",
                                    2,
                                    talkingToNpc: true,
                                    leaveAfter: true
                                );
                            }
                            else
                            {
                                subChoiceMode = "";
                                npcFaceId = FaceId.YellowHorror;
                                flickerTimer = 0.45f;
                                storyStep = 9;
                                StartDialogue(
                                    "Cô gái (Áo mưa vàng)",
                                    "Cô ta đứng khựng lại, giọng thằng Dũng biến mất, đổi sang tiếng trầm khàn: 'Mắt tinh tú đấy... Tiếc thật.'",
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
                                TriggerEnding(1);
                                continue;
                            }
                            else
                            {
                                subChoiceMode = "";
                                npcFaceId = FaceId.YellowHorror;
                                flickerTimer = 0.45f;
                                storyStep = 9;
                                StartDialogue(
                                    "Cô gái (Áo mưa vàng)",
                                    "Cô ta đứng khựng lại, nụ cười tắt ngấm rồi đổi sang giọng trầm khàn: 'Mắt tinh tú đấy... Tiếc thật.'",
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
                            isTalkingToNpc = false;
                            dialogueStage = 0;
                            isDoorLocked = false;
                            currentObjective = "Bước ra ngoài cửa hàng để ra về";
                            Raylib.GetMouseDelta();
                        }
                        else
                        {
                            TriggerEnding(4);
                            continue;
                        }
                    }
                }
            }
            else if (dialogueStage == 2)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    string finishedMode = subChoiceMode;
                    bool shouldNpcLeave = npcShouldLeaveAfterDialogue;

                    subChoiceMode = "";
                    npcShouldLeaveAfterDialogue = false;
                    isTalking = false;
                    isTalkingToNpc = false;
                    dialogueStage = 0;
                    Raylib.GetMouseDelta();

                    if (storyStep == 0 && finishedMode == "rules_done")
                    {
                        clockMinutes = 1470f;
                        currentTime = FormatClock(clockMinutes);
                        currentObjective = "Đón vị khách đầu tiên vừa bước vào cửa hàng (00:30)";
                        SpawnNpc(
                            new Vector3(0f, 0f, 12.0f),
                            new Vector3(-1.1f, 0f, -1.1f),
                            new Color(95, 115, 125, 255),
                            new Color(225, 225, 230, 255),
                            faceId: FaceId.OldMan
                        );
                        storyStep = 1;
                    }
                    else if (shouldNpcLeave && npcActive)
                    {
                        npcTarget = new Vector3(0f, 0f, 14.0f);
                        npcLeaving = true;
                    }
                    else if (storyStep == 4 && finishedMode == "desk_phone")
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

    float targetFov = anyCutscene ? 57.0f : ((isTalking && isTalkingToNpc && npcActive) ? TalkNpcFov : NormalFov);
    float zoomSmooth = 1f - MathF.Exp(-6.0f * dt);
    camera.FovY += (targetFov - camera.FovY) * zoomSmooth;

    if (npcActive)
    {
        float targetWeight = (npcLeaving || anyCutscene) ? 0f : 1f;
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
    float currentDaylight = isDawnCutscene ? dawnProgress : (storyStep >= 10 ? 1.0f : 0.0f);
    Color nightSky = new(10, 12, 18, 255);
    Color dawnSky = new(135, 168, 205, 255);
    Color skyColor = LerpColor(nightSky, dawnSky, currentDaylight);

    RenderFoggyScene(() =>
    {
        Props.DrawStore(lightsOn, isDoorLocked, (float)Raylib.GetTime(), currentTime, storyStep == 4 && !isTalking, currentDaylight);

        // Trong Cutscene làm việc mở đầu, 2 nhân viên ca tối làm việc ở cánh trái & cánh phải
        if (isWorkCutscene)
        {
            float wt = workCutsceneTimer;
            // Nhân viên 1 dọn quầy đảo & trạm nước Polar Pop bên trái
            float staff1Z = 0.2f + MathF.Sin(wt * 1.4f) * 1.2f;
            Props.DrawPerson(new Vector3(-5.65f, 0f, staff1Z), -90f,
                new Color(195, 48, 48, 255), new Color(35, 30, 28, 255), wt * 6f, 18f, isFemale: true);

            // Nhân viên 2 kiểm tra kệ hàng & tủ nước bên phải
            float staff2Z = -0.8f + MathF.Cos(wt * 1.1f) * 1.6f;
            Props.DrawPerson(new Vector3(6.10f, 0f, staff2Z), 90f,
                new Color(32, 68, 125, 255), new Color(28, 28, 32, 255), wt * 7f, 22f);
        }

        if (npcActive)
            Props.DrawPerson(npcPos, npcYaw, npcShirtColor, npcHairColor, npcWalkPhase, npcSwing,
                             npcRaincoat, npcIsFemale, npcHeadYaw, npcHeadPitch, npcHeadRoll, npcEyeGaze, npcFaceId);
        Props.DrawStoreGlass(isDoorLocked, currentDaylight);

        if (!isTalking && !anyCutscene && activeInteractId != "")
        {
            if (activeInteractId == "rules")
                Raylib.DrawCubeWires(new Vector3(-1.1f, 1.07f, -2.1f), 0.38f, 0.05f, 0.48f, Color.Yellow);
            else if (activeInteractId == "logbook")
                Raylib.DrawCubeWires(new Vector3(-0.62f, 1.07f, -2.12f), 0.32f, 0.06f, 0.40f, Color.Yellow);
            else if (activeInteractId == "phone")
                Raylib.DrawCubeWires(new Vector3(-0.15f, 1.13f, -2.1f), 0.32f, 0.18f, 0.28f, Color.Yellow);
            else if (activeInteractId == "door")
                Raylib.DrawCubeWires(new Vector3(0f, 1.3f, 7.5f), 2.44f, 2.64f, 0.12f, Color.Yellow);
            else if (activeInteractId == "poster")
                Raylib.DrawCubeWires(new Vector3(-2.1f, 1.55f, 9.02f), 0.30f, 0.38f, 0.08f, Color.Yellow);
        }
    }, skyColor, LerpColor(new Color(24, 0, 0, 255), new Color(14, 0, 0, 255), currentDaylight).R * 0.1f);

    DrawVignette(lightsOn ? (int)(10 * (1f - currentDaylight)) : 75);

    if (isWorkCutscene)
    {
        int barH = 74;
        Raylib.DrawRectangle(0, 0, ScreenWidth, barH, Color.Black);
        Raylib.DrawRectangle(0, ScreenHeight - barH, ScreenWidth, barH, Color.Black);

        string caption = workCutsceneTimer < 2.6f
            ? "23:46 - Quầy đảo bánh ngọt & trạm nước tự phục vụ sáng đèn trước giờ giao ca"
            : (workCutsceneTimer < 5.2f
                ? "23:47 - Nhân viên ca tối đang kiểm kê lại kệ hàng và dãy tủ nước giải khát"
                : "23:48 - Quản lý đang đứng chờ bàn giao ca tại quầy thu ngân");
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

        Raylib.DrawCircle(ScreenWidth / 2, ScreenHeight / 2, interactPrompt != "" ? 5 : 2, interactPrompt != "" ? Color.Yellow : Color.LightGray);
        if (interactPrompt != "" && !isTalking)
        {
            DrawViText(interactPrompt, ScreenWidth / 2 - 95, ScreenHeight / 2 + 16, 22, Color.Yellow);
        }
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
Raylib.UnloadRenderTexture(sceneTarget);
Raylib.UnloadFont(viFont);
Raylib.UnloadSound(cafeAmbience);
Raylib.UnloadSound(footstepSound);
Raylib.UnloadSound(voiceStaticSound);
Raylib.UnloadSound(phoneRingSound);
Raylib.UnloadSound(phoneVibrateSound);
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

static Sound CreatePhoneVibrateSound()
{
    const int sampleRate = 22050;
    const float duration = 0.22f;
    int sampleCount = (int)(sampleRate * duration);
    short[] samples = new short[sampleCount];

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float env = MathF.Sin(MathF.PI * (t / duration));
        float buzz = MathF.Sin(2f * MathF.PI * 135f * t) * 0.65f + MathF.Sin(2f * MathF.PI * 270f * t) * 0.25f;
        samples[i] = (short)(buzz * env * short.MaxValue * 0.55f);
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

static Sound CreateCafeAmbienceSound()
{
    const int sampleRate = 22050;
    const int seconds = 8;
    int totalSamples = sampleRate * seconds;
    short[] samples = new short[totalSamples];
    Random rng = new(42);

    float[] voiceFreqs = { 135f, 165f, 195f, 230f, 270f, 310f };
    float[] talkRates = { 3.1f, 4.3f, 2.7f, 3.8f, 4.9f, 3.4f };

    float pink = 0f;
    for (int i = 0; i < totalSamples; i++)
    {
        float t = (float)i / sampleRate;
        float loopEnv = MathF.Sin(MathF.PI * t / seconds);
        loopEnv = MathF.Min(1f, loopEnv * 12f);

        float white = (float)(rng.NextDouble() * 2.0 - 1.0);
        pink = 0.94f * pink + 0.06f * white;
        float sample = pink * 0.25f;

        for (int v = 0; v < voiceFreqs.Length; v++)
        {
            float sentenceWave = MathF.Sin(2f * MathF.PI * (v + 1) * 0.25f * t + v * 1.3f);
            float syllableWave = MathF.Max(0f, MathF.Sin(2f * MathF.PI * talkRates[v] * t + v * 2.1f));
            float env = MathF.Max(0f, sentenceWave) * syllableWave;

            float f0 = voiceFreqs[v] + MathF.Sin(t * 5f + v) * 14f;
            float formant = MathF.Sin(2f * MathF.PI * f0 * t) * 0.6f
                          + MathF.Sin(2f * MathF.PI * (f0 * 2.02f) * t) * 0.3f;

            sample += formant * env * 0.045f;
        }

        float clinkTime = (t % 4.0f) - 2.3f;
        if (clinkTime > 0f && clinkTime < 0.08f)
        {
            float clinkEnv = MathF.Exp(-clinkTime * 65f);
            sample += MathF.Sin(2f * MathF.PI * 2450f * clinkTime) * clinkEnv * 0.06f;
        }

        samples[i] = (short)(Math.Clamp(sample * loopEnv, -0.9f, 0.9f) * short.MaxValue);
    }

    Sound snd = BuildSoundFromPcm(samples, sampleRate);
    Raylib.SetSoundVolume(snd, 0.22f);
    return snd;
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

static unsafe void DisableIme()
{
    NativeMethods.ImmAssociateContext((IntPtr)Raylib.GetWindowHandle(), IntPtr.Zero);
}

static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("imm32.dll")]
    public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
}