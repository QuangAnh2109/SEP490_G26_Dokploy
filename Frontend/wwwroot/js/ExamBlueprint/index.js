$(document).ready(function () {
    const state = {
        page: 1,
        pageSize: 10,
        selectedId: null
    };

    const role = getUserRole();
    if (!(role === 'Teacher' || role === 'Giáo viên' || role === 'Admin' || role === 'Quản trị viên' || role === 'Administrator')) {
        showPageError('Bạn không có quyền truy cập màn hình ma trận đề.');
        return;
    }

    const userId = typeof getUserIdFromToken === 'function' ? getUserIdFromToken() : 'N/A';
    console.log('[ExamBlueprint] User identity:', { userId, role });

    bindEvents();
    renderNoticeFromQuery();
    loadSubjects().then(function () {
        console.log('[ExamBlueprint] Subjects loaded, starting loadList(1)');
        loadList(1);
    });

    function appendTemplate($container, templateId) {
        const template = document.getElementById(templateId);
        if (template && template.content) {
            $container.append(template.content.cloneNode(true));
        } else {
            console.error(`Template not found: ${templateId}`);
        }
    }

    function bindEvents() {
        $('#blueprintFilterForm').on('submit', function (e) {
            e.preventDefault();
            loadList(1);
        });

        $('#blueprintTableBody').on('click', '[data-action="view-blueprint"]', function () {
            const id = parseInt($(this).closest('tr').attr('data-blueprint-id'), 10);
            if (Number.isInteger(id)) {
                loadDetail(id);
            }
        });

        $('#blueprintPagination').on('click', 'a[data-page]', function (e) {
            e.preventDefault();
            const page = parseInt($(this).attr('data-page'), 10);
            if (Number.isInteger(page)) {
                loadList(page);
            }
        });
    }

    function renderNoticeFromQuery() {
        const params = new URLSearchParams(window.location.search);
        const flashSuccess = sessionStorage.getItem('examBlueprintFlashSuccess');
        const flashWarningsRaw = sessionStorage.getItem('examBlueprintFlashWarnings');

        if (params.get('created') === '1') {
            $('#pageNotice')
                .text(flashSuccess || 'Tạo ma trận đề thành công.')
                .removeClass('d-none');
        }

        if (flashWarningsRaw) {
            try {
                const warnings = JSON.parse(flashWarningsRaw);
                if (Array.isArray(warnings) && warnings.length > 0) {
                    const $container = $('#flashWarningContainer');
                    const $list = $('#flashWarningList');
                    const t = document.getElementById('flashWarningItemTemplate');
                    
                    $list.empty();
                    warnings.forEach(w => {
                        const li = t.content.cloneNode(true).firstElementChild;
                        li.textContent = w;
                        $list.append(li);
                    });
                    $container.removeClass('d-none');
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
                const $select = $('#filterSubject');
                $select.find('option:not(:first)').remove();
                (subjects || []).forEach(function (subject) {
                    const text = subject.code ? `${subject.code} - ${subject.name}` : subject.name;
                    $select.append(new Option(text, subject.subjectId));
                });
            })
            .catch(function (error) {
                console.error(error);
                showPageError(resolveApiError(error));
            });
    }

    function loadList(page) {
        state.page = page;
        $('#pageError').addClass('d-none').text('');

        const params = new URLSearchParams();
        params.set('page', String(page));
        params.set('pageSize', String(state.pageSize));

        const keyword = ($('#filterKeyword').val() || '').toString().trim();
        const subjectId = ($('#filterSubject').val() || '').toString();
        if (keyword) params.set('keyword', keyword);
        if (subjectId) params.set('subjectId', subjectId);

        const $tbody = $('#blueprintTableBody');
        $tbody.empty();
        appendTemplate($tbody, 'blueprintLoadingTemplate');

        apiClient.get(`/api/exam-blueprints?${params.toString()}`)
            .then(function (response) {
                console.log('[ExamBlueprint] API Response:', response);
                const items = response.items || [];
                
                if (items.length > 0) {
                    const firstId = items[0].examBlueprintId;
                    state.selectedId = items.some(i => i.examBlueprintId === state.selectedId) ? state.selectedId : firstId;
                } else {
                    state.selectedId = null;
                }

                renderTable(items);
                renderPagination(response.page || 1, response.totalPages || 0);

                if (state.selectedId) {
                    loadDetail(state.selectedId);
                } else {
                    renderEmptyDetail();
                }
            })
            .catch(function (error) {
                console.error(error);
                $tbody.empty();
                appendTemplate($tbody, 'blueprintErrorTemplate');
                renderEmptyDetail('Không thể tải chi tiết do lỗi danh sách.');
                renderPagination(1, 0);
                showPageError(resolveApiError(error));
            });
    }

    function cloneTemplateOrFallback(templateId, fallbackHtml) {
        const t = document.getElementById(templateId);
        if (t && t.content) {
            return t.content.cloneNode(true);
        }
        console.warn(`Missing template #${templateId}. Falling back to inline row.`);
        const container = document.createElement('tbody');
        container.innerHTML = fallbackHtml;
        return container.firstElementChild || document.createTextNode('');
    }

    function renderTable(items) {
        console.log('[ExamBlueprint] renderTable called with items:', items.length);
        const $tbody = $('#blueprintTableBody');
        $tbody.empty();

        if (!items.length) {
            console.log('[ExamBlueprint] No items to render');
            appendTemplate($tbody, 'blueprintEmptyTemplate');
            return;
        }

        const t = document.getElementById('blueprintRowTemplate');
        if (!t) {
            console.error('[ExamBlueprint] blueprintRowTemplate not found!');
            return;
        }

        items.forEach(function (item, index) {
            try {
                const row = t.content.cloneNode(true).firstElementChild;
                const subjectText = item.subjectCode || item.subjectName || '';
                
                row.setAttribute('data-blueprint-id', item.examBlueprintId);
                if (item.examBlueprintId === state.selectedId) {
                    row.classList.add('table-active');
                }
                
                row.querySelector('[data-field-name]').textContent = item.name || '';
                row.querySelector('[data-field-questions]').textContent = item.totalQuestions ?? 0;
                row.querySelector('[data-field-subject]').textContent = subjectText;
                
                const badge = row.querySelector('[data-field-status]');
                if (badge) {
                    badge.className = 'badge ' + getStatusClass(item.status);
                    badge.textContent = item.statusLabel || '';
                }
                
                const updatedField = row.querySelector('[data-field-updated]');
                if (updatedField) {
                    updatedField.textContent = formatDate(item.updatedAtUtc);
                }
                
                $tbody.append(row);
            } catch (err) {
                console.error(`[ExamBlueprint] Error rendering row ${index}:`, err, item);
            }
        });
        console.log('[ExamBlueprint] renderTable finished');
    }

    function renderPagination(page, totalPages) {
        console.log('[ExamBlueprint] renderPagination:', { page, totalPages });
        const $pagination = $('#blueprintPagination');
        $pagination.empty();

        if (!totalPages || totalPages <= 1) return;

        try {
            const prevTId = page > 1 ? 'paginationPrevTemplate' : 'paginationPrevDisabledTemplate';
            const prevT = document.getElementById(prevTId);
            if (!prevT) throw new Error(`Template not found: ${prevTId}`);

            const prevLi = prevT.content.cloneNode(true).firstElementChild;
            if (page > 1) {
                const a = prevLi.querySelector('[data-page]');
                if (a) a.setAttribute('data-page', page - 1);
            }
            $pagination.append(prevLi);

            for (let i = 1; i <= totalPages; i++) {
                const pageTId = i === page ? 'paginationPageActiveTemplate' : 'paginationPageTemplate';
                const pageT = document.getElementById(pageTId);
                if (!pageT) continue;

                const pageLi = pageT.content.cloneNode(true).firstElementChild;
                if (i === page) {
                    const link = pageLi.querySelector('.page-link');
                    if (link) link.textContent = i;
                } else {
                    const a = pageLi.querySelector('[data-page]');
                    if (a) {
                        a.setAttribute('data-page', i);
                        a.textContent = i;
                    }
                }
                $pagination.append(pageLi);
            }

            const nextTId = page < totalPages ? 'paginationNextTemplate' : 'paginationNextDisabledTemplate';
            const nextT = document.getElementById(nextTId);
            if (nextT) {
                const nextLi = nextT.content.cloneNode(true).firstElementChild;
                if (page < totalPages) {
                    const a = nextLi.querySelector('[data-page]');
                    if (a) a.setAttribute('data-page', page + 1);
                }
                $pagination.append(nextLi);
            }
        } catch (err) {
            console.error('[ExamBlueprint] Error rendering pagination:', err);
        }
    }

    function loadDetail(id) {
        state.selectedId = id;
        $('#blueprintTableBody tr').removeClass('table-active');
        $(`#blueprintTableBody tr[data-blueprint-id="${id}"]`).addClass('table-active');

        apiClient.get(`/api/exam-blueprints/${id}`)
            .then(function (detail) {
                $('#blueprintInfoName').text(detail.name || '--');
                $('#blueprintInfoSubject').text(detail.subjectCode || detail.subjectName || '--');
                $('#blueprintInfoUpdated').text(formatDate(detail.updatedAtUtc));
                $('#blueprintInfoQuestionCount').text(`${detail.totalQuestions ?? 0} câu`);

                const rows = detail.rows || [];
                const $matrixBody = $('#blueprintMatrixBody');
                $matrixBody.empty();

                if (!rows.length) {
                    const emptyT = document.getElementById('matrixEmptyTemplate');
                    const emptyRow = emptyT.content.cloneNode(true).firstElementChild;
                    emptyRow.querySelector('[data-message]').textContent = 'Ma trận đề chưa có dòng nào.';
                    $matrixBody.append(emptyRow);
                    return;
                }

                const t = document.getElementById('blueprintMatrixRowTemplate');
                rows.forEach(function (row) {
                    const tr = t.content.cloneNode(true).firstElementChild;
                    tr.querySelector('[data-field-chapter]').textContent = row.chapterName || '';
                    tr.querySelector('[data-field-difficulty]').textContent = row.difficultyLabel || '';
                    tr.querySelector('[data-field-questions]').textContent = row.totalQuestions ?? 0;
                    $matrixBody.append(tr);
                });
            })
            .catch(function (error) {
                console.error(error);
                renderEmptyDetail('Không thể tải chi tiết ma trận đề.');
                showPageError(resolveApiError(error));
            });
    }

    function renderEmptyDetail(message) {
        $('#blueprintInfoName').text('Chưa chọn');
        $('#blueprintInfoSubject').text('--');
        $('#blueprintInfoUpdated').text('--');
        $('#blueprintInfoQuestionCount').text('--');
        
        const $matrixBody = $('#blueprintMatrixBody');
        $matrixBody.empty();
        const emptyT = document.getElementById('matrixEmptyTemplate');
        const emptyRow = emptyT.content.cloneNode(true).firstElementChild;
        emptyRow.querySelector('[data-message]').textContent = message || 'Chọn một ma trận đề để xem chi tiết.';
        $matrixBody.append(emptyRow);
    }

    function getStatusClass(status) {
        switch (status) {
            case 0: return 'badge-status badge-draft';
            case 1: return 'badge-status badge-published';
            case 2: return 'badge-status badge-inuse';
            case 3: return 'badge-status badge-archived';
            default: return 'text-bg-secondary';
        }
    }

    function formatDate(value) {
        if (!value) return '--';
        const d = new Date(value);
        if (Number.isNaN(d.getTime())) return '--';
        return d.toLocaleDateString('vi-VN');
    }

    function showPageError(message) {
        $('#pageError')
            .removeClass('d-none alert-warning')
            .addClass('alert-danger')
            .text(message || 'Đã xảy ra lỗi.');
    }

    function resolveApiError(error) {
        return error?.xhr?.responseJSON?.message || error?.message || 'Đã có lỗi xảy ra từ máy chủ.';
    }

    // escapeHtml is no longer needed in many places due to .textContent, 
    // but kept as helper if needed for text node creation in complex scenarios.
});
