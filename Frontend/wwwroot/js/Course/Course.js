const API_BASE = window.API_BASE_URL; // chỉnh đúng port backend của m


function getAuthToken() {
    const token =
        sessionStorage.getItem('jwtToken') ||
        localStorage.getItem('jwtToken') ||
        '';

    return token;
}


/* ==============================
   LOAD EXAMS BY CLASS
============================== */

function loadClassExams(classId) {

    const token = getAuthToken();

    console.log("Calling API:", `${API_BASE}/api/course/${classId}/exams`);
    console.log("Token:", token);

    $.ajax({
        url: `${API_BASE}/api/course/${classId}/exams`,
        method: 'GET',
        headers: token
            ? { 'Authorization': `Bearer ${token}` }
            : {},
        success: function (data) {

            console.log("API SUCCESS - Exams:", data);

            if (!data || data.length === 0) {
                console.warn("API returned empty list");
            }

            $(document).trigger('classExamsLoaded', [data]);
        },
        error: function (xhr) {

            console.error("API ERROR");
            console.error("Status:", xhr.status);
            console.error("Response:", xhr.responseText);
        }
    });
}


/* ==============================
   LOAD CHAPTERS
============================== */

function loadClassChapters(classId) {

    const token = getAuthToken();

    console.log("Calling API:", `${API_BASE}/api/course/${classId}/chapters`);

    $.ajax({
        url: `${API_BASE}/api/course/${classId}/chapters`,
        method: 'GET',
        headers: token
            ? { 'Authorization': `Bearer ${token}` }
            : {},
        success: function (data) {

            console.log("API SUCCESS - Chapters:", data);

            $(document).trigger('classChaptersLoaded', [data]);
        },
        error: function (xhr) {

            console.error("Chapter API ERROR");
            console.error("Status:", xhr.status);
            console.error("Response:", xhr.responseText);
        }
    });
}
