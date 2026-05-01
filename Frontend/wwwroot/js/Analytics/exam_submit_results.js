// ═══════════════════════════════════════════
//  ExamSubmitResults — Thống kê nộp bài (Giáo viên)
// ═══════════════════════════════════════════

(function () {
    var root, examId, classId, allStudents = [], filteredStudents = [], apiData = {};
    var currentPage = 1;
    var PAGE_SIZE = 15;

    async function init() {
        root = document.getElementById("submitResultsRoot");
        if (!root) return;

        examId = root.dataset.examId;
        classId = root.dataset.classId || null;

        if (!examId || examId === "0") {
            showError("Không tìm thấy mã bài thi.");
            return;
        }

        await window.userReady;

        if (!isAuthenticated()) {
            window.location.href = "/Auth/Login";
            return;
        }
        if (getUserRole() !== RoleIds.Teacher) {
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

        var rowTemplate = document.getElementById("student-row-template");
        var historyRowTemplate = document.getElementById("history-row-template");

        pageStudents.forEach(function (s, idx) {
            var tr;
            if (rowTemplate) {
                tr = rowTemplate.content.cloneNode(true).querySelector("tr");
                tr.dataset.studentId = s.studentId || s.StudentId;
                tr.dataset.status = s.status || s.Status || "";
                tr.dataset.keyword = ((s.studentCode || s.StudentCode || "") + " " + (s.fullName || s.FullName || "")).toLowerCase();

                var lastSubmit = s.lastSubmitAt || s.LastSubmitAt;
                var durationStr = s.durationFormatted || s.DurationFormatted || "-";
                var score = s.lastScore ?? s.LastScore;
                var attempts = s.attemptCount ?? s.AttemptCount ?? 0;
                var maxAttempts = apiData.maxAttempts ?? apiData.MaxAttempts ?? 999;
                var status = s.status || s.Status || "Vắng thi";

                var scoreText = score != null ? String(score) : "-";
                var attemptsText = attempts + "/" + maxAttempts;

                tr.querySelector(".col-code").textContent = s.studentCode || s.StudentCode || "-";
                tr.querySelector(".col-name").textContent = s.fullName || s.FullName || "-";
                tr.querySelector(".col-last-submit").textContent = formatDateVN(lastSubmit);
                tr.querySelector(".col-duration").textContent = durationStr;
                tr.querySelector(".col-score").textContent = scoreText;
                tr.querySelector(".col-attempts").textContent = attemptsText;

                var statusSpan = document.createElement("span");
                if (status === "Đã nộp") statusSpan.className = "text-success";
                else if (status === "Đang làm") statusSpan.className = "text-primary";
                else if (status === "Vắng thi") statusSpan.className = "text-danger";
                statusSpan.textContent = status;
                tr.querySelector(".col-status").appendChild(statusSpan);

                var hasHistory = (s.history || s.History || []).length > 0;
                var actionCol = tr.querySelector(".col-action");
                if (hasHistory) {
                    var expandBtn = document.createElement("button");
                    expandBtn.type = "button";
                    expandBtn.className = "btn btn-link text-dark p-0 expand-btn";
                    expandBtn.innerHTML = '<i class="bi bi-chevron-down"></i>';
                    actionCol.appendChild(expandBtn);
                } else {
                    var disabledBtn = document.createElement("button");
                    disabledBtn.type = "button";
                    disabledBtn.className = "btn btn-link text-secondary p-0";
                    disabledBtn.disabled = true;
                    disabledBtn.innerHTML = '<i class="bi bi-chevron-down"></i>';
                    actionCol.appendChild(disabledBtn);
                }

                tbody.appendChild(tr);

                if (hasHistory && historyRowTemplate) {
                    var historyRow = historyRowTemplate.content.cloneNode(true).querySelector("tr");
                    historyRow.dataset.studentId = s.studentId || s.StudentId;
                    historyRow.querySelector(".history-title").textContent = "Lịch sử làm bài (" + (s.fullName || s.FullName || "Học sinh") + ")";
                    
                    var historyTbody = historyRow.querySelector(".history-tbody");
                    var historyRecords = s.history || s.History || [];
                    buildHistoryHtml(historyRecords, historyTbody);
                    
                    tbody.appendChild(historyRow);
                }
            }
        });

        bindRowEvents();
    }

    function buildHistoryHtml(historyList, historyTbody) {
        var recordTemplate = document.getElementById("history-record-template");
        if (!recordTemplate) return;

        (historyList || []).forEach(function (h, i) {
            var attemptNum = h.attemptNumber ?? h.AttemptNumber ?? (i + 1);
            var submittedAt = h.submittedAt || h.SubmittedAt;
            var duration = h.durationFormatted || h.DurationFormatted || "-";
            var score = h.score ?? h.Score;
            var isLast = h.isLast ?? h.IsLast;
            var submissionId = h.submissionId ?? h.SubmissionId;

            var recTr = recordTemplate.content.cloneNode(true).querySelector("tr");
            if (isLast) recTr.classList.add("bg-primary-subtle", "bg-opacity-10");

            var attemptLabel = isLast ? "Lần " + attemptNum + " (Cuối)" : "Lần " + attemptNum;
            recTr.querySelector(".col-attempt").textContent = attemptLabel;
            recTr.querySelector(".col-submitted").textContent = formatDateVN(submittedAt);
            recTr.querySelector(".col-duration").textContent = duration;

            if (score != null) {
                if (isLast) {
                    var sEl = document.createElement("span");
                    sEl.className = "fw-bold text-dark";
                    sEl.textContent = score;
                    recTr.querySelector(".col-score").appendChild(sEl);
                } else {
                    recTr.querySelector(".col-score").textContent = score;
                }
            } else {
                recTr.querySelector(".col-score").textContent = "-";
            }

            recTr.querySelector(".btn-view").href = '/Analytics/ViewSubmission?submissionId=' + submissionId + '&examId=' + (apiData.examId || apiData.ExamId || examId) + '&classId=' + (classId || '');

            historyTbody.appendChild(recTr);
        });
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

        var pageItemTemplate = document.getElementById("pagination-item-template");

        var maxVisible = 5;
        var half = Math.floor(maxVisible / 2);
        var firstPage = Math.max(1, currentPage - half);
        var lastPage = Math.min(totalPages, firstPage + maxVisible - 1);
        if (lastPage - firstPage < maxVisible - 1) firstPage = Math.max(1, lastPage - maxVisible + 1);

        if (pageItemTemplate) {
            var prevLi = pageItemTemplate.content.cloneNode(true).querySelector("li");
            if (currentPage <= 1) prevLi.classList.add("disabled");
            var prevLink = prevLi.querySelector("a");
            prevLink.dataset.page = "prev";
            prevLink.setAttribute("aria-label", "Trước");
            var prevIcon = document.createElement("i");
            prevIcon.className = "bi bi-chevron-left";
            prevLink.appendChild(prevIcon);
            listEl.appendChild(prevLi);

            for (var p = firstPage; p <= lastPage; p++) {
                var li = pageItemTemplate.content.cloneNode(true).querySelector("li");
                if (p === currentPage) li.classList.add("active");
                var a = li.querySelector("a");
                a.dataset.page = String(p);
                a.textContent = p;
                listEl.appendChild(li);
            }

            var nextLi = pageItemTemplate.content.cloneNode(true).querySelector("li");
            if (currentPage >= totalPages) nextLi.classList.add("disabled");
            var nextLink = nextLi.querySelector("a");
            nextLink.dataset.page = "next";
            nextLink.setAttribute("aria-label", "Sau");
            var nextIcon = document.createElement("i");
            nextIcon.className = "bi bi-chevron-right";
            nextLink.appendChild(nextIcon);
            listEl.appendChild(nextLi);
        }

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
