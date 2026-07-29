using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AggilleDFe.Web;
using AggilleDFe.Web.Services;
using Microsoft.AspNetCore.Authorization;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5007";

builder.Services.AddMudServices();
builder.Services.AddScoped<ThemeService>();

builder.Services.AddScoped<TokenAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<TokenAuthenticationStateProvider>());
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddAuthorizationCore(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("Administrador", policy => policy.RequireClaim("administrador", "true"));
    options.AddPolicy("XmlsBaixados", policy => policy.RequireClaim("permissao", "xmls-baixados"));
    options.AddPolicy("Registros", policy => policy.RequireClaim("permissao", "registros"));
    options.AddPolicy("Empresas", policy => policy.RequireClaim("permissao", "empresas"));
    options.AddPolicy("Configuracao", policy => policy.RequireClaim("permissao", "configuracao"));
    options.AddPolicy("Importacao", policy => policy.RequireClaim("permissao", "importacao"));
    options.AddPolicy("BaixarXml", policy => policy.RequireClaim("permissao", "baixar-xml"));
});

builder.Services.AddTransient<TokenAuthorizationHandler>();
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiUrl))
    .AddHttpMessageHandler<TokenAuthorizationHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

await builder.Build().RunAsync();
