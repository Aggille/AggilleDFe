using AggilleDFe.Domain.Interfaces;
using AggilleDFe.Infrastructure.Data;
using AggilleDFe.Infrastructure.Repositories;
using AggilleDFe.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
