// ══════════════════════════════════════════════════════
//  StudentAnalytics — Học sinh xem phân tích luyện tập
// ══════════════════════════════════════════════════════

var trendChart = null;
var chapterChart = null;
var difficultyChart = null;

document.addEventListener('DOMContentLoaded', async function () {
    await window.userReady;

    if (!isAuthenticated()) { window.location.href = '/Auth/Login'; return; }
    if (getUserRole() !== RoleIds.Student) {
        showError('Trang này chỉ dành cho Học sinh.');
        return;
    }

    ['pendingMenuItem', 'settingsMenuItem'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.style.display = 'none';
    });
    const studentAnalyticsMenu = document.getElementById("studentAnalyticsMenuItem");
    if (studentAnalyticsMenu) studentAnalyticsMenu.classList.remove("d-none");

    loadSidebarClassName();
    loadAnalytics();
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
                { text: 'Phân tích của tôi', url: null }
            ]);
        }
    } catch (_) { /* ignore */ }
}

// ── Load analytics data ──
async function loadAnalytics() {
    try {
        const data = await apiClient.get('/api/practice/analytics/me?classId=' + classId);
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

    // Stat cards
    document.getElementById('statTotalSessions').textContent = d.totalSessions;
    document.getElementById('statTotalQuestions').textContent = d.totalQuestionsAttempted;
    document.getElementById('statAccuracy').textContent =
        d.totalSessions > 0 ? d.overallAccuracyRate.toFixed(1) + '%' : '—';
    document.getElementById('statLastPractice').textContent =
        d.lastPracticeAt ? formatVNDateShort(d.lastPracticeAt) : '—';

    if (d.totalSessions === 0) {
        document.getElementById('noDataState').classList.remove('d-none');
        document.getElementById('chartsArea').classList.add('d-none');
        renderRecommendations(d.recommendations || []);
        return;
    }

    renderTrendChart(d.trendData || []);
    renderChapterChart(d.chapterStats || []);
    renderDifficultyChart(d.difficultyStats || []);
    renderChapterTable(d.chapterStats || []);
    renderRecommendations(d.recommendations || []);
}

// ── Trend Line Chart ──
function renderTrendChart(trend) {
    if (!trend.length) return;
    if (trendChart) trendChart.destroy();

    const ctx = document.getElementById('trendChart').getContext('2d');
    trendChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: trend.map(t => t.dateLabel),
            datasets: [{
                label: 'Tỉ lệ đúng (%)',
                data: trend.map(t => t.accuracyRate),
                borderColor: '#0d6efd',
                backgroundColor: 'rgba(13,110,253,0.1)',
                tension: 0.3,
                fill: true,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => 'Tỉ lệ đúng: ' + ctx.parsed.y.toFixed(1) + '%'
                    }
                }
            },
            scales: {
                y: {
                    min: 0, max: 100,
                    ticks: { callback: v => v + '%' },
                    grid: { color: 'rgba(0,0,0,0.05)' }
                },
                x: { grid: { display: false } }
            }
        }
    });
}

// ── Chapter Horizontal Bar Chart ──
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

// ── Difficulty Radar/Bar Chart ──
function renderDifficultyChart(diffs) {
    if (!diffs.length) return;
    if (difficultyChart) difficultyChart.destroy();

    const ctx = document.getElementById('difficultyChart').getContext('2d');
    const labels = diffs.map(d => d.difficultyName);
    const values = diffs.map(d => d.accuracyRate || 0);
    const colors = values.map(v => v < 50 ? 'rgba(220,53,69,0.8)' : v < 80 ? 'rgba(255,193,7,0.8)' : 'rgba(25,135,84,0.8)');

    difficultyChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels.map((l, i) => l + ' (' + values[i].toFixed(1) + '%)'),
            datasets: [{
                data: diffs.map(d => d.totalAttempted),
                backgroundColor: ['#0d6efd', '#6f42c1', '#fd7e14', '#dc3545'],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { position: 'bottom' },
                tooltip: {
                    callbacks: {
                        label: function (ctx) {
                            const d = diffs[ctx.dataIndex];
                            return ctx.label.split(' (')[0] + ': ' + d.totalAttempted +
                                ' câu (' + d.accuracyRate.toFixed(1) + '% đúng)';
                        }
                    }
                }
            }
        }
    });
}

// ── Chapter Table ──
function renderChapterTable(chapters) {
    const tbody = document.getElementById('chapterTableBody');
    if (!chapters.length) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">Chưa có dữ liệu.</td></tr>';
        return;
    }

    tbody.innerHTML = chapters.map(ch => {
        const levelClass = ch.proficiencyLevel === 'Yếu' ? 'bg-danger'
            : ch.proficiencyLevel === 'Trung bình' ? 'bg-warning text-dark' : 'bg-success';
        const acc = ch.accuracyRate !== undefined ? ch.accuracyRate : 0;

        return '<tr>' +
            '<td><strong>' + escapeHtml(ch.chapterName) + '</strong></td>' +
            '<td class="text-center">' + ch.totalAttempted + '</td>' +
            '<td class="text-center">' + ch.correctCount + '</td>' +
            '<td class="text-center">' +
                '<div class="pa-progress-wrap">' +
                    '<div class="progress pa-progress">' +
                        '<div class="progress-bar ' + getBarClass(acc) + '" style="width:' + acc + '%"></div>' +
                    '</div>' +
                    '<span class="pa-progress-label">' + acc.toFixed(1) + '%</span>' +
                '</div>' +
            '</td>' +
            '<td class="text-center"><span class="badge ' + levelClass + '">' + escapeHtml(ch.proficiencyLevel) + '</span></td>' +
            '</tr>';
    }).join('');
}

// ── Recommendations ──
function renderRecommendations(recs) {
    const box = document.getElementById('recommendationsList');
    if (!recs.length) {
        box.innerHTML = '<p class="text-muted mb-0">Chưa có đủ dữ liệu để đưa ra gợi ý.</p>';
        return;
    }
    box.innerHTML = recs.map(r =>
        '<div class="pa-rec-item">' + escapeHtml(r) + '</div>'
    ).join('');
}

// ── Helpers ──
function getBarClass(acc) {
    return acc < 50 ? 'bg-danger' : acc < 70 ? 'bg-warning' : 'bg-success';
}

function formatVNDate(dateStr) {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleString('vi-VN', {
        timeZone: 'Asia/Ho_Chi_Minh',
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit'
    });
}

function formatVNDateShort(dateStr) {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('vi-VN', {
        timeZone: 'Asia/Ho_Chi_Minh',
        day: '2-digit', month: '2-digit'
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
