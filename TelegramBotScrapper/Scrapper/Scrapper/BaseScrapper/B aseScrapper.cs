using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;

namespace TelegramBotScrapper.Scrapper;

public abstract class Scrapper : BackgroundService
{
    protected string UserAgent 
    {
        get
        {
            string agent = string.Empty;

            switch (RandomNumberGenerator.GetInt32(1, 4))
            {
                case 1:
                    agent = "--user-agent=Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";
                    break;

                case 2:
                    agent = "--user-agent=Mozilla/5.0 (X11; CrOS x86_64 10066.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";
                    break;
                
                case 3:
                    agent = "--user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/604.1 Edg/114.0.100.0";
                    break;
            }

            return agent;
        }
    }

    protected abstract override Task ExecuteAsync(CancellationToken cancellationToken);
}