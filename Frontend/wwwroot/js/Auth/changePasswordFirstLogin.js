// Trang đổi password lần đầu — KHÔNG await window.userReady (bootstrap /me sẽ trả 403 và redirect lại trang này).
// Form submit → BE đổi password + revoke all + reissue cookie với mcp=false → redirect '/'.

const ERROR_MESSAGES = {
    AUTH_CURRENT_PASSWORD_WRONG: 'Mật khẩu hiện tại không đúng.',
    AUTH_NEW_PASSWORD_SAME_AS_OLD: 'Mật khẩu mới không được trùng với mật khẩu cũ.',
    AUTH_USER_NOT_FOUND: 'Tài khoản không tồn tại hoặc đã bị xóa.',
    VALIDATION: 'Mật khẩu mới phải có 8-72 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.'
};

$(document).ready(function () {
    const $form = $('#changePasswordFirstLoginForm');
    const $msg = $('#formMessage');
    const $btn = $('#btnSubmit');

    function showError(text) {
        $msg.removeClass('text-success').addClass('text-danger').text(text);
    }

    $form.on('submit', function (e) {
        e.preventDefault();
        $msg.text('');

        const currentPassword = $('#CurrentPassword').val();
        const newPassword = $('#NewPassword').val();
        const confirmPassword = $('#ConfirmPassword').val();

        if (!currentPassword) {
            showError('Vui lòng nhập mật khẩu hiện tại.');
            return;
        }

        const passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$/;
        if (!passwordPattern.test(newPassword)) {
            showError(ERROR_MESSAGES.VALIDATION);
            return;
        }
        if (newPassword !== confirmPassword) {
            showError('Xác nhận mật khẩu không trùng khớp.');
            return;
        }

        $btn.prop('disabled', true);
        $.ajax({
            url: API_BASE_URL.replace(/\/+$/, '') + '/api/auth/change-password-first-login',
            type: 'POST',
            contentType: 'application/json',
            xhrFields: { withCredentials: true },
            data: JSON.stringify({ currentPassword: currentPassword, newPassword: newPassword }),
            success: function () {
                window.location.href = '/';
            },
            error: function (xhr) {
                $btn.prop('disabled', false);
                const code = xhr.responseJSON?.code;
                showError(ERROR_MESSAGES[code] || 'Đã có lỗi xảy ra. Vui lòng thử lại.');
            }
        });
    });
});
