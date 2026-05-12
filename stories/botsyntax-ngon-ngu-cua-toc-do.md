# BotSyntax — Ngôn Ngữ Của Tốc Độ

Năm 2025, kho hàng của Minh Tuấn ngập trong đơn trả hàng.

Mỗi sáng anh mở máy, ba nhân viên CSKH đang gõ phím loạn xạ trên cửa sổ chat nội bộ. Một người hỏi: *"Đơn 29847 giờ ở đâu rồi?"* Người kia nhắn: *"Khách kêu bảo hành BH2209 nhưng mình không tra được."* Người thứ ba chỉ gửi một dấu chấm than vào void.

Minh Tuấn biết vấn đề không phải ở nhân viên. Vấn đề là mỗi câu hỏi đều cần mở năm tab, đăng nhập hai hệ thống, và copy-paste mã đơn hàng đủ ba lần trước khi trả lời được khách.

---

Người viết ra BotSyntax không phải kỹ sư cao cấp nào. Đó là Lan — một nhân viên CSKH đã làm việc trong kho ba năm, người hiểu rằng 80% câu hỏi của khách đều thuộc về mười cái domain: *đơn hàng, vận chuyển, đổi trả, sản phẩm, tồn kho, bảo hành, công nợ, khiếu nại, báo cáo, giỏ hàng.*

Không nhiều hơn. Không ít hơn.

Lan vẽ ra một ý tưởng trên tờ giấy A4: *Nếu mỗi domain có thể được gọi bằng một câu lệnh duy nhất thì sao?*

```
/order status 29847
/ship status 29847
/returns create --order_id=29847 --reason="lỗi sản phẩm"
```

Ba giây. Không cần mở tab. Không cần đăng nhập lại.

---

BotSyntax được xây dựng từ triết lý đó.

Ở tầng dưới cùng là một **CompactString** — ngôn ngữ trung gian mà mọi input đều được dịch về trước khi thực thi. Dù nhân viên gõ slash command, dù khách nhắn tin tự nhiên bằng tiếng Việt, dù hệ thống khác gọi API — tất cả đều trở thành cùng một dạng:

```
context('staff', 'emp001').order.status(order_id: 29847)
```

Rõ ràng. Không nhập nhằng. Có thể log, có thể debug, có thể kiểm tra quyền.

Ở tầng trên là **NLU** — không phải AI đắt tiền, mà là những regex được viết tay bởi người hiểu tiếng Việt thực tế. *"Đơn hàng của tôi đang ở đâu rồi"* → `order.status`. *"Muốn trả hàng đơn 12345"* → `returns.create`. Đủ dùng, đủ nhanh, đủ rẻ.

Và ở giữa là **BotExecutor** — kẻ gác cổng biết ai được làm gì, domain nào thuộc về handler nào.

---

Sáu tháng sau ngày ra mắt, kho hàng của Minh Tuấn thay đổi.

Ba nhân viên CSKH giờ xử lý gấp đôi lượng ticket. Không phải vì họ làm nhanh hơn — mà vì họ không còn mất thời gian tra cứu nữa. Một lệnh. Một giây. Một câu trả lời.

Lan, người vẽ ý tưởng trên tờ giấy A4, giờ là trưởng nhóm sản phẩm.

---

BotSyntax không phải chatbot thông minh nhất thế giới.

Nó chỉ là một ngôn ngữ — được thiết kế bởi người hiểu rằng sự phức tạp thực sự không nằm ở công nghệ, mà nằm ở khoảng cách giữa câu hỏi của con người và dữ liệu trong hệ thống.

Và đôi khi, thu hẹp khoảng cách đó chỉ cần một dấu gạch chéo.

```
/
```
