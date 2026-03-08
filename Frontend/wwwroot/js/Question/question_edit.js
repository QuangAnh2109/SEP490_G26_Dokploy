(() => {
    'use strict';
    const QE = window.QuestionEditor;
    const container = document.getElementById('editQuestionContainer');
    const saveBtn = document.getElementById('saveQuestionBtn');
    const qId = document.getElementById('currentQuestionId')?.value;

    if (!container || !qId) return;
    const item = container.querySelector('[data-question-item]');
    if (!item) return;

    let inputTypesData = [], subjectsData = [];

    const submit = async () => {
        saveBtn.disabled = true;
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
            saveBtn.disabled = false;
        }
    };

    saveBtn.addEventListener('click', () => submit());

    (async () => {
        try {
            const metadata = await apiClient.get('/api/questions/metadata');
            inputTypesData = metadata.inputTypes || [];
            subjectsData = metadata.subjects || [];
            // Populate subjects dropdown
            const subSel = item.querySelector('[data-subject-select]');
            subjectsData.forEach(s => subSel.add(new Option(s.code || s.name, s.subjectId)));
            
            QE.initItem(item, { inputTypesData, subjectsData });
            
            const detail = await apiClient.get(`/api/questions/${qId}`);
            if (detail) QE.setData(item, detail, { inputTypesData, subjectsData });
        } catch (e) { console.error(e); }
    })();
})();
