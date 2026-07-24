using AggilleDFe.API;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Application.Services;
using AggilleDFe.Domain.Interfaces;
using AggilleDFe.Infrastructure.Data;
using AggilleDFe.Infrastructure.Integrations;
using AggilleDFe.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string webClientCorsPolicy = "WebClient";

var origensPermitidas = builder.Configuration.GetSection("WebClientOrigins").Get<string[]>()
    ?? ["http://localhost:5071", "https://localhost:7170"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(webClientCorsPolicy, policy => policy
        .WithOrigins(origensPermitidas)
        .AllowAnyHeader()
        .AllowAnyMethod());
});
builder.Services.AddSwaggerDocumentation();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
builder.Services.AddScoped<IConfiguracaoService, ConfiguracaoService>();
builder.Services.AddScoped<IEmpresaRepository, EmpresaRepository>();
builder.Services.AddScoped<IEmpresaService, EmpresaService>();
builder.Services.AddScoped<ISefazStatusService, SefazStatusService>();
builder.Services.AddHttpClient<ICnpjConsultaService, CnpjWsConsultaService>(client =>
{
    client.BaseAddress = new Uri("https://publica.cnpj.ws/");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseHttpsRedirection();
app.UseCors(webClientCorsPolicy);

app.MapConfiguracaoEndpoints();
app.MapEmpresaEndpoints();

app.Run();
