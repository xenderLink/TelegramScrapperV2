using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotScrapper.VacancyData.UnitOfWork;

namespace TelegramBotScrapper.TgBot.Helpers;

public abstract class TgBotMessageBuilder
{
    protected readonly IUnitOfWork _unitOfWork;
    protected VacancyBlock Chlb, Ekb, Msk, Spb;
    protected InlineKeyboardMarkup citiesKeyboard, navKeyboard, backToKeyboard, noVacanciesKeyboard;

    // сообщение с городами у сервиса  
    protected abstract string greetingMessage { get; set; }

    // у каждого наследника свой CallBackQuery
    protected abstract InlineKeyboardButton citiesButton { get; set; } 

    public TgBotMessageBuilder(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        
        citiesKeyboard = new (new []
        {
            new InlineKeyboardButton[] { new InlineKeyboardButton("Челябинск") { CallbackData = "Челябинск" },
                                            new InlineKeyboardButton("Екатеринбург") { CallbackData = "Екатеринбург" }
                                        },
                        
            new InlineKeyboardButton[] { new InlineKeyboardButton("Москва") { CallbackData = "Москва" },
                                            new InlineKeyboardButton("Санкт-Петербург") { CallbackData = "Санкт-Петербург" }
                                        },
            new InlineKeyboardButton[] { new InlineKeyboardButton("К списку сервисов") { CallbackData = "services edit" } } 
        });
        
        navKeyboard = new (new []
        {
            new InlineKeyboardButton[] { "Далее" },
            new InlineKeyboardButton[] { citiesButton,  new InlineKeyboardButton("К списку сервисов") { CallbackData = "services send" } }
        });
        
        backToKeyboard = new (new []
        {
            citiesButton, new InlineKeyboardButton("К списку сервисов") { CallbackData = "services send" }
        });

        noVacanciesKeyboard = new (new []
        {
            citiesButton, new InlineKeyboardButton("К списку сервисов") { CallbackData = "services edit" }
        });       
    }

    protected void FirstChunkVacancies(VacancyBlock block, StringBuilder sb)
    {
        block.RemainElements = block.Vacancies.Count;

        if (block.Vacancies.Count <= 10)
        {
            for (; block.Index < block.Vacancies.Count; block.Index++)
            {
                sb.Append($"<a href=\"{block.Vacancies[block.Index].Url}\">{block.Vacancies[block.Index].Name}\n</a>"); 
            }
                 
        }
        else
        {
            for (; block.Index < 10; block.Index++)
            {
                sb.Append($"<a href=\"{block.Vacancies[block.Index].Url}\">{block.Vacancies[block.Index].Name}\n</a>"); 
            }
        }
    }

    protected void NextChunkVacancies(VacancyBlock block, StringBuilder sb)
    {
        block.RemainElements = block.Vacancies.Count - block.Index;

        if (block.RemainElements > 10)
        {
            for (int j = block.Index + 10; block.Index < j; block.Index++)
            {
                sb.Append($"<a href=\"{block.Vacancies[block.Index].Url}\">{block.Vacancies[block.Index].Name}\n</a>"); 
            }
        }
        else
        {
            for (; block.Index < block.Vacancies.Count; block.Index++)
            {
                sb.Append($"<a href=\"{block.Vacancies[block.Index].Url}\">{block.Vacancies[block.Index].Name}\n</a>");  
            }

            block.Index = 0;
        }
    }

    protected async Task<int> NoVacancies(ITelegramBotClient botClient, Update update)
    {
        var botMessage = await botClient
            .SendTextMessageAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                text: $"В этом городе нет подходящих вакансий.",
                replyMarkup: noVacanciesKeyboard);

        return botMessage.MessageId;
    }

    protected async Task<int> SendCities(ITelegramBotClient botClient, Update update)
    {
        var botMessage = await botClient
            .SendTextMessageAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                text: greetingMessage,
                replyMarkup: citiesKeyboard);

        return botMessage.MessageId;
    }

    protected async Task<int> EditMessage(ITelegramBotClient botClient, Update update)
    {
        int messageId;
        
        try
        {
            var botMessage = await botClient
                .EditMessageTextAsync(
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    messageId: update.CallbackQuery.Message.MessageId,
                    text: greetingMessage,
                    replyMarkup: citiesKeyboard);

            messageId = botMessage.MessageId; 
        } 
        catch
        {
            messageId = await SendCities(botClient, update);            
        }

        return messageId;
    }

    protected async Task SendVacancies(ITelegramBotClient botClient, Update update, VacancyBlock block, StringBuilder sb, CancellationToken cancellationToken = default)
    {
        if (block.RemainElements <= 10)
        {
            await botClient
                .SendTextMessageAsync(
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: $"Список вакансий на {block.Service} в городе {block.City}:\n{sb}",
                    parseMode: ParseMode.Html,
                    replyMarkup: backToKeyboard,
                    cancellationToken: cancellationToken);
        }
        else
        {
            await botClient
                .SendTextMessageAsync(
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: $"Список вакансий на {block.Service} в городе {block.City}:\n{sb}",
                    parseMode: ParseMode.Html,
                    replyMarkup: navKeyboard);
        }
    }
}