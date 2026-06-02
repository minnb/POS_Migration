using Microsoft.Extensions.Configuration;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;

namespace VCM.POSBLUE.Infrastructure.Gateways.OneFlexiAxis;

/// <summary>
/// Triển khai IOneFlexiAxisClient bằng WCF client (System.ServiceModel) sinh từ WSDL qua dotnet-svcutil.
/// Endpoint lấy từ cấu hình AppSettings:OneFlexiAxisEndpoint (mặc định endpoint SOAP 1.1 trong WSDL).
/// </summary>
public sealed class OneFlexiAxisGateway : IOneFlexiAxisClient
{
    private readonly string _endpoint;

    public OneFlexiAxisGateway(IConfiguration configuration)
    {
        _endpoint = configuration["AppSettings:OneFlexiAxisEndpoint"]
            ?? "http://10.111.55.147:7007/OneFlexiAxisService/services/OneFlexiAxisService.OneFlexiAxisServiceHttpSoap11Endpoint/";
    }

    public async Task<string> ProcessRequestAsync(string in0, string in1, string in2, string in3)
    {
        var client = new OneFlexiAxisServicePortTypeClient(
            OneFlexiAxisServicePortTypeClient.EndpointConfiguration.OneFlexiAxisServiceHttpSoap11Endpoint,
            _endpoint);
        try
        {
            var response = await client.processRequestAsync(in0, in1, in2, in3);
            await client.CloseAsync();
            return response.@return ?? "";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OneFlexiAxis.processRequest");
            client.Abort();
            throw;
        }
    }
}
