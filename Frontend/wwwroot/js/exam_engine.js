class ExamEngine {
    constructor(config) {
        this.data = config.data; // object containing .questions
        this.studentAnswers = config.studentAnswers || new Map();
        this.questionNumber = config.initialQuestion || 1;

        // Optional callbacks
        this.onQuestionRendered = config.onQuestionRendered || function () { };
        this.onNavUpdated = config.onNavUpdated || function () { };
        this.onProgressUpdated = config.onProgressUpdated || function () { };

        // Constants
        this.regexPlaceholder = /\\?placeholder\[([^\]]+)\](?:\{\})?/;
        this.fillInBlank = "FillInBlank";
        this.multipleChoice = "MultipleChoice";
        this.selectionHandler = null;

        // DOM Elements
        this.dom = {
            questionHeader: document.getElementById('question-header'),
            questionLoading: document.getElementById('question-loading'),
            questionArea: document.getElementById('question-area'),
            questionStem: document.getElementById('question-stem'),
            questionTitle: document.getElementById('question-title'),
            questionMcq: document.getElementById('question-block-mcq'),
            questionBlockFib: document.getElementById('question-block-fib'),
            questionBlockFibField: document.getElementById('question-block-fib-field'),
            questionNav: document.getElementById('questionNav'),
            inputTypeContainer: document.getElementById('inputTypeContainer'),
            inputTypeBody: document.getElementById('inputTypeBody'),
            mcqTemplate: document.getElementById("mcq-template"),
            inputLimitBtnTemplate: document.getElementById("input-limit-btn-template"),
            inputLimitTextTemplate: document.getElementById("input-limit-text-template")
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
        if (this.selectionHandler && this.dom.questionBlockFibField) {
            this.dom.questionBlockFibField.removeEventListener("selection-change", this.selectionHandler);
            this.selectionHandler = null;
        }

        this.dom.questionHeader.hidden = true;
        this.dom.questionArea.hidden = true;
        this.dom.questionLoading.hidden = false;
        this.dom.questionBlockFib.hidden = true;
        this.dom.questionMcq.hidden = true;
        this.dom.questionBlockFibField.hidden = true;

        if (question.questionType === this.fillInBlank) {
            let questionContent = JSON.parse(question.questionContent);
            this.dom.questionStem.textContent = questionContent.stem;
            this.dom.questionStem.render?.();
            this.dom.questionTitle.textContent = "Câu " + this.questionNumber;
            this.dom.questionBlockFibField.value = questionContent.frame;
            this.dom.questionBlockFib.hidden = false;
            this.dom.questionBlockFibField.hidden = false;

            // Restore FIB answers
            question.answers.forEach(answer => {
                const answerId = String(answer.questionAnswerId);
                if (this.studentAnswers.has(answerId)) {
                    const placeholderMatch = answer.content.match(this.regexPlaceholder);
                    if (placeholderMatch) {
                        const promptId = placeholderMatch[1];
                        this.dom.questionBlockFibField.setPromptValue(promptId, this.studentAnswers.get(answerId), { silenceNotifications: true });
                    }
                }
            });

            this.selectionHandler = () => {
                let placeholderId = [];
                for (const item of question.answers) {
                    const match = item.content.match(this.regexPlaceholder);
                    if (match) {
                        placeholderId.push(match[1]);
                    }
                }
                this.selectPlaceholder(this.dom.questionBlockFibField, placeholderId, question);
            };
            this.dom.questionBlockFibField.addEventListener("selection-change", this.selectionHandler);
        }
        else if (question.questionType === this.multipleChoice) {
            this.dom.questionStem.textContent = question.questionContent;
            this.dom.questionStem.render?.();
            this.dom.questionTitle.textContent = "Câu " + this.questionNumber;
            this.dom.questionMcq.hidden = false;
            this.dom.questionMcq.innerHTML = "";

            question.answers.forEach((answer) => {
                let newOption = this.dom.mcqTemplate.content.cloneNode(true);
                let li = newOption.querySelector("li");
                let checkbox = li.querySelector("input");
                let answerId = String(answer.questionAnswerId);
                checkbox.value = answerId;

                if (this.studentAnswers.has(answerId)) {
                    checkbox.checked = true;
                }

                checkbox.addEventListener('change', (e) => {
                    if (e.target.checked) this.studentAnswers.set(answerId, '');
                    else this.studentAnswers.delete(answerId);
                });

                let mathSpan = li.querySelector("math-span");
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

    saveCurrentAnswers() {
        if (!this.data) return;
        const question = this.data.questions[this.questionNumber - 1];
        if (!question) return;

        if (question.questionType === this.multipleChoice) {
            const checkboxes = this.dom.questionMcq.querySelectorAll('input[name="answer"]');
            checkboxes.forEach(cb => {
                const answerId = String(cb.value);
                if (cb.checked) this.studentAnswers.set(answerId, '');
                else this.studentAnswers.delete(answerId);
            });
        }
        else if (question.questionType === this.fillInBlank) {
            const promptIds = this.dom.questionBlockFibField.getPrompts();
            promptIds.forEach(promptId => {
                const answer = question.answers.find(a => {
                    const match = a.content.match(this.regexPlaceholder);
                    return match && match[1] === promptId;
                });
                if (answer) {
                    const latex = this.dom.questionBlockFibField.getPromptValue(promptId, 'latex');
                    const answerId = String(answer.questionAnswerId);
                    if (latex && latex.trim() !== '') this.studentAnswers.set(answerId, latex);
                    else this.studentAnswers.delete(answerId);
                }
            });
        }
    }

    validateAllAnswers() {
        if (!this.data) return { isValid: true, errors: [], invalidQNums: [] };
        const errors = [];
        const invalidQNums = [];

        this.data.questions.forEach((question, index) => {
            if (question.questionType !== this.fillInBlank) return;

            question.answers.forEach(answer => {
                if (answer && answer.inputTypes && answer.inputTypes.length > 0) {
                    const answerId = String(answer.questionAnswerId);
                    const latex = this.studentAnswers.get(answerId) || "";
                    if (latex.trim() === "") return;

                    const typeNames = answer.inputTypes.map(it => it.name.toLowerCase());
                    const questionNum = index + 1;

                    if (typeNames.includes('số tự nhiên') || typeNames.includes('natural number')) {
                        const naturalRegex = /^\s*\d+\s*$/;
                        if (!naturalRegex.test(latex)) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Số tự nhiên</b> (ví dụ: 5) nhưng bạn đã nhập '${latex}'.`);
                            invalidQNums.push(questionNum);
                        }
                    }
                    else if (typeNames.includes('số nguyên') || typeNames.includes('integer')) {
                        const intRegex = /^\s*-?\d+\s*$/;
                        if (!intRegex.test(latex)) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Số nguyên</b> (ví dụ: -5, 5) nhưng bạn đã nhập '${latex}'.`);
                            invalidQNums.push(questionNum);
                        }
                    }
                    else if (typeNames.includes('số hữu tỉ') || typeNames.includes('rational number')) {
                        if (latex.includes('\\sqrt') || latex.includes('\\pi') || latex.includes('\\sin') || latex.includes('\\cos') || latex.includes('\\lim')) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Số hữu tỉ</b> (phân số, số thập phân) nhưng chứa ký hiệu vô tỉ/hàm số.`);
                            invalidQNums.push(questionNum);
                        }
                    }
                    else if (typeNames.includes('số vô tỉ') || typeNames.includes('irrational number')) {
                        if (!latex.includes('\\sqrt') && !latex.includes('\\pi') && !latex.includes('e') && !latex.includes('\\phi')) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Số vô tỉ</b> (chứa căn, π, e...) nhưng bạn đã nhập '${latex}'.`);
                            invalidQNums.push(questionNum);
                        } else if (latex.includes('\\sin') || latex.includes('\\cos') || latex.includes('\\tan') || latex.includes('\\log')) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Số vô tỉ</b> nhưng chứa hàm số lượng giác/logarit '${latex}'.`);
                            invalidQNums.push(questionNum);
                        }
                    }
                    else if (typeNames.includes('hàm lượng giác/logarit') || typeNames.includes('trigonometry')) {
                        if (!latex.includes('\\sin') && !latex.includes('\\cos') && !latex.includes('\\tan') && !latex.includes('\\cot') && !latex.includes('\\log') && !latex.includes('\\ln')) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Hàm lượng giác/Logarit</b> nhưng không tìm thấy sin, cos, tan, log, ln...`);
                            invalidQNums.push(questionNum);
                        }
                    }
                    else if (typeNames.includes('hàm lim') || typeNames.includes('limit')) {
                        if (!latex.includes('\\lim')) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Hàm lim</b> nhưng không tìm thấy ký hiệu giới hạn (lim).`);
                            invalidQNums.push(questionNum);
                        }
                    }
                    else if (typeNames.includes('ma trận') || typeNames.includes('matrix')) {
                        if (!latex.includes('matrix')) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Ma trận</b> nhưng không tìm thấy định dạng ma trận.`);
                            invalidQNums.push(questionNum);
                        }
                    }
                    else if (typeNames.includes('số phức') || typeNames.includes('complex number')) {
                        if (!latex.includes('i') && !latex.includes('j')) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Số phức</b> nhưng không tìm thấy phần ảo (i hoặc j).`);
                            invalidQNums.push(questionNum);
                        }
                    }
                    else if (typeNames.includes('biểu thức so sánh')) {
                        if (!latex.match(/[<>\=]|\\ge|\\le|\\neq|\\approx|\\equiv/)) {
                            errors.push(`Câu ${questionNum}: Yêu cầu <b>Biểu thức so sánh</b> nhưng không tìm thấy dấu (>, <, =, ...).`);
                            invalidQNums.push(questionNum);
                        }
                    }
                }
            });
        });

        return { isValid: errors.length === 0, errors: errors, invalidQNums: [...new Set(invalidQNums)] };
    }

    selectPlaceholder(mathField, placeholderId, currentQuestion) {
        const pos = mathField.position;
        let isInsidePlaceholder = false;

        for (const id of placeholderId) {
            const range = mathField.getPromptRange(id);
            if (range && pos >= range[0] && pos <= range[1]) {
                isInsidePlaceholder = true;
                const answer = currentQuestion.answers.find(a => {
                    const match = a.content.match(this.regexPlaceholder);
                    return match && match[1] === id;
                });

                if (this.dom.inputTypeContainer && this.dom.inputTypeBody) {
                    this.dom.inputTypeBody.innerHTML = '';
                    const headerTh = this.dom.inputTypeContainer.querySelector('thead th');

                    if (answer && answer.inputTypes && answer.inputTypes.length > 0) {
                        if (headerTh) {
                            const names = answer.inputTypes.map(it => it.name).join(', ');
                            headerTh.textContent = 'Loại dữ liệu: ' + names;
                        }
                        answer.inputTypes.forEach(it => {
                            const mappedLatex = typeof inputTypeMathMapping !== 'undefined' ? inputTypeMathMapping[it.name] : null;

                            if (Array.isArray(mappedLatex)) {
                                if (mappedLatex.length > 0) {
                                    const tr = document.createElement('tr');
                                    const td = document.createElement('td');
                                    td.className = "d-flex flex-wrap gap-2";

                                    mappedLatex.forEach(latexStr => {
                                        const btn = document.createElement('button');
                                        btn.type = "button";
                                        btn.className = "btn btn-outline-primary btn-sm";
                                        btn.innerHTML = `<math-span>${latexStr}</math-span>`;

                                        setTimeout(() => {
                                            const ms = btn.querySelector('math-span');
                                            if (ms && ms.render) ms.render();
                                        }, 0);

                                        btn.onmousedown = e => e.preventDefault();
                                        btn.onclick = () => {
                                            mathField.focus();
                                            mathField.insert(latexStr);
                                        };
                                        td.appendChild(btn);
                                    });
                                    tr.appendChild(td);
                                    this.dom.inputTypeBody.appendChild(tr);
                                }
                            } else if (it.groupType === 'Sets') {
                                const newRow = this.dom.inputLimitTextTemplate.content.cloneNode(true);
                                newRow.querySelector('span').textContent = 'Tập hợp: ' + it.name;
                                this.dom.inputTypeBody.appendChild(newRow);
                            } else {
                                const newRow = this.dom.inputLimitBtnTemplate.content.cloneNode(true);
                                const btn = newRow.querySelector('button');

                                const latexToInsert = mappedLatex || ('\\text{' + it.name + '}');

                                if (mappedLatex) {
                                    btn.innerHTML = `<math-span>${mappedLatex}</math-span>`;
                                    setTimeout(() => {
                                        const ms = btn.querySelector('math-span');
                                        if (ms && ms.render) ms.render();
                                    }, 0);
                                } else {
                                    btn.textContent = it.name;
                                }

                                btn.onmousedown = e => e.preventDefault();
                                btn.onclick = () => { mathField.focus(); mathField.insert(latexToInsert); };
                                this.dom.inputTypeBody.appendChild(newRow);
                            }
                        });
                        this.dom.inputTypeContainer.hidden = false;
                    } else {
                        if (headerTh) headerTh.textContent = 'Loại dữ liệu';
                        this.dom.inputTypeContainer.hidden = true;
                    }
                }
                break;
            }
        }
        if (!isInsidePlaceholder) {
            if (this.dom.inputTypeContainer && this.dom.inputTypeBody) {
                const headerTh = this.dom.inputTypeContainer.querySelector('thead th');
                if (headerTh) headerTh.textContent = 'Loại dữ liệu';

                this.dom.inputTypeBody.innerHTML = '<tr><td class="text-muted small">Vui lòng click vào một ô trống để xem các ký hiệu hỗ trợ.</td></tr>';
                this.dom.inputTypeContainer.hidden = false;
            }
        }
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
