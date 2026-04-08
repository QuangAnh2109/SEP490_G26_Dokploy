// inviteCode is injected globally by Razor

let inviteCode = "";

document.addEventListener("DOMContentLoaded", function () {
    const inviteDataEl = document.getElementById("courseData");
    inviteCode = inviteDataEl ? inviteDataEl.dataset.inviteCode : "";

    if (!inviteCode) {
        showError("Vui lòng cung cấp mã mời (code) trong đường link.");
        return;
    }
    
    checkAuthAndJoin();
});

function getToken() {
    return sessionStorage.getItem("jwtToken") || localStorage.getItem("jwtToken") || "";
}

async function checkAuthAndJoin() {
    const token = getToken();
    if (!token) {
        // Not logged in -> Redirect to login page and remember return URL
        const currentUrl = encodeURIComponent(window.location.href);
        // Assume login page is /Auth/Login or similar
        // We'll pass the link back
        showConfirm("Bạn cần đăng nhập để tham gia lớp. Đi tới trang đăng nhập?", "Yêu cầu đăng nhập", () => {
            window.location.href = `/Auth/Login?returnUrl=${currentUrl}`;
        });
        return;
    }

    try {
        const res = await fetch(`${API_BASE_URL}/api/course/join`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": "Bearer " + token
            },
            body: JSON.stringify({ invitationCode: inviteCode })
        });

        if (res.ok) {
            document.getElementById("processingStatus").classList.add("d-none");
            document.getElementById("successStatus").classList.remove("d-none");
            
            setTimeout(() => {
                window.location.href = "/Course/CourseList";
            }, 3000);
        }
        else if (res.status === 400 || res.status === 404) {
            const text = await res.text();
            showError("Mã mời không hợp lệ, đã hết hạn, hoặc bạn đã ở sẵn trong lớp này. (" + text + ")");
        } else {
            showError("Lỗi hệ thống khi tham gia lớp.");
        }
    } catch (error) {
        showError("Không thể kết nối đến máy chủ.");
    }
}

function showError(msg) {
    document.getElementById("processingStatus").classList.add("d-none");
    document.getElementById("errorStatus").classList.remove("d-none");
    document.getElementById("errorMsg").textContent = msg;
}
