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
                    chip.setAttribute('data-regex', it.regex || it.Regex || '');
                    chip.setAttribute('data-constraint-label', it.name || it.Name || '');

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

    const SETS_GROUP = 'Sets';

    // ── Human-readable examples from regex patterns ──
    const REGEX_EXAMPLES = {
        'Số tự nhiên': ['0', '1', '42', '100'],
        'Số nguyên': ['-5', '0', '7', '-100'],
        'Số vô tỉ': ['\\sqrt{2}', '\\pi', 'e', '\\phi'],
        'Số hữu tỉ': ['3/4', '-1.5', '\\frac{1}{3}'],
        'Số thực': ['5', '-3/2', '\\sqrt{3}', '\\pi'],
        'Số phức': ['3+2i', '-1.5+4j', '2i'],
        'Ký hiệu toán học cơ bản': ['+', '-', '\\times', '\\div', '\\sqrt{}', '\\pm'],
        'Giải tích & Vi phân': ['\\partial', '\\nabla', '\\iint', '\\infty'],
        'Biểu thức so sánh': ['<', '>', '\\ge', '\\le', '\\approx', '\\neq'],
        'Hàm lượng giác/Logarit:': ['\\sin(x)', '\\cos(\\pi)', '\\log(10)', '\\ln(e)'],
        'Hàm lim': ['\\lim_{x\\to0}', '\\lim_{n\\to\\infty}'],
        'Chữ cái': ['x', 'y', 'n', 'A'],
        'Ma trận': ['\\begin{pmatrix}1&2\\\\3&4\\end{pmatrix}'],
        'Toán rời rạc (Logic/Tập hợp)': ['\\wedge', '\\vee', '\\cup', '\\cap', '\\in', '\\forall'],
        'Toán rời rạc (Modulo/Trần/Sàn)': ['\\bmod', '\\pmod{n}', '\\lceil x\\rceil', '\\lfloor x\\rfloor'],
        'Xác suất (Tổ hợp/Kỳ vọng/Phương sai)': ['\\binom{n}{k}', 'E(X)', '\\mu', '\\sigma^2'],
        'Biến đổi Laplace/Fourier': ['\\mathcal{L}', '\\mathcal{F}'],
    };

    const showConstraintDetail = (row, chip) => {
        hideConstraintDetail(row);

        const name = chip.getAttribute('data-constraint-label') || '';
        if (!name) return;

        const examples = REGEX_EXAMPLES[name] || [];

        if (examples.length === 0) return;

        // Clone panel from template
        const panelT = row.querySelector('[data-constraint-detail-template]');
        const exTagT = row.querySelector('[data-constraint-detail-example-template]');
        if (!panelT) return;

        const panel = panelT.content.cloneNode(true).firstElementChild;
        if (!panel) return;

        // Fill name
        const nameEl = panel.querySelector('[data-detail-name]');
        if (nameEl) nameEl.textContent = name;

        const exContainer = panel.querySelector('[data-detail-examples]');
        if (examples.length > 0 && exContainer && exTagT) {
            examples.forEach(ex => {
                const tag = exTagT.content.cloneNode(true).firstElementChild;
                if (!tag) return;
                tag.setAttribute('data-latex-example', ex);
                exContainer.appendChild(tag);
            });
        } else if (exContainer) {
            exContainer.remove();
        }

        // Render KaTeX on example tags
        if (window.katex) {
            panel.querySelectorAll('[data-latex-example]').forEach(tag => {
                const latex = tag.getAttribute('data-latex-example') || '';
                try {
                    window.katex.render(latex, tag, {
                        throwOnError: false,
                        displayMode: false,
                        trust: true,
                        strict: false
                    });
                } catch {
                    tag.textContent = latex;
                }
            });
        }

        const container = row.querySelector('[data-constraint-container]');
        if (container) {
            container.appendChild(panel);
        }
    };

    const hideConstraintDetail = (row) => {
        const existing = row.querySelector('[data-constraint-detail-panel]');
        if (existing) existing.remove();
    };

    const bindConstraintChips = (row) => {
        const chips = row.querySelectorAll('.constraint-chip');
        UTILS.toArray(chips).forEach(chip => {
            chip.addEventListener('click', () => {
                const gt = chip.getAttribute('data-group-type');
                const active = chip.classList.contains('active');

                if (gt === SETS_GROUP) {
                    // Radio behavior: only 1 in Sets group
                    const siblings = row.querySelectorAll(`.constraint-chip[data-group-type="${SETS_GROUP}"]`);
                    UTILS.toArray(siblings).forEach(s => s.classList.remove('active'));
                    if (!active) chip.classList.add('active');
                } else {
                    // Toggle behavior: multi-select for non-Sets groups
                    chip.classList.toggle('active');
                }

                // Show detail panel for the clicked chip
                if (chip.classList.contains('active')) {
                    showConstraintDetail(row, chip);
                } else {
                    hideConstraintDetail(row);
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
                        const willSelect = !card.classList.contains('selected');
                        if (willSelect) {
                            // Deselect this segment from all OTHER groups
                            const allGroups = item.querySelectorAll('[data-blank-group-item]');
                            const myGroup = card.closest('[data-blank-group-item]');
                            UTILS.toArray(allGroups).forEach(otherG => {
                                if (otherG !== myGroup) {
                                    const same = otherG.querySelector(`.blank-group-segment[data-segment-index="${sIdx}"]`);
                                    same?.classList.remove('selected');
                                }
                            });
                        }
                        card.classList.toggle('selected');
                        updateGroupScores(item);
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
                            UTILS.renderLatexInElement(contentElem, newContent);
                            contentElem._lastContent = newContent;
                        }
                    }
                }
                if (card) {
                    fragment.appendChild(card);
                    existingCards.delete(sIdx);
                }
            });


            // Remove cards no longer in use
            existingCards.forEach(c => c.remove());

            // Build new list (moves existing elements to correct position)
            container.appendChild(fragment);
        });
        updateGroupScores(item);
    };

    const updateGroupScores = (item) => {
        const latex = UTILS.getFrameLatex(item);
        const segments = UTILS.parseLatexSegments(latex);
        const groups = item.querySelectorAll('[data-blank-group-item]');

        // Determine point per blank
        const scoringToggle = item.querySelector('[data-scoring-toggle]');
        const scoring = !!scoringToggle?.checked;
        const totalBlanks = item.querySelectorAll('[data-blank-answer-item]').length;
        const equalPoint = totalBlanks > 0 ? Math.floor(100 / totalBlanks) : 0;
        const equalRemainder = totalBlanks > 0 ? 100 - equalPoint * totalBlanks : 0;

        // Build a map: blankNum -> point
        const blankPoints = new Map();
        const allRows = item.querySelectorAll('[data-blank-answer-item]');
        UTILS.toArray(allRows).forEach((row, i) => {
            const num = row.getAttribute('data-blank-num');
            if (scoring) {
                const scoreInp = row.querySelector('[data-blank-score]');
                blankPoints.set(num, parseInt(scoreInp?.value) || 0);
            } else {
                blankPoints.set(num, equalPoint + (i === 0 ? equalRemainder : 0));
            }
        });

        UTILS.toArray(groups).forEach(g => {
            const selectedCards = g.querySelectorAll('.blank-group-segment.selected');
            const selectedSegIdxs = UTILS.toArray(selectedCards).map(c => parseInt(c.getAttribute('data-segment-index')));
            let total = 0;

            selectedSegIdxs.forEach(si => {
                const seg = segments.find(s => s.index === si);
                if (seg) {
                    UTILS.getNumberedPlaceholders(seg.content).forEach(n => {
                        total += blankPoints.get(String(n)) || 0;
                    });
                }
            });

            const badge = g.querySelector('[data-group-score-value]');
            if (badge) badge.textContent = total;
        });
    };

    const refreshDependencyDropdowns = (item) => {
        const allGroups = item.querySelectorAll('[data-blank-group-item]');
        const groupList = UTILS.toArray(allGroups);

        groupList.forEach((g, idx) => {
            const select = g.querySelector('[data-blank-group-depends]');
            if (!select) return;

            const currentVal = select.value;
            // Clear options
            select.innerHTML = '<option value="">Không phụ thuộc</option>';

            // Add other groups as options
            groupList.forEach((otherG, otherIdx) => {
                if (otherIdx === idx) return;
                const nameInp = otherG.querySelector('[data-blank-group-name]');
                const gId = otherG.getAttribute('data-group-id');
                const label = nameInp?.value || `Hình thức ${otherIdx + 1}`;
                const optVal = gId || `__idx_${otherIdx}`;
                const opt = document.createElement('option');
                opt.value = optVal;
                opt.textContent = label;
                select.appendChild(opt);
            });

            // Restore selection if possible
            if (currentVal && select.querySelector(`option[value="${currentVal}"]`)) {
                select.value = currentVal;
            }
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
                nameInp.value = initialData.name || 'Hình thức';
            }
            if (initialData.groupAnswerId) {
                r.setAttribute('data-group-id', initialData.groupAnswerId);
            }
        } else {
            if (nameInp) {
                const count = list.querySelectorAll('[data-blank-group-item]').length;
                nameInp.value = `Hình thức ${count + 1}`;
            }
        }

        const removeBtn = r.querySelector('[data-remove-blank-group]');

        removeBtn?.addEventListener('click', () => {
            r.remove();
            refreshDependencyDropdowns(item);
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

    const insertPlaceholder = (item) => {
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
        const raw = item.querySelector('[data-tabbed-editor="frame"] [data-editor-code]');
        if (raw) {
            let pos = raw.selectionStart || raw.value.length;
            const prefix = (pos > 0 && raw.value[pos - 1] !== ' ' && raw.value[pos - 1] !== '\n') ? ' ' : '';
            raw.value = raw.value.slice(0, pos) + prefix + ph + raw.value.slice(pos);
            raw.selectionStart = raw.selectionEnd = pos + prefix.length + ph.length;
            raw.focus();

            syncPlaceholderState(item, item._inputTypesData);
            if (item._frameEditor) {
                item._frameEditor.refreshPreview();
            }
        }
    };

    const init = (item) => {





        const scoringToggle = item.querySelector('[data-scoring-toggle]');
        scoringToggle?.addEventListener('change', () => {
            const scoring = scoringToggle.checked;
            const list = item.querySelector('[data-blank-answer-list]');
            if (list) {
                UTILS.toArray(list.querySelectorAll('[data-blank-answer-item]')).forEach(r => {
                    r.querySelector('[data-blank-score-field]')?.classList.toggle('d-none', !scoring);
                });
            }
            updateScoreSummary(item);
        });

        item.addEventListener('input', (e) => {
            if (e.target.matches('[data-blank-score]')) {
                updateScoreSummary(item);
                updateGroupScores(item);
            }
        });

        const addGroupBtn = item.querySelector('[data-add-blank-group]');
        addGroupBtn?.addEventListener('click', () => {
            const list = item.querySelector('[data-blank-group-list]');
            const gEl = createBlankGroupItem(item, list);
            if (gEl) {
                list.appendChild(gEl);
                syncBlankGroupSegments(item);
                refreshDependencyDropdowns(item);
            }
        });
    };

    const getPayload = (item, frame) => {
        const answers = [];
        const scoringToggle = item.querySelector('[data-scoring-toggle]');
        const scoring = !!scoringToggle?.checked;

        const answerRows = item.querySelectorAll('[data-blank-answer-item]');
        if (answerRows.length === 0) {
            throw new Error("Câu hỏi điền khuyết phải có ít nhất 1 ô trống.");
        }

        UTILS.toArray(answerRows).forEach(row => {
            const bNum = parseInt(row.getAttribute('data-blank-num'));
            const activeChips = row.querySelectorAll('.constraint-chip.active');
            const scoreInp = row.querySelector('[data-blank-score]');
            const ansInp = row.querySelector('[data-blank-answer]');

            if (!activeChips || activeChips.length === 0) {
                throw new Error(`Ô trống số ${bNum} chưa chọn Giới hạn nhập liệu.`);
            }

            const correctAns = UTILS.getMathValue(ansInp);
            if (!correctAns || !String(correctAns).trim()) {
                throw new Error(`Ô trống số ${bNum} chưa có giá trị nhập liệu (đáp án).`);
            }

            const inputTypeIds = UTILS.toArray(activeChips).map(c => parseInt(c.getAttribute('data-input-type-id')));

            answers.push({
                answerId: parseInt(row.getAttribute('data-answer-id')) || null,
                content: `\\placeholder[${bNum}]{}`,
                correctAnswer: correctAns,
                isCorrect: true,
                blankIndex: bNum,
                inputTypeIds: inputTypeIds,
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
                const dependsSelect = g.querySelector('[data-blank-group-depends]');
                const dependsVal = dependsSelect?.value || '';

                let dependsOnGroupId = null;
                let dependsOnGroupIndex = null;

                if (dependsVal) {
                    if (dependsVal.startsWith('__idx_')) {
                        // New group reference by index
                        dependsOnGroupIndex = parseInt(dependsVal.replace('__idx_', ''), 10);
                        if (isNaN(dependsOnGroupIndex)) dependsOnGroupIndex = null;
                    } else {
                        // Existing group reference by DB ID
                        dependsOnGroupId = parseInt(dependsVal, 10);
                        if (isNaN(dependsOnGroupId)) dependsOnGroupId = null;
                    }
                }

                groups.push({
                    groupAnswerId: parseInt(g.getAttribute('data-group-id')) || null,
                    name: nameElem?.value || 'Nhóm',
                    dependsOnGroupId: dependsOnGroupId,
                    dependsOnGroupIndex: dependsOnGroupIndex,
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
        if (item._frameEditor) {
            item._frameEditor.setValue(data.frame || '');
        }

        // Wait for rows to be created, then set values
        const waitForRowsAndSet = () => {
            syncPlaceholderState(item, inputTypesData);

            const rows = item.querySelectorAll('[data-blank-answer-item]');
            if (rows.length === 0 && data.answers?.length > 0) {
                setTimeout(waitForRowsAndSet, 200);
                return;
            }

            // Apply answer values to rows
            let hasCustom = false;
            const answerPoints = [];
            data.answers?.forEach(ans => {
                const row = item.querySelector(`[data-blank-answer-item][data-blank-num="${ans.blankIndex}"]`);
                if (row) {
                    if (ans.answerId) {
                        row.setAttribute('data-answer-id', ans.answerId);
                    }

                    const ansInp = row.querySelector('[data-blank-answer]');
                    if (ansInp) {
                        ansInp.value = ans.correctAnswer || '';
                    }

                    // Restore active chips (supports both legacy inputTypeId and new inputTypeIds)
                    const ids = ans.inputTypeIds || (ans.inputTypeId ? [ans.inputTypeId] : []);
                    ids.forEach(id => {
                        const chip = row.querySelector(`.constraint-chip[data-input-type-id="${id}"]`);
                        chip?.classList.add('active');
                    });

                    const scoreInp = row.querySelector('[data-blank-score]');
                    if (scoreInp && ans.point != null) {
                        scoreInp.value = ans.point;
                    }
                    answerPoints.push(ans.point || 0);
                }
            });

            // Detect custom scoring
            if (answerPoints.length > 0) {
                const equalPoint = Math.floor(100 / answerPoints.length);
                hasCustom = answerPoints.some(p => p !== equalPoint && p !== equalPoint + 1);
            }

            if (hasCustom) {
                const st = item.querySelector('[data-scoring-toggle]');
                if (st) {
                    st.checked = true;
                    st.dispatchEvent(new Event('change'));
                }
            }

            // Explicitly update score summary
            updateScoreSummary(item);

            // Set up blank groups
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
                    if (!gEl) return;

                    if (g.segmentIndices?.length > 0) {
                        g.segmentIndices.forEach(si => {
                            const segCard = gEl.querySelector(`[data-segment-index="${si}"]`);
                            segCard?.classList.add('selected');
                        });
                    } else {
                        const segs = UTILS.parseLatexSegments(data.frame);
                        g.blankIndices?.forEach(ph => {
                            const si = segs.findIndex(s => UTILS.getNumberedPlaceholders(s.content).includes(ph));
                            if (si !== -1) {
                                const segCard = gEl.querySelector(`[data-segment-index="${si}"]`);
                                segCard?.classList.add('selected');
                            }
                        });
                    }
                });

                refreshDependencyDropdowns(item);
                data.blankGroups?.forEach((g, i) => {
                    const gEl = gEls[i];
                    if (gEl && g.dependsOnGroupId) {
                        const select = gEl.querySelector('[data-blank-group-depends]');
                        if (select) select.value = String(g.dependsOnGroupId);
                    }
                });

                updateGroupScores(item);
            }, 50);
        };

        setTimeout(waitForRowsAndSet, 300);
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
        insertPlaceholder,
        getPayload,
        setData
    };
})();

