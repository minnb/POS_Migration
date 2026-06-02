using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

public interface IOfferService
{
    Task ProcessRetryTopUpPointsStaffAsync(VinIDSalesRequest request);
    Task<string> ProcessTopUpPointsStaffAsync(string queueName, int number, int isRetry);
    Task<(System.Net.HttpStatusCode Status, OfferStaffRemnDto? Data, string Message)> OfferStaffCheckAsync(
        string storeNo, string posNo, string phoneNumber);
    Task<(System.Net.HttpStatusCode Status, string Message)> OfferStaffTransactionAsync(
        OfferStaffTransactionRequest request);
}
