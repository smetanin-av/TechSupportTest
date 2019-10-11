using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TechSupportTest
{
    internal class Program
    {
        private static readonly HttpClient client = new HttpClient();

        internal static void Main(string[] args)
        {
            GetAppBaseUrl(args);

            while (true)
            {
                Console.WriteLine("\nВыберите команду:\n\t- A\tдобавление запроса;\n\t- G\tполучение сведений;\n\t- R\tотмена запроса;\n\t- Q\tвыход;");
                var command = Console.ReadKey();

                switch (command.Key)
                {
                    case ConsoleKey.A:
                        CreateIssues();
                        break;

                    case ConsoleKey.G:
                        DoForIssues("GetState");
                        break;

                    case ConsoleKey.R:
                        DoForIssues("Cancel");
                        break;

                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        return;
                }
            }
        }

        /// <summary>
        /// Получаем URL приложения из параметров или от пользователя
        /// </summary>
        private static void GetAppBaseUrl(string[] args)
        {
            var baseUrl = args.FirstOrDefault();
            if (baseUrl != null)
            {
                Console.WriteLine($"\nURL приложения: {baseUrl}");
            }

            do
            {
                baseUrl = baseUrl ?? PromptStr("URL приложения");
                try
                {
                    client.BaseAddress = new Uri(baseUrl);
                }
                catch (Exception e)
                {
                    baseUrl = null;
                    Console.WriteLine(e.Message);
                }
            } while (baseUrl == null);
        }

        /// <summary>
        /// Получение значения от пользователя.
        /// </summary>
        private static string PromptStr(string message)
        {
            Console.Write($"\n{message}: ");
            return Console.ReadLine() ?? string.Empty;
        }

        /// <summary>
        /// Получение значения от пользователя и преобразование к целому.
        /// </summary>
        private static int? PromptInt(string message)
        {
            try
            {
                return Convert.ToInt32(PromptStr(message));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Создание нескольких запросов. Запросы отправляются все сразу.
        /// </summary>
        private static void CreateIssues()
        {
            int? count;
            int? delayMin;
            int? delayMax;

            do
            {
                count = PromptInt("Количество запросов");
                delayMin = PromptInt("Минимальное время между запросами, мс");
                delayMax = PromptInt("Максимальное время между запросами, мс");
            } while (count == null || delayMin == null || delayMax == null);

            var tasks = new List<Task>();
            var generator = new Random();

            for (var index = 1; index <= count; index++)
            {
                tasks.Add(SendRequest("Create", new { text = $"issue #{DateTime.Now.Ticks}" }));

                if (index < count)
                {
                    var delay = generator.Next(delayMin.Value, delayMax.Value);
                    System.Threading.Thread.Sleep(delay);
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Выполение метода (получение статуса, отмена запроса) для нескольких запросов.
        /// </summary>
        private static void DoForIssues(string method)
        {
            long[] issuesIds;
            do
            {
                try
                {
                    issuesIds = PromptStr("Номера запросов через запятую или пробел")
                        .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => Convert.ToInt64(x))
                        .ToArray();
                }
                catch (Exception e)
                {
                    issuesIds = null;
                    Console.WriteLine(e.Message);
                }
            } while (issuesIds == null || !issuesIds.Any());

            var tasks = issuesIds
                .Select(x => SendRequest(method, new { issueId = x }))
                .ToArray();
            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Отправка http запроса с указанным методом и параметрами.
        /// </summary>
        private static async Task SendRequest(string method, object body)
        {
            try
            {
                var requestUrl = new Uri($"/Issues/{method}", UriKind.Relative);
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(requestUrl, content);

                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"\nВызов {method}\n\tкод ответа: {response.StatusCode};\n\tрезультат: {result};");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nОшибка при вызове {method}: \"{e.Message}\".");
            }
        }
    }
}