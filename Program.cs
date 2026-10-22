using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Options;
using NguyenHoaiPhong_1681.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Options
builder.Services.Configure<AppOptions>(config.GetSection(AppOptions.Section));
builder.Services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
builder.Services.Configure<GoogleOptions>(config.GetSection(GoogleOptions.Section));
builder.Services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.Section));
builder.Services.Configure<TwilioOptions>(config.GetSection(TwilioOptions.Section));
builder.Services.Configure<FaceOptions>(config.GetSection(FaceOptions.Section));
builder.Services.Configure<QrOptions>(config.GetSection(QrOptions.Section));
builder.Services.Configure<AuthOptions>(config.GetSection(AuthOptions.Section));
builder.Services.Configure<OtpOptions>(config.GetSection(OtpOptions.Section));
builder.Services.Configure<SeedOptions>(config.GetSection(SeedOptions.Section));

// Controllers + lỗi validate trả 400 kèm danh sách lỗi
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());
            var problem = new ValidationProblemDetails(ctx.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu không hợp lệ."
            };
            problem.Extensions["message"] = "Dữ liệu không hợp lệ.";
            problem.Extensions["errors"] = errors;
            return new BadRequestObjectResult(problem);
        };
    });

// JWT
var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "unique_name"
        };
    });
builder.Services.AddAuthorization(o =>
    o.AddPolicy("ManagerOrAdmin", p => p.RequireRole("Manager", "Admin")));

// CORS (chỉ Development)
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Rate limiter
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/problem+json";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new Dictionary<string, object>
        {
            ["status"] = 429,
            ["title"] = "Bạn thao tác quá nhanh, vui lòng thử lại sau.",
            ["message"] = "Bạn thao tác quá nhanh, vui lòng thử lại sau."
        }, ct);
    };

    static RateLimitPartition<string> PerIp(HttpContext ctx, int permit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });

    o.AddPolicy("auth", ctx => PerIp(ctx, 10));
    o.AddPolicy("kiosk", ctx => PerIp(ctx, 30));
    o.AddPolicy("challenge", ctx => PerIp(ctx, 30));
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "NguyenHoaiPhong_1681 - Chấm công API", Version = "v1" });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml);

    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập access token (không cần gõ chữ Bearer)."
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(config.GetConnectionString("Default"))
    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));
builder.Services.AddScoped<DbSeeder>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<ISmsSender, SmsSender>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IWorkLocationService, WorkLocationService>();
builder.Services.AddScoped<IShiftAssignmentService, ShiftAssignmentService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IFaceChallengeService, FaceChallengeService>();
builder.Services.AddSingleton<IFaceService, FaceService>();
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// Migrate + seed khi khởi động
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

// Nạp model khuôn mặt nền để request đầu tiên không bị chậm.
_ = Task.Run(() =>
{
    try { app.Services.GetRequiredService<IFaceService>().WarmUp(); }
    catch (Exception ex) { app.Logger.LogError(ex, "Không nạp được model khuôn mặt"); }
});

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
