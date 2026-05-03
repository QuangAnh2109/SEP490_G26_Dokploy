// Trang đổi password lần đầu — KHÔNG await window.userReady (bootstrap /me sẽ trả 403 và redirect lại trang này).
// Form submit → BE đổi password + revoke all + clear cookies → hiện alert + countdown trên nút "Đăng nhập lại"; click chủ động hoặc hết 3s tự redirect '/Auth/Login'.

$(document).ready(function () {
    const ERROR_MESSAGES = {
        AUTH_CURRENT_PASSWORD_WRONG: 'Mật khẩu hiện tại không đúng.',
        AUTH_NEW_PASSWORD_SAME_AS_OLD: 'Mật khẩu mới không được trùng với mật khẩu cũ.',
        AUTH_USER_NOT_FOUND: 'Tài khoản không tồn tại hoặc đã bị xóa.',
        VALIDATION: 'Mật khẩu mới phải có 8-72 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.'
    };
    const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$/;

    const $form = $('#changePasswordFirstLoginForm');
    const $msg = $('#formMessage');
    const $btn = $('#btnSubmit');

    function showError(text) {
        $msg.removeClass('text-success').addClass('text-danger').text(text);
    }

    // Password show/hide is handled by Shared/passwordToggle.js (delegated).

    $('#NewPassword').on('blur', function () {
        const val = $(this).val();
        $('#NewPasswordError').text(val && !PASSWORD_PATTERN.test(val) ? ERROR_MESSAGES.VALIDATION : '');
    });

    $form.on('submit', function (e) {
        e.preventDefault();
        $msg.text('');
        $('#NewPasswordError').text('');

        const currentPassword = $('#CurrentPassword').val();
        const newPassword = $('#NewPassword').val();
        const confirmPassword = $('#ConfirmPassword').val();

        if (!currentPassword) {
            showError('Vui lòng nhập mật khẩu hiện tại.');
            return;
        }

        if (!PASSWORD_PATTERN.test(newPassword)) {
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
                $msg.removeClass('text-danger text-success').html(
                    '<div class="alert alert-success d-flex align-items-center gap-2 mb-0" role="alert">' +
                        '<i class="fa-solid fa-circle-check fa-lg"></i>' +
                        '<div class="fw-bold">Đổi mật khẩu thành công. Vui lòng đăng nhập lại bằng mật khẩu mới.</div>' +
                    '</div>'
                );
                $form.find('input').prop('disabled', true);

                let timer = null;
                const goToLogin = function () {
                    if (timer) clearInterval(timer);
                    window.location.href = '/Auth/Login';
                };

                let secs = 3;
                $btn.prop('disabled', false)
                    .attr('type', 'button')
                    .text('Đăng nhập lại (' + secs + 's)')
                    .off('click').on('click', goToLogin);

                timer = setInterval(function () {
                    secs--;
                    if (secs <= 0) {
                        goToLogin();
                    } else {
                        $btn.text('Đăng nhập lại (' + secs + 's)');
                    }
                }, 1000);
            },
            error: function (xhr) {
                $btn.prop('disabled', false);
                const code = xhr.responseJSON?.code;
                showError(ERROR_MESSAGES[code] || 'Đã có lỗi xảy ra. Vui lòng thử lại.');
            }
        });
    });
});
