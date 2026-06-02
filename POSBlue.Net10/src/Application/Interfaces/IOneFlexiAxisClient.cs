namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Abstraction cho SOAP service OneFlexiAxis (VINID). Thay việc khởi tạo trực tiếp
/// wsVinID = new OneFlexiAxisService() trong LoyaltyController cũ.
/// Operation duy nhất: processRequest(in0..in3) → chuỗi kết quả.
/// </summary>
public interface IOneFlexiAxisClient
{
    Task<string> ProcessRequestAsync(string in0, string in1, string in2, string in3);
}
