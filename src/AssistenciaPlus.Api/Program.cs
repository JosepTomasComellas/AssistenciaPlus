using AssistenciaPlus.Api.Middleware;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AssistenciaPlus.Application.Interfaces;
using AssistenciaPlus.Application.Services;
using AssistenciaPlus.Core.Interfaces;
using AssistenciaPlus.Infrastructure.AI;
using AssistenciaPlus.Infrastructure.Email;
using AssistenciaPlus.Infrastructure.Excel;
using AssistenciaPlus.Infrastructure.Repositories;
using AssistenciaPlus.Infrastructure.Data;
using AssistenciaPlus.Infrastructure.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.Text;

// ── Serilog bootstrap ────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog complet ──────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
    {
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(builder.Configuration["App:LogPath"] ?? "/app/logs", "api-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30);
    });

    // ── PostgreSQL + EF Core (Domain-based AppDbContext) ─────
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.MigrationsAssembly("AssistenciaPlus.Infrastructure")
        )
        .UseSnakeCaseNamingConvention()
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    );

    // ── Redis ─────────────────────────────────────────────────
    var redisConn = builder.Configuration["Redis:ConnectionString"]
        ?? throw new InvalidOperationException("Redis connection string no configurada");

    var redisMultiplexer = ConnectionMultiplexer.Connect(redisConn);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);
    builder.Services.AddScoped<ICacheService, RedisCacheService>();

    // Persistir claus DataProtection a Redis — evita invalidar sessions en redeploy
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(redisMultiplexer, "AssistenciaPlus-DataProtection-Keys")
        .SetApplicationName("AssistenciaPlus");

    builder.Services.AddStackExchangeRedisCache(options =>
        options.Configuration = redisConn);
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(480);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

    // ── JWT ───────────────────────────────────────────────────
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT Secret no configurat");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };

            // Suport SignalR
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("NomesEquipDirectiu",
            p => p.RequireRole("EquipDirectiu"));
        options.AddPolicy("NomesEquipDirectiuIAdministratiu",
            p => p.RequireRole("EquipDirectiu", "Administratiu"));
        options.AddPolicy("QualsevolRol",
            p => p.RequireRole("Mestre", "EquipDirectiu", "Administratiu"));
    });

    // ── Repositoris de Domini (Application interfaces) ───────
    builder.Services.AddScoped<IAlumneRepository, AlumneRepository>();
    builder.Services.AddScoped<IGrupRepository, GrupRepository>();
    builder.Services.AddScoped<IAssistenciaRepository, AssistenciaRepository>();
    builder.Services.AddScoped<IUsuariRepository, UsuariRepository>();
    builder.Services.AddScoped<ICalendariRepository, CalendariRepository>();

    // ── Serveis d'aplicació ───────────────────────────────────
    builder.Services.AddScoped<IAssistenciaService, AssistenciaService>();
    builder.Services.AddScoped<IInformesService, InformesService>();

    // ── Serveis auxiliars ─────────────────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // ── Correu ────────────────────────────────────────────────
    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
    builder.Services.AddScoped<IEmailService, EmailService>();

    // ── Excel ─────────────────────────────────────────────────
    builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
    builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
    builder.Services.AddScoped<InformesExcelService>();

    // ── Ollama IA ─────────────────────────────────────────────
    builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));
    builder.Services.AddHttpClient<IOllamaService, OllamaService>();

    // ── API ───────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ── Swagger ───────────────────────────────────────────────
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AssistenciaPlus API",
            Version = "v1",
            Description = "API de gestió d'assistència escolar — Escola Primària"
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Token JWT: Bearer {token}",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    // ── CORS ──────────────────────────────────────────────────
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(
                    builder.Configuration["App:PublicUrl"] ?? "https://localhost",
                    "https://localhost:7001"
                )
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .WithHeaders("Authorization", "Content-Type", "X-Requested-With")
                .AllowCredentials()
        )
    );

    // ── Rate limiting (brute force login) ─────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("login", opt =>
        {
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── SignalR ───────────────────────────────────────────────
    builder.Services.AddSignalR();

    // ── Health checks ─────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
        .AddRedis(redisConn);

    var app = builder.Build();

    // ── Migracions automàtiques + seed ────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await AssistenciaPlus.Infrastructure.Data.Seed.SeedData.InitializeAsync(db);
        Log.Information("Base de dades inicialitzada correctament");
    }

    // ── Pipeline ──────────────────────────────────────────────
    // ── Fitxers estàtics (fotos pujades) ─────────────────────
    var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
    Directory.CreateDirectory(Path.Combine(uploadsPath, "alumnes"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "usuaris"));
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads",
        OnPrepareResponse = ctx =>
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=86400")
    });

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AssistenciaPlus API v1"));
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseCors();
    app.UseRateLimiter();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapHub<AttendanceHub>("/hubs/assistencia").RequireAuthorization();

    Log.Information("AssistenciaPlus API iniciada. Entorn: {Env}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error fatal a l'inici de l'API");
}
finally
{
    Log.CloseAndFlush();
}
