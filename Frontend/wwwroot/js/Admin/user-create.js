$(function () {
    const $roleId = $('#roleId');
    const $studentIdRow = $('#studentIdRow');
    const $form = $('#createUserForm');
    const $btn = $('#btnSubmit');

    $roleId.on('change', function () {
        if ($(this).val() === RoleIds.Student) {
            $studentIdRow.show();
        } else {
            $studentIdRow.hide();
            $('#studentId').val('');
            $('#studentIdError').text('');
        }
    });

    $('#studentId').on('input', function () {
        this.value = this.value.toUpperCase();
    });

    $form.on('submit', function (e) {
        e.preventDefault();
        
        $('.help').text('');
        let hasError = false;

        const roleIdRaw = $roleId.val();
        const roleId = parseInt(roleIdRaw, 10);
        const fullName = $('#fullName').val().trim();
        const email = $('#email').val().trim();
        const phoneNumber = $('#phoneNumber').val().trim();
        const studentId = $('#studentId').val().trim();

        if (!AdminUserValidation.isValidFullName(fullName)) {
            $('#fullNameError').text('Họ tên phải từ 2 đến 200 ký tự.');
            hasError = true;
        }

        if (!AdminUserValidation.isValidEmail(email)) {
            $('#emailError').text('Email không hợp lệ.');
            hasError = true;
        }

        if (phoneNumber && !AdminUserValidation.isValidPhoneNumber(phoneNumber)) {
            $('#phoneNumberError').text('Số điện thoại phải gồm 10 chữ số bắt đầu bằng 0.');
            hasError = true;
        }

        if (roleIdRaw === RoleIds.Student && !AdminUserValidation.isValidStudentId(studentId)) {
            $('#studentIdError').text('Mã sinh viên phải có dạng 2 chữ cái + 6 chữ số (VD: SE123456).');
            hasError = true;
        }

        if (hasError) return;

        $btn.prop('disabled', true).text('Đang tạo...');

        const data = {
            roleId,
            fullName,
            email,
            phoneNumber: phoneNumber || null,
            studentId: roleIdRaw === RoleIds.Student ? studentId : null
        };

        apiClient.post('/api/admin/users', data)
            .then(function () {
                $('#modalEmailDisplay').text(email);
                openModal('emailSentModal');
            })
            .catch(function (err) {
                AdminUI.showError(err, 'Lỗi khi tạo tài khoản.');
            })
            .finally(function () {
                $btn.prop('disabled', false).text('Tạo tài khoản');
            });
    });
});
