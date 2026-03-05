(() => {
    'use strict';
    const toArray = (v) => Array.from(v || []);
    const questionList = document.getElementById('questionBatchList');
    const addQuestionBtn = document.getElementById('addQuestionItemBtn');
    const countText = document.getElementById('questionCountText');
    const saveAllBtn = document.getElementById('saveAllBtn');
    const saveDraftBtn = document.getElementById('saveDraftBtn');
    if (!questionList || !addQuestionBtn) { return; }

    // ── Global state ──
    let inputTypesData = []; // loaded from API
    let subjectsData = [];   // loaded from API

    // ── MathLive helpers ──
    const getMathValue = (f) => {
        if (!f) { return ''; }
        if (typeof f.getValue === 'function') { const l = f.getValue('latex'); return typeof l === 'string' ? l : ''; }
        if (typeof f.value === 'string') { return f.value; }
        return f.textContent || '';
    };
    const setMathValue = (f, v) => {
        if (!f) { return; }
        if (typeof f.setValue === 'function') { f.setValue(v, { silenceNotifications: true }); return; }
        if (typeof f.value === 'string') { f.value = v; return; }
        f.textContent = v;
    };

    // ── Render toggle ──
    const setupPairToggle = (mathField, rawTextarea, onChange) => {
        if (!mathField || !rawTextarea) { return { setRenderMode: () => { } }; }
        let savedRaw = '';
        let mathDirty = false;
        const setRenderMode = (renderOn) => {
            if (renderOn) {
                savedRaw = rawTextarea.value;
                mathDirty = false;
                setMathValue(mathField, savedRaw);
                mathField.classList.remove('d-none');
                rawTextarea.classList.add('d-none');
            } else {
                if (!mathDirty) { rawTextarea.value = savedRaw; }
                mathField.classList.add('d-none');
                rawTextarea.classList.remove('d-none');
                rawTextarea.focus();
            }
            if (onChange) { onChange(); }
        };
        mathField.addEventListener('input', () => { mathDirty = true; rawTextarea.value = getMathValue(mathField); if (onChange) { onChange(); } });
        rawTextarea.addEventListener('input', () => { if (onChange) { onChange(); } });
        return { setRenderMode };
    };

    // ── Numbered placeholders ──
    const getNumberedPlaceholders = (latex) => {
        const s = typeof latex === 'string' ? latex : '';
        const matches = s.matchAll(/\\placeholder\s*\[(\d+)\]\s*\{.*?\}/g);
        const nums = new Set();
        for (const m of matches) { nums.add(parseInt(m[1], 10)); }
        return Array.from(nums).sort((a, b) => a - b);
    };

    const getFrameLatex = (item) => {
        const raw = item.querySelector('[data-frame-raw]');
        if (raw && raw.value) { return raw.value; }
        const editor = item.querySelector('[data-frame-editor]');
        return getMathValue(editor);
    };

    const insertNumberedPlaceholder = (item) => {
        const renderToggle = item.querySelector('[data-render-toggle]');
        const isRaw = renderToggle && !renderToggle.checked;
        const latex = getFrameLatex(item);
        const existing = getNumberedPlaceholders(latex);
        let next = 1;
        for (const n of existing) { if (n === next) { next++; } else { break; } }
        const ph = `\\placeholder[${next}]{}`;
        if (isRaw) {
            const raw = item.querySelector('[data-frame-raw]');
            if (raw) {
                const pos = raw.selectionStart || raw.value.length;
                raw.value = raw.value.slice(0, pos) + ph + raw.value.slice(pos);
                raw.focus();
                raw.selectionStart = raw.selectionEnd = pos + ph.length;
            }
        } else {
            const field = item.querySelector('[data-frame-editor]');
            if (field && typeof field.insert === 'function') { field.insert(ph); }
            else { setMathValue(field, latex + ph); }
        }
    };

    // ── MCQ helpers ──
    const createMcqOptionRow = (list) => {
        const t = list.querySelector('[data-answer-template]');
        const r = t ? t.cloneNode(true) : null;
        if (!r) { return null; }
        r.removeAttribute('data-answer-template');
        r.setAttribute('data-answer-item', '');
        r.classList.remove('d-none');
        setMathValue(r.querySelector('[data-option-content]'), '');
        const rawTa = r.querySelector('[data-option-raw]'); if (rawTa) { rawTa.value = ''; }
        const c = r.querySelector('[data-option-correct]'); if (c) { c.checked = false; }
        return r;
    };

    const syncMcqRows = (item, num) => {
        const list = item.querySelector('[data-answer-list]'); if (!list) { return; }
        const rows = toArray(list.querySelectorAll('[data-answer-item]'));
        rows.forEach((row, i) => {
            const idx = row.querySelector('[data-answer-index]'); if (idx) { idx.textContent = String.fromCharCode(65 + i); }
            const ci = row.querySelector('[data-option-correct]');
            const cl = row.querySelector('[data-option-correct-label]');
            if (ci) { const id = `q${num}-opt-${i + 1}`; ci.id = id; if (cl) { cl.setAttribute('for', id); } }
            const rb = row.querySelector('[data-remove-answer]'); if (rb) { rb.disabled = rows.length <= 2; }
        });
    };

    const applyMcqRenderMode = (item, renderOn) => {
        const list = item.querySelector('[data-answer-list]'); if (!list) { return; }
        const allRows = toArray(list.querySelectorAll('[data-answer-item], [data-answer-template]'));
        allRows.forEach(row => {
            const mf = row.querySelector('[data-option-content]');
            const ta = row.querySelector('[data-option-raw]');
            if (!mf || !ta) { return; }
            if (renderOn) { setMathValue(mf, ta.value); mf.classList.remove('d-none'); ta.classList.add('d-none'); }
            else { ta.value = getMathValue(mf); mf.classList.add('d-none'); ta.classList.remove('d-none'); }
        });
    };

    // ── Build constraint chips HTML from API data ──
    // Uses <div> instead of <label>+<input> to avoid browser double-click issues.
    // Selection state is tracked purely via the 'active' CSS class on each chip.
    const buildConstraintChipsHtml = () => {
        if (!inputTypesData || inputTypesData.length === 0) { return '<p class="form-note">Đang tải giới hạn nhập liệu...</p>'; }
        const groups = {};
        inputTypesData.forEach(it => {
            const g = it.groupType || '__none__';
            if (!groups[g]) { groups[g] = []; }
            groups[g].push(it);
        });
        let html = '';
        for (const [groupType, items] of Object.entries(groups)) {
            const label = groupType === '__none__' ? 'Khác' : groupType;
            html += `<div class="constraint-section"><div class="constraint-section-label">${label}</div><div class="constraint-grid">`;
            items.forEach(it => {
                html += `<div class="constraint-chip" data-group-type="${groupType}" data-input-type-id="${it.inputTypeId}">`;
                html += `<span>${it.name}</span></div>`;
            });
            html += '</div></div>';
        }
        return html;
    };

    // ── Blank answer rows ──
    const createBlankAnswerRow = (blankNum) => {
        const row = document.createElement('div');
        row.className = 'blank-answer-row';
        row.setAttribute('data-blank-answer-item', '');
        row.setAttribute('data-blank-num', String(blankNum));
        row.innerHTML = `
      <div>
        <div class="blank-answer-header">
          <label class="form-label" data-blank-index-label>Đáp án ô trống ${blankNum}</label>
        </div>
        <math-field class="math-input" data-blank-answer></math-field>
        <button type="button" class="blank-constraint-toggle" data-blank-constraint-toggle>
          <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" viewBox="0 0 16 16">
            <path d="M1.646 4.646a.5.5 0 0 1 .708 0L8 10.293l5.646-5.647a.5.5 0 0 1 .708.708l-6 6a.5.5 0 0 1-.708 0l-6-6a.5.5 0 0 1 0-.708z"/>
          </svg>
          Giới hạn nhập liệu
        </button>
        <div class="blank-constraint-body" data-blank-constraint-body>
          ${buildConstraintChipsHtml()}
        </div>
      </div>
      <div class="blank-score-field" data-blank-score-field>
        <label>Hệ số điểm (%)</label>
        <input class="form-control form-control-sm" type="number" value="0" min="0" max="100" step="1" data-blank-score>
      </div>`;
        bindBlankConstraints(row);
        return row;
    };

    const syncBlankAnswerRows = (item, numberedList) => {
        const list = item.querySelector('[data-blank-answer-list]');
        const empty = item.querySelector('[data-blank-answer-empty]');
        if (!list) { return; }
        const count = numberedList.length;
        let rows = toArray(list.querySelectorAll('[data-blank-answer-item]'));

        while (rows.length < count) {
            const blankNum = numberedList[rows.length];
            const newRow = createBlankAnswerRow(blankNum);
            list.appendChild(newRow);
            rows = toArray(list.querySelectorAll('[data-blank-answer-item]'));
        }
        while (rows.length > count) { const last = rows.pop(); if (last) { last.remove(); } }

        const scoring = !!item.querySelector('[data-scoring-toggle]:checked');
        rows.forEach((row, i) => {
            const num = numberedList[i];
            const label = row.querySelector('[data-blank-index-label]');
            if (label) { label.textContent = `Đáp án ô trống ${num}`; }
            row.setAttribute('data-blank-num', String(num));
            const sf = row.querySelector('[data-blank-score-field]'); if (sf) { sf.classList.toggle('visible', scoring); }
            row.classList.toggle('has-scoring', scoring);
        });
        list.classList.toggle('d-none', count <= 0);
        if (empty) { empty.classList.toggle('d-none', count > 0); }

        updateScoreSummary(item);
    };

    const syncPlaceholderState = (item) => {
        const counter = item.querySelector('[data-placeholder-count]');
        const latex = getFrameLatex(item);
        const numbered = getNumberedPlaceholders(latex);
        if (counter) { counter.textContent = `${numbered.length} ô trống`; }
        syncBlankAnswerRows(item, numbered);
        syncBlankGroupSegments(item);
    };

    // ── Constraint chip binding ──
    // Pure click handler on <div> elements. No hidden inputs, no label forwarding issues.
    // Within same GroupType: only 1 chip can be active (radio behavior via JS).
    const bindBlankConstraints = (row) => {
        const toggle = row.querySelector('[data-blank-constraint-toggle]');
        const body = row.querySelector('[data-blank-constraint-body]');
        if (toggle && body) {
            toggle.addEventListener('click', (e) => { e.preventDefault(); body.classList.toggle('open'); toggle.classList.toggle('active'); });
        }
        toArray(row.querySelectorAll('.constraint-chip')).forEach(chip => {
            chip.addEventListener('click', () => {
                const groupType = chip.getAttribute('data-group-type');
                const wasActive = chip.classList.contains('active');
                // Deselect all siblings in same group
                toArray(row.querySelectorAll(`.constraint-chip[data-group-type="${groupType}"]`)).forEach(sibling => {
                    sibling.classList.remove('active');
                });
                // Toggle: if it was already active, leave it deselected; otherwise activate
                if (!wasActive) {
                    chip.classList.add('active');
                }
            });
        });
    };

    // ── Scoring toggle ──
    const bindScoringToggle = (item) => {
        const t = item.querySelector('[data-scoring-toggle]'); if (!t) { return; }
        t.addEventListener('change', () => {
            toArray(item.querySelectorAll('[data-blank-answer-item]')).forEach(row => {
                const sf = row.querySelector('[data-blank-score-field]'); if (sf) { sf.classList.toggle('visible', t.checked); }
                row.classList.toggle('has-scoring', t.checked);
            });
            const scoreSummary = item.querySelector('[data-score-summary]');
            if (scoreSummary) { scoreSummary.classList.toggle('d-none', !t.checked); }
            updateScoreSummary(item);
        });
    };

    // ── Score summary ──
    const updateScoreSummary = (item) => {
        const sel = item.querySelector('[data-question-type-select]');
        const type = sel ? sel.value : 'FillInBlank';
        const scoreSummary = item.querySelector('[data-score-summary]');
        const scoring = !!item.querySelector('[data-scoring-toggle]:checked');

        if (type === 'FillInBlank' && scoring && scoreSummary) {
            let total = 0;
            toArray(item.querySelectorAll('[data-blank-answer-item] [data-blank-score]')).forEach(inp => {
                total += parseInt(inp.value, 10) || 0;
            });
            const totalEl = scoreSummary.querySelector('[data-score-total]');
            if (totalEl) { totalEl.textContent = total; }
            scoreSummary.classList.remove('is-valid', 'is-invalid');
            scoreSummary.classList.add(total === 100 ? 'is-valid' : 'is-invalid');
            scoreSummary.classList.remove('d-none');
        }
    };

    // ── Blank Groups (segment-based) ──
    let blankGroupCounter = 0;

    const getGroupSection = (item) => {
        return item.closest('.form-section')?.querySelector('[data-blank-group-section]')
            || item.querySelector('[data-blank-group-section]');
    };

    const splitLatexByTopLevelNewlines = (latex) => {
        const parts = [];
        let currentPart = '';
        let envDepth = 0;
        let i = 0;
        while (i < latex.length) {
            if (latex.substr(i, 6) === '\\begin') {
                envDepth++;
                currentPart += '\\begin';
                i += 6;
            } else if (latex.substr(i, 4) === '\\end') {
                envDepth = Math.max(0, envDepth - 1);
                currentPart += '\\end';
                i += 4;
            } else if (envDepth === 0 && latex.substr(i, 2) === '\\\\') {
                parts.push(currentPart);
                currentPart = '';
                i += 2;
            } else {
                currentPart += latex[i];
                i++;
            }
        }
        parts.push(currentPart);
        return parts;
    };

    const parseLatexSegments = (latex) => {
        if (!latex || !latex.trim()) { return []; }
        const s = latex.trim();
        const dlMatch = s.match(/^\\displaylines\s*\{([\s\S]*)\}$/);
        const inner = dlMatch ? dlMatch[1] : s;
        const parts = splitLatexByTopLevelNewlines(inner);
        return parts.map((part, i) => ({ index: i, content: part.trim() })).filter(p => p.content.length > 0);
    };

    const buildSegmentCard = (seg) => {
        const card = document.createElement('div');
        card.className = 'blank-group-segment';
        card.setAttribute('data-segment-index', String(seg.index));
        const head = document.createElement('div');
        head.className = 'blank-group-segment-head';
        head.innerHTML = `<span class="blank-group-segment-label">Dòng ${seg.index + 1}</span>` +
            `<span class="blank-group-segment-check"><svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16"><path d="M13.854 3.646a.5.5 0 0 1 0 .708l-7 7a.5.5 0 0 1-.708 0l-3.5-3.5a.5.5 0 1 1 .708-.708L6.5 10.293l6.646-6.647a.5.5 0 0 1 .708 0z"/></svg></span>` +
            `<span class="blank-group-segment-taken-label">Đã thuộc nhóm khác</span>`;
        card.appendChild(head);
        const mf = document.createElement('math-field');
        mf.setAttribute('read-only', '');
        mf.classList.add('math-display');
        setTimeout(() => setMathValue(mf, seg.content), 0);
        card.appendChild(mf);
        return card;
    };

    const syncBlankGroupSegments = (item) => {
        const section = getGroupSection(item);
        if (!section) { return; }
        const latex = getFrameLatex(item);
        const segments = parseLatexSegments(latex);
        const noHint = section.querySelector('[data-blank-group-no-placeholders]');
        if (noHint) { noHint.classList.toggle('d-none', segments.length > 0); }
        const allGroups = toArray(section.querySelectorAll('[data-blank-group-item]'));
        allGroups.forEach(groupEl => {
            const container = groupEl.querySelector('[data-blank-group-segments]');
            if (!container) { return; }
            const selectedIndices = new Set();
            toArray(container.querySelectorAll('.blank-group-segment.selected')).forEach(c => {
                selectedIndices.add(parseInt(c.getAttribute('data-segment-index'), 10));
            });
            container.innerHTML = '';
            if (segments.length === 0) {
                const emptyMsg = document.createElement('span');
                emptyMsg.className = 'blank-group-no-segments';
                emptyMsg.textContent = 'Chưa có nội dung trong khung trả lời';
                container.appendChild(emptyMsg);
                return;
            }
            segments.forEach(seg => {
                const card = buildSegmentCard(seg);
                if (selectedIndices.has(seg.index)) { card.classList.add('selected'); }
                const takenElsewhere = isSegmentTakenElsewhere(section, groupEl, seg.index);
                if (takenElsewhere && !selectedIndices.has(seg.index)) { card.classList.add('taken'); }
                card.addEventListener('click', () => {
                    if (card.classList.contains('taken')) { return; }
                    card.classList.toggle('selected');
                    refreshSegmentStates(section, segments);
                });
                container.appendChild(card);
            });
        });
    };

    const isSegmentTakenElsewhere = (section, excludeGroup, segIndex) => {
        const allGroups = toArray(section.querySelectorAll('[data-blank-group-item]'));
        for (const g of allGroups) {
            if (g === excludeGroup) { continue; }
            const card = g.querySelector(`.blank-group-segment.selected[data-segment-index="${segIndex}"]`);
            if (card) { return true; }
        }
        return false;
    };

    const refreshSegmentStates = (section, segments) => {
        const allGroups = toArray(section.querySelectorAll('[data-blank-group-item]'));
        const takenMap = new Map();
        allGroups.forEach(groupEl => {
            toArray(groupEl.querySelectorAll('.blank-group-segment.selected')).forEach(c => {
                takenMap.set(parseInt(c.getAttribute('data-segment-index'), 10), groupEl);
            });
        });
        allGroups.forEach(groupEl => {
            toArray(groupEl.querySelectorAll('.blank-group-segment')).forEach(card => {
                const idx = parseInt(card.getAttribute('data-segment-index'), 10);
                const isSelected = card.classList.contains('selected');
                const takenBy = takenMap.get(idx);
                if (takenBy && takenBy !== groupEl && !isSelected) { card.classList.add('taken'); }
                else { card.classList.remove('taken'); }
            });
        });
    };

    const createBlankGroupItem = (item, container) => {
        blankGroupCounter++;
        const groupEl = document.createElement('div');
        groupEl.className = 'blank-group-item';
        groupEl.setAttribute('data-blank-group-item', '');
        groupEl.innerHTML =
            `<div class="blank-group-header">
        <input type="text" class="blank-group-name" value="Nhóm ${blankGroupCounter}"
               data-blank-group-name placeholder="Tên nhóm…">
        <button type="button" class="blank-group-remove" data-remove-blank-group title="Xóa nhóm">
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
            <path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0V6z"/>
            <path fill-rule="evenodd" d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H5.5l1-1h3l1 1H14a1 1 0 0 1 1 1v1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4H4.118zM2.5 3V2h11v1h-11z"/>
          </svg>
        </button>
      </div>
      <div class="blank-group-segments" data-blank-group-segments></div>`;
        container.appendChild(groupEl);
        const removeBtn = groupEl.querySelector('[data-remove-blank-group]');
        if (removeBtn) {
            removeBtn.addEventListener('click', () => {
                groupEl.remove();
                const section = getGroupSection(item);
                if (section) { refreshSegmentStates(section, parseLatexSegments(getFrameLatex(item))); }
            });
        }
        syncBlankGroupSegments(item);
        return groupEl;
    };

    const bindBlankGroups = (item) => {
        const section = getGroupSection(item);
        if (!section) { return; }
        if (section.getAttribute('data-blank-group-bound') === '1') { return; }
        section.setAttribute('data-blank-group-bound', '1');
        const addBtn = section.querySelector('[data-add-blank-group]');
        const listEl = section.querySelector('[data-blank-group-list]');
        if (addBtn && listEl) { addBtn.addEventListener('click', () => createBlankGroupItem(item, listEl)); }
        syncBlankGroupSegments(item);
    };

    const resetBlankGroups = (item) => {
        const section = getGroupSection(item);
        if (!section) { return; }
        section.removeAttribute('data-blank-group-bound');
        const listEl = section.querySelector('[data-blank-group-list]');
        if (listEl) { listEl.innerHTML = ''; }
    };

    const syncBlankGroupVisibility = (item) => {
        const sel = item.querySelector('[data-question-type-select]');
        const type = sel ? sel.value : 'FillInBlank';
        const row = item.querySelector('.col-12.col-xl-4');
        if (!row) { return; }
        const section = row.querySelector('[data-blank-group-section]');
        if (section) { section.style.display = (type === 'FillInBlank') ? '' : 'none'; }
    };

    // ── Panel switching ──
    const syncQuestionTypePanel = (item) => {
        const sel = item.querySelector('[data-question-type-select]'); if (!sel) { return; }
        const type = sel.value;
        toArray(item.querySelectorAll('[data-question-type-panel]')).forEach(p => {
            p.classList.toggle('d-none', p.getAttribute('data-question-type-panel') !== type);
        });
        syncBlankGroupVisibility(item);
        if (type === 'FillInBlank') { syncPlaceholderState(item); return; }
        if (type === 'MultipleChoice') { syncMcqRows(item, Number(item.getAttribute('data-question-index')) || 1); }
    };

    // ── Populate subject/chapter dropdowns ──
    const populateSubjectDropdowns = (item) => {
        const subjectSel = item.querySelector('[data-subject-select]');
        const chapterSel = item.querySelector('[data-chapter-select]');
        if (!subjectSel || !chapterSel) { return; }
        subjectSel.innerHTML = '<option value="">Chọn môn học</option>';
        subjectsData.forEach(s => {
            const opt = document.createElement('option');
            opt.value = s.subjectId;
            opt.textContent = s.code || s.name;
            subjectSel.appendChild(opt);
        });
        subjectSel.addEventListener('change', () => {
            const subId = parseInt(subjectSel.value, 10);
            chapterSel.innerHTML = '<option value="">Chọn chương</option>';
            if (!subId) { return; }
            const sub = subjectsData.find(s => s.subjectId === subId);
            if (sub && sub.chapters) {
                sub.chapters.forEach(c => {
                    const opt = document.createElement('option');
                    opt.value = c.chapterId;
                    opt.textContent = c.name;
                    chapterSel.appendChild(opt);
                });
            }
        });
    };

    // ── Reset ──
    const resetQuestionItem = (item) => {
        toArray(item.querySelectorAll('textarea')).forEach(i => { i.value = ''; });
        toArray(item.querySelectorAll('input[type="text"]')).forEach(i => { if (!i.readOnly) { i.value = ''; } });
        toArray(item.querySelectorAll('select:not([data-subject-select]):not([data-chapter-select]):not([data-difficulty-select])')).forEach(s => { s.selectedIndex = 0; });
        toArray(item.querySelectorAll('input[type="checkbox"],input[type="radio"]')).forEach(i => { i.checked = false; });
        toArray(item.querySelectorAll('input[type="number"]')).forEach(i => { i.value = '0'; });
        toArray(item.querySelectorAll('math-field')).forEach(f => setMathValue(f, ''));
        toArray(item.querySelectorAll('.constraint-chip')).forEach(c => { c.classList.remove('active'); });
        toArray(item.querySelectorAll('[data-blank-constraint-body]')).forEach(b => b.classList.remove('open'));
        toArray(item.querySelectorAll('[data-blank-constraint-toggle]')).forEach(t => t.classList.remove('active'));
        ['[data-stem-render-toggle]', '[data-render-toggle]', '[data-mcq-render-toggle]'].forEach(sel => {
            const rt = item.querySelector(sel); if (rt) { rt.checked = true; }
        });
        toArray(item.querySelectorAll('math-field')).forEach(mf => mf.classList.remove('d-none'));
        toArray(item.querySelectorAll('.math-raw-textarea')).forEach(ta => { ta.classList.add('d-none'); ta.value = ''; });
        const st = item.querySelector('[data-scoring-toggle]'); if (st) { st.checked = false; }
        const scoreSummary = item.querySelector('[data-score-summary]'); if (scoreSummary) { scoreSummary.classList.add('d-none'); }
        const al = item.querySelector('[data-answer-list]');
        if (al) {
            let opts = toArray(al.querySelectorAll('[data-answer-item]'));
            while (opts.length < 4) { const r = createMcqOptionRow(al); if (!r) { break; } al.appendChild(r); opts = toArray(al.querySelectorAll('[data-answer-item]')); }
            while (opts.length > 4) { const l = opts.pop(); if (l) { l.remove(); } }
            opts.forEach(r => { setMathValue(r.querySelector('[data-option-content]'), ''); const rawT = r.querySelector('[data-option-raw]'); if (rawT) { rawT.value = ''; } const c = r.querySelector('[data-option-correct]'); if (c) { c.checked = false; } });
        }
        const blankList = item.querySelector('[data-blank-answer-list]');
        if (blankList) { blankList.innerHTML = ''; blankList.classList.add('d-none'); }
        resetBlankGroups(item);
        const ts = item.querySelector('[data-question-type-select]'); if (ts) { ts.value = 'FillInBlank'; }
        populateSubjectDropdowns(item);
        syncQuestionTypePanel(item);
    };

    // ── Bind ──
    const bindQuestionItem = (item) => {
        if (item.getAttribute('data-question-bound') === '1') { return; }
        item.setAttribute('data-question-bound', '1');
        const ts = item.querySelector('[data-question-type-select]');
        if (ts) { ts.addEventListener('change', () => syncQuestionTypePanel(item)); }

        const ib = item.querySelector('[data-insert-placeholder]');
        if (ib) { ib.addEventListener('click', () => { insertNumberedPlaceholder(item); syncPlaceholderState(item); }); }

        const fe = item.querySelector('[data-frame-editor]');
        if (fe) { fe.addEventListener('input', () => syncPlaceholderState(item)); }

        // Stem render toggle
        const stemToggle = item.querySelector('[data-stem-render-toggle]');
        const stemMf = item.querySelector('[data-question-stem]');
        const stemRaw = item.querySelector('[data-stem-raw]');
        if (stemToggle && stemMf && stemRaw) {
            const pair = setupPairToggle(stemMf, stemRaw);
            stemToggle.addEventListener('change', () => pair.setRenderMode(stemToggle.checked));
        }
        // Frame render toggle
        const frameToggle = item.querySelector('[data-render-toggle]');
        const frameMf = item.querySelector('[data-frame-editor]');
        const frameRaw = item.querySelector('[data-frame-raw]');
        if (frameToggle && frameMf && frameRaw) {
            const pair = setupPairToggle(frameMf, frameRaw, () => syncPlaceholderState(item));
            frameToggle.addEventListener('change', () => pair.setRenderMode(frameToggle.checked));
        }
        // MCQ render toggle
        const mcqToggle = item.querySelector('[data-mcq-render-toggle]');
        if (mcqToggle) { mcqToggle.addEventListener('change', () => applyMcqRenderMode(item, mcqToggle.checked)); }

        // Score input listeners
        item.addEventListener('input', (e) => {
            if (e.target.matches('[data-blank-score]')) { updateScoreSummary(item); }
        });

        bindScoringToggle(item);
        bindBlankGroups(item);
        populateSubjectDropdowns(item);
        syncQuestionTypePanel(item);
    };

    // ── Observe new blank rows ──
    // NOTE: bindBlankConstraints is already called inside createBlankAnswerRow.
    // Do NOT call it again here or event listeners will be doubled (toggle opens+closes instantly).
    const observer = new MutationObserver(() => { });

    // ── Sync all ──
    const syncQuestionItems = () => {
        const items = toArray(questionList.querySelectorAll('[data-question-item]'));
        items.forEach((item, idx) => {
            const num = idx + 1;
            item.setAttribute('data-question-index', String(num));
            const title = item.querySelector('[data-question-title]'); if (title) { title.textContent = `Câu hỏi #${num}`; }
            const al = item.querySelector('[data-answer-list]');
            const ab = item.querySelector('[data-add-answer]');
            if (al && ab) { const id = `q-mcq-opts-${num}`; al.id = id; ab.setAttribute('data-add-answer', id); }
            const st = item.querySelector('[data-scoring-toggle]');
            if (st) { st.id = `scoringToggle${num}`; const lbl = item.querySelector('[data-scoring-toggle-bar] label.form-check-label'); if (lbl) { lbl.setAttribute('for', st.id); } }
            const rt = item.querySelector('[data-render-toggle]');
            if (rt) { rt.id = `renderToggle${num}`; const lbl = rt.closest('.render-toggle-bar')?.querySelector('label'); if (lbl) { lbl.setAttribute('for', rt.id); } }
            const srt = item.querySelector('[data-stem-render-toggle]');
            if (srt) { srt.id = `stemRenderToggle${num}`; const lbl = srt.closest('.render-toggle-bar')?.querySelector('label'); if (lbl) { lbl.setAttribute('for', srt.id); } }
            const mrt = item.querySelector('[data-mcq-render-toggle]');
            if (mrt) { mrt.id = `mcqRenderToggle${num}`; const lbl = mrt.closest('.render-toggle-bar')?.querySelector('label'); if (lbl) { lbl.setAttribute('for', mrt.id); } }
            const rb = item.querySelector('[data-remove-question]'); if (rb) { rb.disabled = items.length === 1; }
            bindQuestionItem(item);
            const bl = item.querySelector('[data-blank-answer-list]');
            if (bl) { observer.observe(bl, { childList: true }); }
            syncMcqRows(item, num);
        });
        if (countText) { countText.textContent = `Đang soạn: ${items.length} câu hỏi`; }
    };

    addQuestionBtn.addEventListener('click', () => {
        const base = questionList.querySelector('[data-question-item]'); if (!base) { return; }
        const clone = base.cloneNode(true);
        clone.removeAttribute('data-question-bound');
        resetQuestionItem(clone);
        questionList.appendChild(clone);
        syncQuestionItems();
    });

    questionList.addEventListener('click', (e) => {
        if (e.target.closest('[data-add-answer]')) {
            const btn = e.target.closest('[data-add-answer]');
            const listId = btn.getAttribute('data-add-answer');
            const list = document.getElementById(listId);
            if (list) { const r = createMcqOptionRow(list); if (r) { list.appendChild(r); } }
            window.setTimeout(syncQuestionItems, 0);
        }
        if (e.target.closest('[data-remove-answer]')) {
            const btn = e.target.closest('[data-remove-answer]');
            const row = btn.closest('[data-answer-item]');
            if (row) { row.remove(); }
            window.setTimeout(syncQuestionItems, 0);
        }
        const rb = e.target.closest('[data-remove-question]'); if (!rb) { return; }
        const items = toArray(questionList.querySelectorAll('[data-question-item]')); if (items.length <= 1) { return; }
        const item = rb.closest('[data-question-item]'); if (!item) { return; }
        item.remove(); syncQuestionItems();
    });

    // ══════════════════════════════════════
    //  COLLECT & SUBMIT
    // ══════════════════════════════════════
    const collectPayload = (status) => {
        const items = toArray(questionList.querySelectorAll('[data-question-item]'));
        const questions = [];

        for (const item of items) {
            const sel = item.querySelector('[data-question-type-select]');
            const type = sel ? sel.value : 'FillInBlank';

            // Stem
            const stemRaw = item.querySelector('[data-stem-raw]');
            const stemMf = item.querySelector('[data-question-stem]');
            const stem = (stemRaw && stemRaw.value) ? stemRaw.value : getMathValue(stemMf);

            // Frame + explanation
            const frame = getFrameLatex(item);
            const explanation = item.querySelector('[data-explanation]')?.value || '';

            // Chapter + difficulty
            const chapterId = parseInt(item.querySelector('[data-chapter-select]')?.value, 10) || 0;
            const difficulty = parseInt(item.querySelector('[data-difficulty-select]')?.value, 10) || 1;

            const answers = [];
            if (type === 'FillInBlank') {
                const scoring = !!item.querySelector('[data-scoring-toggle]:checked');
                toArray(item.querySelectorAll('[data-blank-answer-item]')).forEach(row => {
                    const blankNum = parseInt(row.getAttribute('data-blank-num'), 10) || 0;
                    const answerMf = row.querySelector('[data-blank-answer]');
                    const correctAnswer = getMathValue(answerMf);
                    // Get selected InputTypeId
                    const selectedChip = row.querySelector('.constraint-chip.active');
                    const inputTypeId = selectedChip ? parseInt(selectedChip.getAttribute('data-input-type-id'), 10) : null;
                    const scoreInput = row.querySelector('[data-blank-score]');
                    const point = scoring ? (parseInt(scoreInput?.value, 10) || 0) : 0;
                    answers.push({
                        content: `placeholder[${blankNum}]{}`,
                        correctAnswer: correctAnswer,
                        isCorrect: true,
                        inputTypeId: inputTypeId,
                        blankIndex: blankNum,
                        point: point
                    });
                });

                // If not using custom scoring, distribute evenly
                if (!scoring && answers.length > 0) {
                    const perBlank = Math.floor(100 / answers.length);
                    const remainder = 100 - perBlank * answers.length;
                    answers.forEach((a, i) => { a.point = perBlank + (i === 0 ? remainder : 0); });
                }
            } else if (type === 'mcq') {
                const mcqScore = parseInt(item.querySelector('[data-mcq-score]')?.value, 10) || 0;
                toArray(item.querySelectorAll('[data-answer-list] [data-answer-item]')).forEach(row => {
                    const optMf = row.querySelector('[data-option-content]');
                    const optRaw = row.querySelector('[data-option-raw]');
                    const content = (optRaw && optRaw.value) ? optRaw.value : getMathValue(optMf);
                    const isCorrect = row.querySelector('[data-option-correct]')?.checked || false;
                    answers.push({
                        content: content,
                        correctAnswer: content,
                        isCorrect: isCorrect,
                        inputTypeId: null,
                        blankIndex: null,
                        point: isCorrect ? mcqScore : 0
                    });
                });

                // For MCQ, ensure total = 100: distribute among correct answers
                const correctAnswers = answers.filter(a => a.isCorrect);
                if (correctAnswers.length > 0) {
                    const perCorrect = Math.floor(100 / correctAnswers.length);
                    const remainder = 100 - perCorrect * correctAnswers.length;
                    correctAnswers.forEach((a, i) => { a.point = perCorrect + (i === 0 ? remainder : 0); });
                }
            }

            const blankGroups = [];
            const groupSection = getGroupSection(item);
            if (groupSection && type === 'FillInBlank') {
                const groupLatex = getFrameLatex(item);
                const groupSegments = parseLatexSegments(groupLatex);
                toArray(groupSection.querySelectorAll('[data-blank-group-item]')).forEach(groupEl => {
                    const name = groupEl.querySelector('[data-blank-group-name]')?.value || 'Nhóm';
                    const segIndices = [];
                    const blankIndices = [];
                    toArray(groupEl.querySelectorAll('.blank-group-segment.selected')).forEach(c => {
                        const segIdx = parseInt(c.getAttribute('data-segment-index'), 10);
                        segIndices.push(segIdx);
                        // Find the segment content and extract placeholder numbers
                        const seg = groupSegments.find(s => s.index === segIdx);
                        if (seg) {
                            getNumberedPlaceholders(seg.content).forEach(n => blankIndices.push(n));
                        }
                    });
                    if (segIndices.length > 0) {
                        const uniqueBlanks = Array.from(new Set(blankIndices));
                        blankGroups.push({ name: name, segmentIndices: segIndices, blankIndices: uniqueBlanks });
                    }
                });
            }

            const backendType = type;

            questions.push({
                questionType: backendType,
                stem: stem,
                frame: type === 'FillInBlank' ? frame : null,
                explanation: explanation,
                chapterId: chapterId,
                difficulty: difficulty,
                status: status,
                answers: answers,
                blankGroups: blankGroups.length > 0 ? blankGroups : null
            });
        }
        return { questions };
    };

    const validateBeforeSave = () => {
        const errors = [];
        const items = toArray(questionList.querySelectorAll('[data-question-item]'));
        items.forEach((item, idx) => {
            const prefix = `Câu hỏi #${idx + 1}`;
            const type = item.querySelector('[data-question-type-select]')?.value || 'FillInBlank';
            const stemRaw = item.querySelector('[data-stem-raw]');
            const stemMf = item.querySelector('[data-question-stem]');
            const stem = (stemRaw && stemRaw.value) ? stemRaw.value : getMathValue(stemMf);
            if (!stem.trim()) { errors.push(`${prefix}: Đề bài không được để trống.`); }
            const chapterId = parseInt(item.querySelector('[data-chapter-select]')?.value, 10);
            if (!chapterId) { errors.push(`${prefix}: Chưa chọn chương.`); }

            if (type === 'FillInBlank') {
                const blankRows = toArray(item.querySelectorAll('[data-blank-answer-item]'));
                if (blankRows.length === 0) { errors.push(`${prefix}: Phải có ít nhất 1 ô trống.`); }
                blankRows.forEach((row, j) => {
                    const selectedChip = row.querySelector('.constraint-chip.active');
                    if (!selectedChip) { errors.push(`${prefix}, ô trống #${j + 1}: Phải chọn ít nhất 1 giới hạn nhập liệu.`); }
                });
                const scoring = !!item.querySelector('[data-scoring-toggle]:checked');
                if (scoring) {
                    let total = 0;
                    blankRows.forEach(row => { total += parseInt(row.querySelector('[data-blank-score]')?.value, 10) || 0; });
                    if (total !== 100) { errors.push(`${prefix}: Tổng hệ số điểm phải bằng 100% (hiện tại: ${total}%).`); }
                }
            } else if (type === 'MultipleChoice') {
                const opts = toArray(item.querySelectorAll('[data-answer-list] [data-answer-item]'));
                if (opts.length < 2) { errors.push(`${prefix}: Phải có ít nhất 2 lựa chọn.`); }
                const hasCorrect = opts.some(r => r.querySelector('[data-option-correct]')?.checked);
                if (!hasCorrect) { errors.push(`${prefix}: Phải có ít nhất 1 đáp án đúng.`); }
            }
        });
        return errors;
    };

    const submitQuestions = async (status) => {
        if (status === 'Active') {
            const errors = validateBeforeSave();
            if (errors.length > 0) {
                alert('Không thể lưu:\n\n' + errors.join('\n'));
                return;
            }
        }
        const payload = collectPayload(status);
        try {
            const result = await apiClient.post('/api/questions/batch', payload);
            alert(`Đã lưu thành công ${result.count} câu hỏi!`);
            window.location.href = '/Question';
        } catch (err) {
            const msg = err.message || 'Lỗi khi lưu câu hỏi.';
            const details = err.xhr?.responseJSON?.errors;
            if (details && details.length > 0) {
                alert('Lỗi:\n\n' + details.join('\n'));
            } else {
                alert(msg);
            }
        }
    };

    if (saveAllBtn) { saveAllBtn.addEventListener('click', () => submitQuestions('Active')); }
    if (saveDraftBtn) { saveDraftBtn.addEventListener('click', () => submitQuestions('Draft')); }

    // ══════════════════════════════════════
    //  INIT — Load API data then setup UI
    // ══════════════════════════════════════
    const init = async () => {
        try {
            const [inputTypes, subjects] = await Promise.all([
                apiClient.get('/api/questions/input-types'),
                apiClient.get('/api/questions/subjects')
            ]);
            inputTypesData = inputTypes || [];
            subjectsData = subjects || [];
        } catch (err) {
            console.error('Failed to load initial data', err);
        }
        syncQuestionItems();
    };

    init();
})();
