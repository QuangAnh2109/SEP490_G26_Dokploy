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

    $.ajax({
        url: `${API_BASE}/api/student/exams/${EXAM_ID}/preview`,
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        success: function (data) {
            renderPreview(data);
        },
        error: function (xhr) {
            if (xhr.status === 401) {
                showError('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
            } else if (xhr.status === 403) {
                showError('Bạn không có quyền xem đề thi này.');
            } else if (xhr.status === 404) {
                showError('Không tìm thấy đề thi.');
            } else {
                showError('Đã xảy ra lỗi khi tải thông tin đề thi. Vui lòng thử lại.');
            }
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
        $('#btn-take-exam').prop('disabled', false).off('click').on('click', function () { goToExam(); });
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
    const $tbody = $('#matrix-body');
    $tbody.empty();

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

    const rowT = document.getElementById('matrixRowTemplate');
    matrix.forEach(row => {
        const tr = rowT.content.cloneNode(true).firstElementChild;
        tr.querySelector('[data-field-chapter]').textContent = row.chapterName || '';
        tr.querySelector('[data-field-recognize]').textContent = row.recognize || 0;
        tr.querySelector('[data-field-understand]').textContent = row.understand || 0;
        tr.querySelector('[data-field-apply]').textContent = row.apply || 0;
        tr.querySelector('[data-field-advanced]').textContent = row.advancedApply || 0;
        tr.querySelector('[data-field-total]').textContent = row.total || 0;
        $tbody.append(tr);
    });

    const totalT = document.getElementById('matrixTotalRowTemplate');
    const totalRow = totalT.content.cloneNode(true).firstElementChild;
    totalRow.querySelector('[data-field-recognize]').textContent = totals.recognize;
    totalRow.querySelector('[data-field-understand]').textContent = totals.understand;
    totalRow.querySelector('[data-field-apply]').textContent = totals.apply;
    totalRow.querySelector('[data-field-advanced]').textContent = totals.advancedApply;
    totalRow.querySelector('[data-field-total]').textContent = totals.total;
    $tbody.append(totalRow);
}

function goToExam() {
    window.location.href = `/StudentExam/TakeExam?examId=${EXAM_ID}`;
}

function showError(message) {
    $('#loading-state').addClass('d-none');
    $('#error-message').text(message);
    $('#error-state').removeClass('d-none');
}

function formatDateTime(isoString) {
    if (!isoString) return '—';
    const d = new Date(isoString);
    if (isNaN(d.getTime())) return '—';
    const pad = n => String(n).padStart(2, '0');
    return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
