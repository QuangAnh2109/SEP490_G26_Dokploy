(() => {
    'use strict';
    const QE = window.QuestionEditor;
    const MCQ = window.QuestionEditorMCQ;
    const toArray = (v) => Array.from(v || []);

    // ═══════════════════════════════════
    //  DOM REFS
    // ═══════════════════════════════════

    const stepper = document.getElementById('stepper');
    const step1Content = document.getElementById('step1Content');
    const step2Content = document.getElementById('step2Content');
    const step1Summary = document.getElementById('step1Summary');
    const questionList = document.getElementById('questionBatchList');
    const addBtn = document.getElementById('addQuestionItemBtn');
    const countText = document.getElementById('questionCountText');

    // Shared fields
    const sharedSubject = document.getElementById('sharedSubjectSelect');
    const sharedPurpose = document.getElementById('sharedPurposeSelect');

    // Navigation buttons
    const btnNext1 = document.getElementById('btnNext1');
    const btnBack2 = document.getElementById('btnBack2');

    // Save buttons (top + step2)
    const saveAllBtn = document.getElementById('saveAllBtn');
    const saveDraftBtn = document.getElementById('saveDraftBtn');
    const saveAllBtn2 = document.getElementById('saveAllBtn2');
    const saveDraftBtn2 = document.getElementById('saveDraftBtn2');

    if (!questionList) return;

    let inputTypesData = [];
    let subjectsData = [];
    let currentStep = 1;

    // ═══════════════════════════════════
    //  STEPPER NAVIGATION
    // ═══════════════════════════════════

    const goToStep = (step) => {
        currentStep = step;
        const steps = stepper.querySelectorAll('.step');
        const line = stepper.querySelector('.step-line');

        steps.forEach(s => {
            const sNum = parseInt(s.dataset.step);
            s.classList.remove('active', 'completed');
            if (sNum === step) s.classList.add('active');
            else if (sNum < step) s.classList.add('completed');
        });

        if (line) {
            if (step >= 2) line.classList.add('progress-full');
            else line.classList.remove('progress-full');
        }

        if (step === 1) {
            step1Content.classList.remove('d-none');
            step2Content.classList.add('d-none');
            // Hide top save buttons on step 1
            saveAllBtn.classList.add('d-none');
            saveDraftBtn.classList.add('d-none');
        } else {
            step1Content.classList.add('d-none');
            step2Content.classList.remove('d-none');
            // Show top save buttons on step 2
            saveAllBtn.classList.remove('d-none');
            saveDraftBtn.classList.remove('d-none');
            updateStep2Badges();
        }

        window.scrollTo({ top: 0, behavior: 'smooth' });
    };

    const validateStep1 = () => {
        if (!sharedSubject?.value) {
            sharedSubject?.parentElement?.classList.add('is-invalid');
            showToast('Vui lòng chọn môn học trước khi tiếp tục.', 'error');
            return false;
        }
        sharedSubject?.parentElement?.classList.remove('is-invalid');
        return true;
    };

    const updateStep1Summary = () => {
        const subLabel = sharedSubject?.selectedOptions?.[0]?.text || '-';
        const purposeLabel = sharedPurpose?.selectedOptions?.[0]?.text || 'Kiểm tra';

        document.getElementById('summarySubject').textContent = subLabel;
        document.getElementById('summaryPurpose').textContent = purposeLabel;
        step1Summary?.classList.remove('d-none');
    };

    const updateStep2Badges = () => {
        const subLabel = sharedSubject?.selectedOptions?.[0]?.text || '-';
        const purposeLabel = sharedPurpose?.selectedOptions?.[0]?.text || 'Kiểm tra';
        const el1 = document.getElementById('step2SubjectBadge');
        const el2 = document.getElementById('step2PurposeBadge');
        if (el1) el1.textContent = subLabel;
        if (el2) el2.textContent = purposeLabel;
    };

    // Step 1 → 2
    btnNext1?.addEventListener('click', () => {
        if (validateStep1()) {
            updateStep1Summary();
            goToStep(2);
            syncBatch(); // initialize items when entering step 2
        }
    });

    // Step 2 → 1
    btnBack2?.addEventListener('click', () => {
        goToStep(1);
    });

    // Clicking stepper indicators
    stepper?.addEventListener('click', (e) => {
        const stepEl = e.target.closest('.step');
        if (!stepEl) return;
        const num = parseInt(stepEl.dataset.step);
        if (num === 1) goToStep(1);
        if (num === 2 && validateStep1()) {
            updateStep1Summary();
            goToStep(2);
            syncBatch();
        }
    });

    // Show summary when fields change
    sharedSubject?.addEventListener('change', () => {
        if (sharedSubject.value) {
            updateStep1Summary();
            // Also update chapters for all items
            const items = questionList.querySelectorAll('[data-question-item]');
            toArray(items).forEach(item => syncChapterForItem(item));
        }
    });
    sharedPurpose?.addEventListener('change', updateStep1Summary);

    // ═══════════════════════════════════
    //  ACCORDION HELPERS
    // ═══════════════════════════════════

    const collapseItem = (item) => {
        const body = item.querySelector('[data-question-body]');
        const icon = item.querySelector('[data-toggle-icon]');
        if (body) body.classList.add('collapsed');
        if (icon) icon.classList.add('collapsed');
        item.classList.add('is-collapsed');
        updateBadges(item);
    };

    const expandItem = (item) => {
        const body = item.querySelector('[data-question-body]');
        const icon = item.querySelector('[data-toggle-icon]');
        if (body) body.classList.remove('collapsed');
        if (icon) icon.classList.remove('collapsed');
        item.classList.remove('is-collapsed');
    };

    const toggleItem = (item) => {
        const body = item.querySelector('[data-question-body]');
        body?.classList.contains('collapsed') ? expandItem(item) : collapseItem(item);
    };

    const collapseAllExcept = (activeItem) => {
        toArray(questionList.querySelectorAll('[data-question-item]')).forEach(item => {
            item !== activeItem ? collapseItem(item) : expandItem(item);
        });
    };

    const updateBadges = (item) => {
        const badges = item.querySelector('[data-question-badges]');
        if (!badges) return;
        const typeSel = item.querySelector('[data-question-type-select]');
        const diffSel = item.querySelector('[data-difficulty-select]');
        const chapSel = item.querySelector('[data-chapter-select]');

        const typeLabel = typeSel?.value === 'MultipleChoice' ? 'Trắc nghiệm' : 'Điền ô trống';
        const diffLabels = { '1': 'Nhận biết', '2': 'Thông hiểu', '3': 'Vận dụng', '4': 'VD cao' };
        const diffLabel = diffLabels[diffSel?.value] || '';
        const chapText = chapSel?.selectedOptions?.[0]?.text;
        const chapLabel = (chapSel?.value && chapText !== 'Chọn chương') ? chapText : '';

        badges.innerHTML = '';
        if (typeLabel) badges.innerHTML += `<span class="badge bg-primary bg-opacity-75">${typeLabel}</span>`;
        if (diffLabel) badges.innerHTML += `<span class="badge bg-secondary">${diffLabel}</span>`;
        if (chapLabel) badges.innerHTML += `<span class="badge bg-info text-dark">${chapLabel}</span>`;
    };

    // ═══════════════════════════════════
    //  CLASSIFICATION HELPERS
    // ═══════════════════════════════════

    const hidePerItemClassification = (item) => {
        const shared = item.querySelector('[data-classification-shared]');
        if (shared) shared.style.display = 'none';
    };

    const syncChapterForItem = (item) => {
        const chapSel = item.querySelector('[data-chapter-select]');
        if (!chapSel) return;
        const currentVal = chapSel.value;
        while (chapSel.options.length > 1) chapSel.remove(1);
        const subId = parseInt(sharedSubject?.value);
        if (!subId) return;
        const sub = subjectsData.find(s => (s.subjectId || s.SubjectId) === subId);
        const chapters = sub?.chapters || sub?.Chapters;
        if (chapters) {
            chapters.forEach(c => {
                chapSel.add(new Option(c.name || c.Name, c.chapterId || c.ChapterId));
            });
        }
        if (currentVal) chapSel.value = currentVal;
    };

    const populateSharedSubjects = () => {
        if (!sharedSubject || !subjectsData?.length) return;
        const current = sharedSubject.value;
        while (sharedSubject.options.length > 1) sharedSubject.remove(1);
        subjectsData.forEach(s => {
            sharedSubject.add(new Option(s.code || s.Code || s.name || s.Name, s.subjectId || s.SubjectId));
        });
        if (current) sharedSubject.value = current;
    };

    // ═══════════════════════════════════
    //  SYNC BATCH
    // ═══════════════════════════════════

    const syncBatch = () => {
        const items = toArray(questionList.querySelectorAll('[data-question-item]'));
        items.forEach((item, idx) => {
            const titleElem = item.querySelector('[data-question-title]');
            if (titleElem) titleElem.textContent = `Câu hỏi #${idx + 1}`;

            const removeBtn = item.querySelector('[data-remove-question]');
            if (removeBtn) removeBtn.disabled = items.length === 1;

            QE.initItem(item, { inputTypesData, subjectsData });
            hidePerItemClassification(item);
            syncChapterForItem(item);

            // Bind toggle
            const toggle = item.querySelector('[data-question-toggle]');
            if (toggle && !toggle._bound) {
                toggle._bound = true;
                toggle.addEventListener('click', (e) => {
                    if (e.target.closest('[data-remove-question]')) return;
                    toggleItem(item);
                });
            }
        });
        if (countText) countText.textContent = `Đang soạn: ${items.length} câu hỏi`;
    };

    // ═══════════════════════════════════
    //  ADD / REMOVE
    // ═══════════════════════════════════

    addBtn?.addEventListener('click', () => {
        const base = questionList.querySelector('[data-question-item]');
        const clone = base.cloneNode(true);
        clone.removeAttribute('data-bound');
        const toggle = clone.querySelector('[data-question-toggle]');
        if (toggle) toggle._bound = false;

        toArray(clone.querySelectorAll('textarea, input[type="text"]')).forEach(i => { i.value = ''; });
        toArray(clone.querySelectorAll('[data-blank-answer-list], [data-blank-group-list]')).forEach(l => {
            while (l.firstChild) l.removeChild(l.firstChild);
        });

        const chapSel = clone.querySelector('[data-chapter-select]');
        if (chapSel) chapSel.value = '';

        const ansList = clone.querySelector('[data-answer-list]');
        if (ansList) {
            while (ansList.firstChild) ansList.removeChild(ansList.firstChild);
            ansList.appendChild(MCQ.createMcqOptionRow(clone, ansList));
            ansList.appendChild(MCQ.createMcqOptionRow(clone, ansList));
            MCQ.syncMcqRows(ansList);
        }

        toArray(clone.querySelectorAll('[data-mcq-render-toggle]')).forEach(sw => { sw.checked = true; });

        // Ensure new item is expanded
        const body = clone.querySelector('[data-question-body]');
        if (body) body.classList.remove('collapsed');
        const icon = clone.querySelector('[data-toggle-icon]');
        if (icon) icon.classList.remove('collapsed');
        clone.classList.remove('is-collapsed');

        questionList.appendChild(clone);
        syncBatch();
        collapseAllExcept(clone);
        setTimeout(() => clone.scrollIntoView({ behavior: 'smooth', block: 'start' }), 100);
    });

    questionList.addEventListener('click', (e) => {
        const removeBtn = e.target.closest('[data-remove-question]');
        if (removeBtn) {
            e.stopPropagation();
            const items = questionList.querySelectorAll('[data-question-item]');
            if (items.length > 1) {
                e.target.closest('[data-question-item]')?.remove();
                syncBatch();
                const remaining = questionList.querySelectorAll('[data-question-item]');
                if (remaining.length > 0) expandItem(remaining[remaining.length - 1]);
            }
        }
    });

    // ═══════════════════════════════════
    //  SUBMIT
    // ═══════════════════════════════════

    const submit = async (status) => {
        if (currentStep === 1) {
            showToast('Vui lòng hoàn thành bước 1 trước.', 'error');
            return;
        }
        if (!sharedSubject?.value) {
            showToast('Vui lòng chọn Môn học trước khi lưu.', 'error');
            return;
        }

        const purposeVal = parseInt(sharedPurpose?.value) || 1;

        try {
            const items = questionList.querySelectorAll('[data-question-item]');
            const payload = toArray(items).map((item, idx) => {
                const p = QE.collectPayload(item);
                if (!p) throw new Error("Không thể thu thập dữ liệu câu hỏi.");
                p.status = status;
                p.questionPurpose = purposeVal;
                if (!p.chapterId) throw new Error(`Câu hỏi #${idx + 1}: Vui lòng chọn Chương.`);
                return p;
            });

            const res = await apiClient.post('/api/questions', payload);
            showToast(`Đã lưu ${res.length} câu hỏi thành công!`);
            setTimeout(() => { window.location.href = '/Question'; }, 1000);
        } catch (err) {
            console.error('Submit error:', err);
            const msg = err.details ? (err.message + '\n' + err.details) : (err.message || 'Không thể lưu.');
            showToast('Lỗi: ' + msg, 'error');
        }
    };

    // Wire both sets of save buttons
    saveAllBtn?.addEventListener('click', () => submit('Active'));
    saveDraftBtn?.addEventListener('click', () => submit('Draft'));
    saveAllBtn2?.addEventListener('click', () => submit('Active'));
    saveDraftBtn2?.addEventListener('click', () => submit('Draft'));

    // ═══════════════════════════════════
    //  INIT
    // ═══════════════════════════════════

    // Start on step 1 — hide save buttons
    saveAllBtn?.classList.add('d-none');
    saveDraftBtn?.classList.add('d-none');

    (async () => {
        try {
            const res = await apiClient.get('/api/questions/metadata');
            const root = (res && res.data) ? res.data : res;
            inputTypesData = root.inputTypes || root.InputTypes || [];
            subjectsData = root.subjects || root.Subjects || [];
            populateSharedSubjects();
            syncBatch();

            if (subjectsData.length === 0 && inputTypesData.length === 0) {
                showToast('Dữ liệu hệ thống vẫn đang trống (Môn học/Giới hạn).', 'error');
            }
        } catch (e) {
            console.error('Failed to load metadata:', e);
            const status = e.xhr ? e.xhr.status : (e.status || 'unknown');
            let msg = `Lỗi ${status}: Không thể tải dữ liệu hệ thống.`;
            if (status === 401) msg += ' Vui lòng đăng nhập lại.';
            else if (status === 403) msg += ' Bạn không có quyền truy cập (Yêu cầu GV).';
            else msg += ' Vui lòng kiểm tra kết nối mạng hoặc máy chủ.';
            showToast(msg, 'error');
        }
    })();
})();
