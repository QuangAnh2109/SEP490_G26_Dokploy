$(function () {
    const tbody = document.getElementById('tbody-subjects');
    const pagerEl = document.getElementById('pager-subjects');
    const pageLoading = document.getElementById('page-loading');
    const pageContent = document.getElementById('page-content');
    const PAGE_SIZE = 10;

    let dataList = [];
    let currentPage = 1;
    let closeTargetId = 0;
    let firstLoad = true;

    function renderStatusPill(status) {
        if (status === 1) return '<span class="pill pill-success"><span class="dot"></span>Đang hoạt động</span>';
        return '<span class="pill pill-soft"><span class="dot"></span>Đã đóng</span>';
    }

    function load(page) {
        currentPage = Math.max(1, page || 1);
        const q = $('#filter-q').val().trim();
        const status = $('#filter-status').val();

        const params = new URLSearchParams();
        if (q) params.set('q', q);
        if (status) params.set('status', status);
        params.set('page', currentPage);
        params.set('pageSize', PAGE_SIZE);

        if (!firstLoad) {
            tbody.innerHTML = '';
        }

        apiClient.get('/api/admin/curriculum/subjects?' + params.toString())
            .then(res => {
                const items = res.items || [];
                dataList = items;

                if (firstLoad) {
                    pageLoading.classList.add('is-hidden');
                    pageContent.classList.remove('is-hidden');
                    firstLoad = false;
                }

                if (items.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="6" class="empty-state"><div class="title">Không có dữ liệu</div></td></tr>';
                    pagerEl.innerHTML = '';
                    return;
                }

                tbody.innerHTML = items.map(s => `
                    <tr data-id="${s.subjectId}" class="clickable-row">
                        <td><code>${escapeHtml(s.code)}</code></td>
                        <td style="font-weight: 500; color: var(--ink-900);">${escapeHtml(s.name)}</td>
                        <td>${renderStatusPill(s.status)}</td>
                        <td>${s.activeChapterCount} / ${s.chapterCount}</td>
                        <td>${s.activeClassCount} / ${s.classCount}</td>
                        <td class="text-right">
                            <button class="icon-btn" data-action="view" title="Chi tiết">
                                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" width="18" height="18">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                                </svg>
                            </button>
                            ${s.status === 1 ? `
                                <button class="icon-btn" data-action="close" title="Đóng môn học" style="margin-left: 8px;">
                                    <svg fill="none" stroke="var(--danger)" viewBox="0 0 24 24" width="18" height="18">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"></path>
                                    </svg>
                                </button>
                            ` : ''}
                        </td>
                    </tr>`).join('');

                AdminPager.render(pagerEl, {
                    current: res.page || currentPage,
                    pageSize: res.pageSize || PAGE_SIZE,
                    totalItems: res.totalItems || 0,
                    onChange: (target) => load(target)
                });
            })
            .catch(err => {
                if (firstLoad) {
                    pageLoading.classList.add('is-hidden');
                    pageContent.classList.remove('is-hidden');
                    firstLoad = false;
                }
                tbody.innerHTML = `<tr><td colspan="6" class="empty-state" style="color:var(--danger)">Lỗi: ${escapeHtml(err.message || 'Không tải được dữ liệu')}</td></tr>`;
                pagerEl.innerHTML = '';
            });
    }

    // Modal Create
    $('#btn-create').on('click', () => {
        const form = document.getElementById('form-create');
        form.reset();
        $(form).find('.help-error').text('');
        openModal('modal-stage-create');
    });

    $('#btn-submit-create').on('click', () => {
        const form = document.getElementById('form-create');
        const data = Object.fromEntries(new FormData(form));
        $(form).find('.help-error').text('');
        let hasErr = false;

        const codeVal = data.code.trim().toUpperCase();
        if (!codeVal) {
            $(form.code).siblings('.help-error').text('Vui lòng nhập mã môn học.');
            hasErr = true;
        } else if (!/^[A-Z0-9]+$/.test(codeVal)) {
            $(form.code).siblings('.help-error').text('Mã môn học chỉ gồm chữ cái và số, không khoảng trắng.');
            hasErr = true;
        } else if (codeVal.length > 20) {
            $(form.code).siblings('.help-error').text('Tối đa 20 ký tự.');
            hasErr = true;
        }

        if (!data.name.trim()) { $(form.name).siblings('.help-error').text('Vui lòng nhập tên môn học.'); hasErr = true; }

        if (hasErr) return;

        data.code = codeVal;

        apiClient.post('/api/admin/curriculum/subjects', data)
            .then(() => {
                closeModal('modal-stage-create');
                AdminUI.showNotice('success', 'Thành công', 'Đã tạo môn học mới.');
                load(1);
            })
            .catch(err => {
                AdminUI.showError(err);
            });
    });

    // Handle Row Clicks
    tbody.addEventListener('click', (e) => {
        const tr = e.target.closest('tr');
        if (!tr) return;

        const id = parseInt(tr.dataset.id);
        const btn = e.target.closest('button');

        if (btn && btn.dataset.action === 'close') {
            e.stopPropagation();
            const item = dataList.find(x => x.subjectId === id);
            if (!item) return;

            closeTargetId = id;
            document.getElementById('close-name').textContent = item.name;
            openModal('modal-stage-close');
            return;
        }

        window.location.href = '/Admin/SubjectDetail/' + id;
    });

    // Close Confirmation
    $('#btn-confirm-close').on('click', () => {
        if (!closeTargetId) return;

        apiClient.patch(`/api/admin/curriculum/subjects/${closeTargetId}/close`, null)
            .then(() => {
                closeModal('modal-stage-close');
                AdminUI.showNotice('success', 'Thành công', 'Đã đóng môn học.');
                load(currentPage);
            })
            .catch(err => {
                AdminUI.showError(err);
            });
    });

    // Filters — search/filter luôn reset về trang 1
    let timeout;
    $('#filter-q').on('input', () => {
        clearTimeout(timeout);
        timeout = setTimeout(() => load(1), 300);
    });
    $('#filter-status').on('change', () => load(1));

    // Initial load
    if (window.userReady) {
        window.userReady.then(() => load(1));
    } else {
        load(1);
    }
});
