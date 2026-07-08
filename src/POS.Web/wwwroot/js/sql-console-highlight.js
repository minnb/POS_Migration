// Syntax highlighting cho ô SQL trong SQL Console (SqlConsolePage.razor).
// Kỹ thuật: <pre><code> tô màu nằm phía sau, <textarea> thật nằm phía trước với chữ
// trong suốt (chỉ còn caret + selection hiển thị) — người dùng gõ vào textarea như bình
// thường, mắt thấy chữ tô màu của <pre> hiện xuyên qua.
// Gọi từ C#: await JS.InvokeVoidAsync("posSqlHighlightBind", textareaId, codeId) sau khi render lần đầu,
// và await JS.InvokeVoidAsync("posSqlHighlightRefresh", textareaId, codeId) mỗi lần Blazor tự đổi
// value của textarea (vd nút "Xóa") để đồng bộ lại phần tô màu.

(function () {
    const KEYWORDS = new Set([
        'SELECT', 'INSERT', 'UPDATE', 'DELETE', 'FROM', 'WHERE', 'JOIN', 'INNER', 'LEFT', 'RIGHT',
        'FULL', 'OUTER', 'CROSS', 'ON', 'GROUP', 'BY', 'ORDER', 'HAVING', 'IN', 'AND', 'OR', 'NOT',
        'NULL', 'IS', 'LIKE', 'BETWEEN', 'AS', 'DISTINCT', 'TOP', 'INTO', 'VALUES', 'SET', 'CREATE',
        'ALTER', 'TABLE', 'PROCEDURE', 'PROC', 'DROP', 'TRUNCATE', 'EXEC', 'EXECUTE', 'DECLARE',
        'BEGIN', 'END', 'IF', 'ELSE', 'WHILE', 'RETURN', 'GO', 'PRIMARY', 'KEY', 'FOREIGN',
        'REFERENCES', 'DEFAULT', 'CONSTRAINT', 'INDEX', 'UNIQUE', 'CHECK', 'CASCADE', 'UNION', 'ALL',
        'EXISTS', 'CASE', 'WHEN', 'THEN', 'ASC', 'DESC', 'NOLOCK', 'WITH', 'MERGE', 'GRANT', 'REVOKE',
        'DENY', 'TRANSACTION', 'COMMIT', 'ROLLBACK', 'TRY', 'CATCH', 'THROW', 'CAST', 'CONVERT',
        'COUNT', 'SUM', 'AVG', 'MIN', 'MAX', 'OVER', 'PARTITION', 'ADD', 'COLUMN', 'VARCHAR', 'INT',
        'BIGINT', 'DECIMAL', 'DATETIME', 'DATE', 'BIT', 'NVARCHAR', 'IDENTITY'
    ]);

    // 1 pass duy nhất qua regex có alternation — tránh bug tô màu chồng lên nhau khi
    // thay thế tuần tự nhiều regex khác nhau (comment/string bị match lại bởi rule keyword).
    const TOKEN_RE = /(--[^\n]*)|(\/\*[\s\S]*?\*\/)|('(?:[^']|'')*')|(\[[^\]]*\])|(\b\d+\.?\d*\b)|(\b[A-Za-z_][A-Za-z0-9_]*\b)|([\s\S])/g;

    function escapeHtml(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function highlight(sql) {
        let out = '';
        let m;
        TOKEN_RE.lastIndex = 0;
        while ((m = TOKEN_RE.exec(sql)) !== null) {
            const [full, comment, block, str, bracket, num, word] = m;
            if (comment || block) out += `<span class="sql-com">${escapeHtml(full)}</span>`;
            else if (str) out += `<span class="sql-str">${escapeHtml(full)}</span>`;
            else if (bracket) out += `<span class="sql-ident">${escapeHtml(full)}</span>`;
            else if (num) out += `<span class="sql-num">${escapeHtml(full)}</span>`;
            else if (word) out += KEYWORDS.has(word.toUpperCase())
                ? `<span class="sql-kw">${escapeHtml(full)}</span>`
                : escapeHtml(full);
            else out += escapeHtml(full);
        }
        return out;
    }

    function render(textarea, code) {
        // Thêm 1 dòng trống cuối để tránh dòng cuối bị hụt chiều cao so với textarea.
        code.innerHTML = highlight(textarea.value) + '\n';
        code.parentElement.scrollTop = textarea.scrollTop;
        code.parentElement.scrollLeft = textarea.scrollLeft;
    }

    window.posSqlHighlightBind = (textareaId, codeId) => {
        const textarea = document.getElementById(textareaId);
        const code = document.getElementById(codeId);
        if (!textarea || !code) return;

        const onRender = () => render(textarea, code);
        textarea.addEventListener('input', onRender);
        textarea.addEventListener('scroll', () => {
            code.parentElement.scrollTop = textarea.scrollTop;
            code.parentElement.scrollLeft = textarea.scrollLeft;
        });
        onRender();
    };

    window.posSqlHighlightRefresh = (textareaId, codeId) => {
        const textarea = document.getElementById(textareaId);
        const code = document.getElementById(codeId);
        if (!textarea || !code) return;
        render(textarea, code);
    };
})();
