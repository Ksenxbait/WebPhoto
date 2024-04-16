using System;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace loneliness
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new TelegramBotClient(System.IO.File.ReadAllText("token.txt"));
            client.StartReceiving(Update, Error);
            Console.ReadLine();
        }

        private static async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;
            if (message != null)
            {
                if (message.Text.Equals("/start", StringComparison.CurrentCultureIgnoreCase))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Привет!\nС помощью этого бота вы можете получить скриншот страницы по URL адресу сайта.");
                    return;
                }
                else
                {
                    if (Uri.IsWellFormedUriString(message.Text, UriKind.RelativeOrAbsolute))
                    {
                        ChromeOptions options = new ChromeOptions();
                        options.AddArguments("start-maximized");

                        ChromeDriver driver = new ChromeDriver(options);
                        driver.Navigate().GoToUrl(message.Text);

                        Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                        screenshot.SaveAsFile("screenshot.png");

                        driver.Close();
                        driver.Quit();
                        Stream stream = System.IO.File.OpenRead("screenshot.png");
                        await botClient.SendDocumentAsync(message.Chat.Id, new InputFileStream(stream, "screenshot.png"), null, null, "Веб-сайт: " + Uri.EscapeUriString(message.Text), parseMode: ParseMode.Html);
                        return;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Неправильная ссылка!\nПопробуйте еще раз.");
                        return;
                    }
                }
            }
        }

        private static Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken token)
        {
            Console.WriteLine(exception);
            return Task.CompletedTask;
        }
    }

    class WebsiteCaptureMaker
    {
        private WebBrowser webBrowser;

        public WebsiteCaptureMaker()
        {
            webBrowser = new WebBrowser();
            webBrowser.ScrollBarsEnabled = false;
            webBrowser.ScriptErrorsSuppressed = false;
        }

        public Bitmap MakeScreenshot(string websiteURL)
        {
            webBrowser.Navigate(websiteURL);
            while (webBrowser.ReadyState != WebBrowserReadyState.Complete || webBrowser.IsBusy)
            {
                Application.DoEvents();
            }

            webBrowser.Width = webBrowser.Document.Body.ScrollRectangle.Width;
            webBrowser.Height = webBrowser.Document.Body.ScrollRectangle.Height;

            Bitmap websiteScreenshot = new Bitmap(webBrowser.Width, webBrowser.Height);
            webBrowser.DrawToBitmap(websiteScreenshot, new Rectangle(0, 0, webBrowser.Width, webBrowser.Height));

            return websiteScreenshot;
        }

        public void Dispose()
        {
            webBrowser.Dispose();
        }
    }
}