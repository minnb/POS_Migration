---
name: web-syntax-highlight-textarea
description: Tô màu cú pháp SQL/code cho ô textarea lớn bằng overlay CSS + JS thuần, không dùng thư viện ngoài (Monaco/CodeMirror). Đọc khi cần highlight syntax cho 1 ô nhập text.
---

# Pattern: Textarea overlay syntax highlighting (không dùng thư viện ngoài)

> Áp dụng khi: cần tô màu cú pháp (SQL/code) cho 1 ô nhập text lớn mà không muốn thêm dependency
> ngoài MudBlazor (Monaco/CodeMirror quá nặng cho 1 ô nhập trong 1 trang admin nội bộ).

Kỹ thuật: `<pre><code>` tô màu nằm PHÍA SAU (`position:absolute`, `pointer-events:none`), `<textarea>`
thật nằm PHÍA TRƯỚC với `color:transparent; background:transparent; caret-color:<màu thật>` — người
dùng gõ vào textarea như bình thường, mắt thấy chữ tô màu của `<pre>` hiện xuyên qua. JS (vanilla,
không lib ngoài) lắng nghe `input`/`scroll` của textarea để re-render `<pre>` + đồng bộ scroll:

```js
window.posSqlHighlightBind = (textareaId, codeId) => {
    const textarea = document.getElementById(textareaId);
    const code = document.getElementById(codeId);
    if (!textarea || !code) return;   // no-op an toàn nếu gọi trước khi phần tử tồn tại
    const render = () => { code.innerHTML = highlight(textarea.value) + '\n'; };
    textarea.addEventListener('input', render);
    textarea.addEventListener('scroll', () => { code.parentElement.scrollTop = textarea.scrollTop; });
    render();
};
```
- Tokenizer dùng **1 regex duy nhất có alternation** (comment/string/bracket/number/keyword) quét
  1 lượt — tránh bug tô màu chồng khi thay thế tuần tự nhiều regex khác nhau (comment/string bị
  match lại bởi rule keyword).
- Escape HTML (`&`/`<`/`>`) TRƯỚC khi build `innerHTML` — bắt buộc, tránh injection nếu nội dung
  người dùng gõ có ký tự đặc biệt.
- Gọi từ C#: `OnAfterRenderAsync(firstRender)` → `bind` lần đầu, `refresh` các lần sau (đồng bộ lại
  khi Blazor tự đổi `value` của textarea, vd nút "Xóa" clear nội dung).
- Giữ nguyên `Immediate="false"`/`@bind:event="onchange"` cho phần bind C# (không đổi hành vi
  round-trip sẵn có) — highlight chạy HOÀN TOÀN client-side qua JS, không cần round-trip SignalR
  mỗi phím gõ.

> Ví dụ thực tế: `wwwroot/js/sql-console-highlight.js`, `Components/Pages/Admin/SqlConsolePage.razor` (`.pos-sql-editor` trong `app.css`)
