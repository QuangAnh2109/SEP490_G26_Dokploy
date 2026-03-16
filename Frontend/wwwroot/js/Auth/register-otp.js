$(document).ready(function () {
    // 1. Verify Role is Selected
    var roleId = localStorage.getItem('pendingRegistrationRole');
    if (!roleId) {
        window.location.href = '/Auth/SelectRole';
        return;
    }

    // Display human-readable role (bold role name)
    var roleName = roleId === "1" ? "Giáo viên" : "Học sinh";
    $('#roleDisplay').html(`Đăng ký tài khoản với vai trò: <strong>${roleName}</strong>`);

    // 2. Handle Manual Form Registration -> OTP
    $('#registerForm').submit(function (e) {
        e.preventDefault();

        var email = $('#Email').val();
        var password = $('#Password').val();
        var confirmPassword = $('#ConfirmPassword').val();

        // Email Validation (RFC 5322 approximation)
        var emailPattern = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$/;
        if (!emailPattern.test(email)) {
            showError("Vui lòng nhập Email hợp lệ.");
            return;
        }

        // Password Policy Validation: 8-72 chars, upper, lower, number, special
        var passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$/;
        if (!passwordPattern.test(password)) {
            showError("Mật khẩu phải từ 8-72 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
            return;
        }

        if (password !== confirmPassword) {
            showError("Mật khẩu nhập lại không khớp.");
            return;
        }

        // Show loading
        $('#loadingOverlay').css('display', 'flex');
        $('#formError').hide();

        var requestData = {
            Email: email,
            Password: password,
            RoleId: parseInt(roleId)
        };

        apiClient.post('/api/auth/send-otp', requestData)
            .then(function (response) {
                // OTP sent successfully!
                localStorage.setItem('pendingRegistrationEmail', email);
                window.location.href = '/Auth/VerifyOTP';
            })
            .catch(function (err) {
                $('#loadingOverlay').hide();
                showError(err.message || "Đã có lỗi xảy ra. Vui lòng thử lại sau.");
            });
    });
});

function showError(message) {
    $('#formError').text(message).show();
}

// 3. Handle Google Registration - click nút Google ẩn (script tự render từ g_id_onload)
function triggerGoogleRegister() {
    // Google render button vào #googleSignInWrapper - có thể là div hoặc iframe
    var wrapper = document.getElementById('googleSignInWrapper');
    var googleButton = wrapper && (wrapper.querySelector('[role=button]') || wrapper.querySelector('div') || wrapper.querySelector('iframe'));
    if (googleButton) {
        googleButton.click();
    } else {
        // Script có thể chưa load - chờ rồi thử lại
        setTimeout(function () {
            wrapper = document.getElementById('googleSignInWrapper');
            googleButton = wrapper && (wrapper.querySelector('[role=button]') || wrapper.querySelector('div') || wrapper.querySelector('iframe'));
            if (googleButton) {
                googleButton.click();
            } else {
                showError("Không thể tải dịch vụ Google. Vui lòng F5 trang và thử lại.");
            }
        }, 1500);
    }
}

// This function is called by the Google GIS script after user selects their account
function handleCredentialResponse(response) {
    var roleId = localStorage.getItem('pendingRegistrationRole');

    // Show a general loading state
    $('#loadingOverlay').find('p').text('Đang tạo tài khoản qua Google...');
    $('#loadingOverlay').css('display', 'flex');

    var requestData = {
        IdToken: response.credential,
        RoleId: parseInt(roleId)
    };

    apiClient.post('/api/auth/google-register', requestData)
        .then(function (data) {
            if (data.token) {
                localStorage.setItem('jwtToken', data.token);
                localStorage.removeItem('pendingRegistrationRole');
                window.location.href = '/';
            } else {
                $('#loadingOverlay').hide();
                showError("Đăng ký thành công nhưng thiếu thông tin đăng nhập.");
            }
        })
        .catch(function (err) {
            $('#loadingOverlay').hide();
            showError("Lỗi đăng ký Google: " + err.message);
        });
}
