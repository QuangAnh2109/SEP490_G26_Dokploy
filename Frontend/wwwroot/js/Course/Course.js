/* ==============================
   LOAD EXAMS BY CLASS
============================== */

function loadClassExams(classId) {
    apiClient.get(`/api/course/${classId}/exams`)
        .then(function (data) {

            console.log("API SUCCESS - Exams:", data);

            if (!data || data.length === 0) {
                console.warn("API returned empty list");
            }

            $(document).trigger('classExamsLoaded', [data]);
        })
        .catch(function (err) {

            console.error("API ERROR");
            console.error("Status:", err.xhr ? err.xhr.status : err.status);
            console.error("Response:", err.message);
        });
}


/* ==============================
   LOAD CHAPTERS
============================== */

function loadClassChapters(classId) {

    apiClient.get(`/api/course/${classId}/chapters`)
        .then(function (data) {

            console.log("API SUCCESS - Chapters:", data);

            $(document).trigger('classChaptersLoaded', [data]);
        })
        .catch(function (err) {

            console.error("Chapter API ERROR");
            console.error("Status:", err.xhr ? err.xhr.status : err.status);
            console.error("Response:", err.message);
        });
}
