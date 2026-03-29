# 🔐 Hướng dẫn chuyển dữ liệu bảo mật sang file .env

> **Ngày tạo:** 2026-03-29  
> **Trạng thái:** Cần thực hiện ngay  
> **Mức độ nghiêm trọng:** 🔴 Cao — Dữ liệu bảo mật đang bị hardcode trong source code

---

## 1. Kết quả quét bảo mật

### 📁 File `appsettings.json` — ⚠️ PHÁT HIỆN 5 VẤN ĐỀ NGHIÊM TRỌNG

| # | Loại dữ liệu | Giá trị hiện tại | Mức độ |
|---|--------------|-------------------|--------|
| 1 | **Database Connection String** | `Server=172.19.0.5;...Password=4oZQ123456@;...` | 🔴 Nghiêm trọng |
| 2 | **JWT Secret Key** | `ThisIsASecretKeyForJwtAuthentication...` | 🔴 Nghiêm trọng |
| 3 | **Email Sender Password** | `kvhu uaei rula htar` (Gmail App Password) | 🔴 Nghiêm trọng |
| 4 | **Email Sender Address** | `doduccoloa1@gmail.com` | 🟡 Trung bình |
| 5 | **Google Client ID** | `570918216833-...apps.googleusercontent.com` | 🟡 Trung bình |

### 📁 File `appsettings.Development.json` — ⚠️ 1 VẤN ĐỀ

| # | Loại dữ liệu | Giá trị | Mức độ |
|---|--------------|---------|--------|
| 1 | **Google Client ID** | `570918216833-...apps.googleusercontent.com` | 🟡 Trung bình |

### 📁 Các file Constants — ✅ AN TOÀN

Tất cả 9 file trong thư mục `Constants/` chỉ chứa enum values, labels, và messages. **Không có dữ liệu bảo mật.**

- `UserRoles.cs` — Teacher/Student roles
- `Messages.cs` — Error/Success/Validation messages  
- `DifficultyLevel.cs` — Recognition/Comprehension/Application/HighApplication
- `ExamBlueprintStatus.cs` — NotStarted/Approved/InUse/Archived
- `ExamStatus.cs` — Ready/Published/InProgress/Deleted/Cancelled/Closed
- `MemberStatus.cs` — Pending/Active/Invited
- `QuestionStatus.cs` — Draft/Active/Archive/Inprogress
- `QuestionType.cs` — FillInBlank/MultipleChoice
- `SubmissionStatus.cs` — InProgress/Submitted/Absent

---

## 2. Các bước thực hiện

### Bước 1: Cài package DotNetEnv

> ✅ **Đã hoàn thành** — Package `DotNetEnv 3.1.1` đã được cài vào `Backend.csproj`.

Nếu cần cài lại:
```bash
cd Backend
dotnet add package DotNetEnv --version 3.1.1
```

### Bước 2: Tạo file `Backend/.env`

Tạo file `Backend/.env` với nội dung sau (thay giá trị thật của bạn vào):

```env
# ===========================================
# DATABASE
# ===========================================
DB_CONNECTION_STRING=Server=172.19.0.5;Database=MTCA_SEP490_G26;User Id=sa;Password=4oZQ123456@;TrustServerCertificate=True;

# ===========================================
# JWT AUTHENTICATION
# ===========================================
JWT_KEY=ThisIsASecretKeyForJwtAuthenticationWhichMustBeAtLeast256Bits
JWT_ISSUER=LibraryManagementAPI
JWT_AUDIENCE=LibraryManagementClient

# ===========================================
# EMAIL SETTINGS (Gmail App Password)
# ===========================================
EMAIL_SMTP_SERVER=smtp.gmail.com
EMAIL_PORT=587
EMAIL_SENDER=doduccoloa1@gmail.com
EMAIL_PASSWORD=kvhu uaei rula htar

# ===========================================
# GOOGLE OAUTH
# ===========================================
GOOGLE_CLIENT_ID=570918216833-7fo1hhn5vgfpeb07qdv5818epue3f7hl.apps.googleusercontent.com

# ===========================================
# FRONTEND
# ===========================================
FRONTEND_BASE_URL=https://localhost:7109
```

### Bước 3: Cập nhật file `Backend/.env.example`

File này là template cho team, **an toàn để commit lên git**:

```env
# ===========================================
# DATABASE
# ===========================================
DB_CONNECTION_STRING=Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;

# ===========================================
# JWT AUTHENTICATION
# ===========================================
JWT_KEY=YOUR_JWT_SECRET_KEY_MIN_256_BITS
JWT_ISSUER=LibraryManagementAPI
JWT_AUDIENCE=LibraryManagementClient

# ===========================================
# EMAIL SETTINGS (Gmail App Password)
# ===========================================
EMAIL_SMTP_SERVER=smtp.gmail.com
EMAIL_PORT=587
EMAIL_SENDER=your_email@gmail.com
EMAIL_PASSWORD=your_app_password

# ===========================================
# GOOGLE OAUTH
# ===========================================
GOOGLE_CLIENT_ID=your_google_client_id.apps.googleusercontent.com

# ===========================================
# FRONTEND
# ===========================================
FRONTEND_BASE_URL=https://localhost:7109
```

### Bước 4: Cập nhật `Backend/appsettings.json`

Thay toàn bộ nội dung bằng phiên bản **không chứa secret**, sử dụng placeholder đọc từ environment variables:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "MyCnn": ""
  },
  "Jwt": {
    "Key": "",
    "Issuer": "",
    "Audience": ""
  },
  "EmailSettings": {
    "SmtpServer": "",
    "Port": 587,
    "SenderEmail": "",
    "SenderPassword": ""
  },
  "Google": {
    "ClientId": ""
  },
  "FrontendSettings": {
    "BaseUrl": ""
  }
}
```

### Bước 5: Cập nhật `Backend/appsettings.Development.json`

Xóa Google ClientId, chỉ giữ lại Logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Bước 6: Cập nhật `Backend/Program.cs`

Thêm đoạn code sau **ngay sau** dòng `var builder = WebApplication.CreateBuilder(args);` (dòng 17):

```csharp
var builder = WebApplication.CreateBuilder(args);

// ============================
// LOAD .env FILE
// ============================
DotNetEnv.Env.Load();

// Map environment variables sang Configuration
builder.Configuration["ConnectionStrings:MyCnn"] = 
    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "";
builder.Configuration["Jwt:Key"] = 
    Environment.GetEnvironmentVariable("JWT_KEY") ?? "";
builder.Configuration["Jwt:Issuer"] = 
    Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "";
builder.Configuration["Jwt:Audience"] = 
    Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "";
builder.Configuration["EmailSettings:SmtpServer"] = 
    Environment.GetEnvironmentVariable("EMAIL_SMTP_SERVER") ?? "";
builder.Configuration["EmailSettings:Port"] = 
    Environment.GetEnvironmentVariable("EMAIL_PORT") ?? "587";
builder.Configuration["EmailSettings:SenderEmail"] = 
    Environment.GetEnvironmentVariable("EMAIL_SENDER") ?? "";
builder.Configuration["EmailSettings:SenderPassword"] = 
    Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? "";
builder.Configuration["Google:ClientId"] = 
    Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "";
builder.Configuration["FrontendSettings:BaseUrl"] = 
    Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "";
```

> **Lưu ý:** Không cần thêm `using` vì đã dùng full qualified name `DotNetEnv.Env.Load()`.

### Bước 7: Cập nhật `.gitignore`

Thêm vào cuối file `.gitignore` ở thư mục gốc project:

```gitignore
# Environment files
.env
!.env.example
```

---

## 3. Kiểm tra sau khi hoàn thành

### Checklist xác nhận:

- [ ] File `.env` đã được tạo trong `Backend/` với giá trị thật
- [ ] File `.env.example` đã được cập nhật với placeholder
- [ ] File `appsettings.json` đã xóa hết secret values (chỉ còn chuỗi rỗng)
- [ ] File `appsettings.Development.json` đã xóa Google ClientId
- [ ] File `Program.cs` đã thêm code load `.env`
- [ ] File `.gitignore` đã thêm rule ignore `.env`
- [ ] Chạy `dotnet build` thành công
- [ ] Chạy `dotnet run` và test các chức năng (Login, JWT, Email, Google OAuth)

### Test nhanh:

```bash
cd Backend
dotnet build
dotnet run
```

Nếu app khởi động và kết nối DB thành công → migration hoàn thành ✅

---

## 4. Lưu ý quan trọng

### ⚠️ Đổi tất cả mật khẩu/secret key

Sau khi chuyển sang `.env`, hãy **đổi tất cả credentials** vì chúng đã từng bị expose trong git history:

1. **Đổi password database** `sa` trên SQL Server
2. **Generate JWT Key mới** (ít nhất 256 bits / 32 characters)
3. **Revoke và tạo Gmail App Password mới** tại: https://myaccount.google.com/apppasswords
4. **Kiểm tra Google Client ID** tại Google Cloud Console

### 🔒 Bảo vệ file `.env`

- File `.env` chứa **TẤT CẢ bí mật** của project
- **KHÔNG BAO GIỜ** commit file này lên git
- Chỉ chia sẻ qua kênh an toàn (tin nhắn riêng, password manager)

### 👥 Hướng dẫn cho team

- Mỗi thành viên tự tạo file `.env` riêng dựa trên `.env.example`
- Trên server production, có thể set environment variables trực tiếp thay vì dùng file `.env`

---

## 5. Tóm tắt thay đổi

| File | Hành động |
|------|-----------|
| `Backend.csproj` | ✅ Đã thêm package `DotNetEnv 3.1.1` |
| `Backend/.env` | 🔨 Cần tạo mới — chứa giá trị thật |
| `Backend/.env.example` | 🔨 Cần cập nhật — chứa placeholder |
| `Backend/appsettings.json` | 🔨 Cần xóa tất cả secret values |
| `Backend/appsettings.Development.json` | 🔨 Cần xóa Google ClientId |
| `Backend/Program.cs` | 🔨 Cần thêm code load `.env` |
| `.gitignore` | 🔨 Cần thêm rule ignore `.env` |
