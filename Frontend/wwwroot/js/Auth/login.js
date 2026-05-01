$(document).ready(function () {
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
            Password: password
        };

        apiClient.post("/api/auth/login", requestData)
            .then(function (response) {
                const returnUrl = $('#returnUrl').val();
                window.location.href = returnUrl ? returnUrl : '/';
            })
            .catch(function (err) {
                $('#formError').text("Email hoặc mật khẩu không chính xác.");
            });
    });
});

// 2. Handle Google Login callback
function handleCredentialResponse(response) {
    const requestData = {
        IdToken: response.credential
    };

    apiClient.post("/api/auth/google-login", requestData)
        .then(function () {
            const returnUrl = $('#returnUrl').val();
            window.location.href = returnUrl ? returnUrl : '/';
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