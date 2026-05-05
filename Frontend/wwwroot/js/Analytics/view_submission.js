// ═══════════════════════════════════════════
//  ViewSubmission — Giáo viên xem bài làm học sinh
// ═══════════════════════════════════════════

var DOM = {};

document.addEventListener("DOMContentLoaded", async function () {
    DOM = {
        root: document.getElementById("analyticsRoot"),
        loading: document.getElementById("analyticsLoading"),
        error: document.getElementById("analyticsError"),
        errorMsg: document.getElementById("errorMessage"),
        content: document.getElementById("analyticsContent"),
        title: document.getElementById("examTitle"),
        scoreSection: document.getElementById("scoreSection"),
        recBox: document.getElementById("recommendationsBox"),
        reviewBox: document.getElementById("answerReviewBox"),
        tplRec: document.getElementById("tpl-rec-item"),
        tplQuestion: document.getElementById("tpl-review-question"),
        tplOption: document.getElementById("tpl-review-option")
    };

    await window.userReady;

    if (!isAuthenticated()) { window.location.href = '/Auth/Login'; return; }
    if (getUserRole() !== RoleIds.Teacher) {
        showError("Bạn không có quyền truy cập. Chỉ Giáo viên mới được xem bài làm.");
        return;
    }

    var submissionId = DOM.root && DOM.root.dataset.submissionId;
    if (!submissionId) { showError("Không tìm thấy mã bài làm."); return; }

    var examId = DOM.root.dataset.examId;
    var classId = DOM.root.dataset.classId;
    var backLink = document.getElementById("backToResults");
    if (backLink && examId) {
        backLink.href = "/Analytics/ExamSubmitResults?examId=" + examId + (classId ? "&classId=" + classId : "");
    } else if (backLink) {
        backLink.href = "/Class/ClassList";
    }

    apiClient.get("/api/analytics/submission/" + submissionId)
        .then(renderSubmissionResult)
        .catch(function (err) { showError(err.message || "Lỗi không xác định."); });
});

function getCSSColor(varName, alpha) {
    var val = getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
    if (val.startsWith('hsl')) return val.replace('hsl(', 'hsla(').replace(')', ',' + (alpha || 1) + ')');
    if (val.startsWith('rgb')) return val.replace('rgb(', 'rgba(').replace(')', ',' + (alpha || 1) + ')');
    return val;
}

function showError(msg) {
    DOM.loading.hidden = true;
    DOM.error.hidden = false;
    if (DOM.errorMsg) DOM.errorMsg.textContent = msg;
}

function renderSubmissionResult(data) {
    DOM.loading.hidden = true;
    DOM.content.hidden = false;
    DOM.title.textContent = "Kết quả: " + (data.examTitle || data.ExamTitle || "N/A");

    var showScore = data.showScore !== false;
    var showAnswer = data.showAnswer !== false;

    if (showScore) {
        DOM.scoreSection.hidden = false;
        fillScoreCards(data);
        renderComparisonChart(data);
        renderRadar("chapterRadar", data.chapterStats || data.ChapterStats, "chapterName", "accuracyRate", "--clr-success");
        renderRadar("difficultyRadar", data.difficultyStats || data.DifficultyStats, "difficultyName", "accuracyRate", "--clr-warning");
        renderRecList(data.recommendations || data.Recommendations);
    }

    renderAnswerReview(data.answerReview || data.AnswerReview, showAnswer);
    if (typeof MathLive !== "undefined") MathLive.renderMathInDocument();
}

function fillScoreCards(data) {
    var map = {
        totalPoints: data.totalPoints != null ? data.totalPoints : (data.TotalPoints ?? "—"),
        miniScore: data.totalPoints != null ? data.totalPoints : (data.TotalPoints ?? "—"),
        correctCount: data.correctCount ?? data.CorrectCount ?? 0,
        miniCorrect: data.correctCount ?? data.CorrectCount ?? 0,
        wrongCount: data.wrongCount ?? data.WrongCount ?? 0,
        totalQuestions: data.totalQuestions ?? data.TotalQuestions ?? 0,
        classAvgLabel: data.classAverageScore != null ? data.classAverageScore : (data.ClassAverageScore ?? "—"),
        classMaxLabel: data.classMaxScore != null ? data.classMaxScore : (data.ClassMaxScore ?? "—")
    };
    Object.keys(map).forEach(function (id) {
        var el = document.getElementById(id);
        if (el) el.textContent = map[id];
    });
}

function renderComparisonChart(data) {
    var totalPoints = data.totalPoints ?? data.TotalPoints;
    var classAvg = data.classAverageScore ?? data.ClassAverageScore;
    if (totalPoints == null || classAvg == null) return;
    var ctx = document.getElementById("comparisonChart");
    if (!ctx) return;
    ctx = ctx.getContext("2d");
    new Chart(ctx, {
        type: "bar",
        data: {
            labels: ["Học sinh", "TB Lớp", "Top 1"],
            datasets: [{
                data: [totalPoints, classAvg, data.classMaxScore || data.ClassMaxScore || 10],
                backgroundColor: [getCSSColor("--clr-primary", 0.8), getCSSColor("--clr-info", 0.2), getCSSColor("--clr-success", 0.2)],
                borderRadius: 8,
                barThickness: 20
            }]
        },
        options: {
            responsive: true,
            indexAxis: "y",
            plugins: { legend: { display: false } },
            scales: {
                x: { beginAtZero: true, grid: { display: false }, ticks: { font: { weight: '600' } } },
                y: { grid: { display: false }, ticks: { font: { weight: '600' } } }
            }
        }
    });
}

function renderRadar(canvasId, stats, labelKey, valueKey, colorVar) {
    if (!stats || stats.length === 0) return;
    var canvas = document.getElementById(canvasId);
    if (!canvas) return;
    var ctx = canvas.getContext("2d");
    var labels = stats.map(function (s) { return s[labelKey] || s[labelKey.charAt(0).toUpperCase() + labelKey.slice(1)]; });
    var values = stats.map(function (s) { return s[valueKey] || s[valueKey.charAt(0).toUpperCase() + valueKey.slice(1)]; });
    new Chart(ctx, {
        type: "radar",
        data: {
            labels: labels,
            datasets: [{
                label: "Tỉ lệ đúng (%)",
                data: values,
                backgroundColor: getCSSColor(colorVar, 0.15),
                borderColor: getCSSColor(colorVar, 1),
                borderWidth: 3,
                pointBackgroundColor: "#fff",
                pointBorderColor: getCSSColor(colorVar, 1),
                pointBorderWidth: 2,
                pointRadius: 4,
                fill: true,
                tension: 0.2
            }]
        },
        options: {
            responsive: true,
            scales: {
                r: {
                    beginAtZero: true,
                    max: 100,
                    ticks: { display: false, stepSize: 20 },
                    grid: { color: "#f1f5f9" },
                    angleLines: { color: "#f1f5f9" },
                    pointLabels: { font: { size: 11, weight: '600' }, color: "#64748b" }
                }
            },
            plugins: { legend: { display: false } }
        }
    });
}

function renderRecList(recs) {
    DOM.recBox.innerHTML = "";
    if (!recs || recs.length === 0) {
        var emptyDiv = document.createElement("div");
        emptyDiv.className = "text-center py-4 text-muted small";
        emptyDiv.textContent = "Không có đề xuất.";
        DOM.recBox.appendChild(emptyDiv);
        return;
    }
    recs.forEach(function (rec) {
        var item = DOM.tplRec.content.cloneNode(true);
        var div = item.querySelector(".rec-item");
        
        var iconStr = (rec + "").includes("🚨") ? "bi-patch-exclamation-fill" : ((rec + "").includes("⚠️") ? "bi-exclamation-triangle-fill" : "bi-stars");
        var icon = document.createElement("i");
        icon.className = "bi " + iconStr;
        
        var span = document.createElement("span");
        span.textContent = rec;
        
        div.innerHTML = "";
        div.appendChild(icon);
        div.appendChild(span);
        
        div.classList.add((rec + "").includes("🚨") ? "rec-danger" : ((rec + "").includes("⚠️") ? "rec-warning" : ((rec + "").includes("🌟") ? "rec-success" : "rec-info")));
        DOM.recBox.appendChild(item);
    });
}

function renderAnswerReview(answers, showAnswer) {
    DOM.reviewBox.innerHTML = "";
    if (!answers || answers.length === 0) { DOM.reviewBox.textContent = "Không có dữ liệu bài làm."; return; }
    answers.forEach(function (q) {
        var opts = q.options || q.Options || [];
        var qBlock = DOM.tplQuestion.content.cloneNode(true);
        var container = qBlock.querySelector(".answer-review-item");
        if (showAnswer) container.classList.add(checkQuestionCorrect(opts) ? "correct" : "wrong");
        container.querySelector("[data-field='order']").textContent = q.questionOrder || q.QuestionOrder;
        var stemEl = container.querySelector("[data-field='content']");
        stemEl.textContent = parseQuestionContent(q.questionContent || q.QuestionContent);
        if (typeof stemEl.render === "function") stemEl.render();
        var chapterEl = container.querySelector("[data-field='chapter']");
        var ch = q.chapterName || q.ChapterName;
        if (ch) chapterEl.textContent = "[" + ch + "]";
        else chapterEl.remove();
        var optSlot = container.querySelector("[data-slot='options']");
        opts.forEach(function (opt) { optSlot.appendChild(buildOptionRow(opt, q.questionType || q.QuestionType, showAnswer)); });
        DOM.reviewBox.appendChild(qBlock);
    });
}

function buildOptionRow(opt, questionType, showAnswer) {
    var optEl = DOM.tplOption.content.cloneNode(true);
    var row = optEl.querySelector(".option-row");
    var isSelected = opt.isSelected !== undefined ? opt.isSelected : opt.IsSelected;
    var isCorrect = opt.isCorrect !== undefined ? opt.isCorrect : opt.IsCorrect;
    if (isSelected) row.classList.add("selected");
    if (showAnswer && isCorrect === true) row.classList.add("correct-answer");
    if (showAnswer && isSelected && isCorrect === false) row.classList.add("wrong-answer");
    var icon = row.querySelector("[data-field='icon']");
    icon.textContent = showAnswer ? (isCorrect === true ? "✓" : (isSelected ? "✗" : "○")) : (isSelected ? "●" : "○");
    icon.className = "option-icon " + (showAnswer ? (isCorrect === true ? "text-success" : (isSelected ? "text-danger" : "")) : (isSelected ? "text-primary" : ""));
    var contentEl = row.querySelector("[data-field='content']");
    contentEl.textContent = opt.content || opt.Content;
    if (typeof contentEl.render === "function") contentEl.render();
    var responseEl = row.querySelector("[data-field='response']");
    var resp = opt.studentResponse || opt.StudentResponse;
    if (questionType === "FillInBlank" && resp) responseEl.textContent = "Đã điền: " + resp;
    else responseEl.remove();
    var correctEl = row.querySelector("[data-field='correct']");
    var correctAns = opt.correctAnswer || opt.CorrectAnswer;
    if (showAnswer && correctAns && questionType === "FillInBlank") correctEl.textContent = "(Đáp án: " + correctAns + ")";
    else correctEl.remove();
    return optEl;
}

function checkQuestionCorrect(options) {
    for (var i = 0; i < options.length; i++) {
        var opt = options[i];
        var sel = opt.isSelected !== undefined ? opt.isSelected : opt.IsSelected;
        var cor = opt.isCorrect !== undefined ? opt.isCorrect : opt.IsCorrect;
        if (sel && cor === false) return false;
        if (!sel && cor === true) return false;
    }
    return true;
}

function parseQuestionContent(content) {
    try { return JSON.parse(content).stem || content; } catch (e) { return content || ""; }
}
