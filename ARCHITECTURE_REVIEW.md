# FitJourney API Mimari Uyum Komutu (Refactor & Review Prompt)

> Bu projeyi referans aldığım `fit-journey-api` ile aynı mimari yapıya uydur. Aşağıda **tam convention seti** ve **kural listesi** var. Mevcut kodu tara, sapmaları listele, sonra düzelt. Hiçbir kuralı atlama, kıyıdan köşeden geçmiyor.

---

## 0) STACK & GENEL YAPI

- **.NET / ASP.NET Core**, **MediatR (CQRS)**, **EF Core (PostgreSQL)**, **AutoMapper**, **FluentValidation**, **Serilog**, **YAML-based localization**
- **6 katman**: `Domain`, `Persistence`, `Core`, `Application`, `Infrastructure`, `WebAPI`
- Multi-tenant, JWT auth, soft delete (DeletedDate + ArchivedDate), 3 dil (tr/en/ru)

### Katman Sorumlulukları

| Katman | Sorumluluk |
|---|---|
| **Domain** | Entity'ler, Enum'lar. `Entity` base class (Id, CreatedDate, UpdatedDate, DeletedDate, ArchivedDate). |
| **Persistence** | `BaseDbContext`, `IEntityTypeConfiguration` Fluent config'ler, `EfRepositoryBase`'den türeyen repository implementasyonları. |
| **Core** | Cross-cutting: pipeline behaviors, generic repository, exception types, localization, security (JWT/Hashing/AES), paging. |
| **Application** | CQRS (Features/{X}/Commands, Queries, Rules, Constants, Resources, Profiles), service abstraction'ları. |
| **Infrastructure** | External adapter'lar (file, mail, sms, stripe, otp, notification). |
| **WebAPI** | Controller'lar, `BaseController`, sadece `Mediator.Send` çağrısı yapar — iş mantığı YOK. |

---

## 1) FEATURE KLASÖR ŞABLONU (ZORUNLU)

Her feature **aynı** yapıya sahip olmalı:

```
/Application/Features/{FeatureName}/
├── Commands/
│   ├── Create/
│   │   ├── Create{Entity}Command.cs            ← Command + Handler iç sınıf olarak
│   │   ├── Create{Entity}CommandValidator.cs   ← FluentValidation
│   │   └── Created{Entity}Response.cs          ← IResponse implements
│   ├── Update/  (aynı 3 dosya, "Update"/"Updated")
│   └── Delete/  (aynı 3 dosya, "Delete"/"Deleted")
├── Queries/
│   ├── GetById/
│   │   ├── GetById{Entity}Query.cs             ← Query + Handler iç sınıf
│   │   └── GetById{Entity}Response.cs
│   └── GetList/
│       ├── GetList{Entity}Query.cs
│       └── GetList{Entity}ListItemDto.cs       ← IDto implements
├── Rules/
│   └── {Entity}BusinessRules.cs                ← BaseBusinessRules'tan türer
├── Constants/
│   ├── {Entity}BusinessMessages.cs             ← Localization key constants + SectionName
│   └── {Entity}OperationClaims.cs              ← Role string constants
├── Resources/Locales/
│   ├── {Feature}.tr.yaml
│   ├── {Feature}.en.yaml
│   └── {Feature}.ru.yaml
└── Profiles/
    └── MappingProfiles.cs                      ← AutoMapper Profile
```

**Sapma örnekleri (düzelt):** Handler'ı ayrı dosyaya çıkarmış olmak, Validator/Response'u tek dosyaya birleştirmek, Rules'u Domain'e koymak, Resources eksikliği.

---

## 2) CQRS — COMMAND ŞABLONU

**KURAL:** Handler, Command sınıfının **içinde nested class** olarak yazılır. Ayrı dosyaya KOYMA.

```csharp
public class Create{Entity}Command :
    IRequest<Created{Entity}Response>,
    ITenantAware,        // multi-tenant ise
    ISecuredRequest,     // korumalı endpoint ise (her zaman)
    ILoggableRequest     // her zaman ekle
{
    [JsonIgnore] public long TenantId { get; set; }   // Pipeline set eder, body'den gelmez
    public string SomeField { get; set; }
    // ...

    public string[] Roles =>
    [
        OperationClaims.TenantAdmin,
        OperationClaims.GeneralAdmin,
        {Entity}OperationClaims.Create
    ];

    public class Create{Entity}CommandHandler :
        IRequestHandler<Create{Entity}Command, Created{Entity}Response>
    {
        private readonly IMapper _mapper;
        private readonly I{Entity}Repository _repository;
        private readonly {Entity}BusinessRules _businessRules;

        public Create{Entity}CommandHandler(/* DI */) { /* assign */ }

        public async Task<Created{Entity}Response> Handle(
            Create{Entity}Command request, CancellationToken cancellationToken)
        {
            {Entity} entity = _mapper.Map<{Entity}>(request);
            await _businessRules.SomeRule(entity);
            await _repository.AddAsync(entity);
            return _mapper.Map<Created{Entity}Response>(entity);
        }
    }
}
```

**Validator ayrı dosyada:**
```csharp
public class Create{Entity}CommandValidator : AbstractValidator<Create{Entity}Command>
{
    public Create{Entity}CommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SomeField).NotEmpty();
    }
}
```

**Response:**
```csharp
public class Created{Entity}Response : IResponse
{
    public long Id { get; set; }
}
```

---

## 3) CQRS — QUERY ŞABLONU

```csharp
public class GetById{Entity}Query :
    IRequest<GetById{Entity}Response>,
    ITenantAware, ISecuredRequest, ILoggableRequest
{
    public long Id { get; set; }
    [JsonIgnore] public long TenantId { get; set; }

    public string[] Roles => [ OperationClaims.TenantAdmin, {Entity}OperationClaims.View ];

    public class GetById{Entity}QueryHandler : IRequestHandler<GetById{Entity}Query, GetById{Entity}Response>
    {
        // ctor + DI

        public async Task<GetById{Entity}Response> Handle(
            GetById{Entity}Query request, CancellationToken cancellationToken)
        {
            {Entity}? entity = await _repository.GetAsync(
                predicate: e => e.Id == request.Id && e.TenantId == request.TenantId,
                include: q => q.Include(x => x.Children),
                cancellationToken: cancellationToken);

            await _businessRules.{Entity}ShouldExistWhenSelected(entity);

            return _mapper.Map<GetById{Entity}Response>(entity);
        }
    }
}
```

**GetList paginated:** `IRequest<GetListResponse<GetList{Entity}ListItemDto>>` döner; `PageRequest` taşır; predicate'te `!e.ArchivedDate.HasValue` filtre koy.

---

## 4) MARKER INTERFACES — ÖNEMİ ÇOK BÜYÜK

Bunlar **MediatR pipeline behavior'ları tetikleyen marker interface**'lerdir. Yanlış implement etmek = endpoint güvenliğini kaybetmek.

### `ITenantAware`
```csharp
namespace Core.Application.Pipelines.Tenant;
public interface ITenantAware { public long TenantId { get; set; } }
```
- **Ne işe yarar:** `TenantMiddleware` JWT token'daki **encrypted** `tenantid` claim'ini AES ile decrypt edip `request.TenantId`'a yazar.
- **Kural:** Multi-tenant verisine dokunan **HER** Command/Query bunu implement etmeli. `TenantId` `[JsonIgnore]` olmalı (client gönderemesin).
- **Handler'da:** Tüm DB sorgularında `e.TenantId == request.TenantId` filtresi **ZORUNLU**.

### `ISecuredRequest`
```csharp
namespace Core.Application.Pipelines.Authorization;
public interface ISecuredRequest { public string[] Roles { get; } }
```
- **Ne işe yarar:** `AuthorizationBehavior` JWT'den userId çeker, `IUserAuthorizationService.HasAnyRoleAsync(userId, request.Roles)` ile kontrol eder. Yoksa `AuthorizationException` fırlatır.
- **Kural:** Anonymous endpoint dışındaki **HER** request bunu implement etmeli. `Roles` arrayi sabit olarak `OperationClaims.*` referans etmeli — string literal yazma.

### `ILoggableRequest`
- Boş marker. `LoggingBehavior` Serilog ile request/response/duration loglar, response header'a `X-Transaction-Id` ekler. **Her** Command/Query bunu implement etmeli.

### `IMobileAware`
- Mobil-spesifik request'ler için. `MobileMiddleware` device ID/member ID extract eder.

### `ITransactionalRequest`
- `TransactionScopeBehavior` `TransactionScope` açar. Birden fazla aggregate'e yazan command'lara ekle.

**Pipeline kayıt sırası (ApplicationServiceRegistration.cs):**
```csharp
configuration.AddOpenBehavior(typeof(LocalizationMiddleware<,>));    // 1
configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));           // 2
configuration.AddOpenBehavior(typeof(AuthorizationBehavior<,>));     // 3
configuration.AddOpenBehavior(typeof(TenantMiddleware<,>));          // 4
configuration.AddOpenBehavior(typeof(MobileMiddleware<,>));          // 5
configuration.AddOpenBehavior(typeof(TransactionScopeBehavior<,>));  // 6
```

---

## 5) BUSINESS RULES YAPISI

**Konum:** `/Application/Features/{X}/Rules/{Entity}BusinessRules.cs`

```csharp
public class {Entity}BusinessRules : BaseBusinessRules    // Core'da boş abstract class
{
    private readonly ILocalizationService _localizationService;

    public {Entity}BusinessRules(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public async Task {Entity}ShouldExistWhenSelected({Entity}? entity)
    {
        if (entity == null)
            await ThrowBusinessException({Entity}BusinessMessages.{Entity}NotExists);
    }

    private async Task ThrowBusinessException(string messageKey)
    {
        string message = await _localizationService.GetLocalizedAsync(
            messageKey, {Entity}BusinessMessages.SectionName);
        throw new BusinessException(message);
    }
}
```

**Kayıt:** `services.AddSubClassesOfType(Assembly.GetExecutingAssembly(), typeof(BaseBusinessRules))` — otomatik.

**Çağırma (Handler'da):** `await _businessRules.{RuleMethod}(...);`

---

## 6) INLINE THROW YASAĞI

> **Kural:** `throw new Exception(...)` ya da random `throw new InvalidOperationException(...)` **yasak**. İş kuralı ihlalleri **her zaman** `BusinessException` fırlatmalı ve mesaj **localization key'den** gelmeli.

### Doğru kullanım
```csharp
// 1) Localized (tercih edilen):
private async Task ThrowBusinessException(string messageKey)
{
    string message = await _localizationService.GetLocalizedAsync(
        messageKey, {Entity}BusinessMessages.SectionName);
    throw new BusinessException(message);
}

// 2) Sadece teknik validation (file ext/size gibi parametreli):
throw new BusinessException(string.Format({Entity}BusinessMessages.InvalidFileExtension, allowed));
```

### Yanlış (sapma — düzelt!)
```csharp
throw new Exception("Trainer not found");                     // YANLIS
throw new InvalidOperationException("blah");                  // YANLIS
if (x == null) throw new BusinessException("Bulunamadı");     // YANLIS (hardcoded, localized değil)
return BadRequest("Hata");                                    // YANLIS (Controller'da iş mantığı yok)
```

**Exception Types** (`/Core/CrossCuttingConcerns/Exception/Types/`): `BusinessException`, `NotFoundException`, `ValidationException`, `AuthorizationException`. Sadece bunları kullan.

---

## 7) RESOURCES / LOCALIZATION

**Constants:**
```csharp
namespace Application.Features.{X}.Constants;
public static class {Entity}BusinessMessages
{
    public const string SectionName = "{Entity}";   // YAML section adı

    public const string {Entity}NotExists = "{Entity}NotExists";
    public const string InvalidFileExtension = "InvalidFileExtension";
}
```

**YAML dosyaları (3 dil zorunlu):**
```yaml
# {Feature}.tr.yaml
{Entity}NotExists: "Antrenör bulunamadı."
InvalidFileExtension: "Geçersiz dosya uzantısı: {0}"
```
Aynısı `.en.yaml` ve `.ru.yaml` için yapılmalı.

**Kullanım:** `await _localizationService.GetLocalizedAsync(key, SectionName)`.

---

## 8) OPERATION CLAIMS (ROLES)

```csharp
namespace Application.Features.{X}.Constants;
public static class {Entity}OperationClaims
{
    private const string _section = "{Entity}";
    public const string Admin  = $"{_section}.Admin";
    public const string Create = $"{_section}.Create";
    public const string Update = $"{_section}.Update";
    public const string Delete = $"{_section}.Delete";
    public const string View   = $"{_section}.View";
    public const string List   = $"{_section}.List";
}
```

`Roles =>` her zaman bu constant'ları referans et — string literal yazma.

---

## 9) DOMAIN ENTITY

```csharp
public class {Entity} : Entity   // Core.Repositories.Entity
{
    // Id, CreatedDate, UpdatedDate, DeletedDate, ArchivedDate base'den gelir
    public long TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = default!;

    public string SomeField { get; set; }
    public virtual ICollection<Child> Children { get; set; } = default!;
}
```

- **Soft delete:** `ArchivedDate` (handler'da) **veya** `DeletedDate` (`HasQueryFilter` ile). Repo'dan gerçek silme yapılmaz.
- JSON için `string` saklanır, `[Column(TypeName="jsonb")]` veya Fluent API ile `HasColumnType("jsonb")`.

---

## 10) PERSISTENCE — REPOSITORY & CONFIG

**Repository interface (Application'da):**
```csharp
// /Application/Services/Repositories/I{Entity}Repository.cs
public interface I{Entity}Repository : IAsyncRepository<{Entity}> { }
```

**Concrete (Persistence'ta):**
```csharp
public class {Entity}Repository : EfRepositoryBase<{Entity}, BaseDbContext>, I{Entity}Repository
{
    public {Entity}Repository(BaseDbContext context) : base(context) { }
}
```

**EntityConfiguration (Fluent API):**
```csharp
public class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.ToTable("{Entity}s").HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").IsRequired();
        builder.Property(e => e.TenantId).HasColumnName("TenantId").IsRequired();
        // tüm property'ler tek tek ayarlanır

        builder.HasOne(e => e.Tenant);
        builder.HasMany(e => e.Children);

        builder.HasQueryFilter(e => !e.DeletedDate.HasValue);   // soft delete
        builder.HasBaseType((string)null!);
    }
}
```

**DbContext'e DbSet ekle.** `OnModelCreating` zaten `ApplyConfigurationsFromAssembly` çağırıyor.

---

## 11) WEBAPI CONTROLLERS

```csharp
[Route("api/[controller]")]
[ApiController]
public class {Entity}Controller : BaseController   // /WebAPI/Controllers/BaseController.cs
{
    [HttpPost]
    public async Task<IActionResult> Add([FromForm] Create{Entity}Command cmd)
        => Created("", await Mediator.Send(cmd));

    [HttpPut]
    public async Task<IActionResult> Update([FromForm] Update{Entity}Command cmd)
        => Ok(await Mediator.Send(cmd));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] long id)
        => Ok(await Mediator.Send(new Delete{Entity}Command { Id = id }));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] long id)
        => Ok(await Mediator.Send(new GetById{Entity}Query { Id = id }));

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] PageRequest pageRequest)
        => Ok(await Mediator.Send(new GetList{Entity}Query { PageRequest = pageRequest }));
}
```

**Kurallar:**
- Controller'da **iş mantığı yok**, sadece `Mediator.Send`.
- Dosya yükleme varsa `[FromForm]`, yoksa `[FromBody]`.
- 201 Created / 200 Ok dön. Manuel `BadRequest`/`NotFound` döndürme — exception'lar global handler tarafından yakalanır.

---

## 12) NAMING CONVENTION TABLOSU

| Artifact | Convention |
|---|---|
| Command | `{Action}{Entity}Command` (`CreateTenantTrainerCommand`) |
| Handler | `{Action}{Entity}CommandHandler` (nested) |
| Validator | `{Action}{Entity}CommandValidator` |
| Response | `{ActionPast}{Entity}Response` (`Created…`, `Updated…`, `Deleted…`) |
| Query | `{GetAction}{Entity}Query` |
| ListItem DTO | `GetList{Entity}ListItemDto : IDto` |
| Business Rules | `{Entity}BusinessRules : BaseBusinessRules` |
| Operation Claims | `{Entity}OperationClaims` |
| Business Messages | `{Entity}BusinessMessages` |
| Repository (interface) | `I{Entity}Repository : IAsyncRepository<{Entity}>` |
| Repository (concrete) | `{Entity}Repository` |
| Entity Config | `{Entity}Configuration : IEntityTypeConfiguration<{Entity}>` |
| Controller | `{Entity}sController : BaseController` |

---

## 13) SOFT DELETE PATTERN

Delete handler:
```csharp
entity.ArchivedDate = DateTime.UtcNow;     // hard delete YOK
await _repository.UpdateAsync(entity);
```
GetList handler'da: `predicate: e => e.TenantId == request.TenantId && !e.ArchivedDate.HasValue`.

---

## 14) APPLICATIONSERVICEREGISTRATION (DI)

Şunlar var olmalı:
- `services.AddAutoMapper(Assembly.GetExecutingAssembly())`
- `services.AddMediatR(...)` + 6 pipeline behavior (yukarıdaki sıra)
- `services.AddSubClassesOfType(asm, typeof(BaseBusinessRules))` — rules otomatik
- `services.AddValidatorsFromAssembly(asm)` — validator'lar otomatik
- `services.AddYamlResourceLocalization()`
- Manuel `AddScoped<I…, …Manager>()` — service abstraction'ları

---

## SENİN GÖREVİN (BU PROJE İÇİN)

1. **Tara:** Tüm `Application/Features/*` klasörlerini ve Controller'ları gez.
2. **Sapma raporu çıkar** (kategori bazında): hangi feature'da hangi kural ihlal edilmiş?
   - [ ] Feature klasör yapısı sapmaları
   - [ ] Marker interface eksiklikleri (`ITenantAware`/`ISecuredRequest`/`ILoggableRequest` olmayan request'ler)
   - [ ] `[JsonIgnore] TenantId` eksiklikleri
   - [ ] Inline `throw new Exception/InvalidOperation/...` kullanımları (grep `throw new` — `BusinessException` dışındakileri listele)
   - [ ] Hardcoded mesajlar (localization key kullanmayan)
   - [ ] Eksik YAML dosyası (tr/en/ru hepsi olmalı)
   - [ ] Handler'ı ayrı dosyada olan command/query'ler
   - [ ] `BaseBusinessRules` extend etmeyen rule sınıfları
   - [ ] `EfRepositoryBase` kullanmayan veya raw `DbContext` ile çalışan handler'lar
   - [ ] Controller'da iş mantığı (`Mediator.Send` dışı kod)
   - [ ] Soft delete yerine hard delete yapan handler'lar
   - [ ] TenantId filtresi eksik query'ler (multi-tenant izolasyon kırılması — **kritik güvenlik**)
   - [ ] String literal `Roles` (constant kullanmayan)
   - [ ] `Pipeline behavior` kayıt sırası bozulmuş mu?
3. **Düzeltme planı sun** (önce göster, onay al, sonra uygula).
4. **Düzelt:** Her feature'ı tek tek refactor et, **küçük commit'ler** halinde ilerle.
5. **Migration etkisi** olan değişiklikleri ayrıca işaretle.

> **EN KRİTİK 3 KURAL:**
> 1. **TenantId filter eksikliği** = veri sızıntısı. Multi-tenant her sorguda `e.TenantId == request.TenantId` olacak.
> 2. **Inline throw** = localization & error handling bozulur. Sadece `BusinessException` (+ Localization service).
> 3. **Marker interface eksikliği** = pipeline atlanır → unauthorized erişim, tenant resolve olmaz, log olmaz.
