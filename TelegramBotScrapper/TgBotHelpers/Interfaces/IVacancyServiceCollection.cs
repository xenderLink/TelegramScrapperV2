namespace TelegramBotScrapper.TgBot.Helpers;

public interface IVacancyServiceCollection
{
    public IVacancyService GetService(ServiceType service);
}