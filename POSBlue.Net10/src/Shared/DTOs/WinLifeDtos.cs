using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace VCM.POSBLUE.Shared.DTOs;

// WinLife / WinCode DTOs — port nguyên field từ bản cũ.

// ── GET api/v2/otp/generate ───────────────────────────────────────────────────
public class OtpResponse
{
    public string? DeveloperMessage { get; set; }
    public string? Message { get; set; }
    public CXResponseData? Data { get; set; }
}

public class CXResponseData
{
    public string? MessageId { get; set; }
    public string? Status { get; set; }
}

// ── POST api/v2/otp/verify ────────────────────────────────────────────────────
public class PosVerifyOtpRequest
{
    [Required] public string PosNo { get; set; } = "";
    [Required] public string PhoneNumber { get; set; } = "";
    [Required] public string Otp { get; set; } = "";
    public string Action { get; set; } = "";
}

// ── GET api/blue/winlife/generateOTP ─────────────────────────────────────────
public class WinLifeOtpResponse
{
    [JsonProperty("errorCode")] public string? ErrorCode { get; set; }
    [JsonProperty("errorMessage")] public string? ErrorMessage { get; set; }
}

// ── POST api/blue/winlife/register ────────────────────────────────────────────
public class WinLifeRegisterRequest
{
    [Required] public string fullName { get; set; } = "";
    [Required] public string title { get; set; } = "";
    [Required, MinLength(9)] public string phoneNo { get; set; } = "";
    [Required] public string gender { get; set; } = ""; // F / M
    public string? otp { get; set; }
    public string? userPOS { get; set; }
    [Required, StringLength(6, MinimumLength = 4)] public string storeCode { get; set; } = "";
    [Required] public string posCode { get; set; } = "";
    public string? dob { get; set; }
    public string? identifierCardid { get; set; }
    public string? issuedDate { get; set; }
    public string? issuedPlace { get; set; }
    public string status { get; set; } = "REGISTER"; // REGISTER / TEMP / CAP
    public string? email { get; set; }
    public string? address { get; set; }
    public string? province { get; set; }
    public string? district { get; set; }
    public string? city { get; set; }
    public string? ward { get; set; }
}

// CX request model (build internally, never exposed to POS)
public class WinLifeRegisterCxRequest
{
    [JsonProperty("fullName")] public string? fullName { get; set; }
    [JsonProperty("title")] public string? title { get; set; }
    [JsonProperty("phoneNo")] public string? phoneNo { get; set; }
    [JsonProperty("gender")] public string? gender { get; set; }
    [JsonProperty("merchantId")] public string? merchantId { get; set; }
    [JsonProperty("otp")] public string? otp { get; set; }
    [JsonProperty("dob")] public string? dob { get; set; }
    [JsonProperty("storeNo")] public string? storeNo { get; set; }
    [JsonProperty("source")] public string? source { get; set; }
    [JsonProperty("staffEntityId")] public string? staffEntityId { get; set; }
    [JsonProperty("refStaffId")] public string? refStaffId { get; set; }
    [JsonProperty("identifierCardid")] public string? identifierCardid { get; set; }
    [JsonProperty("issuedDate")] public string? issuedDate { get; set; }
    [JsonProperty("issuedPlace")] public string? issuedPlace { get; set; }
}

public class WinLifeRegisterPosResponse
{
    public string? fullName { get; set; }
    public string? phoneNo { get; set; }
    public string? gender { get; set; }
    public string? csn { get; set; }
}

public class WinLifeRegisterCxResponse
{
    [JsonProperty("developerMessage")] public string? developerMessage { get; set; }
    [JsonProperty("message")] public string? message { get; set; }
    [JsonProperty("code")] public string? code { get; set; }
    [JsonProperty("data")] public WinLifeRegisterPosResponse? data { get; set; }
    [JsonProperty("errors")] public List<WinLifeRegisterError>? errors { get; set; }
}

public class WinLifeRegisterError
{
    [JsonProperty("field")] public string? field { get; set; }
    [JsonProperty("message")] public string? message { get; set; }
}

// ── POST api/blue/winlife/update-promotions ───────────────────────────────────
public class WinLifeUpdatePromotionsRequest
{
    public string? csn { get; set; }
    [Required] public string action { get; set; } = ""; // A = ghi nhận, I = xoá
    [Required] public string storeCode { get; set; } = "";
    [Required] public string posCode { get; set; } = "";
    public string? phoneNumber { get; set; }
    [Required] public string orderNo { get; set; } = "";
    public List<AppliedCodeItem>? appliedCodes { get; set; }
}

public class AppliedCodeItem
{
    public string? winCode { get; set; }
    public int quantity { get; set; }
}

// ── GET api/blue/winlife/winCode-histories ────────────────────────────────────
public class WinLifeHistoryModel
{
    [JsonProperty("csn")] public string? csn { get; set; }
    [JsonProperty("histories")] public List<WinCodeHistoryItem>? histories { get; set; }
}

public class WinCodeHistoryItem
{
    [JsonProperty("winCode")] public string? winCode { get; set; }
    [JsonProperty("winCodeName")] public string? winCodeName { get; set; }
    [JsonProperty("quantityOnHands")] public int quantityOnHands { get; set; }
    [JsonProperty("transactions")] public List<WinCodeTransactionItem>? transactions { get; set; }
}

public class WinCodeTransactionItem
{
    [JsonProperty("billNo")] public string? billNo { get; set; }
    [JsonProperty("merchantId")] public string? merchantId { get; set; }
    [JsonProperty("storeNo")] public string? storeNo { get; set; }
    [JsonProperty("posNo")] public string? posNo { get; set; }
    [JsonProperty("quantity")] public int quantity { get; set; }
    [JsonProperty("transactionDatetime")] public string? transactionDatetime { get; set; }
}

// ── GET api/blue/winlife/smart-pos/customer-by-last-digits-phone ──────────────
public class CustomerByLastDigitsPhoneResponse
{
    public string? Title { get; set; }
    public string? PhoneNo { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? PotentialInd { get; set; }
    public string? VipInd { get; set; }
    public string? Dob { get; set; }
}

// ── POST api/blue/winlife/smart-pos/update-customer-info ──────────────────────
public class WinLifeUpdateCustomerRequest
{
    public string? fullName { get; set; }
    public string? phoneNo { get; set; }
    public string? gender { get; set; }
    public string? title { get; set; }
    public string? storeCode { get; set; }
    public string? posCode { get; set; }
    public string? dob { get; set; }
    public string? email { get; set; }
    public string? address { get; set; }
    public string? province { get; set; }
    public string? district { get; set; }
    public string? city { get; set; }
    public string? ward { get; set; }
}

// ── CX API generic response ───────────────────────────────────────────────────
public class CxApiResponse
{
    [JsonProperty("code")] public string? code { get; set; }
    [JsonProperty("message")] public string? message { get; set; }
    [JsonProperty("developerMessage")] public string? developerMessage { get; set; }
    [JsonProperty("data")] public object? data { get; set; }
}

// ── WinCode DB Dtos ───────────────────────────────────────────────────────────
public class WinCodeCustomerRecord
{
    public Guid ID { get; set; }
    public string? WinCode { get; set; }
    public string? MemberCard { get; set; }
    public string? Csn { get; set; }
    public int QuantityRecieptedSum { get; set; }
    public int QuantityReciepted { get; set; }
    public string? OrderNo { get; set; }
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime TransDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
