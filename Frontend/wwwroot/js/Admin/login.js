$(function () {
    // Password show/hide is handled by Shared/passwordToggle.js (delegated).

    $('#loginForm').on('submit', function (e) {
        e.preventDefault();
        const email = $('#email').val().trim();
        const password = $('#password').val();
        const $btn = $('#btnSubmit');
        const $err = $('#loginError');

        $err.text('');
        $btn.prop('disabled', true).text('Đang xử lý...');

        apiClient.post('/api/auth/login', { email, password })
            .then(function () {
                window.location.href = '/Admin/Users';
            })
            .catch(function (err) {
                $btn.prop('disabled', false).text('Đăng nhập');
                if (err.xhr && err.xhr.status === 403 && err.xhr.responseJSON?.code === 'AUTH_ACCOUNT_LOCKED') {
                    $err.text('Tài khoản bị khoá.');
                } else if (err.xhr && err.xhr.status === 401) {
                    $err.text('Email hoặc mật khẩu không đúng.');
                } else {
                    $err.text(err.message || 'Lỗi đăng nhập.');
                }
            });
    });
});
