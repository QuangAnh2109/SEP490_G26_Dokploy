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
    if (!node) return false;
    if (node.closest('.d-none, [hidden]')) return false;
    const style = window.getComputedStyle(node);
    if (style.display === 'none' || style.visibility === 'hidden') return false;
    return node.getClientRects().length > 0;
  };

  const getMathFieldValue = (field) => {
    if (!field) return '';
    if (typeof field.getValue === 'function') {
      const latex = field.getValue('latex');
      return typeof latex === 'string' ? latex : '';
    }
    if (typeof field.value === 'string') return field.value;
    return field.textContent || '';
  };

  const hasPromptAnswer = (field) => {
    if (!field || typeof field.getPrompts !== 'function' || typeof field.getPromptValue !== 'function') return null;
    const prompts = field.getPrompts();
    if (!Array.isArray(prompts) || prompts.length === 0) return null;
    return prompts.some((promptId) => {
      const value = field.getPromptValue(promptId);
      return typeof value === 'string' ? value.trim().length > 0 : false;
    });
  };

  const isAnswered = (questionNode) => {
    const hasChecked = toArray(questionNode.querySelectorAll('input[type="radio"], input[type="checkbox"]'))
      .some((input) => !input.disabled && isElementVisible(input) && input.checked);
    if (hasChecked) return true;

    const hasText = toArray(questionNode.querySelectorAll('input[type="text"], input[type="number"], textarea'))
      .some((input) => !input.disabled && isElementVisible(input) && input.value.trim().length > 0);
    if (hasText) return true;

    const hasMathAnswer = toArray(questionNode.querySelectorAll('math-field[data-answer-math]'))
      .some((field) => {
        if (field.hasAttribute('disabled') || !isElementVisible(field)) return false;
        const promptAnswer = hasPromptAnswer(field);
        if (promptAnswer !== null) return promptAnswer;
        return getMathFieldValue(field).trim().length > 0;
      });
    return hasMathAnswer;
  };

  const bindValidateUnanswered = () => {
    const btn = document.querySelector('[data-action="validate-unanswered"]');
    if (!btn) return;
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
        output.textContent = unanswered === 0 ? 'Tat ca cau hoi da co cau tra loi.' : `Con ${unanswered} cau chua tra loi.`;
      }
    });
  };

  const reindexAnswers = (list) => {
    toArray(list.querySelectorAll('[data-answer-item]')).forEach((item, idx) => {
      const label = item.querySelector('[data-answer-index]');
      if (label) label.textContent = String.fromCharCode(65 + idx);
    });
  };

  const bindAnswerRepeater = () => {
    document.addEventListener('click', (event) => {
      const addBtn = event.target.closest('[data-add-answer]');
      if (addBtn) {
        const listId = addBtn.getAttribute('data-add-answer');
        const list = document.getElementById(listId);
        if (!list) return;
        const template = list.querySelector('[data-answer-template]');
        if (!template) return;
        const clone = template.cloneNode(true);
        clone.removeAttribute('data-answer-template');
        clone.setAttribute('data-answer-item', '');
        clone.classList.remove('d-none');
        toArray(clone.querySelectorAll('input[type="text"], input[type="number"], textarea')).forEach((input) => input.value = '');
        toArray(clone.querySelectorAll('input[type="radio"], input[type="checkbox"]')).forEach((input) => input.checked = false);
        list.appendChild(clone);
        reindexAnswers(list);
        return;
      }
      const removeBtn = event.target.closest('[data-remove-answer]');
      if (!removeBtn) return;
      const list = removeBtn.closest('[data-answer-list]');
      const row = removeBtn.closest('[data-answer-item]');
      if (!list || !row) return;
      if (toArray(list.querySelectorAll('[data-answer-item]')).length <= 2) return;
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
    if (totalCountNode) totalCountNode.textContent = String(totalQuestions);
    if (totalPointNode) totalPointNode.textContent = `${totalPoints.toFixed(1)}`;
  };

  const bindMatrixRepeater = () => {
    toArray(document.querySelectorAll('[data-add-matrix]')).forEach((btn) => {
      btn.addEventListener('click', () => {
        const listId = btn.getAttribute('data-add-matrix');
        const list = document.getElementById(listId);
        if (!list) return;
        const template = list.querySelector('[data-matrix-template]');
        if (!template) return;
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
      if (!removeBtn) return;
      const list = removeBtn.closest('[data-matrix-list]');
      const row = removeBtn.closest('[data-matrix-item]');
      if (!list || !row) return;
      if (toArray(list.querySelectorAll('[data-matrix-item]')).length <= 1) return;
      row.remove();
      recalcMatrixTotals(list);
    });
    document.addEventListener('input', (event) => {
      if (!event.target.closest('.matrix-count, .matrix-point')) return;
      const list = event.target.closest('[data-matrix-list]');
      if (list) recalcMatrixTotals(list);
    });
    toArray(document.querySelectorAll('[data-matrix-list]')).forEach((list) => recalcMatrixTotals(list));
  };

  const bindImportForms = () => {
    toArray(document.querySelectorAll('[data-import-form]')).forEach((form) => {
      form.addEventListener('submit', (event) => {
        event.preventDefault();
        const fileInput = form.querySelector('[data-import-file]');
        const result = form.querySelector('[data-import-result]');
        if (!fileInput || !result) return;
        const allowedExt = (form.getAttribute('data-allowed-ext') || '').split(',').map((ext) => ext.trim().toLowerCase()).filter(Boolean);
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
  const toArray = (v) => Array.from(v || []);
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

  if (!publicToggle || !classTable || !classSection) return;

  const classPageSize = 5;
  const blueprintPageSize = 5;
  const manualQuestionPageSize = 10;
  let classCurrentPage = 1, blueprintCurrentPage = 1, classTotalPages = 1, blueprintTotalPages = 1;
  let selectedBlueprintId = null, manualQuestionCurrentPage = 1, manualQuestionTotalPages = 1;
  let selectedClassId = null;
  const classSubjectById = new Map();
  const subjectIdByCode = new Map();
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
      for (const base of ['https://localhost:7167', window.location.origin, 'http://localhost:5217']) {
        try {
          const res = await fetch(`${(base || '').replace(/\/+$/, '')}/api/auth/me`, { headers: { Authorization: `Bearer ${token}` } });
          if (res.ok) {
            const data = await res.json();
            const id = data?.userId != null ? Number(data.userId) : null;
            if (Number.isInteger(id) && id > 0) { teacherIdNumber = id; break; }
          }
        } catch (_) { /* try next */ }
      }
    }
  }
  const teacherId = Number.isInteger(teacherIdNumber) && teacherIdNumber > 0 ? teacherIdNumber : null;

  const apiBaseCandidates = (() => {
    const configured = window.ASSIGN_EXAM_API_BASE_URL;
    return Array.from(new Set([configured, 'https://localhost:7167/api/assign-exam', '/api/assign-exam', `${window.location.origin}/api/assign-exam`].filter(Boolean)));
  })();

  let resolvedApiBaseUrl = null;

  const toAbsoluteUrl = (base, path, params) => {
    const url = new URL(`${(base || '').replace(/\/+$/, '')}${path.startsWith('/') ? path : `/${path}`}`, window.location.origin);
    Object.entries(params || {}).forEach(([key, value]) => { if (value != null && value !== '') url.searchParams.set(key, String(value)); });
    return url;
  };

  const apiGetJson = async (path, params) => {
    const candidates = resolvedApiBaseUrl ? [resolvedApiBaseUrl, ...apiBaseCandidates.filter((x) => x !== resolvedApiBaseUrl)] : apiBaseCandidates;
    let lastError = null;
    for (const base of candidates) {
      try {
        const url = toAbsoluteUrl(base, path, params);
        const res = await fetch(url.toString());
        if (!res.ok) throw new Error(`status ${res.status}`);
        resolvedApiBaseUrl = base;
        return await res.json();
      } catch (error) { lastError = error; }
    }
    throw lastError || new Error('Cannot reach assign exam API.');
  };

  const apiPostJson = async (path, body) => {
    const candidates = resolvedApiBaseUrl ? [resolvedApiBaseUrl, ...apiBaseCandidates.filter((x) => x !== resolvedApiBaseUrl)] : apiBaseCandidates;
    let lastError = null;
    for (const base of candidates) {
      try {
        const url = toAbsoluteUrl(base, path);
        const res = await fetch(url.toString(), {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(body)
        });
        const json = await res.json().catch(() => null);
        if (!res.ok) {
          const e = new Error(json?.message || `status ${res.status}`);
          e.status = res.status;
          throw e;
        }
        resolvedApiBaseUrl = base;
        return json;
      } catch (error) { if (error?.status) throw error; lastError = error; }
    }
    throw lastError || new Error('Cannot reach assign exam API.');
  };

  const toApiDateTime = (value) => {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  };

  const toDateOrNull = (value) => {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
  };

  const getCurrentSubjectId = () => {
    const code = (Boolean(publicToggle?.checked) ? publicSubjectFilter?.value : manualQuestionSubjectFilter?.value) || '';
    if (!code) return null;
    const id = subjectIdByCode.get(code);
    return id != null ? Number(id) : (Number(code) || null);
  };

  const DIFFICULTY_LEVEL = { 1: 'Nhận biết', 2: 'Thông hiểu', 3: 'Vận dụng', 4: 'Vận dụng cao' };
  const difficultyLabelByValue = DIFFICULTY_LEVEL;
  const difficultyValueByLabel = { 'nhận biết': 1, 'thông hiểu': 2, 'vận dụng': 3, 'vận dụng cao': 4 };
  const toDateDisplay = (val) => {
    if (!val) return '-';
    const d = new Date(val);
    return Number.isNaN(d.getTime()) ? '-' : new Intl.DateTimeFormat('vi-VN').format(d);
  };
  const getCheckedClassId = () => {
    const sel = toArray(classTableBody?.querySelectorAll('.class-item') || []).find((i) => i.checked);
    if (!sel) return null;
    const v = Number(sel.value);
    return Number.isInteger(v) && v > 0 ? v : null;
  };
  const syncManualSelectionWithCurrentFilters = async () => {
    const sub = (Boolean(publicToggle?.checked) ? publicSubjectFilter?.value : manualQuestionSubjectFilter?.value) || '';
    if (!sub) return;
    try {
      const rows = await apiGetJson('/questions', { teacherId, subjectCode: sub });
      const validIds = new Set(rows.map((r) => String(r.questionId)));
      Array.from(manualSelectedQuestionIds).forEach((id) => { if (!validIds.has(id)) manualSelectedQuestionIds.delete(id); });
    } catch (_) { /* ignore */ }
  };
  const applyExamGenerationMode = () => {
    const isManual = Boolean(examGenerationManual?.checked);
    if (blueprintModeSection) blueprintModeSection.classList.toggle('d-none', isManual);
    if (manualModeSection) manualModeSection.classList.toggle('d-none', !isManual);
    if (examGenerationModeDivider) examGenerationModeDivider.classList.toggle('d-none', false);
  };

  const saveAssignExam = async () => {
    if (!teacherId) return showToast('Thiếu teacherId, không thể lưu giao đề.', 'error');

    const title = examTitleInput?.value.trim() || '';
    const duration = Number(examDurationInput?.value || 0);
    const maxAttempts = Number(examMaxAttemptsInput?.value || 0);
    const paperCountRaw = examPaperCountInput?.value?.trim();
    const paperCount = Math.floor(Number(paperCountRaw || 1)) || 1;
    const isPublic = Boolean(publicToggle?.checked);
    const checkedClassId = getCheckedClassId();
    const resolvedClassId = !isPublic ? (checkedClassId || (Number.isInteger(selectedClassId) ? selectedClassId : null)) : null;
    const generationMode = Boolean(examGenerationManual?.checked) ? 'manual' : 'blueprint';
    const openAtDate = toDateOrNull(examOpenAtInput?.value);
    const closeAtDate = toDateOrNull(examCloseAtInput?.value);

    if (generationMode === 'manual') await syncManualSelectionWithCurrentFilters();

    const errors = window.AssignExamValidator.validate({
      title,
      duration,
      maxAttempts,
      paperCount: paperCountRaw,
      openAtDate,
      closeAtDate,
      isPublic,
      publicSubjectValue: publicSubjectFilter?.value || '',
      resolvedClassId,
      generationMode,
      selectedBlueprintId,
      manualQuestionCount: manualSelectedQuestionIds.size,
      fields: {
        title: examTitleInput,
        duration: examDurationInput,
        maxAttempts: examMaxAttemptsInput,
        paperCount: examPaperCountInput,
        openAt: examOpenAtInput,
        closeAt: examCloseAtInput,
        publicSubject: publicSubjectFilter
      }
    });

    if (errors.length > 0) return showToast(errors.join('\n'), 'error', 5000);

    const payload = {
      teacherId, title, description: examDescriptionInput?.value.trim() || '', duration, showScore: Boolean(showScoreToggle?.checked), showAnswer: false, maxAttempts,
      visibleFrom: toApiDateTime(examVisibleFromInput?.value), openAt: toApiDateTime(examOpenAtInput?.value), closeAt: toApiDateTime(examCloseAtInput?.value),
      shuffleQuestion: Boolean(shuffleQuestionToggle?.checked), allowLateSubmission: Boolean(allowLateSubmissionToggle?.checked), isPublic, classId: resolvedClassId,
      generationMode, examBlueprintId: generationMode === 'blueprint' ? selectedBlueprintId : null,
      subjectId: generationMode === 'manual' ? getCurrentSubjectId() : null,
      questionIds: generationMode === 'manual' ? Array.from(manualSelectedQuestionIds).map(Number) : [],
      paperCount, paperCode: 1
    };

    try {
      if (saveConfigTopButton) saveConfigTopButton.disabled = true;
      if (saveConfigBottomButton) saveConfigBottomButton.disabled = true;
      const res = await apiPostJson('', payload);
      showToast(`Lưu thành công. ExamId: ${res.examId}`, 'success');
    } catch (e) {
      showToast(`Lưu thất bại: ${e.message}`, 'error', 5000);
    }
    finally {
      if (saveConfigTopButton) saveConfigTopButton.disabled = false;
      if (saveConfigBottomButton) saveConfigBottomButton.disabled = false;
    }
  };


  const setSelectOptions = (el, opts, placeholder) => {
    if (!el) return;
    const cur = el.value || '';
    el.innerHTML = '';
    el.add(new Option(placeholder, ''));
    opts.forEach((o) => el.add(new Option(o.label, o.value)));
    if (cur && opts.some((x) => x.value === cur)) el.value = cur;
  };

  const ensureSelectHasOption = (el, val, lab) => {
    if (!el || !val || Array.from(el.options).some((o) => o.value === val)) return;
    el.add(new Option(lab || val, val));
  };

  const applySubjectFilters = (subs) => {
    subjectIdByCode.clear();
    subs.forEach((s) => { if (s?.code) subjectIdByCode.set(s.code, s.subjectId); });
    const opts = subs.map((s) => ({ value: s.code || '', label: s.code || s.name })).filter((o) => o.value);
    setSelectOptions(classSubjectFilter, opts, 'Tất cả môn');
    setSelectOptions(manualQuestionSubjectFilter, opts, 'Tất cả môn');
    setSelectOptions(publicSubjectFilter, opts, 'Chọn môn học');
    setSelectOptions(blueprintSubjectFilter, opts, 'Tất cả môn');
  };

  const applySemesterFilters = (sems) => setSelectOptions(classSemesterFilter, sems.map((x) => ({ value: x, label: x })), 'Tất cả học kỳ');

  const showStatusRow = (tbody, templateId, message, colspan) => {
    tbody.innerHTML = '';
    const t = document.getElementById(templateId);
    if (!t) return;
    const row = t.content.cloneNode(true).firstElementChild;
    const msgNode = row.querySelector('[data-message]');
    if (msgNode) msgNode.textContent = message;
    tbody.appendChild(row);
  };

  const renderClassTable = async (reset = false) => {
    if (!classTableBody) return;
    if (reset) classCurrentPage = 1;
    if (!teacherId) return showStatusRow(classTableBody, 'classStatusTemplate', 'Vui lòng truyền teacherId.', 4);

    try {
      const data = await apiGetJson('/classes', {
        teacherId, keyword: classSearchInput?.value || '', subjectCode: classSubjectFilter?.value || '',
        semester: classSemesterFilter?.value || '', page: classCurrentPage, pageSize: classPageSize
      });
      const items = data?.items || [];
      const total = Number(data?.totalItems || 0);
      classTotalPages = Math.max(1, Math.ceil(total / classPageSize));
      if (!selectedClassId && items.length > 0) selectedClassId = Number(items[0].classId);

      classTableBody.innerHTML = '';
      const t = document.getElementById('classRowTemplate');
      if (!t) { showStatusRow(classTableBody, 'classStatusTemplate', 'Lỗi: thiếu template classRowTemplate.', 4); return; }
      items.forEach((item) => {
        const row = t.content.cloneNode(true).firstElementChild;
        row.setAttribute('data-class-code', item.classCode);
        row.setAttribute('data-subject', item.subjectCode);
        const radio = row.querySelector('input');
        radio.value = item.classId;
        radio.checked = Number(item.classId) === selectedClassId;
        row.querySelector('[data-field-code]').textContent = item.classCode;
        row.querySelector('[data-field-subject]').textContent = item.subjectCode;
        row.querySelector('[data-field-semester]').textContent = item.semester;
        classTableBody.appendChild(row);
        classSubjectById.set(Number(item.classId), item.subjectCode);
      });

      if (classPaginationSummary) classPaginationSummary.textContent = total === 0 ? 'Không có lớp học phù hợp.' : `Hiển thị ${total === 0 ? 0 : (classCurrentPage-1)*classPageSize+1}-${total === 0 ? 0 : (classCurrentPage-1)*classPageSize+items.length} trên ${total} lớp`;
      
      toArray(classTableBody.querySelectorAll('.class-item')).forEach(i => i.addEventListener('change', () => {
        selectedClassId = Number(i.value);
        updateSelectedClass();
        syncSubjectFiltersBySelectedClass();
      }));
      updateSelectedClass();
      updateClassPagination();
      syncSubjectFiltersBySelectedClass();
    } catch (e) { showStatusRow(classTableBody, 'classStatusTemplate', 'Lỗi tải danh sách lớp.', 4); }
  };

  const renderBlueprintMatrix = (matrix) => {
    if (!blueprintMatrixBody) return;
    blueprintMatrixBody.innerHTML = '';
    if (!matrix?.length) return showStatusRow(blueprintMatrixBody, 'matrixStatusTemplate', 'Không có dữ liệu ma trận đề.', 3);
    const t = document.getElementById('blueprintMatrixRowTemplate');
    matrix.forEach((x) => {
      const row = t.content.cloneNode(true).firstElementChild;
      row.querySelector('[data-field-chapter]').textContent = x.chapterName;
      row.querySelector('[data-field-difficulty]').textContent = {1:'Nhận biết',2:'Thông hiểu',3:'Vận dụng',4:'Vận dụng cao'}[x.difficulty] || x.difficulty;
      row.querySelector('[data-field-questions]').textContent = x.totalOfQuestions;
      blueprintMatrixBody.appendChild(row);
    });
  };

  const renderBlueprintTable = async (reset = false) => {
    if (!blueprintTableBody) return;
    if (reset) blueprintCurrentPage = 1;
    try {
      const all = await apiGetJson('/blueprints', { teacherId });
      const filtered = all.filter(i => {
        const sub = (blueprintSubjectFilter?.value || '').toLowerCase();
        const kw = (blueprintSearchInput?.value || '').toLowerCase();
        return (!sub || (i.subjectCode || '').toLowerCase() === sub) && (!kw || (i.name || '').toLowerCase().includes(kw));
      });
      const total = filtered.length;
      blueprintTotalPages = Math.max(1, Math.ceil(total / blueprintPageSize));
      const start = (blueprintCurrentPage - 1) * blueprintPageSize;
      const visible = filtered.slice(start, start + blueprintPageSize);
      if (!selectedBlueprintId && visible.length > 0) selectedBlueprintId = visible[0].examBlueprintId;

      blueprintTableBody.innerHTML = '';
      const t = document.getElementById('blueprintRowTemplate');
      visible.forEach(i => {
        const row = t.content.cloneNode(true).firstElementChild;
        row.setAttribute('data-blueprint-id', i.examBlueprintId);
        row.setAttribute('data-blueprint-name', i.name);
        row.setAttribute('data-subject', i.subjectCode);
        row.setAttribute('data-total-questions', i.totalQuestions);
        row.setAttribute('data-updated-display', toDateDisplay(i.updatedAtUtc));
        const radio = row.querySelector('input');
        radio.value = i.examBlueprintId;
        radio.checked = Number(i.examBlueprintId) === Number(selectedBlueprintId);
        row.querySelector('[data-field-name]').textContent = i.name;
        row.querySelector('[data-field-subject]').textContent = i.subjectCode;
        row.querySelector('[data-field-updated]').textContent = toDateDisplay(i.updatedAtUtc);
        blueprintTableBody.appendChild(row);
      });

      if (blueprintFilterSummary) blueprintFilterSummary.textContent = total === 0 ? 'Không có ma trận đề.' : `Hiển thị ${start+1}-${Math.min(start+blueprintPageSize, total)} trên ${total} ma trận.`;
      
      toArray(blueprintTableBody.querySelectorAll('.blueprint-item')).forEach(i => i.addEventListener('change', () => {
        selectedBlueprintId = Number(i.value);
        updateBlueprintDetail();
      }));
      updateBlueprintDetail();
      updateBlueprintPagination();
    } catch (e) { showStatusRow(blueprintTableBody, 'blueprintStatusTemplate', 'Lỗi tải danh sách ma trận.', 4); }
  };

  const updateBlueprintDetail = async () => {
    const radio = toArray(blueprintTableBody.querySelectorAll('.blueprint-item')).find(i => i.checked);
    const row = radio?.closest('tr');
    if (!row) {
      ['blueprintInfoName','blueprintInfoUpdated','blueprintInfoSubject'].forEach(id => { if(document.getElementById(id)) document.getElementById(id).textContent = '-'; });
      if (document.getElementById('blueprintInfoQuestionCount')) document.getElementById('blueprintInfoQuestionCount').textContent = '0 câu';
      renderBlueprintMatrix([]);
      return;
    }
    const id = Number(row.getAttribute('data-blueprint-id'));
    selectedBlueprintId = id;
    if (blueprintInfoName) blueprintInfoName.textContent = row.getAttribute('data-blueprint-name');
    if (blueprintInfoUpdated) blueprintInfoUpdated.textContent = row.getAttribute('data-updated-display');
    if (blueprintInfoSubject) blueprintInfoSubject.textContent = row.getAttribute('data-subject');
    if (blueprintInfoQuestionCount) blueprintInfoQuestionCount.textContent = `${row.getAttribute('data-total-questions')} câu`;
    try { renderBlueprintMatrix(await apiGetJson(`/blueprints/${id}/detail`)); } catch(e) { renderBlueprintMatrix([]); }
  };

  const renderManualQuestionContent = (raw) => {
    const tHolder = document.getElementById('manualContentTemplate').content.cloneNode(true).firstElementChild;
    const latexPattern = /\$([^$]+)\$/g;
    let last = 0;
    for (let m = latexPattern.exec(raw); m !== null; m = latexPattern.exec(raw)) {
      if (m.index > last) {
        const span = document.getElementById('manualTextTemplate').content.cloneNode(true).firstElementChild;
        span.textContent = raw.slice(last, m.index);
        tHolder.appendChild(span);
      }
      const mf = document.getElementById('manualMathTemplate').content.cloneNode(true).firstElementChild;
      mf.textContent = m[1];
      tHolder.appendChild(mf);
      last = m.index + m[0].length;
    }
    if (last < raw.length) {
      const span = document.getElementById('manualTextTemplate').content.cloneNode(true).firstElementChild;
      span.textContent = raw.slice(last);
      tHolder.appendChild(span);
    }
    return tHolder;
  };

  const renderManualTable = async (reset = false) => {
    if (!manualQuestionTableBody) return;
    if (reset) manualQuestionCurrentPage = 1;
    try {
      const levelVal = manualQuestionLevelFilter?.value;
      const difficultyVal = levelVal ? (Number(levelVal) || difficultyValueByLabel[normalizeText(levelVal)]) : undefined;
      const rows = await apiGetJson('/questions', {
        teacherId, subjectCode: (Boolean(publicToggle?.checked) ? publicSubjectFilter?.value : manualQuestionSubjectFilter?.value) || undefined,
        chapterId: Number(manualQuestionChapterFilter?.value) || undefined,
        difficulty: (difficultyVal >= 1 && difficultyVal <= 4) ? difficultyVal : undefined
      });
      const total = rows.length;
      manualQuestionTotalPages = Math.ceil(total / manualQuestionPageSize);
      const start = (manualQuestionCurrentPage - 1) * manualQuestionPageSize;
      const visible = rows.slice(start, start + manualQuestionPageSize);

      manualQuestionTableBody.innerHTML = '';
      if (total === 0) return showStatusRow(manualQuestionTableBody, 'manualQuestionStatusTemplate', 'Không có câu hỏi phù hợp.', 5);

      const t = document.getElementById('manualQuestionRowTemplate');
      visible.forEach(i => {
        const row = t.content.cloneNode(true).firstElementChild;
        const cb = row.querySelector('input');
        cb.checked = manualSelectedQuestionIds.has(String(i.questionId));
        cb.addEventListener('change', () => { if(cb.checked) manualSelectedQuestionIds.add(String(i.questionId)); else manualSelectedQuestionIds.delete(String(i.questionId)); updateManualSummary(); });
        row.querySelector('[data-field-subject]').textContent = i.subjectCode;
        row.querySelector('[data-field-chapter]').textContent = i.chapterName;
        row.querySelector('[data-field-level]').textContent = difficultyLabelByValue[i.difficulty] || i.difficulty;
        row.querySelector('[data-field-content]').appendChild(renderManualQuestionContent(i.contentLatex || i.questionContent || ''));
        manualQuestionTableBody.appendChild(row);
      });
      updateManualPagination(total, visible.length);
      updateManualSummary();
    } catch (e) { showStatusRow(manualQuestionTableBody, 'manualQuestionStatusTemplate', 'Lỗi tải danh sách câu hỏi.', 5); }
  };

  const syncManualChapterOptions = async () => {
    const sub = (Boolean(publicToggle?.checked) ? publicSubjectFilter?.value : manualQuestionSubjectFilter?.value) || '';
    const res = await apiGetJson('/questions', { teacherId, subjectCode: sub || undefined });
    const chapters = Array.from(new Set(res.filter(i => i.chapterId && i.chapterName).map(i => JSON.stringify({v:i.chapterId, l:i.chapterName})))).map(JSON.parse);
    setSelectOptions(manualQuestionChapterFilter, chapters.map(c => ({value:String(c.v), label:c.l})), 'Tất cả chương');
  };

  const rebuildPaginationButtons = (prevBtn, nextBtn, currentPage, totalPages, dataPageAttr, dataItemAttr, onPageChange) => {
    const ul = prevBtn?.closest('ul');
    const nav = prevBtn?.closest('nav');
    if (!ul) return;
    if (nav) nav.classList.toggle('d-none', totalPages <= 1);
    if (totalPages <= 1) return;
    if (prevBtn) prevBtn.disabled = currentPage <= 1;
    if (nextBtn) nextBtn.disabled = currentPage >= totalPages;
    const prevLi = prevBtn?.closest('li');
    const nextLi = nextBtn?.closest('li');
    Array.from(ul.querySelectorAll('li')).filter(li => li !== prevLi && li !== nextLi).forEach(li => li.remove());
    for (let p = 1; p <= totalPages; p++) {
      const li = document.createElement('li');
      li.className = 'page-item' + (p === currentPage ? ' active' : '');
      li.setAttribute(dataItemAttr, p);
      const btn = document.createElement('button');
      btn.className = 'page-link';
      btn.type = 'button';
      btn.setAttribute(dataPageAttr, p);
      btn.textContent = p;
      btn.addEventListener('click', () => onPageChange(p));
      li.appendChild(btn);
      ul.insertBefore(li, nextLi);
    }
  };
  const updateClassPagination = () => {
    rebuildPaginationButtons(
      classPaginationPrev, classPaginationNext, classCurrentPage, classTotalPages,
      'data-class-page', 'data-page-item',
      (p) => { classCurrentPage = p; renderClassTable(false); }
    );
  };
  const updateBlueprintPagination = () => {
    rebuildPaginationButtons(
      blueprintPaginationPrev, blueprintPaginationNext, blueprintCurrentPage, blueprintTotalPages,
      'data-blueprint-page', 'data-blueprint-page-item',
      (p) => { blueprintCurrentPage = p; renderBlueprintTable(false); }
    );
  };
  const updateManualPagination = (total, count) => {
    const start = (manualQuestionCurrentPage - 1) * manualQuestionPageSize;
    if (manualQuestionPaginationSummary) {
      manualQuestionPaginationSummary.textContent = total === 0 ? 'Không có câu hỏi phù hợp.' : `Hiển thị ${start + 1}-${start + count} trên ${total} câu hỏi.`;
    }
    rebuildPaginationButtons(
      manualQuestionPaginationPrev, manualQuestionPaginationNext, manualQuestionCurrentPage, manualQuestionTotalPages,
      'data-manual-page', 'data-manual-page-item',
      (p) => { manualQuestionCurrentPage = p; renderManualTable(false); }
    );
  };
  const updateManualSummary = () => { if(manualQuestionSummary) manualQuestionSummary.textContent = `Đã chọn ${manualSelectedQuestionIds.size} câu hỏi thủ công.`; };
  const updateSelectedClass = () => { if(selectedCount) { const sel = toArray(classTableBody.querySelectorAll('.class-item')).find(i => i.checked); selectedCount.textContent = sel ? `Đã chọn: ${sel.closest('tr').getAttribute('data-class-code')}` : 'Chưa chọn lớp học'; } };
  const syncSubjectFiltersBySelectedClass = () => { /* Logic sync disabled/value */ };
  const applyPublicMode = () => {
    const isPublic = Boolean(publicToggle?.checked);
    if (classSelectionBlock) classSelectionBlock.classList.toggle('d-none', isPublic);
    if (publicSubjectSection) publicSubjectSection.classList.toggle('d-none', !isPublic);
    if (manualQuestionSubjectFilter && publicSubjectFilter) {
      if (isPublic) {
        manualQuestionSubjectFilter.value = publicSubjectFilter.value || '';
        manualQuestionSubjectFilter.disabled = true;
      } else {
        manualQuestionSubjectFilter.disabled = false;
      }
    }
  };

  const loadFilterOptions = async () => {
    if (!teacherId) {
      if (classTableBody) showStatusRow(classTableBody, 'classStatusTemplate', 'Vui lòng truyền teacherId.', 4);
      return;
    }
    try {
      const data = await apiGetJson('/filters', { teacherId });
      applySubjectFilters(Array.isArray(data?.subjects) ? data.subjects : []);
      applySemesterFilters(Array.isArray(data?.semesters) ? data.semesters : []);
      renderClassTable(true);
      renderBlueprintTable(true);
      await syncManualChapterOptions();
      await renderManualTable();
      syncSubjectFiltersBySelectedClass();
      applyPublicMode();
    } catch (e) {
      if (classTableBody) showStatusRow(classTableBody, 'classStatusTemplate', 'Lỗi tải danh sách lớp.', 4);
    }
  };

  applyExamGenerationMode();
  applyPublicMode();
  loadFilterOptions();
  classSubjectFilter?.addEventListener('change', () => renderClassTable(true));
  classSemesterFilter?.addEventListener('change', () => renderClassTable(true));
  classSearchInput?.addEventListener('input', () => renderClassTable(true));
  classPaginationPrev?.addEventListener('click', () => { if (classCurrentPage > 1) { classCurrentPage--; renderClassTable(false); } });
  classPaginationNext?.addEventListener('click', () => { if (classCurrentPage < classTotalPages) { classCurrentPage++; renderClassTable(false); } });
  blueprintSubjectFilter?.addEventListener('change', () => renderBlueprintTable(true));
  blueprintSearchInput?.addEventListener('input', () => renderBlueprintTable(true));
  blueprintPaginationPrev?.addEventListener('click', () => { if (blueprintCurrentPage > 1) { blueprintCurrentPage--; renderBlueprintTable(false); } });
  blueprintPaginationNext?.addEventListener('click', () => { if (blueprintCurrentPage < blueprintTotalPages) { blueprintCurrentPage++; renderBlueprintTable(false); } });
  manualQuestionSubjectFilter?.addEventListener('change', async () => { await syncManualChapterOptions(); await renderManualTable(true); });
  manualQuestionChapterFilter?.addEventListener('change', () => renderManualTable(true));
  manualQuestionLevelFilter?.addEventListener('change', () => renderManualTable(true));
  publicSubjectFilter?.addEventListener('change', async () => {
    if (!publicToggle?.checked) return;
    if (manualQuestionSubjectFilter) manualQuestionSubjectFilter.value = publicSubjectFilter?.value || '';
    await syncManualChapterOptions();
    await renderManualTable(true);
  });
  manualQuestionPaginationPrev?.addEventListener('click', () => { if (manualQuestionCurrentPage > 1) { manualQuestionCurrentPage--; renderManualTable(false); } });
  manualQuestionPaginationNext?.addEventListener('click', () => { if (manualQuestionCurrentPage < manualQuestionTotalPages) { manualQuestionCurrentPage++; renderManualTable(false); } });
  document.body.addEventListener('click', (e) => {
    if (!e.target.closest('#manualQuestionClearButton')) return;
    manualSelectedQuestionIds.clear();
    toArray(document.querySelectorAll('#manualQuestionTable input[type="checkbox"]')).forEach((cb) => { cb.checked = false; });
    updateManualSummary();
  });
  saveConfigTopButton?.addEventListener('click', saveAssignExam);
  saveConfigBottomButton?.addEventListener('click', saveAssignExam);
  publicToggle?.addEventListener('change', applyPublicMode);
  examGenerationBlueprint?.addEventListener('change', applyExamGenerationMode);
  examGenerationManual?.addEventListener('change', applyExamGenerationMode);
})();
