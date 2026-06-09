using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using POS.Common;

namespace POS.Api.Filters;

/// <summary>
/// Global filter thay [ValidateModel] attribute cũ (Web API 2).
/// Đăng ký trong Program.cs: options.Filters.Add&lt;ValidateModelFilter&gt;()
/// Trả ResultResponse chuẩn khi ModelState invalid — action không được gọi.
/// </summary>
public sealed class ValidateModelFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        var errors = string.Join("; ", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));

        context.Result = new BadRequestObjectResult(new ResultResponse
        {
            Message = "Dữ liệu đầu vào không hợp lệ.",
            Status = HttpStatusCode.BadRequest,
            MessageTechnical = errors
        });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
