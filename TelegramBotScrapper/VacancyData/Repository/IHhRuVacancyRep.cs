using TelegramBotScrapper.VacancyData.Model;

namespace TelegramBotScrapper.VacancyData.Repository;

public interface IHhRuVacancyRepository
{
    public Task<IReadOnlyList<(string Name, string Url)>> GetVacancies(string city, CancellationToken cancellationToken);
    public Task AddVacancies(HashSet<Vacancy> vacancies, CancellationToken cancellationToken);    
}