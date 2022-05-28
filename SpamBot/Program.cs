using System.Text;
using System.Xml;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

// ReSharper disable All
#pragma warning disable 8600
#pragma warning disable 8602
#pragma warning disable 8603
#pragma warning disable 8604
#pragma warning disable 8618
#pragma warning disable 8625

namespace SpamBot
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program : Settings
    {
        private static readonly VkApi? Api = new();
        private static Config _config;

        private static void Main()
        {
            try
            {
                _config = CheckConfig(ConfigFile);
                Api?.Authorize(new ApiAuthParams() { AccessToken = _config.Token.Group });
                Spam(Api, default, _config.Token.Group + _config.Token.Group, default);
            }
            catch (Exception exp)
            {
                CreateExpLog(exp.Message + "\n" + exp.StackTrace, default);
            }
        }

        private static void Spam(IVkApiCategories? api, string mes, string? logFile, (int author, int id) attachment)
        {
            Console.WriteLine("Start spam");
            for (var i = 2; ; i++)
            {
                try
                {
                    if (attachment == default)
                        SendMessage(api, mes, i);
                    else
                        SendMessage(api, mes, i, GetPhoto(attachment));

                    Console.WriteLine("Next message");
                    CreateLog(i, mes, _config.File.Logs);
                    Thread.Sleep(int.Parse(IsDefault(text: "timer", expression: _config.Service.Timer, level: "service", configFile: ConfigFile)));
                }
                catch (VkNet.Exception.PermissionToPerformThisActionException e)
                    when (e.Message ==
                          "Permission to perform this action is denied: the user was kicked out of the conversation")
                {
                    CreateLog(i, e.Message, logFile);
                }
                catch (VkNet.Exception.ConversationAccessDeniedException e)
                    when (e.Message == "You don't have access to this chat")
                {
                    CreateLog(i, $"Exp: {e.Message}", logFile);
                    i = 1;
                }
            }
        }

        private static IEnumerable<Photo>? GetPhoto((int author, int id) attachment)
        {
            VkApi vkApi = new();
            vkApi.Authorize(new ApiAuthParams() { AccessToken = _config.Token.User });

            var (author, id) = attachment;
            var photo = vkApi.Photo.Get(new PhotoGetParams
            {
                OwnerId = author,
                AlbumId = PhotoAlbumType.Profile,
                PhotoIds = new List<string>() { id.ToString() },
            });

            return photo;
        }

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

        private static void SendMessage(IVkApiCategories? api, string message, int chatId,
            IEnumerable<MediaAttachment>? attachment)
        {
            Random rnd = new();
            var @params = new MessagesSendParams
            {
                RandomId = rnd.Next(),
                ChatId = chatId,
                Message = message,
                Attachments = attachment
            };
            api?.Messages.Send(@params);
        }

        void OpenFile(string file)
        {
            //List<string> data = new List<string>();
            StreamReader reader = new(file, Encoding.UTF8);            
            Console.WriteLine(reader.ReadToEnd());
            reader.Close();
        }

        static List<string> Get()
        {
            return default;
        }

    }

    internal class Settings
    {
        protected static string ConfigFile 
        {
            get
            {
                string path = "../../../Resources/";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path + "config.xml";
            }
        }

        private const string template = 
            "<?xml version = \"1.0\" encoding=\"utf-8\" ?>" + "\n" +
            "<config>" + "\n" +
            "  <token>" + "\n" +
            "    <groupToken text = \"\"/>" + "\n" +
            "    <userToken text = \"\"/>" + "\n" +
            "  </token>" + "\n" +
            "  <file>" + "\n" +
            "    <header text = \"\"/>" + "\n" +
            "    <pattern text = \"\"/>" + "\n" +
            "    <attachment text = \"\"/>" + "\n" +
            "  </file>" + "\n" +
            "  <service>" + "\n" +
            "    <timer text = \"\"/> <!-- milliseconds -->" + "\n" +
            "    <path text = \"\" />" + "\n" +
            "  </service>" + "\n" +
            "</config>";

        //delegates 
        //protected delegate string CheckConfigDelegate();
        //protected delegate string FillConfigDelegate(string text, string expression, string level, string configFile);

        //methods
        private static (XmlDocument document, XmlElement? root) OpenConfig(string config)
        {
            var document = new XmlDocument();
            document.Load(config);
            var root = document.DocumentElement;
            return (document, root);
        }

        static void CreateConfig(string configFile)
        {
            StreamWriter streamWriter = new(configFile, false, Encoding.UTF8);
            streamWriter.Write(template);
            streamWriter.Close();
        }

        protected static Config CheckConfig(string configFile)
        {
            Config config = new();

            if (!System.IO.File.Exists(configFile))
                CreateConfig(configFile);

            var root = OpenConfig(configFile).root;
            foreach (XmlNode general in root)
            {
                switch (general.Name)
                {
                    case "token":
                        //config.Token.Group = IsDefault("groupToken", general.SelectSingleNode("./groupToken")?.Attributes?["text"]?.Value, "token", configFile);
                        Console.WriteLine(general.SelectSingleNode("./groupToken").Attributes["text"].Value);
                        //config.Token.Group = general.SelectSingleNode("./groupToken").Attributes["text"].Value;
                        //Console.WriteLine(config.Token.Group);
                        config.Token.User = IsDefault("userToken", general.SelectSingleNode("./userToken").Attributes["text"].Value, "token", configFile);
                        break;
                    case "file":
                        config.File.Headers = IsDefault("header", general.SelectSingleNode("./header").Attributes["text"].Value, "file", configFile);
                        config.File.Patterns = IsDefault("pattern", general.SelectSingleNode("./pattern").Attributes["text"].Value, "file", configFile);
                        config.File.Logs = IsDefault("logs", general.SelectSingleNode("./logs").Attributes["text"].Value, "file", configFile);
                        break;
                    case "service":
                        config.Service.Timer = IsDefault("timer", general.SelectSingleNode("./timer").Attributes["text"].Value, "service", configFile);
                        config.Service.Path = IsDefault("path", general.SelectSingleNode("./path").Attributes["text"].Value, "service", configFile);
                        break;
                }
            }
            return config;
        }

        protected static void FillConfig(string text, string expression, string level, string configFile)
        {
            var (document, root) = OpenConfig(configFile);
            foreach (var general in root!.Cast<XmlNode>().Where(general => general.Name == level))
                general.SelectSingleNode($"./{text}")!.Attributes!["text"]!.Value = expression;
            document.Save(configFile);
        }

        protected static string IsDefault(string text, string expression, string level, string configFile)
        {
            if (!string.IsNullOrWhiteSpace(expression))
                return expression;

            Console.Write($"Enter {text}: ");
            expression = Console.ReadLine();
            FillConfig(text, expression, level, configFile);
            return IsDefault(expression, text, level, configFile);
        }

        protected static void CreateLog(int chat, string text, string file)
        {
            var mes = $"{DateTime.Now} Chat {chat}: {text}";
            Console.WriteLine(mes);

            if (file != default)
                Write(mes, file);
            else
                Console.WriteLine("The log cannot be created");
        }

        protected static void CreateExpLog(string exp, string file)
        {
            var mes = $"{DateTime.Now} Exception: {exp} ";
            Console.WriteLine(mes);

            if (file != default)
                Write(mes, file);
            else
                Console.WriteLine("The log cannot be created");
        }

        private static void Write(string text, string? file)
        {
            using StreamWriter stream = new(file!, true, Encoding.UTF8);
            stream.WriteLine(text);
            stream.Close();
        }
    }

    class Config
    {
        public Token Token { get; set; }
        public File File { get; set; }
        public Service Service { get; set; }
    }
        
    class Token
    {
        public string Group { get; set; }
        public string User { get; set; }
    }

    class File
    {
        public string Headers { get; set; }
        public string Patterns { get; set; }
        public string Attachments { get; set; }
        public string Logs { get; set; }
    }

    class Service
    {
        public string Timer { get; set; }
        public string Path { get; set; }
    }
}
