// ═══════════════════════════════════════════
//  ExamSubmitResults — Thống kê nộp bài (Giáo viên)
// ═══════════════════════════════════════════

(function () {
    var root, examId, classId, allStudents = [], filteredStudents = [], apiData = {};
    var currentPage = 1;
    var PAGE_SIZE = 15;

    function init() {
        root = document.getElementById("submitResultsRoot");
        if (!root) return;

        examId = root.dataset.examId;
        classId = root.dataset.classId || null;

        if (!examId || examId === "0") {
            showError("Không tìm thấy mã bài thi.");
            return;
        }

        if (!isAuthenticated()) {
            window.location.href = "/Auth/Login";
            return;
        }
        if (getUserRole() !== "Teacher" && getUserRole() !== "Giáo viên") {
            showError("Bạn không có quyền truy cập. Chỉ Giáo viên mới được xem thống kê nộp bài.");
            return;
        }

        setupBackLink();
        loadData();
        bindFilters();
    }

    function setupBackLink() {
        var backLink = document.getElementById("backLink");
        if (backLink) {
            backLink.href = classId ? "/Course/ExamListInCourse/" + classId : "/Course/CourseList";
        }
    }

    function showError(msg) {
        document.getElementById("loadingState").hidden = true;
        document.getElementById("errorState").hidden = false;
        var el = document.getElementById("errorMessage");
        if (el) el.textContent = msg;
    }

    function showContent() {
        document.getElementById("loadingState").hidden = true;
        document.getElementById("errorState").hidden = true;
        document.getElementById("contentArea").hidden = false;
    }

    function loadData() {
        apiClient.get("/api/analytics/exam/" + examId + "/submissions")
            .then(function (data) {
                apiData = data;
                allStudents = data.students || data.Students || [];
                filteredStudents = allStudents.slice();

                showContent();
                renderHeader(data);
                renderTable();
                updatePagination();
            })
            .catch(function (err) {
                showError(err.message || "Lỗi không xác định khi tải dữ liệu.");
            });
    }

    function renderHeader(data) {
        var d = data || {};
        var title = d.examTitle || d.ExamTitle || "Kết quả thi";
        var className = d.className || d.ClassName || "";
        var duration = d.durationMinutes ?? d.DurationMinutes ?? 0;
        var maxAttempts = d.maxAttempts ?? d.MaxAttempts ?? 1;
        var total = d.totalStudents ?? d.TotalStudents ?? 0;
        var submitted = d.submittedCount ?? d.SubmittedCount ?? 0;

        var titleEl = document.getElementById("examTitle");
        if (titleEl) titleEl.textContent = "Kết quả thi: " + title;

        var classInfo = document.getElementById("classInfo");
        if (classInfo) classInfo.innerHTML = className ? "<strong>Lớp:</strong> " + className : "";

        var durationEl = document.getElementById("durationInfo");
        if (durationEl) durationEl.textContent = duration;

        var maxEl = document.getElementById("maxAttemptsInfo");
        if (maxEl) maxEl.textContent = maxAttempts;

        var subEl = document.getElementById("submittedInfo");
        if (subEl) subEl.textContent = submitted + "/" + total;
    }

    function formatDateVN(dateStr) {
        if (!dateStr) return "-";
        var s = String(dateStr).trim();
        if (s && !s.endsWith('Z') && !/[+-]\d{2}:\d{2}$/.test(s)) s = s + 'Z';
        var d = new Date(s);
        if (isNaN(d.getTime())) return "-";
        var opts = { timeZone: "Asia/Ho_Chi_Minh", hour: "2-digit", minute: "2-digit" };
        return d.toLocaleDateString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" }) + " " + d.toLocaleTimeString("vi-VN", opts);
    }

    function renderTable() {
        var tbody = document.getElementById("studentTableBody");
        if (!tbody) return;

        tbody.innerHTML = "";

        var totalPages = Math.ceil(filteredStudents.length / PAGE_SIZE) || 1;
        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

        var start = (currentPage - 1) * PAGE_SIZE;
        var end = Math.min(start + PAGE_SIZE, filteredStudents.length);
        var pageStudents = filteredStudents.slice(start, end);

        pageStudents.forEach(function (s, idx) {
            var tr = document.createElement("tr");
            tr.className = "student-row";
            tr.dataset.studentId = s.studentId || s.StudentId;
            tr.dataset.status = s.status || s.Status || "";
            tr.dataset.keyword = ((s.studentCode || s.StudentCode || "") + " " + (s.fullName || s.FullName || "")).toLowerCase();

            var lastSubmit = s.lastSubmitAt || s.LastSubmitAt;
            var duration = s.durationFormatted || s.DurationFormatted || "-";
            var score = s.lastScore ?? s.LastScore;
            var attempts = s.attemptCount ?? s.AttemptCount ?? 0;
            var maxAttempts = apiData.maxAttempts ?? apiData.MaxAttempts ?? 999;
            var status = s.status || s.Status || "Vắng thi";

            var scoreText = score != null ? String(score) : "-";
            var attemptsText = attempts + "/" + maxAttempts;

            var hasHistory = (s.history || s.History || []).length > 0;
            var expandBtn = hasHistory
                ? '<button type="button" class="btn btn-link text-dark p-0 expand-btn"><i class="bi bi-chevron-down"></i></button>'
                : '<button type="button" class="btn btn-link text-secondary p-0" disabled><i class="bi bi-chevron-down"></i></button>';

            tr.innerHTML =
                '<td><input class="form-check-input row-checkbox" type="checkbox"></td>' +
                '<td>' + (s.studentCode || s.StudentCode || "-") + '</td>' +
                '<td>' + (s.fullName || s.FullName || "-") + '</td>' +
                '<td>' + formatDateVN(lastSubmit) + '</td>' +
                '<td>' + duration + '</td>' +
                '<td class="fw-bold">' + scoreText + '</td>' +
                '<td>' + attemptsText + '</td>' +
                '<td>' + getStatusHtml(status) + '</td>' +
                '<td class="text-center">' + expandBtn + '</td>';

            tbody.appendChild(tr);

            if (hasHistory) {
                var historyRow = document.createElement("tr");
                historyRow.className = "history-row d-none";
                historyRow.dataset.studentId = s.studentId || s.StudentId;
                var history = s.history || s.History || [];
                historyRow.innerHTML = '<td colspan="9" class="border-top-0 pt-0 pb-4 px-0">' +
                    buildHistoryHtml(s.fullName || s.FullName || "Học sinh", history) +
                    '</td>';
                tbody.appendChild(historyRow);
            }
        });

        bindRowEvents();
    }

    function getStatusHtml(status) {
        if (status === "Đã nộp") return '<span class="text-success">Đã nộp</span>';
        if (status === "Đang làm") return '<span class="text-primary">Đang làm</span>';
        if (status === "Vắng thi") return '<span class="text-danger">Vắng thi</span>';
        return status;
    }

    function buildHistoryHtml(studentName, history) {
        var rows = (history || []).map(function (h, i) {
            var attemptNum = h.attemptNumber ?? h.AttemptNumber ?? (i + 1);
            var submittedAt = h.submittedAt || h.SubmittedAt;
            var duration = h.durationFormatted || h.DurationFormatted || "-";
            var score = h.score ?? h.Score;
            var isLast = h.isLast ?? h.IsLast;
            var submissionId = h.submissionId ?? h.SubmissionId;

            var rowClass = isLast ? "bg-primary-subtle bg-opacity-10" : "";
            var scoreHtml = score != null ? (isLast ? '<span class="fw-bold text-dark">' + score + '</span>' : score) : "-";
            var attemptLabel = isLast ? "Lần " + attemptNum + " (Cuối)" : "Lần " + attemptNum;

            return '<tr class="' + rowClass + '">' +
                '<td>' + attemptLabel + '</td>' +
                '<td>' + formatDateVN(submittedAt) + '</td>' +
                '<td>' + duration + '</td>' +
                '<td>' + scoreHtml + '</td>' +
                '<td class="text-center">' +
                '<a href="/Analytics/ViewSubmission?submissionId=' + submissionId + '&examId=' + (apiData.examId || apiData.ExamId || examId) + '&classId=' + (classId || '') + '" class="btn btn-outline-primary btn-sm px-3 py-1 fw-medium" title="Xem chi tiết bài làm">Xem bài</a>' +
                '</td></tr>';
        }).join("");

        return '<div style="padding-left: 60px; padding-right: 30px;">' +
            '<h6 class="fw-bold mb-3 text-secondary" style="font-size: 0.9rem;">Lịch sử làm bài (' + studentName + ')</h6>' +
            '<div class="row"><div class="col-md-9">' +
            '<table class="table table-sm table-bordered bg-white mb-0 sub-table">' +
            '<thead class="table-light text-secondary">' +
            '<tr><th class="fw-medium">Lần thi</th><th class="fw-medium">Thời gian nộp</th><th class="fw-medium">Thời gian làm</th><th class="fw-medium">Điểm số</th><th class="fw-medium text-center" style="width: 120px;">Hành động</th></tr>' +
            '</thead><tbody>' + rows + '</tbody></table>' +
            '</div></div></div>';
    }

    function bindRowEvents() {
        document.querySelectorAll(".expand-btn").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var tr = btn.closest("tr");
                if (!tr) return;
                var sid = tr.dataset.studentId;
                var next = tr.nextElementSibling;
                if (next && next.classList.contains("history-row") && next.dataset.studentId === sid) {
                    next.classList.toggle("d-none");
                    var icon = btn.querySelector("i");
                    if (icon) icon.className = next.classList.contains("d-none") ? "bi bi-chevron-down" : "bi bi-chevron-up";
                }
            });
        });

        document.getElementById("selectAll").addEventListener("change", function () {
            var checked = this.checked;
            document.querySelectorAll(".row-checkbox").forEach(function (cb) {
                if (!cb.closest(".history-row")) cb.checked = checked;
            });
            updateSelectedCount();
        });

        document.querySelectorAll(".row-checkbox").forEach(function (cb) {
            cb.addEventListener("change", updateSelectedCount);
        });
    }

    function updateSelectedCount() {
        var count = document.querySelectorAll(".row-checkbox:checked").length;
        var el = document.getElementById("selectedCount");
        if (el) el.textContent = "Đã chọn " + count + " học sinh";
    }

    function bindFilters() {
        var searchInput = document.getElementById("searchInput");
        var statusFilter = document.getElementById("statusFilter");

        if (searchInput) {
            searchInput.addEventListener("input", applyFilters);
        }
        if (statusFilter) {
            statusFilter.addEventListener("change", applyFilters);
        }
    }

    function applyFilters() {
        var keyword = (document.getElementById("searchInput").value || "").toLowerCase().trim();
        var status = (document.getElementById("statusFilter").value || "").trim();

        filteredStudents = allStudents.filter(function (s) {
            var kw = ((s.studentCode || s.StudentCode || "") + " " + (s.fullName || s.FullName || "")).toLowerCase();
            var matchKeyword = !keyword || kw.includes(keyword);
            var matchStatus = !status || (s.status || s.Status) === status;
            return matchKeyword && matchStatus;
        });

        currentPage = 1;
        renderTable();
        updatePagination();
    }

    function goToPage(page) {
        var totalPages = Math.ceil(filteredStudents.length / PAGE_SIZE) || 1;
        if (page < 1 || page > totalPages) return;
        currentPage = page;
        renderTable();
        updatePagination();
    }

    function updatePagination() {
        var total = filteredStudents.length;
        var totalPages = total === 0 ? 0 : Math.ceil(total / PAGE_SIZE);
        var start = total === 0 ? 0 : (currentPage - 1) * PAGE_SIZE + 1;
        var end = total === 0 ? 0 : Math.min(currentPage * PAGE_SIZE, total);

        var infoEl = document.getElementById("paginationInfo");
        if (infoEl) infoEl.textContent = "Hiển thị " + start + "-" + end + " trên " + total + " học sinh";

        var listEl = document.getElementById("paginationList");
        if (!listEl) return;

        listEl.innerHTML = "";

        if (totalPages <= 1) return;

        var prevLi = document.createElement("li");
        prevLi.className = "page-item" + (currentPage <= 1 ? " disabled" : "");
        prevLi.innerHTML = '<a class="page-link" href="#" data-page="prev" aria-label="Trước"><i class="bi bi-chevron-left"></i></a>';
        listEl.appendChild(prevLi);

        var maxVisible = 5;
        var half = Math.floor(maxVisible / 2);
        var firstPage = Math.max(1, currentPage - half);
        var lastPage = Math.min(totalPages, firstPage + maxVisible - 1);
        if (lastPage - firstPage < maxVisible - 1) firstPage = Math.max(1, lastPage - maxVisible + 1);

        for (var p = firstPage; p <= lastPage; p++) {
            var li = document.createElement("li");
            li.className = "page-item" + (p === currentPage ? " active" : "");
            li.innerHTML = '<a class="page-link" href="#" data-page="' + p + '">' + p + '</a>';
            listEl.appendChild(li);
        }

        var nextLi = document.createElement("li");
        nextLi.className = "page-item" + (currentPage >= totalPages ? " disabled" : "");
        nextLi.innerHTML = '<a class="page-link" href="#" data-page="next" aria-label="Sau"><i class="bi bi-chevron-right"></i></a>';
        listEl.appendChild(nextLi);

        listEl.querySelectorAll(".page-link").forEach(function (a) {
            a.addEventListener("click", function (e) {
                e.preventDefault();
                if (a.closest(".page-item").classList.contains("disabled")) return;
                var page = a.dataset.page;
                if (page === "prev") goToPage(currentPage - 1);
                else if (page === "next") goToPage(currentPage + 1);
                else goToPage(parseInt(page, 10));
            });
        });
    }

    document.addEventListener("DOMContentLoaded", init);
})();
