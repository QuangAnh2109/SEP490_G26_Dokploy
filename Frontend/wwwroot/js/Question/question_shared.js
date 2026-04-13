window.QuestionEditor = (() => {
    'use strict';
    const UTILS = window.QuestionEditorUtils;
    const MCQ = window.QuestionEditorMCQ;
    const FITB = window.QuestionEditorFITB;

    const syncSubjectDropdown = (item, subjectsData) => {
        const subSel = item.querySelector('[data-subject-select]');
        const chapSel = item.querySelector('[data-chapter-select]');
        if (!subSel || !chapSel || !subjectsData?.length) {
            return;
        }

        const currentSubId = subSel.value;
        while (subSel.options.length > 1) {
            subSel.remove(1);
        }

        subjectsData.forEach(s => {
            const label = s.code || s.Code || s.name || s.Name;
            const value = s.subjectId || s.SubjectId;
            subSel.add(new Option(label, value));
        });

        if (currentSubId) {
            subSel.value = currentSubId;
        }

        if (!subSel._bound) {
            subSel._bound = true;
            subSel.addEventListener('change', () => {
                const subId = parseInt(subSel.value);
                while (chapSel.options.length > 1) {
                    chapSel.remove(1);
                }

                const currentData = item._subjectsData || [];
                const sub = currentData.find(s => (s.subjectId || s.SubjectId) === subId);
                const chapters = sub?.chapters || sub?.Chapters;
                if (chapters) {
                    chapters.forEach(c => {
                        const label = c.name || c.Name;
                        const value = c.chapterId || c.ChapterId;
                        chapSel.add(new Option(label, value));
                    });
                }
            });
        }
    };

    return {
        getMathValue: UTILS.getMathValue,
        setMathValue: UTILS.setMathValue,
        initItem: (item, { inputTypesData, subjectsData } = {}) => {
            if (item.hasAttribute('data-bound')) {
                if (inputTypesData || subjectsData) {
                    item._inputTypesData = inputTypesData || item._inputTypesData;
                    item._subjectsData = subjectsData || item._subjectsData;
                    syncSubjectDropdown(item, item._subjectsData);
                }
                return;
            }

            item.setAttribute('data-bound', '1');
            item._inputTypesData = inputTypesData || [];
            item._subjectsData = subjectsData || [];

            const typeSel = item.querySelector('[data-question-type-select]');
            const syncUI = () => {
                const type = typeSel.value;
                const panels = item.querySelectorAll('[data-question-type-panel]');

                UTILS.toArray(panels).forEach(p => {
                    const panelType = p.getAttribute('data-question-type-panel');
                    p.classList.toggle('d-none', panelType !== type);
                });

                const blankSection = item.querySelector('[data-blank-group-section]');
                if (blankSection) {
                    blankSection.style.display = (type === 'FillInBlank' ? '' : 'none');
                }

                if (type === 'FillInBlank') {
                    FITB.syncPlaceholderState(item, item._inputTypesData);
                } else {
                    MCQ.syncMcqRows(item.querySelector('[data-answer-list]'));
                }
            };

            typeSel.addEventListener('change', syncUI);
            FITB.init(item);
            MCQ.init(item);
            syncSubjectDropdown(item, item._subjectsData);

            const stemContainer = item.querySelector('[data-tabbed-editor="stem"]');
            item._stemEditor = UTILS.setupTabbedEditor(stemContainer, {
                onChange: () => {
                    // Update any other logic if needed
                }
            });

            const frameContainer = item.querySelector('[data-tabbed-editor="frame"]');
            item._frameEditor = UTILS.setupTabbedEditor(frameContainer, {
                onChange: () => FITB.syncPlaceholderState(item, item._inputTypesData),
                onInsertPlaceholder: () => FITB.insertPlaceholder(item)
            });

            syncUI();

        },
        collectPayload: (item) => {
            const typeSel = item.querySelector('[data-question-type-select]');
            const type = typeSel?.value;

            const stem = item._stemEditor?.getValue() || '';


            const chapterSel = item.querySelector('[data-chapter-select]');
            const diffSel = item.querySelector('[data-difficulty-select]');
            const purposeSel = item.querySelector('[data-purpose-select]');
            const expText = item.querySelector('[data-explanation]');

            const payload = {
                questionType: type,
                stem: stem,
                explanation: expText?.value || '',
                chapterId: parseInt(chapterSel?.value) || null,
                difficulty: parseInt(diffSel?.value) || 1,
                questionPurpose: parseInt(purposeSel?.value) || 1
            };

            if (type === 'FillInBlank') {
                payload.frame = UTILS.getFrameLatex(item);
                Object.assign(payload, FITB.getPayload(item, payload.frame));
            } else {
                Object.assign(payload, MCQ.getPayload(item));
            }
            return payload;
        },
        setData: (item, data, { inputTypesData, subjectsData }) => {
            item.querySelector('[data-question-type-select]').value = data.questionType;
            item.querySelector('[data-difficulty-select]').value = data.difficulty;
            const purposeSel = item.querySelector('[data-purpose-select]');
            if (purposeSel) purposeSel.value = data.questionPurpose || 1;
            item.querySelector('[data-explanation]').value = data.explanation || '';

            if (item._stemEditor) {
                item._stemEditor.setValue(data.stem || '');
            }


            syncSubjectDropdown(item, subjectsData);

            const sub = subjectsData.find(s => {
                const chapters = s.chapters || s.Chapters || [];
                return chapters.some(c => (c.chapterId || c.ChapterId) === data.chapterId);
            });

            if (sub) {
                const sSel = item.querySelector('[data-subject-select]');
                const cSel = item.querySelector('[data-chapter-select]');
                sSel.value = sub.subjectId || sub.SubjectId;
                sSel.dispatchEvent(new Event('change'));
                setTimeout(() => {
                    cSel.value = data.chapterId;
                }, 50);
            }

            item.querySelector('[data-question-type-select]').dispatchEvent(new Event('change'));
            if (data.questionType === 'FillInBlank') {
                FITB.setData(item, data, inputTypesData);
            } else {
                MCQ.setData(item, data);
            }
        }
    };
})();
