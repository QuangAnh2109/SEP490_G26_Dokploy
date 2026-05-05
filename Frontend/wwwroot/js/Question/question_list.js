(() => {
    'use strict';

    const typeLabels = {
        'FillInBlank': 'Điền ô trống',
        'MultipleChoice': 'Trắc nghiệm'
    };

    const statusLabels = {
        'Active': 'Active',
        'Draft': 'Draft',
        'Archived': 'Archived',
        'Inprogress': 'Inprogress'
    };

    const statusBadgeClass = {
        'Active': 'badge-status-active',
        'Draft': 'badge-status-draft',
        'Archived': 'badge-status-archived'
    };

    const purposeLabels = {
        1: 'Kiểm tra',
        2: 'Luyện tập'
    };

    const purposeBadgeClass = {
        1: 'bg-info text-dark',
        2: 'bg-warning text-dark'
    };

    const difficultyBadgeClass = {
        1: 'badge-diff-1',
        2: 'badge-diff-2',
        3: 'badge-diff-3',
        4: 'badge-diff-4'
    };

    let currentPage = 1;
    const pageSize = 10;

    const tbody = document.getElementById('questionTableBody');
    const paginationContainer = document.getElementById('paginationContainer');
    const paginationSummary = document.getElementById('paginationSummary');
    const selectedCountText = document.getElementById('selectedCountText');
    const filterSubject = document.getElementById('filterSubject');
    const filterChapter = document.getElementById('filterChapter');
    const masterCheckbox = document.getElementById('questionMasterCheckbox');

    if (!tbody) return;

    const getContentPreview = (q) => {
        if (!q.contentPreview) return '';
        try {
            const parsed = JSON.parse(q.contentPreview);
            return parsed.stem || parsed.Stem || '';
        } catch {
            return q.contentPreview || '';
        }
    };

    const UTILS = window.QuestionEditorUtils;

    const setLatexContent = (element, latex, { inline = false } = {}) => {
        if (!element) return;
        if (!latex || !String(latex).trim()) {
            element.textContent = '-';
            return;
        }
        const text = String(latex).trim();
        if (inline && window.katex) {
            try {
                katex.render(text, element, { throwOnError: false, displayMode: false, trust: true, strict: false });
            } catch (e) {
                element.textContent = text;
            }
        } else if (UTILS && UTILS.renderLatexInElement) {
            UTILS.renderLatexInElement(element, text);
        } else {
            element.textContent = text;
        }
    };

    const loadSubjects = async () => {
        try {
            const metadata = await apiClient.get('/api/questions/metadata');
            const subjects = metadata.subjects || [];
            if (filterSubject) {
                filterSubject.innerHTML = '<option value="">Tất cả</option>';
                subjects.forEach(s => {
                    filterSubject.add(new Option(s.code || s.name, s.subjectId));
                });
            }
            if (filterSubject && filterChapter) {
                filterSubject.addEventListener('change', () => {
                    const subId = parseInt(filterSubject.value, 10);
                    filterChapter.innerHTML = '<option value="">Tất cả</option>';
                    if (!subId) return;
                    const sub = subjects.find(s => s.subjectId === subId);
                    if (sub && sub.chapters) {
                        sub.chapters.forEach(c => {
                            filterChapter.add(new Option(c.name, c.chapterId));
                        });
                    }
                });
            }
        } catch (err) {
            console.error('Failed to load subjects', err);
        }
    };

    const buildQuery = (page) => {
        const params = new URLSearchParams();
        params.set('page', page);
        params.set('pageSize', pageSize);

        const keyword = document.getElementById('filterKeyword')?.value?.trim();
        if (keyword) params.set('keyword', keyword);

        const qType = document.getElementById('filterQuestionType')?.value;
        if (qType) params.set('questionType', qType);

        const diff = document.getElementById('filterDifficulty')?.value;
        if (diff) params.set('difficulty', diff);

        if (filterChapter?.value) params.set('chapterId', filterChapter.value);
        if (filterSubject?.value) params.set('subjectId', filterSubject.value);

        const status = document.getElementById('filterStatus')?.value;
        if (status) params.set('status', status);

        const purpose = document.getElementById('filterPurpose')?.value;
        if (purpose) params.set('questionPurpose', purpose);

        return params.toString();
    };

    const updateSelectedCount = () => {
        const checked = tbody.querySelectorAll('.question-item-checkbox:checked');
        const n = checked.length;
        if (selectedCountText) {
            selectedCountText.textContent = `Đã chọn ${n} câu hỏi`;
        }
    };

    const bindCheckboxes = () => {
        const itemCbs = tbody.querySelectorAll('.question-item-checkbox');
        if (masterCheckbox) {
            masterCheckbox.checked = false;
            masterCheckbox.onchange = () => {
                itemCbs.forEach(cb => { cb.checked = masterCheckbox.checked; });
                updateSelectedCount();
            };
        }
        itemCbs.forEach(cb => {
            cb.onchange = () => {
                if (masterCheckbox) {
                    masterCheckbox.checked = itemCbs.length > 0 && Array.from(itemCbs).every(c => c.checked);
                }
                updateSelectedCount();
            };
        });
    };

    const fetchQuestionDetail = async (questionId) => {
        const data = await apiClient.get(`/api/questions/${questionId}`);
        return data;
    };

    const renderDetailRow = (detailRow, qDetail) => {
        const stemEl = detailRow.querySelector('.detail-stem');
        const answersEl = detailRow.querySelector('.detail-answers');
        const explanationEl = detailRow.querySelector('.detail-explanation');
        const explanationWrap = detailRow.querySelector('.detail-explanation-wrap');
        const editBtn = detailRow.querySelector('.btn-edit');
        const archiveBtn = detailRow.querySelector('.btn-archive');

        if (stemEl) setLatexContent(stemEl, qDetail.stem || '');

        if (answersEl) {
            answersEl.innerHTML = '';
            const answers = qDetail.answers || [];
            const letters = 'ABCDEFGHIJ';
            const isFillBlank = qDetail.questionType === 'FillInBlank';
            answers.forEach((a, idx) => {
                const div = document.createElement('div');
                div.className = 'answer-item' + (a.isCorrect ? ' correct' : '');
                const displayVal = isFillBlank ? (a.correctAnswer || '') : (a.content || a.correctAnswer || '');
                const span = document.createElement('span');
                span.className = 'answer-content';
                if (isFillBlank) {
                    const labelSpan = document.createElement('span');
                    labelSpan.className = 'answer-label me-2';
                    labelSpan.textContent = `Ô trống ${idx + 1}: `;
                    div.appendChild(labelSpan);
                } else {
                    const prefixSpan = document.createElement('span');
                    prefixSpan.className = 'answer-prefix me-2';
                    prefixSpan.textContent = letters[idx] + '. ';
                    div.appendChild(prefixSpan);
                }
                div.appendChild(span);
                if (a.isCorrect) {
                    const icon = document.createElement('i');
                    icon.className = 'bi bi-check-circle-fill text-success ms-2';
                    div.appendChild(icon);
                }
                answersEl.appendChild(div);
                setLatexContent(span, displayVal || '(Đáp án trống)', { inline: true });
            });
        }

        if (explanationEl) {
            setLatexContent(explanationEl, qDetail.explanation || 'Không có giải thích.');
        }
        if (explanationWrap) {
            explanationWrap.style.display = qDetail.explanation ? 'flex' : 'none';
        }

        if (window.renderMathInElement) {
            renderMathInElement(detailRow, {
                delimiters: [
                    { left: '$$', right: '$$', display: true },
                    { left: '$', right: '$', display: false },
                    { left: '\\(', right: '\\)', display: false },
                    { left: '\\[', right: '\\]', display: true }
                ],
                trust: true, strict: false
            });
        }

        if (editBtn) {
            editBtn.onclick = () => { window.location.href = `/Question/Edit/${detailRow.dataset.questionId}`; };
        }
        
        const deleteBtn = detailRow.querySelector('.btn-delete');
        if (deleteBtn) {
            // Only Draft or Active statuses should be deletable based on UI, but we can verify in API.
            // We can also disable it here if we want, but let's let backend or modal trigger.
            if (qDetail.status !== 'Draft' && qDetail.status !== 'Active') {
                deleteBtn.style.display = 'none';
            } else {
                deleteBtn.onclick = () => {
                    const id = parseInt(detailRow.dataset.questionId, 10);
                    if (id) showDeleteConfirm(id);
                };
            }
        }

        if (archiveBtn) {
            if (qDetail.status !== 'Inprogress') {
                archiveBtn.style.display = 'none';
            } else {
                archiveBtn.onclick = () => {
                    const id = parseInt(detailRow.dataset.questionId, 10);
                    if (id) showArchiveConfirm([id]);
                };
            }
        }
    };

    const toggleExpand = async (row, questionId) => {
        const nextRow = row.nextElementSibling;
        const isExpanded = nextRow && nextRow.classList.contains('question-detail-row');

        if (isExpanded) {
            nextRow.remove();
            row.querySelector('.expand-btn i').className = 'bi bi-chevron-down';
            return;
        }

        const btn = row.querySelector('.expand-btn i');
        btn.className = 'bi bi-chevron-up';

        const detailTemplate = document.getElementById('questionDetailRowTemplate');
        const detailRow = detailTemplate.content.cloneNode(true).firstElementChild;
        detailRow.dataset.questionId = questionId;
        detailRow.querySelector('.detail-stem').textContent = 'Đang tải...';

        row.after(detailRow);

        try {
            const qDetail = await fetchQuestionDetail(questionId);
            renderDetailRow(detailRow, qDetail);
        } catch (err) {
            detailRow.querySelector('.detail-stem').textContent = 'Không thể tải chi tiết.';
        }
    };

    const showArchiveConfirm = (ids) => {
        pendingArchiveIds = ids;
        if (archiveModal) archiveModal.show();
    };

    let pendingDeleteId = null;
    const deleteConfirmModal = document.getElementById('deleteConfirmModal');
    const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
    let deleteModal = null;

    if (deleteConfirmModal) {
        deleteModal = new bootstrap.Modal(deleteConfirmModal);
    }

    const showDeleteConfirm = (id) => {
        pendingDeleteId = id;
        if (deleteModal) deleteModal.show();
    };

    if (confirmDeleteBtn) {
        confirmDeleteBtn.onclick = async () => {
            if (!pendingDeleteId) return;
            confirmDeleteBtn.disabled = true;
            confirmDeleteBtn.textContent = 'Đang xử lý...';
            try {
                await apiClient.delete(`/api/questions/${pendingDeleteId}`);
                if (deleteModal) deleteModal.hide();
                showToast('Đã xóa câu hỏi thành công!');
                loadQuestions(currentPage);
            } catch (err) {
                showToast(err.message || 'Đã xảy ra lỗi khi xóa câu hỏi.', 'error');
            } finally {
                confirmDeleteBtn.disabled = false;
                confirmDeleteBtn.textContent = 'Xóa vĩnh viễn';
            }
        };
    }

    const bindRowActions = (rows) => {
        rows.forEach(row => {
            const expandBtn = row.querySelector('.expand-btn');
            const editBtn = row.querySelector('[data-action-edit]');
            const archiveBtn = row.querySelector('[data-action-archive]');
            const qId = row.dataset.questionId;

            if (expandBtn && qId) {
                expandBtn.onclick = () => toggleExpand(row, parseInt(qId, 10));
            }
            if (editBtn && qId) {
                editBtn.onclick = () => { window.location.href = `/Question/Edit/${qId}`; };
            }
            if (archiveBtn && qId) {
                archiveBtn.onclick = () => showArchiveConfirm([parseInt(qId, 10)]);
            }
        });
    };

    const renderTable = (data) => {
        const items = data.items || [];
        tbody.innerHTML = '';

        if (items.length === 0) {
            tbody.appendChild(document.getElementById('tableEmptyTemplate').content.cloneNode(true));
            if (paginationSummary) paginationSummary.textContent = '';
            if (paginationContainer) paginationContainer.innerHTML = '';
            return;
        }

        const rowTemplate = document.getElementById('questionRowTemplate');

        items.forEach(q => {
            const row = rowTemplate.content.cloneNode(true).firstElementChild;
            row.dataset.questionId = q.questionId;
            row.dataset.status = q.status;

            const diffClass = difficultyBadgeClass[q.difficulty] || 'badge-diff-1';
            const statusClass = statusBadgeClass[q.status] || 'badge-status-draft';
            const typeLabel = typeLabels[q.questionType] || q.questionType;
            const statusLabel = statusLabels[q.status] || q.status;
            const dateStr = q.updatedAt ? new Date(q.updatedAt).toLocaleDateString('vi-VN') : '-';
            const contentPreview = getContentPreview(q);

            row.querySelector('[data-field-id]').textContent = `Q-${q.questionId}`;
            row.querySelector('[data-field-subject]').textContent = q.subjectCode || '-';
            row.querySelector('[data-field-chapter]').textContent = q.chapterName || '-';
            setLatexContent(row.querySelector('[data-field-content]'), contentPreview);
            row.querySelector('[data-field-type]').textContent = typeLabel;

            const diffSpan = row.querySelector('[data-field-difficulty]');
            diffSpan.className = `badge ${diffClass} rounded-1 py-2 px-2 fw-medium`;
            diffSpan.textContent = q.difficultyLabel || '';

            const statusSpan = row.querySelector('[data-field-status]');
            statusSpan.className = `badge ${statusClass} rounded-1 py-2 px-2 fw-medium`;
            statusSpan.innerHTML = `<i class="bi bi-${q.status === 'Active' ? 'check-circle' : q.status === 'Archived' ? 'archive' : 'pencil-square'} me-1"></i>${statusLabel}`;

            const purposeSpan = row.querySelector('[data-field-purpose]');
            const purposeClass = purposeBadgeClass[q.questionPurpose] || 'bg-secondary';
            const purposeLabel = q.questionPurposeLabel || purposeLabels[q.questionPurpose] || 'N/A';
            purposeSpan.className = `badge ${purposeClass} rounded-1 py-2 px-2 fw-medium`;
            purposeSpan.textContent = purposeLabel;

            tbody.appendChild(row);
        });

        const start = (data.currentPage - 1) * pageSize + 1;
        const end = start + items.length - 1;
        if (paginationSummary) {
            paginationSummary.textContent = `Hiển thị ${start}-${end} trên ${data.totalCount} câu hỏi`;
        }

        renderPagination(data.currentPage, data.totalPages);
        bindCheckboxes();
        bindRowActions(tbody.querySelectorAll('.question-row'));
        updateSelectedCount();

        const doRenderMath = () => {
            if (typeof MathLive !== 'undefined' && MathLive.renderMathInElement) {
                MathLive.renderMathInElement(tbody);
            }
        };
        doRenderMath();
        if (typeof MathLive === 'undefined') {
            window.addEventListener('load', doRenderMath);
        }
    };

    let pendingArchiveIds = [];
    const archiveConfirmModal = document.getElementById('archiveConfirmModal');
    const confirmArchiveBtn = document.getElementById('confirmArchiveBtn');
    let archiveModal = null;

    if (archiveConfirmModal) {
        archiveModal = new bootstrap.Modal(archiveConfirmModal);
    }

    if (confirmArchiveBtn) {
        confirmArchiveBtn.onclick = async () => {
            if (pendingArchiveIds.length === 0) return;
            confirmArchiveBtn.disabled = true;
            confirmArchiveBtn.textContent = 'Đang xử lý...';
            try {
                await apiClient.patch('/api/questions/status', {
                    questionIds: pendingArchiveIds,
                    status: 'Archived'
                });
                if (archiveModal) archiveModal.hide();
                showToast('Đã lưu trữ thành công!');
                loadQuestions(currentPage);
            } catch (err) {
                showToast('Đã xảy ra lỗi khi lưu trữ.', 'error');
            } finally {
                confirmArchiveBtn.disabled = false;
                confirmArchiveBtn.textContent = 'Đồng ý lưu trữ';
            }
        };
    }

    const bulkArchiveBtn = document.getElementById('bulkArchiveBtn');
    if (bulkArchiveBtn) {
        bulkArchiveBtn.onclick = () => {
            const checked = Array.from(tbody.querySelectorAll('.question-item-checkbox:checked'));
            let invalidStatus = false;
            const ids = checked.map(cb => {
                const row = cb.closest('.question-row');
                if (row) {
                    const id = parseInt(row.dataset.questionId, 10);
                    const status = row.dataset.status;
                    if (status !== 'Inprogress') {
                        invalidStatus = true;
                    }
                    return id;
                }
                return null;
            }).filter(id => id != null);
            
            if (ids.length === 0) {
                showToast('Vui lòng chọn ít nhất một câu hỏi.', 'info');
                return;
            }
            if (invalidStatus) {
                showToast('Chức năng Lưu trữ hàng loạt chỉ áp dụng với những câu hỏi ở trạng thái Inprogress.', 'warning');
                return;
            }
            showArchiveConfirm(ids);
        };
    }

    const renderPagination = (current, total) => {
        if (!paginationContainer || total <= 0) {
            paginationContainer.innerHTML = '';
            return;
        }

        paginationContainer.innerHTML = '';

        const addLink = (page, label, disabled = false, active = false) => {
            const li = document.createElement('li');
            li.className = 'page-item' + (disabled ? ' disabled' : '') + (active ? ' active' : '');
            const a = document.createElement('a');
            a.className = 'page-link border rounded-1 px-3 me-1' + (active ? '' : ' text-secondary');
            a.href = '#';
            a.innerHTML = label;
            if (!disabled && !active) {
                a.onclick = (e) => {
                    e.preventDefault();
                    currentPage = page;
                    loadQuestions(page);
                };
            }
            li.appendChild(a);
            paginationContainer.appendChild(li);
        };

        addLink(current - 1, '<i class="bi bi-chevron-left"></i>', current <= 1);

        const delta = 2;
        const range = [];
        for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
            range.push(i);
        }

        if (range[0] > 1) {
            addLink(1, '1');
            if (range[0] > 2) {
                const li = document.createElement('li');
                li.className = 'page-item disabled';
                li.innerHTML = '<a class="page-link text-secondary border-0 bg-transparent px-2 me-1">...</a>';
                paginationContainer.appendChild(li);
            }
        }

        range.forEach(i => {
            addLink(i, String(i), false, i === current);
        });

        if (range[range.length - 1] < total) {
            if (range[range.length - 1] < total - 1) {
                const li = document.createElement('li');
                li.className = 'page-item disabled';
                li.innerHTML = '<a class="page-link text-secondary border-0 bg-transparent px-2 me-1">...</a>';
                paginationContainer.appendChild(li);
            }
            addLink(total, String(total));
        }

        addLink(current + 1, '<i class="bi bi-chevron-right"></i>', current >= total);
    };

    const loadQuestions = async (page) => {
        tbody.innerHTML = '';
        tbody.appendChild(document.getElementById('tableLoadingTemplate').content.cloneNode(true));

        try {
            const qs = buildQuery(page);
            const data = await apiClient.get(`/api/questions?${qs}`);
            renderTable(data);
        } catch (err) {
            console.error('Failed to load questions', err);
            tbody.innerHTML = '';
            tbody.appendChild(document.getElementById('tableErrorTemplate').content.cloneNode(true));
        }
    };

    const applyFilter = () => {
        currentPage = 1;
        loadQuestions(1);
        const dropdown = document.querySelector('.filter-dropdown');
        if (dropdown && bootstrap.Dropdown) {
            const instance = bootstrap.Dropdown.getInstance(document.getElementById('filterDropdownBtn'));
            if (instance) instance.hide();
        }
    };

    const clearFilter = () => {
        document.getElementById('filterKeyword').value = '';
        document.getElementById('filterQuestionType').value = '';
        document.getElementById('filterDifficulty').value = '';
        document.getElementById('filterStatus').value = '';
        if (filterSubject) filterSubject.value = '';
        if (filterChapter) filterChapter.value = '';
        const filterPurpose = document.getElementById('filterPurpose');
        if (filterPurpose) filterPurpose.value = '';
        currentPage = 1;
        loadQuestions(1);
    };

    document.getElementById('applyFilterBtn')?.addEventListener('click', applyFilter);
    document.getElementById('clearFilterBtn')?.addEventListener('click', clearFilter);

    document.getElementById('filterKeyword')?.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') applyFilter();
    });

    loadSubjects();
    loadQuestions(1);
})();
