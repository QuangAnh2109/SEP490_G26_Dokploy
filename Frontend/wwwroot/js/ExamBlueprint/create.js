$(document).ready(function () {
    const state = {
        chapterOptions: []
    };

    const role = getUserRole();
    if (!(role === 'Teacher' || role === 'Giáo viên' || role === 'Admin' || role === 'Quản trị viên' || role === 'Administrator')) {
        showError(['Bạn không có quyền truy cập màn hình tạo ma trận đề.']);
        $('#btnSaveDraft, #btnPublish, #btnAddRow').prop('disabled', true);
        return;
    }

    bindEvents();
    loadSubjects();
    recalcTotals();
    refreshEmptyHint();

    function bindEvents() {
        $('#btnAddRow').on('click', function () {
            addMatrixRow();
        });

        $('#matrixRowList').on('click', '[data-remove-row]', function () {
            $(this).closest('[data-matrix-item]').remove();
            recalcTotals();
            refreshEmptyHint();
        });

        $('#matrixRowList').on('input change', '.matrix-count, .matrix-chapter, .matrix-difficulty', function () {
            recalcTotals();
            validateRowCapacity($(this).closest('[data-matrix-item]'));
        });

        $('#blueprintSubject').on('change', function () {
            const subjectId = parseInt($(this).val(), 10);
            hideMessages();
            if (!Number.isInteger(subjectId) || subjectId <= 0) {
                state.chapterOptions = [];
                refreshAllChapterSelects();
                return;
            }
            loadChapters(subjectId);
        });

        $('#btnSaveDraft').on('click', function () {
            submitCreate(0);
        });

        $('#btnPublish').on('click', function () {
            submitCreate(1);
        });
    }

    function loadSubjects() {
        apiClient.get('/api/exam-blueprints/subjects')
            .then(function (subjects) {
                const $subject = $('#blueprintSubject');
                $subject.find('option:not(:first)').remove();
                (subjects || []).forEach(function (item) {
                    const text = item.code ? `${item.code} - ${item.name}` : item.name;
                    $subject.append(new Option(text, item.subjectId));
                });
            })
            .catch(function (error) {
                console.error(error);
                showError([resolveApiError(error)]);
            });
    }

    function loadChapters(subjectId) {
        apiClient.get(`/api/exam-blueprints/subjects/${subjectId}/chapters`)
            .then(function (chapters) {
                state.chapterOptions = chapters || [];
                refreshAllChapterSelects();
            })
            .catch(function (error) {
                console.error(error);
                state.chapterOptions = [];
                refreshAllChapterSelects();
                showError([resolveApiError(error)]);
            });
    }

    function addMatrixRow(initial) {
        const template = document.getElementById('matrixRowTemplate');
        if (!template) return;

        const cloned = template.content.cloneNode(true);
        const $rowList = $('#matrixRowList');
        $rowList.append(cloned);

        const $row = $rowList.find('[data-matrix-item]').last();
        if (initial) {
            $row.find('.matrix-difficulty').val(String(initial.Difficulty ?? 1));
            $row.find('.matrix-count').val(initial.TotalQuestions ?? 0);
        }

        refreshChapterSelect($row.find('.matrix-chapter'), initial?.ChapterId ?? null);
        recalcTotals();
        refreshEmptyHint();
    }

    function refreshAllChapterSelects() {
        $('#matrixRowList .matrix-chapter').each(function () {
            const current = parseInt($(this).val(), 10);
            refreshChapterSelect($(this), Number.isInteger(current) ? current : null);
        });
    }

    function refreshChapterSelect($select, selectedValue) {
        const select = $select[0];
        if (!select) return;
        
        select.innerHTML = '';
        select.add(new Option('Chọn chương', ''));

        state.chapterOptions.forEach(function (chapter) {
            const text = buildChapterOptionLabel(chapter);
            const option = new Option(text, chapter.chapterId);
            if (selectedValue && Number(chapter.chapterId) === Number(selectedValue)) {
                option.selected = true;
            }
            select.add(option);
        });
    }

    function buildChapterOptionLabel(chapter) {
        const availability = toAvailabilityMap(chapter.availabilityByDifficulty);
        return `${chapter.name} (NB:${availability[1]}, TH:${availability[2]}, VD:${availability[3]}, VDC:${availability[4]})`;
    }

    function recalcTotals() {
        let total = 0;
        $('#matrixRowList .matrix-count').each(function () {
            const val = parseInt($(this).val(), 10);
            if (Number.isInteger(val) && val >= 0) {
                total += val;
            }
        });
        $('#computedMatrixTotal').text(total);
    }

    function refreshEmptyHint() {
        $('#emptyMatrixHint').toggleClass('d-none', $('#matrixRowList [data-matrix-item]').length > 0);
    }

    function collectRows() {
        const rows = [];

        $('#matrixRowList [data-matrix-item]').each(function () {
            const chapterId = parseInt($(this).find('.matrix-chapter').val(), 10);
            const difficulty = parseInt($(this).find('.matrix-difficulty').val(), 10);
            const totalQuestions = parseInt($(this).find('.matrix-count').val(), 10);

            rows.push({
                ChapterId: Number.isInteger(chapterId) ? chapterId : 0,
                Difficulty: Number.isInteger(difficulty) ? difficulty : 0,
                TotalQuestions: Number.isInteger(totalQuestions) ? totalQuestions : -1
            });
        });

        return rows;
    }

    function buildChapterAvailability() {
        const map = {};
        state.chapterOptions.forEach(function (chapter) {
            map[chapter.chapterId] = toAvailabilityMap(chapter.availabilityByDifficulty);
        });
        return map;
    }

    function setSubmitting(isSubmitting) {
        $('#btnAddRow, #btnSaveDraft, #btnPublish').prop('disabled', isSubmitting);
    }

    function validateRowCapacity($row) {
        const chapterId = parseInt($row.find('.matrix-chapter').val(), 10);
        const difficulty = parseInt($row.find('.matrix-difficulty').val(), 10);
        const count = parseInt($row.find('.matrix-count').val(), 10);
        const $warning = $row.find('.row-warning');

        if (!Number.isInteger(chapterId) || chapterId <= 0 || !Number.isInteger(count) || count <= 0) {
            $warning.addClass('d-none').text('');
            return;
        }

        const chapter = state.chapterOptions.find(c => c.chapterId === chapterId);
        if (chapter) {
            const availability = toAvailabilityMap(chapter.availabilityByDifficulty);
            const available = availability[difficulty] || 0;
            if (count > available) {
                $warning.text(`Vượt quá số câu hiện có (tối đa: ${available})`).removeClass('d-none');
            } else {
                $warning.addClass('d-none').text('');
            }
        } else {
            $warning.addClass('d-none').text('');
        }
    }

    function submitCreate(targetStatus) {
        hideMessages();

        const subjectId = parseInt($('#blueprintSubject').val(), 10);
        const targetTotalQuestions = parseInt($('#blueprintTargetQuestionCount').val(), 10);
        const rows = collectRows();

        const payload = {
            Name: ($('#blueprintName').val() || '').toString(),
            Description: ($('#blueprintDescription').val() || '').toString(),
            SubjectId: Number.isInteger(subjectId) ? subjectId : 0,
            TargetTotalQuestions: Number.isInteger(targetTotalQuestions) ? targetTotalQuestions : -1,
            TargetStatus: targetStatus,
            Rows: rows
        };

        const validator = window.BlueprintValidator;
        const rowErrors = validator.validateRows(rows);
        const payloadErrors = validator.validate(payload, buildChapterAvailability());
        const errors = [...rowErrors, ...payloadErrors];
        if (errors.length > 0) {
            showError(errors);
            return;
        }

        setSubmitting(true);
        apiClient.post('/api/exam-blueprints', payload)
            .then(function (response) {
                const hasWarnings = Array.isArray(response?.warnings) && response.warnings.length > 0;
                
                if (hasWarnings) {
                    showSuccess(response.message || 'Lưu ma trận đề thành công.');
                    showWarnings(response.warnings);
                    // Stay on page, but update UI to let user know they can leave
                    if (!$('#btnGoBack').length) {
                        $('.page-actions').prepend('<a id="btnGoBack" class="btn btn-outline-success" href="/ExamBlueprint">Quay lại danh sách</a>');
                    }
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                } else {
                    try {
                        if (response?.message) {
                            sessionStorage.setItem('examBlueprintFlashSuccess', response.message);
                        }
                    } catch (e) {
                        console.warn('Cannot persist flash message', e);
                    }
                    window.location.href = '/ExamBlueprint?created=1';
                }
            })
            .catch(function (error) {
                console.error(error);
                const serverErrors = error?.xhr?.responseJSON?.errors;
                if (Array.isArray(serverErrors) && serverErrors.length > 0) {
                    showError(serverErrors);
                } else {
                    showError([resolveApiError(error)]);
                }
            })
            .finally(function () {
                setSubmitting(false);
            });
    }

    function showSuccess(message) {
        $('#createSuccess').text(message).removeClass('d-none');
    }

    function showWarnings(warnings) {
        const $list = $('#warningList');
        $list.empty();
        warnings.forEach(w => {
            $list.append(`<li>${w.message || 'Cảnh báo dữ liệu'}</li>`);
        });
        $('#createWarnings').removeClass('d-none');
    }

    function showError(messages) {
        const errorContainer = document.getElementById('createError');
        if (!errorContainer) return;

        errorContainer.innerHTML = '';
        const listTemplate = document.getElementById('errorListTemplate');
        const itemTemplate = document.getElementById('errorItemTemplate');
        if (!listTemplate || !itemTemplate) return;

        const list = listTemplate.content.cloneNode(true).firstElementChild;
        const listBody = list.hasAttribute('data-error-list') ? list : list.querySelector('[data-error-list]');
        
        messages.forEach(m => {
            const item = itemTemplate.content.cloneNode(true).firstElementChild;
            const msgNode = item.hasAttribute('data-error-message') ? item : item.querySelector('[data-error-message]');
            if (msgNode) msgNode.textContent = m;
            listBody.appendChild(item);
        });

        errorContainer.appendChild(list);
        errorContainer.classList.remove('d-none');
        $('#createWarnings').addClass('d-none').empty();
    }

    function hideMessages() {
        $('#createError, #createWarnings, #createSuccess').addClass('d-none').empty();
        $('#createWarnings #warningList').empty();
    }

    function resolveApiError(error) {
        return error?.xhr?.responseJSON?.message || error?.message || 'Đã có lỗi xảy ra từ máy chủ.';
    }

    function toAvailabilityMap(list) {
        const map = { 1: 0, 2: 0, 3: 0, 4: 0 };
        (list || []).forEach(function (item) {
            map[item.difficulty] = item.availableQuestions;
        });
        return map;
    }
});
