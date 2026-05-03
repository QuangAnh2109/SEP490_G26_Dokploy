$(function () {
    const tbody = document.getElementById('tbody-semesters');
    const pagerEl = document.getElementById('pager-semesters');
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

        apiClient.get('/api/admin/curriculum/semesters?' + params.toString())
            .then(res => {
                const items = res.items || [];
                dataList = items;

                if (firstLoad) {
                    pageLoading.classList.add('is-hidden');
                    pageContent.classList.remove('is-hidden');
                    firstLoad = false;
                }

                if (items.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="7" class="empty-state"><div class="title">Không có dữ liệu</div></td></tr>';
                    pagerEl.innerHTML = '';
                    return;
                }

                tbody.innerHTML = items.map(s => `
                    <tr data-id="${s.semesterId}">
                        <td><code>${escapeHtml(s.code)}</code></td>
                        <td>${escapeHtml(s.name)}</td>
                        <td>${s.startDate}</td>
                        <td>${s.endDate}</td>
                        <td>${renderStatusPill(s.status)}</td>
                        <td>${s.activeClassCount} / ${s.activeExamCount}</td>
                        <td class="text-right">
                            ${s.status === 1 ? `
                                <button class="icon-btn" data-action="edit" title="Sửa">
                                    <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" width="18" height="18">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                                    </svg>
                                </button>
                                <button class="icon-btn" data-action="close" title="Đóng">
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
                tbody.innerHTML = `<tr><td colspan="7" class="empty-state" style="color:var(--danger)">Lỗi: ${escapeHtml(err.message || 'Không tải được dữ liệu')}</td></tr>`;
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

        if (!data.code.trim()) { $(form.code).siblings('.help-error').text('Vui lòng nhập mã kỳ học.'); hasErr = true; }
        if (!data.name.trim()) { $(form.name).siblings('.help-error').text('Vui lòng nhập tên kỳ học.'); hasErr = true; }
        if (!data.startDate) { $(form.startDate).siblings('.help-error').text('Bắt buộc.'); hasErr = true; }
        if (!data.endDate) { $(form.endDate).siblings('.help-error').text('Bắt buộc.'); hasErr = true; }

        if (data.startDate && data.endDate && data.endDate <= data.startDate) {
            $(form.endDate).siblings('.help-error').text('Ngày kết thúc phải sau ngày bắt đầu.');
            hasErr = true;
        }

        if (hasErr) return;

        apiClient.post('/api/admin/curriculum/semesters', data)
            .then(() => {
                closeModal('modal-stage-create');
                AdminUI.showNotice('success', 'Thành công', 'Đã tạo kỳ học.');
                load(1);
            })
            .catch(err => {
                const msg = err.code ? AdminUI.translateError(err.code) : err.message;
                AdminUI.showNotice('error', 'Lỗi', msg);
            });
    });

    // Modal Edit
    tbody.addEventListener('click', (e) => {
        const btn = e.target.closest('button');
        if (!btn) return;
        const tr = btn.closest('tr');
        const id = parseInt(tr.dataset.id);
        const action = btn.dataset.action;

        const item = dataList.find(x => x.semesterId === id);
        if (!item) return;

        if (action === 'edit') {
            const form = document.getElementById('form-edit');
            form.reset();
            $(form).find('.help-error').text('');

            form.id.value = item.semesterId;
            form.concurrencyStamp.value = item.concurrencyStamp;
            form.code.value = item.code;
            form.name.value = item.name;
            form.startDate.value = item.startDate;
            form.endDate.value = item.endDate;

            openModal('modal-stage-edit');
        } else if (action === 'close') {
            closeTargetId = id;
            document.getElementById('close-name').textContent = item.name;
            document.getElementById('close-class-count').textContent = item.activeClassCount;
            document.getElementById('close-exam-count').textContent = item.activeExamCount;
            openModal('modal-stage-close');
        }
    });

    $('#btn-submit-edit').on('click', () => {
        const form = document.getElementById('form-edit');
        const data = Object.fromEntries(new FormData(form));
        $(form).find('.help-error').text('');
        let hasErr = false;

        if (!data.name.trim()) { $(form.name).siblings('.help-error').text('Vui lòng nhập tên kỳ học.'); hasErr = true; }
        if (!data.startDate) { $(form.startDate).siblings('.help-error').text('Bắt buộc.'); hasErr = true; }
        if (!data.endDate) { $(form.endDate).siblings('.help-error').text('Bắt buộc.'); hasErr = true; }

        if (data.startDate && data.endDate && data.endDate <= data.startDate) {
            $(form.endDate).siblings('.help-error').text('Ngày kết thúc phải sau ngày bắt đầu.');
            hasErr = true;
        }

        if (hasErr) return;

        const payload = {
            name: data.name,
            startDate: data.startDate,
            endDate: data.endDate,
            concurrencyStamp: data.concurrencyStamp
        };

        apiClient.put(`/api/admin/curriculum/semesters/${data.id}`, payload)
            .then(() => {
                closeModal('modal-stage-edit');
                AdminUI.showNotice('success', 'Thành công', 'Đã lưu thay đổi.');
                load(currentPage);
            })
            .catch(err => {
                const msg = err.code ? AdminUI.translateError(err.code) : err.message;
                AdminUI.showNotice('error', 'Lỗi', msg);
            });
    });

    // Close Confirmation
    $('#btn-confirm-close').on('click', () => {
        if (!closeTargetId) return;

        apiClient.patch(`/api/admin/curriculum/semesters/${closeTargetId}/close`, null)
            .then(() => {
                closeModal('modal-stage-close');
                AdminUI.showNotice('success', 'Thành công', 'Đã đóng kỳ học.');
                load(currentPage);
            })
            .catch(err => {
                const msg = err.code ? AdminUI.translateError(err.code) : err.message;
                AdminUI.showNotice('error', 'Lỗi', msg);
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
