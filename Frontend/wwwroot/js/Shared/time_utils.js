function formatDateTime(isoString, includeTime = true, fallback = '—') {
    if (!isoString) return fallback;
    let s = String(isoString).trim();
    if (s && !s.endsWith('Z') && !/[+-]\d{2}:\d{2}$/.test(s)) s += 'Z';
    const d = new Date(s);
    if (isNaN(d.getTime())) return fallback;

    const opts = { timeZone: "Asia/Ho_Chi_Minh" };
    if (includeTime) {
        opts.year = "numeric";
        opts.month = "2-digit";
        opts.day = "2-digit";
        opts.hour = "2-digit";
        opts.minute = "2-digit";
        return d.toLocaleString("vi-VN", opts);
    }
    return d.toLocaleDateString("vi-VN", opts);
}

// Aliases for backward compatibility
window.formatDate = (isoString) => formatDateTime(isoString, false, '--');
window.formatDateVN = formatDateTime;
