
async function acceptInvite() {
    const tokenDataEl = document.getElementById("courseData");
    const tokenQuery = tokenDataEl ? tokenDataEl.dataset.tokenQuery : "";
    const currentToken = getToken();

    if (!currentToken) {
        // Save redirect URL and go to login
        sessionStorage.setItem("redirectAfterLogin", window.location.href);
        window.location.href = "/Auth/Login";
        return;
    }

    if (!tokenQuery) {
        showError("Link không hợp lệ.");
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/api/Course/accept-invite`, {
            method: 'POST',
            headers: {
                "Authorization": "Bearer " + currentToken,
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ token: tokenQuery })
        });

        if (response.ok) {
            document.getElementById("loadingStatus").classList.add("d-none");
            document.getElementById("successStatus").classList.remove("d-none");
            
            setTimeout(() => {
                window.location.href = "/Course/CourseList";
            }, 2000);
        } else {
            const err = await response.text();
            showError(err || "Link không hợp lệ hoặc đã hết hạn");
        }
    } catch (error) {
        console.error(error);
        showError("Đã xảy ra lỗi mạng.");
    }
}

function showError(msg) {
    document.getElementById("loadingStatus").classList.add("d-none");
    document.getElementById("errorStatus").classList.remove("d-none");
    document.getElementById("errorMessage").innerText = msg;
}

document.addEventListener("DOMContentLoaded", acceptInvite);
