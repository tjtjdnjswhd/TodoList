using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
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

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITodoItemService, TodoItemService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVerifyCodeService, VerifyCodeService>();
builder.Services.AddScoped<IJwtService, JwtService>();

string connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddSqlServer<TodoListDbContext>(connectionString, option =>
{
    option.MigrationsAssembly("TodoList.Migrations");
});

builder.Services.AddDistributedSqlServerCache(option =>
{
    option.ConnectionString = connectionString;
    option.SchemaName = "dbo";
    option.TableName = "RefreshTokenCache";
});

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.Configure<PasswordHashSettings>(builder.Configuration.GetSection("PasswordHashSettings"));
builder.Services.Configure<DistributedCacheEntryOptions>(builder.Configuration.GetSection("CacheOptions"));

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
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
                context.Response.Headers.Add("IS-ACCESS-TOKEN-EXPIRED", "true");
            }
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("accessToken", out string? accessToken))
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
        option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        option.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
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
        option.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
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
