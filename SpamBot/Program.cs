using System.Security.Cryptography;
using System.Text;
using VkNet;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace SpamBot
{
    class Program : Service
    {
        private static readonly VkApi? Api = new VkApi();

        private const string Token =
            "ec5620b8d98c4ed4f2a2c447d6e9fd5dc4ca50e2ff9ed393f0a702eaa3ab8f7a131282f1302ddf79e14bd";

        private static void Main(string[] args)
        {
            Api?.Authorize(new ApiAuthParams() {AccessToken = Token});
            Spam(Api);
        }

        private static void Spam(VkApi? api)
        {
            while (true)
            {
                for (var i = 2;; i++)
                {
                    try
                    {
                        Thread.Sleep(15000);
                        Console.WriteLine("Next message");
                        const string mes = "Text";
                        SendMessage(api, mes, i);
                        CreateLog(i, mes);
                        Console.WriteLine($"i: {i}, Mes: {mes}");
                    }
                    catch (VkNet.Exception.PermissionToPerformThisActionException e)
                        when (e.Message ==
                              "Permission to perform this action is denied: the user was kicked out of the conversation")
                    {
                        Print(i, e.Message);
                    }
                    catch (VkNet.Exception.ConversationAccessDeniedException e)
                        when (e.Message == "You don't have access to this chat")
                    {
                        Print(i, $"Exp: {e.Message}");
                        break;

                    }
                }
            }
        }

        private static void Print(int i, string message) => CreateLog(i, message);

        private static void SendMessage(IVkApiCategories? api, string message, int chatId)
        {
            Random rnd = new();
            api?.Messages.Send(new MessagesSendParams
            {
                RandomId = rnd.Next(),
                ChatId = chatId,
                Message = message
            });
        }
    }

    internal class Service
    {
        void FileSubstitution()
        {
        }

        void Copy()
        {
        }

        private byte[] HashSum(string text) => new HMACSHA1().ComputeHash(Encoding.ASCII.GetBytes(text));
        
        void CreateConfig()
        {
        }
        
        void CheckConfig()
        {
        }

        void FillConfig()
        {
        }

        protected static void CreateLog(int chat, string text)
        {
            const string path = "../../../Resources/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using StreamWriter stream = new($"{path}logs.txt", true, Encoding.UTF8);
            var mes = $"{DateTime.Now.TimeOfDay.TryFormat("hh:mm")} Chat {chat}: {text}";
            stream.WriteLine(mes);
            stream.Close();
            Console.WriteLine(mes);
        }

        void Start()
        {
            CheckConfig();
        }

    }
}