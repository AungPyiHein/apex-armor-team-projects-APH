using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MiniPos.Frontend;
using MiniPos.Frontend.Shared.Services;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddTransient<CookiesHandler>();
builder.Services.AddTransient<UnauthorizedHandler>();
builder.Services.AddSingleton<NotificationService>();

builder.Services.AddHttpClient("MiniPos.Api",
        client => client.BaseAddress = new Uri("http://localhost:5107/"))
    .AddHttpMessageHandler<CookiesHandler>()
    .AddHttpMessageHandler<UnauthorizedHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("MiniPos.Api"));

await builder.Build().RunAsync();