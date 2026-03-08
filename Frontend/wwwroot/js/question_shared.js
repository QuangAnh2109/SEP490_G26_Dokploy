/**
 * QuestionEditor Shared Library (Phase 2 - Deep Consolidation)
 * Centralizes ALL logic for a single Question Item.
 */
window.QuestionEditor = (() => {
    'use strict';
    const toArray = (v) => Array.from(v || []);

    // ── Internal Helpers ──
    const getMathValue = (f) => {
        if (!f) return '';
        if (typeof f.getValue === 'function') { 
            const l = f.getValue('latex'); 
            return typeof l === 'string' ? l : ''; 
        }
        if (typeof f.value === 'string') return f.value;
        return f.textContent || '';
    };

    const setMathValue = (f, v) => {
        if (!f) return;
        if (typeof f.setValue === 'function') { 
            // Simple normalization for comparison
            const current = (f.getValue('latex') || '').replace(/\s+/g, '');
            const target = (v || '').replace(/\s+/g, '');
            if (current === target) return;
            f.setValue(v, { silenceNotifications: true }); 
            return; 
        }
        if (typeof f.value === 'string') { if (f.value === v) return; f.value = v; return; }
        if (f.textContent === v) return;
        f.textContent = v;
    };

    const getNumberedPlaceholders = (latex) => {
        const s = typeof latex === 'string' ? latex : '';
        const matches = s.matchAll(/\\placeholder\s*\[(\d+)\]/g);
        const nums = new Set();
        for (const m of matches) { nums.add(parseInt(m[1], 10)); }
        return Array.from(nums).sort((a, b) => a - b);
    };

    const getFrameLatex = (item) => {
        const raw = item.querySelector('[data-frame-raw]');
        if (raw && !raw.classList.contains('d-none')) { return raw.value; }
        const editor = item.querySelector('[data-frame-editor]');
        return getMathValue(editor);
    };

    const setupPairToggle = (mathField, rawTextarea, onChange) => {
        if (!mathField || !rawTextarea) return { setRenderMode: () => { } };

        let mirroring = false;
        let currentRenderOn = !mathField.classList.contains('d-none');

        const setRenderMode = (renderOn) => {
            if (mirroring || renderOn === currentRenderOn) return;
            mirroring = true;
            currentRenderOn = renderOn;
            try {
                if (renderOn) {
                    setMathValue(mathField, rawTextarea.value);
                    mathField.classList.remove('d-none');
                    rawTextarea.classList.add('d-none');
                } else {
                    rawTextarea.value = getMathValue(mathField);
                    mathField.classList.add('d-none');
                    rawTextarea.classList.remove('d-none');
                    if (document.activeElement !== rawTextarea) rawTextarea.focus();
                }
                if (onChange) onChange();
            } finally {
                // Short delay to ensure browser/MathLive events settle
                setTimeout(() => { mirroring = false; }, 100);
            }
        };

        mathField.addEventListener('input', () => { 
            if (mirroring) return;
            mirroring = true;
            rawTextarea.value = getMathValue(mathField); 
            if (onChange) onChange(); 
            // Use a slight delay for mirroring to settle synchronously
            setTimeout(() => { mirroring = false; }, 10);
        });

        rawTextarea.addEventListener('input', () => { 
            if (mirroring) return;
            mirroring = true;
            if (onChange) onChange(); 
            setTimeout(() => { mirroring = false; }, 10);
        });

        return { setRenderMode };
    };

    const buildConstraintChipsHtml = (inputTypesData) => {
        if (!inputTypesData || inputTypesData.length === 0) return '<p class="form-note">Đang tải...</p>';
        const groups = {};
        inputTypesData.forEach(it => {
            const g = it.groupType || it.GroupType || '__none__';
            if (!groups[g]) groups[g] = [];
            groups[g].push(it);
        });
        let html = '';
        for (const [groupType, items] of Object.entries(groups)) {
            const label = groupType === '__none__' ? 'Khác' : groupType;
            html += `<div class="constraint-section"><div class="constraint-section-label">${label}</div><div class="constraint-grid">`;
            items.forEach(it => {
                const id = it.inputTypeId || it.InputTypeId;
                const name = it.name || it.Name;
                html += `<div class="constraint-chip" data-group-type="${groupType}" data-input-type-id="${id}"><span>${name}</span></div>`;
            });
            html += '</div></div>';
        }
        return html;
    };

    const parseLatexSegments = (latex) => {
        if (!latex || !latex.trim()) return [];
        let s = latex.trim();
        const dlMatch = s.match(/^\\displaylines\s*\{([\s\S]*)\}$/);
        const inner = dlMatch ? dlMatch[1] : s;

        const segments = [];
        let current = "";
        let envDepth = 0;
        let braceDepth = 0;

        for (let i = 0; i < inner.length; i++) {
            const char = inner[i];

            if (char === '\\') {
                if (inner.startsWith("begin", i + 1)) { envDepth++; }
                else if (inner.startsWith("end", i + 1)) { envDepth = Math.max(0, envDepth - 1); }
            }

            if (char === '{' && (i === 0 || inner[i - 1] !== '\\')) {
                braceDepth++;
            } else if (char === '}' && (i === 0 || inner[i - 1] !== '\\')) {
                braceDepth = Math.max(0, braceDepth - 1);
            }

            if (char === '\\' && inner[i + 1] === '\\' && envDepth === 0 && braceDepth === 0) {
                const trimmed = current.trim();
                if (trimmed) segments.push(trimmed);
                current = "";
                i++; 
                continue;
            }

            current += char;
        }

        const last = current.trim();
        if (last) segments.push(last);

        return segments.map((content, i) => ({ index: i, content }));
    };

    const syncMcqRows = (list) => {
        const rows = toArray(list.querySelectorAll('[data-answer-item]'));
        rows.forEach((row, i) => {
            const idx = row.querySelector('[data-answer-index]');
            if (idx) idx.textContent = String.fromCharCode(65 + i);
            const rb = row.querySelector('[data-remove-answer]');
            if (rb) rb.disabled = rows.length <= 2;
        });
    };

    const createMcqOptionRow = (item, list, initialData = null) => {
        const t = item.querySelector('[data-answer-template]');
        if (!t) return null;
        const r = t.cloneNode(true);
        r.removeAttribute('data-answer-template');
        r.setAttribute('data-answer-item', '');
        r.classList.remove('d-none');
        const mf = r.querySelector('[data-option-content]'), raw = r.querySelector('[data-option-raw]'), ck = r.querySelector('[data-option-correct]');
        if (initialData) {
            setMathValue(mf, initialData.content || '');
            if (raw) raw.value = initialData.content || '';
            if (ck) ck.checked = !!initialData.isCorrect;
            if (initialData.answerId) r.setAttribute('data-answer-id', initialData.answerId);
        }
        r.querySelector('[data-remove-answer]')?.addEventListener('click', () => { r.remove(); syncMcqRows(list); });
        return r;
    };

    const bindConstraintChips = (row) => {
        toArray(row.querySelectorAll('.constraint-chip')).forEach(chip => {
            chip.addEventListener('click', () => {
                const gt = chip.getAttribute('data-group-type'), active = chip.classList.contains('active');
                toArray(row.querySelectorAll(`.constraint-chip[data-group-type="${gt}"]`)).forEach(s => s.classList.remove('active'));
                if (!active) chip.classList.add('active');
            });
        });
    };

    // ── Item Logic Coordination ──
    const syncPlaceholderState = (item, inputTypesData) => {
        const latex = getFrameLatex(item);
        const dataCount = (inputTypesData || []).length;
        const lastDataCount = item._lastDataCount || 0;
        
        if (item._lastSyncLatex === latex && dataCount === lastDataCount) return;
        item._lastSyncLatex = latex;
        item._lastDataCount = dataCount;

        const numbered = getNumberedPlaceholders(latex);
        const counter = item.querySelector('[data-placeholder-count]');
        if (counter) counter.textContent = `${numbered.length} ô trống`;

        const manager = item.querySelector('[data-placeholder-manager]'), nav = item.querySelector('[data-placeholder-nav]'), list = item.querySelector('[data-blank-answer-list]');
        if (!list) return;

        const existingMap = new Map();
        toArray(list.querySelectorAll('[data-blank-answer-item]')).forEach(r => existingMap.set(r.getAttribute('data-blank-num'), r));
        
        const currentActiveNum = list.querySelector('[data-blank-answer-item].active')?.getAttribute('data-blank-num');
        list.innerHTML = '';
        nav.innerHTML = '';

        numbered.forEach(num => {
            const sNum = String(num);
            let row = existingMap.get(sNum);
            if (!row) {
                row = document.createElement('div');
                row.className = 'blank-answer-row';
                row.setAttribute('data-blank-answer-item', '');
                row.setAttribute('data-blank-num', sNum);
                row.innerHTML = `<div class="blank-answer-main"><div class="blank-answer-header"><span class="badge rounded-pill bg-primary">Ô trống ${num}</span></div>
                    <div class="d-flex gap-2 align-items-center mb-2"><math-field class="math-input flex-grow-1" data-blank-answer placeholder="Đáp án đúng..."></math-field></div>
                    <div class="blank-score-field mb-2" data-blank-score-field><label class="form-label small mb-1">Hệ số điểm (%)</label><input class="form-control form-control-sm" type="number" value="0" min="0" max="100" data-blank-score></div>
                    <button type="button" class="blank-constraint-toggle" data-blank-constraint-toggle>Giới hạn nhập liệu</button>
                    <div class="blank-constraint-body" data-blank-constraint-body>${buildConstraintChipsHtml(inputTypesData)}</div></div>`;
                
                const toggle = row.querySelector('[data-blank-constraint-toggle]'), body = row.querySelector('[data-blank-constraint-body]');
                toggle.addEventListener('click', (e) => { e.preventDefault(); body.classList.toggle('open'); toggle.classList.toggle('active'); });
                if (inputTypesData && inputTypesData.length > 0) {
                    row._constraintsBound = true;
                    bindConstraintChips(row);
                }
            } else {
                // Refresh constraints if they were loading
                if (!row._constraintsBound && inputTypesData && inputTypesData.length > 0) {
                    const body = row.querySelector('[data-blank-constraint-body]');
                    if (body) {
                        body.innerHTML = buildConstraintChipsHtml(inputTypesData);
                        row._constraintsBound = true;
                        bindConstraintChips(row);
                    }
                }
            }
            list.appendChild(row);

            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'btn btn-sm btn-outline-primary px-3' + (sNum === currentActiveNum ? ' active' : '');
            btn.textContent = num;
            btn.addEventListener('click', () => switchActivePlaceholder(item, sNum));
            nav.appendChild(btn);
        });

        const scoring = !!item.querySelector('[data-scoring-toggle]:checked');
        toArray(list.querySelectorAll('[data-blank-answer-item]')).forEach(r => {
            r.querySelector('[data-blank-score-field]')?.classList.toggle('d-none', !scoring);
        });

        const hasPlaceholders = numbered.length > 0;
        manager?.classList.toggle('d-none', !hasPlaceholders);
        item.querySelector('[data-blank-answer-empty]')?.classList.toggle('d-none', hasPlaceholders);

        if (hasPlaceholders && !list.querySelector('[data-blank-answer-item].active')) {
            switchActivePlaceholder(item, currentActiveNum || String(numbered[0]));
        }

        updateScoreSummary(item);
        syncBlankGroupSegments(item);
    };

    const updateScoreSummary = (item) => {
        const scoring = !!item.querySelector('[data-scoring-toggle]:checked');
        const summary = item.querySelector('[data-score-total]');
        if (!summary) return;
        const bar = item.querySelector('[data-score-summary]');
        if (scoring && bar) {
            let total = 0;
            toArray(item.querySelectorAll('[data-blank-answer-item] [data-blank-score]')).forEach(i => total += parseInt(i.value) || 0);
            summary.textContent = total;
            bar.classList.remove('d-none');
            bar.classList.toggle('is-invalid', total !== 100);
            bar.classList.toggle('is-valid', total === 100);
        } else if (bar) bar.classList.add('d-none');
    };

    const syncBlankGroupSegments = (item) => {
        const latex = getFrameLatex(item), segments = parseLatexSegments(latex);
        toArray(item.querySelectorAll('[data-blank-group-item]')).forEach(g => {
            const container = g.querySelector('[data-blank-group-segments]');
            const selectedSet = new Set(toArray(container.querySelectorAll('.blank-group-segment.selected')).map(s => parseInt(s.getAttribute('data-segment-index'))));
            container.innerHTML = segments.length ? '' : '<em>Khung trả lời trống</em>';
            segments.forEach(seg => {
                const card = document.createElement('div');
                card.className = 'blank-group-segment' + (selectedSet.has(seg.index) ? ' selected' : '');
                card.setAttribute('data-segment-index', seg.index);
                card.innerHTML = `<div class="blank-group-segment-head"><span>Dòng ${seg.index + 1}</span></div><math-field read-only class="math-display">${seg.content}</math-field>`;
                card.addEventListener('click', () => card.classList.toggle('selected'));
                container.appendChild(card);
            });
        });
    };

    const switchActivePlaceholder = (item, sNum) => {
        const list = item.querySelector('[data-blank-answer-list]');
        const nav = item.querySelector('[data-placeholder-nav]');
        if (!list || !nav) return;

        toArray(list.querySelectorAll('[data-blank-answer-item]')).forEach(r => {
            r.classList.toggle('active', r.getAttribute('data-blank-num') === sNum);
        });
        toArray(nav.querySelectorAll('button')).forEach(btn => {
            btn.classList.toggle('active', btn.textContent === sNum);
        });
    };

    const syncSubjectDropdown = (item, subjectsData) => {
        const subSel = item.querySelector('[data-subject-select]'), chapSel = item.querySelector('[data-chapter-select]');
        if (!subSel || !chapSel || !subjectsData || subjectsData.length === 0) return;
        
        const currentSubId = subSel.value;
        subSel.innerHTML = '<option value="">Chọn môn học</option>';
        subjectsData.forEach(s => {
            const id = s.subjectId || s.SubjectId;
            const label = s.code || s.Code || s.name || s.Name;
            subSel.add(new Option(label, id));
        });
        if (currentSubId) subSel.value = currentSubId;

        // Re-bind change event if not already done (though initItem does it if it's new)
        if (!subSel._bound) {
            subSel._bound = true;
            subSel.addEventListener('change', () => {
                const subId = parseInt(subSel.value);
                chapSel.innerHTML = '<option value="">Chọn chương</option>';
                const sub = (item._subjectsData || []).find(s => (s.subjectId || s.SubjectId) === subId);
                const chapters = sub?.chapters || sub?.Chapters;
                if (chapters) {
                    chapters.forEach(c => {
                        const cId = c.chapterId || c.ChapterId;
                        const cName = c.name || c.Name;
                        chapSel.add(new Option(cName, cId));
                    });
                }
            });
        }
    };

    // ── Public API ──
    return {
        getMathValue, setMathValue, getNumberedPlaceholders, getFrameLatex, parseLatexSegments, syncMcqRows,
        
        initItem: (item, { inputTypesData, subjectsData } = {}) => {
            if (item.hasAttribute('data-bound')) {
                // Already bound events, just sync the data if provided
                if (inputTypesData || subjectsData) {
                    item._inputTypesData = inputTypesData || item._inputTypesData;
                    item._subjectsData = subjectsData || item._subjectsData;
                    const typeSel = item.querySelector('[data-question-type-select]');
                    if (typeSel && typeSel.value === 'FillInBlank') syncPlaceholderState(item, item._inputTypesData);
                    syncSubjectDropdown(item, item._subjectsData);
                }
                return;
            }
            item.setAttribute('data-bound', '1');
            item._inputTypesData = inputTypesData || [];
            item._subjectsData = subjectsData || [];

            const typeSel = item.querySelector('[data-question-type-select]');
            const uid = Math.random().toString(36).substring(2, 7);
            toArray(item.querySelectorAll('.form-check-input[role="switch"]')).forEach((sw, i) => {
                const id = `sw-${uid}-${i}`;
                sw.id = id;
                const label = sw.parentNode.querySelector('label');
                if (label) label.setAttribute('for', id);
            });

            const syncUI = () => {
                const type = typeSel.value;
                toArray(item.querySelectorAll('[data-question-type-panel]')).forEach(p => p.classList.toggle('d-none', p.getAttribute('data-question-type-panel') !== type));
                const gSec = item.querySelector('[data-blank-group-section]');
                if (gSec) gSec.style.display = (type === 'FillInBlank' ? '' : 'none');
                if (type === 'FillInBlank') syncPlaceholderState(item, item._inputTypesData);
                else syncMcqRows(item.querySelector('[data-answer-list]'));
            };

            typeSel.addEventListener('change', syncUI);

            const stemToggle = item.querySelector('[data-stem-render-toggle]');
            const { setRenderMode: setStemMode } = setupPairToggle(item.querySelector('[data-question-stem]'), item.querySelector('[data-stem-raw]'));
            if (stemToggle) {
                stemToggle.addEventListener('change', (e) => setStemMode(e.target.checked));
                setStemMode(stemToggle.checked);
            }

            const frameToggle = item.querySelector('[data-render-toggle]'); 
            const { setRenderMode: setFrameMode } = setupPairToggle(item.querySelector('[data-frame-editor]'), item.querySelector('[data-frame-raw]'), () => syncPlaceholderState(item, item._inputTypesData));
            if (frameToggle) {
                frameToggle.addEventListener('change', (e) => setFrameMode(e.target.checked));
                setFrameMode(frameToggle.checked);
            }

            const frameEditor = item.querySelector('[data-frame-editor]');
            frameEditor.addEventListener('placeholder-focus', (e) => {
                const phId = e.detail?.placeholderId;
                if (phId) switchActivePlaceholder(item, phId);
            });
            frameEditor.addEventListener('click', () => {
                setTimeout(() => {
                    const phId = frameEditor.getPlaceholderId?.() || frameEditor.activePlaceholderId;
                    if (phId) switchActivePlaceholder(item, phId);
                }, 50);
            });
            
            const insertBtn = item.querySelector('[data-insert-placeholder]');
            if (insertBtn) {
                insertBtn.addEventListener('click', () => {
                    const mf = item.querySelector('[data-frame-editor]'), existing = getNumberedPlaceholders(getFrameLatex(item));
                    let next = 1;
                    const sorted = existing.sort((a,b) => a-b);
                    for(const n of sorted) { if(n === next) next++; else if (n > next) break; }
                    
                    const ph = `\\placeholder[${next}]{}`;
                    if (mf.classList.contains('d-none')) {
                        const raw = item.querySelector('[data-frame-raw]');
                        let pos = raw.selectionStart || raw.value.length;
                        
                        // Decisive Raw Mode fix: if cursor is anywhere inside a \placeholder block, jump out
                        const textBefore = raw.value.slice(0, pos);
                        const openMatch = textBefore.match(/\\placeholder\s*\[\d+\]\s*\{[^}]*$/);
                        if (openMatch) {
                            const nextBrace = raw.value.indexOf('}', pos);
                            if (nextBrace !== -1) pos = nextBrace + 1;
                        }

                        const prefix = (pos > 0 && raw.value[pos-1] !== ' ' && raw.value[pos-1] !== '\n') ? ' ' : '';
                        raw.value = raw.value.slice(0, pos) + prefix + ph + raw.value.slice(pos);
                        raw.selectionStart = raw.selectionEnd = pos + prefix.length + ph.length;
                        raw.focus();
                    } else { 
                        if (typeof mf.insert === 'function') {
                            mf.focus();
                            // Force exit from any existing groups
                            for(let i=0; i<3; i++) mf.executeCommand('moveAfterParent');
                            // selectionMode: 'after' is key to prevent focus landing inside the new blank
                            mf.insert(ph, { focus: true, selectionMode: 'after' }); 
                        } else {
                            const current = getMathValue(mf);
                            setMathValue(mf, current + ph);
                        }
                    }
                    // Sync state after a brief delay to ensure UI stability
                    setTimeout(() => syncPlaceholderState(item, item._inputTypesData), 50);
                });
            }

            item.querySelector('[data-scoring-toggle]')?.addEventListener('change', () => syncPlaceholderState(item, item._inputTypesData));
            item.addEventListener('input', (e) => { if (e.target.matches('[data-blank-score]')) updateScoreSummary(item); });

            item.querySelector('[data-add-blank-group]')?.addEventListener('click', () => {
                const list = item.querySelector('[data-blank-group-list]');
                if (!list) return;
                const gEl = document.createElement('div');
                gEl.className = 'blank-group-item'; gEl.setAttribute('data-blank-group-item', '');
                gEl.innerHTML = `<div class="blank-group-header"><input type="text" class="blank-group-name" value="Nhóm ${list.children.length + 1}" data-blank-group-name><button type="button" class="blank-group-remove" data-remove-blank-group>&times;</button></div><div class="blank-group-segments" data-blank-group-segments></div>`;
                list.appendChild(gEl);
                gEl.querySelector('[data-remove-blank-group]').addEventListener('click', () => gEl.remove());
                syncBlankGroupSegments(item);
            });

            syncSubjectDropdown(item, item._subjectsData);

            item.querySelector('[data-add-answer]')?.addEventListener('click', () => {
                const list = item.querySelector('[data-answer-list]');
                if (list) {
                    list.appendChild(createMcqOptionRow(item, list));
                    syncMcqRows(list);
                }
            });

            let mcqToggleBusy = false;
            item.querySelector('[data-mcq-render-toggle]')?.addEventListener('change', (e) => {
                if (mcqToggleBusy) return;
                mcqToggleBusy = true;
                const renderOn = e.target.checked;
                try {
                    toArray(item.querySelectorAll('[data-answer-item]')).forEach(row => {
                        const mf = row.querySelector('[data-option-content]'), raw = row.querySelector('[data-option-raw]');
                        if (renderOn) { setMathValue(mf, raw.value); mf.classList.remove('d-none'); raw.classList.add('d-none'); }
                        else { raw.value = getMathValue(mf); mf.classList.add('d-none'); raw.classList.remove('d-none'); }
                    });
                } finally {
                    setTimeout(() => { mcqToggleBusy = false; }, 250);
                }
            });

            syncUI();
        },

        collectPayload: (item) => {
            const typeSel = item.querySelector('[data-question-type-select]');
            if (!typeSel) throw new Error("Không tìm thấy bộ chọn loại câu hỏi.");
            const type = typeSel.value;
            
            const stemToggle = item.querySelector('[data-stem-render-toggle]');
            const stemField = item.querySelector(stemToggle?.checked ? '[data-question-stem]' : '[data-stem-raw]');
            const stem = getMathValue(stemField);
            
            const frame = type === 'FillInBlank' ? getFrameLatex(item) : null;
            const answers = [];
            if (type === 'FillInBlank') {
                const scoring = !!item.querySelector('[data-scoring-toggle]:checked');
                toArray(item.querySelectorAll('[data-blank-answer-item]')).forEach(row => {
                    const bNum = parseInt(row.getAttribute('data-blank-num')), chip = row.querySelector('.constraint-chip.active');
                    answers.push({ answerId: parseInt(row.getAttribute('data-answer-id')) || null, content: `placeholder[${bNum}]{}`, correctAnswer: getMathValue(row.querySelector('[data-blank-answer]')), isCorrect: true, blankIndex: bNum, inputTypeId: chip ? parseInt(chip.getAttribute('data-input-type-id')) : null, point: scoring ? (parseInt(row.querySelector('[data-blank-score]').value) || 0) : 0 });
                });
                if (!scoring && answers.length > 0) {
                    const p = Math.floor(100 / answers.length);
                    answers.forEach((a, i) => a.point = p + (i === 0 ? (100 - p * answers.length) : 0));
                }
            } else {
                const mcqScore = parseInt(item.querySelector('[data-mcq-score]')?.value) || 0;
                const mcqToggle = item.querySelector('[data-mcq-render-toggle]');
                toArray(item.querySelectorAll('[data-answer-list] [data-answer-item]')).forEach(row => {
                    const val = getMathValue(row.querySelector(mcqToggle?.checked ? '[data-option-content]' : '[data-option-raw]'));
                    const ck = row.querySelector('[data-option-correct]')?.checked;
                    answers.push({ answerId: parseInt(row.getAttribute('data-answer-id')) || null, content: val, correctAnswer: val, isCorrect: !!ck, point: ck ? mcqScore : 0 });
                });
                const corrects = answers.filter(a => a.isCorrect);
                if (corrects.length > 0) {
                    const p = Math.floor(100 / corrects.length);
                    corrects.forEach((a, i) => a.point = p + (i === 0 ? (100 - p * corrects.length) : 0));
                }
            }
            const groups = [];
            if (type === 'FillInBlank') {
                toArray(item.querySelectorAll('[data-blank-group-item]')).forEach(g => {
                    const segIdxs = toArray(g.querySelectorAll('.blank-group-segment.selected')).map(c => parseInt(c.getAttribute('data-segment-index'))), blanks = [], segments = parseLatexSegments(frame);
                    segIdxs.forEach(si => getNumberedPlaceholders(segments.find(s => s.index === si)?.content).forEach(n => blanks.push(n)));
                    if (segIdxs.length > 0) groups.push({ groupAnswerId: parseInt(g.getAttribute('data-group-id')) || null, name: g.querySelector('[data-blank-group-name]')?.value || 'Nhóm', segmentIndices: segIdxs, blankIndices: [...new Set(blanks)] });
                });
            }
            return { questionType: type, stem, frame, explanation: item.querySelector('[data-explanation]')?.value || '', chapterId: parseInt(item.querySelector('[data-chapter-select]')?.value) || null, difficulty: parseInt(item.querySelector('[data-difficulty-select]')?.value) || 1, answers, blankGroups: groups.length > 0 ? groups : null };
        },

        setData: (item, data, { inputTypesData, subjectsData }) => {
            item.querySelector('[data-question-type-select]').value = data.questionType;
            item.querySelector('[data-difficulty-select]').value = data.difficulty;
            item.querySelector('[data-explanation]').value = data.explanation || '';
            setMathValue(item.querySelector('[data-question-stem]'), data.stem || '');
            
            const sub = subjectsData.find(s => s.chapters?.some(c => c.chapterId === data.chapterId));
            if (sub) {
                const sSel = item.querySelector('[data-subject-select]'), cSel = item.querySelector('[data-chapter-select]');
                sSel.value = sub.subjectId; 
                sSel.dispatchEvent(new Event('change'));
                setTimeout(() => cSel.value = data.chapterId, 50);
            }

            item.querySelector('[data-question-type-select]').dispatchEvent(new Event('change'));

            if (data.questionType === 'FillInBlank') {
                setMathValue(item.querySelector('[data-frame-editor]'), data.frame || '');
                setTimeout(() => {
                    syncPlaceholderState(item, inputTypesData);
                    setTimeout(() => {
                        let hasCustom = false;
                        data.answers?.forEach(ans => {
                            const row = item.querySelector(`[data-blank-num="${ans.blankIndex}"]`);
                            if (row) {
                                if (ans.answerId) row.setAttribute('data-answer-id', ans.answerId);
                                setMathValue(row.querySelector('[data-blank-answer]'), ans.correctAnswer || '');
                                if (ans.inputTypeId) row.querySelector(`.constraint-chip[data-input-type-id="${ans.inputTypeId}"]`)?.classList.add('active');
                                const sInp = row.querySelector('[data-blank-score]');
                                if (sInp && ans.point > 0) { sInp.value = ans.point; hasCustom = true; }
                            }
                        });
                        if (hasCustom) { const st = item.querySelector('[data-scoring-toggle]'); st.checked = true; st.dispatchEvent(new Event('change')); }
                        data.blankGroups?.forEach(g => {
                            item.querySelector('[data-add-blank-group]').click();
                            const gEl = item.querySelector('[data-blank-group-list]').lastElementChild;
                            gEl.setAttribute('data-group-id', g.groupAnswerId);
                            gEl.querySelector('[data-blank-group-name]').value = g.name || 'Nhóm';
                            const segs = parseLatexSegments(data.frame);
                            g.blankIndices?.forEach(ph => {
                                const si = segs.findIndex(s => getNumberedPlaceholders(s.content).includes(ph));
                                if (si !== -1) gEl.querySelector(`[data-segment-index="${si}"]`)?.classList.add('selected');
                            });
                        });
                    }, 100);
                }, 300);
            } else {
                const list = item.querySelector('[data-answer-list]');
                toArray(list.querySelectorAll('[data-answer-item]')).forEach(r => r.remove());
                data.answers?.forEach(ans => {
                    const row = createMcqOptionRow(item, list, ans);
                    list.appendChild(row);
                    if (ans.point > 0) item.querySelector('[data-mcq-score]').value = ans.point;
                });
                syncMcqRows(list);
            }
        }
    };
})();
