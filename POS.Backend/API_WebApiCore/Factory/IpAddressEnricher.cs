using Serilog.Core;
using Serilog.Events;
using System.Linq;
using System.Net;

public class IpAddressEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ipAddress = host
                        .AddressList
                        .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        ?.ToString();

        var ipProperty = new LogEventProperty("IPAddress", new ScalarValue(ipAddress));
        logEvent.AddPropertyIfAbsent(ipProperty);
    }
}