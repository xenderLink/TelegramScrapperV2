using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotScrapper.VacancyData.UnitOfWork;
using Update = Telegram.Bot.Types.Update;

namespace TelegramBotScrapper.TgBot.Helpers;

public sealed class HhRuService : TgBotMessageBuilder, IVacancyService 
{
    protected override string greetingMessage { get; set; } = "Выберите город, в котором вас интересует список вакансий на hh.ru.";
    protected override InlineKeyboardButton citiesButton { get; set; } = new InlineKeyboardButton("К списку городов") { CallbackData = "hh.ru cities" };

    public HhRuService(IUnitOfWork unitOfWork) : base(unitOfWork) {}

    public async Task<int> SendBlock(ITelegramBotClient client, Update update, CancellationToken cancellationToken = default)
    {
        int oldBotMessageId = 0;

        switch(update.CallbackQuery.Data)
        {
            case "hh.ru":
            {
                oldBotMessageId = await EditMessage(client, update);
            }
            break;

            case "hh.ru cities":
            {
                oldBotMessageId = await SendCities(client, update);
            } 
            break;  

            case "Челябинск":
            {
                var vacancies = await _unitOfWork.Vacancies.GetVacancies("Челябинск", cancellationToken);

                if (vacancies.Count is 0)
                {
                    oldBotMessageId = await NoVacancies(client, update);
                }
                else
                {
                    Chlb = new VacancyBlock()
                    {
                        Service = "hh.ru",
                        City = "Челябинск", 
                        Vacancies = vacancies
                    };

                    StringBuilder sb = new ();

                    FirstChunkVacancies(Chlb, sb);
                    await SendVacancies(client, update, Chlb, sb, cancellationToken);
                }
            }
            break;

            case "Екатеринбург":
            {
                var vacancies = await _unitOfWork.Vacancies.GetVacancies("Екатеринбург", cancellationToken);

                if (vacancies.Count is 0)
                {
                    oldBotMessageId = await NoVacancies(client, update);
                }
                else
                {
                    Ekb = new VacancyBlock()
                    {
                        Service = "hh.ru",
                        City = "Екатеринбург",
                        Vacancies = vacancies
                    };

                    StringBuilder sb = new ();

                    FirstChunkVacancies(Ekb, sb);
                    await SendVacancies(client, update, Ekb, sb, cancellationToken);
                }
            }
            break;

            case "Москва":
            {
                var vacancies = await _unitOfWork.Vacancies.GetVacancies("Москва", cancellationToken);

                if (vacancies.Count is 0)
                {
                    oldBotMessageId = await NoVacancies(client, update);
                }
                else
                {
                    Msk = new VacancyBlock()
                    {
                        Service = "hh.ru",
                        City = "Москва",
                        Vacancies = vacancies
                    };

                    StringBuilder sb = new ();

                    FirstChunkVacancies(Msk, sb);
                    await SendVacancies(client, update, Msk, sb, cancellationToken);
                }
            }
            break;

            case "Санкт-Петербург":
            {
                var vacancies = await _unitOfWork.Vacancies.GetVacancies("Санкт-Петербург", cancellationToken);

                if (vacancies.Count is 0)
                {
                    oldBotMessageId = await NoVacancies(client, update);
                }
                else
                {
                    Spb = new VacancyBlock()
                    {
                        Service = "hh.ru",
                        City = "Санкт-Петербург",
                        Vacancies = vacancies
                    };

                    StringBuilder sb = new ();

                    FirstChunkVacancies(Spb, sb);
                    await SendVacancies(client, update, Spb, sb, cancellationToken);
                }
            }
            break;

            case "Далее":
            {
                if (update.CallbackQuery.Message.Text.Contains("Челябинск"))
                {
                    if (Chlb is null)
                    {
                        oldBotMessageId = await SendCities(client, update);
                    }
                    else
                    {
                        StringBuilder sb = new ();

                        NextChunkVacancies(Chlb, sb);
                        await SendVacancies(client, update, Chlb, sb, cancellationToken);
                        
                    }

                }
                else if (update.CallbackQuery.Message.Text.Contains("Екатеринбург"))
                {
                    if (Ekb is null)
                    {
                        oldBotMessageId = await SendCities(client, update);
                    }
                    else
                    {
                        StringBuilder sb = new ();

                        NextChunkVacancies(Ekb, sb);
                        await SendVacancies(client, update, Ekb, sb, cancellationToken);                        
                    }
                }
                else if (update.CallbackQuery.Message.Text.Contains("Москва"))
                {
                    if (Msk is null)
                    {
                        oldBotMessageId = await SendCities(client, update);
                    }
                    else
                    {
                        StringBuilder sb = new ();

                        NextChunkVacancies(Msk, sb);
                        await SendVacancies(client, update, Msk, sb, cancellationToken);                      
                    }

                }
                else if (update.CallbackQuery.Message.Text.Contains("Санкт-Петербург"))
                {
                    if (Spb is null)
                    {
                        oldBotMessageId = await SendCities(client, update);
                    }
                    else
                    {
                        StringBuilder sb = new ();

                        NextChunkVacancies(Spb, sb);
                        await SendVacancies(client, update, Spb, sb, cancellationToken);                        
                    }            
                }
            }
            break;
        }

        return oldBotMessageId;
    }
}