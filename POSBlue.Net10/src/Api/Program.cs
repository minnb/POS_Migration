using Serilog;
using Serilog.Filters;
using Serilog.Sinks.Elasticsearch;
using VCM.POSBLUE.Api.Filters;
using VCM.POSBLUE.Api.Middlewares;
using VCM.POSBLUE.Application;
using VCM.POSBLUE.Infrastructure;
using VCM.POSBLUE.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog → Elasticsearch (port từ Global.asax Application_Start) ----
var esUrl = builder.Configuration.GetConnectionString("Elasticsearch") ?? "http://localhost:9200";
var esUser = builder.Configuration["AppSettings:Elasticsearch_USER"];
var esPass = builder.Configuration["AppSettings:Elasticsearch_PASS"];

Log.Logger = new LoggerConfiguration()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("ResponseTime", 0)
    .Enrich.WithProperty("WebApi", "")
    .Enrich.WithProperty("PosNo", "")
    .Enrich.WithProperty("HttpContext", "")
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(esUrl))
    {
        AutoRegisterTemplate = true,
        OverwriteTemplate = true,
        DetectElasticsearchVersion = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
        IndexFormat = "wcm_pos_log-{0:yyyy-MM-dd-HH}",
        ModifyConnectionSettings = x => string.IsNullOrEmpty(esUser) ? x : x.BasicAuthentication(esUser, esPass),
    })
    .Filter.ByExcluding(Matching.WithProperty<string>("WebApi", n => n.Contains("api/salary/view-salary")))
    .Filter.ByExcluding(Matching.WithProperty<string>("WebApi", n => n.Contains("api/common/POSMonitor")))
    .CreateLogger();

builder.Host.UseSerilog();

// ---- Options ----
builder.Services.Configure<AppSettingsOptions>(builder.Configuration.GetSection(AppSettingsOptions.SectionName));

// ---- MVC + Newtonsoft (giữ nguyên định dạng JSON như bản cũ dùng JsonConvert) ----
builder.Services
    .AddControllers(options =>
    {
        // Global filter: Basic Auth — tương đương config.Filters.Add(new BasicAuthenticationAttribute())
        options.Filters.Add<BasicAuthenticationFilter>();
    })
    .AddNewtonsoftJson();

builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký filter để DI resolve được dependency của nó
builder.Services.AddScoped<BasicAuthenticationFilter>();

// ---- Application & Infrastructure ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware log request/response — thay MessageLoggingHandler
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting VCM.POSBLUE.API (.NET 10)");
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
