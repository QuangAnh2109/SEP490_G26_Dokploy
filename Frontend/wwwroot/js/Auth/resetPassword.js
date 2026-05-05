$(document).ready(function () {
    $('#resetPasswordForm').on('submit', function (e) {
        e.preventDefault();

        const email = $('#Email').val().trim();
        const otpCode = $('#OtpCode').val().trim();
        const newPassword = $('#NewPassword').val();
        const confirmPassword = $('#ConfirmPassword').val();
        const $msg = $('#formMessage');
        const $btn = $('#btnSubmit');

        if (!otpCode || !newPassword || !confirmPassword) {
            $msg.text("Vui lòng điền đầy đủ thông tin.").removeClass('text-success').addClass('text-danger');
            return;
        }
        if (newPassword !== confirmPassword) {
            $msg.text("Mật khẩu nhập lại không khớp.").removeClass('text-success').addClass('text-danger');
            return;
        }
        if (!/^\d{6}$/.test(otpCode)) {
            $msg.text("Mã OTP phải là 6 chữ số.").removeClass('text-success').addClass('text-danger');
            return;
        }

        var passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$/;
        if (!passwordPattern.test(newPassword)) {
            $msg.text("Mật khẩu phải từ 8-72 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.").removeClass('text-success').addClass('text-danger');
            return;
        }

        $btn.prop('disabled', true).text('Đang xử lý...');
        $msg.text("");

        const requestData = {
            Email: email,
            OtpCode: otpCode,
            NewPassword: newPassword
        };

        apiClient.post("/api/auth/reset-password", requestData)
            .then(function (response) {
                $msg.text("Mật khẩu đã được đặt lại thành công. Đang chuyển đến trang đăng nhập...")
                    .removeClass('text-danger').addClass('text-success');
                setTimeout(() => {
                    window.location.href = '/Auth/Login';
                }, 1500);
            })
            .catch(function (err) {
                $msg.text(err.message || err.responseJSON?.message || "Có lỗi xảy ra khi đặt lại mật khẩu.")
                    .removeClass('text-success').addClass('text-danger');
                $btn.prop('disabled', false).text('Xác nhận Đặt lại');
            });
    });
});
