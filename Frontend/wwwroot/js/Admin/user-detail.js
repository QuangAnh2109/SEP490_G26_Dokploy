$(function () {
    const userId = parseInt($('#hdnUserId').val());
    if (!userId) {
        window.location.href = '/Admin/Users';
        return;
    }

    const pageLoading = document.getElementById('page-loading');
    const pageContent = document.getElementById('page-content');
    let firstLoad = true;

    function revealContent() {
        if (firstLoad) {
            pageLoading.classList.add('is-hidden');
            pageContent.classList.remove('is-hidden');
            firstLoad = false;
        }
    }

    function renderRole(roleId) {
        if (roleId === 1) return 'Giáo viên';
        if (roleId === 2) return 'Học sinh';
        if (roleId === 3) return 'Quản trị viên';
        return 'Không rõ';
    }

    function loadUserDetail() {
        apiClient.get('/api/admin/users/' + userId)
            .then(function (user) {
                revealContent();
                const initials = user.fullName ? user.fullName.substring(0, 2).toUpperCase() : user.email.substring(0, 2).toUpperCase();
                
                let actionsHtml = '';
                if (user.roleId !== 3) {
                    if (user.status === 1) {
                        actionsHtml += `<button class="btn btn-secondary btn-sm" onclick="openModal('lockModal')">Khóa tài khoản</button>`;
                    } else {
                        actionsHtml += `<button class="btn btn-primary btn-sm" onclick="openModal('unlockModal')">Mở khóa</button>`;
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
                    <li><div class="k">Số điện thoại</div><div class="v">${escapeHtml(user.phoneNumber || '-')}</div></li>
                `;

                if (user.roleId === 2) {
                    kvHtml += `<li><div class="k">Mã sinh viên</div><div class="v">${escapeHtml(user.studentId || '-')}</div></li>`;
                }

                kvHtml += `
                    <li><div class="k">Trạng thái</div><div class="v">${user.status === 1 ? 'Đang hoạt động' : 'Đã khóa'}</div></li>
                    <li><div class="k">Kích hoạt</div><div class="v">${!user.mustChangePassword ? 'Đã kích hoạt' : 'Chưa kích hoạt'}</div></li>
                `;

                $('#userKvList').html(kvHtml);
            })
            .catch(function (err) {
                revealContent();
                AdminUI.showError(err, 'Lỗi khi tải chi tiết.');
            });
    }

    $('#btnConfirmUnlock').on('click', function() {
        const $btn = $(this);
        $btn.prop('disabled', true).text('Đang mở khóa...');

        apiClient.patch('/api/admin/users/' + userId + '/unlock', {})
            .then(function() {
                closeModal('unlockModal');
                AdminUI.showNotice('success', 'Thành công', 'Đã mở khóa tài khoản.');
                loadUserDetail();
            })
            .catch(function(err) {
                AdminUI.showError(err, 'Lỗi khi mở khóa.');
            })
            .finally(function() {
                $btn.prop('disabled', false).text('Mở khóa');
            });
    });

    $('#btnConfirmLock').on('click', function() {
        const $btn = $(this);
        $btn.prop('disabled', true).text('Đang khóa...');

        apiClient.patch('/api/admin/users/' + userId + '/lock', {})
            .then(function() {
                closeModal('lockModal');
                AdminUI.showNotice('success', 'Thành công', 'Đã khóa tài khoản.');
                loadUserDetail();
            })
            .catch(function(err) {
                AdminUI.showError(err, 'Lỗi khi khóa.');
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
                AdminUI.showError(err, 'Lỗi khi cấp lại mật khẩu.');
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
