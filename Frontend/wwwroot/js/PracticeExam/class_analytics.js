// ═══════════════════════════════════════════════════
//  ClassAnalytics — Giáo viên xem phân tích luyện tập
// ═══════════════════════════════════════════════════

var allStudentStats = [];
var chapterChart = null;

document.addEventListener('DOMContentLoaded', async function () {
    await window.userReady;

    if (!isAuthenticated()) { window.location.href = '/Auth/Login'; return; }
    if (getUserRole() !== RoleIds.Teacher) {
        showError('Trang này chỉ dành cho Giáo viên.');
        return;
    }

    loadSidebarClassName();
    loadAnalytics();

    document.getElementById('studentSearch').addEventListener('input', function () {
        filterStudents(this.value.trim().toLowerCase());
    });
});

// ── Sidebar class name ──
async function loadSidebarClassName() {
    try {
        const data = await apiClient.get('/api/class/' + classId + '/settings');
        const name = data.className || 'Lớp ' + classId;
        const el = document.getElementById('sidebarClassName');
        if (el) el.textContent = name;

        if (typeof setBreadcrumb === 'function') {
            setBreadcrumb([
                { text: 'Lớp học', url: '/Class/ClassList' },
                { text: name, url: '/Class/ExamListInClass/' + classId },
                { text: 'Phân tích luyện tập', url: null }
            ]);
        }
    } catch (_) { /* ignore */ }
}

// ── Load analytics data ──
async function loadAnalytics() {
    try {
        const data = await apiClient.get('/api/practice/analytics/class/' + classId);
        render(data);
    } catch (err) {
        showError(err.message || 'Lỗi không xác định.');
    }
}

// ── Main render ──
function render(d) {
    document.getElementById('analyticsLoading').classList.add('d-none');
    document.getElementById('analyticsContent').classList.remove('d-none');

    document.getElementById('headerSubject').textContent = d.subjectName || '';
    document.getElementById('statTotalStudents').textContent = d.totalStudents;
    document.getElementById('statActiveStudents').textContent = d.activeStudents;
    document.getElementById('statTotalSessions').textContent = d.totalSessions;
    document.getElementById('statAvgAccuracy').textContent =
        d.activeStudents > 0 ? d.averageAccuracyRate.toFixed(1) + '%' : '—';

    renderRecommendations(d.recommendations || []);
    renderChapterTable(d.chapterStats || []);
    renderChapterChart(d.chapterStats || []);

    allStudentStats = d.studentStats || [];
    renderStudentTable(allStudentStats);
}

// ── Recommendations ──
function renderRecommendations(recs) {
    const box = document.getElementById('recommendationsList');
    if (!recs.length) {
        box.innerHTML = '<p class="text-muted mb-0">Chưa có đủ dữ liệu để đưa ra đề xuất.</p>';
        return;
    }
    box.innerHTML = recs.map(r =>
        '<div class="pa-rec-item">' + escapeHtml(r) + '</div>'
    ).join('');
}

// ── Chapter Table ──
function renderChapterTable(chapters) {
    const tbody = document.getElementById('chapterTableBody');
    const empty = document.getElementById('chapterEmpty');

    if (!chapters.length) {
        empty.classList.remove('d-none');
        document.getElementById('chapterTable').classList.add('d-none');
        return;
    }
    empty.classList.add('d-none');
    document.getElementById('chapterTable').classList.remove('d-none');

    tbody.innerHTML = chapters.map(ch => {
        const acc = ch.accuracyRate !== undefined ? ch.accuracyRate : 0;
        const statusClass = ch.status === 'Báo động' ? 'bg-danger'
            : ch.status === 'Cần chú ý' ? 'bg-warning text-dark' : 'bg-success';
        return '<tr>' +
            '<td><strong>' + escapeHtml(ch.chapterName) + '</strong></td>' +
            '<td class="text-center">' + ch.studentPracticed + '</td>' +
            '<td class="text-center">' + ch.totalAttempts + '</td>' +
            '<td class="text-center">' +
                '<div class="pa-progress-wrap">' +
                    '<div class="progress pa-progress">' +
                        '<div class="progress-bar ' + getAccuracyBarClass(acc) + '" style="width:' + acc + '%"></div>' +
                    '</div>' +
                    '<span class="pa-progress-label">' + acc.toFixed(1) + '%</span>' +
                '</div>' +
            '</td>' +
            '<td class="text-center"><span class="badge ' + statusClass + '">' + escapeHtml(ch.status) + '</span></td>' +
            '</tr>';
    }).join('');
}

// ── Chapter Bar Chart ──
function renderChapterChart(chapters) {
    if (!chapters.length) return;
    if (chapterChart) chapterChart.destroy();

    const ctx = document.getElementById('chapterChart').getContext('2d');
    const labels = chapters.map(c => c.chapterName);
    const values = chapters.map(c => c.accuracyRate || 0);
    const colors = values.map(v => v < 50 ? 'rgba(220,53,69,0.8)' : v < 80 ? 'rgba(255,193,7,0.8)' : 'rgba(25,135,84,0.8)');

    chapterChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Tỉ lệ đúng (%)',
                data: values,
                backgroundColor: colors,
                borderRadius: 4,
                maxBarThickness: 60
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => ctx.parsed.y.toFixed(1) + '%'
                    }
                }
            },
            scales: {
                y: { min: 0, max: 100, ticks: { callback: v => v + '%' } }
            }
        }
    });
}

// ── Student Table ──
function renderStudentTable(students) {
    const tbody = document.getElementById('studentTableBody');
    const empty = document.getElementById('studentEmpty');

    if (!students.length) {
        empty.classList.remove('d-none');
        document.getElementById('studentTable').classList.add('d-none');
        return;
    }
    empty.classList.add('d-none');
    document.getElementById('studentTable').classList.remove('d-none');

    tbody.innerHTML = students.map((s, idx) => {
        const levelClass = s.proficiencyLevel === 'Yếu' ? 'bg-danger'
            : s.proficiencyLevel === 'Trung bình' ? 'bg-warning text-dark'
            : s.proficiencyLevel === 'Mạnh' ? 'bg-success'
            : 'bg-secondary';

        const lastPractice = s.lastPracticeAt ? formatVNDate(s.lastPracticeAt) : '—';
        const acc = s.totalSessions > 0 ? s.accuracyRate.toFixed(1) + '%' : '—';

        const chapterRows = (s.chapterBreakdown || []).map(ch =>
            '<tr class="pa-chapter-detail-row">' +
            '<td colspan="2" class="ps-4 text-muted small">' + escapeHtml(ch.chapterName) + '</td>' +
            '<td class="text-center small">' + ch.totalAttempted + '</td>' +
            '<td class="text-center small">' + ch.correctCount + '</td>' +
            '<td class="text-center small">' + ch.accuracyRate.toFixed(1) + '%</td>' +
            '<td class="text-center"><span class="badge badge-sm ' +
                (ch.proficiencyLevel === 'Yếu' ? 'bg-danger' : ch.proficiencyLevel === 'Trung bình' ? 'bg-warning text-dark' : 'bg-success') +
                '">' + escapeHtml(ch.proficiencyLevel) + '</span></td>' +
            '<td></td><td></td>' +
            '</tr>'
        ).join('');

        return '<tr class="pa-student-row" data-idx="' + idx + '">' +
            '<td>' + escapeHtml(s.studentCode) + '</td>' +
            '<td><strong>' + escapeHtml(s.studentName) + '</strong></td>' +
            '<td class="text-center">' + s.totalSessions + '</td>' +
            '<td class="text-center">' + s.totalQuestionsAttempted + '</td>' +
            '<td class="text-center">' + acc + '</td>' +
            '<td class="text-center"><span class="badge ' + levelClass + '">' + escapeHtml(s.proficiencyLevel) + '</span></td>' +
            '<td class="text-center"><small>' + lastPractice + '</small></td>' +
            '<td class="text-center">' +
                (s.chapterBreakdown && s.chapterBreakdown.length
                    ? '<button class="btn btn-sm btn-outline-secondary pa-expand-btn" data-idx="' + idx + '">' +
                      '<i class="fas fa-chevron-down"></i></button>'
                    : '<span class="text-muted">—</span>') +
            '</td>' +
            '</tr>' +
            (s.chapterBreakdown && s.chapterBreakdown.length
                ? '<tr class="pa-detail-rows d-none" data-for="' + idx + '">' +
                  '<td colspan="8" class="p-0"><table class="table table-sm mb-0"><thead class="table-light"><tr>' +
                  '<th class="ps-4">Chương</th><th></th><th class="text-center">Câu</th>' +
                  '<th class="text-center">Đúng</th><th class="text-center">Tỉ lệ</th>' +
                  '<th class="text-center">Mức độ</th><th></th><th></th></tr></thead>' +
                  '<tbody>' + chapterRows + '</tbody></table></td></tr>'
                : '');
    }).join('');

    // Toggle expand
    tbody.querySelectorAll('.pa-expand-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const idx = this.dataset.idx;
            const detailRow = tbody.querySelector('.pa-detail-rows[data-for="' + idx + '"]');
            if (detailRow) {
                detailRow.classList.toggle('d-none');
                const icon = this.querySelector('i');
                icon.classList.toggle('fa-chevron-down');
                icon.classList.toggle('fa-chevron-up');
            }
        });
    });
}

// ── Filter students ──
function filterStudents(keyword) {
    if (!keyword) { renderStudentTable(allStudentStats); return; }
    const filtered = allStudentStats.filter(s =>
        (s.studentName || '').toLowerCase().includes(keyword) ||
        (s.studentCode || '').toLowerCase().includes(keyword)
    );
    renderStudentTable(filtered);
}

// ── Helpers ──
function getAccuracyBarClass(acc) {
    return acc < 50 ? 'bg-danger' : acc < 70 ? 'bg-warning' : 'bg-success';
}

function formatVNDate(dateStr) {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleString('vi-VN', {
        timeZone: 'Asia/Ho_Chi_Minh',
        year: 'numeric', month: '2-digit', day: '2-digit'
    });
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

function showError(msg) {
    document.getElementById('analyticsLoading').classList.add('d-none');
    document.getElementById('analyticsError').classList.remove('d-none');
    document.getElementById('errorMessage').textContent = msg;
}
