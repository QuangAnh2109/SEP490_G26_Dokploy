/* ═══════════════════════════════════════════
   Practice Exam — Setup Page JS (Logic Only)
   ═══════════════════════════════════════════ */

let chaptersData = [];
let selectedChapters = new Set();

const MAX_QUESTIONS = 30;
const MIN_QUESTIONS = 5;

const DIFFICULTY_CONFIG = [
    { level: 1, name: 'Nhận biết', colorClass: 'diff-easy', color: '#198754' },
    { level: 2, name: 'Thông hiểu', colorClass: 'diff-medium', color: '#0d6efd' },
    { level: 3, name: 'Vận dụng', colorClass: 'diff-hard', color: '#fd7e14' },
    { level: 4, name: 'Vận dụng cao', colorClass: 'diff-expert', color: '#dc3545' }
];

document.addEventListener('DOMContentLoaded', () => {
    sessionStorage.setItem('currentPracticeClassId', practiceClassId);
    loadChapters();
    loadInProgressHistory();
    setupSlider();
});

// ── Slider Setup ──────────────────────────────────────────
function setupSlider() {
    const slider = document.getElementById('questionCountSlider');
    const input = document.getElementById('questionCountInput');
    slider.addEventListener('input', () => {
        input.value = slider.value;
        updateDifficultyPreview();
    });
    input.addEventListener('change', () => {
        let v = Math.max(MIN_QUESTIONS, Math.min(MAX_QUESTIONS, parseInt(input.value) || 10));
        input.value = v;
        slider.value = v;
        updateDifficultyPreview();
    });
}

// ── Load Chapters ─────────────────────────────────────────
async function loadChapters() {

    try {
        chaptersData = await apiClient.get('/api/practice/class/' + practiceClassId + '/chapters');
        renderChapters(chaptersData);
        document.getElementById('setupLoading').classList.add('d-none');
        document.getElementById('setupContent').classList.remove('d-none');
        updateDifficultyPreview();
    } catch (err) {
        const httpStatus = err.xhr ? err.xhr.status : null;
        if (httpStatus === 401) { window.location.href = '/Auth/Login'; return; }
        if (httpStatus === 404) {
            showToast('Không tìm thấy Lớp học hoặc bạn không thuộc lớp này.', 'error');
            return;
        }
        showToast('Lỗi tải dữ liệu: ' + err.message, 'error');
    }
}

// ── Render Chapters ───────────────────────────────────────
function renderChapters(chapters) {
    var grid = document.getElementById('chapterGrid');
    var template = document.getElementById('chapterItemTemplate');
    grid.innerHTML = '';

    if (!chapters || chapters.length === 0) {
        var p = document.createElement('p');
        p.className = 'text-muted';
        p.textContent = 'Không có chương nào.';
        grid.appendChild(p);
        return;
    }

    chapters.forEach(function (ch) {
        var rate = ch.overallAccuracyRate;
        var barClass = rate < 50 ? 'weak' : rate < 80 ? 'medium' : 'strong';
        var rateText = ch.totalAttempted > 0 ? rate + '%' : 'Chưa làm';

        var clone = template.content.cloneNode(true);
        var item = clone.querySelector('.chapter-item');

        item.dataset.chapterId = ch.chapterId;
        clone.querySelector('.chapter-name').textContent = ch.chapterName;
        clone.querySelector('.chapter-rate').textContent = rateText;
        clone.querySelector('.chapter-available').textContent = ch.availableQuestions + ' câu';

        var fill = clone.querySelector('.proficiency-fill');
        fill.classList.add(barClass);
        fill.style.width = (ch.totalAttempted > 0 ? rate : 0) + '%';

        item.addEventListener('click', function () { toggleChapter(item, ch.chapterId); });
        grid.appendChild(clone);
    });

    updateAvailableCount();
}

// ── Toggle Chapter ────────────────────────────────────────
function toggleChapter(el, chapterId) {
    if (selectedChapters.has(chapterId)) {
        selectedChapters.delete(chapterId);
        el.classList.remove('selected');
    } else {
        selectedChapters.add(chapterId);
        el.classList.add('selected');
    }
    updateAvailableCount();
    updateDifficultyPreview();
}

// ── Available Count ───────────────────────────────────────
function updateAvailableCount() {
    var total = 0;
    chaptersData.forEach(function (ch) {
        if (selectedChapters.size === 0 || selectedChapters.has(ch.chapterId)) {
            total += ch.availableQuestions;
        }
    });
    document.getElementById('availableCount').textContent = total;

    var slider = document.getElementById('questionCountSlider');
    var input = document.getElementById('questionCountInput');
    var max = Math.min(MAX_QUESTIONS, total);
    slider.max = max;
    if (parseInt(input.value) > max) {
        input.value = max;
        slider.value = max;
    }
}

// ── Difficulty Preview (Auto-Allocation) ──────────────────
function computeDifficultyAllocation() {
    // Tính proficiency cho từng mức độ dựa trên các chương đã chọn
    var diffStats = [];
    DIFFICULTY_CONFIG.forEach(function (dc) {
        var totalAttempted = 0;
        var totalCorrect = 0;
        var totalAvailable = 0;

        chaptersData.forEach(function (ch) {
            if (selectedChapters.size === 0 || selectedChapters.has(ch.chapterId)) {
                if (ch.difficultyBreakdown) {
                    var db = ch.difficultyBreakdown.find(function (d) { return d.difficulty === dc.level; });
                    if (db) {
                        totalAttempted += db.totalAttempted || 0;
                        totalCorrect += db.correctCount || 0;
                        totalAvailable += db.availableQuestions || 0;
                    }
                }
            }
        });

        var accuracyRate = totalAttempted > 0
            ? Math.round(totalCorrect / totalAttempted * 1000) / 10
            : -1; // -1 = chưa làm bao giờ

        diffStats.push({
            level: dc.level,
            name: dc.name,
            colorClass: dc.colorClass,
            color: dc.color,
            accuracyRate: accuracyRate,
            totalAttempted: totalAttempted,
            totalAvailable: totalAvailable
        });
    });

    // Tính phân bổ tỉ lệ
    var totalQuestions = parseInt(document.getElementById('questionCountInput').value) || 10;
    var hasHistory = diffStats.some(function (d) { return d.totalAttempted > 0; });

    var allocation = [];
    if (!hasHistory) {
        // Chưa có lịch sử → chia đều 4 mức độ
        var base = Math.floor(totalQuestions / 4);
        var remainder = totalQuestions - base * 4;
        diffStats.forEach(function (d, idx) {
            var count = base + (idx < remainder ? 1 : 0);
            allocation.push(Object.assign({}, d, { allocatedCount: count }));
        });
    } else {
        // Có lịch sử → mức yếu được ưu tiên nhiều hơn
        // Phân loại: yếu (< 50%), trung bình (50-80%), mạnh (>= 80%), chưa làm (-1)
        var weights = diffStats.map(function (d) {
            if (d.totalAttempted === 0) return 2.0;  // Chưa làm → coi như cần luyện
            if (d.accuracyRate < 50) return 3.0;  // Yếu
            if (d.accuracyRate < 80) return 2.0;  // Trung bình
            return 1.0;                               // Mạnh
        });

        var totalWeight = weights.reduce(function (s, w) { return s + w; }, 0);
        var allocated = 0;
        var allocRaw = [];

        weights.forEach(function (w, idx) {
            var raw = totalQuestions * w / totalWeight;
            allocRaw.push({ idx: idx, raw: raw, floor: Math.floor(raw), frac: raw - Math.floor(raw) });
        });

        // Phân bổ phần nguyên trước
        var sumFloor = allocRaw.reduce(function (s, a) { return s + a.floor; }, 0);
        var leftover = totalQuestions - sumFloor;

        // Sắp xếp theo phần dư giảm dần để phân bổ phần dư
        allocRaw.sort(function (a, b) { return b.frac - a.frac; });
        allocRaw.forEach(function (a) {
            a.final = a.floor;
            if (leftover > 0) {
                a.final += 1;
                leftover--;
            }
        });

        // Sắp lại theo idx
        allocRaw.sort(function (a, b) { return a.idx - b.idx; });

        allocRaw.forEach(function (a, idx) {
            allocation.push(Object.assign({}, diffStats[idx], { allocatedCount: a.final }));
        });
    }

    return allocation;
}

function updateDifficultyPreview() {
    var container = document.getElementById('difficultyPreview');
    if (!container) return;

    var allocation = computeDifficultyAllocation();
    var totalQuestions = parseInt(document.getElementById('questionCountInput').value) || 10;

    var html = '<div class="diff-preview-items">';
    allocation.forEach(function (d) {
        var pct = totalQuestions > 0 ? Math.round(d.allocatedCount / totalQuestions * 100) : 0;
        var profLabel = '';
        if (d.totalAttempted > 0) {
            if (d.accuracyRate < 50) profLabel = '<span class="badge bg-danger ms-2">Yếu</span>';
            else if (d.accuracyRate < 80) profLabel = '<span class="badge bg-warning text-dark ms-2">TB</span>';
            else profLabel = '<span class="badge bg-success ms-2">Tốt</span>';
        } else {
            profLabel = '<span class="badge bg-secondary ms-2">Chưa làm</span>';
        }

        html += '<div class="diff-preview-item">';
        html += '  <div class="diff-preview-header">';
        html += '    <span class="diff-preview-name ' + d.colorClass + '-text">' + d.name + '</span>';
        html += '    ' + profLabel;
        html += '  </div>';
        html += '  <div class="diff-preview-bar-wrapper">';
        html += '    <div class="diff-preview-bar ' + d.colorClass + '-bg" style="width: ' + pct + '%;"></div>';
        html += '  </div>';
        html += '  <div class="diff-preview-count">' + d.allocatedCount + ' câu (' + pct + '%)</div>';
        html += '</div>';
    });
    html += '</div>';

    container.innerHTML = html;
}

// ── Load In-Progress History ──────────────────────────────
async function loadInProgressHistory() {
    try {
        var history = await apiClient.get('/api/practice/history?classId=' + practiceClassId);
        var inProgress = history.filter(function (h) { return h.status === 'Đang làm'; });

        if (inProgress.length > 0) {
            var section = document.getElementById('inProgressSection');
            section.classList.remove('d-none');

            var list = document.getElementById('inProgressList');
            var template = document.getElementById('inProgressItemTemplate');
            list.innerHTML = '';

            inProgress.forEach(function (h) {
                var d = new Date(h.createdAtUtc);
                var fmt = d.toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' });

                var clone = template.content.cloneNode(true);
                clone.querySelector('.history-chapter-names').textContent = h.chapterNames.join(', ');
                clone.querySelector('.history-detail').textContent = h.totalQuestions + ' câu — Bắt đầu: ' + fmt;
                clone.querySelector('.history-resume-link').href = '/PracticeExam/TakePractice?submissionId=' + h.submissionId;

                list.appendChild(clone);
            });
        }
    } catch (err) {
        console.error('Error loading history', err);
    }
}

// ── Create Practice Exam ──────────────────────────────────
async function createPracticeExam() {
    if (selectedChapters.size === 0) {
        showToast('Vui lòng chọn ít nhất 1 chương.', 'error');
        return;
    }

    var totalQuestions = parseInt(document.getElementById('questionCountInput').value);
    if (isNaN(totalQuestions) || totalQuestions < MIN_QUESTIONS || totalQuestions > MAX_QUESTIONS) {
        showToast('Số câu hỏi phải từ ' + MIN_QUESTIONS + ' đến ' + MAX_QUESTIONS + '.', 'error');
        return;
    }

    var btn = document.getElementById('btnCreatePractice');
    btn.disabled = true;

    var spinner = document.createElement('span');
    spinner.className = 'spinner-border spinner-border-sm me-1';
    btn.textContent = '';
    btn.appendChild(spinner);
    btn.appendChild(document.createTextNode(' Đang tạo đề...'));

    try {
        var body = {
            classId: practiceClassId,
            chapterIds: Array.from(selectedChapters),
            totalQuestions: totalQuestions
        };

        // Không gửi difficultyLevels — backend sẽ tự phân bổ

        var data = await apiClient.post('/api/practice/create', body);
        window.location.href = '/PracticeExam/TakePractice?submissionId=' + data.submissionId;
    } catch (err) {
        showToast(err.message, 'error');
        btn.disabled = false;
        btn.textContent = '';
        var icon = document.createElement('i');
        icon.className = 'fas fa-play me-1';
        btn.appendChild(icon);
        btn.appendChild(document.createTextNode(' Bắt đầu luyện tập'));
    }
}
