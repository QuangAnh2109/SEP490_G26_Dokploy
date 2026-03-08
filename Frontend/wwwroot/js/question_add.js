(() => {
    'use strict';
    const QE = window.QuestionEditor;
    const toArray = (v) => Array.from(v || []);
    
    const questionList = document.getElementById('questionBatchList');
    const addBtn = document.getElementById('addQuestionItemBtn');
    const countText = document.getElementById('questionCountText');
    const saveAllBtn = document.getElementById('saveAllBtn');
    const saveDraftBtn = document.getElementById('saveDraftBtn');

    if (!questionList || !addBtn) return;

    let inputTypesData = [], subjectsData = [];

    const syncBatch = () => {
        const items = toArray(questionList.querySelectorAll('[data-question-item]'));
        items.forEach((item, idx) => {
            item.querySelector('[data-question-title]').textContent = `Câu hỏi #${idx + 1}`;
            item.querySelector('[data-remove-question]').disabled = items.length === 1;
            QE.initItem(item, { inputTypesData, subjectsData });
        });
        if (countText) countText.textContent = `Đang soạn: ${items.length} câu hỏi`;
    };

    addBtn.addEventListener('click', () => {
        const base = questionList.querySelector('[data-question-item]');
        const clone = base.cloneNode(true);
        clone.removeAttribute('data-bound');
        // Reset everything in clone
        toArray(clone.querySelectorAll('textarea, input[type="text"]')).forEach(i => i.value = '');
        toArray(clone.querySelectorAll('math-field')).forEach(mf => QE.setMathValue(mf, ''));
        toArray(clone.querySelectorAll('[data-blank-answer-list], [data-blank-group-list]')).forEach(l => l.innerHTML = '');
        
        // Reset MCQ to default state (A, B) or clear extra rows
        const ansList = clone.querySelector('[data-answer-list]');
        if (ansList) {
            const rows = toArray(ansList.querySelectorAll('[data-answer-item]'));
            rows.forEach((r, idx) => {
                if (idx >= 2) r.remove();
                else {
                    QE.setMathValue(r.querySelector('[data-option-content]'), '');
                    r.querySelector('[data-option-correct]').checked = false;
                }
            });
        }

        // Reset visibility to Math Mode visibility
        toArray(clone.querySelectorAll('[data-stem-render-toggle], [data-render-toggle], [data-mcq-render-toggle]')).forEach(sw => sw.checked = true);
        toArray(clone.querySelectorAll('[data-question-stem], [data-frame-editor], [data-option-content]')).forEach(mf => mf.classList.remove('d-none'));
        toArray(clone.querySelectorAll('[data-stem-raw], [data-frame-raw], [data-option-raw]')).forEach(raw => raw.classList.add('d-none'));

        questionList.appendChild(clone);
        syncBatch();
    });

    questionList.addEventListener('click', (e) => {
        if (e.target.closest('[data-remove-question]')) {
            if (questionList.querySelectorAll('[data-question-item]').length > 1) {
                e.target.closest('[data-question-item]').remove();
                syncBatch();
            }
        }
    });

    const submit = async (status) => {
        try {
            const payload = toArray(questionList.querySelectorAll('[data-question-item]')).map(item => {
                const p = QE.collectPayload(item);
                if (!p) throw new Error("Không thể thu thập dữ liệu câu hỏi.");
                p.status = status;
                return p;
            });
            const res = await apiClient.post('/api/questions', payload);
            showToast(`Đã lưu ${res.length} câu hỏi thành công!`);
            setTimeout(() => {
                window.location.href = '/Question';
            }, 1000);
        } catch (err) {
            console.error('Submit error:', err);
            showToast('Lỗi: ' + (err.message || 'Không thể lưu.'), 'error');
        }
    };

    saveAllBtn?.addEventListener('click', () => submit('Active'));
    saveDraftBtn?.addEventListener('click', () => submit('Draft'));

    (async () => {
        try {
            const metadata = await apiClient.get('/api/questions/metadata');
            inputTypesData = metadata.inputTypes || [];
            subjectsData = metadata.subjects || [];
            // Populate subjects for the first item which is already in DOM
            const subSel = questionList.querySelector('[data-subject-select]');
            subjectsData.forEach(s => subSel.add(new Option(s.code || s.name, s.subjectId)));
            syncBatch();
        } catch (e) { console.error(e); }
    })();
})();
