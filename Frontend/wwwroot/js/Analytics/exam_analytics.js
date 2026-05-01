// ═══════════════════════════════════════════
//  ExamAnalytics — Giáo viên
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
        submissions:document.getElementById("totalSubmissions"),
        recBox:     document.getElementById("recommendationsBox"),
        tplHardest: document.getElementById("tpl-hardest-row"),
        tplStudent: document.getElementById("tpl-student-row"),
        tplRec:     document.getElementById("tpl-rec-item")
    };

    await window.userReady;

    // ── Kiểm tra đăng nhập & role ──
    if (!isAuthenticated()) { window.location.href = '/Auth/Login'; return; }
    if (getUserRole() !== RoleIds.Teacher) { showError("Bạn không có quyền truy cập. Chỉ Giáo viên mới được xem phân tích bài thi."); return; }

    var examId = DOM.root.dataset.examId;
    if (!examId) { showError("Không tìm thấy mã bài thi."); return; }

    apiClient.get("/api/analytics/exam/" + examId + "/detail")
        .then(renderAnalytics)
        .catch(function (err) { showError(err.message || "Lỗi không xác định."); });
});

// ═══════════════════════════════════════════
//  Helper: đọc CSS variable → Chart.js color
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
function renderAnalytics(rawData) {
    console.log("Analytics Data received:", rawData); // DEBUG
    if (!rawData) { showError("Hệ thống trả về dữ liệu rỗng."); return; }

    // Đồng bộ hóa case-sensitivity
    var data = {
        examTitle:          rawData.examTitle || rawData.ExamTitle || "N/A",
        totalSubmissions:   rawData.totalSubmissions != null ? rawData.totalSubmissions : (rawData.TotalSubmissions || 0),
        averageScore:       rawData.averageScore || rawData.AverageScore || 0,
        maxScore:           rawData.maxScore || rawData.MaxScore || 0,
        minScore:           rawData.minScore || rawData.MinScore || 0,
        medianScore:        rawData.medianScore || rawData.MedianScore || 0,
        scoreDistribution:  rawData.scoreDistribution || rawData.ScoreDistribution,
        chapterStats:       rawData.chapterStats || rawData.ChapterStats || [],
        difficultyStats:    rawData.difficultyStats || rawData.DifficultyStats || [],
        hardestQuestions:   rawData.hardestQuestions || rawData.HardestQuestions || [],
        studentResults:     rawData.studentResults || rawData.StudentResults || [],
        recommendations:    rawData.recommendations || rawData.Recommendations || []
    };

    DOM.loading.hidden = true;
    DOM.content.hidden = false;

    // Cập nhật giao diện
    DOM.title.textContent = "Phân tích: " + data.examTitle;
    DOM.submissions.textContent = data.totalSubmissions;

    if (data.totalSubmissions === 0) {
        var alertDiv = document.createElement("div");
        alertDiv.className = "alert alert-info";
        alertDiv.textContent = "Chưa có dữ liệu bài làm để phân tích năng lực.";
        DOM.recBox.innerHTML = "";
        DOM.recBox.appendChild(alertDiv);
        return;
    }

    // Stat cards
    var statMap = { 
        totalSubmissions: data.totalSubmissions,
        avgScore:         data.averageScore.toFixed(2), 
        avgScoreQuick:    data.averageScore.toFixed(2),
        maxScore:         data.maxScore, 
        minScore:         data.minScore, 
        medianScore:      data.medianScore 
    };

    Object.keys(statMap).forEach(function (id) { 
        var el = document.getElementById(id);
        if (el) el.textContent = statMap[id];
    });

    renderScoreDistChart(data.scoreDistribution);
    renderAccuracyChart("chapterChart", data.chapterStats, "chapterName", "accuracyRate");
    renderAccuracyChart("difficultyChart", data.difficultyStats, "difficultyName", "accuracyRate");
    
    renderTable("#hardestTable tbody", DOM.tplHardest, data.hardestQuestions, fillHardestRow);
    renderTable("#studentTable tbody", DOM.tplStudent, data.studentResults, fillStudentRow);
    renderRecList(data.recommendations, DOM.recBox, DOM.tplRec);

    renderMath(DOM.content);
}

function getSafeVal(obj, keys) {
    for (var i = 0; i < keys.length; i++) {
        if (obj[keys[i]] !== undefined) return obj[keys[i]];
    }
    return null;
}

// ═══════════════════════════════════════════
//  Biểu đồ — màu lấy từ CSS variable
// ═══════════════════════════════════════════
function renderScoreDistChart(distribution) {
    var canvas = document.getElementById("scoreDistChart");
    if (!canvas) return;
    var ctx = canvas.getContext("2d");

    if (!distribution || Object.keys(distribution).length === 0) {
        ctx.font = "14px Inter";
        ctx.fillStyle = "#94a3b8";
        ctx.textAlign = "center";
        ctx.fillText("Chưa có dữ liệu phổ điểm", canvas.width / 2, canvas.height / 2);
        return;
    }

    var labels = Object.keys(distribution);
    var values = Object.values(distribution).map(v => v || 0);

    new Chart(ctx, {
        type: "bar",
        data: {
            labels: labels,
            datasets: [{
                label: "Số học sinh",
                data: values,
                backgroundColor: getCSSColor("--clr-primary", 0.7),
                borderRadius: 6,
                barThickness: 30
            }]
        },
        options: { 
            responsive: true, 
            plugins: { legend: { display: false } }, 
            scales: { 
                y: { beginAtZero: true, ticks: { stepSize: 1, font: { weight: '600' } }, grid: { color: "#f1f5f9" } },
                x: { grid: { display: false }, ticks: { font: { weight: '600' } } }
            } 
        }
    });
}

function renderAccuracyChart(canvasId, stats, labelKey, valueKey) {
    var canvas = document.getElementById(canvasId);
    if (!canvas) return;
    var ctx = canvas.getContext("2d");

    if (!stats || stats.length === 0) {
        ctx.font = "14px Inter";
        ctx.fillStyle = "#94a3b8";
        ctx.textAlign = "center";
        ctx.fillText("Chưa có dữ liệu phân tích", canvas.width / 2, canvas.height / 2);
        return;
    }
    
    // Thử lấy key (hỗ trợ cả PascalCase trong mảng)
    var labels = stats.map(function (s) { return s[labelKey] || s[labelKey.charAt(0).toUpperCase() + labelKey.slice(1)]; });
    var values = stats.map(function (s) { return s[valueKey] || s[valueKey.charAt(0).toUpperCase() + valueKey.slice(1)] || 0; });
    
    var colors = values.map(function (v) {
        return v < 40 ? getCSSColor("--clr-danger", 0.7) : (v < 70 ? getCSSColor("--clr-warning", 0.7) : getCSSColor("--clr-success", 0.7));
    });
    new Chart(ctx, {
        type: "bar",
        data: {
            labels: labels,
            datasets: [{ 
                label: "Tỉ lệ đúng (%)", 
                data: values, 
                backgroundColor: colors, 
                borderRadius: 6,
                barThickness: 15
            }]
        },
        options: { 
            responsive: true, 
            indexAxis: 'y',
            plugins: { legend: { display: false } }, 
            scales: { 
                x: { beginAtZero: true, max: 100, grid: { color: "#f1f5f9" }, ticks: { font: { weight: '600' } } },
                y: { grid: { display: false }, ticks: { font: { weight: '600' } } }
            } 
        }
    });
}

// ═══════════════════════════════════════════
//  Bảng — clone template + fill callback
// ═══════════════════════════════════════════
function renderTable(tbodySelector, template, items, fillFn) {
    if (!items || items.length === 0) return;
    var tbody = document.querySelector(tbodySelector);
    tbody.innerHTML = "";
    items.forEach(function (item, i) {
        var row = template.content.cloneNode(true);
        fillFn(row, item, i);
        tbody.appendChild(row);
    });
}

function fillHardestRow(row, q, i) {
    row.querySelector("[data-col='index']").textContent = i + 1;
    var contentEl = row.querySelector("[data-col='content']");
    var content = q.questionContent || q.QuestionContent || "";
    contentEl.textContent = truncateText(content, 80);
    if (typeof contentEl.render === "function") contentEl.render();
    row.querySelector("[data-col='chapter']").textContent = q.chapterName || q.ChapterName;
    row.querySelector("[data-col='difficulty']").textContent = q.difficultyName || q.DifficultyName;
    var badge = row.querySelector("[data-col='accuracy']");
    var acc = q.accuracyRate || q.AccuracyRate || 0;
    badge.textContent = acc + "%";
    badge.classList.add(getScoreClass(acc));
}

function fillStudentRow(row, s, i) {
    row.querySelector("[data-col='index']").textContent = i + 1;
    row.querySelector("[data-col='name']").textContent = s.studentName;
    var badge = row.querySelector("[data-col='score']");
    badge.textContent = s.totalPoints != null ? s.totalPoints : "—";
    badge.classList.add(getScoreClass(s.totalPoints));
    row.querySelector("[data-col='date']").textContent = formatDateVN(s.submittedAt);
}

// ═══════════════════════════════════════════
//  Đề xuất — clone template
// ═══════════════════════════════════════════
function renderRecList(recs, box, template) {
    box.innerHTML = "";
    if (!recs || recs.length === 0) {
        var emptyDiv = document.createElement("div");
        emptyDiv.className = "text-center py-4 text-muted small";
        emptyDiv.textContent = "Cần thêm dữ liệu để hệ thống đưa ra lời khuyên.";
        box.appendChild(emptyDiv);
        return;
    }
    recs.forEach(function (rec) {
        var item = template.content.cloneNode(true);
        var div = item.querySelector(".rec-item");

        var iconStr = (rec.includes("🚨") || rec.includes("CẢNH BÁO")) ? "bi-patch-exclamation-fill" :
                      ((rec.includes("⚠️") || rec.includes("cần")) ? "bi-exclamation-triangle-fill" : "bi-stars");

        var icon = document.createElement("i");
        icon.className = "bi " + iconStr;
        var span = document.createElement("span");
        span.textContent = rec;

        div.innerHTML = ""; // Clear existing
        div.appendChild(icon);
        div.appendChild(span);
        div.classList.add(getRecClass(rec));
        box.appendChild(item);
    });
}

// ═══════════════════════════════════════════
//  Utility
// ═══════════════════════════════════════════
function getScoreClass(v) {
    if (v == null) return "score-bad";
    return v >= 70 || v >= 8 ? "score-good" : (v >= 40 || v >= 5 ? "score-medium" : "score-bad");
}

function getRecClass(rec) {
    if (rec.includes("🚨") || rec.includes("CẢNH BÁO")) return "rec-danger";
    if (rec.includes("⚠️") || rec.includes("cần")) return "rec-warning";
    if (rec.includes("🌟") || rec.includes("vững")) return "rec-success";
    return "rec-info";
}

function truncateText(str, maxLen) {
    if (!str) return "";
    try { str = JSON.parse(str).stem || str; } catch (e) { }
    return str.length > maxLen ? str.substring(0, maxLen) + "..." : str;
}

function formatDateVN(dateStr) {
    if (!dateStr) return "—";
    var d = new Date(dateStr);
    return d.toLocaleDateString("vi-VN") + " " + d.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
}

function sanitizeLatex(str) {
    return str;
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
