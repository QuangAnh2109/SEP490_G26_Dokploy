// Shared KaTeX renderer cho FIB question frames.
// Dùng chung cho teacher (preview readonly) và student (interactive blanks).
//
// Public API:
//   FibKatexRenderer.render(container, frameLatex, options)
//     options.mode: 'teacher' | 'student'    (REQUIRED)
//     options.onBlankClick: (blankId) => void (REQUIRED khi mode='student')
//     options.answeredMap: Map<string,string> (optional, student)
//     options.errorMap: Map<string,true>      (optional, student — blank không khớp inputType)
//     options.activeBlankId: string           (optional, student)
//     options.placeholder: string             (optional, HTML khi frame rỗng)

window.FibKatexRenderer = (() => {
    'use strict';

    const PLACEHOLDER_REGEX = /\\placeholder\[(\d+)\](?:\{\})?/g;
    const TEXT_COMMAND_REGEX = /(\\[a-zA-Z]+\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}|\\[a-zA-Z]+)/g;
    const MATH_DELIMITER_REGEX = /(\$\$[\s\S]*?\$\$|\$[\s\S]*?\$|\\\(.*?\\\)|\\\[.*?\\\])/g;

    const fieldHtml = (m, id) =>
        `\\htmlId{field-${id}}{\\fbox{\\phantom{\\text{..}}[${id}]\\phantom{\\text{..}}}}`;

    // ────────────────── Pipeline (port từ renderLatexInElement) ──────────────────
    const renderKatex = (previewBox, content, placeholderHtml) => {
        if (!content || !content.trim()) {
            previewBox.innerHTML = placeholderHtml ||
                "<p style='color:#ccc; font-style: italic;'>Nội dung trống...</p>";
            return false;
        }

        const raw = content.trim();
        const dlMatch = raw.match(/^\\displaylines\s*\{([\s\S]*)\}\s*$/);
        const s = dlMatch ? dlMatch[1].trim() : raw;

        const hasDelimiters = MATH_DELIMITER_REGEX.test(s);
        MATH_DELIMITER_REGEX.lastIndex = 0;

        try {
            if (!window.katex) {
                previewBox.innerHTML = `<pre>${content}</pre>`;
                return false;
            }

            if (hasDelimiters) {
                let lastIdx = 0;
                let processed = "";
                let match;

                while ((match = MATH_DELIMITER_REGEX.exec(s)) !== null) {
                    const before = s.substring(lastIdx, match.index);
                    processed += before
                        .replace(TEXT_COMMAND_REGEX, m => `$${m}$`)
                        .replace(PLACEHOLDER_REGEX, (m, id) => `$${fieldHtml(m, id)}$`);

                    processed += match[0].replace(PLACEHOLDER_REGEX, fieldHtml);
                    lastIdx = MATH_DELIMITER_REGEX.lastIndex;
                }

                const remaining = s.substring(lastIdx);
                processed += remaining
                    .replace(TEXT_COMMAND_REGEX, m => `$${m}$`)
                    .replace(PLACEHOLDER_REGEX, (m, id) => `$${fieldHtml(m, id)}$`);

                previewBox.innerHTML = processed;
                if (window.renderMathInElement) {
                    window.renderMathInElement(previewBox, {
                        delimiters: [
                            { left: '$$', right: '$$', display: true },
                            { left: '$', right: '$', display: false },
                            { left: '\\(', right: '\\)', display: false },
                            { left: '\\[', right: '\\]', display: true }
                        ],
                        trust: true,
                        strict: false
                    });
                } else {
                    window.katex.render(processed, previewBox, {
                        displayMode: true, trust: true, strict: false
                    });
                }
            } else {
                const processed = s.replace(PLACEHOLDER_REGEX, fieldHtml);
                const finalLatex = processed.includes('\\\\')
                    ? `\\begin{gathered}${processed}\\end{gathered}`
                    : processed;

                window.katex.render(finalLatex, previewBox, {
                    displayMode: true, trust: true, strict: false
                });
            }
            return true;
        } catch (e) {
            previewBox.innerHTML = `<span style="color:red">Lỗi LaTeX: ${e.message}</span>`;
            return false;
        }
    };

    // ────────────────── Post-process per mode ──────────────────
    const replaceFieldsTeacher = (container) => {
        container.querySelectorAll('[id^="field-"]').forEach(f => {
            const id = f.id.replace('field-', '');
            f.innerHTML = `<input type="text" class="katex-input" placeholder="${id}" readonly>`;
        });
    };

    const replaceFieldsStudent = (container, options) => {
        const { onBlankClick, answeredMap, errorMap, activeBlankId } = options;
        container.querySelectorAll('[id^="field-"]').forEach(span => {
            const blankId = span.id.replace('field-', '');
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.setAttribute('data-blank-id', blankId);
            btn.addEventListener('click', () => onBlankClick(blankId));
            btn.addEventListener('keydown', e => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    onBlankClick(blankId);
                }
            });

            const stored = answeredMap?.get(blankId);
            if (stored && stored.trim() !== '') {
                btn.className = 'fib-blank fib-blank-answered';
                try {
                    window.katex.render(stored, btn, {
                        throwOnError: false, trust: true, strict: false
                    });
                } catch (_) {
                    btn.textContent = stored;
                }
            } else {
                btn.className = 'fib-blank';
                btn.textContent = blankId;
            }

            if (errorMap?.has(blankId)) {
                btn.classList.add('fib-blank-error');
                btn.title = 'Đáp án không đúng định dạng yêu cầu';
            }

            if (activeBlankId === blankId) {
                btn.classList.add('fib-blank-active');
            }

            span.replaceWith(btn);
        });
    };

    // ────────────────── Public API ──────────────────
    const render = (container, frameLatex, options) => {
        if (!options || !['teacher', 'student'].includes(options.mode)) {
            throw new Error("FibKatexRenderer.render: options.mode must be 'teacher' or 'student'");
        }
        if (options.mode === 'student' && typeof options.onBlankClick !== 'function') {
            throw new Error("FibKatexRenderer.render: onBlankClick is required when mode='student'");
        }

        const ok = renderKatex(container, frameLatex, options.placeholder);
        if (!ok) return;

        if (options.mode === 'teacher') {
            replaceFieldsTeacher(container);
        } else {
            replaceFieldsStudent(container, options);
        }
    };

    return { render };
})();
