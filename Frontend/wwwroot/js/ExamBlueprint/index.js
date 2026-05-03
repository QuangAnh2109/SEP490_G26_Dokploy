$(document).ready(async function () {
    await window.userReady;

    const state = {
        page: 1,
        pageSize: 10,
        totalCount: 0
    };

    if (getUserRole() !== RoleIds.Teacher) {
        showPageError('Bạn không có quyền truy cập màn hình ma trận đề.');
        return;
    }

    const tbody = document.getElementById('blueprintTableBody');
    const paginationContainer = document.getElementById('blueprintPagination');
    const paginationSummary = document.getElementById('paginationSummary');
    const selectedCountText = document.getElementById('selectedCountText');
    const filterKeyword = document.getElementById('filterKeyword');
    const filterSubject = document.getElementById('filterSubject');
    const masterCheckbox = document.getElementById('blueprintMasterCheckbox');
    const bulkArchiveBtn = document.getElementById('bulkArchiveBtn');
    const applyFilterBtn = document.getElementById('applyFilterBtn');
    const clearFilterBtn = document.getElementById('clearFilterBtn');

    if (!tbody) return;

    bindEvents();
    renderNoticeFromQuery();
    loadSubjects().then(function () {
        loadList(1);
    });

    function appendTemplate(container, templateId) {
        const template = document.getElementById(templateId);
        if (template && template.content) {
            container.appendChild(template.content.cloneNode(true));
        }
    }

    function bindEvents() {
        if (applyFilterBtn) {
            applyFilterBtn.addEventListener('click', () => loadList(1));
        }
        if (clearFilterBtn) {
            clearFilterBtn.addEventListener('click', () => {
                if (filterKeyword) filterKeyword.value = '';
                if (filterSubject) filterSubject.value = '';
                loadList(1);
            });
        }

        if (bulkArchiveBtn) {
            bulkArchiveBtn.addEventListener('click', () => {
                const checked = tbody.querySelectorAll('.blueprint-item-checkbox:checked');
                const ids = Array.from(checked)
                    .filter(cb => cb.closest('.blueprint-row'))
                    .map(cb => parseInt(cb.closest('.blueprint-row').dataset.blueprintId, 10))
                    .filter(id => Number.isInteger(id) && id > 0);
                if (ids.length > 0) showArchiveConfirm(ids);
            });
        }

        if (masterCheckbox) {
            masterCheckbox.addEventListener('change', () => {
                tbody.querySelectorAll('.blueprint-item-checkbox').forEach(cb => {
                    if (cb.closest('.blueprint-row')) cb.checked = masterCheckbox.checked;
                });
                updateSelectedCount();
            });
        }

        tbody.addEventListener('change', (e) => {
            if (e.target.classList.contains('blueprint-item-checkbox')) {
                updateSelectedCount();
            }
        });

        tbody.addEventListener('click', (e) => {
            const expandBtn = e.target.closest('.expand-btn');
            if (expandBtn) {
                const row = expandBtn.closest('.blueprint-row');
                if (row) {
                    const id = parseInt(row.dataset.blueprintId, 10);
                    if (id) toggleExpand(row, id);
                }
            }

        });

        if (paginationContainer) {
            paginationContainer.addEventListener('click', (e) => {
                const a = e.target.closest('a[data-page]');
                if (a) {
                    e.preventDefault();
                    const page = parseInt(a.dataset.page, 10);
                    if (Number.isInteger(page)) loadList(page);
                }
            });
        }
    }

    function toggleExpand(row, blueprintId) {
        const nextRow = row.nextElementSibling;
        const isExpanded = nextRow && nextRow.classList.contains('blueprint-detail-row');

        if (isExpanded) {
            nextRow.remove();
            row.querySelector('.expand-btn i').className = 'bi bi-chevron-down';
            row.classList.remove('table-light');
            row.querySelectorAll('td').forEach(td => td.classList.remove('border-bottom-0'));
            return;
        }

        row.classList.add('table-light');
        row.querySelectorAll('td').forEach(td => td.classList.add('border-bottom-0'));
        row.querySelector('.expand-btn i').className = 'bi bi-chevron-up';

        const detailTemplate = document.getElementById('blueprintDetailRowTemplate');
        const detailRow = detailTemplate.content.cloneNode(true).firstElementChild;
        detailRow.dataset.blueprintId = blueprintId;
        detailRow.querySelector('.detail-title').textContent = 'Chi tiết ma trận đề (Đang tải...)';
        row.after(detailRow);

        apiClient.get(`/api/exam-blueprints/${blueprintId}`)
            .then(function (detail) {
                renderDetailRow(detailRow, detail);
            })
            .catch(function (err) {
                console.error(err);
                detailRow.querySelector('.detail-title').textContent = 'Không thể tải chi tiết.';
            });
    }

    function renderDetailRow(detailRowEl, detail) {
        const titleEl = detailRowEl.querySelector('.detail-title');
        const matrixBody = detailRowEl.querySelector('.detail-matrix-body');
        const totalEl = detailRowEl.querySelector('.detail-total');
        const editBtn = detailRowEl.querySelector('.btn-edit');
        const archiveBtn = detailRowEl.querySelector('.btn-archive');
        const deleteBtn = detailRowEl.querySelector('.btn-delete');

        const subjectCode = detail.subjectCode || detail.subjectName || '';
        titleEl.textContent = `Chi tiết ma trận đề${subjectCode ? ` (${subjectCode})` : ''}`;

        matrixBody.innerHTML = '';
        const rows = detail.rows || [];
        const matrixRowTemplate = document.getElementById('blueprintMatrixRowTemplate');

        if (rows.length === 0) {
            const tr = document.createElement('tr');
            tr.innerHTML = '<td colspan="3" class="py-2 px-3 text-center text-muted">Ma trận đề chưa có dòng nào.</td>';
            matrixBody.appendChild(tr);
        } else {
            rows.forEach(function (row) {
                const tr = matrixRowTemplate.content.cloneNode(true).firstElementChild;
                tr.querySelector('[data-field-chapter]').textContent = row.chapterName || '';
                tr.querySelector('[data-field-difficulty]').textContent = row.difficultyLabel || '';
                tr.querySelector('[data-field-questions]').textContent = row.totalQuestions ?? 0;
                matrixBody.appendChild(tr);
            });
        }

        totalEl.textContent = detail.totalQuestions ?? 0;

        if (editBtn) {
            editBtn.href = '/ExamBlueprint/Edit/' + detail.examBlueprintId;
            editBtn.title = 'Sửa ma trận đề';
        }
        if (archiveBtn) {
            if (detail.status === 3) {
                archiveBtn.classList.add('d-none');
            } else {
                archiveBtn.dataset.blueprintId = detail.examBlueprintId;
                archiveBtn.onclick = (e) => {
                    e.stopPropagation();
                    showArchiveConfirm([detail.examBlueprintId]);
                };
            }
        }
        if (deleteBtn) {
            if (detail.status === 2 || detail.status === 3) {
                deleteBtn.classList.add('d-none');
            } else {
                deleteBtn.dataset.blueprintId = detail.examBlueprintId;
                deleteBtn.onclick = (e) => {
                    e.stopPropagation();
                    showDeleteConfirm(detail.examBlueprintId);
                };
            }
        }
    }

    let pendingArchiveIds = [];
    const archiveModalEl = document.getElementById('archiveConfirmModal');
    const confirmArchiveBtn = document.getElementById('confirmArchiveBtn');
    let archiveModal = null;

    let pendingDeleteId = 0;
    const deleteModalEl = document.getElementById('deleteConfirmModal');
    const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
    let deleteModal = null;

    if (archiveModalEl) archiveModal = new bootstrap.Modal(archiveModalEl);
    if (deleteModalEl) deleteModal = new bootstrap.Modal(deleteModalEl);

    function showArchiveConfirm(ids) {
        pendingArchiveIds = ids || [];
        if (archiveModal) archiveModal.show();
    }

    function showDeleteConfirm(id) {
        pendingDeleteId = id;
        if (deleteModal) deleteModal.show();
    }

    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', async () => {
            if (!pendingDeleteId) return;
            confirmDeleteBtn.disabled = true;
            confirmDeleteBtn.textContent = 'Đang xử lý...';
            try {
                await apiClient.delete('/api/exam-blueprints/' + pendingDeleteId);
                if (deleteModal) deleteModal.hide();
                if (typeof showToast === 'function') showToast('Đã xóa ma trận thành công!');
                loadList(state.page);
            } catch (err) {
                const msg = err?.xhr?.responseJSON?.message || err?.message || 'Đã xảy ra lỗi.';
                if (typeof showToast === 'function') showToast(msg, 'error');
            } finally {
                confirmDeleteBtn.disabled = false;
                confirmDeleteBtn.textContent = 'Đồng ý xóa';
            }
        });
    }

    if (confirmArchiveBtn) {
        confirmArchiveBtn.addEventListener('click', async () => {
            if (pendingArchiveIds.length === 0) return;
            confirmArchiveBtn.disabled = true;
            confirmArchiveBtn.textContent = 'Đang xử lý...';
            try {
                await apiClient.patch('/api/exam-blueprints/status', {
                    examBlueprintIds: pendingArchiveIds,
                    status: 3
                });
                if (archiveModal) archiveModal.hide();
                if (typeof showToast === 'function') showToast('Đã lưu trữ thành công!');
                loadList(state.page);
            } catch (err) {
                const msg = err?.xhr?.responseJSON?.message || err?.message || 'Đã xảy ra lỗi.';
                if (typeof showToast === 'function') showToast(msg, 'error');
            } finally {
                confirmArchiveBtn.disabled = false;
                confirmArchiveBtn.textContent = 'Đồng ý lưu trữ';
            }
        });
    }

    function renderNoticeFromQuery() {
        const params = new URLSearchParams(window.location.search);
        const flashSuccess = sessionStorage.getItem('examBlueprintFlashSuccess');
        const flashWarningsRaw = sessionStorage.getItem('examBlueprintFlashWarnings');

        if (params.get('created') === '1' || params.get('updated') === '1') {
            const notice = document.getElementById('pageNotice');
            if (notice) {
                notice.textContent = flashSuccess || (params.get('updated') === '1' ? 'Cập nhật ma trận đề thành công.' : 'Tạo ma trận đề thành công.');
                notice.classList.remove('d-none');
            }
        }

        if (flashWarningsRaw) {
            try {
                const warnings = JSON.parse(flashWarningsRaw);
                if (Array.isArray(warnings) && warnings.length > 0) {
                    const container = document.getElementById('flashWarningContainer');
                    const list = document.getElementById('flashWarningList');
                    const t = document.getElementById('flashWarningItemTemplate');
                    if (container && list && t) {
                        list.innerHTML = '';
                        warnings.forEach(w => {
                            const li = t.content.cloneNode(true).firstElementChild;
                            li.textContent = w;
                            list.appendChild(li);
                        });
                        container.classList.remove('d-none');
                    }
                }
            } catch (e) {
                console.warn('Cannot parse flash warnings', e);
            }
        }

        sessionStorage.removeItem('examBlueprintFlashSuccess');
        sessionStorage.removeItem('examBlueprintFlashWarnings');
    }

    function loadSubjects() {
        return apiClient.get('/api/exam-blueprints/subjects')
            .then(function (subjects) {
                if (!filterSubject) return;
                filterSubject.innerHTML = '<option value="">Tất cả môn học</option>';
                (subjects || []).forEach(function (subject) {
                    const text = subject.code ? `${subject.code} - ${subject.name}` : subject.name;
                    filterSubject.appendChild(new Option(text, subject.subjectId));
                });
            })
            .catch(function (error) {
                console.error(error);
                showPageError(resolveApiError(error));
            });
    }

    function loadList(page) {
        state.page = page;
        const pageError = document.getElementById('pageError');
        if (pageError) {
            pageError.classList.add('d-none');
            pageError.textContent = '';
        }

        const params = new URLSearchParams();
        params.set('page', String(page));
        params.set('pageSize', String(state.pageSize));

        const keyword = (filterKeyword?.value || '').toString().trim();
        const subjectId = (filterSubject?.value || '').toString();
        if (keyword) params.set('keyword', keyword);
        if (subjectId) params.set('subjectId', subjectId);

        tbody.innerHTML = '';
        appendTemplate(tbody, 'blueprintLoadingTemplate');

        apiClient.get(`/api/exam-blueprints?${params.toString()}`)
            .then(function (response) {
                const items = response.items || [];
                state.totalCount = response.totalItems ?? response.totalCount ?? 0;

                renderTable(items);
                renderPagination(response.page || 1, response.totalPages ?? 0, state.totalCount);
            })
            .catch(function (error) {
                console.error(error);
                tbody.innerHTML = '';
                appendTemplate(tbody, 'blueprintErrorTemplate');
                renderPagination(1, 0, state.totalCount);
                showPageError(resolveApiError(error));
            });
    }

    function renderTable(items) {
        tbody.innerHTML = '';

        if (!items.length) {
            appendTemplate(tbody, 'blueprintEmptyTemplate');
            return;
        }

        const rowTemplate = document.getElementById('blueprintRowTemplate');
        if (!rowTemplate) return;

        items.forEach(function (item) {
            const row = rowTemplate.content.cloneNode(true).firstElementChild;
            const subjectText = item.subjectCode || item.subjectName || '';

            row.dataset.blueprintId = item.examBlueprintId;

            row.querySelector('[data-field-name]').textContent = item.name || '';
            row.querySelector('[data-field-questions]').textContent = item.totalQuestions ?? 0;
            row.querySelector('[data-field-subject]').textContent = subjectText;
            row.querySelector('[data-field-updated]').textContent = formatDate(item.updatedAtUtc);

            const badge = row.querySelector('[data-field-status]');
            if (badge) {
                badge.className = 'badge rounded-1 py-2 px-2 fw-medium ' + getStatusClass(item.status);
                badge.textContent = item.statusLabel || '';
            }

            tbody.appendChild(row);
        });
    }

    function renderPagination(page, totalPages, totalCount) {
        if (!paginationContainer) return;
        paginationContainer.innerHTML = '';

        if (paginationSummary) {
            const start = totalCount === 0 ? 0 : (page - 1) * state.pageSize + 1;
            const end = Math.min(page * state.pageSize, totalCount);
            paginationSummary.textContent = `Hiển thị ${start}-${end} trên ${totalCount} ma trận`;
        }

        if (!totalPages || totalPages <= 1) return;

        const addPageItem = (content, disabled, pageNum) => {
            const li = document.createElement('li');
            li.className = 'page-item' + (disabled ? ' disabled' : '');
            const a = document.createElement('a');
            a.className = 'page-link text-secondary border rounded-1 px-3 me-1';
            if (disabled) {
                a.href = '#';
                a.tabIndex = -1;
                a.setAttribute('aria-disabled', 'true');
            } else {
                a.href = '#';
                a.dataset.page = pageNum;
            }
            a.innerHTML = content;
            li.appendChild(a);
            paginationContainer.appendChild(li);
        };

        addPageItem('<i class="bi bi-chevron-left"></i>', page <= 1, page - 1);

        for (let i = 1; i <= totalPages; i++) {
            const li = document.createElement('li');
            li.className = 'page-item' + (i === page ? ' active' : '');
            const a = document.createElement('a');
            a.className = 'page-link border rounded-1 px-3 me-1' + (i === page ? '' : ' text-secondary');
            a.href = '#';
            a.dataset.page = i;
            a.textContent = i;
            li.appendChild(a);
            paginationContainer.appendChild(li);
        }

        addPageItem('<i class="bi bi-chevron-right"></i>', page >= totalPages, page + 1);
    }

    function updateSelectedCount() {
        const checked = tbody.querySelectorAll('.blueprint-item-checkbox:checked');
        const n = Array.from(checked).filter(cb => cb.closest('.blueprint-row')).length;
        if (selectedCountText) {
            selectedCountText.textContent = `Đã chọn ${n} ma trận`;
        }
        if (bulkArchiveBtn) {
            bulkArchiveBtn.disabled = n === 0;
        }
        if (masterCheckbox) {
            const allCheckboxes = tbody.querySelectorAll('.blueprint-item-checkbox');
            const dataRows = Array.from(allCheckboxes).filter(cb => cb.closest('.blueprint-row'));
            masterCheckbox.checked = dataRows.length > 0 && dataRows.every(cb => cb.checked);
        }
    }

    function getStatusClass(status) {
        switch (status) {
            case 0: return 'bg-secondary-subtle text-secondary-emphasis';
            case 1: return 'bg-success-subtle text-success-emphasis';
            case 2: return 'bg-primary-subtle text-primary-emphasis';
            case 3: return 'bg-warning-subtle text-warning-emphasis';
            default: return 'bg-secondary-subtle text-secondary-emphasis';
        }
    }


    function showPageError(message) {
        const el = document.getElementById('pageError');
        if (el) {
            el.classList.remove('d-none', 'alert-warning');
            el.classList.add('alert-danger');
            el.textContent = message || 'Đã xảy ra lỗi.';
        }
    }

    function resolveApiError(error) {
        return error?.xhr?.responseJSON?.message || error?.message || 'Đã có lỗi xảy ra từ máy chủ.';
    }
});
