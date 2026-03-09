window.QuestionEditor = (() => {
    'use strict';
    const UTILS = window.QuestionEditorUtils;
    const MCQ = window.QuestionEditorMCQ;
    const FITB = window.QuestionEditorFITB;

    const syncSubjectDropdown = (item, subjectsData) => {
        const subSel = item.querySelector('[data-subject-select]'), chapSel = item.querySelector('[data-chapter-select]');
        if (!subSel || !chapSel || !subjectsData?.length) return;
        const currentSubId = subSel.value;
        while (subSel.options.length > 1) subSel.remove(1);
        subjectsData.forEach(s => subSel.add(new Option(s.code || s.Code || s.name || s.Name, s.subjectId || s.SubjectId)));
        if (currentSubId) subSel.value = currentSubId;
        if (!subSel._bound) {
            subSel._bound = true;
            subSel.addEventListener('change', () => {
                const subId = parseInt(subSel.value);
                while (chapSel.options.length > 1) chapSel.remove(1);
                const sub = (item._subjectsData || []).find(s => (s.subjectId || s.SubjectId) === subId);
                const chapters = sub?.chapters || sub?.Chapters;
                if (chapters) chapters.forEach(c => chapSel.add(new Option(c.name || c.Name, c.chapterId || c.ChapterId)));
            });
        }
    };

    return {
        getMathValue: UTILS.getMathValue,
        setMathValue: UTILS.setMathValue,
        initItem: (item, { inputTypesData, subjectsData } = {}) => {
            if (item.hasAttribute('data-bound')) {
                if (inputTypesData || subjectsData) {
                    item._inputTypesData = inputTypesData || item._inputTypesData; item._subjectsData = subjectsData || item._subjectsData;
                    if (item.querySelector('[data-question-type-select]')?.value === 'FillInBlank') FITB.syncPlaceholderState(item, item._inputTypesData);
                    syncSubjectDropdown(item, item._subjectsData);
                }
                return;
            }
            item.setAttribute('data-bound', '1'); item._inputTypesData = inputTypesData || []; item._subjectsData = subjectsData || [];

            const typeSel = item.querySelector('[data-question-type-select]');
            const syncUI = () => {
                const type = typeSel.value;
                UTILS.toArray(item.querySelectorAll('[data-question-type-panel]')).forEach(p => p.classList.toggle('d-none', p.getAttribute('data-question-type-panel') !== type));
                item.querySelector('[data-blank-group-section]').style.display = (type === 'FillInBlank' ? '' : 'none');
                if (type === 'FillInBlank') FITB.syncPlaceholderState(item, item._inputTypesData);
                else MCQ.syncMcqRows(item.querySelector('[data-answer-list]'));
            };

            typeSel.addEventListener('change', syncUI);
            FITB.init(item); MCQ.init(item); syncSubjectDropdown(item, item._subjectsData);

            const { setRenderMode: setStemMode } = UTILS.setupPairToggle(item.querySelector('[data-question-stem]'), item.querySelector('[data-stem-raw]'));
            item.querySelector('[data-stem-render-toggle]')?.addEventListener('change', (e) => setStemMode(e.target.checked));
            setStemMode(!!item.querySelector('[data-stem-render-toggle]:checked'));

            const { setRenderMode: setFrameMode } = UTILS.setupPairToggle(item.querySelector('[data-frame-editor]'), item.querySelector('[data-frame-raw]'), () => FITB.syncPlaceholderState(item, item._inputTypesData));
            item.querySelector('[data-render-toggle]')?.addEventListener('change', (e) => setFrameMode(e.target.checked));
            setFrameMode(!!item.querySelector('[data-render-toggle]:checked'));

            item.querySelector('[data-frame-editor]')?.addEventListener('selection-change', (e) => FITB.selectPlaceholderByPosition(item, e.target));
            syncUI();
        },
        collectPayload: (item) => {
            const type = item.querySelector('[data-question-type-select]')?.value;
            const stem = UTILS.getMathValue(item.querySelector(item.querySelector('[data-stem-render-toggle]:checked') ? '[data-question-stem]' : '[data-stem-raw]'));
            const payload = {
                questionType: type, stem, explanation: item.querySelector('[data-explanation]')?.value || '',
                chapterId: parseInt(item.querySelector('[data-chapter-select]')?.value) || null, difficulty: parseInt(item.querySelector('[data-difficulty-select]')?.value) || 1
            };
            if (type === 'FillInBlank') {
                payload.frame = UTILS.getFrameLatex(item);
                Object.assign(payload, FITB.getPayload(item, payload.frame));
            } else Object.assign(payload, MCQ.getPayload(item));
            return payload;
        },
        setData: (item, data, { inputTypesData, subjectsData }) => {
            item.querySelector('[data-question-type-select]').value = data.questionType;
            item.querySelector('[data-difficulty-select]').value = data.difficulty;
            item.querySelector('[data-explanation]').value = data.explanation || '';
            UTILS.setMathValue(item.querySelector('[data-question-stem]'), data.stem || '');
            syncSubjectDropdown(item, subjectsData);
            const sub = subjectsData.find(s => s.chapters?.some(c => c.chapterId === data.chapterId));
            if (sub) {
                const sSel = item.querySelector('[data-subject-select]'), cSel = item.querySelector('[data-chapter-select]');
                sSel.value = sub.subjectId; sSel.dispatchEvent(new Event('change'));
                setTimeout(() => cSel.value = data.chapterId, 50);
            }
            item.querySelector('[data-question-type-select]').dispatchEvent(new Event('change'));
            if (data.questionType === 'FillInBlank') FITB.setData(item, data, inputTypesData);
            else MCQ.setData(item, data);
        }
    };
})();
