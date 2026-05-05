// ═══════════════════════════════════════════
//  StudentResult — Học sinh
// ═══════════════════════════════════════════

var DOM = {};

document.addEventListener("DOMContentLoaded", async function () {
    // ── Cache DOM 1 lần duy nhất ──
    DOM = {
        root:       document.getElementById("analyticsRoot"),
        loading:    document.getElementById("analyticsLoading"),
        error:      document.getElementById("analyticsError"),
        errorMsg:   document.getElementById("errorMessage"),
        content:    document.getElementById("analyticsContent"),
        title:      document.getElementById("examTitle"),
        scoreSection: document.getElementById("scoreSection"),
        noScoreNotice: document.getElementById("noScoreNotice"),
        recBox:     document.getElementById("recommendationsBox"),
        reviewBox:  document.getElementById("answerReviewBox"),
        tplRec:     document.getElementById("tpl-rec-item"),
        tplQuestion:document.getElementById("tpl-review-question"),
        tplOption:  document.getElementById("tpl-review-option")
    };

    await window.userReady;

    // ── Kiểm tra đăng nhập & role ──
    if (!isAuthenticated()) { window.location.href = '/Auth/Login'; return; }
    if (getUserRole() !== RoleIds.Student) {
        showError("Bạn không có quyền truy cập. Chỉ Học sinh mới được xem kết quả bài làm.");
        return;
    }

    var examId = DOM.root.dataset.examId;
    if (!examId) { showError("Không tìm thấy mã bài thi."); return; }

    apiClient.get("/api/analytics/exam/" + examId + "/student")
        .then(renderStudentResult)
        .catch(function (err) { showError(err.message || "Lỗi không xác định."); });
});

// ═══════════════════════════════════════════
//  Helper: CSS variable → Chart.js color
// ═══════════════════════════════════════════
function getCSSColor(varName, alpha) {
    var val = getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
    if (val.startsWith('hsl')) {
        return val.replace('hsl(', 'hsla(').replace(')', ',' + (alpha || 1) + ')');
    }
    if (val.startsWith('rgb')) {
        return val.replace('rgb(', 'rgba(').replace(')', ',' + (alpha || 1) + ')');
    }
    return val;
}

function showError(msg) {
    DOM.loading.hidden = true;
    DOM.error.hidden = false;
    DOM.errorMsg.textContent = msg;
}

// ═══════════════════════════════════════════
//  Render chính
// ═══════════════════════════════════════════
function renderStudentResult(data) {
    DOM.loading.hidden = true;
    DOM.content.hidden = false;
    DOM.title.textContent = "Kết quả: " + (data.examTitle || "N/A");

    if (data.showScore) {
        DOM.scoreSection.hidden = false;
        fillScoreCards(data);
        renderComparisonChart(data);
        renderRadar("chapterRadar", data.chapterStats, "chapterName", "accuracyRate", "--clr-success");
        renderRadar("difficultyRadar", data.difficultyStats, "difficultyName", "accuracyRate", "--clr-warning");
        renderRecList(data.recommendations);
    } else {
        DOM.noScoreNotice.hidden = false;
    }

    renderAnswerReview(data.answerReview, data.showAnswer);

    // Render LaTeX sau khi toàn bộ DOM đã sẵn sàng
    renderMath(DOM.content);
}

// ═══════════════════════════════════════════
//  Điểm — batch update
// ═══════════════════════════════════════════
function fillScoreCards(data) {
    var map = {
        totalPoints:  data.totalPoints != null ? data.totalPoints : "—",
        miniScore:    data.totalPoints != null ? data.totalPoints : "—",
        correctCount: data.correctCount || 0,
        miniCorrect:  data.correctCount || 0,
        wrongCount:   data.wrongCount || 0,
        totalQuestions: data.totalQuestions || 0,
        classAvgLabel: data.classAverageScore != null ? data.classAverageScore : "—",
        classMaxLabel: data.classMaxScore != null ? data.classMaxScore : "—"
    };
    Object.keys(map).forEach(function (id) {
        var el = document.getElementById(id);
        if (el) el.textContent = map[id];
    });
}

// ═══════════════════════════════════════════
//  Biểu đồ — Modern Styling
// ═══════════════════════════════════════════
function renderComparisonChart(data) {
    if (data.totalPoints == null || data.classAverageScore == null) return;
    var ctx = document.getElementById("comparisonChart").getContext("2d");

    new Chart(ctx, {
        type: "bar",
        data: {
            labels: ["Em", "TB Lớp", "Top 1"],
            datasets: [{
                data: [data.totalPoints, data.classAverageScore, data.classMaxScore || 10],
                backgroundColor: [
                    getCSSColor("--clr-primary", 0.8),
                    getCSSColor("--clr-info", 0.2),
                    getCSSColor("--clr-success", 0.2)
                ],
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
    var ctx = document.getElementById(canvasId).getContext("2d");

    new Chart(ctx, {
        type: "radar",
        data: {
            labels: stats.map(function (s) { return s[labelKey]; }),
            datasets: [{
                label: "Tỉ lệ đúng (%)",
                data: stats.map(function (s) { return s[valueKey]; }),
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

// ═══════════════════════════════════════════
//  Đề xuất
// ═══════════════════════════════════════════
function renderRecList(recs) {
    DOM.recBox.innerHTML = "";
    if (!recs || recs.length === 0) {
        var emptyDiv = document.createElement("div");
        emptyDiv.className = "text-center py-4 text-muted small";
        emptyDiv.textContent = "Cần thêm dữ liệu để hệ thống đưa ra lời khuyên cá nhân hóa.";
        DOM.recBox.appendChild(emptyDiv);
        return;
    }
    recs.forEach(function (rec) {
        var item = DOM.tplRec.content.cloneNode(true);
        var div = item.querySelector(".rec-item");

        // Thêm icon trang trí
        var iconStr = rec.includes("🚨") ? "bi-patch-exclamation-fill" :
                      (rec.includes("⚠️") ? "bi-exclamation-triangle-fill" : "bi-stars");

        var icon = document.createElement("i");
        icon.className = "bi " + iconStr;
        var span = document.createElement("span");
        span.textContent = rec;
        
        div.innerHTML = "";
        div.appendChild(icon);
        div.appendChild(span);
        div.classList.add(getRecClass(rec));
        DOM.recBox.appendChild(item);
    });
}

// ═══════════════════════════════════════════
//  Xem lại bài làm
// ═══════════════════════════════════════════
function renderAnswerReview(answers, showAnswer) {
    DOM.reviewBox.innerHTML = "";
    if (!answers || answers.length === 0) { DOM.reviewBox.textContent = "Không có dữ liệu bài làm."; return; }

    answers.forEach(function (q) {
        var qBlock = DOM.tplQuestion.content.cloneNode(true);
        var container = qBlock.querySelector(".answer-review-item");

        if (showAnswer) {
            container.classList.add(checkQuestionCorrect(q.options) ? "correct" : "wrong");
        }

        container.querySelector("[data-field='order']").textContent = q.questionOrder;
        var stemEl = container.querySelector("[data-field='content']");
        stemEl.textContent = parseQuestionContent(q.questionContent);
        if (typeof stemEl.render === "function") stemEl.render();

        var chapterEl = container.querySelector("[data-field='chapter']");
        if (q.chapterName) { chapterEl.textContent = "[" + q.chapterName + "]"; }
        else { chapterEl.remove(); }

        var optSlot = container.querySelector("[data-slot='options']");
        q.options.forEach(function (opt) { optSlot.appendChild(buildOptionRow(opt, q.questionType, showAnswer)); });

        DOM.reviewBox.appendChild(qBlock);
    });
}

function buildOptionRow(opt, questionType, showAnswer) {
    var optEl = DOM.tplOption.content.cloneNode(true);
    var row = optEl.querySelector(".option-row");

    if (opt.isSelected) row.classList.add("selected");
    if (showAnswer && opt.isCorrect === true) row.classList.add("correct-answer");
    if (showAnswer && opt.isSelected && opt.isCorrect === false) row.classList.add("wrong-answer");

    var icon = row.querySelector("[data-field='icon']");
    icon.textContent = getOptionIcon(opt, showAnswer);
    icon.className = "option-icon " + getOptionIconClass(opt, showAnswer);

    var contentEl = row.querySelector("[data-field='content']");
    contentEl.textContent = opt.content;
    if (typeof contentEl.render === "function") contentEl.render();

    var responseEl = row.querySelector("[data-field='response']");
    if (questionType === "FillInBlank" && opt.studentResponse) {
        responseEl.textContent = "Đã điền: " + opt.studentResponse;
    } else { responseEl.remove(); }

    var correctEl = row.querySelector("[data-field='correct']");
    if (showAnswer && opt.correctAnswer && questionType === "FillInBlank") {
        correctEl.textContent = "(Đáp án: " + opt.correctAnswer + ")";
    } else { correctEl.remove(); }

    return optEl;
}

// ═══════════════════════════════════════════
//  Utility
// ═══════════════════════════════════════════
function checkQuestionCorrect(options) {
    for (var i = 0; i < options.length; i++) {
        if (options[i].isSelected && options[i].isCorrect === false) return false;
        if (!options[i].isSelected && options[i].isCorrect === true) return false;
    }
    return true;
}

function sanitizeLatex(str) {
    return str; // Không cần thiết khi dùng math-span
}

function renderMath(container) {
    // MathLive tự tìm và render LaTeX trong toàn document
    if (typeof MathLive !== "undefined") {
        MathLive.renderMathInDocument();
    } else {
        // MathLive đang defer load — chờ rồi render
        window.addEventListener("load", function () {
            if (typeof MathLive !== "undefined") MathLive.renderMathInDocument();
        });
    }
}

function parseQuestionContent(content) {
    try { return JSON.parse(content).stem || content; } catch (e) { return content; }
}

function getOptionIcon(opt, showAnswer) {
    if (showAnswer) return opt.isCorrect === true ? "✓" : (opt.isSelected ? "✗" : "○");
    return opt.isSelected ? "●" : "○";
}

function getOptionIconClass(opt, showAnswer) {
    if (showAnswer) return opt.isCorrect === true ? "text-success" : (opt.isSelected ? "text-danger" : "");
    return opt.isSelected ? "text-primary" : "";
}

function getRecClass(rec) {
    if (rec.includes("🚨")) return "rec-danger";
    if (rec.includes("⚠️")) return "rec-warning";
    if (rec.includes("🌟")) return "rec-success";
    return "rec-info";
}
