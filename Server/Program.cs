using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using System.Text;

using TodoList.Server;
using TodoList.Shared.Data;
using TodoList.Shared.Settings;
using TodoList.Shared.Svcs.Interfaces;
using TodoList.Shared.Svcs.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

//Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.Configure<PasswordHashSettings>(builder.Configuration.GetRequiredSection("PasswordHashSettings"));

builder.Services.AddScoped<ITodoItemService, TodoItemService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetRequiredSection("MailSettings"));

builder.Services.AddScoped<IVerifyCodeService, VerifyCodeService>();
builder.Services.Configure<VerifyCodeSettings>(builder.Configuration.GetRequiredSection("VerifyCodeSettings"));

IConfigurationSection tokenSettingConf = builder.Configuration.GetRequiredSection("AuthorizeTokenSetting");
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.Configure<AuthorizeTokenSetting>(tokenSettingConf);
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

builder.Services.AddAutoMapper(typeof(CustomProfile));

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    IConfigurationSection jwt = builder.Configuration.GetRequiredSection("Jwt");
    option.SaveToken = true;
    option.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"])),
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
        option.SwaggerEndpoint("/swagger/v1/swagger.json", "Todoitem API");
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

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
