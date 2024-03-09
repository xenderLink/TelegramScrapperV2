namespace TelegramBotScrapper.TgBot.Helpers;

public sealed class VacancyServiceCollection : IVacancyServiceCollection
{
    private readonly HhRuService _hhRuService;

    public VacancyServiceCollection(HhRuService hhRuService)
    {
        _hhRuService = hhRuService;
    }

    public IVacancyService GetService(ServiceType service) => service switch
    {
        ServiceType.Hhru => _hhRuService,
        _=> throw new NotSupportedException(
            "This service is not supported!"
        )
    };  
}