# -*- coding: utf-8 -*-
import hashlib
import os
import re
import time
from gtts import gTTS

os.makedirs("Assets/Voices", exist_ok=True)

DIALOGUE_LINES = [
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
    ("Dũng", "Vậy mười giờ tao nhắn hỏi thăm. Del rep là tao qua xách đít mày về."),
    ("Tú", "Trước khi rời quán cà phê, mình có nên gửi định vị quán tiện lợi cho Dũng và Đen không?"),
    ("Tú", "(Vừa bấm gửi ghim vị trí đường Số 9 vào nhóm chat, màn hình điện thoại bỗng nháy sọc nhiễu và tự động báo 'Đã xem' ngay lập tức, dù Dũng vẫn đang cắm cúi lướt điện thoại đối diện chưa hề mở tin nhắn...)"),
    ("Quản lý", "Alo? À, sinh viên hả? Được, được. Mười một giờ rưỡi em tới nghen, anh dặn kỹ rồi giao ca luôn. Không cần mang giấy tờ gì đâu."),
    ("Tú", "Trời tối sương bắt đầu xuống rồi... Chạy xe về phòng trọ nghỉ một lát rồi 11 rưỡi qua đường Số 9 nhận ca. (Bấm [W/S] Ga/Phanh, [A/D] Lái, [L] Đèn pha, [Space] Còi)"),
    ("Tú", "(Biển chỉ dẫn bên phải ghi 'Đường Số 9 -> 1.2 km'. Sương ngoài đó dày đặc hơn hẳn khu này...)"),
    ("Tú", "23:45 - Đêm sương dày đặc ở cuối đường Số 9. Phía trước là cửa hàng tiện lợi Thuận Phát 24H rộng lớn sáng trưng ánh đèn huỳnh quang."),
    ("Tú", "Trên cột đèn sát vỉa hè có dán một tờ thông báo cũ đã ố vàng. Bên trong tiệm, nhân viên ca tối đang tất bật kiểm hàng trước giờ bàn giao."),
    ("Tú", "Vào nhận bàn giao ca thôi. Nhớ bật ghi âm điện thoại cho chắc ăn..."),
    ("Tú", "Lời thằng Hưng dặn lúc nãy cũng có lý... Mình nên bật ghi âm trên điện thoại để phòng thân trước khi nhận ca."),
    ("Quản lý", "Tới rồi hả Tú? Ca đêm nhàn lắm, em cứ ngồi quầy đọc kỹ tờ quy tắc rồi trực tới 5 giờ sáng anh quay lại đưa tiền mặt. Nhớ kỹ điều số 3 nha!"),
    ("Áp phích ngã tư", "THÔNG BÁO TÌM NHÂN CHỨNG: Khoảng 02:30 rạng sáng tuần trước, tại ngã tư cuối đường Số 9 xảy ra vụ tai nạn giao thông nghiêm trọng làm một cụ ông 72 tuổi tử vong tại chỗ..."),
    ("Tờ quy tắc ca đêm", "1. Chào hỏi khách đàng hoàng trước khi tính tiền. 2. Tuyệt đối không bán hàng cho ■■■■■■. 3. Đúng 03:00 khóa chặt cửa chính, không mở cho bất kỳ ai."),
    ("Chữ viết tay bên lề", "Dòng chữ chì mờ mờ ghi ở mép dưới: 'Sóng điện thoại trong tiệm này nối thẳng ra ngã tư. Nó đọc lén được tin nhắn gửi vị trí — 3 giờ sáng nó sẽ nhại đúng giọng người bạn vừa nhắn...'"),
    ("Sổ giao ca", "Trang cuối ghi nét bút run rẩy của ca trước (Trần Văn Hùng): 'Đừng tin quản lý. Bài đăng tuyển dụng trên mạng là mồi nhử — hễ ai gửi định vị đường Số 9 cho bạn bè, thứ ở ngã tư sẽ đọc lén được tên người đó và giả danh gọi mở cửa lúc 3 giờ sáng...'"),
    ("Ông cụ", "Chào cháu, lấy cho ông hộp sữa tươi với ổ bánh mì nhé. Ông ở ngay con hẻm đối diện, tối nay khó ngủ nên đi bộ ra mua chút đồ ăn sáng."),
    ("Ông cụ", "Cảm ơn cháu lễ phép nhé. Ông gửi tiền, cháu mới đi làm ca đêm cố gắng giữ sức khỏe nha."),
    ("Ông cụ", "Ông quen đường rồi cháu ơi. Cảm ơn cháu, ông gửi tiền sữa với bánh mì nhé."),
    ("Chị Mai", "Em ơi tính tiền giúp chị ly mì với chai nước suối nhé. Khu này dạo này vắng quá, nhất là từ sau vụ tai nạn ngoài ngã tư tuần trước..."),
    ("Chị Mai", "Tội nghiệp ông cụ tóc bạc ở con hẻm đối diện quán mình nè, đi bộ buổi tối bị xe tải quẹt mất tuần trước rồi, nhà đóng cửa im ỉm mấy nay..."),
    ("Chị Mai", "Ừ, cảm ơn em nha. Chị về trước đây, trực đêm cẩn thận đó."),
    ("Điện thoại bàn", "*Rè rè...* (Đầu dây bên kia không có ai nói, chỉ có tiếng bước chân cộp... cộp... vang lên đều đặn trong tiếng mưa rào)"),
    ("Anh Hùng", "(Trên ngực áo sơ mi của anh ta vẫn đeo bảng tên nhân viên ca đêm: TRẦN VĂN HÙNG) Lấy cho tôi hộp sữa tươi với ổ bánh mì... Tôi ở ngay con hẻm đối diện..."),
    ("Anh Hùng", "Ông cụ đó mất tuần trước rồi... Cậu còn mở cửa bán hàng thì trước 3 giờ nhớ khóa chặt chốt, đừng để thứ ngoài ngã tư lọt vào."),
    ("Anh Hùng", "Không cần mua gì đâu. Nhớ khóa chặt cửa lúc 3 giờ, đừng mở cho bất kỳ ai gõ kính..."),
    ("Tin nhắn (Dũng)", "Điện thoại Tú rung lên tin nhắn từ một tài khoản lạ sao chép y hệt ảnh đại diện Dũng: 'Ê, tao chạy ngang đường Số 9 ghé qua chỗ mày nè, ra mở cửa tao coi.' (Ngoài cửa kính xuất hiện một bóng người đứng khuất trong sương)"),
    ("Tú", "Mình vừa gạt chốt khóa cửa đúng 03:00 thì bóng áo mưa vàng lao sầm tới áp sát cửa kính, đập dồn dập: CỐC! CỐC! CỐC!"),
    ("Cô gái (Áo mưa vàng)", "Bóng áo mưa vàng áp sát mặt kính, nhưng từ cổ họng cô ta lại phát ra giọng hệt thằng Dũng: 'Ê Tú! Tao Dũng nè, mở cửa nhanh đi ngoài này lạnh teo rồi!'"),
    ("Cô gái (Áo mưa vàng)", "Em ơi mở cửa nhanh giúp chị với! Lúc nãy chị làm rơi chùm chìa khóa phòng trọ ngay trước quầy thu ngân!"),
    ("Điện thoại (Dũng)", "Đầu dây bên kia giọng Dũng ngái ngủ: 'Alo? Tao nhắn hỏi thăm hồi 10 giờ không thấy rep nên ngủ luôn rồi, qua đường Số 9 hồi nào!' Ngoài kính, cô gái áo mưa vàng đứng khựng lại, nụ cười tắt ngấm rồi gầm gừ: 'Mắt tinh tú đấy... Tiếc thật.'"),
    ("Cô gái (Áo mưa vàng)", "Cô ta đứng khựng lại, giọng thằng Dũng biến mất, đổi sang tiếng trầm khàn: 'Mắt tinh tú đấy... Tiếc thật.'"),
    ("Cô gái (Áo mưa vàng)", "Cô ta đứng khựng lại, nụ cười tắt ngấm rồi đổi sang giọng trầm khàn: 'Mắt tinh tú đấy... Tiếc thật.'"),
    ("Quản lý", "Làm tốt lắm Tú, đây là phong bì 500 nghìn tiền công đêm nay. Em ký xác nhận vào tờ giấy này rồi tối mai 12 giờ lại qua trực tiếp nhé."),
    ("Quản lý", "Làm tốt lắm Tú, đây là phong bì 500 nghìn tiền công đêm nay của em. Giờ em về nghỉ ngơi đi, tối mai 12 giờ lại qua trực tiếp nhé."),
]

def get_key(speaker: str, text: str) -> str:
    raw = f"{speaker}|{text.strip()}".encode("utf-8")
    return hashlib.md5(raw).hexdigest()[:12].lower()

def clean_for_tts(text: str) -> str:
    s = text.replace("Nigg*", "níc gà").replace("del", "đếch")
    s = s.replace("■■■■■■", "kẻ lạ mặt")
    s = s.replace("->", "còn").replace("*", "")
    s = re.sub(r"\[([^\]]+)\]", r"\1", s)
    return s.strip()

def main():
    total = len(DIALOGUE_LINES)
    for idx, (speaker, text) in enumerate(DIALOGUE_LINES, start=1):
        key = get_key(speaker, text)
        out_path = f"Assets/Voices/{key}.mp3"

        if os.path.exists(out_path) and os.path.getsize(out_path) > 0:
            print(f"[{idx}/{total}] Da co: {out_path}")
            continue

        spoken_text = clean_for_tts(text)
        tts = gTTS(text=spoken_text, lang="vi", slow=False)
        tts.save(out_path)
        print(f"[{idx}/{total}] Da tao [{speaker}]: {out_path}")
        time.sleep(0.2)

if __name__ == "__main__":
    main()
