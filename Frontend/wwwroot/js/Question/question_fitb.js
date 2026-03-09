window.QuestionEditorFITB = (() => {
    'use strict';
    const UTILS = window.QuestionEditorUtils;

    const renderConstraintChips = (row, inputTypesData) => {
        const loading = row.querySelector('[data-blank-constraint-loading]'), container = row.querySelector('[data-constraint-container]');
        if (loading) loading.classList.toggle('d-none', !!inputTypesData?.length);
        if (!container || !inputTypesData?.length) return;
        while (container.firstChild) container.removeChild(container.firstChild);
        const groups = {};
        inputTypesData.forEach(it => {
            const g = it.groupType || it.GroupType || '__none__';
            if (!groups[g]) groups[g] = [];
            groups[g].push(it);
        });

        const sectionT = row.querySelector('[data-constraint-section-template]'), chipT = row.querySelector('[data-constraint-chip-template]');
        for (const [groupType, items] of Object.entries(groups)) {
            const section = sectionT?.content.cloneNode(true).firstElementChild;
            if (!section) continue;
            section.querySelector('[data-constraint-group-label]').textContent = groupType === '__none__' ? 'Khác' : groupType;
            const grid = section.querySelector('[data-constraint-grid]');
            items.forEach(it => {
                const chip = chipT?.content.cloneNode(true).firstElementChild;
                if (chip) {
                    const id = it.inputTypeId || it.InputTypeId;
                    chip.setAttribute('data-group-type', groupType); chip.setAttribute('data-input-type-id', id);
                    chip.querySelector('[data-constraint-name]').textContent = it.name || it.Name;
                    grid.appendChild(chip);
                }
            });
            container.appendChild(section);
        }
    };

    const bindConstraintChips = (row) => {
        UTILS.toArray(row.querySelectorAll('.constraint-chip')).forEach(chip => {
            chip.addEventListener('click', () => {
                const gt = chip.getAttribute('data-group-type'), active = chip.classList.contains('active');
                UTILS.toArray(row.querySelectorAll(`.constraint-chip[data-group-type="${gt}"]`)).forEach(s => s.classList.remove('active'));
                if (!active) chip.classList.add('active');
            });
        });
    };

    const switchActivePlaceholder = (item, sNum) => {
        const list = item.querySelector('[data-blank-answer-list]');
        const nav = item.querySelector('[data-placeholder-nav]');
        if (!list || !nav) return;
        UTILS.toArray(list.querySelectorAll('[data-blank-answer-item]')).forEach(r => {
            r.classList.toggle('active', r.getAttribute('data-blank-num') === sNum);
        });
        UTILS.toArray(nav.querySelectorAll('button')).forEach(btn => {
            btn.classList.toggle('active', btn.textContent === sNum);
        });
    };

    const updateScoreSummary = (item) => {
        const scoring = !!item.querySelector('[data-scoring-toggle]:checked');
        const summary = item.querySelector('[data-score-total]');
        if (!summary) return;
        const bar = item.querySelector('[data-score-summary]');
        if (scoring && bar) {
            let total = 0;
            UTILS.toArray(item.querySelectorAll('[data-blank-answer-item] [data-blank-score]')).forEach(i => total += parseInt(i.value) || 0);
            summary.textContent = total;
            bar.classList.remove('d-none');
            bar.classList.toggle('is-invalid', total !== 100);
            bar.classList.toggle('is-valid', total === 100);
        } else if (bar) bar.classList.add('d-none');
    };

    const syncBlankGroupSegments = (item) => {
        const latex = UTILS.getFrameLatex(item), segments = UTILS.parseLatexSegments(latex);
        UTILS.toArray(item.querySelectorAll('[data-blank-group-item]')).forEach(g => {
            const container = g.querySelector('[data-segments-container]'), t = g.querySelector('[data-blank-group-segment-template]');
            const emptyNote = g.querySelector('[data-blank-group-empty-note]');
            if (emptyNote) emptyNote.classList.toggle('d-none', segments.length > 0);
            const selectedSet = new Set(UTILS.toArray(g.querySelectorAll('.blank-group-segment.selected')).map(s => parseInt(s.getAttribute('data-segment-index'))));
            while (container.firstChild) container.removeChild(container.firstChild);
            segments.forEach(seg => {
                const card = t?.content.cloneNode(true).firstElementChild;
                if (card) {
                    card.classList.toggle('selected', selectedSet.has(seg.index));
                    card.setAttribute('data-segment-index', seg.index);
                    card.querySelector('[data-segment-label]').textContent = `Dòng ${seg.index + 1}`;
                    UTILS.setMathValue(card.querySelector('[data-segment-content]'), seg.content);
                    card.addEventListener('click', () => card.classList.toggle('selected'));
                    container.appendChild(card);
                }
            });
        });
    };

    const createBlankGroupItem = (item, list, initialData = null) => {
        const t = item.querySelector('[data-blank-group-template]');
        if (!t) return null;
        const r = t.cloneNode(true);
        r.removeAttribute('data-blank-group-template');
        r.setAttribute('data-blank-group-item', '');
        r.classList.remove('d-none');
        const nameInp = r.querySelector('[data-blank-group-name]');
        if (initialData) {
            if (nameInp) nameInp.value = initialData.name || 'Nhóm';
            if (initialData.groupAnswerId) r.setAttribute('data-group-id', initialData.groupAnswerId);
        } else {
            if (nameInp) nameInp.value = `Nhóm ${list.querySelectorAll('[data-blank-group-item]').length + 1}`;
        }
        r.querySelector('[data-remove-blank-group]')?.addEventListener('click', () => r.remove());
        return r;
    };

    const syncPlaceholderState = (item, inputTypesData) => {
        const latex = UTILS.getFrameLatex(item);
        const dataCount = (inputTypesData || []).length;
        if (item._lastSyncLatex === latex && item._lastDataCount === dataCount) return;
        item._lastSyncLatex = latex; item._lastDataCount = dataCount;

        const numbered = UTILS.getNumberedPlaceholders(latex);
        const counter = item.querySelector('[data-placeholder-count]');
        if (counter) counter.textContent = `${numbered.length} ô trống`;

        const manager = item.querySelector('[data-placeholder-manager]'),
            nav = item.querySelector('[data-placeholder-nav]'),
            list = item.querySelector('[data-blank-answer-list]');
        if (!list) return;

        const existingMap = new Map();
        UTILS.toArray(list.querySelectorAll('[data-blank-answer-item]')).forEach(r => existingMap.set(r.getAttribute('data-blank-num'), r));
        const currentActiveNum = list.querySelector('[data-blank-answer-item].active')?.getAttribute('data-blank-num');
        while (list.firstChild) list.removeChild(list.firstChild);
        while (nav.firstChild) {
            if (nav.firstChild.tagName === 'TEMPLATE') break; // Keep the template
            nav.removeChild(nav.firstChild);
        }

        numbered.forEach(num => {
            const sNum = String(num);
            let row = existingMap.get(sNum);
            if (!row) {
                const t = item.querySelector('[data-blank-answer-template]');
                row = t?.content.cloneNode(true).firstElementChild;
                if (row) {
                    row.setAttribute('data-blank-answer-item', ''); row.setAttribute('data-blank-num', sNum);
                    const label = row.querySelector('[data-blank-label]');
                    if (label) label.textContent = `Ô trống ${num}`;

                    const toggle = row.querySelector('[data-blank-constraint-toggle]'), body = row.querySelector('[data-blank-constraint-body]');
                    toggle?.addEventListener('click', (e) => { e.preventDefault(); body.classList.toggle('open'); toggle.classList.toggle('active'); });
                    if (inputTypesData?.length > 0) {
                        renderConstraintChips(row, inputTypesData);
                        row._constraintsBound = true; bindConstraintChips(row);
                    }
                }
            } else if (!row._constraintsBound && inputTypesData?.length > 0) {
                renderConstraintChips(row, inputTypesData); row._constraintsBound = true; bindConstraintChips(row);
            }
            if (row) list.appendChild(row);

            const btnT = item.querySelector('[data-blank-nav-button-template]');
            const btn = btnT?.content.cloneNode(true).firstElementChild;
            if (btn) {
                if (sNum === currentActiveNum) btn.classList.add('active');
                btn.textContent = num; btn.addEventListener('click', () => switchActivePlaceholder(item, sNum));
                nav.appendChild(btn);
            }
        });

        const scoring = !!item.querySelector('[data-scoring-toggle]:checked');
        UTILS.toArray(list.querySelectorAll('[data-blank-answer-item]')).forEach(r => r.querySelector('[data-blank-score-field]')?.classList.toggle('d-none', !scoring));
        manager?.classList.toggle('d-none', numbered.length === 0);
        item.querySelector('[data-blank-answer-empty]')?.classList.toggle('d-none', numbered.length > 0);

        if (numbered.length > 0 && !list.querySelector('[data-blank-answer-item].active')) {
            switchActivePlaceholder(item, currentActiveNum || String(numbered[0]));
        }
        updateScoreSummary(item);
        syncBlankGroupSegments(item);
    };

    const init = (item) => {
        item.querySelector('[data-insert-placeholder]')?.addEventListener('click', () => {
            const mf = item.querySelector('[data-frame-editor]'), latex = UTILS.getFrameLatex(item), existing = UTILS.getNumberedPlaceholders(latex);
            let next = 1; for (const n of existing.sort((a, b) => a - b)) { if (n === next) next++; else if (n > next) break; }
            const ph = `\\placeholder[${next}]{}`;
            if (mf.classList.contains('d-none')) {
                const raw = item.querySelector('[data-frame-raw]');
                let pos = raw.selectionStart || raw.value.length;
                const textBefore = raw.value.slice(0, pos), openMatch = textBefore.match(/\\placeholder\s*\[\d+\]\s*\{[^}]*$/);
                if (openMatch) { const nextBrace = raw.value.indexOf('}', pos); if (nextBrace !== -1) pos = nextBrace + 1; }
                const prefix = (pos > 0 && raw.value[pos - 1] !== ' ' && raw.value[pos - 1] !== '\n') ? ' ' : '';
                raw.value = raw.value.slice(0, pos) + prefix + ph + raw.value.slice(pos);
                raw.selectionStart = raw.selectionEnd = pos + prefix.length + ph.length; raw.focus();
            } else {
                if (typeof mf.insert === 'function') {
                    mf.focus(); for (let i = 0; i < 3; i++) mf.executeCommand('moveAfterParent');
                    mf.insert(ph, { focus: true, selectionMode: 'after' });
                } else UTILS.setMathValue(mf, UTILS.getMathValue(mf) + ph);
            }
            setTimeout(() => syncPlaceholderState(item, item._inputTypesData), 50);
        });

        item.querySelector('[data-scoring-toggle]')?.addEventListener('change', () => syncPlaceholderState(item, item._inputTypesData));
        item.addEventListener('input', (e) => { if (e.target.matches('[data-blank-score]')) updateScoreSummary(item); });

        item.querySelector('[data-add-blank-group]')?.addEventListener('click', () => {
            const list = item.querySelector('[data-blank-group-list]');
            const gEl = createBlankGroupItem(item, list);
            if (gEl) { list.appendChild(gEl); syncBlankGroupSegments(item); }
        });
    };

    const getPayload = (item, frame) => {
        const answers = [];
        const scoring = !!item.querySelector('[data-scoring-toggle]:checked');
        UTILS.toArray(item.querySelectorAll('[data-blank-answer-item]')).forEach(row => {
            const bNum = parseInt(row.getAttribute('data-blank-num')), chip = row.querySelector('.constraint-chip.active');
            answers.push({ answerId: parseInt(row.getAttribute('data-answer-id')) || null, content: `placeholder[${bNum}]{}`, correctAnswer: UTILS.getMathValue(row.querySelector('[data-blank-answer]')), isCorrect: true, blankIndex: bNum, inputTypeId: chip ? parseInt(chip.getAttribute('data-input-type-id')) : null, point: scoring ? (parseInt(row.querySelector('[data-blank-score]').value) || 0) : 0 });
        });
        if (!scoring && answers.length > 0) {
            const p = Math.floor(100 / answers.length);
            answers.forEach((a, i) => a.point = p + (i === 0 ? (100 - p * answers.length) : 0));
        }
        const groups = [];
        const groupsList = UTILS.toArray(item.querySelectorAll('[data-blank-group-item]'));
        const segments = UTILS.parseLatexSegments(frame);
        groupsList.forEach(g => {
            const selectedSegIdxs = UTILS.toArray(g.querySelectorAll('.blank-group-segment.selected')).map(c => parseInt(c.getAttribute('data-segment-index')));
            const blanks = [];
            selectedSegIdxs.forEach(si => {
                const seg = segments.find(s => s.index === si);
                if (seg) UTILS.getNumberedPlaceholders(seg.content).forEach(n => blanks.push(n));
            });
            if (blanks.length > 0) {
                groups.push({ groupAnswerId: parseInt(g.getAttribute('data-group-id')) || null, name: g.querySelector('[data-blank-group-name]')?.value || 'Nhóm', segmentIndices: selectedSegIdxs, blankIndices: [...new Set(blanks)] });
            }
        });
        return { answers, blankGroups: groups.length > 0 ? groups : null };
    };

    const setData = (item, data, inputTypesData) => {
        UTILS.setMathValue(item.querySelector('[data-frame-editor]'), data.frame || '');
        setTimeout(() => {
            syncPlaceholderState(item, inputTypesData);
            setTimeout(() => {
                let hasCustom = false;
                data.answers?.forEach(ans => {
                    const row = item.querySelector(`[data-blank-num="${ans.blankIndex}"]`);
                    if (row) {
                        if (ans.answerId) row.setAttribute('data-answer-id', ans.answerId);
                        UTILS.setMathValue(row.querySelector('[data-blank-answer]'), ans.correctAnswer || '');
                        if (ans.inputTypeId) row.querySelector(`.constraint-chip[data-input-type-id="${ans.inputTypeId}"]`)?.classList.add('active');
                        if (ans.point > 0) { row.querySelector('[data-blank-score]').value = ans.point; hasCustom = true; }
                    }
                });
                if (hasCustom) { const st = item.querySelector('[data-scoring-toggle]'); st.checked = true; st.dispatchEvent(new Event('change')); }
                data.blankGroups?.forEach(g => {
                    const list = item.querySelector('[data-blank-group-list]'), gEl = createBlankGroupItem(item, list, g);
                    if (gEl) list.appendChild(gEl);
                });
                syncBlankGroupSegments(item);
                setTimeout(() => {
                    const gEls = item.querySelectorAll('[data-blank-group-item]');
                    data.blankGroups?.forEach((g, i) => {
                        const gEl = gEls[i]; if (!gEl) return;
                        if (g.segmentIndices?.length > 0) {
                            g.segmentIndices.forEach(si => gEl.querySelector(`[data-segment-index="${si}"]`)?.classList.add('selected'));
                        } else {
                            const segs = UTILS.parseLatexSegments(data.frame);
                            g.blankIndices?.forEach(ph => {
                                const si = segs.findIndex(s => UTILS.getNumberedPlaceholders(s.content).includes(ph));
                                if (si !== -1) gEl.querySelector(`[data-segment-index="${si}"]`)?.classList.add('selected');
                            });
                        }
                    });
                }, 50);
            }, 100);
        }, 300);
    };

    return {
        syncPlaceholderState, updateScoreSummary, syncBlankGroupSegments,
        switchActivePlaceholder, createBlankGroupItem, bindConstraintChips,
        selectPlaceholderByPosition: (item, mathField) => {
            const pos = mathField.position;
            const latex = UTILS.getFrameLatex(item);
            const ids = UTILS.getNumberedPlaceholders(latex);
            for (const id of ids) {
                const range = mathField.getPromptRange(String(id));
                if (range && pos >= range[0] && pos <= range[1]) {
                    switchActivePlaceholder(item, String(id));
                    break;
                }
            }
        },
        init, getPayload, setData
    };
})();
