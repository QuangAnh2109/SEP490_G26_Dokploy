(() => {
    'use strict';

    const difficultyBadgeClass = { 1: 'badge-easy', 2: 'badge-medium', 3: 'badge-hard', 4: 'badge-hard' };
    const statusBadgeClass = { 'Active': 'status-active', 'Draft': 'status-draft', 'Archived': 'status-archived' };
    const typeLabels = { 'FillInBlank': 'Điền vào ô trống', 'MultipleChoice': 'Trắc nghiệm' };

    let currentPage = 1;
    const pageSize = 10;

    const tbody = document.getElementById('questionTableBody');
    const paginationContainer = document.getElementById('paginationContainer');
    const paginationSummary = document.getElementById('paginationSummary');
    const applyFilterBtn = document.getElementById('applyFilterBtn');
    const selectAllBtn = document.getElementById('selectAllQuestionsBtn');
    const clearAllBtn = document.getElementById('clearAllQuestionsBtn');
    const masterCheckbox = document.getElementById('questionMasterCheckbox');
    const filterSubject = document.getElementById('filterSubject');
    const filterChapter = document.getElementById('filterChapter');

    if (!tbody) { return; }

    // ── Load subjects/chapters for filter dropdowns ──
    const loadSubjects = async () => {
        try {
            const metadata = await apiClient.get('/api/questions/metadata');
            const subjects = metadata.subjects || [];
            if (filterSubject) {
                subjects.forEach(s => {
                    const opt = document.createElement('option');
                    opt.value = s.subjectId;
                    opt.textContent = s.code || s.name;
                    filterSubject.appendChild(opt);
                });
            }
            if (filterSubject && filterChapter) {
                filterSubject.addEventListener('change', () => {
                    const subId = parseInt(filterSubject.value, 10);
                    filterChapter.innerHTML = '<option value="">Tất cả chương</option>';
                    if (!subId) { return; }
                    const sub = subjects.find(s => s.subjectId === subId);
                    if (sub && sub.chapters) {
                        sub.chapters.forEach(c => {
                            const opt = document.createElement('option');
                            opt.value = c.chapterId;
                            opt.textContent = c.name;
                            filterChapter.appendChild(opt);
                        });
                    }
                });
            }
        } catch (err) {
            console.error('Failed to load subjects', err);
        }
    };

    // ── Build query string ──
    const buildQuery = (page) => {
        const params = new URLSearchParams();
        params.set('page', page);
        params.set('pageSize', pageSize);

        const keyword = document.getElementById('filterKeyword')?.value?.trim();
        if (keyword) { params.set('keyword', keyword); }

        const qType = document.getElementById('filterQuestionType')?.value;
        if (qType) { params.set('questionType', qType); }

        const diff = document.getElementById('filterDifficulty')?.value;
        if (diff) { params.set('difficulty', diff); }

        const chapter = filterChapter?.value;
        if (chapter) { params.set('chapterId', chapter); }

        const subject = filterSubject?.value;
        if (subject) { params.set('subjectId', subject); }

        const status = document.getElementById('filterStatus')?.value;
        if (status) { params.set('status', status); }

        return params.toString();
    };

    // ── Extract stem for display ──
    const getContentPreview = (q) => {
        if (!q.contentPreview) return '';
        try {
            const parsed = JSON.parse(q.contentPreview);
            // Handle both lowercase 'stem' and PascalCase 'Stem'
            return parsed.stem || parsed.Stem || '';
        } catch {
            // Fallback for non-JSON content
            return q.contentPreview || '';
        }
    };

    // ── Render table ──
    const renderTable = (data) => {
        const items = data.items || [];
        if (items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="10" class="text-center text-muted py-4">Không tìm thấy câu hỏi nào.</td></tr>';
            if (paginationSummary) { paginationSummary.textContent = ''; }
            if (paginationContainer) { paginationContainer.innerHTML = ''; }
            return;
        }

        tbody.innerHTML = items.map(q => {
            const badgeDiff = difficultyBadgeClass[q.difficulty] || 'badge-easy';
            const badgeStatus = statusBadgeClass[q.status] || 'status-draft';
            const typeLabel = typeLabels[q.questionType] || q.questionType;
            const dateStr = new Date(q.updatedAt).toLocaleDateString('vi-VN');
            const contentLatex = getContentPreview(q);
            
            return `<tr class="interactive-row">
        <td><input class="form-check-input question-item-checkbox" type="checkbox" aria-label="Chọn câu hỏi Q-${q.questionId}"></td>
        <td>Q-${q.questionId}</td>
        <td>
            <math-field read-only class="math-preview">${contentLatex}</math-field>
        </td>
        <td>${typeLabel}</td>
        <td><span class="badge ${badgeDiff}">${q.difficultyLabel}</span></td>
        <td>${escapeHtml(q.subjectCode)}</td>
        <td>${escapeHtml(q.chapterName)}</td>
        <td>${dateStr}</td>
        <td><span class="badge ${badgeStatus}">${q.status}</span></td>
        <td>
            <div class="toolbar">
                <button class="btn btn-sm btn-outline-secondary" data-action-edit data-id="${q.questionId}">Sửa</button>
                <button class="btn btn-sm btn-outline-danger" data-action-archive data-id="${q.questionId}">Lưu trữ</button>
            </div>
        </td>
      </tr>`;
        }).join('');

        // Pagination summary
        const start = (data.currentPage - 1) * pageSize + 1;
        const end = start + items.length - 1;
        if (paginationSummary) {
            paginationSummary.textContent = `Hiển thị ${start}-${end} trên ${data.totalCount} câu hỏi.`;
        }

        // Pagination buttons
        renderPagination(data.currentPage, data.totalPages);
        bindCheckboxes();
        bindRowActions();
    };

    const bulkArchiveBtn = document.getElementById('bulkArchiveBtn');
    const archiveConfirmModal = document.getElementById('archiveConfirmModal');
    const confirmArchiveBtn = document.getElementById('confirmArchiveBtn');
    let archiveModal = null;
    let pendingArchiveIds = [];

    if (archiveConfirmModal) {
        archiveModal = new bootstrap.Modal(archiveConfirmModal);
    }

    // ── Row Actions (Edit / Archive) ──
    const bindRowActions = () => {
        const editBtns = tbody.querySelectorAll('[data-action-edit]');
        const archiveBtns = tbody.querySelectorAll('[data-action-archive]');
        
        editBtns.forEach(btn => {
            btn.addEventListener('click', (e) => {
                const qId = e.currentTarget.getAttribute('data-id');
                if(qId) {
                    window.location.href = `/Question/Edit/${qId}`;
                }
            });
        });

        archiveBtns.forEach(btn => {
            btn.addEventListener('click', (e) => {
                const qId = e.currentTarget.getAttribute('data-id');
                if(!qId) return;
                
                pendingArchiveIds = [parseInt(qId, 10)];
                if (archiveModal) archiveModal.show();
            });
        });
    };

    if (bulkArchiveBtn) {
        bulkArchiveBtn.addEventListener('click', () => {
            const checkedCbs = Array.from(tbody.querySelectorAll('.question-item-checkbox:checked'));
            const ids = checkedCbs.map(cb => {
                const row = cb.closest('tr');
                const archiveBtn = row?.querySelector('[data-action-archive]');
                return archiveBtn ? parseInt(archiveBtn.getAttribute('data-id'), 10) : null;
            }).filter(id => id !== null);

            if (ids.length === 0) {
                showToast('Vui lòng chọn ít nhất một câu hỏi để lưu trữ.', 'info');
                return;
            }

            pendingArchiveIds = ids;
            if (archiveModal) archiveModal.show();
        });
    }

    if (confirmArchiveBtn) {
        confirmArchiveBtn.addEventListener('click', async () => {
            if (pendingArchiveIds.length === 0) return;

            confirmArchiveBtn.disabled = true;
            confirmArchiveBtn.textContent = 'Đang xử lý...';

            try {
                const response = await apiClient.patch('/api/questions/status', {
                    questionIds: pendingArchiveIds,
                    status: 'Archived'
                });
                
                if (archiveModal) archiveModal.hide();
                showToast(response.message || 'Đã lưu trữ thành công!');
                loadQuestions(currentPage);
            } catch (err) {
                console.error('Lỗi khi lưu trữ hàng loạt', err);
                showToast('Đã xảy ra lỗi khi lưu trữ câu hỏi.', 'error');
            } finally {
                confirmArchiveBtn.disabled = false;
                confirmArchiveBtn.textContent = 'Đồng ý lưu trữ';
            }
        });
    }

    // ── Pagination ──
    const renderPagination = (current, total) => {
        if (!paginationContainer || total <= 0) {
            if (paginationContainer) { paginationContainer.innerHTML = ''; }
            return;
        }

        let html = '';
        // Previous button
        html += `<li class="page-item ${current <= 1 ? 'disabled' : ''}">
                    <button class="page-link" data-page="${current - 1}" aria-label="Trang trước">
                        <span aria-hidden="true">&laquo;</span>
                    </button>
                 </li>`;

        const delta = 2; // Number of pages to show around current page
        const range = [];
        for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
            range.push(i);
        }

        if (range[0] > 1) {
            html += `<li class="page-item"><button class="page-link" data-page="1">1</button></li>`;
            if (range[0] > 2) {
                html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            }
        }

        range.forEach(i => {
            html += `<li class="page-item ${i === current ? 'active' : ''}"><button class="page-link" data-page="${i}">${i}</button></li>`;
        });

        if (range[range.length - 1] < total) {
            if (range[range.length - 1] < total - 1) {
                html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            }
            html += `<li class="page-item"><button class="page-link" data-page="${total}">${total}</button></li>`;
        }

        // Next button
        html += `<li class="page-item ${current >= total ? 'disabled' : ''}">
                    <button class="page-link" data-page="${current + 1}" aria-label="Trang sau">
                        <span aria-hidden="true">&raquo;</span>
                    </button>
                 </li>`;

        paginationContainer.innerHTML = html;

        paginationContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-page]');
            if (!btn) return;
            const page = parseInt(btn.getAttribute('data-page'), 10);
            if (page >= 1 && page <= total && page !== current) {
                currentPage = page;
                loadQuestions(page);
                window.scrollTo({ top: 0, behavior: 'smooth' });
            }
        }, { once: true }); // Use once to avoid multiple bindings, or better yet, refactor to single binding
    };

    // ── Checkbox logic ──
    const bindCheckboxes = () => {
        const itemCbs = Array.from(document.querySelectorAll('.question-item-checkbox'));
        if (masterCheckbox) {
            masterCheckbox.checked = false;
            masterCheckbox.addEventListener('change', () => {
                itemCbs.forEach(cb => { cb.checked = masterCheckbox.checked; });
            });
        }
        itemCbs.forEach(cb => {
            cb.addEventListener('change', () => {
                if (masterCheckbox) {
                    masterCheckbox.checked = itemCbs.every(c => c.checked);
                }
            });
        });
    };

    if (selectAllBtn) {
        selectAllBtn.addEventListener('click', () => {
            document.querySelectorAll('.question-item-checkbox').forEach(cb => { cb.checked = true; });
            if (masterCheckbox) { masterCheckbox.checked = true; }
        });
    }
    if (clearAllBtn) {
        clearAllBtn.addEventListener('click', () => {
            document.querySelectorAll('.question-item-checkbox').forEach(cb => { cb.checked = false; });
            if (masterCheckbox) { masterCheckbox.checked = false; }
        });
    }

    // ── Load data ──
    const loadQuestions = async (page) => {
        tbody.innerHTML = '<tr><td colspan="10" class="text-center text-muted py-4">Đang tải dữ liệu...</td></tr>';
        try {
            const qs = buildQuery(page);
            const data = await apiClient.get(`/api/questions?${qs}`);
            renderTable(data);
        } catch (err) {
            console.error('Failed to load questions', err);
            tbody.innerHTML = '<tr><td colspan="10" class="text-center text-danger py-4">Lỗi khi tải dữ liệu. Vui lòng thử lại.</td></tr>';
        }
    };

    if (applyFilterBtn) {
        applyFilterBtn.addEventListener('click', () => {
            currentPage = 1;
            loadQuestions(1);
        });
    }


    // ── Escape HTML ──
    const escapeHtml = (str) => {
        if (!str) { return ''; }
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    };

    // ── Init ──
    loadSubjects();
    loadQuestions(1);
})();
