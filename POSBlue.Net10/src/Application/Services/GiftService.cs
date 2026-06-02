using System.Net;
using System.Threading.Tasks;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.DTOs.WinCare;

namespace VCM.POSBLUE.Application.Services
{
    /// <summary>
    /// Stub implementation for Gift related operations.
    /// In a real migration this would call the legacy POSGiftService and other gateways.
    /// </summary>
    public class GiftService : IGiftService
    {
        // TODO: inject actual gateway clients when they are implemented.
        public GiftService()
        {
            // No dependencies for the stub.
        }

        public async Task<ResultResponse> ClaimMMLSchemeAsync(MMLSchemeRequest request)
        {
            // Stub: simply return OK with a placeholder message.
            await Task.CompletedTask;
            return new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "ClaimMMLScheme stub executed",
                Data = null,
                MessageTechnical = null
            };
        }

        public async Task<ResultResponse> PostGiftCheckAsync(GiftDataRequest request)
        {
            // Stub: simply return OK with a placeholder message.
            await Task.CompletedTask;
            return new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "PostGiftCheck stub executed",
                Data = null,
                MessageTechnical = null
            };
        }
    }
}
