using System.Numerics;
using Raylib_cs;

partial class Program
{
    static readonly Vector3 cafeSeatPos = new(0.0f, 1.32f, -1.15f);
    static readonly Vector3 dungHeadPos = new(0.0f, 1.28f, 0.85f);
    static readonly Vector3 denHeadPos = new(0.95f, 1.28f, -0.15f);
    static readonly Vector3 hungHeadPos = new(-0.95f, 1.28f, -0.15f);

    static readonly List<BoundingBox> solidObjects =
    [
        new(new Vector3(-3.2f, 0f, -2.6f), new Vector3(1.25f, 1.2f, -1.7f)),
        new(new Vector3(-3.2f, 0f, -5.4f), new Vector3(-2.3f, 1.1f, -1.7f)),
        new(new Vector3(-5.05f, 0f, -1.35f), new Vector3(-3.65f, 1.7f, 2.35f)),
        new(new Vector3(-7.45f, 0f, -3.35f), new Vector3(-6.20f, 1.8f, 2.95f)),
        new(new Vector3(1.82f, 0f, 1.25f), new Vector3(2.68f, 1.7f, 2.05f)),
        new(new Vector3(3.52f, 0f, -3.05f), new Vector3(4.18f, 1.85f, 1.45f)),
        new(new Vector3(5.02f, 0f, -3.05f), new Vector3(5.68f, 1.85f, 1.45f)),
        new(new Vector3(6.65f, 0f, -5.1f), new Vector3(7.45f, 2.5f, 5.9f))
    ];

    static readonly BoundingBox rulesPaperBox = new(new Vector3(-1.35f, 1.02f, -2.36f), new Vector3(-0.88f, 1.15f, -1.84f));
    static readonly BoundingBox logbookBox = new(new Vector3(-0.80f, 1.02f, -2.35f), new Vector3(-0.44f, 1.15f, -1.90f));
    static readonly BoundingBox deskPhoneBox = new(new Vector3(-0.35f, 1.05f, -2.35f), new Vector3(0.08f, 1.35f, -1.85f));
    static readonly BoundingBox frontDoorBox = new(new Vector3(-1.2f, 0.0f, 7.2f), new Vector3(1.2f, 2.5f, 7.8f));
    static readonly BoundingBox posterBox = new(new Vector3(-2.35f, 1.20f, 8.85f), new Vector3(-1.85f, 1.90f, 9.30f));

    static readonly (string App, string Sender, string Time, string Body)[] initialPhoneMessages =
    [
        ("Zalo", "Cô Lan (Chủ trọ)", "17:38", "Tú ơi, tháng này tiền trọ 2 triệu 8 nghen. Cô tăng 300 nghìn, điện nước cô tính riêng. Trước mùng 5 hen."),
        ("Ngân hàng", "Biến động số dư", "17:40", "Tài khoản +500.000đ. Số dư hiện tại: 187.400đ."),
        ("Zalo", "Mẹ", "17:41", "Con ăn cơm chưa? Mẹ chuyển 500.000 rồi đó, đừng có nhịn nghen. Tháng này hơi khó khăn, con tiết kiệm chút."),
        ("Zalo", "Hội Cà Phê Chiều Thứ Sáu", "17:42", "Dũng: Ê Tú, đi cà phê hông? Đen bao.")
    ];

    static readonly (string Speaker, string Text)[] cafeDialogue =
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

    static readonly string[] introMonologue =
    [
        "23:45 - Đêm sương dày đặc ở cuối đường Số 9. Phía trước là cửa hàng tiện lợi Thuận Phát 24H rộng lớn sáng trưng ánh đèn huỳnh quang.",
        "Trên cột đèn sát vỉa hè có dán một tờ thông báo cũ đã ố vàng. Bên trong tiệm, nhân viên ca tối đang tất bật kiểm hàng trước giờ bàn giao.",
        "Vào nhận bàn giao ca thôi. Nhớ bật ghi âm điện thoại cho chắc ăn..."
    ];

    static int GetClueCount()
    {
        int count = 0;
        if (readPoster) count++;
        if (readLogbook) count++;
        if (readMargin) count++;
        if (nameTag) count++;
        if (talkedMai) count++;
        return count;
    }

    static float GetSpeakerPitch(string speaker) => speaker switch
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

    static string FormatClock(float totalMinutes)
    {
        int mins = (int)totalMinutes;
        int h = (mins / 60) % 24;
        int m = mins % 60;
        return $"{h:D2}:{m:D2}";
    }

    static void TriggerEnding(int type)
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
            endingTitle = "ENDING 1: MỒI CỦA ĐƯỜNG SỐ 9 (BAD ENDING)";
            string trapReason = spoofAlertShown
                ? "Tin vào tin nhắn giả mạo của Dũng gửi lúc 02:54, Tú đẩy cửa kính bước ra ngoài màn sương."
                : "Tú vừa gạt chốt mở cửa kính lúc 03:00, toàn bộ bóng đèn trong cửa hàng nổ tung.";

            endingText =
                $"{trapReason}\n" +
                "Bóng người mặc áo mưa màu vàng lao sầm tới từ bóng tối với nụ cười rách toạc mang tai...\n" +
                "Sáng hôm sau, người ta chỉ tìm thấy chiếc balo cùng chiếc điện thoại nứt vỡ của Tú nằm lăn lóc dưới quầy thu ngân.";
        }
        else if (type == 2)
        {
            isBadEnding = false;
            endingTitle = "ENDING 2: KẺ THẾ MẠNG MỚI (LOOP ENDING)";
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
            string momLine = lieToMom
                ? "Trở về căn trọ tối om với 187.400đ ít ỏi trong tài khoản, Tú cắn răng nhịn đói, không dám gọi cho mẹ vì chiều nay đã trót nói dối rằng mình còn tiền."
                : "Trở về phòng trọ an toàn, Tú gọi điện thú nhận mọi chuyện với mẹ. Dù tiền trọ tháng này vẫn còn thiếu, giọng mẹ ở đầu dây bên kia khiến cậu nhẹ nhõm phần nào.";

            endingText =
                "Cảm nhận sự bất thường rợn gáy bủa vây khắp căn tiệm, Tú quyết định bỏ ngang ca trực, đẩy cửa kính chạy thẳng về dãy trọ trong đêm sương.\n" +
                momLine + "\n" +
                "Ba tuần sau, tại quán cà phê quen thuộc, Dũng dán mắt vào màn hình điện thoại: 'Ê tụi bay, trên group có chỗ tuyển trực ca đêm 500k một đêm nè... Cho em xin số ạ!'";
        }
        else if (type == 4)
        {
            isBadEnding = false;
            endingTitle = "ENDING 4: NHÂN CHỨNG SỐNG (GOOD ENDING)";
            endingText =
                $"Nhờ bật ghi âm từ đầu ca, Tú gạt phăng tờ hợp đồng của gã Quản lý và chạy thoát khỏi cửa hàng khi trời vừa hửng sáng.\n" +
                $"Tuy nhiên, do chỉ thu thập được {GetClueCount()}/5 manh mối (và thái độ dè dặt khiến nhân chứng chưa mở lời hết), đoạn băng chỉ ghi lại tiếng rè nhiễu cùng tiếng đập kính lúc 03:00.\n" +
                "Cơ quan chức năng không đủ chứng cứ để khởi tố đường dây bí ẩn ở cuối đường Số 9, nhưng ít nhất Tú đã giữ được mạng sống trở về.";
        }
        else if (type == 5)
        {
            isBadEnding = false;
            endingTitle = "ENDING 5: CUỐN BĂNG GHI ÂM LÚC 05:00 (TRUE ENDING)";
            string callNote = calledRealDung
                ? "Đặc biệt, cuộc gọi kiểm chứng với Dũng thật lúc 03:00 đã ghi lại trọn vẹn bằng chứng giọng nói bị đánh cắp."
                : "Từng lời khai của chị Mai, bảng tên Trần Văn Hùng cùng tiếng đập kính lúc 03:00 đều khớp nhau hoàn hảo.";

            endingText =
                $"Với {GetClueCount()}/5 manh mối ghép lại cùng đoạn ghi âm xuyên đêm trong điện thoại, Tú gạt phăng tờ hợp đồng của gã Quản lý và lao thẳng ra khỏi cửa hàng khi trời vừa hửng sáng.\n" +
                $"{callNote} Toàn bộ chứng cứ được Tú nộp lên công an phường, lật tẩy đường dây dụ dỗ sinh viên nghèo vào thế mạng ở cuối đường Số 9.\n" +
                "Tú không nhận được 500 nghìn tiền mặt đêm đó, nhưng khi gọi điện về báo bình an cho mẹ, cậu biết mình đã giữ lại được thứ quý giá nhất.";
        }
    }

    static void ResetGame()
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
        calledRealDung = false;
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
        isCafeCutscene = false;
        cafeCutsceneTimer = 0f;
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
        npcSpec = FearsBodySpec.Manager;
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
        Raylib.StopSound(motorbikeEngineSound);
        Raylib.StopSound(ambientHumSound);
        Raylib.DisableCursor();
    }
}