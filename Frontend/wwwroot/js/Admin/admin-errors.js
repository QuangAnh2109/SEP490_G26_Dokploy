const AdminApiErrors = Object.freeze({
    ADMIN_USER_EMAIL_EXISTS:        { msg: 'Email đã tồn tại.',                                                        field: 'emailError' },
    ADMIN_USER_INVALID_ROLE:        { msg: 'Chỉ tạo được tài khoản Giáo viên hoặc Học sinh.' },
    ADMIN_USER_EMAIL_SEND_FAILED:   { msg: 'Không gửi được email mật khẩu — kiểm tra cấu hình SMTP và thử lại.' },
    ADMIN_USER_CANNOT_LOCK_SELF:    { msg: 'Không thể khóa chính tài khoản của bạn.' },
    ADMIN_USER_CANNOT_MODIFY_ADMIN: { msg: 'Không thể thao tác trên tài khoản admin khác.' },
    ADMIN_USER_NOT_FOUND:           { msg: 'Không tìm thấy tài khoản.' },
    VALIDATION:                     { msg: 'Dữ liệu không hợp lệ — kiểm tra lại các trường đã nhập.' }
});

function showApiError(err, fallback) {
    const code = err?.xhr?.responseJSON?.code;
    const entry = AdminApiErrors[code];
    const msg = entry?.msg ?? err?.message ?? fallback;
    if (entry?.field) {
        $('#' + entry.field).text(msg).addClass('help-error');
    } else {
        showToast(msg, 'error');
    }
}
