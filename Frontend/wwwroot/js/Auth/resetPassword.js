$(document).ready(function () {
    $('#resetPasswordForm').on('submit', function (e) {
        e.preventDefault();

        const email = $('#Email').val().trim();
        const otpCode = $('#OtpCode').val().trim();
        const newPassword = $('#NewPassword').val();
        const $msg = $('#formMessage');
        const $btn = $('#btnSubmit');

        if (!otpCode || !newPassword) {
            $msg.text("Vui lòng điền đầy đủ thông tin.").removeClass('text-success').addClass('text-danger');
            return;
        }

        if (newPassword.length < 6) {
            $msg.text("Mật khẩu mới phải có ít nhất 6 ký tự.").removeClass('text-success').addClass('text-danger');
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
                showToast("Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập với mật khẩu mới.");
                setTimeout(() => {
                    window.location.href = '/Auth/Login';
                }, 1500);
            })
            .catch(function (err) {
                $msg.text(err.responseJSON?.message || "Có lỗi xảy ra khi đặt lại mật khẩu.")
                    .removeClass('text-success').addClass('text-danger');
                $btn.prop('disabled', false).text('Xác nhận Đặt lại');
            });
    });
});
