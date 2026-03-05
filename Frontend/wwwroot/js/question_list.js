(() => {
    'use strict';

    const difficultyBadgeClass = { 1: 'badge-easy', 2: 'badge-medium', 3: 'badge-hard', 4: 'badge-hard' };
    const statusBadgeClass = { 'Active': 'status-active', 'Draft': 'status-draft', 'Archived': 'status-archived' };
    const typeLabels = { 'FillInBlank': 'Điền vào ô trống', 'MultipleChoice': 'Trắc nghiệm' };

    let currentPage = 1;
    const pageSize = 20;

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
            const subjects = await apiClient.get('/api/questions/subjects');
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
        try {
            const parsed = JSON.parse(q.contentPreview);
            return parsed.stem || '';
        } catch {
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
        <td><math-field read-only class="math-preview" data-latex="${encodeURIComponent(contentLatex)}"></math-field></td>
        <td>${typeLabel}</td>
        <td><span class="badge ${badgeDiff}">${q.difficultyLabel}</span></td>
        <td>${escapeHtml(q.subjectCode)}</td>
        <td>${escapeHtml(q.chapterName)}</td>
        <td>${dateStr}</td>
        <td><span class="badge ${badgeStatus}">${q.status}</span></td>
        <td><div class="toolbar"><button class="btn btn-sm btn-outline-secondary">Sửa</button><button class="btn btn-sm btn-outline-secondary">Lưu trữ</button></div></td>
      </tr>`;
        }).join('');

        // Set LaTeX values programmatically after DOM update to avoid HTML escaping issues
        tbody.querySelectorAll('math-field[data-latex]').forEach(mf => {
            const latex = decodeURIComponent(mf.getAttribute('data-latex'));
            mf.value = latex;
            mf.removeAttribute('data-latex');
        });

        // Pagination summary
        const start = (data.currentPage - 1) * data.pageSize + 1;
        const end = Math.min(data.currentPage * data.pageSize, data.totalCount);
        if (paginationSummary) {
            paginationSummary.textContent = `Hiển thị ${start}-${end} trên ${data.totalCount} câu hỏi.`;
        }

        // Pagination buttons
        renderPagination(data.currentPage, data.totalPages);
        bindCheckboxes();
    };

    // ── Pagination ──
    const renderPagination = (current, total) => {
        if (!paginationContainer || total <= 1) {
            if (paginationContainer) { paginationContainer.innerHTML = ''; }
            return;
        }

        let html = '';
        html += `<li class="page-item ${current <= 1 ? 'disabled' : ''}"><button class="page-link" data-page="${current - 1}">Trước</button></li>`;
        for (let i = 1; i <= total; i++) {
            html += `<li class="page-item ${i === current ? 'active' : ''}"><button class="page-link" data-page="${i}">${i}</button></li>`;
        }
        html += `<li class="page-item ${current >= total ? 'disabled' : ''}"><button class="page-link" data-page="${current + 1}">Sau</button></li>`;
        paginationContainer.innerHTML = html;

        paginationContainer.querySelectorAll('[data-page]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const page = parseInt(e.target.getAttribute('data-page'), 10);
                if (page >= 1 && page <= total) {
                    currentPage = page;
                    loadQuestions(page);
                }
            });
        });
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
