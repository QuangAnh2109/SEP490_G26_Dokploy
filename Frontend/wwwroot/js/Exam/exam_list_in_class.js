/* =========================================================
   exam_list_in_class.js – Exam List In Class Page Logic
   ========================================================= */

// These globals are set by the Razor page before this script loads (extracted to DOM):
// - classId (int)
// - classNameFromServer (string|null)
let allExams = [];
let classId = null;
let classNameFromServer = null;
let currentClassStatus = 1;

function initBreadcrumb(name) {
    setBreadcrumb([
        { text: "Lớp học", url: "/Class/ClassList" },
        { text: name, url: "/Class/ExamListInClass/" + classId + (name ? "?className=" + encodeURIComponent(name) : "") },
        { text: "Danh sách đề", url: null }
    ]);
}

async function ensureClassNameAndBreadcrumb() {
    try {
        const data = await apiClient.get(`/api/class/${classId}/settings`);
        currentClassStatus = data.status ?? 1;
        if (!classNameFromServer) initBreadcrumb(data.className || "Lớp " + classId);
        else initBreadcrumb(classNameFromServer);
    } catch {
        if (!classNameFromServer) initBreadcrumb("Lớp " + classId);
        else initBreadcrumb(classNameFromServer);
    }
}

async function loadExams() {

    if (!isAuthenticated()) {
        showToast("Bạn chưa đăng nhập", "error");
        window.location.href = "/Auth/Login";
        return;
    }

    const role = getUserRole();
    if (role === RoleIds.Student) {
        const settingsMenu = document.getElementById("settingsMenuItem");
        if (settingsMenu) settingsMenu.style.display = 'none';
        const pendingMenu = document.getElementById("pendingMenuItem");
        if (pendingMenu) pendingMenu.style.display = 'none';

        if (currentClassStatus === 0) {
            const practiceMenu = document.getElementById("practiceMenuItem");
            if (practiceMenu) practiceMenu.style.display = 'none';
        }

        // Hiện menu phân tích cá nhân dành cho học sinh
        const studentAnalyticsMenu = document.getElementById("studentAnalyticsMenuItem");
        if (studentAnalyticsMenu) studentAnalyticsMenu.classList.remove("d-none");
        const classAnalyticsMenu = document.getElementById("classAnalyticsMenuItem");
        if (classAnalyticsMenu) classAnalyticsMenu.style.display = 'none';
    }
    if (role === RoleIds.Teacher) {
        const btnCreate = document.getElementById("btnCreateExam");
        if (btnCreate && currentClassStatus !== 0) btnCreate.classList.remove("d-none");

        // Ẩn menu luyện tập dành cho học sinh
        const practiceMenu = document.getElementById("practiceMenuItem");
        if (practiceMenu) practiceMenu.style.display = 'none';
        const practiceHistoryMenu = document.getElementById("practiceHistoryMenuItem");
        if (practiceHistoryMenu) practiceHistoryMenu.style.display = 'none';
        const studentAnalyticsMenu = document.getElementById("studentAnalyticsMenuItem");
        if (studentAnalyticsMenu) studentAnalyticsMenu.style.display = 'none';

        // Hiện menu phân tích luyện tập dành cho giáo viên
        const classAnalyticsMenu = document.getElementById("classAnalyticsMenuItem");
        if (classAnalyticsMenu) classAnalyticsMenu.classList.remove("d-none");
    }

    await loadChapters();   // load chapter trước

    try {
        allExams = await apiClient.get(`/api/class/${classId}/exams`);
    } catch (err) {
        const httpStatus = err.xhr ? err.xhr.status : null;
        if (httpStatus === 401) {
            showToast("Phiên đăng nhập hết hạn", "error");
            window.location.href = "/Auth/Login";
            return;
        }
        allExams = [];
    }
    renderExams(allExams);
    initFilters();
}

async function loadChapters() {

    if (!isAuthenticated()) return;

    let chapters;
    try {
        chapters = await apiClient.get(`/api/class/${classId}/chapters`);
    } catch (err) {
        console.error("Không load được chapters");
        return;
    }

    const chapterSelect = document.getElementById("chapterFilter");
    chapterSelect.innerHTML = "";
    
    const defaultOpt = document.createElement("option");
    defaultOpt.value = "";
    defaultOpt.textContent = "Tất cả chương";
    chapterSelect.appendChild(defaultOpt);

    if (!chapters || chapters.length === 0) return;

    chapters.forEach(ch => {
        const opt = document.createElement("option");
        opt.value = ch.chapterId;
        opt.textContent = ch.name ?? ch.title;
        chapterSelect.appendChild(opt);
    });
}

function getExamStatus(openAt, closeAt) {

    if (!openAt || !closeAt) return "unknown";

    const toUtcDate = (s) => {
        let str = String(s).trim();
        if (str && !str.endsWith('Z') && !/[+-]\d{2}:\d{2}$/.test(str)) str += 'Z';
        return new Date(str);
    };

    const now = new Date();
    const openTime = toUtcDate(openAt);
    const closeTime = toUtcDate(closeAt);

    const diffMinutes = (openTime - now) / 1000 / 60;

    if (now > closeTime) return "closed";
    if (now >= openTime && now <= closeTime) return "open";
    if (diffMinutes > 0 && diffMinutes <= 30) return "upcoming";

    return "upcoming";
}

function applyFilter() {

    const chapter = document.getElementById("chapterFilter").value;
    const status = document.getElementById("statusFilter").value;
    const keyword = document.getElementById("searchFilter").value.toLowerCase();

    document.querySelectorAll(".exam-row").forEach(row => {

        const matchChapter =
            !chapter || row.dataset.chapter == chapter;

        const matchStatus =
            !status || row.dataset.status === status;

        const matchKeyword =
            !keyword || row.dataset.keyword.includes(keyword);

        row.classList.toggle(
            "d-none",
            !(matchChapter && matchStatus && matchKeyword)
        );
    });
}



function getStatusStyles(status) {
    // Integer statuses from DB (teacher view)
    const dbStatusMap = {
        0: { text: "Chờ duyệt", css: "badge bg-warning text-dark" },
        1: { text: "Đã duyệt", css: "badge bg-success" },
        2: { text: "Đang thi", css: "badge bg-primary" },
        3: { text: "Đã hủy", css: "badge bg-secondary" },
        4: { text: "Đã đóng", css: "badge bg-danger" },
    };
    if (typeof status === 'number' && dbStatusMap[status] !== undefined) {
        return dbStatusMap[status];
    }

    // String statuses (student view, computed from openAt/closeAt)
    if (status === "open")
        return { text: "Mở", css: "badge bg-primary" };
    if (status === "closed")
        return { text: "Đóng", css: "badge bg-danger" };
    if (status === "upcoming")
        return { text: "Sắp diễn ra", css: "badge bg-warning text-dark" };

    return { text: "Không xác định", css: "badge bg-secondary" };
}

function renderExams(exams) {

    const tbody = document.getElementById("examTableBody");
    tbody.innerHTML = "";

    if (!exams || exams.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 5;
        td.className = "text-center";
        td.textContent = "Không có bài kiểm tra";
        tr.appendChild(td);
        tbody.appendChild(tr);
        return;
    }

    const role = getUserRole();
    const isStudent = role === RoleIds.Student;
    const isTeacher = role === RoleIds.Teacher;

    const rowTemplate = document.getElementById("exam-row-template");
    const teacherActionTemplate = document.getElementById("teacher-action-template");
    const studentActionTemplate = document.getElementById("student-action-template");

    exams.forEach(exam => {
        // For teachers: use DB status directly; for students: compute from time
        const status = isTeacher ? exam.status : getExamStatus(exam.openAt, exam.closeAt);
        const statusKey = isTeacher ? exam.status : status; // for filtering

        const detailUrl = isTeacher
            ? `/Exam/ExamReview?examId=${exam.examId}`
            : `/StudentExam/ExamPreview?examId=${exam.examId}`;

        let tr;
        if (rowTemplate) {
            tr = rowTemplate.content.cloneNode(true).querySelector("tr");
            tr.dataset.chapter = exam.chapterId || "";
            tr.dataset.status = statusKey;
            tr.dataset.keyword = (exam.title || "").toLowerCase();

            tr.querySelector(".exam-title").textContent = exam.title;
            tr.querySelector(".exam-time").textContent = `${formatDateTime(exam.openAt)} - ${formatDateTime(exam.closeAt)}`;
            tr.querySelector(".exam-duration").textContent = `${exam.durationMinutes ?? '-'} phút`;

            const badgeStyles = getStatusStyles(status);
            const badgeSpan = document.createElement("span");
            badgeSpan.className = badgeStyles.css;
            badgeSpan.textContent = badgeStyles.text;
            tr.querySelector(".exam-status").appendChild(badgeSpan);

            const actionsCol = tr.querySelector(".exam-actions");
            
            if (isTeacher && teacherActionTemplate) {
                const actionFragment = teacherActionTemplate.content.cloneNode(true);
                actionFragment.querySelector(".btn-detail").href = detailUrl;
                
                if (exam.status !== 0) {
                    actionFragment.querySelector(".btn-result").href = `/Analytics/ExamSubmitResults?examId=${exam.examId}&classId=${classId}`;
                    actionFragment.querySelector(".btn-analytics").href = `/Analytics/ExamAnalytics?examId=${exam.examId}`;
                } else {
                    actionFragment.querySelector(".btn-result").remove();
                    actionFragment.querySelector(".btn-analytics").remove();
                }
                actionsCol.appendChild(actionFragment);
            } else if (isStudent && studentActionTemplate) {
                const actionFragment = studentActionTemplate.content.cloneNode(true);
                actionFragment.querySelector(".btn-detail").href = detailUrl;

                // Hide btn-take when:
                //  - exam is not open or class is closed, OR
                //  - student has hit MaxAttempts AND has no in-progress submission for this exam
                //    (student with in-progress can still continue regardless of attempts).
                const maxAttempts = exam.maxAttempts || 0;
                const studentAttempts = exam.studentAttempts || 0;
                const hasInProgress = !!exam.hasInProgressSubmission;
                const attemptsExhausted = maxAttempts > 0 && studentAttempts >= maxAttempts && !hasInProgress;

                if (status === "open" && currentClassStatus !== 0 && !attemptsExhausted) {
                    actionFragment.querySelector(".btn-take").href = `/StudentExam/TakeExam?examId=${exam.examId}`;
                } else {
                    actionFragment.querySelector(".btn-take").remove();
                }

                actionsCol.appendChild(actionFragment);
            }
        }
        
        tbody.appendChild(tr);
    });
}

function initFilters() {

    const chapter = document.getElementById("chapterFilter");
    const status = document.getElementById("statusFilter");
    const search = document.getElementById("searchFilter");

    if (chapter && status && search) {
        [chapter, status].forEach(el =>
            el.addEventListener("change", applyFilter)
        );

        search.addEventListener("input", applyFilter);
    }
}

function bypassContainer() {
    const wrapper = document.querySelector('.class-wrapper');
    if (wrapper) {
        let parent = wrapper.parentElement;
        while (parent && parent.tagName !== 'BODY') {
            if (parent.classList.contains('container')) {
                parent.classList.remove('container');
                parent.classList.add('container-fluid');
                parent.style.padding = '0';
                parent.style.maxWidth = '100%';
            }
            if (parent.tagName === 'MAIN') {
                parent.style.padding = '0';
            }
            parent = parent.parentElement;
        }
    }
}

document.addEventListener("DOMContentLoaded", async function () {
    await window.userReady;
    const dataEl = document.getElementById("classData");
    classId = dataEl ? dataEl.dataset.classId : null;
    classNameFromServer = dataEl ? dataEl.dataset.className : null;

    bypassContainer();

    // Set sidebar class name
    const sidebarName = document.getElementById('sidebarClassName');
    if (sidebarName && classNameFromServer) {
        sidebarName.textContent = classNameFromServer;
    }

    await ensureClassNameAndBreadcrumb();
    loadExams();
});
