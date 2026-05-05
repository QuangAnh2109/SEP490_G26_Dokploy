(() => {
    'use strict';
    const QE = window.QuestionEditor;
    const container = document.getElementById('editQuestionContainer');
    const saveBtn = document.getElementById('saveQuestionBtn');
    const qIdElem = document.getElementById('currentQuestionId');
    const qId = qIdElem?.value;

    if (!container || !qId) {
        return;
    }

    const item = container.querySelector('[data-question-item]');
    if (!item) {
        return;
    }

    let inputTypesData = [];
    let subjectsData = [];

    const submit = async () => {
        if (saveBtn) {
            saveBtn.disabled = true;
        }

        try {
            const payload = QE.collectPayload(item);
            payload.status = "Active";
            await apiClient.put(`/api/questions/${qId}`, payload);
            showToast('Đã cập nhật câu hỏi thành công!');

            setTimeout(() => {
                window.location.href = '/Question';
            }, 1000);
        } catch (err) {
            showToast('Lỗi: ' + (err.message || 'Không thể cập nhật.'), 'error');
            if (saveBtn) {
                saveBtn.disabled = false;
            }
        }
    };

    saveBtn?.addEventListener('click', () => {
        submit();
    });

    (async () => {
        try {
            const metadata = await apiClient.get('/api/questions/metadata');
            inputTypesData = metadata.inputTypes || [];
            subjectsData = metadata.subjects || [];

            // Populate subjects dropdown
            const subSel = item.querySelector('[data-subject-select]');
            if (subSel) {
                subjectsData.forEach(s => {
                    const label = s.code || s.name;
                    const value = s.subjectId;
                    subSel.add(new Option(label, value));
                });
            }

            QE.initItem(item, {
                inputTypesData,
                subjectsData
            });

            const detail = await apiClient.get(`/api/questions/${qId}`);
            if (detail) {
                QE.setData(item, detail, {
                    inputTypesData,
                    subjectsData
                });
            }
        } catch (e) {
            console.error(e);
        }
    })();
})();
