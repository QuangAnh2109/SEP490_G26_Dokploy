document.addEventListener("DOMContentLoaded", function () {
    initAuthUI();
    initRoleUI();

    if (document.getElementById("courseGrid")) {
        loadCourses();
    }

    if (document.getElementById("examTableBody")) {
        const classId = document.body.dataset.classId;
        ExamPage.initExamList(classId)
            .catch(err => console.error("INIT EXAM ERROR:", err));
    }

    if (document.getElementById("studentTableBody")) {
        const classId = document.body.dataset.classId;
        CoursePage.initStudentPage(classId);
    }

    initJoinForm();
});

function setupRoleUI() {
    const role = getUserRole();

    const settings = document.getElementById("settingsMenuItem");
    if (settings) {
        settings.style.display =
            (role === "Teacher") ? "block" : "none";
    }

    const addBtn = document.getElementById("addStudentBtn");
    if (addBtn) {
        addBtn.style.display =
            (role === "Teacher") ? "inline-block" : "none";
    }
}
function renderTableHeader() {
    const isTeacher = getUserRole() === "Teacher";

    let html = `
        <th><input type="checkbox" id="checkAll"></th>
        <th>STT</th>
        <th>Mã SV</th>
        <th>Họ tên</th>
        <th>Email</th>
    `;

    if (isTeacher) {
        html += `<th>Ngày tham gia</th><th>Hành động</th>`;
    }

    document.getElementById("studentTableHead").innerHTML = html;
}
// ===== AUTH NAVBAR =====
function initAuthUI() {
    const isAuth = isAuthenticated();
    const role = getUserRole();

    document.querySelectorAll(".auth-guest").forEach(e => {
        e.classList.toggle("d-none", isAuth);
    });

    document.querySelectorAll(".auth-user").forEach(e => {
        e.classList.toggle("d-none", !isAuth);
    });

    // role teacher
    document.querySelectorAll(".role-dependent").forEach(e => {
        e.classList.add("d-none");
    });

    if (role === "Teacher") {
        document.querySelectorAll(".role-teacher").forEach(e => {
            e.classList.remove("d-none");
        });
    }

    // ẩn đổi mật khẩu nếu login google
    if (isGoogleUser()) {
        const changePass = document.querySelector('a[href="/Auth/ChangePassword"]');
        if (changePass) changePass.style.display = "none";
    }
}

// ===== ROLE UI =====
function initRoleUI() {
    const role = getUserRole();

    const title = document.getElementById("pageTitle");
    const subtitle = document.getElementById("pageSubtitle");
    const btnContainer = document.getElementById("actionButtonContainer");

    if (!btnContainer) return;

    if (role === "Teacher") {
        title.innerText = "Danh sách lớp giảng dạy";
        subtitle.innerText = "Tìm kiếm và quản lý lớp học";

        btnContainer.innerHTML = `
            <a href="/Course/Create" class="btn btn-primary">
                + Tạo lớp
            </a>
        `;
    } else {
        title.innerText = "Lớp học của tôi";
        subtitle.innerText = "Danh sách lớp đã tham gia";

        btnContainer.innerHTML = `
            <button class="btn btn-primary"
                    data-bs-toggle="modal"
                    data-bs-target="#joinClassModal">
                + Tham gia lớp
            </button>
        `;
    }
}

// ===== LOAD COURSES =====
function loadCourses() {
    const token = getToken();
    if (!token) {
        showToast("Bạn chưa đăng nhập", "error");
        return;
    }

    apiClient.get("/api/course/my")
        .then(data => {
            renderCourses(data);
            initCourseFilters();
        })
        .catch(err => {
            showToast(err.message, "error");
        });
}

// ===== RENDER =====
function renderCourses(courses) {
    const role = getUserRole();
    const isTeacher = role === "Teacher";
    const grid = document.getElementById("courseGrid");
    grid.innerHTML = "";

    if (!courses || courses.length === 0) {
        grid.innerHTML = `<div class="col-12 text-muted">Chưa có lớp học</div>`;
        return;
    }

    const semesters = new Set();
    const subjects = new Set();

    courses.forEach(c => {
        semesters.add(c.semester);

        const subjectDisplay = c.subjectCode
            ? c.subjectCode + ' - ' + (c.subjectName || '')
            : (c.subjectName || '');

        if (subjectDisplay) subjects.add(subjectDisplay);

        const col = document.createElement("div");
        col.className = "col-12 col-sm-6 col-lg-3";

        col.innerHTML = `
            <div class="card class-card h-100"
                data-subject="${subjectDisplay}"
                data-semester="${c.semester || ''}"
                data-keyword="${`${c.className} ${c.teacherName} ${subjectDisplay}`.toLowerCase()}">

                <div class="card-body d-flex flex-column">
                    <h5>${c.className}</h5>
                    <small class="text-muted mb-2">${c.teacherName}</small>

                    <small>Môn: ${subjectDisplay}</small>
                    <small>Học kỳ: ${c.semester || ''}</small>

                    <div class="mt-auto small text-muted mb-2">
                        Exams: ${c.examCount} • Sĩ số ${c.studentCount}
                    </div>

                    <a class="btn btn-enter btn-sm w-100"
                       href="/Course/ExamListInCourse/${c.classId}">
                       Vào khóa học
                    </a>

                    ${!isTeacher ? `
                        <button class="btn btn-outline-danger btn-sm mt-2 w-100"
                                onclick="leaveCourse(${c.classId})">
                            Rời lớp
                        </button>` : ""}
                </div>
            </div>
        `;

        grid.appendChild(col);
    });

    populateSelect("courseSemesterFilter", semesters);
    populateSelect("courseSubjectFilter", subjects);
}

// ===== FILTER =====
function populateSelect(id, values) {
    const select = document.getElementById(id);
    select.innerHTML = `<option value="">Tất cả</option>`;

    values.forEach(v => {
        if (!v) return;
        const o = document.createElement("option");
        o.value = v;
        o.textContent = v;
        select.appendChild(o);
    });
}

let allStudents = [];

function applyFilter() {
    let filtered = [...allStudents];

    const keyword = (document.getElementById("searchInput")?.value || "").toLowerCase();

    if (keyword) {
        filtered = filtered.filter(s =>
            (s.fullName || "").toLowerCase().includes(keyword) ||
            (s.studentCode || "").toLowerCase().includes(keyword)
        );
    }

    const sort = document.getElementById("sortSelect")?.value;

    switch (sort) {
        case "name_asc":
            filtered.sort((a, b) => a.fullName.localeCompare(b.fullName));
            break;
        case "name_desc":
            filtered.sort((a, b) => b.fullName.localeCompare(a.fullName));
            break;
        case "code_asc":
            filtered.sort((a, b) => a.studentCode.localeCompare(b.studentCode));
            break;
        case "newest":
            filtered.sort((a, b) => new Date(b.joinedAtUtc) - new Date(a.joinedAtUtc));
            break;
    }

    CourseUI.renderStudents(filtered);
}

// ===== ACTION =====
function leaveCourse(id) {
    showConfirm("Bạn chắc chắn muốn rời lớp?", "Xác nhận", () => {
        apiClient.post(`/api/course/${id}/leave`)
            .then(() => {
                showToast("Đã rời lớp");
                loadCourses();
            })
            .catch(err => showToast(err.message, "error"));
    });
}

// ===== JOIN =====
function initJoinForm() {
    const form = document.getElementById("joinClassForm");
    if (!form) return;

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        const code = document.getElementById("joinClassCodeInput").value.trim();
        if (!code) return;

        apiClient.post("/api/course/join", { invitationCode: code })
            .then(() => {
                showToast("Tham gia thành công");

                const modal = bootstrap.Modal.getInstance(document.getElementById("joinClassModal"));
                modal && modal.hide();

                form.reset();
                loadCourses();
            })
            .catch(err => showToast(err.message, "error"));
    });
}
const CourseService = (function () {

    function handleUnauthorized(error) {
        if (error?.xhr?.status === 401) {
            showToast("Phiên đăng nhập hết hạn", "error");
            removeToken();
            window.location.href = "/Auth/Login";
            return true;
        }
        return false;
    }

    async function getStudents(classId) {
        try {
            const res = await apiClient.get(`/api/Course/${classId}/students`);
            return res;
        } catch (error) {
            if (handleUnauthorized(error)) return null;
            throw error;
        }
    }

    async function getExams(classId) {
        try {
            const res = await apiClient.get(`/api/Course/${classId}/exams`);
            return res.data || res;
        } catch (error) {
            if (handleUnauthorized(error)) return null;
            throw error;
        }
    }

    async function getCourseDetail(classId) {
        try {
            return await apiClient.get(`/api/Course/${classId}`);
        } catch (error) {
            if (handleUnauthorized(error)) return null;
            throw error;
        }
    }
    async function getChapters(classId) {
        try {
            return await apiClient.get(`/api/Course/${classId}/chapters`);
        } catch (error) {
            if (handleUnauthorized(error)) return null;
            throw error;
        }
    }

    return {
        getStudents,
        getExams,
        getCourseDetail,
        getChapters
    };
})();

const CourseUI = (function () {

    function hideSettingsIfStudent() {
        const role = getUserRole();
        if (role === "Student") {
            const el = document.getElementById("settingsMenuItem");
            if (el) el.style.display = "none";
        }
    }

    function formatDateTime(dateString) {
        if (!dateString) return "-";
        return new Date(dateString).toLocaleString("vi-VN");
    }

    function renderStudents(students) {
        const tbody = document.getElementById("studentTableBody");
        if (!tbody) return;

        tbody.innerHTML = "";

        const isTeacher = getUserRole() === "Teacher";

        students.forEach((s, i) => {
            let html = `
            <td>
                <input type="checkbox" class="student-checkbox" value="${s.studentId}">
            </td>
            <td>${i + 1}</td>
            <td class="text-primary">${s.studentCode}</td>
            <td>${s.fullName}</td>
            <td>${s.email}</td>
        `;

            if (isTeacher) {
                html += `
                <td>${new Date(s.joinedAtUtc).toLocaleString("vi-VN")}</td>
                <td><i class="fa fa-trash text-danger"></i></td>
            `;
            }

            const tr = document.createElement("tr");
            tr.innerHTML = html;
            tbody.appendChild(tr);
        });
    }

    function renderError(message = "Có lỗi xảy ra") {
        const tbody = document.getElementById("studentTableBody");
        if (!tbody) return;

        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="text-center text-danger">
                    ${message}
                </td>
            </tr>
        `;
    }

    return {
        hideSettingsIfStudent,
        renderStudents,
        renderError
    };
})();

const CoursePage = (function () {

    async function initStudentPage(classId) {
        if (!isAuthenticated()) {
            window.location.href = "/Auth/Login";
            return;
        }

        setupRoleUI();
        renderTableHeader();

        try {
            const res = await CourseService.getStudents(classId);

            // API mới trả về list trực tiếp
            const students = res.data || res;

            allStudents = students || [];
            applyFilter();

        } catch (e) {
            console.error(e);
            CourseUI.renderError();
        }

        document.getElementById("searchInput")?.addEventListener("input", applyFilter);
        document.getElementById("sortSelect")?.addEventListener("change", applyFilter);

        initStudentCheckbox();
    }

    return {
        initStudentPage
    };
})();

const ExamUI = (function () {

    function renderChapters(chapters) {
        const select = document.getElementById("chapterFilter");
        if (!select) return;

        select.innerHTML = `<option value="">Tất cả chương</option>`;

        chapters?.forEach(ch => {
            select.innerHTML += `
                <option value="${ch.chapterId}">
                    ${ch.name || ch.title}
                </option>
            `;
        });
    }

    function getExamStatus(openAt, closeAt) {
        if (!openAt || !closeAt) return "unknown";

        const now = new Date();
        const open = new Date(openAt);
        const close = new Date(closeAt);

        const diff = (open - now) / 60000;

        if (now > close) return "closed";
        if (now >= open && now <= close) return "open";
        if (diff > 0 && diff <= 30) return "upcoming";

        return "upcoming";
    }

    function renderStatus(status) {
        if (status === "open") return '<span class="badge bg-primary">Mở</span>';
        if (status === "closed") return '<span class="badge bg-danger">Đóng</span>';
        if (status === "upcoming") return '<span class="badge bg-warning text-dark">Sắp diễn ra</span>';
        return '<span class="badge bg-secondary">Không xác định</span>';
    }

    function renderExams(exams) {

        const tbody = document.getElementById("examTableBody");
        if (!tbody) return;

        tbody.innerHTML = "";

        if (!exams || exams.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center">Không có bài kiểm tra</td></tr>`;
            return;
        }

        const role = getUserRole();

        exams.forEach(e => {

            const status = getExamStatus(e.openAt, e.closeAt);

            let actions = `
                <a class="btn btn-sm btn-outline-secondary" href="/StudentExam/ExamPreview?examId=${e.examId}">
                    Chi tiết
                </a>
            `;

            if (role === "Student" && status === "open") {
                actions += `
                    <a class="btn btn-sm btn-primary" href="/StudentExam/TakeExam?examId=${e.examId}">
                        Vào làm
                    </a>
                `;
            }

            if (role === "Teacher") {
                actions += `
                    <a class="btn btn-sm btn-info text-white" href="/Analytics/ExamAnalytics?examId=${e.examId}">
                        Phân tích
                    </a>
                `;
            }

            const row = document.createElement("tr");
            row.classList.add("exam-row");
            row.dataset.chapter = e.chapterId || "";
            row.dataset.status = status;
            row.dataset.keyword = (e.title || "").toLowerCase();

            row.innerHTML = `
                <td>${e.title}</td>
                <td>${formatDateTime(e.openAt)} - ${formatDateTime(e.closeAt)}</td>
                <td>${e.durationMinutes || '-'} phút</td>
                <td>${renderStatus(status)}</td>
                <td>${actions}</td>
            `;

            tbody.appendChild(row);
        });
    }

    function formatDateTime(date) {
        return date ? new Date(date).toLocaleString("vi-VN") : "-";
    }

    return {
        renderChapters,
        renderExams
    };
})();
const ExamPage = (function () {

    async function initExamList(classId) {

        if (!isAuthenticated()) {
            showToast("Bạn chưa đăng nhập", "error");
            window.location.href = "/Auth/Login";
            return;
        }

        CourseUI.hideSettingsIfStudent();

        try {
            const res = await CourseService.getExams(classId);
            const exams = res.data || res;

            if (!Array.isArray(exams)) {
                throw new Error("Dữ liệu không hợp lệ");
            }

            ExamUI.renderExams(exams);

            const chapters = await CourseService.getChapters(classId);
            ExamUI.renderChapters(chapters);

            initExamFilters();

        } catch (err) {
            console.error(err);
        }
    }
    return {
        initExamList
    };
})();
function initStudentCheckbox() {
    const checkAll = document.getElementById("checkAll");

    if (!checkAll) return;

    checkAll.addEventListener("change", function () {
        document.querySelectorAll(".student-checkbox")
            .forEach(cb => cb.checked = checkAll.checked);
    });
}
function getSelectedStudents() {
    return Array.from(document.querySelectorAll(".student-checkbox:checked"))
        .map(cb => cb.value);
}
function initCourseFilters() {
    ["courseSearchInput", "courseSemesterFilter", "courseSubjectFilter"]
        .forEach(id => {
            const el = document.getElementById(id);
            if (!el) return;
            el.addEventListener("input", applyCourseFilters);
            el.addEventListener("change", applyCourseFilters);
        });
}

function applyCourseFilters() {
    const keyword = (document.getElementById("courseSearchInput").value || "").toLowerCase();
    const semester = (document.getElementById("courseSemesterFilter").value || "").toLowerCase();
    const subject = (document.getElementById("courseSubjectFilter").value || "").toLowerCase();

    document.querySelectorAll(".class-card").forEach(card => {
        const match =
            (!keyword || card.dataset.keyword.includes(keyword)) &&
            (!semester || card.dataset.semester.toLowerCase() === semester) &&
            (!subject || card.dataset.subject.toLowerCase() === subject);

        card.closest(".col-12").classList.toggle("d-none", !match);
    });
}
function initExamFilters() {
    ["chapterFilter", "statusFilter", "searchFilter"]
        .forEach(id => {
            const el = document.getElementById(id);
            if (!el) return;
            el.addEventListener("change", applyExamFilter);
            el.addEventListener("input", applyExamFilter);
        });
}

function applyExamFilter() {
    const chapter = document.getElementById("chapterFilter").value;
    const status = document.getElementById("statusFilter").value;
    const keyword = document.getElementById("searchFilter").value.toLowerCase();

    document.querySelectorAll(".exam-row").forEach(row => {
        const match =
            (!chapter || row.dataset.chapter == chapter) &&
            (!status || row.dataset.status === status) &&
            (!keyword || row.dataset.keyword.includes(keyword));

        row.classList.toggle("d-none", !match);
    });
}