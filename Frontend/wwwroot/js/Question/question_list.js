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
                subjects.forEach(s => filterSubject.add(new Option(s.code || s.name, s.subjectId)));
            }
            if (filterSubject && filterChapter) {
                filterSubject.addEventListener('change', () => {
                    const subId = parseInt(filterSubject.value, 10);
                    while (filterChapter.firstChild) filterChapter.removeChild(filterChapter.firstChild);
                    filterChapter.add(new Option('Tất cả chương', ''));
                    if (!subId) { return; }
                    const sub = subjects.find(s => s.subjectId === subId);
                    if (sub && sub.chapters) {
                        sub.chapters.forEach(c => filterChapter.add(new Option(c.name, c.chapterId)));
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
        while (tbody.firstChild) tbody.removeChild(tbody.firstChild);
        if (items.length === 0) {
            const t = document.getElementById('tableEmptyTemplate');
            tbody.appendChild(t.content.cloneNode(true));
            if (paginationSummary) { paginationSummary.textContent = ''; }
            if (paginationContainer) { paginationContainer.innerHTML = ''; }
            return;
        }

        const template = document.getElementById('questionRowTemplate');
        if (!template) {
            console.error('Template questionRowTemplate not found!');
            return;
        }
        items.forEach(q => {
            const row = template.content.cloneNode(true).firstElementChild;
            const badgeDiff = difficultyBadgeClass[q.difficulty] || 'badge-easy';
            const badgeStatus = statusBadgeClass[q.status] || 'status-draft';
            const typeLabel = typeLabels[q.questionType] || q.questionType;
            const dateStr = new Date(q.updatedAt).toLocaleDateString('vi-VN');
            const contentLatex = getContentPreview(q);

            row.querySelector('.question-item-checkbox').setAttribute('aria-label', `Chọn câu hỏi Q-${q.questionId}`);
            row.querySelector('[data-field-id]').textContent = `Q-${q.questionId}`;
            const mf = row.querySelector('[data-field-content]');
            if (mf) { mf.setAttribute('value', contentLatex); mf.textContent = contentLatex; }
            row.querySelector('[data-field-type]').textContent = typeLabel;
            
            const diffSpan = row.querySelector('[data-field-difficulty]');
            diffSpan.className = `badge ${badgeDiff}`; diffSpan.textContent = q.difficultyLabel;
            
            row.querySelector('[data-field-subject]').textContent = q.subjectCode || '';
            row.querySelector('[data-field-chapter]').textContent = q.chapterName || '';
            row.querySelector('[data-field-updated]').textContent = dateStr;
            
            const statusSpan = row.querySelector('[data-field-status]');
            statusSpan.className = `badge ${badgeStatus}`; statusSpan.textContent = q.status;

            const editBtn = row.querySelector('[data-action-edit]');
            editBtn.setAttribute('data-id', q.questionId);
            const archiveBtn = row.querySelector('[data-action-archive]');
            archiveBtn.setAttribute('data-id', q.questionId);

            tbody.appendChild(row);
        });

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
            if (paginationContainer) {
                while (paginationContainer.firstChild) paginationContainer.removeChild(paginationContainer.firstChild);
            }
            return;
        }

        while (paginationContainer.firstChild) paginationContainer.removeChild(paginationContainer.firstChild);
        const btnTemplate = document.getElementById('paginationButtonTemplate');
        const prevTemplate = document.getElementById('paginationPrevTemplate');
        const nextTemplate = document.getElementById('paginationNextTemplate');
        const ellipsisTemplate = document.getElementById('paginationEllipsisTemplate');

        const addPageBtn = (p, label, active = false, disabled = false) => {
            let li;
            if (label === '«') li = prevTemplate.content.cloneNode(true).firstElementChild;
            else if (label === '»') li = nextTemplate.content.cloneNode(true).firstElementChild;
            else {
                li = btnTemplate.content.cloneNode(true).firstElementChild;
                li.querySelector('.page-link').textContent = label;
            }
            
            if (active) li.classList.add('active');
            if (disabled) li.classList.add('disabled');
            li.querySelector('.page-link').setAttribute('data-page', p);
            paginationContainer.appendChild(li);
        };

        const addEllipsis = () => {
            const li = ellipsisTemplate.content.cloneNode(true).firstElementChild;
            paginationContainer.appendChild(li);
        };

        // Previous button
        addPageBtn(current - 1, '«', false, current <= 1);

        const delta = 2;
        const range = [];
        for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
            range.push(i);
        }

        if (range[0] > 1) {
            addPageBtn(1, '1');
            if (range[0] > 2) addEllipsis();
        }

        range.forEach(i => {
            addPageBtn(i, String(i), i === current);
        });

        if (range[range.length - 1] < total) {
            if (range[range.length - 1] < total - 1) addEllipsis();
            addPageBtn(total, String(total));
        }

        // Next button
        addPageBtn(current + 1, '»', false, current >= total);

        // Single event listener for pagination container (event delegation)
        if (!paginationContainer._bound) {
            paginationContainer._bound = true;
            paginationContainer.addEventListener('click', (e) => {
                const btn = e.target.closest('[data-page]');
                const li = btn?.closest('.page-item');
                if (!btn || li?.classList.contains('disabled') || li?.classList.contains('active')) return;
                const page = parseInt(btn.getAttribute('data-page'), 10);
                if (page >= 1 && page <= total) {
                    currentPage = page;
                    loadQuestions(page);
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
            });
        }
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
        while (tbody.firstChild) tbody.removeChild(tbody.firstChild);
        const tLoad = document.getElementById('tableLoadingTemplate');
        tbody.appendChild(tLoad.content.cloneNode(true));
        try {
            const qs = buildQuery(page);
            const data = await apiClient.get(`/api/questions?${qs}`);
            console.log('Successfully loaded questions:', data);
            renderTable(data);
        } catch (err) {
            console.error('Failed to load questions', err);
            while (tbody.firstChild) tbody.removeChild(tbody.firstChild);
            const tErr = document.getElementById('tableErrorTemplate');
            tbody.appendChild(tErr.content.cloneNode(true));
        }
    };

    if (applyFilterBtn) {
        applyFilterBtn.addEventListener('click', () => {
            currentPage = 1;
            loadQuestions(1);
        });
    }


    // ── Escape HTML (DOM Pure) ──
    const escapeHtml = (str) => {
        if (!str) { return ''; }
        const textNode = document.createTextNode(str);
        const div = document.createElement('div');
        div.appendChild(textNode);
        return div.innerHTML;
    };

    // ── Init ──
    loadSubjects();
    loadQuestions(1);
})();
