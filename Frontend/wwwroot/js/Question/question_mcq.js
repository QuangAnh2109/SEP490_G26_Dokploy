window.QuestionEditorMCQ = (() => {
    'use strict';
    const UTILS = window.QuestionEditorUtils;

    const syncMcqRows = (list) => {
        const rows = UTILS.toArray(list.querySelectorAll('[data-answer-item]'));
        rows.forEach((row, i) => {
            const idx = row.querySelector('[data-answer-index]');
            if (idx) {
                idx.textContent = String.fromCharCode(65 + i);
            }

            const rb = row.querySelector('[data-remove-answer]');
            if (rb) {
                rb.disabled = rows.length <= 2;
            }
        });
    };

    const createMcqOptionRow = (item, list, initialData = null) => {
        const t = item.querySelector('[data-answer-template]');
        if (!t) {
            return null;
        }

        const r = t.content.cloneNode(true).firstElementChild;
        if (!r) {
            return null;
        }

        r.setAttribute('data-answer-item', '');
        const mf = r.querySelector('[data-option-content]');
        const raw = r.querySelector('[data-option-raw]');
        const ck = r.querySelector('[data-option-correct]');

        if (initialData) {
            UTILS.setMathValue(mf, initialData.content || '');
            if (raw) {
                raw.value = initialData.content || '';
            }
            if (ck) {
                ck.checked = !!initialData.isCorrect;
            }
            if (initialData.answerId) {
                r.setAttribute('data-answer-id', initialData.answerId);
            }
        }

        const removeBtn = r.querySelector('[data-remove-answer]');
        removeBtn?.addEventListener('click', () => {
            r.remove();
            syncMcqRows(list);
        });

        return r;
    };

    const init = (item) => {
        let mcqToggleBusy = false;
        const toggleBtn = item.querySelector('[data-mcq-render-toggle]');

        toggleBtn?.addEventListener('change', (e) => {
            if (mcqToggleBusy) {
                return;
            }
            mcqToggleBusy = true;
            const renderOn = e.target.checked;

            try {
                const rows = item.querySelectorAll('[data-answer-item]');
                UTILS.toArray(rows).forEach(row => {
                    const mf = row.querySelector('[data-option-content]');
                    const raw = row.querySelector('[data-option-raw]');

                    if (renderOn) {
                        UTILS.setMathValue(mf, raw.value);
                        mf.classList.remove('d-none');
                        raw.classList.add('d-none');
                    } else {
                        raw.value = UTILS.getMathValue(mf);
                        mf.classList.add('d-none');
                        raw.classList.remove('d-none');
                    }
                });
            } finally {
                setTimeout(() => {
                    mcqToggleBusy = false;
                }, 250);
            }
        });

        const addBtn = item.querySelector('[data-add-answer]');
        addBtn?.addEventListener('click', () => {
            const list = item.querySelector('[data-answer-list]');
            if (list) {
                const newRow = createMcqOptionRow(item, list);
                if (newRow) {
                    list.appendChild(newRow);
                    syncMcqRows(list);
                }
            }
        });
    };

    const getPayload = (item) => {
        const answers = [];
        const scoreElem = item.querySelector('[data-mcq-score]');
        const mcqScore = parseInt(scoreElem?.value) || 0;
        const mcqToggle = item.querySelector('[data-mcq-render-toggle]');

        const rows = item.querySelectorAll('[data-answer-list] [data-answer-item]');
        
        if (rows.length < 2) {
            throw new Error("Câu hỏi trắc nghiệm phải có ít nhất 2 đáp án.");
        }

        UTILS.toArray(rows).forEach((row, i) => {
            const selector = mcqToggle?.checked ? '[data-option-content]' : '[data-option-raw]';
            const val = UTILS.getMathValue(row.querySelector(selector));
            const ck = row.querySelector('[data-option-correct]')?.checked;

            if (!val || !String(val).trim()) {
                throw new Error(`Đáp án ${String.fromCharCode(65 + i)} chưa có nội dung.`);
            }

            answers.push({
                answerId: parseInt(row.getAttribute('data-answer-id')) || null,
                content: val,
                correctAnswer: val,
                isCorrect: !!ck,
                point: ck ? mcqScore : 0
            });
        });

        const corrects = answers.filter(a => a.isCorrect);
        if (corrects.length === 0) {
            throw new Error("Vui lòng chọn ít nhất 1 đáp án đúng.");
        }

        if (corrects.length > 0) {
            const p = Math.floor(100 / corrects.length);
            corrects.forEach((a, i) => {
                a.point = p + (i === 0 ? (100 - p * corrects.length) : 0);
            });
        }
        return {
            answers
        };
    };

    const setData = (item, data) => {
        const list = item.querySelector('[data-answer-list]');
        if (!list) return;

        const currentRows = list.querySelectorAll('[data-answer-item]');
        UTILS.toArray(currentRows).forEach(r => {
            r.remove();
        });

        data.answers?.forEach(ans => {
            const row = createMcqOptionRow(item, list, ans);
            if (row) {
                list.appendChild(row);
            }
            if (ans.point > 0) {
                const scoreInp = item.querySelector('[data-mcq-score]');
                if (scoreInp) {
                    scoreInp.value = ans.point;
                }
            }
        });
        syncMcqRows(list);
    };

    return {
        syncMcqRows,
        createMcqOptionRow,
        init,
        getPayload,
        setData
    };
})();
