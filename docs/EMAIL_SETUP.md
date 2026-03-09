# Cấu hình Email (Quên mật khẩu / OTP)

Tính năng **Quên mật khẩu** gửi mã OTP qua email. Để gửi email thật, cần cấu hình SMTP trong `Backend/appsettings.json` hoặc `Backend/appsettings.Development.json`.

## Cấu hình Gmail

1. Bật **Xác minh 2 bước** cho tài khoản Gmail
2. Tạo **Mật khẩu ứng dụng**: [Google Account → Security → App passwords](https://myaccount.google.com/apppasswords)
3. Cập nhật `Backend/appsettings.Development.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.fpt.edu.vn",
    "Port": 587,
    "SenderEmail": "ducdmhe172047@fpt.edu.vn",
    "SenderPassword": "osmk aznv pxwx abpw"
  }
}
```

## Chế độ Development (chưa cấu hình email)

Khi **chưa** cấu hình `SenderEmail` và `SenderPassword`, Backend sẽ **in mã OTP ra Console** thay vì gửi email. Kiểm tra cửa sổ terminal/console của Backend để lấy mã OTP khi test.
