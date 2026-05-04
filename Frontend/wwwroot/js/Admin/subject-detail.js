$(function () {
    const SUBJECT_ID = parseInt(document.querySelector('[data-subject-id]').dataset.subjectId, 10);
    const listEl = document.getElementById('chapter-list');
    const pageLoading = document.getElementById('page-loading');
    const pageContent = document.getElementById('page-content');

    let subjectDetail = null;
    let deleteTargetId = 0;
    let sortableInstance = null;
    let firstLoad = true;

    function renderStatus(status) {
        if (status === 1) return '<span class="pill pill-success"><span class="dot"></span>Đang hoạt động</span>';
        return '<span class="pill pill-soft"><span class="dot"></span>Đã đóng</span>';
    }

    function formatDate(isoString) {
        if (!isoString) return '';
        const d = new Date(isoString);
        return d.toLocaleString('vi-VN');
    }

    function loadDetail() {
        apiClient.get('/api/admin/curriculum/subjects/' + SUBJECT_ID)
            .then(data => {
                subjectDetail = data;

                if (firstLoad) {
                    pageLoading.classList.add('is-hidden');
                    pageContent.classList.remove('is-hidden');
                    firstLoad = false;
                }

                // Render subject header
                document.getElementById('subject-name').textContent = data.name;
                document.getElementById('subject-code').textContent = data.code;
                document.getElementById('subject-status').innerHTML = renderStatus(data.status);
                document.getElementById('subject-description').textContent = data.description || 'Không có mô tả.';
                
                document.getElementById('created-by').textContent = data.createdByName || 'Hệ thống';
                document.getElementById('created-at').textContent = formatDate(data.createdAtUtc);
                document.getElementById('updated-by').textContent = data.updatedByName || 'Hệ thống';
                document.getElementById('updated-at').textContent = formatDate(data.updatedAtUtc);

                document.getElementById('chapter-stats').textContent = `${data.activeChapterCount} / ${data.chapterCount} chương đang bật`;

                // Handle D5 rule
                const isClosed = data.status === 0;
                if (isClosed) {
                    $('#btn-edit-subject').hide();
                    $('#btn-close-subject').hide();
                    $('#banner-subject-closed').removeClass('is-hidden');
                    $('#btn-create-chapter').hide();
                    $('#btn-save-order').hide();
                    listEl.classList.add('chapter-list-locked');
                    if (sortableInstance) {
                        sortableInstance.destroy();
                        sortableInstance = null;
                    }
                } else {
                    $('#btn-edit-subject').show();
                    $('#btn-close-subject').show();
                    $('#banner-subject-closed').addClass('is-hidden');
                    $('#btn-create-chapter').show();
                    $('#btn-save-order').show();
                    listEl.classList.remove('chapter-list-locked');
                }

                // Render chapters
                if (!data.chapters || data.chapters.length === 0) {
                    listEl.innerHTML = '<li class="empty-state">Chưa có chương nào.</li>';
                } else {
                    listEl.innerHTML = data.chapters.map(c => `
                        <li class="chapter-row ${c.status === 0 ? 'row-deleted' : ''}" data-chapter-id="${c.chapterId}">
                            <div class="drag-handle" title="Kéo để sắp xếp" ${isClosed ? 'style="visibility:hidden;"' : ''}>
                                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" width="20" height="20">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8h16M4 16h16"></path>
                                </svg>
                            </div>
                            <div class="order-badge">${c.displayOrder}</div>
                            <div style="flex:1; min-width:0;">
                                <div style="font-weight:600; color:var(--ink-900); display:flex; align-items:center; gap:8px;">
                                    ${escapeHtml(c.name)}
                                    ${c.status === 0 ? '<span class="pill pill-soft">Đã xoá</span>' : ''}
                                </div>
                                ${c.description ? `<div style="font-size:12px; color:var(--ink-500); margin-top:2px;">${escapeHtml(c.description)}</div>` : ''}
                            </div>
                            <div style="display:flex; gap:4px; margin-left:16px;">
                                <button class="icon-btn btn-edit-chapter" title="Sửa chương" ${isClosed || c.status === 0 ? 'style="display:none;"' : ''}>
                                    <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" width="16" height="16">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                                    </svg>
                                </button>
                                <button class="icon-btn btn-delete-chapter" title="Xoá chương" ${isClosed || c.status === 0 ? 'style="display:none;"' : ''}>
                                    <svg fill="none" stroke="var(--danger)" viewBox="0 0 24 24" width="16" height="16">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                                    </svg>
                                </button>
                            </div>
                        </li>
                    `).join('');

                    // Re-init Sortable if active
                    if (!isClosed && !sortableInstance) {
                        sortableInstance = Sortable.create(listEl, {
                            handle: '.drag-handle',
                            filter: '.row-deleted',
                            preventOnFilter: false,
                            animation: 150,
                            onEnd: () => {
                                document.getElementById('btn-save-order').disabled = false;
                            }
                        });
                    }
                }
            })
            .catch(err => {
                if (firstLoad) {
                    pageLoading.classList.add('is-hidden');
                    pageContent.classList.remove('is-hidden');
                    firstLoad = false;
                }
                if (err.code === 'SUBJECT_NOT_FOUND') {
                    AdminUI.showNotice('error', 'Lỗi', 'Không tìm thấy môn học.');
                    setTimeout(() => window.location.href = '/Admin/Subjects', 1500);
                } else {
                    AdminUI.showError(err);
                }
            });
    }

    // Edit Subject
    $('#btn-edit-subject').on('click', () => {
        if (!subjectDetail) return;
        const form = document.getElementById('form-edit-subject');
        form.reset();
        $(form).find('.help-error').text('');

        form.concurrencyStamp.value = subjectDetail.concurrencyStamp;
        document.getElementById('edit-subject-code').value = subjectDetail.code;
        form.name.value = subjectDetail.name;
        form.description.value = subjectDetail.description || '';

        openModal('modal-stage-edit-subject');
    });

    $('#btn-submit-edit-subject').on('click', () => {
        const form = document.getElementById('form-edit-subject');
        const data = Object.fromEntries(new FormData(form));
        $(form).find('.help-error').text('');

        if (!data.name.trim()) {
            $(form.name).siblings('.help-error').text('Vui lòng nhập tên môn học.');
            return;
        }

        apiClient.put(`/api/admin/curriculum/subjects/${SUBJECT_ID}`, {
            name: data.name,
            description: data.description,
            concurrencyStamp: data.concurrencyStamp
        }).then(() => {
            closeModal('modal-stage-edit-subject');
            AdminUI.showNotice('success', 'Thành công', 'Đã lưu thông tin môn học.');
            loadDetail();
        }).catch(err => AdminUI.showError(err));
    });

    // Close Subject
    $('#btn-close-subject').on('click', () => openModal('modal-stage-close-subject'));
    $('#btn-confirm-close-subject').on('click', () => {
        apiClient.patch(`/api/admin/curriculum/subjects/${SUBJECT_ID}/close`, null)
            .then(() => {
                closeModal('modal-stage-close-subject');
                AdminUI.showNotice('success', 'Thành công', 'Đã đóng môn học.');
                loadDetail();
            })
            .catch(err => AdminUI.showError(err));
    });

    // Create Chapter
    $('#btn-create-chapter').on('click', () => {
        const form = document.getElementById('form-create-chapter');
        form.reset();
        $(form).find('.help-error').text('');
        openModal('modal-stage-create-chapter');
    });

    $('#btn-submit-create-chapter').on('click', () => {
        const form = document.getElementById('form-create-chapter');
        const data = Object.fromEntries(new FormData(form));
        $(form).find('.help-error').text('');

        if (!data.name.trim()) {
            $(form.name).siblings('.help-error').text('Vui lòng nhập tên chương.');
            return;
        }

        apiClient.post(`/api/admin/curriculum/subjects/${SUBJECT_ID}/chapters`, data)
            .then(() => {
                closeModal('modal-stage-create-chapter');
                AdminUI.showNotice('success', 'Thành công', 'Đã thêm chương mới.');
                loadDetail();
            })
            .catch(err => AdminUI.showError(err));
    });

    // Save Order
    $('#btn-save-order').on('click', async () => {
        const btn = document.getElementById('btn-save-order');
        btn.disabled = true;
        btn.textContent = 'Đang lưu...';

        const items = [...listEl.querySelectorAll('.chapter-row:not(.row-deleted)')]
            .map((li, idx) => ({ 
                chapterId: parseInt(li.dataset.chapterId), 
                displayOrder: idx + 1 
            }));

        try {
            await apiClient.patch(`/api/admin/curriculum/subjects/${SUBJECT_ID}/chapters/order`, { items });
            AdminUI.showNotice('success', 'Thành công', 'Đã lưu thứ tự chương.');
            loadDetail();
        } catch (err) {
            AdminUI.showError(err);
            btn.disabled = false;
        } finally {
            btn.textContent = 'Lưu thứ tự';
        }
    });

    // Edit/Delete Chapter Delegation
    listEl.addEventListener('click', (e) => {
        const btnEdit = e.target.closest('.btn-edit-chapter');
        const btnDel = e.target.closest('.btn-delete-chapter');
        if (!btnEdit && !btnDel) return;

        const li = e.target.closest('.chapter-row');
        const chapterId = parseInt(li.dataset.chapterId);
        const chapter = subjectDetail.chapters.find(c => c.chapterId === chapterId);
        if (!chapter) return;

        if (btnEdit) {
            const form = document.getElementById('form-edit-chapter');
            form.reset();
            $(form).find('.help-error').text('');
            
            form.chapterId.value = chapter.chapterId;
            form.concurrencyStamp.value = chapter.concurrencyStamp;
            form.name.value = chapter.name;
            form.description.value = chapter.description || '';
            
            openModal('modal-stage-edit-chapter');
        } else if (btnDel) {
            deleteTargetId = chapterId;
            document.getElementById('delete-chapter-name').textContent = chapter.name;
            openModal('modal-stage-delete-chapter');
        }
    });

    $('#btn-submit-edit-chapter').on('click', () => {
        const form = document.getElementById('form-edit-chapter');
        const data = Object.fromEntries(new FormData(form));
        $(form).find('.help-error').text('');

        if (!data.name.trim()) {
            $(form.name).siblings('.help-error').text('Vui lòng nhập tên chương.');
            return;
        }

        apiClient.put(`/api/admin/curriculum/subjects/${SUBJECT_ID}/chapters/${data.chapterId}`, {
            name: data.name,
            description: data.description,
            concurrencyStamp: data.concurrencyStamp
        }).then(() => {
            closeModal('modal-stage-edit-chapter');
            AdminUI.showNotice('success', 'Thành công', 'Đã lưu chương.');
            loadDetail();
        }).catch(err => AdminUI.showError(err));
    });

    $('#btn-confirm-delete-chapter').on('click', () => {
        if (!deleteTargetId) return;
        
        apiClient.delete(`/api/admin/curriculum/subjects/${SUBJECT_ID}/chapters/${deleteTargetId}`)
            .then(() => {
                closeModal('modal-stage-delete-chapter');
                AdminUI.showNotice('success', 'Thành công', 'Đã xóa chương.');
                loadDetail();
            })
            .catch(err => AdminUI.showError(err));
    });

    // Initial load
    if (window.userReady) {
        window.userReady.then(() => loadDetail());
    } else {
        loadDetail();
    }
});
