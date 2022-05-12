using System.Security.Cryptography;
using System.Text;
using System.Xml;
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

        private static void CheckConfig()
        {
            
            string? token;
            string header;
            string pattern;
            
            if (!File.Exists(IDefaultSettings.DefaultPath + IDefaultSettings.Config))
                throw new Exception(message: "File is not exists");
            var document = new XmlDocument();
            var root = document.DocumentElement;
            foreach (var general in from XmlElement item in root
                     from XmlNode child in item.ChildNodes
                     where child is {HasChildNodes: true}
                     from XmlNode tag in child.ChildNodes
                     where tag.Name == "config"
                     from XmlNode general in tag.ChildNodes
                     select general)
            {
                if (general.Name != "token") continue;
                try
                {
                    token = general.Attributes?["text"]?.Value;
                    
                }
                catch (Exception exception)
                {
                    CreateLog(exception.Message);
                }
            }

            throw new Exception(message: "File is not exists");
        }

        void UpdateConfig()
        {
        }

        void FillConfig()
        {
        }

        protected static void CreateLog(int chat, string text)
        {
            var mes = $"{DateTime.Now} Chat {chat}: {text}";
            Write(mes);
            Console.WriteLine(mes);
        }

        private static void CreateLog(string exp)
        {
            var mes = $"{DateTime.Now} Exp: {exp}";
            Write(mes);
            Console.WriteLine(mes);
        }

        private static void Write(string text)
        {
            using StreamWriter stream = new(IDefaultSettings.DefaultPath + IDefaultSettings.Log, true, Encoding.UTF8);
            stream.WriteLine(text);
            stream.Close();
        }

        void Start()
        {
            CheckConfig();
        }
    }

    public interface IDefaultSettings
    {
        static string DefaultPath
        {
            get
            {
                string path = "../../../Resources/";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }

        }

        const string Headers = "headers.txt";
        const string Pattens = "patterns.txt";
        const string Log = "logs.txt";
        const string Config = "config.xml";
    }
}
