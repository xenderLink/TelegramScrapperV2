namespace TelegramBotScrapper.TgBot.Helpers;

public sealed record VacancyBlock
{
    public int Index { get; set; } = 0;
    public int RemainElements { get; set; } = 0;
    public string Service { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public IReadOnlyList<(string Name, string Url)> Vacancies { get; init; }
}