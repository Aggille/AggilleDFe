using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;
using AggilleDFe.Infrastructure.Data;
using AggilleDFe.Infrastructure.Integrations;
using AggilleDFe.Infrastructure.Repositories;
using AggilleDFe.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Mesmo motivo do AggilleDFe.API/Program.cs - necessário para rodar como
// Serviço do Windows sem cair em erro 1053 no start.
builder.Services.AddWindowsService();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
builder.Services.AddScoped<IEmpresaRepository, EmpresaRepository>();
builder.Services.AddScoped<IXmlRepository, XmlRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IEmailNotificacaoService, EmailNotificacaoService>();
builder.Services.AddScoped<IDistribuicaoDfeService, DistribuicaoDfeService>();
builder.Services.AddScoped<IDistribuicaoLoteService, DistribuicaoLoteService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
