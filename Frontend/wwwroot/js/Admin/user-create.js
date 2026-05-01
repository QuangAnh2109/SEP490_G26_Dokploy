$(function () {
    const $roleId = $('#roleId');
    const $studentIdRow = $('#studentIdRow');
    const $form = $('#createUserForm');
    const $btn = $('#btnSubmit');

    $roleId.on('change', function () {
        if ($(this).val() === '2') {
            $studentIdRow.show();
        } else {
            $studentIdRow.hide();
            $('#studentId').val('');
        }
    });

    $form.on('submit', function (e) {
        e.preventDefault();
        
        $('.help').text('');
        let hasError = false;

        const roleId = parseInt($roleId.val());
        const fullName = $('#fullName').val().trim();
        const email = $('#email').val().trim();
        const studentId = $('#studentId').val().trim();

        if (!AdminUserValidation.isValidFullName(fullName)) {
            $('#fullNameError').text('Họ tên phải từ 2 đến 200 ký tự.');
            hasError = true;
        }

        if (!AdminUserValidation.isValidEmail(email)) {
            $('#emailError').text('Email không hợp lệ.');
            hasError = true;
        }

        if (roleId === 2 && !AdminUserValidation.isValidStudentId(studentId)) {
            $('#studentIdError').text('Mã sinh viên phải có dạng 2 chữ cái + 6 chữ số (VD: SE123456).');
            hasError = true;
        }

        if (hasError) return;

        $btn.prop('disabled', true).text('Đang tạo...');

        const data = { roleId, fullName, email, studentId: roleId === 2 ? studentId : null };

        apiClient.post('/api/admin/users', data)
            .then(function (res) {
                $('#modalEmailDisplay').text(email);
                openModal('emailSentModal');
            })
            .catch(function (err) {
                let msg = err.message || 'Lỗi khi tạo tài khoản.';
                if (err.xhr && err.xhr.responseJSON?.code === 'ADMIN_USER_EMAIL_EXISTS') {
                    msg = 'Email đã tồn tại.';
                    $('#emailError').text(msg);
                } else if (err.xhr && err.xhr.responseJSON?.code === 'ADMIN_USER_INVALID_ROLE') {
                    msg = 'Chỉ tạo được tài khoản Giáo viên hoặc Học sinh.';
                } else if (err.xhr && err.xhr.responseJSON?.code === 'ADMIN_USER_EMAIL_SEND_FAILED') {
                    msg = 'Không gửi được email mật khẩu — kiểm tra cấu hình SMTP và thử lại.';
                } else {
                    showToast(msg, 'error');
                }
            })
            .finally(function () {
                $btn.prop('disabled', false).text('Tạo tài khoản');
            });
    });
});
