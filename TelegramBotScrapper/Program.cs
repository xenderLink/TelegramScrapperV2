using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotScrapper.TgBot;
using TelegramBotScrapper.TgBot.Helpers;
using TelegramBotScrapper.Scrapper;
using TelegramBotScrapper.VacancyData.Repository;
using TelegramBotScrapper.VacancyData.UnitOfWork;

namespace TelegramBotScrapper;

class Program
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        var builder = Host.CreateApplicationBuilder(args);
        
        builder.Services.AddSingleton<IVacancyServiceCollection, VacancyServiceCollection>();
        builder.Services.AddScoped<HhRuService>();
        builder.Services.AddHostedService<Bot>();
        
        
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddTransient<IHhRuVacancyRepository, HhRuVacancyRepository>();
        
        builder.Services.AddHostedService<HhRuVacScrapper>();
        //builder.Services.AddHostedService<Bot>();
        
        var app = builder.Build();

        await app.RunAsync(cts.Token);
    }
}