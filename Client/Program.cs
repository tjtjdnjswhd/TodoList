using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using TodoList.Client;
using TodoList.Client.Svcs.Interfaces;
using TodoList.Client.Svcs.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<ITodoItemService, TodoItemService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

builder.Services.AddScoped(sp => new HttpClient()
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
});

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddApiAuthorization();

await builder.Build().RunAsync();
