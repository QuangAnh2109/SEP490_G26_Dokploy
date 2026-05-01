$(function () {
    const userId = parseInt($('#hdnUserId').val());
    if (!userId) {
        window.location.href = '/Admin/Users';
        return;
    }

    let currentUserData = null;

    function renderRole(roleId) {
        if (roleId === 1) return 'Giáo viên';
        if (roleId === 2) return 'Học sinh';
        if (roleId === 3) return 'Quản trị viên';
        return 'Không rõ';
    }

    function loadUserDetail() {
        apiClient.get('/api/admin/users/' + userId)
            .then(function (user) {
                currentUserData = user;
                
                const initials = user.fullName ? user.fullName.substring(0, 2).toUpperCase() : user.email.substring(0, 2).toUpperCase();
                
                let actionsHtml = '';
                if (user.roleId !== 3) {
                    if (user.status === 1) {
                        actionsHtml += `<button class="btn btn-secondary btn-sm" onclick="openModal('lockModal')">Khóa tài khoản</button>`;
                    } else {
                        actionsHtml += `<button class="btn btn-secondary btn-sm" onclick="handleUnlock()">Mở khóa</button>`;
                    }
                    actionsHtml += `<button class="btn btn-primary btn-sm" onclick="openModal('resetModal')">Cấp lại mật khẩu</button>`;
                }

                $('#userBanner').html(`
                    <div class="avatar-lg">${initials}</div>
                    <div class="ident">
                        <div class="name">${escapeHtml(user.fullName || 'Chưa cập nhật')}</div>
                        <div class="email">${escapeHtml(user.email)}</div>
                        <div class="badges">
                            <span class="pill pill-soft">${renderRole(user.roleId)}</span>
                            ${user.status === 1 ? '<span class="pill pill-success"><span class="dot"></span>Đang hoạt động</span>' : '<span class="pill pill-danger"><span class="dot"></span>Đã khóa</span>'}
                        </div>
                    </div>
                    <div class="actions">
                        ${actionsHtml}
                    </div>
                `);

                let kvHtml = `
                    <li><div class="k">User ID</div><div class="v">${user.userId}</div></li>
                    <li><div class="k">Email</div><div class="v">${escapeHtml(user.email)}</div></li>
                    <li><div class="k">Họ tên</div><div class="v">${escapeHtml(user.fullName || '-')}</div></li>
                    <li><div class="k">Vai trò</div><div class="v">${renderRole(user.roleId)}</div></li>
                `;
                
                if (user.studentId) {
                    kvHtml += `<li><div class="k">Mã sinh viên</div><div class="v">${escapeHtml(user.studentId)}</div></li>`;
                }
                
                if (user.phoneNumber) {
                    kvHtml += `<li><div class="k">Số điện thoại</div><div class="v">${escapeHtml(user.phoneNumber)}</div></li>`;
                }

                kvHtml += `
                    <li><div class="k">Trạng thái</div><div class="v">${user.status === 1 ? 'Đang hoạt động' : 'Đã khóa'}</div></li>
                    <li><div class="k">Kích hoạt</div><div class="v">${!user.mustChangePassword ? 'Đã kích hoạt' : 'Chưa kích hoạt'}</div></li>
                `;

                $('#userKvList').html(kvHtml);
            })
            .catch(function (err) {
                let msg = 'Lỗi khi tải chi tiết.';
                if (err.xhr && err.xhr.responseJSON?.code === 'ADMIN_USER_NOT_FOUND') {
                    msg = 'Không tìm thấy tài khoản.';
                }
                showToast(msg, 'error');
            });
    }

    window.handleUnlock = function() {
        showConfirm('Bạn có chắc chắn muốn mở khóa tài khoản này?', 'Xác nhận mở khóa', function() {
            apiClient.patch('/api/admin/users/' + userId + '/unlock', {})
                .then(function() {
                    showToast('Đã mở khóa tài khoản.');
                    loadUserDetail();
                })
                .catch(function(err) {
                    let msg = err.message || 'Lỗi khi mở khóa.';
                    if (err.xhr && err.xhr.responseJSON?.code === 'ADMIN_USER_CANNOT_MODIFY_ADMIN') {
                        msg = 'Không thể thao tác trên tài khoản admin khác.';
                    }
                    showToast(msg, 'error');
                });
        });
    };

    $('#btnConfirmLock').on('click', function() {
        const $btn = $(this);
        $btn.prop('disabled', true).text('Đang khóa...');
        
        apiClient.patch('/api/admin/users/' + userId + '/lock', {})
            .then(function() {
                closeModal('lockModal');
                showToast('Đã khóa tài khoản.');
                loadUserDetail();
            })
            .catch(function(err) {
                let msg = err.message || 'Lỗi khi khóa.';
                if (err.xhr && err.xhr.responseJSON?.code === 'ADMIN_USER_CANNOT_LOCK_SELF') {
                    msg = 'Không thể khóa chính tài khoản của bạn.';
                } else if (err.xhr && err.xhr.responseJSON?.code === 'ADMIN_USER_CANNOT_MODIFY_ADMIN') {
                    msg = 'Không thể thao tác trên tài khoản admin khác.';
                }
                showToast(msg, 'error');
            })
            .finally(function() {
                $btn.prop('disabled', false).text('Khóa tài khoản');
            });
    });

    $('#btnConfirmReset').on('click', function() {
        const $btn = $(this);
        $btn.prop('disabled', true).text('Đang cấp lại...');
        
        apiClient.post('/api/admin/users/' + userId + '/reset-password', {})
            .then(function() {
                closeModal('resetModal');
                openModal('emailSentModal');
                loadUserDetail();
            })
            .catch(function(err) {
                let msg = err.message || 'Lỗi khi cấp lại mật khẩu.';
                if (err.xhr && err.xhr.responseJSON?.code === 'ADMIN_USER_CANNOT_MODIFY_ADMIN') {
                    msg = 'Không thể thao tác trên tài khoản admin khác.';
                } else if (err.xhr && err.xhr.responseJSON?.code === 'ADMIN_USER_EMAIL_SEND_FAILED') {
                    msg = 'Không gửi được email mật khẩu — kiểm tra cấu hình SMTP và thử lại.';
                }
                showToast(msg, 'error');
            })
            .finally(function() {
                $btn.prop('disabled', false).text('Cấp lại mật khẩu');
            });
    });

    if (window.userReady) {
        window.userReady.then(() => loadUserDetail());
    } else {
        loadUserDetail();
    }
});
