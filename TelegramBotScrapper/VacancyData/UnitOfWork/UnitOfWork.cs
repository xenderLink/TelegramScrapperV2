using Microsoft.Extensions.Configuration;
using TelegramBotScrapper.VacancyData.Repository;

namespace TelegramBotScrapper.VacancyData.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IConfiguration _configuration;

    private readonly IHhRuVacancyRepository _hhRepository;

    public UnitOfWork(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IHhRuVacancyRepository Vacancies 
    { 
        get
        {
            return _hhRepository ?? (new HhRuVacancyRepository(_configuration.GetConnectionString("Vacancies")));            
        } 
    }
}