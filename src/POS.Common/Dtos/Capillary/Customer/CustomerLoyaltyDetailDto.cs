namespace POS.Common.Dtos.Capillary.Customer;

public class CustomerDetailCapDto
{
    public CustomerDetailCap? CustomerDetails { get; set; }
    public LedgerDetailsCap? LedgerDetails { get; set; }
    public List<LedgerEntriesCap>? LedgerEntries { get; set; }
    public List<object>? Warnings { get; set; }
}

public class CustomerDetailCap
{
    public string? FirstName { get; set; }
    public long UserId { get; set; }
    public string? ExternalId { get; set; }
    public string? EntityType { get; set; }
}

public class LedgerDetailsCap
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalEntries { get; set; }
    public int PageCount { get; set; }
}

public class LedgerEntriesCap
{
    public long EventLogId { get; set; }
    public string? EventName { get; set; }
    public long CustomerId { get; set; }
    public DateTime LedgerCreatedDate { get; set; }
    public object? CustomerDetails { get; set; }
    public List<EntryDetailCap>? EntryDetails { get; set; }
    public string? NetPointsOnEvent { get; set; }
    public TransactionDetailCap? TransactionDetails { get; set; }
    public string? Store { get; set; }
    public string? StoreCode { get; set; }
    public string? TillCode { get; set; }
    public object? EventDetails { get; set; }
    public int SourceProgramId { get; set; }
    public string? SourceProgramName { get; set; }
}

public class TransactionDetailCap
{
    public long TransactionId { get; set; }
    public string? TransactionNumber { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public decimal GrossBillAmount { get; set; }
    public string? Source { get; set; }
}

public class EntryDetailCap
{
    public string? LedgerEntryType { get; set; }
    public string? Points { get; set; }
    public string? PointsCategory { get; set; }
    public string? ProgramName { get; set; }
    public int ProgramId { get; set; }
}
