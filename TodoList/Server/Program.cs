using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;

using System.Text;

using TodoList.Shared.Data;
using TodoList.Shared.Settings;
using TodoList.Shared.Svcs.Interfaces;
using TodoList.Shared.Svcs.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();

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

builder.Services.Configure<MailSetting>(builder.Configuration.GetSection("MailSetting"));
builder.Services.Configure<DistributedCacheEntryOptions>(builder.Configuration.GetSection("CacheOptions"));

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
                context.Response.Headers.Add("IS-TOKEN-EXPIRED", "true");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddSwaggerGen(option =>
{
});

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

app.UseEndpoints(conf =>
{
    conf.MapDefaultControllerRoute();
});

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
