using AggilleDFe.API;
using AggilleDFe.API.Auth;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Application.Services;
using AggilleDFe.Domain.Interfaces;
using AggilleDFe.Infrastructure.Data;
using AggilleDFe.Infrastructure.Integrations;
using AggilleDFe.Infrastructure.Repositories;
using AggilleDFe.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Necessário para rodar como Serviço do Windows (sc.exe create ...) — sem
// isso, o processo sobe normalmente mas nunca avisa o Gerenciador de
// Controle de Serviços que iniciou com sucesso, e o Windows derruba com
// erro 1053 ("o serviço não respondeu à solicitação de início a tempo").
// Não afeta rodar via "dotnet run"/console — só ativa quando de fato
// executado como serviço.
builder.Host.UseWindowsService();

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
builder.Services.AddScoped<IXmlRepository, XmlRepository>();
builder.Services.AddScoped<IXmlService, XmlService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<ISefazStatusService, SefazStatusService>();
builder.Services.AddScoped<IDistribuicaoDfeService, DistribuicaoDfeService>();
builder.Services.AddScoped<IDistribuicaoLoteService, DistribuicaoLoteService>();
builder.Services.AddScoped<IXmlArquivoService, XmlArquivoService>();
builder.Services.AddScoped<IManifestacaoService, ManifestacaoService>();
builder.Services.AddScoped<IDanfeService, DanfeService>();
builder.Services.AddScoped<IXmlImportService, XmlImportService>();
builder.Services.AddHttpClient<ICnpjConsultaService, CnpjWsConsultaService>(client =>
{
    client.BaseAddress = new Uri("https://publica.cnpj.ws/");
});

builder.Services.AddAuthentication("BasicApi")
    .AddScheme<AuthenticationSchemeOptions, BasicApiAuthenticationHandler>("BasicApi", null);
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseHttpsRedirection();
app.UseCors(webClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapConfiguracaoEndpoints();
app.MapEmpresaEndpoints();
app.MapLogEndpoints();
app.MapXmlEndpoints();
app.MapDfeEndpoints();

app.Run();
