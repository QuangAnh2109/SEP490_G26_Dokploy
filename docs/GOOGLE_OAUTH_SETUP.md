# Cấu hình Google OAuth (Đăng nhập bằng Google)

## Lỗi "401: invalid_client" / "The OAuth client was not found"

Lỗi này xảy ra khi **Google Client ID** chưa được cấu hình đúng.

## Các bước cấu hình

### 1. Tạo OAuth Client trong Google Cloud Console

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Chọn project (hoặc tạo project mới)
3. Vào **APIs & Services** → **Credentials**
4. Bấm **Create Credentials** → **OAuth client ID**
5. Nếu chưa có, cấu hình **OAuth consent screen** trước
6. Chọn **Application type**: **Web application**
7. Đặt tên cho client (ví dụ: "Math Test Creator - Local")
8. **Authorized JavaScript origins** – thêm:
   - `https://localhost:7109`
   - `http://localhost:5291`
9. Bấm **Create** và sao chép **Client ID** (dạng `xxxxx.apps.googleusercontent.com`)

### 2. Cập nhật cấu hình trong project

**Frontend** – sửa `Frontend/appsettings.Development.json` (hoặc `appsettings.json`):

```json
{
  "Google": {
    "ClientId": "YOUR_ACTUAL_CLIENT_ID.apps.googleusercontent.com"
  }
}
```

**Backend** – sửa `Backend/appsettings.Development.json` (hoặc `appsettings.json`):

```json
{
  "Google": {
    "ClientId": "YOUR_ACTUAL_CLIENT_ID.apps.googleusercontent.com"
  }
}
```

> **Lưu ý:** Thay `YOUR_ACTUAL_CLIENT_ID.apps.googleusercontent.com` bằng Client ID thật từ Google Cloud Console.

### 3. Khởi động lại ứng dụng

Sau khi cập nhật cấu hình, khởi động lại Frontend và Backend rồi thử đăng nhập lại.
