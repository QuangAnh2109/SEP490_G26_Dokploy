document.addEventListener("DOMContentLoaded", async function () {
    await window.userReady;
    setBreadcrumb([{ text: "Lớp học", url: null }]);
    initRoleUI();
    loadClasses();
    initJoinForm();
});

// Use global functions from site.js instead of local ones

function initRoleUI() {
    const role = getUserRole();
    const title = document.getElementById("pageTitle");
    const subtitle = document.getElementById("pageSubtitle");
    const btnContainer = document.getElementById("actionButtonContainer");

    btnContainer.innerHTML = ""; // clean first

    if (role === RoleIds.Teacher) {
        title.textContent = "Danh sách lớp giảng dạy";
        subtitle.textContent = "Tìm kiếm và quản lý các lớp học bạn đang giảng dạy.";

        const teacherTemplate = document.getElementById("teacher-btn-template");
        if (teacherTemplate) {
            btnContainer.appendChild(teacherTemplate.content.cloneNode(true));
        }
    } else {
        title.textContent = "Lớp học của tôi";
        subtitle.textContent = "Tìm kiếm và truy cập các lớp đã tham gia.";

        const studentTemplate = document.getElementById("student-btn-template");
        if (studentTemplate) {
            btnContainer.appendChild(studentTemplate.content.cloneNode(true));
        }
    }
}

function loadClasses() {
    apiClient.get('/api/class/my')
        .then(data => {
            renderClasses(data);
            initFilters();
        })
        .catch(() => {
            showToast("Không thể tải danh sách lớp.", "error");
        });
}

function renderClasses(classes) {
    const role = getUserRole();
    const isTeacher = role === RoleIds.Teacher;
    const grid = document.getElementById("classGrid");
    grid.innerHTML = "";

    if (!classes || classes.length === 0) {
        const p = document.createElement("p");
        p.textContent = "Chưa có lớp học.";
        grid.appendChild(p);
        return;
    }

    const semesters = new Set();
    const subjects = new Set();

    const cardTemplate = document.getElementById("class-card-template");

    classes.forEach(c => {
        semesters.add(c.semester);

        const subjectDisplay = c.subjectCode ? c.subjectCode + ' - ' + (c.subjectName || '') : (c.subjectName || '');
        if (subjectDisplay) {
            subjects.add(subjectDisplay);
        }

        let article;
        if (cardTemplate) {
            article = cardTemplate.content.cloneNode(true).querySelector("article");
            article.dataset.subject = subjectDisplay;
            article.dataset.semester = c.semester || "";
            article.dataset.keyword = `${c.className} ${c.teacherName} ${subjectDisplay}`.toLowerCase();

            article.querySelector(".class-name").textContent = c.className;
            if (c.role === "Pending") {
                article.querySelector(".pending-badge").classList.remove("d-none");
            }
            if (c.status === 0) {
                article.querySelector(".closed-badge").classList.remove("d-none");
            }

            article.querySelector(".class-teacher").textContent = c.teacherName;
            article.querySelector(".class-subject").textContent = `Môn học: ${subjectDisplay}`;
            article.querySelector(".class-semester").textContent = `Học kỳ: ${c.semester || ""}`;
            article.querySelector(".class-stats").textContent = `Exams: ${c.examCount} • Sĩ số: ${c.studentCount}`;

            const actionsDiv = article.querySelector(".card-actions");
            if (c.role === "Pending") {
                const btnWait = document.createElement("button");
                btnWait.className = "btn btn-sm btn-secondary me-2";
                btnWait.disabled = true;
                btnWait.textContent = "Chờ duyệt";
                actionsDiv.appendChild(btnWait);
            } else {
                const aEnter = document.createElement("a");
                aEnter.className = `btn btn-sm ${isTeacher ? "btn-primary" : "btn-outline-primary"} me-2`;
                aEnter.href = `/Class/ExamListInClass/${c.classId}?className=${encodeURIComponent(c.className || '')}`;
                aEnter.textContent = "Vào Lớp học";
                actionsDiv.appendChild(aEnter);
            }

            if (!isTeacher && c.status !== 0) {
                const btnLeave = document.createElement("button");
                btnLeave.className = "btn btn-sm btn-danger";
                btnLeave.onclick = () => leaveClass(c.classId);
                btnLeave.textContent = "Rời lớp";
                actionsDiv.appendChild(btnLeave);
            }
        }
        if (article) grid.appendChild(article);
    });

    populateSelect("classSemesterFilter", semesters);
    populateSelect("classSubjectFilter", subjects);
}

function populateSelect(id, values) {
    const select = document.getElementById(id);
    select.innerHTML = "";
    const defaultOpt = document.createElement("option");
    defaultOpt.value = "";
    defaultOpt.textContent = "Tất cả";
    select.appendChild(defaultOpt);
    values.forEach(v => {
        if (!v) return;
        const option = document.createElement("option");
        option.value = v;
        option.textContent = v;
        select.appendChild(option);
    });
}

function initFilters() {
    const search = document.getElementById("classSearchInput");
    const semester = document.getElementById("classSemesterFilter");
    const subject = document.getElementById("classSubjectFilter");

    if (search && semester && subject) {
        [search, semester, subject].forEach(el => {
            el.addEventListener("input", applyFilters);
            el.addEventListener("change", applyFilters);
        });
    }
}

function applyFilters() {
    const keyword = document.getElementById("classSearchInput").value.toLowerCase();
    const semester = document.getElementById("classSemesterFilter").value.toLowerCase();
    const subject = document.getElementById("classSubjectFilter").value.toLowerCase();

    document.querySelectorAll(".class-card").forEach(card => {
        const matchKeyword = !keyword || card.dataset.keyword.includes(keyword);
        const matchSemester = !semester || card.dataset.semester.toLowerCase() === semester;
        const matchSubject = !subject || card.dataset.subject.toLowerCase() === subject;

        card.classList.toggle("d-none",
            !(matchKeyword && matchSemester && matchSubject));
    });
}

function leaveClass(classId) {
    showConfirm("Bạn chắc chắn muốn rời Lớp học này?", "Xác nhận rời lớp", () => {
        apiClient.post(`/api/class/${classId}/leave`)
            .then(() => {
                showToast("Đã rời Lớp học.", "success");
                loadClasses();
            })
            .catch(() => {
                showToast("Không thể rời Lớp học.", "error");
            });
    });
}

function initJoinForm() {
    const joinForm = document.getElementById("joinClassForm");

    if (joinForm) {
        joinForm.addEventListener("submit", function (e) {
            e.preventDefault();

            const code = document
                .getElementById("joinClassCodeInput")
                .value
                .trim();

            if (!code) return;

            apiClient.post('/api/class/join', { invitationCode: code })
                .then(() => {
                    showToast("Bạn đã gửi yêu cầu tham gia lớp, vui lòng chờ duyệt.", "success");

                    const modalEl = document.getElementById("joinClassModal");
                    const modal = bootstrap.Modal.getInstance(modalEl);
                    modal.hide();

                    joinForm.reset();
                    loadClasses();
                })
                .catch(err => {
                    showToast(err.message || "Mã mời không hợp lệ hoặc đã xảy ra lỗi.", "error");
                });
        });
    }
}