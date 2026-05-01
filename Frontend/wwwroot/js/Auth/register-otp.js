$(document).ready(function () {
    // 1. Verify Role is Selected
    var roleId = localStorage.getItem('pendingRegistrationRole');
    if (!roleId) {
        window.location.href = '/Auth/SelectRole';
        return;
    }

    // Display human-readable role
    var roleName = roleId === RoleIds.Teacher ? "Giáo viên" : "Học sinh";
    $('#roleDisplay').text(`Đăng ký tài khoản với vai trò: ${roleName}`);

    // Show/Hide StudentId field based on role
    var isStudent = roleId === RoleIds.Student;
    if (isStudent) {
        $('#studentIdGroup').removeClass('d-none');
        $('#StudentId').attr('required', true);
    } else {
        $('#studentIdGroup').addClass('d-none');
        $('#StudentId').removeAttr('required');
    }

    // 2. Handle Manual Form Registration -> OTP
    $('#registerForm').submit(function (e) {
        e.preventDefault();

        var fullName = ($('#FullName').val() || '').toString().trim();
        var phoneNumber = ($('#PhoneNumber').val() || '').toString().trim();
        var studentId = ($('#StudentId').val() || '').toString().trim();
        var email = $('#Email').val();
        var password = $('#Password').val();
        var confirmPassword = $('#ConfirmPassword').val();

        if (!fullName) {
            showError("Vui lòng nhập họ và tên.");
            return;
        }
        var fullNamePattern = /^[\p{L}\p{M}]+(?:\s+[\p{L}\p{M}]+)*$/u;
        if (!fullNamePattern.test(fullName)) {
            showError("Họ và tên chỉ được chứa chữ cái và khoảng trắng.");
            return;
        }

        if (phoneNumber) {
            var phonePattern = /^0\d{9}$/;
            if (!phonePattern.test(phoneNumber)) {
                showError("Số điện thoại phải gồm 10 số và bắt đầu bằng 0.");
                return;
            }
        }

        if (isStudent && !studentId) {
            showError("Vui lòng nhập mã sinh viên.");
            return;
        }
        if (isStudent) {
            var studentIdPattern = /^[A-Za-z]{2}\d{6}$/;
            if (!studentIdPattern.test(studentId)) {
                showError("Mã sinh viên phải gồm 8 ký tự: 2 chữ cái đầu và 6 chữ số sau (ví dụ: SE123456).");
                return;
            }
        }

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
            FullName: fullName,
            PhoneNumber: phoneNumber || null,
            StudentId: studentId || null,
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
    $('#formError').hide();
    $('#loadingOverlay').find('p').text('Đang xác thực với Google...');
    $('#loadingOverlay').css('display', 'flex');

    var requestData = {
        IdToken: response.credential
    };

    apiClient.post('/api/auth/google-login', requestData)
        .then(function (data) {
            localStorage.setItem('tempGoogleToken', requestData.IdToken);

            if (data.email) {
                localStorage.setItem('tempGoogleEmail', data.email);
            }

            if (data.needsRegistration) {
                localStorage.removeItem('tempGoogleNeedsCompletion');
                window.location.href = '/Auth/GoogleRegister';
                return;
            }

            if (data.needsProfileCompletion) {
                localStorage.setItem('tempGoogleNeedsCompletion', '1');
                window.location.href = '/Auth/GoogleRegister';
                return;
            }

            localStorage.removeItem('tempGoogleToken');
            localStorage.removeItem('tempGoogleEmail');
            localStorage.removeItem('tempGoogleNeedsCompletion');
            window.location.href = '/';
        })
        .catch(function (err) {
            $('#loadingOverlay').hide();
            localStorage.removeItem('tempGoogleToken');
            localStorage.removeItem('tempGoogleEmail');
            localStorage.removeItem('tempGoogleNeedsCompletion');
            showError("Lỗi đăng nhập Google: " + err.message);
        });
}
