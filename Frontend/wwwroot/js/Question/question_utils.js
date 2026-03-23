window.QuestionEditorUtils = (() => {
    'use strict';
    const toArray = (v) => Array.from(v || []);

    const getMathValue = (f) => {
        if (!f) {
            return '';
        }
        if (typeof f.getValue === 'function') {
            const l = f.getValue('latex');
            return typeof l === 'string' ? l : '';
        }
        if (typeof f.value === 'string') {
            return f.value;
        }
        return f.textContent || '';
    };

    const setMathValue = (f, v) => {
        if (!f) {
            return;
        }
        const target = (v || '');
        if (typeof f.setValue === 'function') {
            // Normalize for comparison: remove all whitespace AND ensure we compare with what MathLive WOULD return
            const current = (f.getValue('latex') || '').replace(/\s+/g, '');
            const targetNorm = target.replace(/\s+/g, '');
            if (current === targetNorm) {
                return;
            }

            // Ensure no hidden text nodes exist that MathLive might pick up
            while (f.firstChild) {
                f.removeChild(f.firstChild);
            }
            f.setValue(target, {
                silenceNotifications: true
            });
            return;
        }
        if (typeof f.value === 'string') {
            if (f.value === target) {
                return;
            }
            f.value = target;
            return;
        }
        if (f.textContent === target) {
            return;
        }
        f.textContent = target;
    };

    const getNumberedPlaceholders = (latex) => {
        if (typeof latex !== 'string' || !latex.trim()) {
            return [];
        }
        // Match \placeholder[n] but ignore those without [n]
        // Using global flag with matchAll
        const regex = /\\placeholder\s*\[(\d+)\]/g;
        const nums = new Set();
        let match;
        while ((match = regex.exec(latex)) !== null) {
            nums.add(parseInt(match[1], 10));
        }
        return Array.from(nums).sort((a, b) => a - b);
    };

    const getFrameLatex = (item) => {
        if (item._frameEditor) {
            return item._frameEditor.getValue();
        }
        const raw = item.querySelector('[data-frame-raw]');
        if (raw && !raw.classList.contains('d-none')) {
            return raw.value;
        }
        const editor = item.querySelector('[data-frame-editor]');
        return getMathValue(editor);
    };





    const parseLatexSegments = (latex) => {
        if (!latex || !latex.trim()) {
            return [];
        }
        let s = latex.trim();
        const dlMatch = s.match(/^\\displaylines\s*\{([\s\S]*)\}\s*$/);
        const inner = dlMatch ? dlMatch[1].trim() : s;

        const segments = [];
        let current = "";
        let envDepth = 0;
        let braceDepth = 0;

        for (let i = 0; i < inner.length; i++) {
            const char = inner[i];
            if (char === '\\') {
                if (inner.startsWith("begin", i + 1)) {
                    envDepth++;
                } else if (inner.startsWith("end", i + 1)) {
                    envDepth = Math.max(0, envDepth - 1);
                }
            }
            if (char === '{' && (i === 0 || inner[i - 1] !== '\\')) {
                braceDepth++;
            } else if (char === '}' && (i === 0 || inner[i - 1] !== '\\')) {
                braceDepth = Math.max(0, braceDepth - 1);
            }

            if (char === '\\' && inner[i + 1] === '\\' && envDepth === 0 && braceDepth === 0) {
                const trimmed = current.trim();
                if (trimmed) {
                    segments.push(trimmed);
                }
                current = "";
                i++;
                continue;
            }
            current += char;
        }
        const last = current.trim();
        if (last) {
            segments.push(last);
        }
        return segments.map((content, i) => ({
            index: i,
            content
        }));
    };

    const renderLatexInElement = (previewBox, content, options = {}) => {
        if (!content || !content.trim()) {
            previewBox.innerHTML = options.placeholder || "<p style='color:#ccc; font-style: italic;'>Nội dung trống...</p>";
            return;
        }

        let raw = content.trim();
        // Robust strip \displaylines{ ... }
        const dlMatch = raw.match(/^\\displaylines\s*\{([\s\S]*)\}\s*$/);
        const s = dlMatch ? dlMatch[1].trim() : raw;

        const mathRegex = /(\$\$[\s\S]*?\$\$|\$[\s\S]*?\$|\\\(.*?\\\)|\\\[.*?\\\])/g;
        const hasDelimiters = mathRegex.test(s);

        try {
            if (window.katex) {
                if (hasDelimiters) {
                    let lastIdx = 0;
                    let processed = "";
                    let match;
                    mathRegex.lastIndex = 0;

                    while ((match = mathRegex.exec(s)) !== null) {
                        let before = s.substring(lastIdx, match.index);
                        processed += before.replace(/(\\[a-zA-Z]+\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}|\\[a-zA-Z]+)/g, (m) => `$${m}$`)
                            .replace(/\\placeholder\[(\d+)\](?:\{\})?/g, (m, id) => {
                                return `$\\htmlId{field-${id}}{\\fbox{\\phantom{\\text{..}}[${id}]\\phantom{\\text{..}}}}$`;
                            });

                        let mathBlock = match[0];
                        processed += mathBlock.replace(/\\placeholder\[(\d+)\](?:\{\})?/g, (m, id) => {
                            return `\\htmlId{field-${id}}{\\fbox{\\phantom{\\text{..}}[${id}]\\phantom{\\text{..}}}}`;
                        });
                        lastIdx = mathRegex.lastIndex;
                    }

                    let remaining = s.substring(lastIdx);
                    processed += remaining.replace(/(\\[a-zA-Z]+\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}|\\[a-zA-Z]+)/g, (m) => `$${m}$`)
                        .replace(/\\placeholder\[(\d+)\](?:\{\})?/g, (m, id) => {
                            return `$\\htmlId{field-${id}}{\\fbox{\\phantom{\\text{..}}[${id}]\\phantom{\\text{..}}}}$`;
                        });

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
                        window.katex.render(processed, previewBox, { displayMode: true, trust: true, strict: false });
                    }
                } else {
                    // Pure math mode or no delimiters (typical for old DB content)
                    let processed = s.replace(/\\placeholder\[(\d+)\](?:\{\})?/g, (match, id) => {
                        return `\\htmlId{field-${id}}{\\fbox{\\phantom{\\text{..}}[${id}]\\phantom{\\text{..}}}}`;
                    });

                    // If it contains \\, wrap in gathered to support multi-line in displayMode
                    const finalLatex = processed.includes('\\\\') ? `\\begin{gathered}${processed}\\end{gathered}` : processed;

                    window.katex.render(finalLatex, previewBox, {
                        displayMode: true,
                        trust: true,
                        strict: false
                    });
                }

                // Convert htmlId elements to actual inputs
                const fields = previewBox.querySelectorAll('[id^="field-"]');
                fields.forEach(f => {
                    const id = f.id.replace('field-', '');
                    f.innerHTML = `<input type="text" class="katex-input" placeholder="${id}" readonly>`;
                });
            } else {
                previewBox.innerHTML = `<pre>${content}</pre>`;
            }
        } catch (e) {
            previewBox.innerHTML = `<span style="color:red">Lỗi LaTeX: ${e.message}</span>`;
        }
    };

    const setupTabbedEditor = (container, { onChange, onInsertPlaceholder } = {}) => {
        if (!container) return null;

        const btnEdit = container.querySelector('[data-tab-edit]');
        const btnView = container.querySelector('[data-tab-view]');
        const codeArea = container.querySelector('[data-editor-code]');
        const previewBox = container.querySelector('[data-editor-preview]');
        const btnInsert = container.querySelector('[data-editor-insert]');

        const render = () => {
            renderLatexInElement(previewBox, codeArea.value);
        };


        btnEdit?.addEventListener('click', () => {
            btnEdit.classList.add('active');
            btnView?.classList.remove('active');
            codeArea.style.display = 'block';
            previewBox.style.display = 'none';
            codeArea.focus();
        });

        btnView?.addEventListener('click', () => {
            btnView.classList.add('active');
            btnEdit?.classList.remove('active');
            codeArea.style.display = 'none';
            previewBox.style.display = 'block';
            render();
        });

        codeArea?.addEventListener('input', () => {
            if (onChange) onChange();
        });

        btnInsert?.addEventListener('click', () => {
            if (onInsertPlaceholder) {
                onInsertPlaceholder();
            } else {
                // Default insert logic if not provided
                const start = codeArea.selectionStart;
                const end = codeArea.selectionEnd;
                const matches = codeArea.value.match(/\\placeholder\[(\d+)\]/g) || [];
                const nextId = matches.length + 1;
                const textToInsert = `\\placeholder[${nextId}]{}`;
                codeArea.value = codeArea.value.substring(0, start) + textToInsert + codeArea.value.substring(end);
                codeArea.selectionStart = codeArea.selectionEnd = start + textToInsert.length;
                codeArea.focus();
                if (onChange) onChange();
            }
        });

        return {
            getValue: () => codeArea.value,
            setValue: (val) => {
                codeArea.value = val || '';
                if (previewBox.style.display === 'block') {
                    render();
                }
            },
            refreshPreview: render
        };
    };

    return {
        toArray,
        getMathValue,
        setMathValue,
        getNumberedPlaceholders,
        getFrameLatex,
        parseLatexSegments,
        renderLatexInElement,
        setupTabbedEditor
    };
})();

