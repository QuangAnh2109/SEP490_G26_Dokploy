window.CurriculumShared = (function () {
    const ERROR_MAP = {
        'SEMESTER_CODE_DUPLICATE':     'Mã kỳ học đã tồn tại.',
        'SEMESTER_DATE_INVALID':       'Ngày kết thúc phải sau ngày bắt đầu.',
        'SEMESTER_NOT_FOUND':          'Không tìm thấy kỳ học.',
        'SEMESTER_ALREADY_CLOSED':     'Kỳ học này đã được đóng.',
        'SEMESTER_CONCURRENT_UPDATE':  'Dữ liệu đã thay đổi. Vui lòng tải lại trang.',
        'SEMESTER_CLOSED':             'Kỳ học đã đóng, không thể tạo lớp mới.',
        'SUBJECT_CODE_DUPLICATE':      'Mã môn học đã tồn tại.',
        'SUBJECT_NOT_FOUND':           'Không tìm thấy môn học.',
        'SUBJECT_ALREADY_CLOSED':      'Môn học này đã được đóng.',
        'SUBJECT_CONCURRENT_UPDATE':   'Dữ liệu đã thay đổi. Vui lòng tải lại trang.',
        'SUBJECT_CLOSED':              'Môn học đã đóng, không thể tạo lớp mới.',
        'CHAPTER_NAME_DUPLICATE':      'Tên chương đã tồn tại trong môn này.',
        'CHAPTER_NOT_FOUND':           'Không tìm thấy chương.',
        'CHAPTER_ALREADY_DELETED':     'Chương đã được xoá.',
        'CHAPTER_CONCURRENT_UPDATE':   'Dữ liệu đã thay đổi. Vui lòng tải lại trang.',
        'EXAM_TIME_OUT_OF_SEMESTER':   'Thời gian đề thi phải nằm trong khoảng kỳ học.',
    };

    function showNotice(kind, title, message) {
        const stage = document.getElementById('modal-stage-notice');
        if (!stage) return;
        
        const modal = stage.querySelector('.modal');
        modal.classList.remove('modal-success', 'modal-error');
        modal.classList.add(kind === 'error' ? 'modal-error' : 'modal-success');
        
        document.getElementById('notice-title').textContent = title;
        document.getElementById('notice-message').textContent = message;
        
        if (typeof openModal === 'function') {
            openModal('modal-stage-notice');
        } else {
            stage.classList.remove('hidden');
        }
    }

    function translateError(code, fallback) {
        return ERROR_MAP[code] || fallback || 'Có lỗi xảy ra. Vui lòng thử lại.';
    }



    return { showNotice, translateError };
})();
