let examSignalRConnection = null;

$(document).ready(function () {
    const examId = $('#ExamId').val();

    if (!examId) {
        Swal.fire('Lỗi', 'Thiếu định danh đề thi!', 'error');
        return;
    }

    // 1. Khởi tạo bài làm (Start Submission)
    // Hệ thống sẽ tự bốc thăm hoặc ưu tiên bài đanh làm dở theo cấu hình backend
    apiClient.post('/api/student/exams/submission/start', { ExamId: parseInt(examId) })
        .then(function (res) {
            $('#SubmissionId').val(res.submissionId);

            // Render UI directy using the paper object returned from the start endpoint
            if (res.paper) {
                renderExamUI(res.paper, res.remainingSeconds, res.savedAnswers);
            } else {
                Swal.fire('Không thể tìm thấy', 'Không thể tải hệ thống đề thi.', 'error');
                return;
            }

            // 2. Khởi tạo kết nối SignalR để giám sát
            initSignalRConnection(examId);

            // 3. Khởi tạo Anti-Cheat
            // initAntiCheat(); // Tạm tắt để debug giao diện
        })
        .catch(function (error) {
            console.error("Start submission error:", error);
            Swal.fire('Lỗi đăng nhập', 'Lỗi khi bắt đầu làm bài. Vui lòng kiểm tra tài khoản.', 'error');
        });
});



// Hàm Helper: Khắc phục lỗi MathLive không tự xuống dòng và dính ký tự bằng cách tách riêng Text (html thường) và Math (MathLive)
function renderMixedContent(str, isFillInTheBlank = false, questionId = null) {
    if (!str) return '';
    str = str.replace(/<br\s*\/?>/gi, '<br>');
    const parts = str.split('$');
    let html = '<div style="line-height: 1.8; word-wrap: break-word; white-space: normal;">';

    // Regex nhận diện chỗ trống
    const blankRegex = /\[\s*fill_[0-9]+\s*\]/gi;
    const generalBlankRegex = /(\.{3,}|_{3,}|\\[cC]dots|\\[lL]dots|\\[dD]dots|\\[vV]dots)/gi;

    let pCount = 0;

    for (let i = 0; i < parts.length; i++) {
        let segment = parts[i];
        if (i % 2 === 0) {
            // Nằm ngoài thẻ $ (là chữ Tiếng Việt bình thường) -> Giữ nguyên span html để text có thể tự do mọc dòng và cách chữ
            if (isFillInTheBlank) {
                segment = segment.replace(blankRegex, function (match) {
                    return `<math-field class="math-input answer-field d-inline-block align-middle mx-1" style="min-width: 50px; padding: 0.2rem; --placeholder-background-color: #ffffff; --placeholder-color: #333333; background: transparent; border: none;" data-qid="${questionId}" data-is-fill="true" oninput="autoSaveFillInTheBlank(${questionId}, this)">\\placeholder[p${pCount++}]{}</math-field>`;
                });
                segment = segment.replace(generalBlankRegex, function (match) {
                    return `<math-field class="math-input answer-field d-inline-block align-middle mx-1" style="min-width: 50px; padding: 0.2rem; --placeholder-background-color: #ffffff; --placeholder-color: #333333; background: transparent; border: none;" data-qid="${questionId}" data-is-fill="true" oninput="autoSaveFillInTheBlank(${questionId}, this)">\\placeholder[p${pCount++}]{}</math-field>`;
                });
            }
            html += `<span>${segment}</span>`;
        } else {
            // Nằm block toán học -> Tạo Box Mathlive
            if (isFillInTheBlank && (segment.match(blankRegex) || segment.match(generalBlankRegex))) {
                // Những công thức toán học có biểu thức trống [fill] hoặc ... phải biến mình thành ô tương tác để học sinh sửa
                let mathContent = segment;
                mathContent = mathContent.replace(blankRegex, function () { return `\\placeholder[p${pCount++}]{}`; });
                mathContent = mathContent.replace(generalBlankRegex, function () { return `\\placeholder[p${pCount++}]{}`; });
                html += `<math-field class="math-input answer-field d-inline-block align-middle mx-1" style="min-width: 60px; padding: 0.2rem; --placeholder-background-color: #ffffff; --placeholder-color: #333333; background: transparent; border: none; border-bottom: 2px dashed #007bff; border-radius: 0;" data-qid="${questionId}" data-is-fill="true" oninput="autoSaveFillInTheBlank(${questionId}, this)">${mathContent}</math-field>`;
            } else {
                // Biểu thức toán học thuần túy (không chứa placeholder điền khuyết)
                html += `<math-field read-only class="math-display d-inline-block align-middle mx-0 px-1" style="border:none !important; background:transparent !important; min-height:auto;">${segment}</math-field>`;
            }
        }
    }
    html += '</div>';
    return html;
}

function renderExamUI(paper, remainingSeconds, savedAnswers) {
    $('#examTitle').text(paper.title);
    $('#examSubtitle').text(paper.description || 'Sinh viên đang làm bài tự động lưu');

    // Display Paper Code
    if (paper.code) {
        $('#paperCodeDisplay').text(paper.code);
    } else {
        $('#paperCodeDisplay').text('N/A');
    }

    const container = $('#questionContainer');
    const navMap = $('#questionNavMap');
    container.empty();
    navMap.empty();

    let shuffledQuestions = [...paper.questions];

    shuffledQuestions.forEach((q, index) => {
        const qIndex = index + 1;

        // Build Navigation Button
        const navBtn = `<button class="btn btn-sm btn-outline-primary question-nav-btn" id="nav-btn-${q.questionId}" onclick="goToQuestion(${qIndex})">${qIndex}</button>`;
        navMap.append(navBtn);

        let answerAreaHtml = '';
        let displayContent = q.contentLatex || '';

        let shortAnswerData = null;

        // The Backend already parsed and shuffled these into q.options and q.steps
        let isFillInTheBlank = false;

        // Chuẩn hóa format nội dung hiển thị sang dạng MathLive giữ khoảng trắng chữ
        let originalContent = displayContent;
        // Tạm thời biến finalDisplayContent thành originalContent, hàm renderMixedContent sẽ sinh ra html xịn hơn sau.

        if (q.questionType === 'FillInBlank') {
            try {
                if (q.answer) {
                    let parsed = JSON.parse(q.answer);
                    // parsed là mảng chứa các đáp án cho ô trống
                    if (Array.isArray(parsed) && parsed.length >= 1) {
                        shortAnswerData = parsed;
                    }
                }
            } catch (e) { }

            isFillInTheBlank = true; // Luôn coi là điền khuyết
            const blankRegex = /(\.{3,}|_{3,}|\\[cC]dots|\\[lL]dots|\\[dD]dots|\\[vV]dots|\[\s*fill_[0-9]+\s*\])/gi;

            if (!originalContent.match(blankRegex)) {
                if (shortAnswerData && shortAnswerData.length >= 1) {
                    // Nếu DB báo mảng n phần tử mà RegExp không tìm ra chỗ trống rõ ràng
                    for (let i = 0; i < shortAnswerData.length; i++) {
                        originalContent += ` [fill_${i}]`;
                    }
                } else {
                    originalContent += ` [fill_0]`;
                }
            }
        }

        // Tạo ra content bằng html thay vì 1 khung mathlive khổng lồ
        let finalDisplayContent = renderMixedContent(originalContent, isFillInTheBlank, q.questionId);

        // Handle Step-by-Step format
        if (q.steps && Array.isArray(q.steps) && q.steps.length > 0) {
            let stepHtml = '';
            q.steps.forEach((stepObj, idx) => {
                let s = stepObj.step;
                let hint = stepObj.hint;

                stepHtml += `
                    <div class="step-card mb-3 p-3 border rounded bg-light" id="step-${q.questionId}-${s}" style="box-shadow: 0 0.125rem 0.25rem rgba(0,0,0,0.075);">
                        <h6 class="fw-bold text-primary mb-2">Bước ${s}:</h6>
                        <!-- Mốc để học sinh điền kết quả vào -->
                        <label class="form-label mt-2">Trả lời bước ${s}:</label>
                        <math-field class="math-input answer-field mb-2" id="input-${q.questionId}-${s}" data-qid="${q.questionId}" data-step="${s}" oninput="autoSaveAnswer(${q.questionId}, this.value, '${s}')"></math-field>
                `;
                if (hint) {
                    stepHtml += `
                        <div class="alert alert-secondary py-2 mt-2 mb-0" style="font-size: 0.85rem;">
                            <strong>💡 Gợi ý bước ${s}:</strong> ${hint}
                        </div>
                    `;
                }
                stepHtml += `</div>`;
            });
            answerAreaHtml = stepHtml;
            answerAreaHtml = stepHtml;
        }
        // Handle Fill-in-the-blank (Điền khuyết) format inline
        else if (isFillInTheBlank) {
            // Bởi vì câu hỏi FillInBlank đã tích hợp sẵn khung nhập (math field) ngay trong phần Content sinh ra bởi renderMixedContent
            answerAreaHtml = `
                <div class="mb-3">
                    <label class="form-label text-primary fw-bold" style="font-size: 0.9rem;">Hoàn thiện biểu thức / Điền trực tiếp vào ô trống:</label>
                    <div class="border rounded p-3 bg-white" style="font-size: 1.15rem;">
                        ${finalDisplayContent}
                    </div>
                </div>
                <small class="text-muted">Nhấn phím Tab hoặc click chuột để chuyển qua lại giữa các ô trống.</small>
            `;
            // Ẩn câu hỏi dư bằng cách cho thành rỗng, vì ta đã in ra trong answerAreaHtml
            finalDisplayContent = '';
        }
        // Handle MultipleChoice format
        else if (q.questionType === 'MultipleChoice') {
            if (q.options && q.options.length > 0) {
                // IMPORTANT: q.options is ALREADY parsed, cleaned of answers, and cleanly shuffled by the C# Backend
                q.options.forEach((opt, idx) => {
                    const displayLetter = ['A', 'B', 'C', 'D', 'E', 'F'][idx] || '?';
                    // opt.id is the real letter mapping safely managed by the server
                    answerAreaHtml += `
                        <div class="form-check mb-2 d-flex align-items-center">
                            <input class="form-check-input answer-field me-2" type="radio" name="q-${q.questionId}" id="q${q.questionId}${displayLetter}" value="${opt.id}" data-qid="${q.questionId}" onchange="autoSaveAnswer(${q.questionId}, this.value)">
                            <label class="form-check-label w-100 d-flex align-items-center" for="q${q.questionId}${displayLetter}" style="font-size: 1.1rem; line-height: 1.5; cursor: pointer;">
                                <span class="fw-bold me-2">${displayLetter}.</span>
                                <div class="flex-grow-1">${renderMixedContent(opt.text)}</div>
                            </label>
                        </div>
                    `;
                });
            } else {
                answerAreaHtml = `Lỗi hiển thị đáp án: không tìm thấy nội dung Options từ máy chủ.`;
            }
        } else {
            answerAreaHtml = `
                <math-field class="math-input answer-field" data-qid="${q.questionId}" oninput="autoSaveAnswer(${q.questionId}, this.value)"></math-field>
            `;
        }

        let typeLabel = '(Tự luận)';
        if (q.questionType === 'MultipleChoice') typeLabel = '(Trắc nghiệm)';
        else if (q.questionType === 'StepByStep') typeLabel = '(Từng bước)';
        else if (q.questionType === 'ShortAnswer' || q.questionType === 'Trả lời ngắn') typeLabel = '(Trả lời ngắn)';
        else if (q.questionType === 'FillInBlank') typeLabel = '(Điền khuyết - Điền trực tiếp)';
        else if (q.questionType) typeLabel = `(${q.questionType})`;

        // Build Question HTML (Pagination style)
        const displayStyle = qIndex === 1 ? 'block' : 'none';

        // Nếu là Điền Khuyết Inline, ta bỏ phần view read-only ở trên đi vì nó đã tích hợp thẳng vào ô trả lời
        const latexViewerHtml = q.questionType !== 'FillInBlank' && displayContent ? `
            <div class="mb-2" style="font-size: 1.15rem;">
                ${renderMixedContent(displayContent)}
            </div>
        ` : '';

        const html = `
          <section class="question-block" id="question-index-${qIndex}" data-question-id="${q.questionId}" data-question-index="${qIndex}" style="display: ${displayStyle}">
            <h2 class="question-title fs-5 fw-bold text-dark mb-3">Câu ${qIndex} <span class="text-primary">${typeLabel}</span></h2>
            ${latexViewerHtml}
            <div class="mt-3">
                <label class="form-label text-muted">Phần trả lời của bạn:</label>
                <div class="bg-white p-3 border rounded shadow-sm">
                    ${answerAreaHtml}
                </div>
            </div>
          </section>
        `;
        container.append(html);
    });

    // Append Pagination Controls
    const paginationHtml = `
      <div class="d-flex justify-content-between mt-4 pt-3 border-top" id="pagination-controls">
         <button class="btn btn-secondary px-4" id="btn-prev-question" onclick="goToQuestion(currentQuestionIndex - 1)" disabled>
           &laquo; Câu trước
         </button>
         <button class="btn btn-primary px-4" id="btn-next-question" onclick="goToQuestion(currentQuestionIndex + 1)">
           Câu tiếp theo &raquo;
         </button>
      </div>
    `;
    container.append(paginationHtml);

    // Initial Nav Button Update
    updateNavButtonsStyling();

    // Start countdown timer from server calculation
    startCountdownTimer(remainingSeconds);

    // Fill saved answers
    if (savedAnswers && savedAnswers.length > 0) {
        savedAnswers.forEach(ans => {
            const rowIdx = ans.questionIndex;
            const block = $(`#question-index-${rowIdx}`);
            if (block.length > 0) {
                const inputs = block.find('.answer-field');
                let isAnswered = false;

                if (inputs.attr('type') === 'radio') {
                    inputs.each(function () {
                        if ($(this).val() === ans.responseText) {
                            $(this).prop('checked', true);
                            isAnswered = true;
                        }
                    });
                } else if (inputs.length > 0 && inputs[0].tagName.toLowerCase() === 'math-field') {
                    // Check if it is Inline Fill-in-the-blank
                    if ($(inputs[0]).data('is-fill') === true) {
                        try {
                            let savedAnsArray = JSON.parse(ans.responseText);
                            if (Array.isArray(savedAnsArray)) {
                                let ansIndex = 0;
                                inputs.each(function () {
                                    let mf = this;
                                    let promptIds = mf.getPrompts ? mf.getPrompts() : [];
                                    if (promptIds && promptIds.length > 0) {
                                        promptIds.forEach(id => {
                                            if (ansIndex < savedAnsArray.length && savedAnsArray[ansIndex] !== "") {
                                                mf.setPromptValue(id, savedAnsArray[ansIndex], { focus: false });
                                                isAnswered = true;
                                            }
                                            ansIndex++;
                                        });
                                    } else {
                                        // Backup case for no prompt IDs
                                        if (ansIndex < savedAnsArray.length && savedAnsArray[ansIndex] !== "") {
                                            mf.value = savedAnsArray[ansIndex];
                                            isAnswered = true;
                                        }
                                        ansIndex++;
                                    }
                                });
                            }
                        } catch (e) {
                            console.error("Error parsing saved Inline Fill-in-the-blank:", e);
                        }
                    } else if ($(inputs[0]).data('step') !== undefined) {
                        // For StepByStep
                        try {
                            let stepAnswers = JSON.parse(ans.responseText);
                            inputs.each(function () {
                                let sIdx = $(this).data('step');
                                if (sIdx && stepAnswers[`step${sIdx}`]) {
                                    this.value = stepAnswers[`step${sIdx}`];
                                    isAnswered = true;
                                }
                            });
                        } catch (e) {
                            console.error("Error parsing saved StepByStep answers:", e);
                        }
                    } else if (inputs.length === 1) {
                        // Single short-answer
                        inputs[0].value = ans.responseText;
                        if (ans.responseText && ans.responseText.trim() !== '') {
                            isAnswered = true;
                        }
                    }
                }

                if (isAnswered) {
                    const qId = block.data('question-id');
                    $(`#nav-btn-${qId}`).removeClass('btn-outline-primary').addClass('btn-primary');
                }
            }
        });
    }

    $('#loadingSpinner').hide();
    $('#examWorkspace').show();

    // Start 5-minute Auto-Save interval
    startBatchAutoSave(300000); // 300,000 ms = 5 minutes
}

let pendingSaves = {};
let batchAutoSaveInterval;
let autoSaveDebounceTimer;

function savePendingBatch() {
    const questionIndicesToSave = Object.keys(pendingSaves);
    if (questionIndicesToSave.length === 0) {
        return Promise.resolve();
    }

    $('#autoSaveStatus').html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang đẩy lên máy chủ...').removeClass('text-success text-danger').addClass('text-warning');

    const submissionId = $('#SubmissionId').val();
    const bulkData = questionIndicesToSave.map(qIndex => {
        return {
            QuestionIndex: parseInt(qIndex),
            ResponseText: pendingSaves[qIndex].responseText
        };
    });

    return apiClient.post(`/api/student/exams/submission/${submissionId}/answers/batch`, bulkData)
        .then(() => {
            const now = new Date();
            const timeString = `lúc ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:${now.getSeconds().toString().padStart(2, '0')}`;
            $('#autoSaveStatus').html(`✅ Đã lưu tự động an toàn <strong>${timeString}</strong>.`).removeClass('text-warning text-danger text-muted').addClass('text-success');

            // Update UI and clear from buffer ONLY the ones we just saved
            questionIndicesToSave.forEach(qIndex => {
                if (pendingSaves[qIndex]) {
                    $(`#nav-btn-${pendingSaves[qIndex].questionId}`).removeClass('btn-warning btn-danger').addClass('btn-primary');
                    delete pendingSaves[qIndex];
                }
            });
        })
        .catch(err => {
            console.error('Batch Save error', err);
            $('#autoSaveStatus').html('❌ Lỗi kết nối! Hệ thống sẽ thử lại.').removeClass('text-success text-warning').addClass('text-danger');
            questionIndicesToSave.forEach(qIndex => {
                if (pendingSaves[qIndex]) {
                    $(`#nav-btn-${pendingSaves[qIndex].questionId}`).removeClass('btn-warning btn-primary').addClass('btn-danger');
                }
            });
            throw err;
        });
}

function startBatchAutoSave(intervalMs) {
    clearInterval(batchAutoSaveInterval);
    batchAutoSaveInterval = setInterval(() => {
        savePendingBatch().catch(() => { });
    }, intervalMs);
}

let currentQuestionIndex = 1;
let totalQuestions = 0;
let countdownTimerInterval;

function startCountdownTimer(totalSeconds) {
    clearInterval(countdownTimerInterval);
    let remaining = totalSeconds;

    function updateDisplay() {
        let m = Math.floor(remaining / 60);
        let s = remaining % 60;
        let pM = m < 10 ? '0' + m : m;
        let pS = s < 10 ? '0' + s : s;
        $('#countdownTimer').text(`${pM}:${pS}`);

        if (remaining <= 300) { // < 5 mins
            $('#countdownTimer').addClass('text-danger font-weight-bold');
        }
    }

    updateDisplay();

    countdownTimerInterval = setInterval(() => {
        remaining--;
        if (remaining <= 0) {
            clearInterval(countdownTimerInterval);
            $('#countdownTimer').text('00:00');
            Swal.fire({
                title: 'Hết giờ làm bài!',
                text: 'Hệ thống đang tự động thu bài của bạn.',
                icon: 'info',
                timer: 3000,
                showConfirmButton: false
            }).then(() => {
                forceSubmitExam();
            });
        } else {
            updateDisplay();
        }
    }, 1000);
}

function goToQuestion(targetIndex) {
    totalQuestions = $('.question-block').length;
    if (targetIndex < 1 || targetIndex > totalQuestions) return;

    // Hide current
    $(`#question-index-${currentQuestionIndex}`).hide();

    // Show new
    currentQuestionIndex = targetIndex;
    $(`#question-index-${currentQuestionIndex}`).fadeIn(200);

    // Update Prev/Next buttons
    $('#btn-prev-question').prop('disabled', currentQuestionIndex === 1);

    if (currentQuestionIndex === totalQuestions) {
        $('#btn-next-question').removeClass('btn-primary').addClass('btn-secondary').prop('disabled', true);
    } else {
        $('#btn-next-question').removeClass('btn-secondary').addClass('btn-primary').prop('disabled', false);
    }

    updateNavButtonsStyling();
}

function updateNavButtonsStyling() {
    $('.question-nav-btn').each(function () {
        const txt = parseInt($(this).text());
        if (txt === currentQuestionIndex) {
            $(this).addClass('active').css('border-width', '2px');
        } else {
            $(this).removeClass('active').css('border-width', '1px');
        }
    });
}

// Timeout holder for autosave debouncing (removed in favor of batch save)
function autoSaveAnswer(questionId, value, stepIndex = null) {
    const qBlock = $(`#question-index-${currentQuestionIndex}`);
    const qIndex = qBlock.data('question-index');

    let responseTextToSave = value;

    if (stepIndex !== null) {
        let stepAnswers = {};
        qBlock.find('.answer-field').each(function () {
            let sIdx = $(this).data('step');
            if (sIdx) {
                stepAnswers[`step${sIdx}`] = this.value;
            }
        });
        responseTextToSave = JSON.stringify(stepAnswers);
    }

    // Queue for batch save instead of sending immediately
    pendingSaves[qIndex] = {
        questionId: questionId,
        responseText: responseTextToSave
    };

    // Immediate Visual Feedback that changes are pending
    $(`#nav-btn-${questionId}`).removeClass('btn-outline-primary btn-primary btn-danger').addClass('btn-warning');
    $('#autoSaveStatus').html('⏳ Đã ghi nhận thay đổi (Đang đợi vài giây để đẩy lên máy chủ...)').removeClass('text-success text-danger text-muted').addClass('text-warning');

    // Mới cập nhật: Dùng debounce để tự động gửi dữ liệu lên server sau khi học viên ngưng thao tác 3 giây
    clearTimeout(autoSaveDebounceTimer);
    autoSaveDebounceTimer = setTimeout(() => {
        savePendingBatch().catch(() => { });
    }, 3000); // 3 giây
}

// Hàm AutoSave dành riêng cho dạng bài Inline Fill-in-the-blank 
function autoSaveFillInTheBlank(questionId, mathFieldElement) {
    const qBlock = $(`#question-index-${currentQuestionIndex}`);
    const qIndex = qBlock.data('question-index');

    let stepAnswers = [];

    // Tìm tất cả các math-field chứa placeholder của riêng câu hỏi này 
    // Do hệ thống render tách matrix block và text thành nhiều math-field nên gộp đáp án.
    const mathFields = qBlock.find(`math-field.answer-field[data-qid="${questionId}"]`);

    mathFields.each(function () {
        const mf = this;
        let promptIds = mf.getPrompts ? mf.getPrompts() : [];
        if (promptIds && promptIds.length > 0) {
            promptIds.forEach(id => {
                let val = mf.getPromptValue(id);
                stepAnswers.push(val);
            });
        } else {
            // Chữa cháy trường hợp thẻ MathLive không có promptID hoặc bị xóa mất bởi sinh viên
            let val = mf.value;
            if (val === '\\placeholder{}' || val === '\\placeholder') val = '';
            stepAnswers.push(val);
        }
    });

    let responseTextToSave = JSON.stringify(stepAnswers);

    // Queue for batch save
    pendingSaves[qIndex] = {
        questionId: questionId,
        responseText: responseTextToSave
    };

    $(`#nav-btn-${questionId}`).removeClass('btn-outline-primary btn-primary btn-danger').addClass('btn-warning');
    $('#autoSaveStatus').html('⏳ Đã ghi nhận thay đổi vào ô trống (Đang đợi...).').removeClass('text-success text-danger text-muted').addClass('text-warning');

    clearTimeout(autoSaveDebounceTimer);
    autoSaveDebounceTimer = setTimeout(() => {
        savePendingBatch().catch(() => { });
    }, 3000);
}

function highlightUnanswered() {
    let hasUnanswered = false;

    // Check all questions loaded in the DOM
    $('.question-block').each(function () {
        const qid = $(this).data('question-id');
        let isAnswered = false;

        // Find input inside this block (radio or math-field)
        const inputs = $(this).find('.answer-field');

        if (inputs.attr('type') === 'radio') {
            // Check if any radio is checked
            if ($(this).find('.answer-field:checked').length > 0) {
                isAnswered = true;
            }
        } else {
            // Check if math-field has value
            if (inputs.length === 1 && $(inputs[0]).data('is-fill') === true) {
                let mf = inputs[0];
                let pts = mf.getPrompts();
                // Chỉ cần ít nhất 1 ô trống có dữ liệu là coi như đang làm
                for (let i = 0; i < pts.length; i++) {
                    let val = mf.getPromptValue(pts[i]);
                    if (val && val.trim() !== '') {
                        isAnswered = true;
                        break;
                    }
                }
            } else {
                const mathVal = inputs.prop('value');
                if (mathVal && mathVal.trim() !== '') {
                    isAnswered = true;
                }
            }
        }

        if (!isAnswered) {
            $(this).css('border', '2px solid red');
            hasUnanswered = true;
        } else {
            $(this).css('border', 'none');
        }
    });

    if (hasUnanswered) {
        Swal.fire('Chú ý', 'Bạn còn câu hỏi chưa trả lời! Xin kiểm tra các ô màu đỏ trên màn hình.', 'warning');
    } else {
        Swal.fire('Tuyệt vời', 'Bạn đã điền đầy đủ các câu hỏi.', 'success');
    }
}

function flushPendingSaves() {
    return savePendingBatch().catch(err => {
        console.error('Lỗi khi lưu câu trả lời trước khi nộp:', err);
        throw err;
    });
}

function forceSubmitExam() {
    clearInterval(countdownTimerInterval); // Dừng đồng hồ đếm ngược
    const submissionId = $('#SubmissionId').val();
    flushPendingSaves().then(() => {
        apiClient.post(`/api/student/exams/submission/${submissionId}/submit`)
            .then(() => {
                window.location.href = '/Home/Index';
            })
            .catch(err => {
                console.error('Submit error:', err);
                window.location.href = '/Home/Index';
            });
    });
}

function submitExam() {
    const submissionId = $('#SubmissionId').val();
    Swal.fire({
        title: 'Xác nhận nộp bài',
        text: "Bạn có chắc chắn muốn nộp bài? Hành động này không thể hoàn tác.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Có, Nộp bài luôn!',
        cancelButtonText: 'Không nộp'
    }).then((result) => {
        if (result.isConfirmed) {
            clearInterval(countdownTimerInterval); // Dừng đồng hồ đếm ngược
            Swal.fire({
                title: 'Hệ thống đang thu bài...',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });
            flushPendingSaves().then(() => {
                apiClient.post(`/api/student/exams/submission/${submissionId}/submit`)
                    .then(() => {
                        if (examSignalRConnection) examSignalRConnection.stop();
                        Swal.fire('Thành công', 'Nộp bài thành công!', 'success').then(() => {
                            window.location.href = '/Home/Index'; // Redirect to Dashboard
                        });
                    })
                    .catch(err => {
                        console.error('Submit error:', err);
                        Swal.fire('Lỗi thao tác', 'Lỗi khi nộp bài: ' + err, 'error');
                    });
            }).catch(err => {
                Swal.fire('Gian đoạn thu bài lỗi', 'Không thể hoàn tất do mất kết nối', 'error');
            });
        }
    });
}

// ----------------------------------------------------------------------------
// Tích hợp Giám sát thời gian thực (Realtime Proctoring) qua SignalR
// ----------------------------------------------------------------------------
function initSignalRConnection(examId) {
    if (typeof signalR === 'undefined') {
        console.warn("SignalR library not loaded.");
        return;
    }

    const token = getToken();
    let studentId = 0;
    let studentName = "Học sinh";

    // Parse JWT để lấy thông tin Student gửi cho cơ sở dữ liệu giám sát
    if (token) {
        const payload = parseJwt(token);
        if (payload) {
            studentId = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || payload['nameid'] || payload.sub || 0;
            studentName = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || payload['name'] || payload.email || 'Học sinh';
            studentId = parseInt(studentId);
        }
    }

    examSignalRConnection = new signalR.HubConnectionBuilder()
        .withUrl(API_BASE_URL + "/examHub", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

    examSignalRConnection.start()
        .then(() => {
            console.log("🔥 Đã kết nối SignalR Giám sát bài thi!");
            // Gọi hàm trên server để đăng ký phiên làm bài
            examSignalRConnection.invoke("JoinExamGroup", parseInt(examId), studentId, studentName)
                .catch(err => console.error("Lỗi JoinExamGroup: " + err.toString()));
        })
        .catch(err => console.error("SignalR Connection Error: ", err));
}

// ----------------------------------------------------------------------------
// Tích hợp Chống gian lận Client-Side (Anti-Cheat Kit)
// ----------------------------------------------------------------------------
let cheatWarnings = 0;
const MAX_CHEAT_WARNINGS = 3;

function initAntiCheat() {
    // 1. Chặn chuột phải
    document.addEventListener("contextmenu", function (e) {
        e.preventDefault();
    });

    // 2. Chặn các hành động Copy, Cut, Paste
    document.addEventListener("copy", function (e) {
        e.preventDefault();
        Swal.fire('Hành động mờ ám', 'Tính năng Copy bị vô hiệu hóa trong phòng thi!', 'error');
    });
    document.addEventListener("cut", function (e) {
        e.preventDefault();
        Swal.fire('Hành động mờ ám', 'Tính năng Cut bị vô hiệu hóa trong phòng thi!', 'error');
    });
    document.addEventListener("paste", function (e) {
        e.preventDefault();
        Swal.fire('Hành động mờ ám', 'Tính năng Paste bị vô hiệu hóa trong phòng thi!', 'error');
    });

    // 3. Chặn phím F12 (Dev Tools) và các cụm phím tắt phổ biến
    document.addEventListener("keydown", function (e) {
        // F12
        if (e.key === "F12" || e.keyCode === 123) {
            e.preventDefault();
        }
        // Ctrl+Shift+I, Ctrl+Shift+J, Ctrl+Shift+C
        if (e.ctrlKey && e.shiftKey && (e.key === "I" || e.key === "J" || e.key === "C" || e.keyCode === 73 || e.keyCode === 74 || e.keyCode === 67)) {
            e.preventDefault();
        }
        // Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+P
        if (e.ctrlKey && (e.key === "c" || e.key === "C" || e.key === "v" || e.key === "V" || e.key === "x" || e.key === "X" || e.key === "p" || e.key === "P")) {
            e.preventDefault();
        }
    });

    // 4. Phát hiện chuyển Tab / Rời khỏi cửa sổ (Blur Event)
    let isForcingSubmit = false;
    window.addEventListener('blur', function () {
        // Nếu đang trong quá trình nộp cấm hiện thêm cảnh báo
        if (isForcingSubmit || !examSignalRConnection || $('#examWorkspace').is(':hidden')) return;

        // Khóa không cho đếm thêm sự kiện blur nếu đang hiện thông báo
        if (window.isAlerting) return;

        cheatWarnings++;

        if (cheatWarnings >= MAX_CHEAT_WARNINGS) {
            isForcingSubmit = true; // Block các sự kiện blur tiếp theo

            // Xóa đồng hồ đếm ngược và giấu bài thi
            clearInterval(countdownTimerInterval);
            $('#examWorkspace').hide();
            $('#questionNavMap').parent().hide();

            // Hiển thị thông báo KHÔNG THỂ TẮT cho sinh viên
            Swal.fire({
                title: 'CẢNH BÁO TỐI ĐA',
                html: '<span class="text-danger fw-bold">Thao tác gian lận vượt quá 3 lần!!</span><br/><br/>Hệ thống đang niêm phong dữ liệu và thu bài của bạn...',
                icon: 'error',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Ghi nhận lên hệ thống ngay lập tức
            const submissionId = $('#SubmissionId').val();
            // Cố gắng đẩy các đáp án còn lưu đọng lại lên server lần cuối
            flushPendingSaves().then(() => {
                // Đóng SignalR ngay lập tức
                if (examSignalRConnection) examSignalRConnection.stop();

                // Gửi lệnh nộp bài cuối cùng
                apiClient.post(`/api/student/exams/submission/${submissionId}/submit`)
                    .then(() => {
                        // Nộp thành công
                        Swal.fire({
                            title: 'BÀI THI ĐÃ BỊ THU',
                            text: 'Bài thi của bạn bị thu vì quá nhiều lần chuyển tab/cửa sổ. Điểm sẽ được ghi nhận dựa trên các câu đã lưu.',
                            icon: 'info',
                            allowOutsideClick: false,
                            confirmButtonText: 'Quay về trang chủ'
                        }).then(() => {
                            window.location.href = '/Home/Index';
                        });
                    })
                    .catch(err => {
                        // Kể cả lỗi API cũng văng ra màn hình ngoài vì giao diện trong nay đã bị hủy
                        console.error('Submit due to cheat error:', err);
                        window.location.href = '/Home/Index';
                    });
            }).catch(err => {
                // Nếu quá trình flush bị lỗi do rớt mạng, vẫn đá về trang chủ để tránh lách luật
                window.location.href = '/Home/Index';
            });

        } else {
            window.isAlerting = true;
            Swal.fire({
                title: `CẢNH BÁO VI PHẠM (${cheatWarnings}/${MAX_CHEAT_WARNINGS})`,
                text: `Bạn vừa rời khỏi màn hình làm bài! Hệ thống có ghi nhận hành vi này. Nếu vi phạm vượt mức ${MAX_CHEAT_WARNINGS} lần sẽ tự động thu bài.`,
                icon: 'warning',
                confirmButtonText: 'Tôi đã hiểu',
                willClose: () => {
                    // Đợi giao diện ổn định lại rồi mới nhả khóa
                    setTimeout(() => { window.isAlerting = false; }, 500);
                }
            });
        }
    });
}
