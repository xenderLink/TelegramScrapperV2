namespace TelegramBotScrapper.VacancyData.Model;

public sealed class Vacancy
{
    public string vacancyId { get; set; }

    public string Name { get; set; }

    public string Url { get; set; }

    public string City { get; set; }

    public string Date { get; } = DateTime.Now.ToString("yyyy/MM/dd");
}