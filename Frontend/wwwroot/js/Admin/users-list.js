$(function () {
    const $tbody = $('#userTableBody');
    const $q = $('#userQ');
    const $role = $('#userRole');
    const $status = $('#userStatus');
    let currentPage = 1;

    function renderStatusPill(status) {
        if (status === 1) return '<span class="pill pill-success"><span class="dot"></span>Đang hoạt động</span>';
        return '<span class="pill pill-danger"><span class="dot"></span>Đã khóa</span>';
    }

    function renderRole(roleId) {
        if (roleId === 1) return 'Giáo viên';
        if (roleId === 2) return 'Học sinh';
        if (roleId === 3) return 'Quản trị viên';
        return 'Không rõ';
    }

    function renderActivation(user) {
        if (!user.mustChangePassword) return '<span class="muted">Đã kích hoạt</span>';
        return '<span class="pill pill-warn">Chưa kích hoạt</span>';
    }

    function loadUsers(page) {
        currentPage = page || 1;
        const query = {
            q: $q.val().trim(),
            roleId: $role.val() ? parseInt($role.val()) : null,
            status: $status.val() ? parseInt($status.val()) : null,
            page: currentPage,
            pageSize: 20
        };

        const params = new URLSearchParams();
        if (query.q) params.append('q', query.q);
        if (query.roleId) params.append('roleId', query.roleId);
        if (query.status !== null && !isNaN(query.status)) params.append('status', query.status);
        params.append('page', query.page);
        params.append('pageSize', query.pageSize);

        $tbody.html('<tr><td colspan="4" class="empty-state">Đang tải...</td></tr>');

        apiClient.get('/api/admin/users?' + params.toString())
            .then(res => {
                $('#totalCount').text(res.total);
                if (res.items.length === 0) {
                    $tbody.html('<tr><td colspan="4" class="empty-state"><div class="title">Không có dữ liệu</div></td></tr>');
                    $('#pagerWrapper').empty();
                    return;
                }

                let html = '';
                res.items.forEach(u => {
                    const initials = u.fullName ? u.fullName.substring(0, 2).toUpperCase() : u.email.substring(0, 2).toUpperCase();
                    html += `<tr onclick="window.location.href='/Admin/UserDetail/${u.userId}'">
                        <td>
                            <div class="user-cell">
                                <div class="avatar">${initials}</div>
                                <div class="meta">
                                    <div class="name">${escapeHtml(u.fullName || 'Chưa cập nhật')}</div>
                                    <div class="email">${escapeHtml(u.email)}</div>
                                </div>
                            </div>
                        </td>
                        <td>${renderRole(u.roleId)}</td>
                        <td>${renderStatusPill(u.status)}</td>
                        <td>${renderActivation(u)}</td>
                    </tr>`;
                });
                $tbody.html(html);

                const totalPages = Math.ceil(res.total / 20);
                if (totalPages > 1) {
                    $('#pagerWrapper').html(`
                        <div class="pager" id="pagerContainer">
                            <div class="info">
                                Hiển thị ${currentPage} / ${totalPages} trang (${res.total} dòng)
                            </div>
                            <div class="ctrls">
                                <button class="btn-pager" ${currentPage <= 1 ? "disabled" : ""} onclick="window.changePage(${currentPage - 1})">&larr;</button>
                                <button class="active">${currentPage}</button>
                                <button class="btn-pager" ${currentPage >= totalPages ? "disabled" : ""} onclick="window.changePage(${currentPage + 1})">&rarr;</button>
                            </div>
                        </div>
                    `);
                } else {
                    $('#pagerWrapper').empty();
                }
            })
            .catch(err => {
                $tbody.html(`<tr><td colspan="4" class="empty-state" style="color:var(--danger)">Lỗi tải dữ liệu: ${err.message}</td></tr>`);
            });
    }

    window.changePage = function(page) {
        loadUsers(page);
    };

    $('#btnFilter').on('click', () => loadUsers(1));

    if (window.userReady) {
        window.userReady.then(() => loadUsers(1));
    } else {
        loadUsers(1);
    }
});
