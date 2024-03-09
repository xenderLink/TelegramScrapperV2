using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using Update = Telegram.Bot.Types.Update;
using TelegramBotScrapper.TgBot.Helpers;

namespace TelegramBotScrapper.TgBot;

public sealed class Bot : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly TelegramBotClient botClient;
    private int oldBotMessageId = 0;
    private readonly string greetingMessage = "Добро пожаловать в парсер вакансий C#. Выберите сервис, в котором вас интересуют вакансии.";

    // BotConf
    ReceiverOptions receiverOptions;
    InlineKeyboardMarkup servicesKeyboard;
    private readonly  IVacancyServiceCollection _collection;

    public Bot(
        IVacancyServiceCollection collection,
        IConfiguration configuration
                ) 
    {
        _collection = collection;
        _configuration = configuration;

        botClient = new TelegramBotClient(_configuration.GetValue<string>("TelegramToken"));


        receiverOptions = new ()
        {
            AllowedUpdates = new UpdateType[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            }
        };

        servicesKeyboard = new (new []
        {
            new InlineKeyboardButton[] { new InlineKeyboardButton("HeadHunter") { CallbackData = "hh.ru" }},
            new InlineKeyboardButton[] { new InlineKeyboardButton("Работа.Ру") { CallbackData = "rabota.ru" }},                  
        });                        
    }

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {    
        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync, 
            pollingErrorHandler: HandlePollingAsync, receiverOptions,
            cancellationToken: cancellationToken);

        var user = await  botClient.GetMeAsync();
    }

    public async Task HandlePollingAsync(ITelegramBotClient client, Exception exception, CancellationToken token) {}

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type is UpdateType.Message)
        {
            if (update.Message.Text is "/start")
            {
                await VacancyServices(botClient, update, true);
            }
            else
            {
                await SendErrorMessage(botClient, update);
            }
        }
        else if (update.Type is UpdateType.CallbackQuery)   
        {
            if (update.CallbackQuery.Data is "hh.ru" || update.CallbackQuery.Data is "hh.ru cities")
            {
                var messageId = await _collection.GetService(ServiceType.Hhru).SendBlock(botClient, update, cancellationToken);

                if (messageId != 0)
                {
                    oldBotMessageId = messageId;
                }    
            }
            else if (update.CallbackQuery.Data is "rabota.ru")
            {
                await VacancyServices(botClient, update);
            }
            else if (update.CallbackQuery.Data is "services edit")
            {
                await VacancyServices(botClient, update, true);
            }
            else if (update.CallbackQuery.Data is "services send")
            {
                await VacancyServices(botClient, update);
            }
            else if (update.CallbackQuery.Message.Text.Contains("hh"))
            {
                var messageId = await _collection.GetService(ServiceType.Hhru).SendBlock(botClient, update, cancellationToken);

                if (messageId != 0)
                {
                    oldBotMessageId = messageId;
                }
            } 
            else 
            {
                var botMessage = await botClient
                    .SendTextMessageAsync(
                        chatId: update.CallbackQuery.Message.Chat.Id,
                        text: "Ошибка обработки кнопок. Нажмите /start, чтобы перезапустить парсер.");
                    
                oldBotMessageId = botMessage.MessageId;
            }            
        }
    }

    private async Task VacancyServices(ITelegramBotClient botClient, Update update, bool toEdit = false)
    {
        long chatId = 0;

        Message? botMessage;

        if (update.Type is UpdateType.CallbackQuery)
        {
            chatId = update.CallbackQuery.Message.Chat.Id;
        }
        else if (update.Type is UpdateType.Message)
        {
            chatId = update.Message.Chat.Id;

            try
            {
                await botClient.DeleteMessageAsync(chatId, update.Message.MessageId);
            } catch {}
        }

        if (toEdit)
        { 
            try
            {
                botMessage = await botClient
                    .EditMessageTextAsync(
                        chatId: chatId,
                        messageId: oldBotMessageId,
                        text: greetingMessage,
                        replyMarkup: servicesKeyboard);
            }
            catch
            {
                botMessage = await botClient
                    .SendTextMessageAsync(
                        chatId: chatId,
                        text: greetingMessage,
                        replyMarkup: servicesKeyboard);
                
                try
                {
                    await botClient.DeleteMessageAsync(chatId, oldBotMessageId);
                } catch {}
            }
        }
        else
        {
            botMessage = await botClient
                .SendTextMessageAsync(
                    chatId: chatId,
                    text: greetingMessage,
                    replyMarkup: servicesKeyboard);

            try
            {
                await botClient.DeleteMessageAsync(chatId, oldBotMessageId);
            } catch {}
        }

        oldBotMessageId = botMessage.MessageId;
    }

    private async Task SendErrorMessage (ITelegramBotClient botClient, Update update)
    {
        Message botMessage =  null!;

        try
        {
            botMessage = await botClient
                .EditMessageTextAsync(
                    chatId: update.Message.Chat.Id,
                    messageId: oldBotMessageId,
                    text: "Неправильная команда. Для старта бота нажмите /start");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("message to edit not found"))
            {
                botMessage = await botClient
                    .SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "Неправильная команда. Для старта бота нажмите /start"); 
            }  
        }

        try
        {
            await botClient.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);
        } catch {}

        if (botMessage is not null)
        {
           oldBotMessageId = botMessage.MessageId;
        }        
    }
}