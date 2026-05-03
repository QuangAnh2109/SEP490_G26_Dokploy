
$(function () {
    loadExamPreview();
});

function getAuthToken() {
    return sessionStorage.getItem('jwtToken') || localStorage.getItem('jwtToken') || '';
}

function loadExamPreview() {
    const token = getAuthToken();
    if (!token) {
        showError('Bạn chưa đăng nhập. Vui lòng đăng nhập để xem đề thi.');
        return;
    }

    apiClient.get(`/api/student/exams/${EXAM_ID}/preview`)
        .then(function (data) {
            renderPreview(data);
        })
        .catch(function (err) {
            const httpStatus = err.xhr ? err.xhr.status : null;
            if (httpStatus === 401) {
                showError('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
            } else if (httpStatus === 403) {
                showError('Bạn không có quyền xem đề thi này.');
            } else if (httpStatus === 404) {
                showError('Không tìm thấy đề thi.');
            } else {
                showError('Đã xảy ra lỗi khi tải thông tin đề thi. Vui lòng thử lại.');
            }
        });
}

function renderPreview(data) {
    $('#title-text').text(data.title || 'Đề thi');
    renderStatusBadge(data.status);

    $('#subject-code').text(data.subjectCode || '—');
    $('#exam-title-card').text(data.title || '—');
    $('#total-questions').text(data.totalQuestions ? `${data.totalQuestions} câu hỏi` : '—');
    $('#duration').text(data.duration ? `${data.duration} phút` : '—');
    $('#open-at').text(formatDateTime(data.openAt));
    $('#close-at').text(formatDateTime(data.closeAt));

    renderMatrix(data.blueprintMatrix);

    $('#teacher-name').text(data.teacherName || '—');
    $('#updated-at').text(formatDateTime(data.updatedAtUtc));
    $('#description').text(data.description || '—');

    const now = new Date();
    const openAt = data.openAt ? new Date(data.openAt) : null;
    const closeAt = data.closeAt ? new Date(data.closeAt) : null;
    const canTake = (!openAt || now >= openAt) && (!closeAt || now <= closeAt);

    if (canTake) {
        $('#btn-take-exam').prop('disabled', false).on('click', function () { goToExam(); });
    } else {
        const tooltip = openAt && now < openAt
            ? `Đề thi chưa mở. Mở lúc ${formatDateTime(data.openAt)}`
            : `Đề thi đã kết thúc.`;
        $('#btn-take-exam').prop('disabled', true).attr('title', tooltip);
    }

    // Hiển thị nội dung, ẩn loading
    $('#loading-state').addClass('d-none');
    $('#content-state').removeClass('d-none');
}

function renderStatusBadge(status) {
    const map = {
        'public': { label: 'Công khai', cls: 'badge-public' },
        'private': { label: 'Riêng tư', cls: 'badge-private' },
        'closed': { label: 'Đã đóng', cls: 'badge-closed' },
    };
    const info = map[status] || { label: status || '', cls: 'badge-unknown' };
    $('#status-badge').text(info.label).attr('class', `meta-badge ${info.cls}`);
}

function renderMatrix(matrix) {
    if (!matrix || matrix.length === 0) {
        $('#matrix-wrapper').addClass('d-none');
        $('#no-matrix').removeClass('d-none');
        return;
    }

    const totals = matrix.reduce((acc, row) => {
        acc.recognize += row.recognize || 0;
        acc.understand += row.understand || 0;
        acc.apply += row.apply || 0;
        acc.advancedApply += row.advancedApply || 0;
        acc.total += row.total || 0;
        return acc;
    }, { recognize: 0, understand: 0, apply: 0, advancedApply: 0, total: 0 });

    const rows = matrix.map(row => `
        <tr class="interactive-row">
            <th>${escapeHtml(row.chapterName)}</th>
            <td>${row.recognize}</td>
            <td>${row.understand}</td>
            <td>${row.apply}</td>
            <td>${row.advancedApply}</td>
            <td>${row.total}</td>
        </tr>`).join('');

    const totalRow = `
        <tr class="fw-bold">
            <th>Tổng</th>
            <td>${totals.recognize}</td>
            <td>${totals.understand}</td>
            <td>${totals.apply}</td>
            <td>${totals.advancedApply}</td>
            <td>${totals.total}</td>
        </tr>`;

    $('#matrix-body').html(rows + totalRow);
}

function goToExam() {
    const form = $('<form>', { method: 'POST', action: '/StudentExam/TakeExam' });
    form.append($('<input>', { type: 'hidden', name: 'examId', value: EXAM_ID }));
    form.append($('<input>', { type: 'hidden', name: 'paperId', value: 0 }));
    $('body').append(form);
    form.submit();
}

function showError(message) {
    $('#loading-state').addClass('d-none');
    $('#error-message').text(message);
    $('#error-state').removeClass('d-none');
}


function escapeHtml(str) {
    return $('<div>').text(str || '').html();
}
