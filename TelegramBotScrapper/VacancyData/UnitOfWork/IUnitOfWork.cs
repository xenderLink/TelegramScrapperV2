using TelegramBotScrapper.VacancyData.Repository;

namespace TelegramBotScrapper.VacancyData.UnitOfWork;

public interface IUnitOfWork
{
    public IHhRuVacancyRepository Vacancies { get; }
}