// Chỉ chấp nhận same-origin path tương đối, tránh open redirect (`?returnUrl=https://evil.com`).
function safeReturnUrl(raw) {
    if (!raw) return null;
    return (raw.startsWith('/') && !raw.startsWith('//')) ? raw : null;
}

$(document).ready(function () {
    // Password show/hide is handled by Shared/passwordToggle.js (delegated).

    // 1. Handle traditional login form submission
    $('#loginForm').on('submit', function (e) {
        e.preventDefault(); // Prevent standard POST

        $('.text-danger').text(''); // Clear previous errors

        const email = $('#Email').val().trim();
        const password = $('#Password').val();

        if (!email) {
            $('#EmailError').text("Vui lòng nhập Email");
            return;
        }
        var emailPattern = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$/;
        if (!emailPattern.test(email)) {
            $('#EmailError').text("Vui lòng nhập địa chỉ Email hợp lệ.");
            return;
        }
        if (!password) {
            $('#PasswordError').text("Vui lòng nhập Mật khẩu");
            return;
        }

        const requestData = {
            Email: email,
            Password: password,
            RememberMe: $('#rememberMe').is(':checked')
        };

        apiClient.post("/api/auth/login", requestData)
            .then(function () {
                const safe = safeReturnUrl($('#returnUrl').val());
                window.location.href = safe || '/Class/ClassList';
            })
            .catch(function (err) {
                if (err.xhr && err.xhr.status === 403 && err.xhr.responseJSON?.code === 'AUTH_ACCOUNT_LOCKED') {
                    $('#formError').text('Tài khoản đã bị khoá. Vui lòng liên hệ quản trị viên.');
                } else {
                    $('#formError').text("Email hoặc mật khẩu không chính xác.");
                }
            });
    });
});

// 2. Handle Google Login callback
function handleCredentialResponse(response) {
    const requestData = {
        IdToken: response.credential,
        RememberMe: $('#rememberMe').is(':checked')
    };

    apiClient.post("/api/auth/google-login", requestData)
        .then(function () {
            const safe = safeReturnUrl($('#returnUrl').val());
            window.location.href = safe || '/Class/ClassList';
        })
        .catch(function (err) {
            const code = err && err.xhr && err.xhr.responseJSON ? err.xhr.responseJSON.code : null;
            if (code === 'AUTH_INVALID_CREDENTIALS') {
                showToast('Tài khoản Google chưa được đăng ký. Vui lòng liên hệ quản trị viên.', 'error');
            } else if (code === 'AUTH_ACCOUNT_LOCKED') {
                showToast('Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.', 'error');
            } else {
                showToast('Có lỗi xảy ra khi xác thực với Google.', 'error');
            }
        });
}

function triggerGoogleLogin() {
    var gSignInWrap = document.querySelector('.g_id_signin div[role=button]');
    if (gSignInWrap) {
        gSignInWrap.click();
    } else {
        console.error("Google Sign In button not rendered yet.");
    }
}