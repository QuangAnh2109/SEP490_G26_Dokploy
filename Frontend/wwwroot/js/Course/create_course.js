
document.addEventListener("DOMContentLoaded", function () {
    // Kiểm tra role Teacher
    const role = localStorage.getItem("userRole");
    if (role !== "Teacher") {
        showToast("Bạn không có quyền truy cập trang này.", "error");
        window.location.href = "/Course/CourseList";
        return;
    }

    loadSubjects();
    initForm();
});

function getToken() {
    return sessionStorage.getItem("jwtToken") || localStorage.getItem("jwtToken") || "";
}

async function loadSubjects() {
    const token = getToken();
    try {
        const res = await fetch(`${API_BASE_URL}/api/course/subjects`, {
            headers: { "Authorization": "Bearer " + token }
        });
        if (!res.ok) throw new Error("Không thể tải môn học");
        
        const subjects = await res.json();
        const select = document.getElementById("subjectSelect");
        
        select.innerHTML = '';
        const defaultOpt = document.createElement("option");
        defaultOpt.value = "";
        defaultOpt.textContent = "-- Chọn Môn Học --";
        select.appendChild(defaultOpt);
        subjects.forEach(s => {
            const opt = document.createElement("option");
            opt.value = s.subjectId;
            opt.textContent = `${s.code} - ${s.name}`;
            select.appendChild(opt);
        });
    } catch (error) {
        console.error(error);
        showToast("Đã có lỗi xảy ra khi tải danh sách môn học.", "error");
    }
}

function initForm() {
    const form = document.getElementById("createClassForm");
    form.addEventListener("submit", async function(e) {
        e.preventDefault();
        
        const btn = document.getElementById("submitBtn");
        btn.disabled = true;
        btn.textContent = "Đang xử lý...";

        const payload = {
            className: document.getElementById("classNameInput").value.trim(),
            semester: document.getElementById("semesterInput").value.trim(),
            subjectId: parseInt(document.getElementById("subjectSelect").value)
        };

        try {
            const token = getToken();
            const res = await fetch(`${API_BASE_URL}/api/course`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": "Bearer " + token
                },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                const data = await res.json();
                showSuccessModal(data.invitationCode);
            } else if (res.status === 400) {
                const errorData = await res.json();
                // Handle our custom exception text format
                if (errorData.message) {
                    showToast("Lỗi: " + errorData.message, "error");
                } else {
                    showToast("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.", "error");
                }
            } else {
                throw new Error("Lỗi máy chủ");
            }
        } catch (error) {
            console.error(error);
            showToast("Có lỗi xảy ra khi gọi API Tạo lớp.", "error");
        } finally {
            btn.disabled = false;
            btn.textContent = "Tạo Lớp Học";
        }
    });
}

function showSuccessModal(inviteCode) {
    document.getElementById("inviteCodeDisplay").textContent = inviteCode;
    
    const joinLink = `${window.location.origin}/Course/Join?code=${inviteCode}`;
    const linkInput = document.getElementById("inviteLinkInput");
    linkInput.value = joinLink;

    document.getElementById("copyLinkBtn").addEventListener("click", () => {
        navigator.clipboard.writeText(joinLink).then(() => {
            showToast("Đã chép link mời vào bộ nhớ tạm!", "info");
        });
    });

    const modal = new bootstrap.Modal(document.getElementById('successModal'));
    modal.show();
}
