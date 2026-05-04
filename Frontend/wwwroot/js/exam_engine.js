// FIB rendering qua window.FibKatexRenderer + window.FibInputPanel (Shared modules).
// MCQ giữ nguyên DOM template approach.

class ExamEngine {
    constructor(config) {
        this.data = config.data;
        this.studentAnswers = config.studentAnswers || new Map();
        this.questionNumber = config.initialQuestion || 1;

        this.onQuestionRendered = config.onQuestionRendered || function () { };
        this.onNavUpdated = config.onNavUpdated || function () { };
        this.onProgressUpdated = config.onProgressUpdated || function () { };

        this.fillInBlank = "FillInBlank";
        this.multipleChoice = "MultipleChoice";

        // Khi true, onConfirm callback của panel KHÔNG fire onProgressUpdated/onNavUpdated.
        // Set bởi saveCurrentAnswers() trước flushOpen để tránh save trùng — caller đã/sẽ
        // tự fire callback ở đoạn ngay sau (vd: goToQuestion sẽ gọi 2 callback đó).
        this._suppressCallbacks = false;

        this.dom = {
            questionHeader: document.getElementById('question-header'),
            questionLoading: document.getElementById('question-loading'),
            questionArea: document.getElementById('question-area'),
            questionStem: document.getElementById('question-stem'),
            questionTitle: document.getElementById('question-title'),
            questionMcq: document.getElementById('question-block-mcq'),
            questionBlockFib: document.getElementById('question-block-fib'),
            fibKatexFrame: document.getElementById('fib-katex-frame'),
            questionNav: document.getElementById('questionNav'),
            inputTypeContainer: document.getElementById('inputTypeContainer'),
            inputTypeBody: document.getElementById('inputTypeBody'),
            mcqTemplate: document.getElementById("mcq-template"),
        };
    }

    init() {
        if (!this.data || !this.data.questions) return;
        this.buildQuestionNav(this.data.questions.length);
        this.goToQuestion(this.questionNumber);
    }

    buildQuestionNav(total) {
        if (!this.dom.questionNav) return;
        this.dom.questionNav.innerHTML = '';
        for (let i = 1; i <= total; i++) {
            const btn = document.createElement('button');
            btn.className = 'btn btn-outline-primary btn-sm m-1';
            btn.textContent = i;
            btn.style.minWidth = '40px';
            btn.addEventListener('click', () => this.goToQuestion(i));
            this.dom.questionNav.appendChild(btn);
        }
    }

    goToQuestion(num) {
        this.saveCurrentAnswers();
        this.questionNumber = num;
        this.renderQuestion(this.data.questions[num - 1]);
        this.onNavUpdated(this.questionNumber, this.data.questions, this.studentAnswers);
        this.onProgressUpdated(this.data.questions, this.studentAnswers);
    }

    renderQuestion(question) {
        this.dom.questionHeader.hidden = true;
        this.dom.questionArea.hidden = true;
        this.dom.questionLoading.hidden = false;
        this.dom.questionBlockFib.hidden = true;
        this.dom.questionMcq.hidden = true;

        if (question.questionType === this.fillInBlank) {
            const questionContent = JSON.parse(question.questionContent);
            this.dom.questionStem.textContent = questionContent.stem;
            this.dom.questionStem.render?.();
            this.dom.questionTitle.textContent = "Câu " + this.questionNumber;
            this._renderFibQuestion(question, questionContent);
        }
        else if (question.questionType === this.multipleChoice) {
            this.dom.questionStem.textContent = question.questionContent;
            this.dom.questionStem.render?.();
            this.dom.questionTitle.textContent = "Câu " + this.questionNumber;
            this.dom.questionMcq.hidden = false;
            this.dom.questionMcq.innerHTML = "";

            question.answers.forEach(answer => {
                const newOption = this.dom.mcqTemplate.content.cloneNode(true);
                const li = newOption.querySelector("li");
                const checkbox = li.querySelector("input");
                const answerId = String(answer.questionAnswerId);
                checkbox.value = answerId;

                if (this.studentAnswers.has(answerId)) checkbox.checked = true;

                checkbox.addEventListener('change', e => {
                    if (e.target.checked) this.studentAnswers.set(answerId, '');
                    else this.studentAnswers.delete(answerId);
                });

                const mathSpan = li.querySelector("math-span");
                mathSpan.textContent = answer.content;
                mathSpan.render?.();

                this.dom.questionMcq.appendChild(newOption);
            });
        }

        this.dom.questionLoading.hidden = true;
        this.dom.questionHeader.hidden = false;
        this.dom.questionArea.hidden = false;
        this.onQuestionRendered();
    }

    _renderFibQuestion(question, questionContent) {
        if (!this.dom.fibKatexFrame) return;

        const answeredMap = new Map();
        question.answers.forEach(answer => {
            const stored = this.studentAnswers.get(String(answer.questionAnswerId));
            if (stored && stored.trim()) {
                const blankId = this._extractBlankId(answer.content);
                if (blankId) answeredMap.set(blankId, stored);
            }
        });

        window.FibKatexRenderer.render(this.dom.fibKatexFrame, questionContent.frame, {
            mode: 'student',
            onBlankClick: blankId => this._onBlankClick(blankId, question),
            answeredMap,
        });
        this.dom.questionBlockFib.hidden = false;
    }

    _extractBlankId(answerContent) {
        const m = (answerContent || '').match(/\\placeholder\[(\d+)\]/);
        return m ? m[1] : null;
    }

    _onBlankClick(blankId, question) {
        if (window.FibInputPanel.isOpen()) window.FibInputPanel.flushOpen();

        const answer = question.answers.find(a => this._extractBlankId(a.content) === blankId);
        if (!answer) return;

        const answerKey = String(answer.questionAnswerId);
        const currentValue = this.studentAnswers.get(answerKey) || '';

        this._highlightActiveBlank(blankId);
        this._updateInputLimitSidebar(answer);

        window.FibInputPanel.open({
            answer,
            currentValue,
            onConfirm: latex => {
                const trimmed = (latex || '').trim();
                if (trimmed) this.studentAnswers.set(answerKey, trimmed);
                else         this.studentAnswers.delete(answerKey);

                // Re-render frame chỉ khi user vẫn ở cùng câu (tránh ghi đè khi đã chuyển câu).
                const currentQuestion = this.data.questions[this.questionNumber - 1];
                if (currentQuestion === question) {
                    this._renderFibQuestion(question, JSON.parse(question.questionContent));
                }
                if (this._suppressCallbacks) return;
                this.onProgressUpdated(this.data.questions, this.studentAnswers);
                this.onNavUpdated(this.questionNumber, this.data.questions, this.studentAnswers);
            },
            onCancel: () => this._clearHighlight(),
        });
    }

    _highlightActiveBlank(blankId) {
        document.querySelectorAll('.fib-blank.fib-blank-active')
            .forEach(b => b.classList.remove('fib-blank-active'));
        const target = document.querySelector(`.fib-blank[data-blank-id="${blankId}"]`);
        if (target) target.classList.add('fib-blank-active');
    }

    _clearHighlight() {
        document.querySelectorAll('.fib-blank.fib-blank-active')
            .forEach(b => b.classList.remove('fib-blank-active'));
    }

    _updateInputLimitSidebar(answer) {
        const container = this.dom.inputTypeContainer;
        const body = this.dom.inputTypeBody;
        if (!container || !body) return;

        const types = answer.inputTypes || [];
        if (!types.length) {
            body.innerHTML = '<tr><td class="text-muted small">Không giới hạn định dạng</td></tr>';
        } else {
            body.innerHTML = types.map(it =>
                `<tr><td><strong>${this._escape(it.name)}</strong></td></tr>`
            ).join('');
        }
        container.hidden = false;
    }

    _escape(s) {
        return String(s).replace(/[&<>"']/g, c => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[c]));
    }

    saveCurrentAnswers() {
        if (!this.data) return;
        const question = this.data.questions[this.questionNumber - 1];
        if (!question) return;

        // FIB: panel onConfirm đã ghi vào studentAnswers; flushOpen handle
        // trường hợp user gõ giữa chừng nhưng chưa bấm "Xác nhận".
        // Suppress callbacks để tránh save POST trùng — goToQuestion / submit
        // flow caller sẽ tự fire onProgressUpdated/onNavUpdated ở bước sau.
        if (window.FibInputPanel?.isOpen()) {
            this._suppressCallbacks = true;
            try { window.FibInputPanel.flushOpen(); }
            finally { this._suppressCallbacks = false; }
        }

        if (question.questionType === this.multipleChoice) {
            const checkboxes = this.dom.questionMcq.querySelectorAll('input[name="answer"]');
            checkboxes.forEach(cb => {
                const answerId = String(cb.value);
                if (cb.checked) this.studentAnswers.set(answerId, '');
                else this.studentAnswers.delete(answerId);
            });
        }
    }

    validateAllAnswers() {
        if (!this.data) return { isValid: true, errors: [], invalidQNums: [] };
        const errors = [];
        const invalidSet = new Set();

        this.data.questions.forEach((question, index) => {
            if (question.questionType !== this.fillInBlank) return;
            question.answers.forEach(answer => {
                if (!answer.inputTypes?.length) return;
                const latex = (this.studentAnswers.get(String(answer.questionAnswerId)) || '').trim();
                if (!latex) return;

                // BE trả mảng inputTypes — match ÍT NHẤT 1 regex là pass.
                const ok = answer.inputTypes.some(it => {
                    try { return new RegExp(it.regex).test(latex); }
                    catch (e) {
                        console.warn(`[FIB] Bad regex from DB inputTypeId=${it.inputTypeId}: ${it.regex}`, e);
                        return true; // fail-open: KHÔNG block submit khi DB regex hỏng
                    }
                });

                if (!ok) {
                    const names = answer.inputTypes.map(it => it.name).join(', ');
                    const qNum = index + 1;
                    errors.push(`Câu ${qNum}: Yêu cầu <b>${names}</b> nhưng nhập '${latex}' không khớp.`);
                    invalidSet.add(qNum);
                }
            });
        });

        return { isValid: errors.length === 0, errors, invalidQNums: [...invalidSet] };
    }

    getStudentAnswerDtos() {
        const dtos = [];
        if (this.studentAnswers) {
            this.studentAnswers.forEach((response, questionAnswerId) => {
                dtos.push({
                    questionAnswerId: parseInt(questionAnswerId),
                    response: response === '' ? null : response
                });
            });
        }
        return dtos;
    }
}
