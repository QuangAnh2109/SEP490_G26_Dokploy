$(document).ready(function () {
    $('#changePasswordForm').on('submit', function (e) {
        e.preventDefault();

        const oldPassword = $('#OldPassword').val();
        const newPassword = $('#NewPassword').val();
        const confirmPassword = $('#ConfirmPassword').val();
        const $msg = $('#formMessage');
        const $btn = $('#btnSubmit');

        if (!oldPassword || !newPassword || !confirmPassword) {
            $msg.text("Vui lòng điền đầy đủ thông tin.").removeClass('text-success').addClass('text-danger');
            return;
        }

        if (newPassword !== confirmPassword) {
            $msg.text("Mật khẩu xác nhận không khớp.").removeClass('text-success').addClass('text-danger');
            return;
        }

        var passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$/;
        if (!passwordPattern.test(newPassword)) {
            $msg.text("Mật khẩu phải từ 8-72 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.").removeClass('text-success').addClass('text-danger');
            return;
        }

        $btn.prop('disabled', true).text('Đang cập nhật...');
        $msg.text("");

        const requestData = {
            OldPassword: oldPassword,
            NewPassword: newPassword
        };

        apiClient.post("/api/auth/change-password", requestData)
            .then(function (response) {
                $msg.text("Đổi mật khẩu thành công!").removeClass('text-danger').addClass('text-success');
                $('#changePasswordForm')[0].reset();
                setTimeout(() => {
                    // Redirect or close after success
                    window.location.href = '/';
                }, 2000);
            })
            .catch(function (err) {
                $msg.text(err.responseJSON?.message || "Có lỗi xảy ra khi đổi mật khẩu.")
                    .removeClass('text-success').addClass('text-danger');
                $btn.prop('disabled', false).text('Cập nhật Mật khẩu');
            });
    });
});
