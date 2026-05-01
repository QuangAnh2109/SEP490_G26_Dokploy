(async () => {
  await window.userReady;

  const get = (id) => document.getElementById(id);
  const toArray = (v) => Array.from(v || []);
  const toApiDateTime = (v) => v ? new Date(v).toISOString() : null;
  const toDateDisplay = (v) => v ? new Intl.DateTimeFormat('vi-VN').format(new Date(v)) : '-';
  const DIFFICULTY_LEVEL = { 1: 'Nhận biết', 2: 'Thông hiểu', 3: 'Vận dụng', 4: 'Vận dụng cao' };
  const difficultyValueByLabel = { 'nhận biết': 1, 'thông hiểu': 2, 'vận dụng': 3, 'vận dụng cao': 4 };
  const normalizeText = (v = '') => v.toLowerCase().trim();

  const queryParams = new URL(window.location.href).searchParams;
  const preClassId = Number(queryParams.get('classId')) || null;
  const preSubjectCode = queryParams.get('subjectCode') || null;

  const teacherId = (() => {
    const q = queryParams.get('teacherId');
    const g = window.ASSIGN_EXAM_TEACHER_ID;
    const t = typeof getUserIdFromToken === 'function' ? getUserIdFromToken() : null;
    return Number(q || g || t) || null;
  })();

  const apiGetJson = async (path, params) => {
    let url = path;
    if (params) {
        const query = new URLSearchParams();
        Object.entries(params).forEach(([k, v]) => v != null && query.set(k, String(v)));
        const qStr = query.toString();
        if (qStr) url += (url.includes('?') ? '&' : '?') + qStr;
    }
    return await apiClient.get(url);
  };
  const apiPostJson = async (path, body) => {
    return await apiClient.post(path, body);
  };

  const ui = {
    bpSearch: get('blueprintSearchInput'),
    bpTBody: get('blueprintTableBody'), bpPrev: get('blueprintPaginationPrev'), bpNext: get('blueprintPaginationNext'),
    bpSummary: get('blueprintFilterSummary'), bpInfoName: get('blueprintInfoName'), bpInfoUpdated: get('blueprintInfoUpdated'),
    bpInfoSub: get('blueprintInfoSubject'), bpInfoCount: get('blueprintInfoQuestionCount'), bpMatrixBody: get('blueprintMatrixBody'),
    genBp: get('examGenerationBlueprint'), genManual: get('examGenerationManual'), bpMode: get('blueprintModeSection'),
    manualMode: get('manualModeSection'),
    manChap: get('manualQuestionChapterFilter'), manLevel: get('manualQuestionLevelFilter'),
    manTBody: get('manualQuestionTableBody'), manSummary: get('manualQuestionSummary'), manPrev: get('manualQuestionPaginationPrev'),
    manNext: get('manualQuestionPaginationNext'), manPaginationSummary: get('manualQuestionPaginationSummary'),
    btnSaveTop: get('saveConfigTop'), btnSaveBottom: get('saveConfigBottom'), btnCloseTop: get('closeConfigTop'),
    title: get('examTitleInput'), desc: get('examDescriptionInput'), maxAtt: get('examMaxAttemptsInput'),
    paperCount: get('examPaperCountInput'), visFrom: get('examVisibleFromInput'), openAt: get('examOpenAtInput'),
    closeAt: get('examCloseAtInput'), dur: get('examDurationInput'),
    shuffle: get('rule1'), next1: get('btnNext1'), next2: get('btnNext2'),
    back2: get('btnBack2'), back3: get('btnBack3'),
    displayClassName: get('displayClassName'), displaySubjectCode: get('displaySubjectCode')
  };

  const state = {
    bpP: 1, manP: 1, bpTotalP: 1, manTotalP: 1,
    pageSize: { bp: 5, man: 10 }, selClassId: preClassId, selBpId: null,
    selManIds: new Set(), lockedSubject: preSubjectCode
  };

  const showStatusRow = (tbody, templateId, message) => {
    if (!tbody) return; tbody.innerHTML = '';
    const t = get(templateId);
    if (!t) return;
    const row = t.content.cloneNode(true).firstElementChild;
    const msg = row.querySelector('[data-message]');
    if (msg) msg.textContent = message;
    tbody.appendChild(row);
  };

  const syncPagination = (prev, next, current, total, onPage) => {
    if (!prev) return;
    prev.disabled = current <= 1; next.disabled = current >= total;
    const ul = prev.closest('ul');
    toArray(ul.querySelectorAll('.page-item:not(:first-child):not(:last-child)')).forEach(li => li.remove());
    for (let p = 1; p <= total; p++) {
      const li = document.createElement('li'); li.className = `page-item ${p === current ? 'active' : ''}`;
      const b = document.createElement('button'); b.className = 'page-link'; b.textContent = p; b.type = 'button';
      b.onclick = () => onPage(p); li.appendChild(b); ul.insertBefore(li, next.parentElement);
    }
  };

  const renderBlueprintTable = async (reset = false) => {
    if (reset) state.bpP = 1;
    try {
      const all = await apiGetJson('/api/assign-exam/blueprints', { teacherId, subjectCode: state.lockedSubject || undefined });
      const filtered = all.filter(i => {
        const sub = (state.lockedSubject || '').toLowerCase();
        const kw = (ui.bpSearch?.value || '').toLowerCase();
        return (!sub || (i.subjectCode || '').toLowerCase() === sub) && (!kw || (i.name || '').toLowerCase().includes(kw));
      });
      if (ui.bpSummary) ui.bpSummary.textContent = `Tìm thấy ${filtered.length} ma trận đề.`;
      state.bpTotalP = Math.max(1, Math.ceil(filtered.length / state.pageSize.bp));
      const visible = filtered.slice((state.bpP - 1) * state.pageSize.bp, state.bpP * state.pageSize.bp);
      if (!state.selBpId && visible.length > 0) state.selBpId = visible[0].examBlueprintId;
      ui.bpTBody.innerHTML = '';
      const t = get('blueprintRowTemplate');
      visible.forEach(i => {
        const row = t.content.cloneNode(true).firstElementChild;
        const r = row.querySelector('input'); r.value = i.examBlueprintId; r.checked = Number(i.examBlueprintId) === Number(state.selBpId);
        r.onchange = () => { state.selBpId = Number(r.value); updateBlueprintDetail(); };
        row.querySelector('[data-field-name]').textContent = i.name;
        row.querySelector('[data-field-subject]').textContent = i.subjectCode;
        row.querySelector('[data-field-updated]').textContent = toDateDisplay(i.updatedAtUtc);
        row.dataset.id = i.examBlueprintId; row.dataset.name = i.name; row.dataset.sub = i.subjectCode;
        row.dataset.count = i.totalQuestions; row.dataset.upd = toDateDisplay(i.updatedAtUtc);
        ui.bpTBody.appendChild(row);
      });
      updateBlueprintDetail();
      syncPagination(ui.bpPrev, ui.bpNext, state.bpP, state.bpTotalP, p => { state.bpP = p; renderBlueprintTable(); });
    } catch (e) { showStatusRow(ui.bpTBody, 'blueprintStatusTemplate', 'Lỗi tải danh sách ma trận.'); }
  };

  const updateBlueprintDetail = async () => {
    const row = toArray(ui.bpTBody.querySelectorAll('tr')).find(r => r.querySelector('input').checked);
    if (!row) { renderBlueprintMatrix([]); return; }
    state.selBpId = Number(row.dataset.id);
    if (ui.bpInfoName) ui.bpInfoName.textContent = row.dataset.name;
    if (ui.bpInfoSub) ui.bpInfoSub.textContent = row.dataset.sub;
    if (ui.bpInfoCount) ui.bpInfoCount.textContent = `${row.dataset.count} câu`;
    try { renderBlueprintMatrix(await apiGetJson(`/api/assign-exam/blueprints/${state.selBpId}/detail`)); } catch(e) { renderBlueprintMatrix([]); }
  };

  const renderBlueprintMatrix = (matrix) => {
    ui.bpMatrixBody.innerHTML = '';
    if (!matrix?.length) return showStatusRow(ui.bpMatrixBody, 'matrixStatusTemplate', 'Không có dữ liệu ma trận đề.');
    const t = get('blueprintMatrixRowTemplate');
    matrix.forEach(x => {
      const row = t.content.cloneNode(true).firstElementChild;
      row.querySelector('[data-field-chapter]').textContent = x.chapterName;
      row.querySelector('[data-field-difficulty]').textContent = DIFFICULTY_LEVEL[x.difficulty] || x.difficulty;
      row.querySelector('[data-field-questions]').textContent = x.totalOfQuestions;
      ui.bpMatrixBody.appendChild(row);
    });
  };

  const asMathLiveLatex = (str) => {
    if (!str || str.includes('\\text{') || str.includes('\\displaylines{')) return str || '';
    return str.split(/(\$[^\$]+\$)/g).map(p => (p.startsWith('$') && p.endsWith('$')) ? p.slice(1, -1) : (p ? `\\text{${p}}` : '')).join('');
  };

  const renderManualQuestionContent = (raw) => {
    let content = raw;
    try {
      const p = JSON.parse(raw);
      if (p?.stem || p?.frame) {
        const cl = (s) => (s || '').replace(/^\\displaylines\s*\{([\s\S]*)\}\s*$/, '$1').trim();
        content = cl(p.stem) + (p.stem && p.frame ? ' \\\\ ' : '') + cl(p.frame);
      }
    } catch(e) { content = asMathLiveLatex(raw); }
    if (content.includes('\\\\') && !content.trim().startsWith('\\displaylines')) content = `\\displaylines{${content}}`;
    const mf = document.createElement('math-field'); mf.className = 'manual-question-math'; mf.readOnly = true; mf.value = content;
    const d = document.createElement('div'); d.className = 'manual-question-content'; d.appendChild(mf);
    return d;
  };

  const renderManualTable = async (reset = false) => {
    if (reset) state.manP = 1;
    try {
      const sub = state.lockedSubject || undefined;
      const diffVal = ui.manLevel?.value ? (Number(ui.manLevel.value) || difficultyValueByLabel[normalizeText(ui.manLevel.value)]) : undefined;
      const rows = await apiGetJson('/api/assign-exam/questions', { teacherId, subjectCode: sub, chapterId: Number(ui.manChap?.value) || undefined, difficulty: (diffVal >= 1 && diffVal <= 4) ? diffVal : undefined });
      if (ui.manPaginationSummary) ui.manPaginationSummary.textContent = `Tìm thấy ${rows.length} câu hỏi.`;
      state.manTotalP = Math.ceil(rows.length / state.pageSize.man);
      const visible = rows.slice((state.manP - 1) * state.pageSize.man, state.manP * state.pageSize.man);
      ui.manTBody.innerHTML = '';
      if (!rows.length) return showStatusRow(ui.manTBody, 'manualQuestionStatusTemplate', 'Không có câu hỏi phù hợp.');
      const t = get('manualQuestionRowTemplate');
      visible.forEach(i => {
        const row = t.content.cloneNode(true).firstElementChild;
        const cb = row.querySelector('input'); cb.checked = state.selManIds.has(String(i.questionId));
        cb.onchange = () => { if(cb.checked) state.selManIds.add(String(i.questionId)); else state.selManIds.delete(String(i.questionId)); updateManualSummary(); };
        row.querySelector('[data-field-chapter]').textContent = i.chapterName;
        row.querySelector('[data-field-level]').textContent = DIFFICULTY_LEVEL[i.difficulty] || i.difficulty;
        row.querySelector('[data-field-content]').appendChild(renderManualQuestionContent(i.contentLatex || i.questionContent || ''));
        ui.manTBody.appendChild(row);
      });
      syncPagination(ui.manPrev, ui.manNext, state.manP, state.manTotalP, p => { state.manP = p; renderManualTable(); });
    } catch (e) { showStatusRow(ui.manTBody, 'manualQuestionStatusTemplate', 'Lỗi tải danh sách câu hỏi.'); }
  };

  const syncManualChapters = async () => {
    const sub = state.lockedSubject || '';
    const res = await apiGetJson('/api/assign-exam/questions', { teacherId, subjectCode: sub || undefined });
    if (!Array.isArray(res)) return;
    const chaps = Array.from(new Set(res.filter(i => i.chapterId && i.chapterName).map(i => JSON.stringify({v:i.chapterId, l:i.chapterName})))).map(JSON.parse);
    if (!ui.manChap) return; ui.manChap.innerHTML = '<option value="">Tất cả chương</option>';
    chaps.forEach(c => ui.manChap.add(new Option(c.l, c.v)));
  };

  const switchToStep = (step) => {
    get('stepper')?.setAttribute('data-active-step', step);
    document.querySelectorAll('.step-content').forEach(c => c.classList.toggle('d-none', c.id !== `step${step}Content`));
    document.querySelectorAll('.step').forEach(s => { const n = Number(s.dataset.step); s.classList.toggle('active', n === step); s.classList.toggle('completed', n < step); });
    document.querySelectorAll('.step-line').forEach((l, i) => l.classList.toggle('active', i < step - 1));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const validate = (s) => {
    if (s === 1) {
      const ok = !!ui.title?.value?.trim() && !!state.selClassId;
      if (ui.title) ui.title.classList.toggle('is-invalid', !ui.title.value?.trim());
      return ok;
    }
    if (s === 2) {
      const man = ui.genManual.checked; const ok = man ? state.selManIds.size > 0 : !!state.selBpId;
      (man ? ui.manualMode : ui.bpMode).classList.toggle('is-invalid', !ok); return ok;
    }
    // Step 3: sử dụng AssignExamValidator đầy đủ
    const visVal = ui.visFrom?.value ? new Date(ui.visFrom.value) : null;
    const openVal = ui.openAt?.value ? new Date(ui.openAt.value) : null;
    const closeVal = ui.closeAt?.value ? new Date(ui.closeAt.value) : null;
    const errs = window.AssignExamValidator.validate({
      title: ui.title?.value?.trim(),
      duration: Number(ui.dur?.value),
      maxAttempts: Number(ui.maxAtt?.value),
      paperCount: ui.paperCount?.value,
      visibleFromDate: visVal,
      openAtDate: openVal,
      closeAtDate: closeVal,
      isPublic: false,
      publicSubjectValue: null,
      resolvedClassId: state.selClassId,
      generationMode: ui.genManual?.checked ? 'manual' : 'blueprint',
      selectedBlueprintId: state.selBpId,
      manualQuestionCount: state.selManIds?.size || 0,
      fields: { title: ui.title, duration: ui.dur, maxAttempts: ui.maxAtt, paperCount: ui.paperCount, visibleFrom: ui.visFrom, openAt: ui.openAt, closeAt: ui.closeAt }
    });
    if (errs.length) { errs.forEach(e => showToast(e, 'error')); return false; }
    return true;
  };

  const saveAssignExam = async () => {
    if (!teacherId || !validate(3)) return !teacherId && showToast('Thiếu teacherId.', 'error');
    const isMan = ui.genManual.checked;
    const showScore = Number(document.querySelector('input[name="showScore"]:checked')?.value || 0);
    const showAnswer = Number(document.querySelector('input[name="showAnswer"]:checked')?.value || 0);
    const answerTimingMode = Number(document.querySelector('input[name="answerTimingMode"]:checked')?.value || 0);
    const payload = {
      teacherId, title: ui.title.value.trim(), description: ui.desc.value.trim(), duration: Number(ui.dur.value),
      showScore, showAnswer, answerTimingMode,
      maxAttempts: Number(ui.maxAtt.value),
      visibleFrom: toApiDateTime(ui.visFrom.value), openAt: toApiDateTime(ui.openAt.value), closeAt: toApiDateTime(ui.closeAt.value),
      shuffleQuestion: ui.shuffle.checked, isPublic: false,
      classId: state.selClassId, generationMode: isMan ? 'manual' : 'blueprint',
      examBlueprintId: isMan ? null : state.selBpId, questionIds: isMan ? Array.from(state.selManIds).map(Number) : [],
      paperCount: Number(ui.paperCount.value), paperCode: 1
    };
    try {
      [ui.btnSaveTop, ui.btnSaveBottom].forEach(b => b && (b.disabled = true));
      const res = await apiPostJson('/api/assign-exam', payload);
      showToast(`Tạo đề thi thành công! Đang chuyển hướng đến trang hậu kì...`, 'success');
      setTimeout(() => {
          window.location.href = `/Exam/ExamReview?examId=${res.examId}&classId=${state.selClassId}`;
      }, 1500);
    } catch(e) { showToast(`Lưu thất bại: ${e?.message || e?.error || 'Lỗi không xác định'}`, 'error'); }
    finally { [ui.btnSaveTop, ui.btnSaveBottom].forEach(b => b && (b.disabled = false)); }
  };

  const updateManualSummary = () => { if(ui.manSummary) ui.manSummary.textContent = `Đã chọn ${state.selManIds.size} câu hỏi thủ công.`; };

  const setupListeners = () => {
    const bindClick = (el, fn) => { if (el) el.onclick = fn; };
    const bindChange = (el, fn) => { if (el) el.onchange = fn; };
    const bindInput = (el, fn) => { if (el) el.oninput = fn; };

    bindClick(ui.next1, () => { if(validate(1)) switchToStep(2); else showToast('Vui lòng hoàn thành thông tin bước 1.', 'error'); });
    bindClick(ui.next2, () => { if(validate(2)) switchToStep(3); else showToast('Vui lòng hoàn thành thông tin bước 2.', 'error'); });
    bindClick(ui.back2, () => switchToStep(1));
    bindClick(ui.back3, () => switchToStep(2));
    
    [ui.genBp, ui.genManual].forEach(r => bindChange(r, () => {
      if (ui.bpMode) ui.bpMode.classList.toggle('d-none', ui.genManual?.checked);
      if (ui.manualMode) ui.manualMode.classList.toggle('d-none', !ui.genManual?.checked);
    }));

    bindInput(ui.bpSearch, () => renderBlueprintTable(true));
    bindChange(ui.bpSearch, () => renderBlueprintTable(true));
    
    [ui.manChap, ui.manLevel].forEach(el => bindChange(el, async () => { renderManualTable(true); }));
    
    bindClick(ui.btnSaveBottom, saveAssignExam);
    bindClick(ui.btnSaveTop, () => { if(validate(1) && validate(2) && validate(3)) saveAssignExam(); else showToast('Vui lòng hoàn thành thông tin.', 'error'); });
    
    bindClick(ui.btnCloseTop, () => {
      if (state.selClassId) window.location.href = `/Course/ExamListInCourse/${state.selClassId}`;
      else window.location.href = '/Exam';
    });
    
    const clearBtn = get('manualQuestionClearButton');
    bindClick(clearBtn, () => { 
      state.selManIds.clear(); 
      if (ui.manTBody) toArray(ui.manTBody.querySelectorAll('input')).forEach(i => i.checked = false); 
      updateManualSummary(); 
    });
  };

  setupListeners();

  (async () => {
    try {
      if (!state.selClassId) { showToast("Thiếu thông tin lớp học. Vui lòng quay lại.", "error"); return; }

      // Gọi API lấy thông tin lớp học (apiGetJson đã tự động kèm Token và API_BASE_URL)
      try {
        const courseData = await apiGetJson(`/api/Course/${state.selClassId}/settings`);
        if (courseData) {
          // CourseDTO trả về 'className' hoặc 'name'
          const realName = courseData.className || courseData.name || courseData.classCode;
          if (ui.displayClassName && realName) {
            ui.displayClassName.textContent = `Lớp: ${realName}`;
          }
          if (ui.displaySubjectCode) ui.displaySubjectCode.textContent = courseData.subjectCode || state.lockedSubject;
          if (!state.lockedSubject) state.lockedSubject = courseData.subjectCode;
        }
      } catch (err) { console.warn("Lỗi khi tải tên lớp chi tiết:", err); }

      renderBlueprintTable(true);
      await syncManualChapters();
      renderManualTable();
    } catch(e) { 
      console.error("Initialization error", e); 
      showToast("Lỗi khi tải thông tin: " + (e?.message || e?.error || 'Lỗi không xác định'), "error"); 
    }
  })();
})();
