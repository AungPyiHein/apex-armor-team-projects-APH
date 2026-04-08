using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using POS.Frontend;

using POS.Frontend.Services.Products;
using POS.Frontend.Services.Categories;
using POS.Frontend.Services.Sales;

using POS.Frontend.Services.Merchants;
using POS.Frontend.Services.Inventory;
using POS.Frontend.Services.Users;
using POS.Frontend.Services;

using POS.Frontend.Services.Auth;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var backendUrl = builder.Configuration.GetValue<string>("BackendUrl") ?? "https://localhost:60763";

// Auth Setup
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

builder.Services.AddTransient<TokenInterceptor>();
builder.Services.AddHttpClient("BackendApi", client =>
{
    client.BaseAddress = new Uri(backendUrl);
})
.AddHttpMessageHandler<TokenInterceptor>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("BackendApi"));

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
