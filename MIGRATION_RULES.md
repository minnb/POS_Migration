## KIẾN TRÚC PROJECT MỚI (.NET 10 Clean Architecture)

### Cấu trúc thư mục:
src/
├── Api/                 ← Controllers, Request/Response models, Filters, Middlewares
├── Application/         ← Use Cases, Service interfaces, Orchestration logic
├── Domain/              ← Entities, Enums, Value Objects, Business Rules, Domain Events
├── Infrastructure/      ← Payment gateway clients, DB repositories, External HTTP, Email
└── Shared/              ← DTOs dùng chung, Constants, Helpers, Extensions

### Nguyên tắc phân tầng bắt buộc:
- Api: KHÔNG chứa business logic. Chỉ nhận request → gọi Application → trả response
- Application: KHÔNG reference Infrastructure trực tiếp. Chỉ dùng Interface
- Domain: KHÔNG reference bất kỳ tầng nào khác. Pure C# only
- Infrastructure: Implement các Interface của Application
- Shared: Được reference bởi tất cả các tầng

### Convention đặt tên:
- Controller: [Feature]Controller.cs → Api/Controllers/
- Request/Response: [Action]Request.cs, [Action]Response.cs → Api/Models/[Feature]/
- Use Case / Service Interface: I[Feature]Service.cs → Application/Interfaces/
- Service Implementation: [Feature]Service.cs → Application/Services/
- Entity: [Name].cs → Domain/Entities/
- Enum: [Name]Enum.cs hoặc [Name]Status.cs → Domain/Enums/
- Business Rule / Domain Service: [Name]DomainService.cs → Domain/Services/
- External HTTP Client: [Provider]Client.cs → Infrastructure/Gateways/[Provider]/
- Repository: [Name]Repository.cs → Infrastructure/Persistence/
- DTO dùng chung: [Name]Dto.cs → Shared/DTOs/
- Constants: [Feature]Constants.cs → Shared/Constants/
- Helper/Extension: [Name]Helper.cs hoặc [Name]Extensions.cs → Shared/Helpers/

### Tech stack .NET 10:
- Controller: Inherit ControllerBase, dùng [ApiController], [Route]
- DI: Constructor injection, KHÔNG dùng ServiceLocator
- Config: IOptions<T> thay cho ConfigurationManager/AppSettings
- HTTP Client: IHttpClientFactory thay cho HttpWebRequest/WebClient
- Logging: ILogger<T> thay cho log4net/NLog cũ
- Async: Tất cả method phải async/await, KHÔNG dùng .Result hay .Wait()
- Null safety: Dùng nullable reference types, null-coalescing operator
- Response: Dùng IActionResult hoặc ActionResult<T>
- Validation: FluentValidation hoặc DataAnnotations
- Routes & Auth: GIỮ NGUYÊN 100% route cũ ([Route], [RoutePrefix] → [Route] trên Controller + Action). KHÔNG đổi tên route, KHÔNG thay đổi cơ chế authentication đang dùng (Basic Auth). Mục tiêu là POS client KHÔNG cần thay đổi bất kỳ cấu hình kết nối nào.
