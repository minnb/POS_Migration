using System.Threading.Tasks;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces
{
    public interface IUrboxService
    {
        Task<(bool IsSuccess, string Message, object Data)> CheckSerialUrbox(CheckVoucherPartnerPOSRequest request);
        Task<(bool IsSuccess, string Message, object Data)> PayCodelUrbox(UpdateStatusVoucherPartnerRequest request);
    }
}
