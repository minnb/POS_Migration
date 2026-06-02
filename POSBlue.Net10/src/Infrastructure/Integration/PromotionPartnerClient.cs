using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Configuration;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Integration;

public class PromotionPartnerClient : IPromotionPartnerClient
{
    private readonly HttpClient _httpClient;

    public PromotionPartnerClient(HttpClient httpClient, IOptions<PromotionPartnerOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.Url);
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
        
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("UserName", options.Value.User);
        _httpClient.DefaultRequestHeaders.Add("Password", options.Value.Pass);
    }

    public async Task<(int StatusCode, PromotionStaffResponse? Response)> ForceCheckAsync(string staffCode)
    {
        var endPoint = $"staff-quota/{staffCode}/force-check";
        var response = await _httpClient.GetAsync(endPoint);
        return await ProcessResponseAsync(response);
    }

    public async Task<(int StatusCode, PromotionStaffResponse? Response)> CheckAsync(string staffCode, string quotaCode)
    {
        var endPoint = $"staff-quota/{staffCode}/check?code={quotaCode}";
        var response = await _httpClient.GetAsync(endPoint);
        return await ProcessResponseAsync(response);
    }

    public async Task<(int StatusCode, PromotionStaffResponse? Response)> RedeemAsync(PromotionStaffRedeemPartnerRequest request)
    {
        var endPoint = "staff-quota/redeem";
        var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync(endPoint, content);
        return await ProcessResponseAsync(response);
    }

    public async Task<(int StatusCode, PromotionStaffResponse? Response)> RefundAsync(PromotionStaffRedeemPartnerRequest request)
    {
        var endPoint = "staff-quota/refund";
        var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endPoint, content);
        return await ProcessResponseAsync(response);
    }

    private async Task<(int StatusCode, PromotionStaffResponse? Response)> ProcessResponseAsync(HttpResponseMessage response)
    {
        var resultData = await response.Content.ReadAsStringAsync();
        try
        {
            var parsed = JsonConvert.DeserializeObject<PromotionStaffResponse>(resultData);
            return ((int)response.StatusCode, parsed);
        }
        catch
        {
            return ((int)response.StatusCode, null);
        }
    }
}
