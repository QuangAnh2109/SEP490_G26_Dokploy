$(function () {
    const tbody = document.getElementById('tbody-subjects');
    let dataList = [];
    let closeTargetId = 0;

    function renderStatusPill(status) {
        if (status === 1) return '<span class="pill pill-success"><span class="dot"></span>Đang hoạt động</span>';
        return '<span class="pill pill-soft"><span class="dot"></span>Đã đóng</span>';
    }

    function load() {
        const q = $('#filter-q').val().trim();
        const status = $('#filter-status').val();
        
        const params = new URLSearchParams();
        if (q) params.set('q', q);
        if (status) params.set('status', status);

        tbody.innerHTML = '<tr><td colspan="6" class="empty-state">Đang tải...</td></tr>';

        apiClient.get('/api/admin/curriculum/subjects?' + params.toString())
            .then(items => {
                dataList = items;
                if (items.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="6" class="empty-state"><div class="title">Không có dữ liệu</div></td></tr>';
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
            })
            .catch(err => {
                tbody.innerHTML = `<tr><td colspan="6" class="empty-state" style="color:var(--danger)">Lỗi: ${escapeHtml(err.message)}</td></tr>`;
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

        data.code = codeVal; // uppercase code

        apiClient.post('/api/admin/curriculum/subjects', data)
            .then(res => {
                closeModal('modal-stage-create');
                AdminUI.showNotice('success', 'Thành công', 'Đã tạo môn học mới.');
                load();
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
            e.stopPropagation(); // prevent row click navigation
            const item = dataList.find(x => x.subjectId === id);
            if (!item) return;
            
            closeTargetId = id;
            document.getElementById('close-name').textContent = item.name;
            openModal('modal-stage-close');
            return;
        }

        // Navigate to detail
        window.location.href = '/Admin/SubjectDetail/' + id;
    });

    // Close Confirmation
    $('#btn-confirm-close').on('click', () => {
        if (!closeTargetId) return;
        
        apiClient.patch(`/api/admin/curriculum/subjects/${closeTargetId}/close`, null)
            .then(res => {
                closeModal('modal-stage-close');
                AdminUI.showNotice('success', 'Thành công', 'Đã đóng môn học.');
                load();
            })
            .catch(err => {
                AdminUI.showError(err);
            });
    });

    // Filters
    let timeout;
    $('#filter-q').on('input', () => {
        clearTimeout(timeout);
        timeout = setTimeout(load, 300);
    });
    $('#filter-status').on('change', load);

    // Initial load
    if (window.userReady) {
        window.userReady.then(() => load());
    } else {
        load();
    }
});
