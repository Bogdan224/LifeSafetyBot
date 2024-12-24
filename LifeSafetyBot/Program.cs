using LifeSafetyBot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace LifeSafetyBot
{
    public class TestState
    {
        public bool WaitForAnswer { get; set; } = false;
        public string Answer { get; set; } = "";
    }

    public class Program
    {
        private static TelegramBotClient bot;
        private static readonly string token = "7664998270:AAHgI13c9OE4X8UZimjnLqNzVXv53gGeJyc"; // Замените на свой токен
        private static ConcurrentDictionary<long, TestState> _userTests = new ConcurrentDictionary<long, TestState>();

        public static void Main()
        {
            bot = new TelegramBotClient(token);

            bot.OnError += OnError;
            bot.OnMessage += OnMessage;
            bot.OnUpdate += OnUpdate;

            //Console.WriteLine("Нажмите любую клавишу для выхода.");
            Console.ReadKey();

        }

        async static void StartTesting(Message msg)
        {
            TestState testState = new TestState();
            _userTests[msg.Chat.Id] = testState;

            var inlineMarkup = new InlineKeyboardMarkup();
            inlineMarkup.AddButton("Начать");
            Console.WriteLine(msg.Chat.Id);
            await bot.SendMessage(msg.Chat.Id,
                        "<b><u>Важная информация</u></b>:\n" +
                        "Все вопросы требуют <b>НЕСКОЛЬКО</b> вариантов ответов. Для того, чтобы выбрать вариант ответа, " +
                        "нужно нажать на кнопку с этим вариантом под вопросом. Когда вы выбрали варианты ответов, нужно нажать " +
                        "на кнопку <b>Отправить</b> в самом низу. Удачи!",
                        parseMode: ParseMode.Html, replyMarkup: inlineMarkup);
            testState.WaitForAnswer = true;
            while (testState.WaitForAnswer)
            {
                await Task.Delay(100);
            }
            

            testState.WaitForAnswer = false;
            Test test = new Test();
            List<MedicalKit> kits = MedicalKitExtention.AllMedicalKits();
            inlineMarkup = new InlineKeyboardMarkup();
            for (int i = 0; i < kits.Count; i++)
            {
                if (i % 3 == 0 && i != 0) inlineMarkup.AddNewRow();
                inlineMarkup.AddButton(kits[i].GetDescription());
            }
            inlineMarkup.AddNewRow();
            inlineMarkup.AddButton("Ответить");

            string question = test.GetQuestion();
            
            while (true)
            {
                testState.Answer = "";
                if (question == null) break;
                List<MedicalKit> answers = new List<MedicalKit>();
                await bot.SendMessage(msg.Chat.Id, question);
                await bot.SendMessage(msg.Chat.Id, "Окажите первую помощь пострадавшему, используя предметы из аптечки:", replyMarkup: inlineMarkup);

                while (testState.Answer != "Ответить")
                {
                    testState.WaitForAnswer = true;
                    while (testState.WaitForAnswer)
                    {
                        await Task.Delay(100);
                    }
                    if (testState.Answer != "Ответить" && !answers.Contains(testState.Answer.ToMedicalKit()))
                    {
                        answers.Add(testState.Answer.ToMedicalKit());
                    }
                }
                StringBuilder str = new StringBuilder("Ваш ответ: ");
                foreach (var item in answers)
                {
                    str.Append(item.GetDescription() + ", ");
                }
                str.Remove(str.Length - 2, 2);
                await bot.SendMessage(msg.Chat.Id, str.ToString());
                await bot.SendMessage(msg.Chat.Id, test.CheckAnswers(answers));

                question = test.GetQuestion();
            }
            await bot.SendMessage(msg.Chat.Id, test.GetResult());
            await OnCommand("/start", "", msg);
        }

        async static Task OnError(Exception exception, HandleErrorSource source)
        {
            await Task.Run(() => Console.WriteLine(exception));
        }

        async static Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Text is not { } text)
                Console.WriteLine($"Received a message of type {msg.Type}");
            else if (text.StartsWith('/'.ToString()))
            {
                var space = text.IndexOf(' ');
                if (space < 0) space = text.Length;
                var command = text[..space].ToLower();
                await OnCommand(command, text[space..].TrimStart(), msg);
            }
            else
                await OnTextMessage(msg);
        }

        async static Task OnTextMessage(Message msg) // received a text message that is not a command
        {
            Console.WriteLine($"Received text '{msg.Text}' in {msg.Chat}");
            await OnCommand("/start", "", msg); // for now we redirect to command /start
        }

        async static Task OnCommand(string command, string args, Message msg)
        {
            Console.WriteLine($"Received command: {command} {args}");
            switch (command)
            {
                case "/start":
                    await bot.SendMessage(msg.Chat,
                        "<b><u>Меню</u></b>:\n" +
                        "/start_test - Начать тест\n",
                        parseMode: ParseMode.Html);
                    break;
                case "/start_test":
                    StartTesting(msg);
                    break;
            }
        }

        async static Task OnUpdate(Update update)
        {
            switch (update)
            {
                case { CallbackQuery: { } callbackQuery }: await OnCallbackQuery(callbackQuery); break;
                default: Console.WriteLine($"Received unhandled update {update.Type}"); break;
            };
        }

        async static Task OnCallbackQuery(CallbackQuery callbackQuery)
        {
            _userTests[callbackQuery.Message.Chat.Id].Answer = callbackQuery.Data;
            _userTests[callbackQuery.Message.Chat.Id].WaitForAnswer = false;
            if (callbackQuery.Data == "Ответить" || callbackQuery.Data == "Начать")
                await RemoveKeyboard(bot, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
        }

        private static async Task RemoveKeyboard(ITelegramBotClient botClient, long chatId, int messageId)
        {
            try
            {
                await botClient.EditMessageReplyMarkup(
                     chatId: chatId,
                    messageId: messageId,
                     replyMarkup: null); // Устанавливаем null для удаления клавиатуры
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing keyboard: {ex.Message}");
            }
        }
    }
}