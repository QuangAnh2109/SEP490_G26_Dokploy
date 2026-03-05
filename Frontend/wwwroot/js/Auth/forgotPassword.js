$(document).ready(function () {
    $('#forgotPasswordForm').on('submit', function (e) {
        e.preventDefault();

        const email = $('#Email').val().trim();
        const $msg = $('#formMessage');
        const $btn = $('#btnSubmit');

        if (!email) {
            $msg.text("Vui lòng nhập Email.").removeClass('text-success').addClass('text-danger');
            return;
        }

        $btn.prop('disabled', true).text('Đang gửi OTP...');
        $msg.text("");

        apiClient.post("/api/auth/forgot-password", { Email: email })
            .then(function (response) {
                // Success
                window.location.href = `/Auth/ResetPassword?email=${encodeURIComponent(email)}`;
            })
            .catch(function (err) {
                $msg.text(err.responseJSON?.message || "Có lỗi xảy ra khi gửi yêu cầu. Vui lòng thử lại.")
                    .removeClass('text-success').addClass('text-danger');
                $btn.prop('disabled', false).text('Gửi Mã OTP');
            });
    });
});
