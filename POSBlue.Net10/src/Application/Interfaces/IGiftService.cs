using System.Threading.Tasks;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Api.Models.Gift;

namespace VCM.POSBLUE.Application.Interfaces
{
    public interface IGiftService
    {
        Task<ResultResponse> ClaimMMLSchemeAsync(MMLSchemeRequest request);
        Task<ResultResponse> PostGiftCheckAsync(GiftDataRequest request);
    }
}
