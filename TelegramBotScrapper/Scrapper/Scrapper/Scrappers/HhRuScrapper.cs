using System.Security.Cryptography;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SeleniumExtras.WaitHelpers;
using OpenQA.Selenium.Support.UI;
using TelegramBotScrapper.VacancyData.UnitOfWork;
using TelegramBotScrapper.VacancyData.Model;

namespace TelegramBotScrapper.Scrapper;

public sealed class HhRuVacScrapper : Scrapper
{
    private readonly IUnitOfWork _unitOfWork;

    public HhRuVacScrapper(IUnitOfWork  unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private readonly string[] inputs = { "C# Developer", "C# Разработчик", "ASP NET" };

    private readonly string[] cities = { "Челябинск", "Екатеринбург", "Москва", "Санкт-Петербург" };


    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach(var input in inputs)
        {
            tasks.Add(Task.Run (() => StartChromeDriver(input, cancellationToken)));
        }

        await Task.WhenAll(tasks); 
    }

    private async Task StartChromeDriver(string input, CancellationToken cancellationToken)
    {
        try
        {
            int failsCount = 3; // счётчик неудачных подключений

            while (true)
            {
                ChromeOptions options = new ();
                options.AddArguments(new string[] {
                        "--headless",
                        "--whitelisted-ips=\"\"",
                        "--disable-dev-shm-usage",
                        "--no-sandbox",
                           UserAgent,
                        "--window-size=1920,1050",
                        "--disable-gpu",
                        "--disable-logging",
                        "--disable-blink-features=AutomationControlled"} );

                var driver = new ChromeDriver(".", options, TimeSpan.FromMinutes(3));

                if (DriverIsFailed(driver))
                {
                    failsCount--;

                    if (failsCount == 0)
                    {
                        failsCount = 3;
                        await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
                    }

                    continue;
                }

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                    {
                        PollingInterval = TimeSpan.FromMilliseconds(200)
                    };

                if (await FailFindElement(driver, wait, input, cancellationToken))
                {
                    continue;
                }

                await Task.Delay(TimeSpan.FromHours(12), cancellationToken);
            }
        }
        catch(Exception e)
        {
            Console.WriteLine(e.ToString());    
        }          
    }

    private bool DriverIsFailed(IWebDriver driver)
    {
        bool isFailed = true;

        try
        {
            driver.Navigate().GoToUrl("https://hh.ru/");
            isFailed = false;
        }
        catch (WebDriverException)
        {
            driver.Dispose(); 
        }

        return isFailed;
    }

    private async Task<bool> FailFindElement(IWebDriver driver, WebDriverWait wait, string input, CancellationToken cancellationToken)
    {
        bool elementFailed = false;

        try
        {
            var vacancies = new HashSet<Vacancy>();
            
            wait.Until(ExpectedConditions
                .ElementToBeClickable(driver
                .FindElement(By.XPath("//button[@class='bloko-button bloko-button_kind-primary bloko-button_scale-small']"))))
                .Click();

            driver.FindElement(By.CssSelector("input[data-qa='search-input']")).SendKeys(input);
            await Task.Delay(RandomNumberGenerator.GetInt32(3000, 7001), cancellationToken);

            driver.FindElement(By.CssSelector("button[data-qa='search-button']")).Click();
            await Task.Delay(RandomNumberGenerator.GetInt32(3000, 7001), cancellationToken);  

            var uncheckElmt = driver
                .FindElement(By.XPath("//legend[text()='Регион']"))
                .FindElement(By.XPath("./../../.."))
                .FindElement(By.CssSelector("span[data-qa='serp__novafilter-title']"))
                .FindElement(By.XPath("./.."));

            wait.Until(ExpectedConditions.ElementToBeClickable(uncheckElmt));
            
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click()", uncheckElmt);
            await Task.Delay(RandomNumberGenerator.GetInt32(3000, 7001), cancellationToken); 

            IWebElement region = null;

            var showAll = wait.Until(ExpectedConditions
                .ElementToBeClickable(driver
                .FindElement(By.XPath("//button[@class='bloko-link bloko-link_pseudo' and text()='Показать все']"))));
             ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click()", showAll);


            for (int i = 0; i < cities.Length; i++)
            {
                try
                {
                    if (i > 0)
                    {
                        region.Clear();
                    }
                    
                    region = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//input[@placeholder='Поиск региона']"))); 
                    await Task.Delay(RandomNumberGenerator.GetInt32(3000, 7001), cancellationToken); 
                }
                catch (Exception)
                {
                    showAll = wait.Until(ExpectedConditions
                        .ElementToBeClickable(driver
                        .FindElement(By.XPath("//button[@class='bloko-link bloko-link_pseudo' and text()='Показать все']"))));
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click()", showAll);
                    await Task.Delay(RandomNumberGenerator.GetInt32(3000, 7001), cancellationToken); 

                    region = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//input[@placeholder='Поиск региона']")));
                    await Task.Delay(RandomNumberGenerator.GetInt32(3000, 7001), cancellationToken);                     
                }
                
                region.SendKeys(cities[i]);
                await Task.Delay(RandomNumberGenerator.GetInt32(3000, 7001), cancellationToken);

                driver
                    .FindElement(By.XPath($"//span[@data-qa='serp__novafilter-title' and text()='{cities[i]}']"))
                    .FindElement(By.XPath("./.."))
                    .Click();
                await Task.Delay(RandomNumberGenerator.GetInt32(3000, 7001), cancellationToken); 
            }

            do
            {
                // блоки с вакансиями
                var vacancyElements = driver
                    .FindElement(By.CssSelector("main[class='vacancy-serp-content']"))
                    .FindElements(By.XPath("//div[@class='vacancy-serp-item__layout']"));

                if (!vacancyElements.Any())
                {
                    break;
                }

                foreach(var element in vacancyElements)
                {
                    var anchor = element.FindElement(By.TagName("a"));
                    var vacancyId = Regex.Match(anchor.GetAttribute("href"), @"(?<=vacancy/)([0-9]+)").Value;
                    var city = element.FindElement(By.CssSelector("div[data-qa='vacancy-serp__vacancy-address']"));

                    vacancies.Add(new Vacancy() 
                        { 
                            vacancyId = vacancyId, 
                            Name = anchor.Text, 
                            Url = anchor.GetAttribute("href"), 
                            City = cities.FirstOrDefault(region => city.Text.Contains(region)) 
                        });
                }

                if (NextButtonExists(driver, wait) is false)
                {
                    break;
                }

                await Task.Delay(RandomNumberGenerator.GetInt32(3000, 7001), cancellationToken); 
            }
            while (true);

            await _unitOfWork.Vacancies.AddVacancies(vacancies, cancellationToken);

        }
        catch
        {
            elementFailed = true;
        }
        finally
        {
            driver.Dispose();
        }

        return elementFailed;
    }

    private bool NextButtonExists(IWebDriver driver, WebDriverWait wait) 
    {
        bool buttonExists = true;

        try
        {
            var nextButton = driver
                .FindElement(By.XPath("//span[text()='дальше']"))
                .FindElement(By.XPath("./.."));

            wait.Until(ExpectedConditions.ElementToBeClickable(nextButton));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click()", nextButton);
        }
        catch (NoSuchElementException)
        {
            buttonExists = false;
        }

        return buttonExists;
    }
}