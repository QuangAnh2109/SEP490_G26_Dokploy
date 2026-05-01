async function acceptInvite() {
    const tokenDataEl = document.getElementById("courseData");
    const tokenQuery = tokenDataEl ? tokenDataEl.dataset.tokenQuery : "";

    await window.userReady;

    if (!isAuthenticated()) {
        sessionStorage.setItem("redirectAfterLogin", window.location.href);
        window.location.href = "/Auth/Login";
        return;
    }

    if (!tokenQuery) {
        showError("Link không hợp lệ.");
        return;
    }

    try {
        await apiClient.post('/api/Course/accept-invite', { token: tokenQuery });

        document.getElementById("loadingStatus").classList.add("d-none");
        document.getElementById("successStatus").classList.remove("d-none");

        setTimeout(() => {
            window.location.href = "/Course/CourseList";
        }, 2000);

    } catch (error) {
        console.error(error);
        showError(error.message || "Link không hợp lệ hoặc đã hết hạn");
    }
}

function showError(msg) {
    document.getElementById("loadingStatus").classList.add("d-none");
    document.getElementById("errorStatus").classList.remove("d-none");
    document.getElementById("errorMessage").innerText = msg;
}

document.addEventListener("DOMContentLoaded", acceptInvite);