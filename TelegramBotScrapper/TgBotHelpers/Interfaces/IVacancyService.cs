using Telegram.Bot;
using Update = Telegram.Bot.Types.Update;

namespace TelegramBotScrapper.TgBot.Helpers;

public interface IVacancyService
{
    public Task<int> SendBlock(ITelegramBotClient client, Update update, CancellationToken cancellationToken);
}