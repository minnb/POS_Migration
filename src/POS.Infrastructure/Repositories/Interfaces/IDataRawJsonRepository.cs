using System.Net;
using POS.Common.Dtos;
using POS.Common.Dtos.CentralMD;
using POS.Common.Dtos.StagingDB;
using POS.Common.Dtos.Tax;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface IDataRawJsonRepository
{
    Task<(bool Success, string Message, string? Detail)> InsertInvoiceCreatedAsync(
        List<InvoiceCreated> invoiceCreated, string connectStringDb, CancellationToken ct = default);

    Task<(bool Success, string Message, object? Data, HttpStatusCode StatusCode)> ValidateTransactionAsync(
        StoreDto storeInfo, string connectStringDb, string orderNo, string appCode = "WCM", CancellationToken ct = default);

    Task<(bool Success, string Message)> InsertDataRawJsonAsync(
        string connectStringDb, List<DataRawJsonDto> request, CancellationToken ct = default);

    Task<string?> GetMessageWarningsVATCheckAsync(string actionType, CancellationToken ct = default);

    Task<List<string>?> GetSyncTableListAsync(CancellationToken ct = default);
}
