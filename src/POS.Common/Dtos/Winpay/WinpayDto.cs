using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.Winpay;

public class POSRequestWinpay
{
    [Required]
    public string StoreCode { get; set; } = string.Empty;
    [Required]
    public string PosCode { get; set; } = string.Empty;
}

public class CashbackWinpayPOS
{
    [Required]
    public string StoreCode { get; set; } = string.Empty;
    [Required]
    public string PosCode { get; set; } = string.Empty;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public int Amount { get; set; }
}

public class GetInfoWinpayPOS
{
    public string? PhoneNumber { get; set; }
    public string? Name { get; set; }
    public string? IDCard { get; set; }
    public DateTime DateOfRegistration { get; set; }
    public string? FingerprintEnrolled { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalCashBack { get; set; }
    public WithdrawQuotaInfo? WithdrawQuotaInfo { get; set; }
}

public class WithdrawQuotaInfo
{
    public decimal QuotaPerDay { get; set; }
    public decimal TotalPerDay { get; set; }
    public decimal QuotaPerWeek { get; set; }
    public decimal TotalPerWeek { get; set; }
    public decimal QuotaPerMonth { get; set; }
    public decimal TotalPerMonth { get; set; }
}

public class PaymentWinpayResponse
{
    public long Id { get; set; }
    public long MemberId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreateTime { get; set; }
    public int Type { get; set; }
    public long Parent_id { get; set; }
    public int Flag { get; set; }
    public string? StoreId { get; set; }
    public string? PosId { get; set; }
    public string? RequestId { get; set; }
    public string? OrderId { get; set; }
    public string? Wallet_type { get; set; }
}

public class WinpayDataPOS : POSRequestWinpay
{
    public string? TerminalCode { get; set; }
    public string? TransactionId { get; set; }
    public string? InvoiceNum { get; set; }
    public string? TransactionDate { get; set; }
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? OperationId { get; set; }
    public string? RequestId { get; set; }
}

public class RefundWinpayPOS : WinpayDataPOS
{
}

public class PaymentWinpayPOS : WinpayDataPOS
{
    public string? Fingerprint { get; set; }
}

public class WithdrawWinpayPOS : PaymentWinpayPOS
{
    public bool WithdrawAll { get; set; }
}

public class UpdateFingerWinpayPOS : POSRequestWinpay
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    public string? OperationId { get; set; }
    [Required]
    public string Otp { get; set; } = string.Empty;
    public List<FPEnrollments>? FPEnrollments { get; set; }
    public string? RequestId { get; set; }
}

public class VerifyFingerWinpayPOS : POSRequestWinpay
{
    public string? PhoneNumber { get; set; }
    public string? TypeCode { get; set; }
    public string? OperationId { get; set; }
    public string? Fingerprint { get; set; }
}

public class UpdateFingerWinpay
{
    public string? Phone_number { get; set; }
    public string? Action_type { get; set; }
    public string? Code { get; set; }
    public string? Store_id { get; set; }
    public string? Pos_id { get; set; }
    public string? Cashier_id { get; set; }
    public List<FPEnrollments>? FPEnrollments { get; set; }
}

public class VerifyFingerWinpay
{
    public string? Action_type { get; set; }
    public string? Phone_number { get; set; }
    public string? Store_id { get; set; }
    public string? Pos_id { get; set; }
    public string? Cashier_id { get; set; }
    public string? Fingerprint { get; set; }
}

public class DataResponseWinpayPOS
{
    public string? Trace { get; set; }
    public string? RequestId { get; set; }
}

public class CommonWinpayRequest
{
    public string? Store_id { get; set; }
    public string? Pos_id { get; set; }
    public string? Cashier_id { get; set; }
    public string? RequestId { get; set; }
}

public class MerchantWinpayDto
{
    public string? Merchant_id { get; set; }
    public string? Request_id { get; set; }
}

public class ResponseWinpay
{
    public int Code { get; set; }
    public string? Message { get; set; }
    public string? Request_id { get; set; }
    public string? Cipher_data { get; set; }
    public string? Base64_iv { get; set; }
    public string? Signature { get; set; }
}

public class PaymentWinpayRequest : MerchantWinpayDto
{
    public DataRequestWinpay? Data { get; set; }
}

public class UncodeRequestWinpay
{
    public string? Merchant_id { get; set; }
    public string? Request_id { get; set; }
    public object? Data { get; set; }
}

public class RequestWinpay
{
    public string? Merchant_id { get; set; }
    public string? Request_id { get; set; }
    public string? Cipher_data { get; set; }
    public string? Base64_iv { get; set; }
    public string? Signature { get; set; }
}

public class DataRequestWinpay
{
    public string? Action_type { get; set; }
    public string? Wallet_type { get; set; }
    public string? Phone_number { get; set; }
    public string? Amount { get; set; }
    public string? Fund_source { get; set; }
    public string? Store_id { get; set; }
    public string? Pos_id { get; set; }
    public string? Order_id { get; set; }
    public string? Fingerprint { get; set; }
}

public class CustomerInfoWinpay
{
    public BlanceWinpay? Balance { get; set; }
    public AccountWinpay? Account { get; set; }
    public string? FingerprintEnrolled { get; set; }
}

public class BlanceWinpay
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string? MemberUsername { get; set; }
    public WinpayWallet? WinpayWallet { get; set; }
    public decimal Balance { get; set; }
    public decimal FrozenBalance { get; set; }
    public decimal IsLock { get; set; }
    public string? Source { get; set; }
    public WalletRule? WalletRule { get; set; }
    public decimal DailyRemainingCashout { get; set; }
    public decimal WeeklyRemainingCashout { get; set; }
    public decimal MonthlyRemainingCashout { get; set; }
}

public class WalletRule
{
    public string? Name { get; set; }
    public decimal DailyCashout { get; set; }
    public decimal WeeklyCashout { get; set; }
    public decimal MonthlyCashout { get; set; }
}

public class AccountWinpay
{
    public string? Username { get; set; }
    public string? RealName { get; set; }
    public string? MobilePhone { get; set; }
    public int MemberLevel { get; set; }
    public int Status { get; set; }
}

public class WinpayWallet
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? EnName { get; set; }
    public string? Unit { get; set; }
    public int Status { get; set; }
    public decimal MinTxFee { get; set; }
    public decimal VndRate { get; set; }
    public decimal MaxTxFee { get; set; }
    public decimal UsdRate { get; set; }
    public int EnableTransfer { get; set; }
    public int Sort { get; set; }
    public int CanWithdraw { get; set; }
    public int CanRecharge { get; set; }
    public int CanTransfer { get; set; }
    public int CanAutoWithdraw { get; set; }
    public decimal WithdrawThreshold { get; set; }
    public float MinWithdrawAmount { get; set; }
    public float MaxWithdrawAmount { get; set; }
    public decimal MinRechargeAmount { get; set; }
    public int IsPlatformWallet { get; set; }
    public bool HasLegal { get; set; }
    public decimal Fee { get; set; }
    public int WithdrawScale { get; set; }
    public string? Infolink { get; set; }
    public int CccountType { get; set; }
    public string? BankAccount { get; set; }
}

public class WithDrawWinpay
{
    public string? Action_type { get; set; }
    public string? Wallet_type { get; set; }
    public string? Phone_number { get; set; }
    public string? Amount { get; set; }
    public string? Store_id { get; set; }
    public string? Pos_id { get; set; }
    public string? Order_id { get; set; }
    public string? Fingerprint { get; set; }
}

public class RefundWinpay : CommonWinpayRequest
{
    public string? Action_type { get; set; }
    public string? Phone_number { get; set; }
    public string? Amount { get; set; }
    public string? Order_id { get; set; }
    public string? Trans_id { get; set; }
    public string? Payment_order_id { get; set; }
}

public class SendOtpWinpay
{
    public string? Action_type { get; set; }
    public string? Phone_number { get; set; }
    public string? Store_id { get; set; }
    public string? Pos_id { get; set; }
}

public class DataOtpWinpay
{
    public string? Otp { get; set; }
}

public class CashbackWinpay
{
    public string? Action_type { get; set; }
    public string? Phone_number { get; set; }
    public string? Store_id { get; set; }
    public string? Pos_id { get; set; }
    public string? Order_id { get; set; }
    public string? Trans_id { get; set; }
    public string? Amount { get; set; }
}

public class RegisterWinpayPOS : POSRequestWinpay
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? IDCard { get; set; }
    public string? OperationId { get; set; }
    [Required]
    public string Otp { get; set; } = string.Empty;
    public List<FPEnrollments>? FPEnrollments { get; set; }
    public string? RequestId { get; set; }
}

public class FPEnrollments
{
    public string? FPIndex { get; set; }
    public string? Base64Str { get; set; }
}

public class RegisterWinpayRequest : MerchantWinpayDto
{
    public DataRegisterWinpay? Data { get; set; }
}

public class DataRegisterWinpay
{
    public string? Action_type { get; set; }
    public string? Phone_number { get; set; }
    public string? Code { get; set; }
    public string? IdCard { get; set; }
    public string? Name { get; set; }
    public List<FPEnrollments>? FPEnrollments { get; set; }
    public string? Pos_id { get; set; }
    public string? Store_id { get; set; }
    public string? Order_id { get; set; }
}

public class CloseAccountWinpay : CommonWinpayRequest
{
    public string? Action_type { get; set; }
    public string? Phone_number { get; set; }
    public string? Fingerprint { get; set; }
    public string? Code { get; set; }
}

public class UnregisterWinpayPOS : POSRequestWinpay
{
    public string? PhoneNumber { get; set; }
    public string? OperationId { get; set; }
    public string? Fingerprint { get; set; }
    [Required]
    public string Otp { get; set; } = string.Empty;
    public string? RequestId { get; set; }
}

public class GetBalanceWinpay
{
    public string? Action_type { get; set; }
    public string? Phone_number { get; set; }
}
