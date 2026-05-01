$(document).ready(function () {
    // Populate readonly email field from LocalStorage (saved during Google Login)
    const tempEmail = localStorage.getItem('tempGoogleEmail');
    const pendingRoleId = (localStorage.getItem('pendingRegistrationRole') || '').toString();
    if (tempEmail) {
        $('#Email').val(tempEmail);
    } else {
        // If there's no temp email, something is wrong (direct access). Redirect to login.
        window.location.href = '/Auth/Login';
    }

    // Show/Hide StudentId based on selected role
    $('#RoleId').on('change', function () {
        const roleId = ($('#RoleId').val() || '').toString();
        const isStudent = roleId === '2';
        if (isStudent) {
            $('#studentIdGroup').removeClass('d-none');
            $('#StudentId').attr('required', true);
        } else {
            $('#studentIdGroup').addClass('d-none');
            $('#StudentId').removeAttr('required');
            $('#StudentId').val('');
        }
    });

    if (pendingRoleId) {
        $('#RoleId').val(pendingRoleId);
    }
    $('#RoleId').trigger('change');

    // Handle form submission
    $('#registerForm').on('submit', function (e) {
        e.preventDefault();

        $('#formError').text('');

        const idToken = localStorage.getItem('tempGoogleToken');
        const roleId = ($('#RoleId').val() || '').toString();
        const fullName = ($('#FullName').val() || '').toString().trim();
        const phoneNumber = ($('#PhoneNumber').val() || '').toString().trim();
        const studentId = ($('#StudentId').val() || '').toString().trim();

        if (!roleId) {
            showToast("Vui lòng chọn vai trò của bạn", "error");
            return;
        }

        if (!idToken) {
            showToast("Phiên đăng nhập không hợp lệ, vui lòng đăng nhập lại Google.", "error");
            setTimeout(() => {
                window.location.href = '/Auth/Login';
            }, 1000);
            return;
        }

        if (!fullName) {
            $('#formError').text("Vui lòng nhập họ và tên.");
            return;
        }
        const fullNamePattern = /^[\p{L}\p{M}]+(?:\s+[\p{L}\p{M}]+)*$/u;
        if (!fullNamePattern.test(fullName)) {
            $('#formError').text("Họ và tên chỉ được chứa chữ cái và khoảng trắng.");
            return;
        }

        if (phoneNumber) {
            const phonePattern = /^0\d{9}$/;
            if (!phonePattern.test(phoneNumber)) {
                $('#formError').text("Số điện thoại phải gồm 10 số và bắt đầu bằng 0.");
                return;
            }
        }

        if (roleId === '2') {
            if (!studentId) {
                $('#formError').text("Vui lòng nhập mã sinh viên.");
                return;
            }
            const studentIdPattern = /^[A-Za-z]{2}\d{6}$/;
            if (!studentIdPattern.test(studentId)) {
                $('#formError').text("Mã sinh viên phải gồm 8 ký tự: 2 chữ cái đầu và 6 chữ số sau (ví dụ: SE123456).");
                return;
            }
        }

        const requestData = {
            IdToken: idToken,
            RoleId: parseInt(roleId, 10),
            FullName: fullName,
            PhoneNumber: phoneNumber || null,
            StudentId: studentId || null
        };

        const needsCompletion = localStorage.getItem('tempGoogleNeedsCompletion') === '1';
        const endpoint = needsCompletion ? "/api/auth/google-complete-profile" : "/api/auth/google-register";

        apiClient.post(endpoint, requestData)
            .then(function (response) {
                localStorage.removeItem('tempGoogleToken');
                localStorage.removeItem('tempGoogleEmail');
                localStorage.removeItem('tempGoogleNeedsCompletion');
                localStorage.removeItem('pendingRegistrationRole');

                window.location.href = '/';
            })
            .catch(function (err) {
                if (err.message) {
                    $('#formError').text(err.message);
                } else {
                    $('#formError').text("Đăng ký không thành công. Vui lòng thử lại.");
                }
            });
    });
});
