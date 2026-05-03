$(function () {
    const $tbody = $('#userTableBody');
    const $q = $('#userQ');
    const $role = $('#userRole');
    const $status = $('#userStatus');
    const pagerEl = document.getElementById('pager-users');
    const pageLoading = document.getElementById('page-loading');
    const pageContent = document.getElementById('page-content');
    const PAGE_SIZE = 10;

    let currentPage = 1;
    let firstLoad = true;

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
        currentPage = Math.max(1, page || 1);

        const params = new URLSearchParams();
        const q = $q.val().trim();
        const roleId = $role.val();
        const status = $status.val();

        if (q) params.append('q', q);
        if (roleId) params.append('roleId', roleId);
        if (status !== '') params.append('status', status);
        params.append('page', currentPage);
        params.append('pageSize', PAGE_SIZE);

        if (!firstLoad) {
            $tbody.html('');
        }

        apiClient.get('/api/admin/users?' + params.toString())
            .then(res => {
                if (firstLoad) {
                    pageLoading.classList.add('is-hidden');
                    pageContent.classList.remove('is-hidden');
                    firstLoad = false;
                }

                $('#totalCount').text(res.total);

                if (!res.items || res.items.length === 0) {
                    $tbody.html('<tr><td colspan="4" class="empty-state"><div class="title">Không có dữ liệu</div></td></tr>');
                    pagerEl.innerHTML = '';
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

                AdminPager.render(pagerEl, {
                    current: res.page || currentPage,
                    pageSize: res.pageSize || PAGE_SIZE,
                    totalItems: res.total || 0,
                    onChange: (target) => loadUsers(target)
                });
            })
            .catch(err => {
                if (firstLoad) {
                    pageLoading.classList.add('is-hidden');
                    pageContent.classList.remove('is-hidden');
                    firstLoad = false;
                }
                $tbody.html(`<tr><td colspan="4" class="empty-state" style="color:var(--danger)">Lỗi tải dữ liệu: ${escapeHtml(err.message || 'Không xác định')}</td></tr>`);
                pagerEl.innerHTML = '';
            });
    }

    // Filters — search/filter luôn reset về trang 1
    let timeout;
    $q.on('input', () => {
        clearTimeout(timeout);
        timeout = setTimeout(() => loadUsers(1), 300);
    });
    $role.on('change', () => loadUsers(1));
    $status.on('change', () => loadUsers(1));

    if (window.userReady) {
        window.userReady.then(() => loadUsers(1));
    } else {
        loadUsers(1);
    }
});
