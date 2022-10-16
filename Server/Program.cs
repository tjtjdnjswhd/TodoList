using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using NLog;
using NLog.Web;

using System.Text;

using TodoList.Server;
using TodoList.Shared.Data;
using TodoList.Shared.Models;
using TodoList.Shared.Settings;
using TodoList.Shared.Svcs.Interfaces;
using TodoList.Shared.Svcs.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

//NLog
LogManager.Setup().SetupSerialization(s =>
{
    //LoginInfo password 감춤
    s.RegisterObjectTransformation<LoginInfo>(info => new
    {
        info.Email
    });

    //SignupInfo password 감춤
    s.RegisterObjectTransformation<SignupInfo>(info => new
    {
        info.Email,
        info.Name
    });

    //MailSettings password 감춤
    s.RegisterObjectTransformation<MailSettings>(settings => new
    {
        settings.From,
        settings.DisplayName,
        settings.Host,
        settings.Port,
        settings.EnableSsl
    });

    //JwtSettings secretKey 감춤
    s.RegisterObjectTransformation<JwtSettings>(settings => new
    {
        settings.Issuer,
        settings.Audience,
        settings.SecurityAlgorithmName
    });
});
builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    options.RequestBodyLogLimit = 4096;
    options.ResponseBodyLogLimit = 4096;
});

//Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.Configure<PasswordHashSettings>(builder.Configuration.GetRequiredSection("PasswordHashSettings"));

builder.Services.AddScoped<ITodoItemService, TodoItemService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetRequiredSection("MailSettings"));

builder.Services.AddScoped<IVerifyCodeService, VerifyCodeService>();
builder.Services.Configure<VerifyCodeSettings>(builder.Configuration.GetRequiredSection("VerifyCodeSettings"));

builder.Services.AddScoped<IJwtService, JwtService>();
IConfigurationSection jwtSettingsConf = builder.Configuration.GetRequiredSection("JwtSettings");
IConfigurationSection tokenSettingConf = builder.Configuration.GetRequiredSection("AuthorizeTokenSetting");
builder.Services.Configure<JwtSettings>(jwtSettingsConf);
builder.Services.Configure<AuthorizeTokenSetting>(tokenSettingConf);
JwtSettings jwtSettings = ConfigurationBinder.Get<JwtSettings>(jwtSettingsConf);
AuthorizeTokenSetting tokenSetting = ConfigurationBinder.Get<AuthorizeTokenSetting>(tokenSettingConf);

//DB Connection
string connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddSqlServer<TodoListDbContext>(connectionString, option =>
{
    option.MigrationsAssembly("TodoList.Migrations");
});

//IDistributedCache
builder.Services.AddDistributedSqlServerCache(option =>
{
    option.ConnectionString = connectionString;
    option.SchemaName = "dbo";
    option.TableName = "RefreshTokenCache";
});

//AutoMapper
builder.Services.AddAutoMapper(typeof(CustomProfile));

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    option.SaveToken = true;
    option.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero,
    };

    if (builder.Environment.IsDevelopment())
    {
        option.TokenValidationParameters.ValidateIssuer = false;
        option.TokenValidationParameters.ValidateAudience = false;
    }

    option.Events = new JwtBearerEvents()
    {
        OnAuthenticationFailed = context =>
        {
            //Access token 만료 시 헤더 추가해 알림
            //Refresh token 만료 시 IdentityController.RefreshAsync()에서 refresh token expired 헤더 추가해 알림
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add(tokenSetting.IsAccessTokenExpiredHeader, "true");
            }
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            //Cookie의 access token을 읽어 인증
            if (context.Request.Cookies.TryGetValue(tokenSetting.AccessTokenKey, out string? accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(option =>
    {
        OpenApiSecurityScheme securityScheme = new()
        {
            Description = $"Jwt Authorization cookie using the Bearer scheme. cookie[{tokenSetting.AccessTokenKey}] set from 'api/identity/login'. EX) {tokenSetting.AccessTokenKey}: 1vssa5vfs1savf;",
            Name = tokenSetting.AccessTokenKey,
            In = ParameterLocation.Cookie,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            Reference = new()
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        option.AddSecurityDefinition("Bearer", securityScheme);

        option.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            { securityScheme , new List<string>() }
        });

        string filePath = Path.Combine(AppContext.BaseDirectory, "TodoList.Server.xml");
        option.IncludeXmlComments(filePath);

        option.SchemaFilter<GenericTypeFilter>();

        option.SwaggerDoc("v1", new OpenApiInfo()
        {
            Title = "Todoitem API",
            Version = "v1",
        });
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI(option =>
    {
        option.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoList API");
    });
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpLogging();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
