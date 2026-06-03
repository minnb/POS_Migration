using FluentValidation;
using POS.Application.Common.DTOs;

namespace POS.Application.Common.Validators;

public class UpdateOrderInfoRequestValidator : AbstractValidator<UpdateOrderInfoRequest>
{
    public UpdateOrderInfoRequestValidator()
    {
        RuleFor(x => x.Header).NotNull().WithMessage("Dữ liệu trả hàng (Header) không được rỗng");
        RuleFor(x => x.ListLine).NotEmpty().WithMessage("Dữ liệu trả hàng (Line) không được rỗng");
    }
}
