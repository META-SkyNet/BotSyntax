# Hai Giờ Sáng Và Một Cái Regex

Hùng nhận được alert lúc 2:17 sáng.

Không phải chuông báo thức. Là Slack — kênh `#botsyntax-prod` — một cái đỏ chói:

> `[WARN] CompactStringParser: 47 parse failures in last 5 minutes`
> `Sample input: context('staff', 'emp003').report.top-products(period: 'week')`
> `Error: Invalid compact string format`

Anh ngồi dậy, mắt còn nhắm, mở laptop.

---

Bốn mươi bảy lần thất bại. Trong năm phút.

Hùng nhìn vào cái input lỗi và thấy ngay vấn đề — `top-products`. Dấu gạch ngang. Cái regex đang dùng là `(?<action>[a-z]+)`, chỉ khớp chữ cái thường, không có dấu gạch ngang.

Mọi action tên đơn đều chạy tốt suốt ba tháng. Hôm nay team báo cáo vừa thêm hai action mới: `top-products` và `pending-orders`. Và cái parser không ai nghĩ đến.

Anh gõ vào terminal:

```bash
grep -r "action>" src/BotSyntax.Core/Parser/
```

Tìm ra ngay dòng 13:

```csharp
@"(?<domain>[a-z]+)\.(?<action>[a-z]+)\((?<params>[^)]*)\)$"
```

Một ký tự. Thiếu `[a-z-]*`.

---

Hùng nhìn cái fix. Đơn giản đến mức buồn cười:

```csharp
@"(?<domain>[a-z]+)\.(?<action>[a-z][a-z-]*)\((?<params>[^)]*)\)$"
```

Thêm `-` vào character class. Hai giây gõ. Ba tháng không ai nhìn vào.

Nhưng anh không commit ngay. Anh mở file test trước.

```csharp
[Theory]
[InlineData("context('staff', 'e1').report.top-products(period:'week')")]
[InlineData("context('staff', 'e1').report.pending-orders()")]
[InlineData("context('staff', 'e1').order.status(order_id:123)")]
public void Parse_AcceptsHyphenatedAndSimpleActions(string input)
{
    var result = _parser.Parse(input);
    result.IsSuccess.Should().BeTrue();
}
```

Chạy test. Đỏ. Sửa regex. Chạy lại. Xanh.

Anh commit lúc 2:31 sáng:

```
fix: support hyphenated action names in CompactStringParser

Regex (?<action>[a-z]+) → (?<action>[a-z][a-z-]*)
Triggered by top-products and pending-orders actions added today.
```

Push. Deploy lên staging. Chờ ba phút. Không còn warn nào.

Deploy lên prod lúc 2:44 sáng.

---

Hùng đóng laptop, nằm xuống, nhìn lên trần nhà.

Hai mươi bảy phút. Từ lúc Slack rung đến lúc prod sạch warn.

Anh nghĩ đến 47 request lỗi đó — 47 lần một nhân viên nào đó gõ `/report top-products` và nhận về một câu trả lời trống. Có thể họ thử lại. Có thể họ nghĩ là lỗi mạng. Có thể họ bỏ qua và làm việc khác.

Sáng mai họ sẽ không biết có gì đã được sửa. Cũng không cần biết.

---

Điều Hùng thích nhất ở BotSyntax không phải là kiến trúc đẹp hay test coverage cao — dù cả hai đều có.

Điều anh thích là khi có lỗi, anh biết tìm ở đâu.

Mọi thứ đi qua `CompactString`. Mọi thứ đều có thể log. Mọi thứ đều có thể reproduce bằng một cái string duy nhất:

```
context('staff', 'emp003').report.top-products(period: 'week')
```

Copy cái đó vào test. Chạy. Sửa. Xong.

Không cần mock database. Không cần dựng cả môi trường. Không cần hỏi user đã làm gì.

---

2:44 sáng. Hùng tắt đèn.

Bên ngoài cửa sổ, thành phố vẫn đang chạy. Và ở đâu đó, trên một cái server không tên, BotSyntax đang nhận câu hỏi tiếp theo — lần này không lỗi nữa.

```
context('staff', 'emp003').report.top-products(period: 'week')
→ ExecutionResult.Ok({ top: [...] })
```
