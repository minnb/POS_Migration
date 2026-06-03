using System.Net;
using System.Web.Http.Controllers;
using System.Net.Http;
using System.Linq;
using System.Web.Http.Filters;
using TCX.API.Common.Models;
using Newtonsoft.Json;

public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(HttpActionContext actionContext)
    {
        if (!actionContext.ModelState.IsValid)
        {
            var errors = actionContext.ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .Select(e => new
                {
                    Field = e.Key,
                    Message = e.Value.Errors.First().ErrorMessage
                }).ToArray();

            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
            {
                Message = "Invalid parameters",
                Status = HttpStatusCode.Conflict,
                Data = null,
                MessageTechnical = JsonConvert.SerializeObject(errors)
            });
        }
    }
}