# BotSyntax — Đặc tả Cú pháp Chatbot Thương mại Điện tử

> **Phiên bản:** 1.0.0 | **Ngày:** 08/05/2026  
> **Đối tượng:** Developer / BA (implement), Nhân viên (sử dụng lệnh)

---

## Mục lục

1. [Tổng quan & Quy ước ký hiệu](#0-tổng-quan--quy-ước-ký-hiệu)
2. [Kiến trúc Compiler & Root Command](#05-kiến-trúc-compiler--root-command)
3. [Shared Entities](#1-shared-entities)
3. [Đơn hàng (Orders)](#2-đơn-hàng-orders)
4. [Giao hàng (Shipping)](#3-giao-hàng-shipping)
5. [Đổi trả & Hoàn tiền (Returns & Refunds)](#4-đổi-trả--hoàn-tiền-returns--refunds)
6. [Sản phẩm (Products)](#5-sản-phẩm-products)
7. [Kho & Nhập hàng (Inventory)](#6-kho--nhập-hàng-inventory)
8. [Bảo hành (Warranty)](#7-bảo-hành-warranty)
9. [Công nợ & Đối soát (Debt & Reconciliation)](#8-công-nợ--đối-soát-debt--reconciliation)
10. [Chăm sóc khách hàng (Customer Support)](#9-chăm-sóc-khách-hàng-customer-support)
11. [Báo cáo & Thống kê (Reports)](#10-báo-cáo--thống-kê-reports)
12. [Giỏ hàng & Thanh toán (Cart & Payment)](#11-giỏ-hàng--thanh-toán-cart--payment)
13. [Appendix A — Bảng lệnh nhân viên](#appendix-a--bảng-lệnh-nhân-viên)
14. [Appendix B — Mẫu câu khách hàng](#appendix-b--mẫu-câu-khách-hàng)

---

## 0. Tổng quan & Quy ước ký hiệu

### Mục đích

Tài liệu này định nghĩa toàn bộ cú pháp tương tác với chatbot TMĐT, bao gồm:
- **Ngôn ngữ tự nhiên** dành cho khách hàng (NLU patterns)
- **Lệnh có cấu trúc** dành cho nhân viên (Slash commands)

Bot vận hành đa kênh (web widget + mạng xã hội) trên một engine duy nhất, phân quyền theo vai trò người dùng.

### Quy ước ký hiệu

| Ký hiệu | Ý nghĩa | Ví dụ |
|---------|---------|-------|
| `<PARAM>` | Tham số bắt buộc | `<ORDER_ID>` |
| `[PARAM]` | Tham số tùy chọn | `[DATE_RANGE]` |
| `A \| B` | Chọn một trong hai | `đổi \| trả` |
| `...` | Lặp lại nhiều giá trị | `<SKU> ...` |
| `*` | Wildcard / tất cả | `/order list *` |
| `UPPER_CASE` | Tên entity hoặc kiểu dữ liệu | `ORDER_ID` |
| `/slash` | Lệnh nhân viên | `/order status` |
| `~ phrase ~` | Mẫu câu tự nhiên của khách | `~ đơn hàng của tôi ~` |
| `--flag` | Flag tùy chọn toàn cục | `--format=csv` |

### Hệ thống phân quyền nhân viên

| Cấp | Role | Quyền |
|-----|------|-------|
| 1 | `STAFF` | Xem, tra cứu, ghi chú |
| 2 | `WAREHOUSE` | + Cập nhật kho, nhập hàng |
| 3 | `SUPERVISOR` | + Hủy đơn, xử lý đổi trả, bảo hành |
| 4 | `ACCOUNTANT` | + Gạch nợ, đối soát tài chính |
| 5 | `MANAGER` | Toàn quyền + báo cáo |

> Mỗi cấp kế thừa toàn bộ quyền của cấp thấp hơn.

### Flags toàn cục

Áp dụng cho mọi lệnh nhân viên:

| Flag | Ý nghĩa |
|------|---------|
| `--help` | Hiển thị hướng dẫn lệnh đó |
| `--format=csv` | Xuất kết quả dạng CSV |
| `--page=N` | Phân trang, N là số trang |
| `--limit=N` | Giới hạn số kết quả trả về |

### Xử lý không dấu và viết tắt (khách hàng)

Bot chấp nhận:
- Viết không dấu: `don hang`, `kiem tra`, `bao hanh`, `giao hang`
- Viết tắt phổ biến: `DH` (đơn hàng), `SP` (sản phẩm), `BH` (bảo hành), `KH` (khách hàng)
- Không phân biệt hoa/thường

---

## 0.5 Kiến trúc Compiler & Root Command

### Mô hình tầng (Layer Model)

Hệ thống có 4 tầng từ sâu nhất đến bề mặt. Tầng sâu hơn là **ground truth** — tầng nông hơn là **biểu diễn** hoặc **sugar syntax** của nó:

```
┌─────────────────────────────────────────────────────────────────┐
│  TẦNG 4 — Human Input (bề mặt)                                  │
│                                                                  │
│  [A] Slash command          [B] Natural language                 │
│  /order status 10234        "đơn 10234 đang ở đâu"              │
│  (nhân viên)                (khách hàng)                         │
└─────────────────────────┬───────────────────────────────────────┘
                          │  Compiler (algorithm | AI Agent)
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│  TẦNG 3 — JSON Object (transport & storage)                     │
│                                                                  │
│  { "root": "context('staff','emp001').order.status(10234)",     │
│    "domain": "order", "action": "status", ... }                 │
└─────────────────────────┬───────────────────────────────────────┘
                          │  serialize / deserialize (lossless)
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│  TẦNG 2 — Compact String (C# interpreted expression)            │
│                                                                  │
│  context('staff', 'emp001').order.status(10234)                 │
│  context('customer', guest).order.status(10234)                 │
└─────────────────────────┬───────────────────────────────────────┘
                          │  Roslyn / C# interpreter
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│  TẦNG 1 — Thực thi (tầng sâu nhất)                             │
│                                                                  │
│  Bot engine dispatch handler theo domain + action + params      │
└─────────────────────────────────────────────────────────────────┘
```

> **Nguyên tắc:** Compact string (tầng 2) là **C# expression chạy được** — `context()` khởi tạo execution context, chain `.domain.action()` là method call thực sự. JSON (tầng 3) là serialization để transport/log. Human input (tầng 4) là sugar syntax được compile xuống compact string.

---

### Input A — Slash Command (Nhân viên)

Nhân viên gõ lệnh cấu trúc. Compiler phân tích cú pháp tĩnh (pure algorithm):

```
/order status 10234
 ──────┬────── ──┬─── ─────┬─────
    domain    action     param[0]
```

**Quy tắc parse:**
1. Token đầu tiên sau `/` → `domain`
2. Token thứ hai → `action`
3. Token thứ ba (nếu không có `--`) → positional param đầu tiên
4. `--key=value` hoặc `--key "value"` → named params
5. `--flag` không có giá trị → boolean flag = `true`

**Output:** Compact string (tầng 2)

---

### Input B — Natural Language (Khách hàng)

Khách hàng gõ câu tự nhiên. Compiler có **hai chế độ** — cấu hình tại deploy time:

#### Chế độ 1: Pure Algorithm (Rule-based NLU)

Pattern matching theo danh sách intent định nghĩa trong spec này:
1. Khớp keyword domain (`đơn hàng`, `giao hàng`, `bảo hành`...)
2. Khớp intent pattern (`~ ... ~`) từ cụ thể → chung
3. Extract entity bằng regex và vị trí trong câu
4. Slot filling nếu thiếu entity bắt buộc

**Output:** Compact string (tầng 2)

#### Chế độ 2: AI Agent API

Gửi input và context lên AI Agent, nhận về compact string:

**Request gửi lên AI Agent:**
```json
{
  "input": "đơn 10234 đang ở đâu",
  "context": {
    "role": "CUSTOMER",
    "channel": "zalo",
    "session_id": "sess_abc123",
    "history": []
  },
  "output_format": "compact_string_v1"
}
```

**Response từ AI Agent:**
```json
{
  "compact": "context('customer', 'cus5501').order.status(10234)",
  "confidence": 0.97,
  "slot_filled": true
}
```

Compiler nhận compact string từ AI Agent → validate schema → tầng 2.

---

### Tầng 2 — Compact String (C# Interpreted Expression)

Compact string **là một lệnh C# được thông dịch** — cú pháp C# hợp lệ, chạy trực tiếp qua **Roslyn Scripting API** hoặc C# interpreter tùy chỉnh.

`context(role, user)` là method định nghĩa sẵn trong script context, trả về fluent builder object. Chain `.order.status(1234)` là C# method chain thực sự.

> **Lưu ý quote:** Interpreter hỗ trợ cả single quote `'` lẫn double quote `"` cho string literal — đây là custom extension so với C# chuẩn (vốn chỉ dùng `"`). `guest` không có quote là C# constant/static property đã định nghĩa sẵn.

> **Chạy trực tiếp bằng Roslyn:**
> ```csharp
> // Script context đã định nghĩa sẵn: context(), Guest, domain objects
> context('staff', 'emp001').order.status(1234)
> context('customer', 'cus5501').order.status(1234)
> context('customer', guest).order.status(1234)
> context('staff', 'emp001').ship.assign(order_id: 10234, carrier: 'GHN')
> ```

#### Compact String Format

```
context(<role>, <identity>).<domain>.<action>(<params>)
```

| Phần | Bắt buộc | Mô tả |
|------|----------|-------|
| `context(role, user)` | Có | Khởi tạo execution context với role và identity |
| `role` | Có | `'staff'`, `'customer'`, `'warehouse'`, `'supervisor'`, `'accountant'`, `'manager'` |
| `user` | Có | `'username'` (đã đăng nhập) hoặc `guest` (ẩn danh — C# constant, không có quote) |
| `domain` | Có | `order`, `ship`, `returns`, `product`, `stock`, `warranty`, `debt`, `customer`, `report`, `cart` |
| `action` | Có | `status`, `list`, `create`, `update`, `cancel`, `confirm`, `search`, `note`, `today`... |
| `(params)` | Không | `key=value` hoặc `key: value` — số không cần quote, string cần `'` hoặc `"` |

#### Phân biệt user identity

| Dạng | Ý nghĩa | Ví dụ |
|------|---------|-------|
| `'username'` | Người dùng đã đăng nhập, có định danh | `context('customer', 'cus5501')` |
| `guest` | Người dùng ẩn danh, chưa đăng nhập | `context('customer', guest)` |

#### Ví dụ

```csharp
context('staff', 'emp001').order.status(1234)
context('customer', 'cus5501').order.status(1234)
context('customer', guest).order.status(1234)

context('staff', 'emp001').order.list(status: 'PENDING', date: '01/05/2026~08/05/2026')
context('staff', 'emp001').ship.assign(order_id: 10234, carrier: 'GHN')
context('supervisor', 'sup01').order.cancel(order_id: 10234, reason: 'khách yêu cầu')
context('accountant', 'acc01').debt.confirm(debt_id: 'CN001', amount: 5000000)
context('customer', 'cus5501').returns.create(order_id: 10234, type: 'exchange')
context('manager', 'mgr01').report.today()
context('customer', guest).product.search(keyword: 'hoodie', price_max: 500000)
```

> **Lưu ý domain `returns`:** Đặt tên `returns` (không phải `return`) vì `return` là reserved keyword trong C# — sẽ gây compile error nếu dùng làm property name.

#### Quy tắc parse Compact String

1. Match `context(` → parse `role` (string) và `user` (string hoặc constant `guest`)
2. Sau `)` gặp `.` → parse `domain`
3. Sau `.domain` gặp `.` → parse `action` và `(params)`
4. Parse params: `key=value` hoặc `key: value` — số là int/float, string trong `'` hoặc `"`
5. Bot engine cast value sang kiểu đúng theo entity schema

---

### Tầng 3 — JSON Object (Serialization)

JSON là **serialization lossless** của compact string — dùng để transport qua API, lưu log, gửi qua message queue. Không thêm thông tin nghiệp vụ so với compact string; chỉ thêm `context` và `meta` từ runtime.

```json
{
  "root":    "<compact string — tầng 1>",
  "domain":  "<string>",
  "action":  "<string>",
  "params":  { "<key>": "<value>", ... },
  "context": {
    "role":       "<CUSTOMER|STAFF|WAREHOUSE|SUPERVISOR|ACCOUNTANT|MANAGER>",
    "channel":    "<web|zalo|messenger|telegram|mobile>",
    "session_id": "<string>",
    "user_id":    "<string|null>",
    "timestamp":  "<ISO 8601>"
  },
  "meta": {
    "input_type":  "<slash|natural|compact>",
    "compiler":    "<algorithm|ai_agent>",
    "confidence":  "<float 0–1 | null>",
    "raw_input":   "<string>"
  }
}
```

**Ví dụ — cùng intent từ 3 input khác nhau, hội tụ về cùng `root`:**

```json
// Input A: /order status 10234  (nhân viên)
{
  "root": "context('staff', 'emp001').order.status(10234)",
  "domain": "order", "action": "status",
  "params": { "order_id": 10234 },
  "context": { "role": "staff", "identity": "emp001", "channel": "web", "timestamp": "2026-05-08T10:30:00+07:00" },
  "meta": { "input_type": "slash", "compiler": "algorithm", "confidence": null, "raw_input": "/order status 10234" }
}

// Input B: "đơn 10234 đang ở đâu"  (khách hàng, AI Agent)
{
  "root": "context('customer', 'cus5501').order.status(10234)",
  "domain": "order", "action": "status",
  "params": { "order_id": 10234 },
  "context": { "role": "customer", "identity": "cus5501", "channel": "zalo", "timestamp": "2026-05-08T10:31:00+07:00" },
  "meta": { "input_type": "natural", "compiler": "ai_agent", "confidence": 0.97, "raw_input": "đơn 10234 đang ở đâu" }
}

// Input C: compact string trực tiếp (khách ẩn danh)
{
  "root": "context('customer', guest).order.status(10234)",
  "domain": "order", "action": "status",
  "params": { "order_id": 10234 },
  "context": { "role": "customer", "identity": null, "channel": "web", "timestamp": "2026-05-08T10:31:00+07:00" },
  "meta": { "input_type": "compact", "compiler": "algorithm", "confidence": null, "raw_input": "context('customer', guest).order.status(10234)" }
}
```

> **Nhận xét:** Trường `root` luôn giống nhau cho cùng intent — bot engine route theo `root`, không quan tâm input đến từ đâu.

---

### Xử lý lỗi Compiler

| Lỗi | Mô tả | Hành động |
|-----|-------|-----------|
| `UNKNOWN_INTENT` | Không khớp được intent nào | Hỏi lại người dùng |
| `MISSING_PARAM` | Thiếu tham số bắt buộc | Slot filling — hỏi từng param |
| `INVALID_PARAM` | Giá trị param sai kiểu/format | Thông báo lỗi + gợi ý format đúng |
| `PERMISSION_DENIED` | Role không đủ quyền thực hiện action | Từ chối + ghi log audit |
| `AMBIGUOUS_INTENT` | Confidence < 0.6 (AI mode) | Đưa ra 2–3 lựa chọn để xác nhận |
| `COMPILER_ERROR` | Lỗi nội bộ compiler | Fallback sang human support |

---

## 1. Shared Entities

Định nghĩa tập trung — tham chiếu ở mọi domain, không định nghĩa lại.

| Entity | Kiểu | Định dạng | Ví dụ |
|--------|------|-----------|-------|
| `ORDER_ID` | string \| number | Chuỗi tự do hoặc số | `10234`, `DH20240501` |
| `SKU` | string | Chuỗi tự do, không dấu | `AO-HOODIE-L-DEN`, `SP001` |
| `CUSTOMER_ID` | string \| number | Chuỗi tự do hoặc số | `5501`, `KH00123` |
| `DATE` | date | `DD/MM/YYYY` | `08/05/2026` |
| `DATE_RANGE` | date range | `DATE~DATE` | `01/05/2026~08/05/2026` |
| `QUANTITY` | integer | Số nguyên dương | `5` |
| `PRICE` | number | VND, không dấu chấm | `250000` |
| `STATUS_CODE` | enum | Định nghĩa per-domain | `PENDING`, `SHIPPED` |
| `PHONE` | string | 10 số, bắt đầu 0 | `0912345678` |
| `WARRANTY_CODE` | string \| number | Chuỗi tự do hoặc số | `BH00456`, `7789` |
| `CARRIER_CODE` | enum | `GHN\|GHTK\|VNPOST\|BEST\|VIETTEL` | `GHN` |
| `DEBT_ID` | string \| number | Chuỗi tự do hoặc số | `CN001`, `9988` |
| `RETURN_ID` | string \| number | Chuỗi tự do hoặc số | `TRA001`, `4412` |
| `IMPORT_ID` | string \| number | Chuỗi tự do hoặc số | `NK001`, `3301` |
| `COMPLAINT_ID` | string \| number | Chuỗi tự do hoặc số | `KN001`, `7712` |
| `CART_ITEM_ID` | string \| number | Chuỗi tự do hoặc số | `GH001`, `891` |
| `VOUCHER_CODE` | string | Chuỗi tự do | `SALE50`, `FREESHIP` |

---

## 2. Đơn hàng (Orders)

### 2.1 Tra cứu trạng thái đơn hàng

- **Vai trò:** CẢ HAI
- **Mô tả:** Xem trạng thái hiện tại của một đơn hàng

#### Cú pháp khách hàng

```
~ đơn [hàng] [của tôi] ~
~ đơn <ORDER_ID> [đang ở đâu | ở đâu rồi | như thế nào | ra sao] ~
~ kiểm tra [đơn] <ORDER_ID> ~
~ tôi đặt [hàng] [hôm qua | hôm nay] ~
```

> Ví dụ: `"đơn 10234 đang ở đâu"`, `"kiểm tra đơn hàng của tôi"`, `"tôi đặt hàng hôm qua chưa thấy giao"`

#### Lệnh nhân viên

```
/order status <ORDER_ID>
```

> Ví dụ: `/order status 10234`

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `ORDER_ID` | string\|number | Có* | *Khách đã đăng nhập có thể bỏ qua để xem tất cả đơn |

#### Response mẫu

Trả về: mã đơn, trạng thái, ngày đặt, danh sách sản phẩm, địa chỉ giao, đơn vị vận chuyển, mã vận đơn.

#### Status codes

| Code | Ý nghĩa |
|------|---------|
| `PENDING` | Chờ xác nhận |
| `CONFIRMED` | Đã xác nhận |
| `PROCESSING` | Đang xử lý |
| `SHIPPING` | Đang giao |
| `DELIVERED` | Đã giao |
| `CANCELLED` | Đã hủy |
| `RETURNED` | Đã đổi/trả |

#### Lỗi & Edge case

- `ORDER_ID` không tồn tại → thông báo lỗi, gợi ý kiểm tra lại mã
- Khách chưa đăng nhập và không cung cấp `ORDER_ID` → yêu cầu đăng nhập hoặc nhập mã đơn
- Nhân viên truy cập đơn không thuộc phạm vi quản lý → từ chối, ghi log audit

---

### 2.2 Danh sách đơn hàng

- **Vai trò:** EMPLOYEE
- **Mô tả:** Liệt kê đơn hàng theo bộ lọc

#### Lệnh nhân viên

```
/order list [--status=<STATUS_CODE>] [--date=<DATE_RANGE>] [--customer=<CUSTOMER_ID>]
```

> Ví dụ: `/order list --status=PENDING --date=01/05/2026~08/05/2026`

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `--status` | STATUS_CODE | Không | Lọc theo trạng thái |
| `--date` | DATE_RANGE | Không | Lọc theo khoảng ngày đặt |
| `--customer` | CUSTOMER_ID | Không | Lọc theo khách hàng |

#### Quyền tối thiểu: `STAFF`

---

### 2.3 Xác nhận đơn hàng

- **Vai trò:** EMPLOYEE
- **Mô tả:** Xác nhận đơn hàng đang ở trạng thái PENDING

#### Lệnh nhân viên

```
/order confirm <ORDER_ID>
```

#### Quyền tối thiểu: `STAFF`

---

### 2.4 Hủy đơn hàng

- **Vai trò:** CẢ HAI
- **Mô tả:** Hủy một đơn hàng chưa giao

#### Cú pháp khách hàng

```
~ hủy đơn [hàng] [<ORDER_ID>] ~
~ tôi muốn hủy [đơn] <ORDER_ID> ~
~ không muốn mua nữa ~
```

> Ví dụ: `"hủy đơn 10234"`, `"tôi muốn hủy đơn hàng của tôi"`

#### Lệnh nhân viên

```
/order cancel <ORDER_ID> [--reason="<lý do>"]
```

> Ví dụ: `/order cancel 10234 --reason="khách yêu cầu"`

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `ORDER_ID` | string\|number | Có | Mã đơn cần hủy |
| `--reason` | string | Không | Lý do hủy |

#### Quyền tối thiểu: `SUPERVISOR`

#### Lỗi & Edge case

- Đơn đã ở trạng thái `SHIPPING` hoặc `DELIVERED` → không thể hủy, bot hướng dẫn làm đổi/trả
- Đơn đã `CANCELLED` → thông báo đã hủy trước đó

---

### 2.5 Cập nhật trạng thái đơn hàng

- **Vai trò:** EMPLOYEE
- **Mô tả:** Thay đổi trạng thái đơn thủ công

#### Lệnh nhân viên

```
/order update <ORDER_ID> --status=<STATUS_CODE> [--note="<ghi chú>"]
```

> Ví dụ: `/order update 10234 --status=CONFIRMED --note="xác nhận qua điện thoại"`

#### Quyền tối thiểu: `SUPERVISOR`

---

### 2.6 Ghi chú đơn hàng

- **Vai trò:** EMPLOYEE
- **Mô tả:** Thêm ghi chú nội bộ vào đơn hàng

#### Lệnh nhân viên

```
/order note <ORDER_ID> "<nội dung ghi chú>"
```

> Ví dụ: `/order note 10234 "khách yêu cầu giao trước 18h"`

#### Quyền tối thiểu: `STAFF`

---

## 3. Giao hàng (Shipping)

### 3.1 Tra cứu vận đơn

- **Vai trò:** CẢ HAI
- **Mô tả:** Xem thông tin và trạng thái vận đơn

#### Cú pháp khách hàng

```
~ vận đơn <ORDER_ID> ~
~ hàng [của tôi] đến chưa ~
~ giao hàng [đơn <ORDER_ID>] bao giờ [tới | đến] ~
~ shipper ở đâu rồi ~
```

> Ví dụ: `"vận đơn 10234 đang ở đâu"`, `"hàng tôi đến chưa"`, `"shipper ở đâu rồi"`

#### Lệnh nhân viên

```
/ship status <ORDER_ID>
```

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `ORDER_ID` | string\|number | Có* | *Khách đăng nhập có thể bỏ qua |

#### Quyền tối thiểu: `STAFF`

---

### 3.2 Gán đơn vị vận chuyển

- **Vai trò:** EMPLOYEE
- **Mô tả:** Phân công đơn vị vận chuyển cho đơn hàng

#### Lệnh nhân viên

```
/ship assign <ORDER_ID> <CARRIER_CODE>
```

> Ví dụ: `/ship assign 10234 GHN`

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `ORDER_ID` | string\|number | Có | Mã đơn hàng |
| `CARRIER_CODE` | enum | Có | Đơn vị vận chuyển |

#### Quyền tối thiểu: `STAFF`

---

### 3.3 Xử lý giao hàng thất bại

- **Vai trò:** EMPLOYEE
- **Mô tả:** Ghi nhận và xử lý đơn giao không thành công

#### Lệnh nhân viên

```
/ship fail <ORDER_ID> --reason="<lý do>" [--action=retry|return|cancel]
```

> Ví dụ: `/ship fail 10234 --reason="khách không nghe máy" --action=retry`

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `ORDER_ID` | string\|number | Có | Mã đơn |
| `--reason` | string | Có | Lý do giao thất bại |
| `--action` | enum | Không | Xử lý tiếp: `retry` (giao lại), `return` (hoàn hàng), `cancel` (hủy) |

#### Quyền tối thiểu: `SUPERVISOR`

---

### 3.4 Thay đổi địa chỉ giao hàng

- **Vai trò:** CẢ HAI
- **Mô tả:** Cập nhật địa chỉ giao trước khi hàng được lấy

#### Cú pháp khách hàng

```
~ đổi [địa chỉ] giao hàng [đơn <ORDER_ID>] ~
~ giao [đến] địa chỉ khác ~
~ thay địa chỉ đơn <ORDER_ID> ~
```

> Ví dụ: `"đổi địa chỉ giao hàng đơn 10234"`, `"tôi muốn giao đến địa chỉ khác"`

#### Lệnh nhân viên

```
/ship address <ORDER_ID> "<địa chỉ mới>" [--phone=<PHONE>]
```

> Ví dụ: `/ship address 10234 "123 Nguyễn Huệ, Q1, HCM" --phone=0912345678`

#### Quyền tối thiểu: `STAFF`

#### Lỗi & Edge case

- Đơn đã được lấy hàng (trạng thái `SHIPPING`) → không thể đổi địa chỉ, thông báo liên hệ CSKH

---

### 3.5 Danh sách đơn giao theo ngày

- **Vai trò:** EMPLOYEE
- **Mô tả:** Xem danh sách đơn giao trong khoảng thời gian

#### Lệnh nhân viên

```
/ship list [--date=<DATE_RANGE>] [--carrier=<CARRIER_CODE>] [--status=<STATUS>]
```

> Ví dụ: `/ship list --date=08/05/2026~08/05/2026 --carrier=GHN`

#### Quyền tối thiểu: `STAFF`

---

## 4. Đổi trả & Hoàn tiền (Returns & Refunds)

### 4.1 Yêu cầu đổi hàng

- **Vai trò:** CẢ HAI
- **Mô tả:** Khách yêu cầu đổi sản phẩm, nhân viên tạo phiếu đổi

#### Cú pháp khách hàng

```
~ [tôi muốn] đổi hàng [đơn <ORDER_ID>] ~
~ sản phẩm [bị] lỗi [muốn] đổi ~
~ đổi [sang] <tên sản phẩm | SKU> ~
```

> Ví dụ: `"tôi muốn đổi hàng đơn 10234"`, `"sản phẩm lỗi muốn đổi"`, `"đổi sang size L"`

#### Lệnh nhân viên

```
/returns create <ORDER_ID> --type=exchange [--sku=<SKU>] [--reason="<lý do>"]
```

> Ví dụ: `/returns create 10234 --type=exchange --sku=AO-HOODIE-L-DEN --reason="sai size"`

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `ORDER_ID` | string\|number | Có | Mã đơn hàng gốc |
| `--type` | enum | Có | `exchange` (đổi) hoặc `return` (trả) |
| `--sku` | SKU | Không | Sản phẩm muốn đổi sang |
| `--reason` | string | Không | Lý do đổi |

#### Quyền tối thiểu: `SUPERVISOR`

---

### 4.2 Yêu cầu trả hàng & hoàn tiền

- **Vai trò:** CẢ HAI
- **Mô tả:** Khách trả hàng và yêu cầu hoàn tiền

#### Cú pháp khách hàng

```
~ [tôi muốn] trả hàng [đơn <ORDER_ID>] ~
~ hoàn tiền [đơn <ORDER_ID>] ~
~ hàng lỗi [muốn] trả lại ~
~ refund [đơn <ORDER_ID>] ~
```

> Ví dụ: `"tôi muốn trả hàng"`, `"hoàn tiền đơn 10234"`, `"hàng lỗi trả lại được không"`

#### Lệnh nhân viên

```
/returns create <ORDER_ID> --type=return [--reason="<lý do>"] [--refund=<PRICE>]
```

> Ví dụ: `/returns create 10234 --type=return --reason="hàng bị lỗi" --refund=250000`

#### Quyền tối thiểu: `SUPERVISOR`

---

### 4.3 Tra cứu trạng thái đổi trả

- **Vai trò:** CẢ HAI
- **Mô tả:** Xem tiến độ xử lý yêu cầu đổi/trả

#### Cú pháp khách hàng

```
~ [đơn] đổi trả [<RETURN_ID>] đến đâu rồi ~
~ yêu cầu hoàn tiền [của tôi] [<RETURN_ID>] ~
~ trạng thái trả hàng <RETURN_ID> ~
```

> Ví dụ: `"đơn đổi trả của tôi đến đâu rồi"`, `"yêu cầu hoàn tiền TRA001"`

#### Lệnh nhân viên

```
/returns status <RETURN_ID>
```

#### Quyền tối thiểu: `STAFF`

#### Status codes đổi trả

| Code | Ý nghĩa |
|------|---------|
| `REQUESTED` | Đã yêu cầu |
| `APPROVED` | Đã duyệt |
| `REJECTED` | Từ chối |
| `PROCESSING` | Đang xử lý |
| `REFUNDED` | Đã hoàn tiền |
| `EXCHANGED` | Đã đổi hàng |

---

### 4.4 Duyệt / Từ chối yêu cầu đổi trả

- **Vai trò:** EMPLOYEE
- **Mô tả:** Xử lý phê duyệt yêu cầu đổi/trả từ khách

#### Lệnh nhân viên

```
/returns approve <RETURN_ID> [--note="<ghi chú>"]
/returns reject <RETURN_ID> --reason="<lý do từ chối>"
```

> Ví dụ: `/returns approve TRA001`, `/returns reject TRA001 --reason="quá thời hạn đổi trả"`

#### Quyền tối thiểu: `SUPERVISOR`

---

## 5. Sản phẩm (Products)

### 5.1 Tìm kiếm sản phẩm

- **Vai trò:** CẢ HAI
- **Mô tả:** Tìm sản phẩm theo tên, SKU, danh mục hoặc bộ lọc

#### Cú pháp khách hàng

```
~ tìm <tên sản phẩm | SKU> ~
~ có [còn] <tên sản phẩm> [không] ~
~ <tên sản phẩm> [giá] bao nhiêu ~
~ gợi ý [cho tôi] <danh mục> [dưới <PRICE>] ~
~ tìm quà [tặng] [cho <đối tượng>] [dưới <PRICE>] ~
```

> Ví dụ: `"áo hoodie size L còn không"`, `"tìm quà tặng cho bạn trai dưới 500k"`, `"giá điện thoại X bao nhiêu"`

#### Lệnh nhân viên

```
/product search "<từ khóa>" [--sku=<SKU>] [--category="<danh mục>"] [--price-max=<PRICE>]
```

> Ví dụ: `/product search "hoodie" --category="áo" --price-max=500000`

#### Quyền tối thiểu: `STAFF`

---

### 5.2 Xem chi tiết sản phẩm

- **Vai trò:** CẢ HAI
- **Mô tả:** Xem thông tin đầy đủ của một sản phẩm

#### Cú pháp khách hàng

```
~ thông tin [sản phẩm] <tên sản phẩm | SKU> ~
~ [sản phẩm] <tên> có những [loại | màu | size] gì ~
```

> Ví dụ: `"thông tin áo hoodie đen"`, `"sản phẩm SP001 có những size gì"`

#### Lệnh nhân viên

```
/product info <SKU>
```

#### Quyền tối thiểu: `STAFF`

---

### 5.3 Cập nhật giá sản phẩm

- **Vai trò:** EMPLOYEE
- **Mô tả:** Thay đổi giá bán của sản phẩm

#### Lệnh nhân viên

```
/product update <SKU> --price=<PRICE>
```

> Ví dụ: `/product update AO-HOODIE-L-DEN --price=299000`

#### Quyền tối thiểu: `SUPERVISOR`

---

### 5.4 Cập nhật trạng thái hiển thị

- **Vai trò:** EMPLOYEE
- **Mô tả:** Bật/tắt hiển thị sản phẩm trên kênh bán

#### Lệnh nhân viên

```
/product update <SKU> --status=active|inactive
```

> Ví dụ: `/product update AO-HOODIE-L-DEN --status=inactive`

#### Quyền tối thiểu: `SUPERVISOR`

---

### 5.5 Kiểm tra tồn kho sản phẩm

- **Vai trò:** CẢ HAI
- **Mô tả:** Xem số lượng tồn kho hiện tại

#### Cú pháp khách hàng

```
~ <tên sản phẩm | SKU> còn hàng không ~
~ còn bao nhiêu <tên sản phẩm> ~
```

> Ví dụ: `"áo hoodie đen L còn hàng không"`, `"còn bao nhiêu SP001"`

#### Lệnh nhân viên

```
/product stock <SKU>
```

#### Quyền tối thiểu: `STAFF`

---

## 6. Kho & Nhập hàng (Inventory)

### 6.1 Cập nhật tồn kho

- **Vai trò:** EMPLOYEE
- **Mô tả:** Điều chỉnh số lượng tồn kho thủ công

#### Lệnh nhân viên

```
/stock update <SKU> <+QUANTITY|-QUANTITY>
```

> Ví dụ: `/stock update AO-HOODIE-L-DEN +50` (nhập thêm), `/stock update AO-HOODIE-L-DEN -5` (xuất kho)

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `SKU` | string | Có | Mã sản phẩm |
| `QUANTITY` | integer | Có | Số lượng với dấu `+` (nhập) hoặc `-` (xuất) |

#### Quyền tối thiểu: `WAREHOUSE`

---

### 6.2 Tạo phiếu nhập hàng

- **Vai trò:** EMPLOYEE
- **Mô tả:** Ghi nhận lô hàng nhập từ nhà cung cấp

#### Lệnh nhân viên

```
/stock import create --supplier="<tên NCC>" --date=<DATE> [--note="<ghi chú>"]
```

> Ví dụ: `/stock import create --supplier="Công ty ABC" --date=08/05/2026`

#### Quyền tối thiểu: `WAREHOUSE`

---

### 6.3 Thêm sản phẩm vào phiếu nhập

- **Vai trò:** EMPLOYEE
- **Mô tả:** Thêm từng dòng sản phẩm vào phiếu nhập đang tạo

#### Lệnh nhân viên

```
/stock import add <IMPORT_ID> <SKU> <QUANTITY> [--price=<PRICE>]
```

> Ví dụ: `/stock import add NK001 AO-HOODIE-L-DEN 100 --price=150000`

#### Quyền tối thiểu: `WAREHOUSE`

---

### 6.4 Xác nhận phiếu nhập

- **Vai trò:** EMPLOYEE
- **Mô tả:** Hoàn tất và xác nhận phiếu nhập hàng vào kho

#### Lệnh nhân viên

```
/stock import confirm <IMPORT_ID>
```

#### Quyền tối thiểu: `SUPERVISOR`

---

### 6.5 Tra cứu lịch sử nhập hàng

- **Vai trò:** EMPLOYEE
- **Mô tả:** Xem danh sách phiếu nhập theo bộ lọc

#### Lệnh nhân viên

```
/stock import list [--date=<DATE_RANGE>] [--supplier="<tên NCC>"] [--sku=<SKU>]
```

> Ví dụ: `/stock import list --date=01/05/2026~08/05/2026`

#### Quyền tối thiểu: `WAREHOUSE`

---

### 6.6 Cảnh báo hàng sắp hết

- **Vai trò:** EMPLOYEE
- **Mô tả:** Liệt kê sản phẩm có tồn kho dưới ngưỡng

#### Lệnh nhân viên

```
/stock low [--threshold=<QUANTITY>]
```

> Ví dụ: `/stock low --threshold=10`

#### Quyền tối thiểu: `WAREHOUSE`

---

## 7. Bảo hành (Warranty)

### 7.1 Kiểm tra thông tin bảo hành

- **Vai trò:** CẢ HAI
- **Mô tả:** Xem thông tin và thời hạn bảo hành của sản phẩm

#### Cú pháp khách hàng

```
~ bảo hành [<WARRANTY_CODE | ORDER_ID>] ~
~ [sản phẩm] còn bảo hành không ~
~ thời hạn bảo hành [đơn <ORDER_ID>] ~
~ kiểm tra bảo hành <WARRANTY_CODE> ~
```

> Ví dụ: `"bảo hành BH00456 còn không"`, `"kiểm tra bảo hành đơn 10234"`, `"sản phẩm còn bảo hành không"`

#### Lệnh nhân viên

```
/warranty status <WARRANTY_CODE>
/warranty check --order=<ORDER_ID>
```

> Ví dụ: `/warranty status BH00456`, `/warranty check --order=10234`

#### Quyền tối thiểu: `STAFF`

---

### 7.2 Tạo yêu cầu bảo hành

- **Vai trò:** CẢ HAI
- **Mô tả:** Mở phiếu bảo hành cho sản phẩm lỗi

#### Cú pháp khách hàng

```
~ [tôi muốn] bảo hành sản phẩm ~
~ sản phẩm [bị] hỏng [muốn] bảo hành ~
~ gửi [đi] bảo hành [đơn <ORDER_ID>] ~
```

> Ví dụ: `"tôi muốn bảo hành sản phẩm"`, `"điện thoại bị hỏng muốn bảo hành"`, `"gửi đi bảo hành đơn 10234"`

#### Lệnh nhân viên

```
/warranty create <ORDER_ID> --issue="<mô tả lỗi>" [--sku=<SKU>]
```

> Ví dụ: `/warranty create 10234 --issue="màn hình bị vỡ" --sku=DT-MODEL-X`

#### Quyền tối thiểu: `SUPERVISOR`

---

### 7.3 Cập nhật trạng thái bảo hành

- **Vai trò:** EMPLOYEE
- **Mô tả:** Cập nhật tiến độ xử lý phiếu bảo hành

#### Lệnh nhân viên

```
/warranty update <WARRANTY_CODE> --status=<STATUS> [--note="<ghi chú>"]
```

> Ví dụ: `/warranty update BH00456 --status=REPAIRING --note="đang chờ linh kiện"`

#### Status codes bảo hành

| Code | Ý nghĩa |
|------|---------|
| `RECEIVED` | Đã nhận sản phẩm |
| `DIAGNOSING` | Đang kiểm tra |
| `REPAIRING` | Đang sửa chữa |
| `REPAIRED` | Đã sửa xong |
| `RETURNED` | Đã trả khách |
| `REPLACED` | Đã đổi máy mới |
| `REJECTED` | Không thuộc phạm vi bảo hành |

#### Quyền tối thiểu: `SUPERVISOR`

---

### 7.4 Danh sách phiếu bảo hành

- **Vai trò:** EMPLOYEE
- **Mô tả:** Xem danh sách phiếu bảo hành theo bộ lọc

#### Lệnh nhân viên

```
/warranty list [--status=<STATUS>] [--date=<DATE_RANGE>] [--customer=<CUSTOMER_ID>]
```

#### Quyền tối thiểu: `STAFF`

---

## 8. Công nợ & Đối soát (Debt & Reconciliation)

### 8.1 Tra cứu công nợ

- **Vai trò:** EMPLOYEE
- **Mô tả:** Xem danh sách công nợ tồn đọng theo đối tác hoặc khoảng thời gian

#### Lệnh nhân viên

```
/debt list [--partner="<tên đối tác>"] [--date=<DATE_RANGE>] [--status=unpaid|paid|partial]
```

> Ví dụ: `/debt list --partner="GHN" --status=unpaid`

#### Quyền tối thiểu: `ACCOUNTANT`

---

### 8.2 Gạch nợ / Xác nhận tiền về

- **Vai trò:** EMPLOYEE
- **Mô tả:** Xác nhận đã nhận tiền COD hoặc thanh toán từ đối tác, ghi nhận gạch nợ

#### Lệnh nhân viên

```
/debt confirm <DEBT_ID> --amount=<PRICE> [--date=<DATE>] [--note="<ghi chú>"]
```

> Ví dụ: `/debt confirm CN001 --amount=5000000 --date=08/05/2026 --note="tiền COD GHN tuần 1/5"`

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `DEBT_ID` | string\|number | Có | Mã phiếu công nợ |
| `--amount` | PRICE | Có | Số tiền thực nhận (VND) |
| `--date` | DATE | Không | Ngày nhận tiền (mặc định: hôm nay) |
| `--note` | string | Không | Ghi chú đối soát |

#### Quyền tối thiểu: `ACCOUNTANT`

---

### 8.3 Đối soát theo đơn vị vận chuyển

- **Vai trò:** EMPLOYEE
- **Mô tả:** Đối soát tiền COD từng đơn vị vận chuyển theo kỳ

#### Lệnh nhân viên

```
/debt reconcile --carrier=<CARRIER_CODE> --date=<DATE_RANGE>
```

> Ví dụ: `/debt reconcile --carrier=GHN --date=01/05/2026~07/05/2026`

#### Quyền tối thiểu: `ACCOUNTANT`

---

### 8.4 Tạo phiếu công nợ

- **Vai trò:** EMPLOYEE
- **Mô tả:** Tạo phiếu ghi nhận khoản nợ mới với nhà cung cấp hoặc đối tác

#### Lệnh nhân viên

```
/debt create --partner="<tên đối tác>" --amount=<PRICE> --due=<DATE> [--note="<mô tả>"]
```

> Ví dụ: `/debt create --partner="Công ty ABC" --amount=10000000 --due=15/05/2026`

#### Quyền tối thiểu: `ACCOUNTANT`

---

## 9. Chăm sóc khách hàng (Customer Support)

### 9.1 Tra cứu thông tin khách hàng

- **Vai trò:** EMPLOYEE
- **Mô tả:** Xem hồ sơ và lịch sử giao dịch của khách hàng

#### Lệnh nhân viên

```
/customer info <CUSTOMER_ID | PHONE>
```

> Ví dụ: `/customer info 0912345678`, `/customer info KH001`

#### Quyền tối thiểu: `STAFF`

---

### 9.2 Ghi chú tài khoản khách

- **Vai trò:** EMPLOYEE
- **Mô tả:** Thêm ghi chú nội bộ vào hồ sơ khách hàng

#### Lệnh nhân viên

```
/customer note <CUSTOMER_ID> "<nội dung>"
```

> Ví dụ: `/customer note KH001 "khách VIP, ưu tiên xử lý, thích giao buổi sáng"`

#### Quyền tối thiểu: `STAFF`

---

### 9.3 Xem lịch sử đơn hàng khách

- **Vai trò:** EMPLOYEE
- **Mô tả:** Liệt kê toàn bộ đơn hàng của một khách

#### Lệnh nhân viên

```
/customer orders <CUSTOMER_ID | PHONE> [--date=<DATE_RANGE>] [--status=<STATUS_CODE>]
```

> Ví dụ: `/customer orders 0912345678 --status=DELIVERED`

#### Quyền tối thiểu: `STAFF`

---

### 9.4 Xử lý khiếu nại

- **Vai trò:** CẢ HAI
- **Mô tả:** Ghi nhận và theo dõi khiếu nại từ khách hàng

#### Cú pháp khách hàng

```
~ [tôi muốn] khiếu nại [về] <vấn đề> ~
~ phản ánh [về] [đơn hàng | sản phẩm | dịch vụ] ~
~ tôi không hài lòng [về] <vấn đề> ~
~ liên hệ [nhân viên | hỗ trợ] ~
```

> Ví dụ: `"tôi muốn khiếu nại về đơn hàng 10234"`, `"tôi không hài lòng về dịch vụ giao hàng"`, `"liên hệ nhân viên"`

#### Lệnh nhân viên

```
/customer complaint create <CUSTOMER_ID> "<mô tả>" [--order=<ORDER_ID>]
/customer complaint status <ID_KHIẾU_NẠI>
/customer complaint close <ID_KHIẾU_NẠI> --resolution="<kết quả xử lý>"
```

#### Quyền tối thiểu: `STAFF` (tạo, xem), `SUPERVISOR` (đóng)

---

### 9.5 Tìm kiếm khách hàng

- **Vai trò:** EMPLOYEE
- **Mô tả:** Tìm khách theo tên, SĐT, hoặc ID

#### Lệnh nhân viên

```
/customer search "<từ khóa>"
```

> Ví dụ: `/customer search "Nguyen Van A"`, `/customer search "0912345678"`

#### Quyền tối thiểu: `STAFF`

---

## 10. Báo cáo & Thống kê (Reports)

### 10.1 Báo cáo doanh thu

- **Vai trò:** EMPLOYEE
- **Mô tả:** Xem tổng doanh thu theo khoảng thời gian

#### Lệnh nhân viên

```
/report revenue [--date=<DATE_RANGE>] [--format=csv]
```

> Ví dụ: `/report revenue --date=01/05/2026~08/05/2026`, `/report revenue --date=08/05/2026~08/05/2026`

#### Quyền tối thiểu: `MANAGER`

---

### 10.2 Sản phẩm bán chạy

- **Vai trò:** EMPLOYEE
- **Mô tả:** Liệt kê top sản phẩm theo doanh số

#### Lệnh nhân viên

```
/report top-products [--date=<DATE_RANGE>] [--limit=<QUANTITY>]
```

> Ví dụ: `/report top-products --date=01/05/2026~08/05/2026 --limit=10`

#### Quyền tối thiểu: `MANAGER`

---

### 10.3 Đơn hàng tồn đọng

- **Vai trò:** EMPLOYEE
- **Mô tả:** Danh sách đơn quá hạn xử lý

#### Lệnh nhân viên

```
/report pending-orders [--days=<số ngày>]
```

> Ví dụ: `/report pending-orders --days=3`

#### Quyền tối thiểu: `SUPERVISOR`

---

### 10.4 Tổng quan nhanh hôm nay

- **Vai trò:** EMPLOYEE
- **Mô tả:** Snapshot tổng hợp: đơn mới, doanh thu, đơn tồn đọng, hàng sắp hết

#### Lệnh nhân viên

```
/report today
```

#### Quyền tối thiểu: `SUPERVISOR`

---

### 10.5 Báo cáo đổi trả

- **Vai trò:** EMPLOYEE
- **Mô tả:** Thống kê số lượng và tỷ lệ đổi trả theo kỳ

#### Lệnh nhân viên

```
/report returns [--date=<DATE_RANGE>] [--format=csv]
```

#### Quyền tối thiểu: `MANAGER`

---

## 11. Giỏ hàng & Thanh toán (Cart & Payment)

### 11.1 Thêm sản phẩm vào giỏ

- **Vai trò:** CUSTOMER
- **Mô tả:** Thêm một sản phẩm vào giỏ hàng hiện tại

#### Cú pháp khách hàng

```
~ thêm <tên sản phẩm | SKU> [vào giỏ] ~
~ cho [tôi] [mua | lấy] <tên sản phẩm> ~
~ order <tên sản phẩm | SKU> ~
```

> Ví dụ: `"thêm áo hoodie đen L vào giỏ"`, `"cho tôi mua SP001"`, `"order AO-HOODIE-L-DEN"`

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| SKU / tên SP | SKU \| string | Có | Sản phẩm cần thêm |
| QUANTITY | integer | Không | Số lượng (mặc định: 1) |

#### Lỗi & Edge case

- Sản phẩm hết hàng → thông báo và gợi ý sản phẩm tương tự
- Khách chưa đăng nhập → yêu cầu đăng nhập trước

---

### 11.2 Xem giỏ hàng

- **Vai trò:** CUSTOMER
- **Mô tả:** Xem danh sách sản phẩm trong giỏ và tổng tiền

#### Cú pháp khách hàng

```
~ [xem] giỏ hàng [của tôi] ~
~ giỏ của tôi ~
~ tôi đang có gì trong giỏ ~
```

> Ví dụ: `"xem giỏ hàng của tôi"`, `"giỏ của tôi"`, `"tôi đang có gì trong giỏ"`

---

### 11.3 Áp mã giảm giá

- **Vai trò:** CUSTOMER
- **Mô tả:** Áp dụng voucher hoặc mã khuyến mãi vào đơn hàng

#### Cú pháp khách hàng

```
~ [dùng | áp] mã <VOUCHER_CODE> ~
~ mã giảm giá <VOUCHER_CODE> ~
~ tôi có mã <VOUCHER_CODE> ~
~ voucher <VOUCHER_CODE> ~
```

> Ví dụ: `"áp mã SALE50"`, `"tôi có mã FREESHIP"`, `"dùng voucher SALE50"`

#### Tham số

| Tên | Entity | Bắt buộc | Mô tả |
|-----|--------|----------|-------|
| `VOUCHER_CODE` | string | Có | Mã khuyến mãi |

#### Lỗi & Edge case

- Mã không tồn tại hoặc hết hạn → thông báo lỗi cụ thể
- Mã không áp dụng cho giỏ hàng hiện tại → giải thích điều kiện

---

### 11.4 Kiểm tra phương thức thanh toán

- **Vai trò:** CUSTOMER
- **Mô tả:** Xem các phương thức thanh toán được hỗ trợ

#### Cú pháp khách hàng

```
~ thanh toán bằng gì [được] ~
~ [có] phương thức thanh toán [nào] ~
~ [có nhận | hỗ trợ] <tên PTTT> không ~
~ tôi muốn dùng <tên PTTT> ~
```

> Ví dụ: `"thanh toán bằng gì được"`, `"có nhận ví MoMo không"`, `"tôi muốn dùng ví điện tử"`

#### Phương thức thanh toán hỗ trợ

| Code | Tên |
|------|-----|
| `COD` | Thanh toán khi nhận hàng |
| `BANK_TRANSFER` | Chuyển khoản ngân hàng |
| `MOMO` | Ví MoMo |
| `ZALOPAY` | ZaloPay |
| `VNPAY` | VNPay |
| `CREDIT_CARD` | Thẻ tín dụng / ghi nợ |

---

### 11.5 Đặt hàng / Thanh toán

- **Vai trò:** CUSTOMER
- **Mô tả:** Xác nhận đặt hàng và chọn phương thức thanh toán

#### Cú pháp khách hàng

```
~ [tôi muốn] đặt hàng [ngay] ~
~ thanh toán [bằng <tên PTTT>] ~
~ mua [ngay] [bằng <tên PTTT>] ~
~ checkout ~
```

> Ví dụ: `"tôi muốn đặt hàng"`, `"thanh toán bằng MoMo"`, `"mua ngay"`, `"checkout"`

---

## Appendix A — Bảng lệnh nhân viên

Bảng tổng hợp nhanh — dùng để tra cứu mà không cần đọc toàn bộ spec.

### Đơn hàng

| Lệnh | Mô tả | Quyền |
|------|-------|-------|
| `/order status <ORDER_ID>` | Xem trạng thái đơn | STAFF |
| `/order list [--status] [--date] [--customer]` | Danh sách đơn | STAFF |
| `/order confirm <ORDER_ID>` | Xác nhận đơn | STAFF |
| `/order cancel <ORDER_ID> [--reason]` | Hủy đơn | SUPERVISOR |
| `/order update <ORDER_ID> --status [--note]` | Cập nhật trạng thái | SUPERVISOR |
| `/order note <ORDER_ID> "<nội dung>"` | Ghi chú đơn | STAFF |

### Giao hàng

| Lệnh | Mô tả | Quyền |
|------|-------|-------|
| `/ship status <ORDER_ID>` | Xem trạng thái vận đơn | STAFF |
| `/ship assign <ORDER_ID> <CARRIER>` | Gán đơn vị vận chuyển | STAFF |
| `/ship fail <ORDER_ID> --reason [--action]` | Ghi nhận giao thất bại | SUPERVISOR |
| `/ship address <ORDER_ID> "<địa chỉ>" [--phone]` | Đổi địa chỉ giao | STAFF |
| `/ship list [--date] [--carrier] [--status]` | Danh sách đơn giao | STAFF |

### Đổi trả & Hoàn tiền

| Lệnh | Mô tả | Quyền |
|------|-------|-------|
| `/returns create <ORDER_ID> --type=exchange\|return [--sku] [--reason]` | Tạo yêu cầu đổi/trả | SUPERVISOR |
| `/returns status <RETURN_ID>` | Xem trạng thái yêu cầu | STAFF |
| `/returns approve <RETURN_ID> [--note]` | Duyệt yêu cầu | SUPERVISOR |
| `/returns reject <RETURN_ID> --reason` | Từ chối yêu cầu | SUPERVISOR |

### Sản phẩm

| Lệnh | Mô tả | Quyền |
|------|-------|-------|
| `/product search "<từ khóa>" [--category] [--price-max]` | Tìm sản phẩm | STAFF |
| `/product info <SKU>` | Chi tiết sản phẩm | STAFF |
| `/product update <SKU> --price=<PRICE>` | Cập nhật giá | SUPERVISOR |
| `/product update <SKU> --status=active\|inactive` | Bật/tắt hiển thị | SUPERVISOR |
| `/product stock <SKU>` | Kiểm tra tồn kho | STAFF |

### Kho & Nhập hàng

| Lệnh | Mô tả | Quyền |
|------|-------|-------|
| `/stock update <SKU> <±QUANTITY>` | Điều chỉnh tồn kho | WAREHOUSE |
| `/stock import create --supplier --date [--note]` | Tạo phiếu nhập | WAREHOUSE |
| `/stock import add <IMPORT_ID> <SKU> <QUANTITY> [--price]` | Thêm dòng phiếu nhập | WAREHOUSE |
| `/stock import confirm <IMPORT_ID>` | Xác nhận phiếu nhập | SUPERVISOR |
| `/stock import list [--date] [--supplier] [--sku]` | Lịch sử nhập hàng | WAREHOUSE |
| `/stock low [--threshold]` | Hàng sắp hết | WAREHOUSE |

### Bảo hành

| Lệnh | Mô tả | Quyền |
|------|-------|-------|
| `/warranty status <WARRANTY_CODE>` | Xem thông tin bảo hành | STAFF |
| `/warranty check --order=<ORDER_ID>` | Kiểm tra BH theo đơn | STAFF |
| `/warranty create <ORDER_ID> --issue [--sku]` | Tạo phiếu bảo hành | SUPERVISOR |
| `/warranty update <WARRANTY_CODE> --status [--note]` | Cập nhật tiến độ BH | SUPERVISOR |
| `/warranty list [--status] [--date] [--customer]` | Danh sách phiếu BH | STAFF |

### Công nợ & Đối soát

| Lệnh | Mô tả | Quyền |
|------|-------|-------|
| `/debt list [--partner] [--date] [--status]` | Tra cứu công nợ | ACCOUNTANT |
| `/debt confirm <DEBT_ID> --amount [--date] [--note]` | Gạch nợ / xác nhận tiền về | ACCOUNTANT |
| `/debt reconcile --carrier --date` | Đối soát theo ĐVVC | ACCOUNTANT |
| `/debt create --partner --amount --due [--note]` | Tạo phiếu công nợ | ACCOUNTANT |

### Chăm sóc khách hàng

| Lệnh | Mô tả | Quyền |
|------|-------|-------|
| `/customer info <CUSTOMER_ID\|PHONE>` | Hồ sơ khách hàng | STAFF |
| `/customer note <CUSTOMER_ID> "<nội dung>"` | Ghi chú tài khoản | STAFF |
| `/customer orders <CUSTOMER_ID\|PHONE> [--date] [--status]` | Lịch sử đơn | STAFF |
| `/customer search "<từ khóa>"` | Tìm kiếm khách | STAFF |
| `/customer complaint create <CUSTOMER_ID> "<mô tả>" [--order]` | Tạo khiếu nại | STAFF |
| `/customer complaint status <ID>` | Xem trạng thái khiếu nại | STAFF |
| `/customer complaint close <ID> --resolution` | Đóng khiếu nại | SUPERVISOR |

### Báo cáo & Thống kê

| Lệnh | Mô tả | Quyền |
|------|-------|-------|
| `/report today` | Tổng quan hôm nay | SUPERVISOR |
| `/report revenue [--date] [--format]` | Doanh thu | MANAGER |
| `/report top-products [--date] [--limit]` | Sản phẩm bán chạy | MANAGER |
| `/report pending-orders [--days]` | Đơn tồn đọng | SUPERVISOR |
| `/report returns [--date] [--format]` | Thống kê đổi trả | MANAGER |

---

## Appendix B — Mẫu câu khách hàng

Bộ mẫu câu thực tế người dùng hay gõ — dùng để test NLU engine và onboarding khách.

### Tra cứu đơn hàng

```
"đơn 10234 đang ở đâu"
"kiểm tra đơn hàng của tôi"
"tôi đặt hàng hôm qua chưa thấy giao"
"don 10234 dang o dau"
"DH10234 nhu the nao roi"
```

### Giao hàng

```
"hàng tôi đến chưa"
"shipper ở đâu rồi"
"bao giờ giao đến"
"giao hàng đơn 10234 tới chưa"
"tôi muốn đổi địa chỉ giao"
```

### Đổi trả

```
"tôi muốn trả hàng"
"hàng bị lỗi đổi được không"
"sản phẩm không đúng mô tả muốn trả"
"hoàn tiền đơn 10234"
"đổi sang size L được không"
"đơn đổi trả của tôi đến đâu rồi"
```

### Tìm kiếm sản phẩm

```
"áo hoodie đen size L còn không"
"tìm quà tặng cho bạn trai dưới 500k"
"giá điện thoại model X bao nhiêu"
"có áo thun trắng size M không"
"gợi ý quà sinh nhật"
"con ao hoodie den L k"
```

### Bảo hành

```
"sản phẩm còn bảo hành không"
"tôi muốn bảo hành máy"
"kiểm tra bảo hành đơn 10234"
"gửi đi bảo hành được không"
"BH00456 đang xử lý đến đâu"
```

### Giỏ hàng & Thanh toán

```
"thêm SP001 vào giỏ"
"áp mã giảm giá SALE50"
"thanh toán bằng gì được"
"còn phương thức nào khác không"
"tôi muốn dùng ví điện tử"
```

### Khiếu nại & Hỗ trợ

```
"tôi muốn khiếu nại"
"phản ánh về dịch vụ giao hàng"
"tôi không hài lòng"
"liên hệ nhân viên"
"cần hỗ trợ gấp"
```
