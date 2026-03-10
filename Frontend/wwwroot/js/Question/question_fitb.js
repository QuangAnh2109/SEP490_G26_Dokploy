window.QuestionEditorFITB = (() => {
    'use strict';
    const UTILS = window.QuestionEditorUtils;

    const renderConstraintChips = (row, inputTypesData) => {
        const loading = row.querySelector('[data-blank-constraint-loading]');
        const container = row.querySelector('[data-constraint-container]');

        if (loading) {
            loading.classList.toggle('d-none', !!inputTypesData?.length);
        }

        if (!container || !inputTypesData?.length) {
            return;
        }

        while (container.firstChild) {
            container.removeChild(container.firstChild);
        }

        const groups = {};
        inputTypesData.forEach(it => {
            const g = it.groupType || it.GroupType || '__none__';
            if (!groups[g]) {
                groups[g] = [];
            }
            groups[g].push(it);
        });

        const sectionT = row.querySelector('[data-constraint-section-template]');
        const chipT = row.querySelector('[data-constraint-chip-template]');

        for (const [groupType, items] of Object.entries(groups)) {
            const section = sectionT?.content.cloneNode(true).firstElementChild;
            if (!section) {
                continue;
            }

            const labelElem = section.querySelector('[data-constraint-group-label]');
            if (labelElem) {
                labelElem.textContent = groupType === '__none__' ? 'Khác' : groupType;
            }

            const grid = section.querySelector('[data-constraint-grid]');
            items.forEach(it => {
                const chip = chipT?.content.cloneNode(true).firstElementChild;
                if (chip) {
                    const id = it.inputTypeId || it.InputTypeId;
                    chip.setAttribute('data-group-type', groupType);
                    chip.setAttribute('data-input-type-id', id);

                    const nameElem = chip.querySelector('[data-constraint-name]');
                    if (nameElem) {
                        nameElem.textContent = it.name || it.Name;
                    }
                    grid.appendChild(chip);
                }
            });
            container.appendChild(section);
        }
    };

    const bindConstraintChips = (row) => {
        const chips = row.querySelectorAll('.constraint-chip');
        UTILS.toArray(chips).forEach(chip => {
            chip.addEventListener('click', () => {
                const gt = chip.getAttribute('data-group-type');
                const active = chip.classList.contains('active');

                const siblings = row.querySelectorAll(`.constraint-chip[data-group-type="${gt}"]`);
                UTILS.toArray(siblings).forEach(s => {
                    s.classList.remove('active');
                });

                if (!active) {
                    chip.classList.add('active');
                }
            });
        });
    };

    const switchActivePlaceholder = (item, sNum) => {
        const list = item.querySelector('[data-blank-answer-list]');
        const nav = item.querySelector('[data-placeholder-nav]');
        if (!list || !nav) {
            return;
        }

        const rows = list.querySelectorAll('[data-blank-answer-item]');
        UTILS.toArray(rows).forEach(r => {
            r.classList.toggle('active', r.getAttribute('data-blank-num') === sNum);
        });

        const buttons = nav.querySelectorAll('button');
        UTILS.toArray(buttons).forEach(btn => {
            btn.classList.toggle('active', btn.textContent === sNum);
        });
    };

    const updateScoreSummary = (item) => {
        const scoringToggle = item.querySelector('[data-scoring-toggle]');
        const scoring = !!scoringToggle?.checked;
        const summary = item.querySelector('[data-score-total]');
        if (!summary) {
            return;
        }

        const bar = item.querySelector('[data-score-summary]');
        if (scoring && bar) {
            let total = 0;
            const scoreInputs = item.querySelectorAll('[data-blank-answer-item] [data-blank-score]');
            UTILS.toArray(scoreInputs).forEach(i => {
                total += parseInt(i.value) || 0;
            });

            summary.textContent = total;
            bar.classList.remove('d-none');
            bar.classList.toggle('is-invalid', total !== 100);
            bar.classList.toggle('is-valid', total === 100);
        } else if (bar) {
            bar.classList.add('d-none');
        }
    };

    const syncBlankGroupSegments = (item) => {
        const latex = UTILS.getFrameLatex(item);
        const segments = UTILS.parseLatexSegments(latex);

        const groups = item.querySelectorAll('[data-blank-group-item]');
        UTILS.toArray(groups).forEach(g => {
            const container = g.querySelector('[data-segments-container]');
            const t = g.querySelector('[data-blank-group-segment-template]');
            const emptyNote = g.querySelector('[data-blank-group-empty-note]');

            if (emptyNote) {
                emptyNote.classList.toggle('d-none', segments.length > 0);
            }

            if (!container) return;

            // Map existing cards for reuse
            const existingCards = new Map();
            UTILS.toArray(container.querySelectorAll('.blank-group-segment')).forEach(c => {
                existingCards.set(c.getAttribute('data-segment-index'), c);
            });

            const fragment = document.createDocumentFragment();
            segments.forEach(seg => {
                const sIdx = String(seg.index);
                let card = existingCards.get(sIdx);
                
                if (!card && t) {
                    card = t.content.cloneNode(true).firstElementChild;
                    card.setAttribute('data-segment-index', sIdx);
                    card.addEventListener('click', () => {
                        card.classList.toggle('selected');
                    });
                }
                
                if (card) {
                    // Update label
                    const labelElem = card.querySelector('[data-segment-label]');
                    if (labelElem) {
                        labelElem.textContent = `Dòng ${seg.index + 1}`;
                    }

                    // Update content if different
                    const contentElem = card.querySelector('[data-segment-content]');
                    if (contentElem) {
                        const newContent = seg.content;
                        // Use a custom property to track last content to avoid unnecessary DOM updates
                        if (contentElem._lastContent !== newContent) {
                            UTILS.setMathValue(contentElem, newContent);
                            contentElem._lastContent = newContent;
                        }
                    }
                    fragment.appendChild(card);
                    existingCards.delete(sIdx);
                }
            });

            // Remove cards no longer in use
            existingCards.forEach(c => c.remove());

            // Build new list (moves existing elements to correct position)
            container.appendChild(fragment);
        });
    };

    const createBlankGroupItem = (item, list, initialData = null) => {
        const t = item.querySelector('[data-blank-group-template]');
        if (!t) {
            return null;
        }

        const r = t.cloneNode(true);
        r.removeAttribute('data-blank-group-template');
        r.setAttribute('data-blank-group-item', '');
        r.classList.remove('d-none');

        const nameInp = r.querySelector('[data-blank-group-name]');
        if (initialData) {
            if (nameInp) {
                nameInp.value = initialData.name || 'Nhóm';
            }
            if (initialData.groupAnswerId) {
                r.setAttribute('data-group-id', initialData.groupAnswerId);
            }
        } else {
            if (nameInp) {
                const count = list.querySelectorAll('[data-blank-group-item]').length;
                nameInp.value = `Nhóm ${count + 1}`;
            }
        }

        const removeBtn = r.querySelector('[data-remove-blank-group]');
        removeBtn?.addEventListener('click', () => {
            r.remove();
        });

        return r;
    };

    const createBlankAnswerRow = (item, sNum, inputTypesData) => {
        const t = item.querySelector('[data-blank-answer-template]');
        if (!t) return null;

        const row = t.content.cloneNode(true).firstElementChild;
        if (row) {
            row.setAttribute('data-blank-answer-item', '');
            row.setAttribute('data-blank-num', sNum);

            const label = row.querySelector('[data-blank-label]');
            if (label) {
                label.textContent = `Ô trống ${sNum}`;
            }

            const toggle = row.querySelector('[data-blank-constraint-toggle]');
            const body = row.querySelector('[data-blank-constraint-body]');
            toggle?.addEventListener('click', (e) => {
                e.preventDefault();
                body.classList.toggle('open');
                toggle.classList.toggle('active');
            });

            if (inputTypesData?.length > 0) {
                renderConstraintChips(row, inputTypesData);
                bindConstraintChips(row);
            }
        }
        return row;
    };

    const syncPlaceholderState = (item, inputTypesData) => {
        const latex = UTILS.getFrameLatex(item);
        const dataCount = (inputTypesData || []).length;

        // Still keep basic cache check but let's be more careful:
        // Ignore whitespace-only changes if the numbered placeholders haven't changed.
        const numbered = UTILS.getNumberedPlaceholders(latex);
        const numberedStr = numbered.join(',');

        if (item._lastSyncLatexNorm === latex.replace(/\s+/g, '') &&
            item._lastDataCount === dataCount &&
            item._lastNumberedStr === numberedStr) {
            return;
        }

        item._lastSyncLatex = latex;
        item._lastSyncLatexNorm = latex.replace(/\s+/g, '');
        item._lastDataCount = dataCount;
        item._lastNumberedStr = numberedStr;

        const counter = item.querySelector('[data-placeholder-count]');
        if (counter) {
            counter.textContent = `${numbered.length} ô trống`;
        }

        const manager = item.querySelector('[data-placeholder-manager]');
        const nav = item.querySelector('[data-placeholder-nav]');
        const list = item.querySelector('[data-blank-answer-list]');
        if (!list) {
            return;
        }

        // Map existing elements for reuse
        const existingRows = new Map();
        UTILS.toArray(list.querySelectorAll('[data-blank-answer-item]')).forEach(r => {
            existingRows.set(r.getAttribute('data-blank-num'), r);
        });

        const existingNavs = new Map();
        UTILS.toArray(nav.children).forEach(c => {
            if (c.tagName !== 'TEMPLATE' && c.hasAttribute('data-blank-num')) {
                existingNavs.set(c.getAttribute('data-blank-num'), c);
            }
        });

        // 1. Update/Add Rows
        const fragment = document.createDocumentFragment();
        numbered.forEach(num => {
            const sNum = String(num);
            let row = existingRows.get(sNum);
            if (!row) {
                row = createBlankAnswerRow(item, sNum, inputTypesData);
            }
            fragment.appendChild(row);
            existingRows.delete(sNum);
        });

        // 2. Remove old rows
        existingRows.forEach(row => row.remove());

        // 3. Clear and re-append in correct order (without flicker if elements are same)
        // Note: appendChild on existing element just moves it.
        list.appendChild(fragment);

        // 4. Update/Add Nav Buttons
        const navFragment = document.createDocumentFragment();
        const navTmpl = item.querySelector('[data-blank-nav-button-template]');
        
        numbered.forEach(num => {
            const sNum = String(num);
            let btn = existingNavs.get(sNum);
            if (!btn && navTmpl) {
                btn = navTmpl.content.cloneNode(true).firstElementChild;
                btn.setAttribute('data-blank-num', sNum);
                btn.textContent = num;
                btn.addEventListener('click', () => switchActivePlaceholder(item, sNum));
            }
            if (btn) {
                navFragment.appendChild(btn);
            }
            existingNavs.delete(sNum);
        });

        // 5. Remove old navs
        existingNavs.forEach(btn => btn.remove());
        nav.appendChild(navFragment);

        // 6. Final UI Status
        const scoringToggle = item.querySelector('[data-scoring-toggle]');
        const scoring = !!scoringToggle?.checked;
        UTILS.toArray(list.querySelectorAll('[data-blank-answer-item]')).forEach(r => {
            r.querySelector('[data-blank-score-field]')?.classList.toggle('d-none', !scoring);
        });

        manager?.classList.toggle('d-none', numbered.length === 0);
        item.querySelector('[data-blank-answer-empty]')?.classList.toggle('d-none', numbered.length > 0);

        if (numbered.length > 0 && !list.querySelector('[data-blank-answer-item].active')) {
            const firstNum = String(numbered[0]);
            switchActivePlaceholder(item, firstNum);
        }

        updateScoreSummary(item);
        syncBlankGroupSegments(item);
    };

    const init = (item) => {
        const insertBtn = item.querySelector('[data-insert-placeholder]');
        insertBtn?.addEventListener('click', () => {
            const mf = item.querySelector('[data-frame-editor]');
            const latex = UTILS.getFrameLatex(item);
            const existing = UTILS.getNumberedPlaceholders(latex);

            let next = 1;
            for (const n of existing.sort((a, b) => a - b)) {
                if (n === next) {
                    next++;
                } else if (n > next) {
                    break;
                }
            }

            const ph = `\\placeholder[${next}]{}`;

            if (mf.classList.contains('d-none')) {
                const raw = item.querySelector('[data-frame-raw]');
                let pos = raw.selectionStart || raw.value.length;
                const textBefore = raw.value.slice(0, pos);
                const openMatch = textBefore.match(/\\placeholder\s*\[\d+\]\s*\{[^}]*$/);

                if (openMatch) {
                    const nextBrace = raw.value.indexOf('}', pos);
                    if (nextBrace !== -1) {
                        pos = nextBrace + 1;
                    }
                }

                const prefix = (pos > 0 && raw.value[pos - 1] !== ' ' && raw.value[pos - 1] !== '\n') ? ' ' : '';
                raw.value = raw.value.slice(0, pos) + prefix + ph + raw.value.slice(pos);
                raw.selectionStart = raw.selectionEnd = pos + prefix.length + ph.length;
                raw.focus();
            } else {
                if (typeof mf.insert === 'function') {
                    mf.focus();
                    for (let i = 0; i < 3; i++) {
                        mf.executeCommand('moveAfterParent');
                    }
                    mf.insert(ph, {
                        focus: true,
                        selectionMode: 'after'
                    });
                } else {
                    UTILS.setMathValue(mf, UTILS.getMathValue(mf) + ph);
                }
            }

            setTimeout(() => {
                syncPlaceholderState(item, item._inputTypesData);
            }, 50);
        });

        const scoringToggle = item.querySelector('[data-scoring-toggle]');
        scoringToggle?.addEventListener('change', () => {
            syncPlaceholderState(item, item._inputTypesData);
        });

        item.addEventListener('input', (e) => {
            if (e.target.matches('[data-blank-score]')) {
                updateScoreSummary(item);
            }
        });

        const addGroupBtn = item.querySelector('[data-add-blank-group]');
        addGroupBtn?.addEventListener('click', () => {
            const list = item.querySelector('[data-blank-group-list]');
            const gEl = createBlankGroupItem(item, list);
            if (gEl) {
                list.appendChild(gEl);
                syncBlankGroupSegments(item);
            }
        });
    };

    const getPayload = (item, frame) => {
        const answers = [];
        const scoringToggle = item.querySelector('[data-scoring-toggle]');
        const scoring = !!scoringToggle?.checked;

        const answerRows = item.querySelectorAll('[data-blank-answer-item]');
        UTILS.toArray(answerRows).forEach(row => {
            const bNum = parseInt(row.getAttribute('data-blank-num'));
            const chip = row.querySelector('.constraint-chip.active');
            const scoreInp = row.querySelector('[data-blank-score]');
            const ansInp = row.querySelector('[data-blank-answer]');

            answers.push({
                answerId: parseInt(row.getAttribute('data-answer-id')) || null,
                content: `\\placeholder[${bNum}]{}`,
                correctAnswer: UTILS.getMathValue(ansInp),
                isCorrect: true,
                blankIndex: bNum,
                inputTypeId: chip ? parseInt(chip.getAttribute('data-input-type-id')) : null,
                point: scoring ? (parseInt(scoreInp?.value) || 0) : 0
            });
        });

        if (!scoring && answers.length > 0) {
            const p = Math.floor(100 / answers.length);
            answers.forEach((a, i) => {
                a.point = p + (i === 0 ? (100 - p * answers.length) : 0);
            });
        }

        const groups = [];
        const groupsList = item.querySelectorAll('[data-blank-group-item]');
        const segments = UTILS.parseLatexSegments(frame);

        UTILS.toArray(groupsList).forEach(g => {
            const selectedCards = g.querySelectorAll('.blank-group-segment.selected');
            const selectedSegIdxs = UTILS.toArray(selectedCards).map(c => parseInt(c.getAttribute('data-segment-index')));
            const blanks = [];

            selectedSegIdxs.forEach(si => {
                const seg = segments.find(s => s.index === si);
                if (seg) {
                    UTILS.getNumberedPlaceholders(seg.content).forEach(n => {
                        blanks.push(n);
                    });
                }
            });

            if (blanks.length > 0) {
                const nameElem = g.querySelector('[data-blank-group-name]');
                groups.push({
                    groupAnswerId: parseInt(g.getAttribute('data-group-id')) || null,
                    name: nameElem?.value || 'Nhóm',
                    segmentIndices: selectedSegIdxs,
                    blankIndices: [...new Set(blanks)]
                });
            }
        });

        return {
            answers,
            blankGroups: groups.length > 0 ? groups : null
        };
    };

    const setData = (item, data, inputTypesData) => {
        const frameEditor = item.querySelector('[data-frame-editor]');
        UTILS.setMathValue(frameEditor, data.frame || '');

        setTimeout(() => {
            syncPlaceholderState(item, inputTypesData);

            setTimeout(() => {
                let hasCustom = false;
                data.answers?.forEach(ans => {
                    const row = item.querySelector(`[data-blank-num="${ans.blankIndex}"]`);
                    if (row) {
                        if (ans.answerId) {
                            row.setAttribute('data-answer-id', ans.answerId);
                        }

                        const ansInp = row.querySelector('[data-blank-answer]');
                        UTILS.setMathValue(ansInp, ans.correctAnswer || '');

                        if (ans.inputTypeId) {
                            const chip = row.querySelector(`.constraint-chip[data-input-type-id="${ans.inputTypeId}"]`);
                            chip?.classList.add('active');
                        }

                        if (ans.point > 0) {
                            const scoreInp = row.querySelector('[data-blank-score]');
                            if (scoreInp) {
                                scoreInp.value = ans.point;
                            }
                            hasCustom = true;
                        }
                    }
                });

                if (hasCustom) {
                    const st = item.querySelector('[data-scoring-toggle]');
                    if (st) {
                        st.checked = true;
                        st.dispatchEvent(new Event('change'));
                    }
                }

                data.blankGroups?.forEach(g => {
                    const list = item.querySelector('[data-blank-group-list]');
                    const gEl = createBlankGroupItem(item, list, g);
                    if (gEl) {
                        list.appendChild(gEl);
                    }
                });

                syncBlankGroupSegments(item);

                setTimeout(() => {
                    const gEls = item.querySelectorAll('[data-blank-group-item]');
                    data.blankGroups?.forEach((g, i) => {
                        const gEl = gEls[i];
                        if (!gEl) {
                            return;
                        }

                        if (g.segmentIndices?.length > 0) {
                            g.segmentIndices.forEach(si => {
                                const segCard = gEl.querySelector(`[data-segment-index="${si}"]`);
                                segCard?.classList.add('selected');
                            });
                        } else {
                            const segs = UTILS.parseLatexSegments(data.frame);
                            g.blankIndices?.forEach(ph => {
                                const si = segs.findIndex(s => {
                                    return UTILS.getNumberedPlaceholders(s.content).includes(ph);
                                });
                                if (si !== -1) {
                                    const segCard = gEl.querySelector(`[data-segment-index="${si}"]`);
                                    segCard?.classList.add('selected');
                                }
                            });
                        }
                    });
                }, 50);
            }, 100);
        }, 300);
    };

    return {
        syncPlaceholderState,
        updateScoreSummary,
        syncBlankGroupSegments,
        switchActivePlaceholder,
        createBlankGroupItem,
        bindConstraintChips,
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
        init,
        getPayload,
        setData
    };
})();
