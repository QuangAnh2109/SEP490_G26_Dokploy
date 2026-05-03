// inviteCode is injected globally by Razor
let inviteCode = "";

document.addEventListener("DOMContentLoaded", function () {
    const inviteDataEl = document.getElementById("classData");
    inviteCode = inviteDataEl ? inviteDataEl.dataset.inviteCode : "";

    if (!inviteCode) {
        showError("Vui lòng cung cấp mã mời (code) trong đường link.");
        return;
    }

    checkAuthAndJoin();
});

async function checkAuthAndJoin() {

    try {
        await apiClient.post('/api/class/join', { invitationCode: inviteCode });

        // Thành công
        document.getElementById("processingStatus").classList.add("d-none");
        document.getElementById("successStatus").classList.remove("d-none");

        setTimeout(() => {
            window.location.href = "/Class/ClassList";
        }, 3000);

    } catch (error) {
        // apiClient trả về object { xhr, status, error, message }
        const errorMsg = error.message || "Mã mời không hợp lệ, đã hết hạn, hoặc bạn đã ở sẵn trong lớp này.";
        showError(errorMsg);
    }
}

function showError(msg) {
    document.getElementById("processingStatus").classList.add("d-none");
    document.getElementById("errorStatus").classList.remove("d-none");
    document.getElementById("errorMsg").textContent = msg;
}