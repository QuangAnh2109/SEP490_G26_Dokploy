(() => {
  const toArray = (value) => Array.from(value || []);

  const bindInteractiveRows = () => {
    toArray(document.querySelectorAll('.interactive-row')).forEach((row) => {
      if (!row.hasAttribute('tabindex')) {
        row.setAttribute('tabindex', '0');
      }
      row.addEventListener('focus', () => row.classList.add('is-active'));
      row.addEventListener('blur', () => row.classList.remove('is-active'));
      row.addEventListener('mouseenter', () => row.classList.add('is-active'));
      row.addEventListener('mouseleave', () => row.classList.remove('is-active'));
    });
  };

  const formatDuration = (totalSeconds) => {
    const sec = Math.max(0, totalSeconds);
    const h = Math.floor(sec / 3600);
    const m = Math.floor((sec % 3600) / 60);
    const s = sec % 60;
    if (h > 0) {
      return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    }
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  };

  const bindCountdowns = () => {
    toArray(document.querySelectorAll('[data-countdown]')).forEach((node) => {
      let remain = Number(node.getAttribute('data-countdown'));
      if (!Number.isFinite(remain) || remain < 0) {
        return;
      }

      const render = () => {
        node.textContent = formatDuration(remain);
        node.classList.remove('is-warning', 'is-danger');
        if (remain <= 300) {
          node.classList.add('is-danger');
        } else if (remain <= 900) {
          node.classList.add('is-warning');
        }
      };

      render();
      const timer = window.setInterval(() => {
        remain = Math.max(0, remain - 1);
        render();
        if (remain <= 0) {
          window.clearInterval(timer);
        }
      }, 1000);
    });
  };

  const isElementVisible = (node) => {
    if (!node) {
      return false;
    }

    if (node.closest('.d-none, [hidden]')) {
      return false;
    }

    const style = window.getComputedStyle(node);
    if (style.display === 'none' || style.visibility === 'hidden') {
      return false;
    }

    return node.getClientRects().length > 0;
  };

  const getMathFieldValue = (field) => {
    if (!field) {
      return '';
    }

    if (typeof field.getValue === 'function') {
      const latex = field.getValue('latex');
      return typeof latex === 'string' ? latex : '';
    }

    if (typeof field.value === 'string') {
      return field.value;
    }

    return field.textContent || '';
  };

  const hasPromptAnswer = (field) => {
    if (!field || typeof field.getPrompts !== 'function' || typeof field.getPromptValue !== 'function') {
      return null;
    }

    const prompts = field.getPrompts();
    if (!Array.isArray(prompts) || prompts.length === 0) {
      return null;
    }

    return prompts.some((promptId) => {
      const value = field.getPromptValue(promptId);
      return typeof value === 'string' ? value.trim().length > 0 : false;
    });
  };

  const isAnswered = (questionNode) => {
    const hasChecked = toArray(questionNode.querySelectorAll('input[type="radio"], input[type="checkbox"]'))
      .some((input) => !input.disabled && isElementVisible(input) && input.checked);
    if (hasChecked) {
      return true;
    }

    const hasText = toArray(questionNode.querySelectorAll('input[type="text"], input[type="number"], textarea'))
      .some((input) => !input.disabled && isElementVisible(input) && input.value.trim().length > 0);
    if (hasText) {
      return true;
    }

    const hasMathAnswer = toArray(questionNode.querySelectorAll('math-field[data-answer-math]'))
      .some((field) => {
        if (field.hasAttribute('disabled') || !isElementVisible(field)) {
          return false;
        }

        const promptAnswer = hasPromptAnswer(field);
        if (promptAnswer !== null) {
          return promptAnswer;
        }

        return getMathFieldValue(field).trim().length > 0;
      });
    return hasMathAnswer;
  };

  const bindValidateUnanswered = () => {
    const btn = document.querySelector('[data-action="validate-unanswered"]');
    if (!btn) {
      return;
    }

    btn.addEventListener('click', () => {
      const questions = toArray(document.querySelectorAll('.question-block[data-question-id]'));
      let unanswered = 0;

      questions.forEach((question) => {
        if (isAnswered(question)) {
          question.classList.remove('border-danger');
          return;
        }
        unanswered += 1;
        question.classList.add('border-danger');
      });

      const output = document.querySelector('[data-unanswered-output]');
      if (output) {
        output.textContent = unanswered === 0
          ? 'Tat ca cau hoi da co cau tra loi.'
          : `Con ${unanswered} cau chua tra loi.`;
      }
    });
  };

  const reindexAnswers = (list) => {
    toArray(list.querySelectorAll('[data-answer-item]')).forEach((item, idx) => {
      const label = item.querySelector('[data-answer-index]');
      if (label) {
        label.textContent = String.fromCharCode(65 + idx);
      }
    });
  };

  const bindAnswerRepeater = () => {
    document.addEventListener('click', (event) => {
      const addBtn = event.target.closest('[data-add-answer]');
      if (addBtn) {
        const listId = addBtn.getAttribute('data-add-answer');
        const list = document.getElementById(listId);
        if (!list) {
          return;
        }

        const template = list.querySelector('[data-answer-template]');
        if (!template) {
          return;
        }

        const clone = template.cloneNode(true);
        clone.removeAttribute('data-answer-template');
        clone.setAttribute('data-answer-item', '');
        clone.classList.remove('d-none');

        toArray(clone.querySelectorAll('input[type="text"], input[type="number"], textarea')).forEach((input) => {
          input.value = '';
        });

        toArray(clone.querySelectorAll('input[type="radio"], input[type="checkbox"]')).forEach((input) => {
          input.checked = false;
        });

        list.appendChild(clone);
        reindexAnswers(list);
        return;
      }

      const removeBtn = event.target.closest('[data-remove-answer]');
      if (!removeBtn) {
        return;
      }

      const list = removeBtn.closest('[data-answer-list]');
      const row = removeBtn.closest('[data-answer-item]');
      if (!list || !row) {
        return;
      }

      const currentRows = toArray(list.querySelectorAll('[data-answer-item]'));
      if (currentRows.length <= 2) {
        return;
      }

      row.remove();
      reindexAnswers(list);
    });

    toArray(document.querySelectorAll('[data-answer-list]')).forEach((list) => reindexAnswers(list));
  };

  const recalcMatrixTotals = (list) => {
    const panel = list.closest('.panel-card, .form-section, .soft-card') || list.parentElement;
    let totalQuestions = 0;
    let totalPoints = 0;

    toArray(list.querySelectorAll('[data-matrix-item]')).forEach((row) => {
      const countInput = row.querySelector('.matrix-count');
      const pointInput = row.querySelector('.matrix-point');
      const count = Number(countInput ? countInput.value : 0);
      const point = Number(pointInput ? pointInput.value : 0);

      totalQuestions += Number.isFinite(count) ? count : 0;
      totalPoints += Number.isFinite(point) ? point : 0;
    });

    const totalCountNode = panel ? panel.querySelector('[data-total-count]') : null;
    const totalPointNode = panel ? panel.querySelector('[data-total-point]') : null;

    if (totalCountNode) {
      totalCountNode.textContent = String(totalQuestions);
    }

    if (totalPointNode) {
      totalPointNode.textContent = `${totalPoints.toFixed(1)}`;
    }
  };

  const bindMatrixRepeater = () => {
    toArray(document.querySelectorAll('[data-add-matrix]')).forEach((btn) => {
      btn.addEventListener('click', () => {
        const listId = btn.getAttribute('data-add-matrix');
        const list = document.getElementById(listId);
        if (!list) {
          return;
        }

        const template = list.querySelector('[data-matrix-template]');
        if (!template) {
          return;
        }

        const clone = template.cloneNode(true);
        clone.removeAttribute('data-matrix-template');
        clone.setAttribute('data-matrix-item', '');
        clone.classList.remove('d-none');

        toArray(clone.querySelectorAll('input')).forEach((input) => {
          input.value = input.classList.contains('matrix-point') ? '1' : '';
        });

        list.appendChild(clone);
        recalcMatrixTotals(list);
      });
    });

    document.addEventListener('click', (event) => {
      const removeBtn = event.target.closest('[data-remove-matrix]');
      if (!removeBtn) {
        return;
      }

      const list = removeBtn.closest('[data-matrix-list]');
      const row = removeBtn.closest('[data-matrix-item]');
      if (!list || !row) {
        return;
      }

      const rows = toArray(list.querySelectorAll('[data-matrix-item]'));
      if (rows.length <= 1) {
        return;
      }

      row.remove();
      recalcMatrixTotals(list);
    });

    document.addEventListener('input', (event) => {
      if (!event.target.closest('.matrix-count, .matrix-point')) {
        return;
      }

      const list = event.target.closest('[data-matrix-list]');
      if (list) {
        recalcMatrixTotals(list);
      }
    });

    toArray(document.querySelectorAll('[data-matrix-list]')).forEach((list) => recalcMatrixTotals(list));
  };

  const bindImportForms = () => {
    toArray(document.querySelectorAll('[data-import-form]')).forEach((form) => {
      form.addEventListener('submit', (event) => {
        event.preventDefault();

        const fileInput = form.querySelector('[data-import-file]');
        const result = form.querySelector('[data-import-result]');
        if (!fileInput || !result) {
          return;
        }

        const allowedExt = (form.getAttribute('data-allowed-ext') || '')
          .split(',')
          .map((ext) => ext.trim().toLowerCase())
          .filter(Boolean);

        const selectedName = fileInput.files && fileInput.files[0] ? fileInput.files[0].name : '';
        const lowerName = selectedName.toLowerCase();
        const isAllowed = selectedName.length > 0 && allowedExt.some((ext) => lowerName.endsWith(ext));

        result.classList.add('show', 'alert');
        result.classList.remove('alert-danger', 'alert-success');

        if (!isAllowed) {
          result.classList.add('alert-danger');
          result.textContent = `Tep khong hop le. Chi chap nhan: ${allowedExt.join(', ') || 'khong xac dinh'}.`;
          return;
        }

        result.classList.add('alert-success');
        result.textContent = `Nhap du lieu mo phong thanh cong: ${selectedName}.`;
      });
    });
  };

  bindInteractiveRows();
  bindCountdowns();
  bindValidateUnanswered();
  bindAnswerRepeater();
  bindMatrixRepeater();
  bindImportForms();
})();

(async () => {
  const publicToggle = document.getElementById('publicExamToggle');
  const classSelectionBlock = document.getElementById('classSelectionBlock');
  const classSection = document.getElementById('classSelectionSection');
  const classSearchInput = document.getElementById('classSearchInput');
  const classSubjectFilter = document.getElementById('classSubjectFilter');
  const classSemesterFilter = document.getElementById('classSemesterFilter');
  const classTable = document.getElementById('classSelectionTable');
  const classTableBody = document.getElementById('classTableBody');
  const selectedCount = document.getElementById('selectedClassCount');
  const classPaginationPrev = document.getElementById('classPaginationPrev');
  const classPaginationNext = document.getElementById('classPaginationNext');
  const classPageButtons = Array.from(document.querySelectorAll('[data-class-page]'));
  const classPageItems = Array.from(document.querySelectorAll('[data-page-item]'));
  const classPaginationSummary = document.getElementById('classPaginationSummary');

  const blueprintSearchInput = document.getElementById('blueprintSearchInput');
  const blueprintSubjectFilter = document.getElementById('blueprintSubjectFilter');
  const blueprintListTable = document.getElementById('blueprintListTable');
  const blueprintTableBody = document.getElementById('blueprintTableBody');
  const blueprintPaginationPrev = document.getElementById('blueprintPaginationPrev');
  const blueprintPaginationNext = document.getElementById('blueprintPaginationNext');
  const blueprintPageButtons = Array.from(document.querySelectorAll('[data-blueprint-page]'));
  const blueprintPageItems = Array.from(document.querySelectorAll('[data-blueprint-page-item]'));
  const blueprintFilterSummary = document.getElementById('blueprintFilterSummary');
  const selectedBlueprintName = document.getElementById('selectedBlueprintName');
  const blueprintInfoName = document.getElementById('blueprintInfoName');
  const blueprintInfoUpdated = document.getElementById('blueprintInfoUpdated');
  const blueprintInfoSubject = document.getElementById('blueprintInfoSubject');
  const blueprintInfoQuestionCount = document.getElementById('blueprintInfoQuestionCount');
  const blueprintMatrixBody = document.getElementById('blueprintMatrixBody');

  const examGenerationBlueprint = document.getElementById('examGenerationBlueprint');
  const examGenerationManual = document.getElementById('examGenerationManual');
  const blueprintModeSection = document.getElementById('blueprintModeSection');
  const manualModeSection = document.getElementById('manualModeSection');
  const examGenerationModeDivider = document.getElementById('examGenerationModeDivider');
  const publicSubjectSection = document.getElementById('publicSubjectSection');
  const publicSubjectFilter = document.getElementById('publicSubjectFilter');

  const manualQuestionSubjectFilter = document.getElementById('manualQuestionSubjectFilter');
  const manualQuestionChapterFilter = document.getElementById('manualQuestionChapterFilter');
  const manualQuestionLevelFilter = document.getElementById('manualQuestionLevelFilter');
  const manualQuestionSearchInput = document.getElementById('manualQuestionSearchInput');
  const manualQuestionTableBody = document.getElementById('manualQuestionTableBody');
  const manualQuestionSummary = document.getElementById('manualQuestionSummary');
  const manualQuestionClearButton = document.getElementById('manualQuestionClearButton');
  const manualQuestionPaginationPrev = document.getElementById('manualQuestionPaginationPrev');
  const manualQuestionPaginationNext = document.getElementById('manualQuestionPaginationNext');
  const manualQuestionPageButtons = Array.from(document.querySelectorAll('[data-manual-page]'));
  const manualQuestionPageItems = Array.from(document.querySelectorAll('[data-manual-page-item]'));
  const manualQuestionPaginationSummary = document.getElementById('manualQuestionPaginationSummary');
  const saveConfigTopButton = document.getElementById('saveConfigTop');
  const saveConfigBottomButton = document.getElementById('saveConfigBottom');
  const examTitleInput = document.getElementById('examTitleInput');
  const examDescriptionInput = document.getElementById('examDescriptionInput');
  const examMaxAttemptsInput = document.getElementById('examMaxAttemptsInput');
  const examPaperCountInput = document.getElementById('examPaperCountInput');
  const examVisibleFromInput = document.getElementById('examVisibleFromInput');
  const examOpenAtInput = document.getElementById('examOpenAtInput');
  const examCloseAtInput = document.getElementById('examCloseAtInput');
  const examDurationInput = document.getElementById('examDurationInput');
  const showScoreToggle = document.getElementById('showScore');
  const shuffleQuestionToggle = document.getElementById('rule1');
  const allowLateSubmissionToggle = document.getElementById('rule3');

  if (!publicToggle || !classTable || !classSection) {
    return;
  }

  const classPageSize = 5;
  const blueprintPageSize = 10;
  const manualQuestionPageSize = 10;
  let classCurrentPage = 1;
  let blueprintCurrentPage = 1;
  let classTotalPages = 1;
  let selectedBlueprintId = null;
  let manualQuestionCurrentPage = 1;
  let manualQuestionTotalPages = 1;
  let selectedClassId = null;
  const classSubjectById = new Map();
  const manualSelectedQuestionIds = new Set();

  const normalizeText = (value = '') => value.toLowerCase().trim();
  const teacherIdFromQuery = new URLSearchParams(window.location.search).get('teacherId');
  const teacherIdRaw = teacherIdFromQuery || window.ASSIGN_EXAM_TEACHER_ID || '';
  let teacherIdNumber = Number(teacherIdRaw);
  if (!Number.isInteger(teacherIdNumber) || teacherIdNumber <= 0) {
    teacherIdNumber = (typeof getUserIdFromToken === 'function' ? getUserIdFromToken() : null) ?? null;
  }
  if (!Number.isInteger(teacherIdNumber) || teacherIdNumber <= 0) {
    const token = typeof getToken === 'function' ? getToken() : null;
    if (token) {
      const authBases = ['https://localhost:7167', window.location.origin].filter(Boolean);
      for (const base of authBases) {
        try {
          const res = await fetch(`${base.replace(/\/+$/, '')}/api/auth/me`, {
            headers: { Authorization: `Bearer ${token}` }
          });
          if (res.ok) {
            const data = await res.json();
            const id = data?.userId != null ? Number(data.userId) : null;
            if (Number.isInteger(id) && id > 0) teacherIdNumber = id;
            break;
          }
        } catch (_) { /* try next base */ }
      }
    }
  }
  const teacherId = Number.isInteger(teacherIdNumber) && teacherIdNumber > 0 ? teacherIdNumber : null;
  const isHttpsPage = window.location.protocol === 'https:';

  const apiBaseCandidates = (() => {
    const configured = window.ASSIGN_EXAM_API_BASE_URL;
    const candidates = [
      configured,
      'https://localhost:7167/api/assign-exam',
      '/api/assign-exam',
      `${window.location.origin}/api/assign-exam`,
      ...(isHttpsPage ? [] : ['https://localhost:7167/api/assign-exam'])
    ].filter(Boolean);

    return Array.from(new Set(candidates));
  })();

  let resolvedApiBaseUrl = null;

  const toAbsoluteUrl = (base, path, params) => {
    const normalizedBase = (base || '').replace(/\/+$/, '');
    const normalizedPath = path.startsWith('/') ? path : `/${path}`;
    const url = new URL(`${normalizedBase}${normalizedPath}`, window.location.origin);
    Object.entries(params || {}).forEach(([key, value]) => {
      if (value === null || value === undefined || value === '') {
        return;
      }
      url.searchParams.set(key, String(value));
    });
    return url;
  };

  const apiGetJson = async (path, params) => {
    const candidates = resolvedApiBaseUrl
      ? [resolvedApiBaseUrl, ...apiBaseCandidates.filter((x) => x !== resolvedApiBaseUrl)]
      : apiBaseCandidates;
    let lastError = null;

    for (const base of candidates) {
      try {
        const url = toAbsoluteUrl(base, path, params);
        const response = await fetch(url.toString());
        if (!response.ok) {
          throw new Error(`status ${response.status}`);
        }
        resolvedApiBaseUrl = base;
        return await response.json();
      } catch (error) {
        lastError = error;
      }
    }

    throw lastError || new Error('Cannot reach assign exam API.');
  };

  const apiPostJson = async (path, body) => {
    const candidates = resolvedApiBaseUrl
      ? [resolvedApiBaseUrl, ...apiBaseCandidates.filter((x) => x !== resolvedApiBaseUrl)]
      : apiBaseCandidates;
    let lastError = null;

    for (const base of candidates) {
      try {
        const url = toAbsoluteUrl(base, path);
        const response = await fetch(url.toString(), {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(body)
        });

        let json = null;
        try {
          json = await response.json();
        } catch {
          json = null;
        }

        if (!response.ok) {
          const message = json && json.message ? json.message : `status ${response.status}`;
          const httpError = new Error(message);
          httpError.status = response.status;
          throw httpError;
        }

        resolvedApiBaseUrl = base;
        return json;
      } catch (error) {
        if (typeof error?.status === 'number') {
          throw error;
        }
        lastError = error;
      }
    }

    throw lastError || new Error('Cannot reach assign exam API.');
  };

  const toApiDateTime = (value) => {
    if (!value) {
      return null;
    }
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  };

  const toDateOrNull = (value) => {
    if (!value) {
      return null;
    }
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
  };

  const markFieldInvalid = (field, isInvalid) => {
    if (!field) {
      return;
    }
    field.classList.toggle('is-invalid', Boolean(isInvalid));
  };

  const resetValidationState = () => {
    [
      examTitleInput,
      examDurationInput,
      examMaxAttemptsInput,
      examPaperCountInput,
      examVisibleFromInput,
      examOpenAtInput,
      examCloseAtInput,
      publicSubjectFilter
    ].forEach((field) => markFieldInvalid(field, false));
  };

  const getCurrentSubjectId = () => null;

  const saveAssignExam = async () => {
    resetValidationState();

    if (!teacherId) {
      showToast('Thiếu teacherId, không thể lưu giao đề.', 'error');
      return;
    }

    const title = examTitleInput ? examTitleInput.value.trim() : '';
    const duration = examDurationInput ? Number(examDurationInput.value || 0) : 0;
    const maxAttempts = examMaxAttemptsInput ? Number(examMaxAttemptsInput.value || 0) : 0;
    const paperCount = examPaperCountInput ? Number(examPaperCountInput.value || 0) : 1;
    const isPublic = Boolean(publicToggle && publicToggle.checked);
    const checkedClassId = getCheckedClassId();
    const resolvedClassId = !isPublic
      ? (checkedClassId || (Number.isInteger(selectedClassId) && selectedClassId > 0 ? selectedClassId : null))
      : null;
    if (!isPublic && resolvedClassId) {
      selectedClassId = resolvedClassId;
    }
    const generationMode = Boolean(examGenerationManual && examGenerationManual.checked) ? 'manual' : 'blueprint';
    const visibleFromDate = toDateOrNull(examVisibleFromInput ? examVisibleFromInput.value : '');
    const openAtDate = toDateOrNull(examOpenAtInput ? examOpenAtInput.value : '');
    const closeAtDate = toDateOrNull(examCloseAtInput ? examCloseAtInput.value : '');
    const errors = [];

    if (generationMode === 'manual') {
      try {
        const removedCount = await syncManualSelectionWithCurrentFilters();
        if (removedCount > 0) {
          await renderManualTable();
        }
      } catch (error) {
        errors.push('Không thể đồng bộ danh sách câu hỏi thủ công trước khi lưu.');
      }
    }

    if (!title) {
      errors.push('Vui lòng nhập tiêu đề đề thi.');
      markFieldInvalid(examTitleInput, true);
    } else if (title.length > 200) {
      errors.push('Tiêu đề đề thi tối đa 200 ký tự.');
      markFieldInvalid(examTitleInput, true);
    }
    if (!Number.isInteger(duration) || duration <= 0) {
      errors.push('Thời lượng phải là số nguyên lớn hơn 0.');
      markFieldInvalid(examDurationInput, true);
    }
    if (!Number.isInteger(maxAttempts) || maxAttempts <= 0) {
      errors.push('Số lần làm phải là số nguyên lớn hơn 0.');
      markFieldInvalid(examMaxAttemptsInput, true);
    }
    if (!Number.isInteger(paperCount) || paperCount <= 0) {
      errors.push('Số mã đề phải là số nguyên lớn hơn 0.');
      markFieldInvalid(examPaperCountInput, true);
    } else if (paperCount > 50) {
      errors.push('Số mã đề tối đa là 50.');
      markFieldInvalid(examPaperCountInput, true);
    }
    if ((examVisibleFromInput && examVisibleFromInput.value && !visibleFromDate)) {
      errors.push('Thời điểm học sinh thấy đề không hợp lệ.');
      markFieldInvalid(examVisibleFromInput, true);
    }
    if ((examOpenAtInput && examOpenAtInput.value && !openAtDate)) {
      errors.push('Thời điểm mở không hợp lệ.');
      markFieldInvalid(examOpenAtInput, true);
    }
    if ((examCloseAtInput && examCloseAtInput.value && !closeAtDate)) {
      errors.push('Thời điểm đóng không hợp lệ.');
      markFieldInvalid(examCloseAtInput, true);
    }
    if (visibleFromDate && openAtDate && visibleFromDate > openAtDate) {
      errors.push('Thời điểm học sinh thấy đề phải nhỏ hơn hoặc bằng thời điểm mở.');
      markFieldInvalid(examVisibleFromInput, true);
      markFieldInvalid(examOpenAtInput, true);
    }
    if (openAtDate && closeAtDate && openAtDate >= closeAtDate) {
      errors.push('Thời điểm mở phải nhỏ hơn thời điểm đóng.');
      markFieldInvalid(examOpenAtInput, true);
      markFieldInvalid(examCloseAtInput, true);
    }
    if (isPublic && publicSubjectFilter && !publicSubjectFilter.value) {
      errors.push('Vui lòng chọn môn học cho đề public.');
      markFieldInvalid(publicSubjectFilter, true);
    }
    if (!isPublic && !resolvedClassId) {
      errors.push('Vui lòng chọn lớp học trước khi lưu.');
    }
    if (generationMode === 'blueprint' && !selectedBlueprintId) {
      errors.push('Vui lòng chọn ma trận đề trước khi lưu.');
    }
    if (generationMode === 'manual' && manualSelectedQuestionIds.size === 0) {
      errors.push('Vui lòng chọn ít nhất 1 câu hỏi thủ công trước khi lưu.');
    }

    if (errors.length > 0) {
      showToast(errors.join('<br>'), 'error', 5000);
      return;
    }

    const payload = {
      teacherId,
      title,
      description: examDescriptionInput ? examDescriptionInput.value.trim() : '',
      duration,
      showScore: Boolean(showScoreToggle && showScoreToggle.checked),
      showAnswer: false,
      maxAttempts,
      visibleFrom: toApiDateTime(examVisibleFromInput ? examVisibleFromInput.value : ''),
      openAt: toApiDateTime(examOpenAtInput ? examOpenAtInput.value : ''),
      closeAt: toApiDateTime(examCloseAtInput ? examCloseAtInput.value : ''),
      shuffleQuestion: Boolean(shuffleQuestionToggle && shuffleQuestionToggle.checked),
      allowLateSubmission: Boolean(allowLateSubmissionToggle && allowLateSubmissionToggle.checked),
      isPublic,
      classId: resolvedClassId,
      generationMode,
      examBlueprintId: generationMode === 'blueprint' ? selectedBlueprintId : null,
      subjectId: generationMode === 'manual' ? getCurrentSubjectId() : null,
      questionIds: generationMode === 'manual' ? Array.from(manualSelectedQuestionIds).map((x) => Number(x)) : [],
      paperCount,
      paperCode: 1
    };

    const setSavingState = (saving) => {
      if (saveConfigTopButton) saveConfigTopButton.disabled = saving;
      if (saveConfigBottomButton) saveConfigBottomButton.disabled = saving;
    };

    try {
      setSavingState(true);
      const result = await apiPostJson('', payload);
      const createdPapers = Array.isArray(result?.papers) ? result.papers : [];
      const paperCodes = createdPapers
        .map((item) => Number(item?.code))
        .filter((x) => Number.isInteger(x));
      const paperSummary = paperCodes.length > 0
        ? `Mã đề: ${paperCodes.join(', ')}.`
        : `PaperId đầu tiên: ${result.paperId}.`;
      showToast(`Lưu giao đề thành công. ExamId: ${result.examId}, Tổng câu: ${result.totalQuestions}. ${paperSummary}`);
    } catch (error) {
      showToast(`Lưu giao đề thất bại: ${error.message || 'Lỗi không xác định.'}`, 'error');
    } finally {
      setSavingState(false);
    }
  };

  const setSelectOptions = (selectEl, options, placeholderText) => {
    if (!selectEl) {
      return;
    }

    const currentValue = selectEl.value || '';
    selectEl.innerHTML = '';

    const placeholderOption = document.createElement('option');
    placeholderOption.value = '';
    placeholderOption.textContent = placeholderText;
    selectEl.appendChild(placeholderOption);

    options.forEach((opt) => {
      const option = document.createElement('option');
      option.value = opt.value;
      option.textContent = opt.label;
      selectEl.appendChild(option);
    });

    if (currentValue && options.some((x) => x.value === currentValue)) {
      selectEl.value = currentValue;
    }
  };

  const ensureSelectHasOption = (selectEl, value, label) => {
    if (!selectEl || !value) {
      return;
    }
    const exists = Array.from(selectEl.options || []).some((opt) => opt.value === value);
    if (exists) {
      return;
    }
    const option = document.createElement('option');
    option.value = value;
    option.textContent = label || value;
    selectEl.appendChild(option);
  };

  const applySubjectFilters = (subjects) => {
    const subjectOptions = subjects
      .map((item) => ({
        value: item.code || '',
        label: item.code ? `${item.code}` : item.name
      }))
      .filter((item) => item.value.length > 0);

    setSelectOptions(classSubjectFilter, subjectOptions, 'Tất cả môn');
    setSelectOptions(manualQuestionSubjectFilter, subjectOptions, 'Tất cả môn');
  };

  const applySemesterFilters = (semesters) => {
    const semesterOptions = semesters.map((x) => ({ value: x, label: x }));
    setSelectOptions(classSemesterFilter, semesterOptions, 'Tất cả học kỳ');
  };

  const showTeacherIdRequiredState = () => {
    if (classTableBody) {
      classTableBody.innerHTML = '<tr><td colspan="4" class="text-center text-warning">Vui lòng truyền teacherId để xem lớp đang dạy.</td></tr>';
    }
    if (classPaginationSummary) {
      classPaginationSummary.textContent = 'Thiếu teacherId: chưa thể tải danh sách lớp.';
    }
    if (selectedCount) {
      selectedCount.textContent = 'Chưa chọn lớp học';
    }
    classTotalPages = 1;
    updateClassPagination();
    if (publicSubjectFilter) {
      setSelectOptions(publicSubjectFilter, publicSubjectOptions, 'Chọn môn học');
      publicSubjectFilter.disabled = false;
    }
  };

  const loadFilterOptions = async () => {
    if (!teacherId) {
      showTeacherIdRequiredState();
      return;
    }

    try {
      const data = await apiGetJson('/filters', { teacherId });
      applySubjectFilters(Array.isArray(data?.subjects) ? data.subjects : []);
      if (publicSubjectFilter) {
        setSelectOptions(publicSubjectFilter, publicSubjectOptions, 'Chọn môn học');
      }
      applySemesterFilters(Array.isArray(data?.semesters) ? data.semesters : []);
      if (publicSubjectFilter) {
        publicSubjectFilter.disabled = false;
      }

      renderClassTable(true);
      renderBlueprintTable(true);
      await syncManualChapterOptions();
      await renderManualTable();
      syncPublicSubjectFilters();
    } catch (error) {
      console.error('Failed to load assign exam filters:', error);
      showTeacherIdRequiredState();
    }
  };

  const difficultyLabelByValue = {
    1: 'Nhận biết',
    2: 'Thông hiểu',
    3: 'Vận dụng',
    4: 'Vận dụng cao'
  };
  const difficultyValueByLabel = {
    'nhận biết': 1,
    'thông hiểu': 2,
    'vận dụng': 3,
    'vận dụng cao': 4
  };
  const chapterCache = new Map();
  let manualQuestionData = [];
  const publicSubjectOptions = [
    { value: 'MAE', label: 'MAE' },
    { value: 'MAS', label: 'MAS' },
    { value: 'MAD', label: 'MAD' }
  ];

  const classRows = () => Array.from(classTable.querySelectorAll('tbody tr'));
  const classSelectors = () => Array.from(classTable.querySelectorAll('.class-item'));
  const getCheckedClassId = () => {
    const selectedInput = classSelectors().find((item) => item.checked);
    if (!selectedInput) {
      return null;
    }
    const value = Number(selectedInput.value);
    return Number.isInteger(value) && value > 0 ? value : null;
  };
  const blueprintRows = () => blueprintListTable ? Array.from(blueprintListTable.querySelectorAll('tbody tr')) : [];
  const blueprintSelectors = () => blueprintListTable ? Array.from(blueprintListTable.querySelectorAll('.blueprint-item')) : [];

  const toDateDisplay = (input) => {
    const date = input ? new Date(input) : null;
    if (!date || Number.isNaN(date.getTime())) {
      return '-';
    }
    return new Intl.DateTimeFormat('vi-VN').format(date);
  };

  const mapDifficultyLabel = (value) => difficultyLabelByValue[value] || `Mức ${value}`;

  const loadBlueprintsFromApi = async (subjectCode, keyword) => {
    const data = await apiGetJson('/blueprints', {
      teacherId,
      subjectCode: subjectCode ?? (blueprintSubjectFilter ? blueprintSubjectFilter.value : ''),
      keyword: keyword ?? (blueprintSearchInput ? blueprintSearchInput.value : '')
    });
    return Array.isArray(data) ? data : [];
  };

  const loadBlueprintDetailFromApi = async (blueprintId) => {
    if (!blueprintId) {
      return [];
    }
    const data = await apiGetJson(`/blueprints/${blueprintId}/detail`);
    return Array.isArray(data) ? data : [];
  };

  const updateSelectedClass = () => {
    if (!selectedCount) return;
    const selectedInput = classSelectors().find((item) => item.checked);
    const selectedRow = selectedInput ? selectedInput.closest('tr') : null;
    const selectedCode = selectedRow ? selectedRow.getAttribute('data-class-code') || '' : '';
    selectedCount.textContent = selectedCode ? `Đã chọn: ${selectedCode}` : 'Chưa chọn lớp học';
  };

  const getSelectedClassSubjectCode = () => {
    if (selectedClassId && classSubjectById.has(Number(selectedClassId))) {
      return classSubjectById.get(Number(selectedClassId)) || '';
    }

    const selectedInput = classSelectors().find((item) => item.checked);
    const selectedRow = selectedInput ? selectedInput.closest('tr') : null;
    return selectedRow ? (selectedRow.getAttribute('data-subject') || '') : '';
  };

  const syncSubjectFiltersBySelectedClass = async () => {
    const isPublic = Boolean(publicToggle && publicToggle.checked);
    if (isPublic) {
      return;
    }

    const subjectCode = getSelectedClassSubjectCode();
    if (!subjectCode) {
      if (blueprintSubjectFilter) blueprintSubjectFilter.disabled = false;
      if (manualQuestionSubjectFilter) manualQuestionSubjectFilter.disabled = false;
      return;
    }

    if (blueprintSubjectFilter) {
      ensureSelectHasOption(blueprintSubjectFilter, subjectCode, subjectCode);
      blueprintSubjectFilter.value = subjectCode;
      blueprintSubjectFilter.disabled = true;
    }
    if (manualQuestionSubjectFilter) {
      ensureSelectHasOption(manualQuestionSubjectFilter, subjectCode, subjectCode);
      manualQuestionSubjectFilter.value = subjectCode;
      manualQuestionSubjectFilter.disabled = true;
    }
    if (manualQuestionChapterFilter) {
      manualQuestionChapterFilter.value = '';
    }

    await renderBlueprintTable(true);
    await syncManualChapterOptions();
    await renderManualTable(true);
  };

  const bindClassSelection = () => {
    classSelectors().forEach((item) => {
      item.addEventListener('change', async () => {
        selectedClassId = Number(item.value);
        const row = item.closest('tr');
        const subject = row ? (row.getAttribute('data-subject') || '') : '';
        if (selectedClassId && subject) {
          classSubjectById.set(selectedClassId, subject);
        }
        updateSelectedClass();
        await syncSubjectFiltersBySelectedClass();
      });
    });
  };

  const applyPublicMode = () => {
    const hideClass = Boolean(publicToggle.checked);
    const block = classSelectionBlock || classSection;
    if (!block) return;
    block.classList.toggle('d-none', hideClass);
    block.style.display = hideClass ? 'none' : '';
    block.setAttribute('aria-hidden', hideClass ? 'true' : 'false');

    if (publicSubjectSection) {
      publicSubjectSection.classList.toggle('d-none', !hideClass);
    }

    // In public mode, centralize subject choice at publicSubjectFilter.
    if (hideClass) {
      if (blueprintSubjectFilter) {
        blueprintSubjectFilter.disabled = true;
      }
      if (manualQuestionSubjectFilter) {
        manualQuestionSubjectFilter.disabled = true;
      }
      syncPublicSubjectFilters();
      return;
    }

    syncSubjectFiltersBySelectedClass();
  };

  const syncPublicSubjectFilters = async () => {
    const isPublic = Boolean(publicToggle.checked);
    if (!isPublic) {
      return;
    }

    const value = publicSubjectFilter ? publicSubjectFilter.value : '';
    if (blueprintSubjectFilter) {
      ensureSelectHasOption(blueprintSubjectFilter, value, value);
      blueprintSubjectFilter.value = value;
    }
    if (manualQuestionSubjectFilter) {
      ensureSelectHasOption(manualQuestionSubjectFilter, value, value);
      manualQuestionSubjectFilter.value = value;
    }

    renderBlueprintTable(true);
    await syncManualChapterOptions();
    await renderManualTable();
  };

  const applyExamGenerationMode = () => {
    const useManual = Boolean(examGenerationManual && examGenerationManual.checked);
    if (blueprintModeSection) {
      blueprintModeSection.hidden = useManual;
      blueprintModeSection.classList.toggle('d-none', useManual);
    }
    if (manualModeSection) {
      manualModeSection.hidden = !useManual;
      manualModeSection.classList.toggle('d-none', !useManual);
    }
    if (examGenerationModeDivider) {
      examGenerationModeDivider.classList.add('d-none');
    }
  };

  const updateClassPagination = () => {
    if (classPaginationPrev) classPaginationPrev.disabled = classCurrentPage <= 1;
    if (classPaginationNext) classPaginationNext.disabled = classCurrentPage >= classTotalPages;

    const firstPage = classCurrentPage > 1 ? classCurrentPage - 1 : 1;
    classPageButtons.forEach((button, idx) => {
      const page = firstPage + idx;
      const pageItem = classPageItems[idx];
      const enabled = page <= classTotalPages;
      button.disabled = !enabled;
      button.dataset.classPage = enabled ? String(page) : '';
      button.textContent = enabled ? String(page) : '-';
      if (pageItem) {
        pageItem.classList.toggle('active', enabled && page === classCurrentPage);
        pageItem.classList.toggle('disabled', !enabled);
      }
    });
  };

  const renderClassTable = async (resetPage = false) => {
    if (!classTableBody) return;
    if (resetPage) classCurrentPage = 1;
    if (!teacherId) {
      showTeacherIdRequiredState();
      return;
    }

    try {
      const data = await apiGetJson('/classes', {
        teacherId,
        keyword: classSearchInput ? classSearchInput.value : '',
        subjectCode: classSubjectFilter ? classSubjectFilter.value : '',
        semester: classSemesterFilter ? classSemesterFilter.value : '',
        page: classCurrentPage,
        pageSize: classPageSize
      });
      const items = Array.isArray(data?.items) ? data.items : [];
      const totalItems = Number(data?.totalItems || 0);
      classTotalPages = Math.max(1, Math.ceil(totalItems / classPageSize));
      if (classCurrentPage > classTotalPages) {
        classCurrentPage = classTotalPages;
      }

      if (!selectedClassId && items.length > 0) {
        selectedClassId = Number(items[0].classId);
      }

      classSubjectById.clear();
      items.forEach((item) => {
        const classId = Number(item.classId);
        const subjectCode = item.subjectCode || '';
        if (classId > 0 && subjectCode) {
          classSubjectById.set(classId, subjectCode);
        }
      });

      classTableBody.innerHTML = items.map((item) => `
        <tr data-class-code="${item.classCode}" data-subject="${item.subjectCode}" data-semester="${item.semester}">
          <td><input class="form-check-input class-item" type="radio" name="selectedClass" value="${item.classId}" ${Number(item.classId) === selectedClassId ? 'checked' : ''}></td>
          <td>${item.classCode}</td>
          <td>${item.subjectCode}</td>
          <td>${item.semester}</td>
        </tr>
      `).join('');

      const start = totalItems === 0 ? 0 : ((classCurrentPage - 1) * classPageSize) + 1;
      const end = totalItems === 0 ? 0 : start + items.length - 1;
      if (classPaginationSummary) {
        classPaginationSummary.textContent = totalItems === 0
          ? 'Không có lớp học phù hợp bộ lọc.'
          : `Hiển thị ${start}-${end} trên ${totalItems} lớp`;
      }

      bindClassSelection();
      updateSelectedClass();
      updateClassPagination();
      await syncSubjectFiltersBySelectedClass();
    } catch (error) {
      console.error('Failed to load classes:', error);
      classTableBody.innerHTML = '<tr><td colspan="4" class="text-center text-danger">Không tải được danh sách lớp.</td></tr>';
      if (classPaginationSummary) {
        classPaginationSummary.textContent = 'Không tải được dữ liệu lớp học.';
      }
    }
  };

  const renderBlueprintMatrix = (matrixRows) => {
    if (!blueprintMatrixBody) return;
    if (!matrixRows || matrixRows.length === 0) {
      blueprintMatrixBody.innerHTML = '<tr><td colspan="3" class="text-center text-muted">Không có dữ liệu ma trận đề.</td></tr>';
      return;
    }
    blueprintMatrixBody.innerHTML = matrixRows.map((x) => `
      <tr>
        <td>${x.chapterName}</td>
        <td>${mapDifficultyLabel(Number(x.difficulty))}</td>
        <td>${x.totalOfQuestions}</td>
      </tr>
    `).join('');
  };

  const updateBlueprintDetail = async () => {
    const selectedInput = blueprintSelectors().find((item) => item.checked);
    const row = selectedInput ? selectedInput.closest('tr') : null;
    const blueprintId = row ? Number(row.getAttribute('data-blueprint-id') || 0) : 0;
    if (!blueprintId) {
      if (selectedBlueprintName) selectedBlueprintName.textContent = 'Không có ma trận đề phù hợp';
      if (blueprintInfoName) blueprintInfoName.textContent = '-';
      if (blueprintInfoUpdated) blueprintInfoUpdated.textContent = '-';
      if (blueprintInfoSubject) blueprintInfoSubject.textContent = '-';
      if (blueprintInfoQuestionCount) blueprintInfoQuestionCount.textContent = '0 câu';
      renderBlueprintMatrix([]);
      return;
    }

    const blueprintName = row ? row.getAttribute('data-blueprint-name') || '-' : '-';
    const blueprintSubject = row ? row.getAttribute('data-subject') || '-' : '-';
    const questionCount = row ? Number(row.getAttribute('data-total-questions') || 0) : 0;
    const updatedDisplay = row ? row.getAttribute('data-updated-display') || '-' : '-';

    selectedBlueprintId = blueprintId;
    if (selectedBlueprintName) selectedBlueprintName.textContent = `Đang chọn: ${blueprintName}`;
    if (blueprintInfoName) blueprintInfoName.textContent = blueprintName;
    if (blueprintInfoUpdated) blueprintInfoUpdated.textContent = updatedDisplay;
    if (blueprintInfoSubject) blueprintInfoSubject.textContent = blueprintSubject;
    if (blueprintInfoQuestionCount) blueprintInfoQuestionCount.textContent = `${questionCount} câu`;

    try {
      const matrixRows = await loadBlueprintDetailFromApi(blueprintId);
      renderBlueprintMatrix(matrixRows);
    } catch (error) {
      console.error('Failed to load blueprint detail:', error);
      renderBlueprintMatrix([]);
    }
  };

  const renderBlueprintTable = async (resetPage = false) => {
    if (!blueprintListTable || !blueprintTableBody) return;
    if (resetPage) blueprintCurrentPage = 1;

    try {
      const allBlueprints = await loadBlueprintsFromApi('', '');
      const blueprintSubjectOptions = Array.from(new Set(allBlueprints
        .map((x) => (x?.subjectCode || '').trim())
        .filter((x) => x.length > 0)))
        .sort((a, b) => a.localeCompare(b))
        .map((code) => ({ value: code, label: code }));
      setSelectOptions(blueprintSubjectFilter, blueprintSubjectOptions, 'Tất cả môn');

      const selectedSubject = normalizeText(blueprintSubjectFilter ? blueprintSubjectFilter.value : '');
      const keyword = normalizeText(blueprintSearchInput ? blueprintSearchInput.value : '');
      const items = allBlueprints.filter((item) => {
        const itemSubject = normalizeText(item?.subjectCode || '');
        const itemName = normalizeText(item?.name || '');
        return (selectedSubject.length === 0 || itemSubject === selectedSubject) &&
          (keyword.length === 0 || itemName.includes(keyword));
      });

      const totalRows = items.length;
      const totalPages = Math.max(1, Math.ceil(totalRows / blueprintPageSize));
      blueprintCurrentPage = Math.min(blueprintCurrentPage, totalPages);
      const start = totalRows === 0 ? 0 : (blueprintCurrentPage - 1) * blueprintPageSize;
      const end = Math.min(start + blueprintPageSize, totalRows);
      const visible = items.slice(start, end);

      blueprintTableBody.innerHTML = visible.map((item) => `
        <tr class="interactive-row"
            data-blueprint-id="${item.examBlueprintId}"
            data-blueprint-name="${item.name}"
            data-subject="${item.subjectCode}"
            data-total-questions="${item.totalQuestions}"
            data-updated-display="${toDateDisplay(item.updatedAtUtc)}">
          <td><input class="form-check-input blueprint-item" type="radio" name="selectedBlueprint" value="${item.examBlueprintId}" ${Number(item.examBlueprintId) === Number(selectedBlueprintId) ? 'checked' : ''}></td>
          <td>${item.name}</td>
          <td>${item.subjectCode}</td>
          <td>${toDateDisplay(item.updatedAtUtc)}</td>
        </tr>
      `).join('');

      if (blueprintFilterSummary) {
        const selectedSubjectLabel = blueprintSubjectFilter && blueprintSubjectFilter.value
          ? ` cho môn ${blueprintSubjectFilter.value}`
          : '';
        blueprintFilterSummary.textContent = totalRows === 0
          ? `Không có ma trận đề${selectedSubjectLabel}.`
          : `Hiển thị ${start + 1}-${end} trên ${totalRows} ma trận đề.`;
      }

      const shouldShowPaging = totalRows > blueprintPageSize;
      blueprintPageButtons.forEach((button, idx) => {
        const pageNumber = idx + 1;
        const pageItem = blueprintPageItems[idx];
        const enabled = shouldShowPaging && pageNumber <= totalPages;
        const active = enabled && pageNumber === blueprintCurrentPage;
        button.disabled = !enabled;
        if (pageItem) {
          pageItem.classList.toggle('d-none', !shouldShowPaging);
          pageItem.classList.toggle('active', active);
          pageItem.classList.toggle('disabled', !enabled);
        }
      });

      if (blueprintPaginationPrev) {
        const prevItem = blueprintPaginationPrev.closest('.page-item');
        if (prevItem) prevItem.classList.toggle('d-none', !shouldShowPaging);
        blueprintPaginationPrev.disabled = !shouldShowPaging || blueprintCurrentPage <= 1;
      }
      if (blueprintPaginationNext) {
        const nextItem = blueprintPaginationNext.closest('.page-item');
        if (nextItem) nextItem.classList.toggle('d-none', !shouldShowPaging);
        blueprintPaginationNext.disabled = !shouldShowPaging || blueprintCurrentPage >= totalPages;
      }

      const selectedExists = items.some((x) => Number(x.examBlueprintId) === Number(selectedBlueprintId));
      if (!selectedExists) {
        selectedBlueprintId = totalRows > 0 ? Number(items[0].examBlueprintId) : null;
      }

      const selectedInput = blueprintSelectors().find((item) => Number(item.value) === Number(selectedBlueprintId));
      if (selectedInput) {
        selectedInput.checked = true;
      } else if (blueprintSelectors().length > 0) {
        blueprintSelectors()[0].checked = true;
      }

      blueprintSelectors().forEach((item) => item.addEventListener('change', () => {
        selectedBlueprintId = Number(item.value);
        updateBlueprintDetail();
      }));

      await updateBlueprintDetail();
    } catch (error) {
      console.error('Failed to load blueprints:', error);
      blueprintTableBody.innerHTML = '<tr><td colspan="4" class="text-center text-danger">Không tải được danh sách ma trận đề.</td></tr>';
      if (blueprintFilterSummary) {
        blueprintFilterSummary.textContent = 'Không tải được dữ liệu ma trận đề.';
      }
      if (selectedBlueprintName) selectedBlueprintName.textContent = 'Không có ma trận đề phù hợp';
      if (blueprintInfoName) blueprintInfoName.textContent = '-';
      if (blueprintInfoUpdated) blueprintInfoUpdated.textContent = '-';
      if (blueprintInfoSubject) blueprintInfoSubject.textContent = '-';
      if (blueprintInfoQuestionCount) blueprintInfoQuestionCount.textContent = '0 câu';
      renderBlueprintMatrix([]);
    }
  };

  const updateManualSummary = () => {
    if (!manualQuestionSummary) return;
    manualQuestionSummary.textContent = `Đã chọn ${manualSelectedQuestionIds.size} câu hỏi thủ công.`;
  };

  const pruneManualSelection = (allowedIds) => {
    const allowed = new Set((allowedIds || []).map((id) => String(id)));
    let removedCount = 0;
    Array.from(manualSelectedQuestionIds).forEach((id) => {
      if (!allowed.has(String(id))) {
        manualSelectedQuestionIds.delete(id);
        removedCount += 1;
      }
    });
    return removedCount;
  };

  const syncManualSelectionWithCurrentFilters = async () => {
    const rows = await loadManualQuestionsFromApi();
    const allowedIds = rows.map((item) => String(item.questionId));
    const removedCount = pruneManualSelection(allowedIds);
    if (removedCount > 0) {
      updateManualSummary();
    }
    return removedCount;
  };

  const updateManualPagination = (totalItems, visibleItemsCount) => {
    manualQuestionTotalPages = Math.max(1, Math.ceil(totalItems / manualQuestionPageSize));
    if (manualQuestionCurrentPage > manualQuestionTotalPages) {
      manualQuestionCurrentPage = manualQuestionTotalPages;
    }

    if (manualQuestionPaginationPrev) {
      manualQuestionPaginationPrev.disabled = manualQuestionCurrentPage <= 1;
    }
    if (manualQuestionPaginationNext) {
      manualQuestionPaginationNext.disabled = manualQuestionCurrentPage >= manualQuestionTotalPages;
    }

    const firstPage = manualQuestionCurrentPage > 1 ? manualQuestionCurrentPage - 1 : 1;
    manualQuestionPageButtons.forEach((button, idx) => {
      const page = firstPage + idx;
      const pageItem = manualQuestionPageItems[idx];
      const enabled = page <= manualQuestionTotalPages;
      button.disabled = !enabled;
      button.dataset.manualPage = enabled ? String(page) : '';
      button.textContent = enabled ? String(page) : '-';
      if (pageItem) {
        pageItem.classList.toggle('active', enabled && page === manualQuestionCurrentPage);
        pageItem.classList.toggle('disabled', !enabled);
      }
    });

    if (!manualQuestionPaginationSummary) {
      return;
    }

    const start = totalItems === 0 ? 0 : ((manualQuestionCurrentPage - 1) * manualQuestionPageSize) + 1;
    const end = totalItems === 0 ? 0 : start + visibleItemsCount - 1;
    manualQuestionPaginationSummary.textContent = totalItems === 0
      ? 'Không có câu hỏi phù hợp bộ lọc.'
      : `Hiển thị ${start}-${end} trên ${totalItems} câu hỏi.`;
  };

  const escapeHtml = (value = '') => value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll('\'', '&#39;');

  const renderManualQuestionContent = (content) => {
    const raw = (content || '').trim();
    if (!raw) {
      return '<span class="text-muted">-</span>';
    }

    const latexPattern = /\$([^$]+)\$/g;
    const parts = [];
    let lastIndex = 0;
    let hasLatex = false;

    for (let match = latexPattern.exec(raw); match !== null; match = latexPattern.exec(raw)) {
      hasLatex = true;
      const plain = raw.slice(lastIndex, match.index);
      if (plain.length > 0) {
        parts.push(`<span class="manual-question-text">${escapeHtml(plain)}</span>`);
      }

      const latex = (match[1] || '').trim();
      if (latex.length > 0) {
        parts.push(`<math-field class="manual-question-math" read-only>${escapeHtml(latex)}</math-field>`);
      }
      lastIndex = match.index + match[0].length;
    }

    const tail = raw.slice(lastIndex);
    if (tail.length > 0) {
      parts.push(`<span class="manual-question-text">${escapeHtml(tail)}</span>`);
    }

    if (!hasLatex) {
      return `<span class="manual-question-text">${escapeHtml(raw)}</span>`;
    }

    return `<span class="manual-question-content">${parts.join('')}</span>`;
  };

  const hydrateManualMathFields = () => {
    if (!manualQuestionTableBody) {
      return;
    }

    manualQuestionTableBody.querySelectorAll('math-field.manual-question-math').forEach((field) => {
      field.setAttribute('read-only', '');
      field.setAttribute('virtual-keyboard-mode', 'off');
      field.setAttribute('smart-mode', 'false');
      field.setAttribute('menu-items', '[]');
    });
  };

  const parseDifficultyFilterValue = () => {
    const raw = manualQuestionLevelFilter ? manualQuestionLevelFilter.value : '';
    if (!raw) {
      return null;
    }

    const numeric = Number(raw);
    if (Number.isInteger(numeric) && numeric > 0) {
      return numeric;
    }

    const normalized = normalizeText(raw);
    return difficultyValueByLabel[normalized] || null;
  };

  const getEffectiveManualSubjectCode = () => {
    const isPublic = Boolean(publicToggle && publicToggle.checked);
    if (isPublic && publicSubjectFilter && publicSubjectFilter.value) {
      return publicSubjectFilter.value;
    }
    return manualQuestionSubjectFilter ? manualQuestionSubjectFilter.value : '';
  };

  const loadManualQuestionsFromApi = async () => {
    const chapterIdRaw = manualQuestionChapterFilter ? manualQuestionChapterFilter.value : '';
    const chapterId = chapterIdRaw ? Number(chapterIdRaw) : null;
    const difficulty = parseDifficultyFilterValue();
    const subjectCode = getEffectiveManualSubjectCode();

    const data = await apiGetJson('/questions', {
      teacherId: teacherId || undefined,
      subjectCode: subjectCode || undefined,
      chapterId: Number.isInteger(chapterId) && chapterId > 0 ? chapterId : undefined,
      difficulty: difficulty || undefined
    });

    return Array.isArray(data) ? data : [];
  };

  const loadManualChapterOptions = async () => {
    const subjectCode = getEffectiveManualSubjectCode();
    const cacheKey = subjectCode || '__all__';
    if (chapterCache.has(cacheKey)) {
      return chapterCache.get(cacheKey);
    }

    const data = await apiGetJson('/questions', {
      teacherId: teacherId || undefined,
      subjectCode: subjectCode || undefined
    });
    const rows = Array.isArray(data) ? data : [];
    const chapterMap = new Map();
    rows.forEach((item) => {
      if (!item || !item.chapterId || !item.chapterName) {
        return;
      }
      chapterMap.set(item.chapterId, item.chapterName);
    });

    const options = Array.from(chapterMap.entries())
      .map(([value, label]) => ({ value: String(value), label }))
      .sort((a, b) => a.label.localeCompare(b.label));
    chapterCache.set(cacheKey, options);
    return options;
  };

  const syncManualChapterOptions = async () => {
    if (!manualQuestionChapterFilter || !manualQuestionSubjectFilter) return;

    const options = await loadManualChapterOptions();
    const current = manualQuestionChapterFilter.value;
    manualQuestionChapterFilter.innerHTML = '<option value="">Tất cả chương</option>';
    options.forEach((chapter) => {
      const option = document.createElement('option');
      option.value = chapter.value;
      option.textContent = chapter.label;
      manualQuestionChapterFilter.appendChild(option);
    });
    if (current && options.some((x) => x.value === current)) {
      manualQuestionChapterFilter.value = current;
    }
  };

  const renderManualTable = async (resetPage = false) => {
    if (!manualQuestionTableBody) return;
    if (resetPage) {
      manualQuestionCurrentPage = 1;
    }

    try {
      const rows = await loadManualQuestionsFromApi();
      manualQuestionData = rows.map((item) => ({
        id: String(item.questionId),
        subject: item.subjectCode || '',
        chapterId: item.chapterId,
        chapter: item.chapterName || '',
        level: difficultyLabelByValue[item.difficulty] || `Mức ${item.difficulty}`,
        content: item.contentLatex || ''
      }));
      pruneManualSelection(manualQuestionData.map((item) => item.id));
    } catch (error) {
      console.error('Failed to load manual questions:', error);
      manualQuestionData = [];
      manualQuestionTableBody.innerHTML = '<tr><td colspan="5" class="text-center text-danger">Không tải được danh sách câu hỏi.</td></tr>';
      updateManualPagination(0, 0);
      updateManualSummary();
      return;
    }

    if (manualQuestionData.length === 0) {
      const selectedSubject = getEffectiveManualSubjectCode();
      manualQuestionTableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">Không có câu hỏi phù hợp bộ lọc.</td></tr>';
      if (manualQuestionPaginationSummary && selectedSubject) {
        manualQuestionPaginationSummary.textContent = `Không có câu hỏi cho môn ${selectedSubject}.`;
      }
      updateManualPagination(0, 0);
      updateManualSummary();
      return;
    }

    const totalItems = manualQuestionData.length;
    manualQuestionTotalPages = Math.max(1, Math.ceil(totalItems / manualQuestionPageSize));
    if (manualQuestionCurrentPage > manualQuestionTotalPages) {
      manualQuestionCurrentPage = manualQuestionTotalPages;
    }
    const start = (manualQuestionCurrentPage - 1) * manualQuestionPageSize;
    const pageItems = manualQuestionData.slice(start, start + manualQuestionPageSize);

    manualQuestionTableBody.innerHTML = pageItems.map((item) => `
      <tr>
        <td><input class="form-check-input manual-question-item" type="checkbox" data-question-id="${item.id}" ${manualSelectedQuestionIds.has(item.id) ? 'checked' : ''}></td>
        <td>${item.subject}</td>
        <td>${item.chapter}</td>
        <td>${item.level}</td>
        <td>${renderManualQuestionContent(item.content)}</td>
      </tr>
    `).join('');
    hydrateManualMathFields();

    manualQuestionTableBody.querySelectorAll('.manual-question-item').forEach((checkbox) => {
      checkbox.addEventListener('change', (event) => {
        const id = event.target.getAttribute('data-question-id');
        if (!id) return;
        if (event.target.checked) manualSelectedQuestionIds.add(id);
        else manualSelectedQuestionIds.delete(id);
        updateManualSummary();
      });
    });
    updateManualPagination(totalItems, pageItems.length);
    updateManualSummary();
  };

  if (classSearchInput) classSearchInput.addEventListener('input', () => renderClassTable(true));
  if (classSubjectFilter) classSubjectFilter.addEventListener('change', () => renderClassTable(true));
  if (classSemesterFilter) classSemesterFilter.addEventListener('change', () => renderClassTable(true));
  if (classPaginationPrev) classPaginationPrev.addEventListener('click', () => {
    if (classCurrentPage > 1) {
      classCurrentPage -= 1;
      renderClassTable();
    }
  });
  if (classPaginationNext) classPaginationNext.addEventListener('click', () => {
    if (classCurrentPage < classTotalPages) {
      classCurrentPage += 1;
      renderClassTable();
    }
  });
  classPageButtons.forEach((button) => button.addEventListener('click', () => {
    const page = Number(button.dataset.classPage || 0);
    if (page > 0) {
      classCurrentPage = page;
      renderClassTable();
    }
  }));

  if (blueprintSearchInput) blueprintSearchInput.addEventListener('input', () => renderBlueprintTable(true));
  if (blueprintSubjectFilter) blueprintSubjectFilter.addEventListener('change', () => renderBlueprintTable(true));
  if (blueprintPaginationPrev) blueprintPaginationPrev.addEventListener('click', () => {
    if (blueprintCurrentPage > 1) {
      blueprintCurrentPage -= 1;
      renderBlueprintTable();
    }
  });
  if (blueprintPaginationNext) blueprintPaginationNext.addEventListener('click', () => {
    blueprintCurrentPage += 1;
    renderBlueprintTable();
  });
  blueprintPageButtons.forEach((button, idx) => button.addEventListener('click', () => {
    blueprintCurrentPage = idx + 1;
    renderBlueprintTable();
  }));

  publicToggle.addEventListener('change', applyPublicMode);
  publicToggle.addEventListener('input', applyPublicMode);
  if (examGenerationBlueprint) examGenerationBlueprint.addEventListener('change', applyExamGenerationMode);
  if (examGenerationManual) examGenerationManual.addEventListener('change', applyExamGenerationMode);

  if (manualQuestionSubjectFilter) manualQuestionSubjectFilter.addEventListener('change', async () => {
    if (manualQuestionChapterFilter) {
      manualQuestionChapterFilter.value = '';
    }
    await syncManualChapterOptions();
    await renderManualTable(true);
  });
  if (manualQuestionChapterFilter) manualQuestionChapterFilter.addEventListener('change', () => renderManualTable(true));
  if (manualQuestionLevelFilter) manualQuestionLevelFilter.addEventListener('change', () => renderManualTable(true));
  if (manualQuestionSearchInput) manualQuestionSearchInput.addEventListener('input', () => renderManualTable(true));
  if (manualQuestionClearButton) manualQuestionClearButton.addEventListener('click', () => { manualSelectedQuestionIds.clear(); renderManualTable(); });
  if (manualQuestionPaginationPrev) manualQuestionPaginationPrev.addEventListener('click', () => {
    if (manualQuestionCurrentPage > 1) {
      manualQuestionCurrentPage -= 1;
      renderManualTable();
    }
  });
  if (manualQuestionPaginationNext) manualQuestionPaginationNext.addEventListener('click', () => {
    if (manualQuestionCurrentPage < manualQuestionTotalPages) {
      manualQuestionCurrentPage += 1;
      renderManualTable();
    }
  });
  manualQuestionPageButtons.forEach((button) => button.addEventListener('click', () => {
    const page = Number(button.dataset.manualPage || 0);
    if (page > 0) {
      manualQuestionCurrentPage = page;
      renderManualTable();
    }
  }));
  if (publicSubjectFilter) publicSubjectFilter.addEventListener('change', syncPublicSubjectFilters);
  if (saveConfigTopButton) saveConfigTopButton.addEventListener('click', saveAssignExam);
  if (saveConfigBottomButton) saveConfigBottomButton.addEventListener('click', saveAssignExam);

  updateSelectedClass();
  applyPublicMode();
  applyExamGenerationMode();
  renderClassTable(true);
  renderBlueprintTable(true);
  syncManualChapterOptions();
  renderManualTable(true);
  loadFilterOptions();
})();
