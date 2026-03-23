(() => {
    'use strict';
    const QE = window.QuestionEditor;
    const MCQ = window.QuestionEditorMCQ;
    const toArray = (v) => Array.from(v || []);

    const questionList = document.getElementById('questionBatchList');
    const addBtn = document.getElementById('addQuestionItemBtn');
    const countText = document.getElementById('questionCountText');
    const saveAllBtn = document.getElementById('saveAllBtn');
    const saveDraftBtn = document.getElementById('saveDraftBtn');

    if (!questionList || !addBtn) {
        return;
    }

    let inputTypesData = [];
    let subjectsData = [];

    const syncBatch = () => {
        const items = toArray(questionList.querySelectorAll('[data-question-item]'));
        items.forEach((item, idx) => {
            const titleElem = item.querySelector('[data-question-title]');
            if (titleElem) {
                titleElem.textContent = `Câu hỏi #${idx + 1}`;
            }

            const removeBtn = item.querySelector('[data-remove-question]');
            if (removeBtn) {
                removeBtn.disabled = items.length === 1;
            }

            QE.initItem(item, {
                inputTypesData,
                subjectsData
            });
        });

        if (countText) {
            countText.textContent = `Đang soạn: ${items.length} câu hỏi`;
        }
    };

    addBtn.addEventListener('click', () => {
        const base = questionList.querySelector('[data-question-item]');
        const clone = base.cloneNode(true);
        clone.removeAttribute('data-bound');

        // Reset everything in clone
        const inputs = clone.querySelectorAll('textarea, input[type="text"]');
        toArray(inputs).forEach(i => {
            i.value = '';
        });

        const emptyLists = clone.querySelectorAll('[data-blank-answer-list], [data-blank-group-list]');
        toArray(emptyLists).forEach(l => {
            while (l.firstChild) {
                l.removeChild(l.firstChild);
            }
        });


        // Reset MCQ to default state (A, B)
        const ansList = clone.querySelector('[data-answer-list]');
        if (ansList) {
            while (ansList.firstChild) {
                ansList.removeChild(ansList.firstChild);
            }
            ansList.appendChild(MCQ.createMcqOptionRow(clone, ansList));
            ansList.appendChild(MCQ.createMcqOptionRow(clone, ansList));
            MCQ.syncMcqRows(ansList);
        }

        const toggles = clone.querySelectorAll('[data-mcq-render-toggle]');
        toArray(toggles).forEach(sw => {
            sw.checked = true;
        });


        questionList.appendChild(clone);
        syncBatch();
    });

    questionList.addEventListener('click', (e) => {
        const removeBtn = e.target.closest('[data-remove-question]');
        if (removeBtn) {
            const items = questionList.querySelectorAll('[data-question-item]');
            if (items.length > 1) {
                const item = e.target.closest('[data-question-item]');
                if (item) {
                    item.remove();
                    syncBatch();
                }
            }
        }
    });

    const submit = async (status) => {
        try {
            const items = questionList.querySelectorAll('[data-question-item]');
            const payload = toArray(items).map(item => {
                const p = QE.collectPayload(item);
                if (!p) {
                    throw new Error("Không thể thu thập dữ liệu câu hỏi.");
                }
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
            const msg = err.details ? (err.message + '\n' + err.details) : (err.message || 'Không thể lưu.');
            showToast('Lỗi: ' + msg, 'error');
        }
    };

    saveAllBtn?.addEventListener('click', () => submit('Active'));
    saveDraftBtn?.addEventListener('click', () => submit('Draft'));

    // Initial sync to bind events immediately
    syncBatch();

    (async () => {
        try {
            const res = await apiClient.get('/api/questions/metadata');

            // Defensive extraction: handle root properties and .data wrapper
            const root = (res && res.data) ? res.data : res;
            inputTypesData = root.inputTypes || root.InputTypes || [];
            subjectsData = root.subjects || root.Subjects || [];

            // Re-sync to pass metadata to already initialized items
            syncBatch();

            if (subjectsData.length === 0 && inputTypesData.length === 0) {
                showToast('Dữ liệu hệ thống vẫn đang trống (Môn học/Giới hạn).', 'error');
            }
        } catch (e) {
            console.error('Failed to load metadata:', e);
            const status = e.xhr ? e.xhr.status : (e.status || 'unknown');
            let msg = `Lỗi ${status}: Không thể tải dữ liệu hệ thống.`;

            if (status === 401) {
                msg += ' Vui lòng đăng nhập lại.';
            } else if (status === 403) {
                msg += ' Bạn không có quyền truy cập (Yêu cầu GV).';
            } else {
                msg += ' Vui lòng kiểm tra kết nối mạng hoặc máy chủ.';
            }
            showToast(msg, 'error');
        }
    })();
})();
