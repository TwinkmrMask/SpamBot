using System.Text;
using System.Xml;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

// ReSharper disable All
#pragma warning disable 0044
#pragma warning disable 0649
#pragma warning disable 8600
#pragma warning disable 8602
#pragma warning disable 8604
#pragma warning disable 8618
#pragma warning disable 8625

namespace SpamBot
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program : Settings
    {
        private static readonly VkApi? Api = new();
        private static Config Config;
        private static readonly Random rand = new();

        //Start method
        private static void Main()
        {
            try
            {
                Config = CheckConfig(ConfigFile);
                Api?.Authorize(new ApiAuthParams() { AccessToken = Config.Token.Group });
                Spam(Api);
            }
            catch (Exception exp)
            {
                CreateExpLog(exp.Message + '\n' + exp.Source + '\n' + exp.StackTrace, default);
            }
        }

        //Main methods
        private static void Spam(IVkApiCategories? api)
        {
            var (headers, templates, attachments) = ReadFiles(Config);
            Console.WriteLine("Start spam");

            for (var i = 2; ; i++)
            {
                try
                {
                    StringBuilder mes = new();
                    mes.Append(headers[Random(headers)]);
                    mes.Append(' ');
                    mes.Append(templates[Random(templates)]);
                    string[] attachment = attachments[Random(attachments)].Split('_');
                    Console.WriteLine("Next message", Encoding.UTF8);
                    SendMessage(api, mes.ToString(), i, GetPhoto(attachment));
                    CreateLog(i, mes.ToString(), attachment, Config.File.Logs);

                }
                catch (VkNet.Exception.ConversationAccessDeniedException exp)
                {
                    CreateExpLog(exp.Message, Config.File.Logs);
                    Spam(api);
                }
                catch (Exception exp) { CreateExpLog(exp.Message, Config.File.Logs); }
            }
        }
        private static void SendMessage(IVkApiCategories? api, string message, int chatId, IEnumerable<MediaAttachment>? attachment)
        {
            var @params = new MessagesSendParams
            {
                RandomId = rand.Next(),
                ChatId = chatId,
                Message = message,
                Attachments = attachment
            };
            api?.Messages.Send(@params);
            int sleep = int.Parse(IsDefault(text: "timer", expression: Config.Service.Timer, level: "service", configFile: ConfigFile));

            Console.WriteLine($"Timer: {sleep}", Encoding.UTF8);
            Thread.Sleep(sleep);
        }

        //Auxiliary methods
        static int Random(List<string> list) => rand.Next(0, list.Count);
        private static IEnumerable<Photo> GetPhoto(string[] attachment)
        {
            VkApi vkApi = new();
            vkApi.Authorize(new ApiAuthParams() { AccessToken = Config.Token.User });
            PhotoAlbumType albumType;

            if (long.Parse(attachment[2]) == 0) albumType = PhotoAlbumType.Profile;
            else albumType = PhotoAlbumType.Id(long.Parse(attachment[2]));

            IEnumerable<Photo> photo = vkApi.Photo.Get(new PhotoGetParams
            {
                OwnerId = long.Parse(attachment[0]),
                AlbumId = albumType,
                PhotoIds = new List<string>() { attachment[1] }
            });

            return photo;
        }
        static List<string> ReadFile(string file, string log)
        {
            List<string> _ = new();
            if (System.IO.File.Exists(file))
                using (StreamReader stream = new(file, Encoding.UTF8))
                    _ = stream.ReadToEnd().Split("\n").ToList();
            else CreateExpLog($"File {file} is not exists", log);
            return _;
        }
        static (List<string> headers, List<string> templates, List<string> attachments) ReadFiles(Config config) => 
            (ReadFile(config.File.Headers, config.File.Logs), 
            ReadFile(config.File.Templates, config.File.Logs),
            ReadFile(config.File.Attachments, config.File.Logs));
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
            "  <file>" + "\n"  +
            "    <path text = \"\" />" + "\n" +
            "    <logs text = \"\"/>" + "\n" +
            "    <header text = \"\"/>" + "\n" +
            "    <template text = \"\"/>" + "\n" +
            "    <attachment text = \"\"/>" + "\n" +
            "  </file>" + "\n" +
            "  <service>" + "\n" +
            "    <timer text = \"\"/> <!-- milliseconds -->" + "\n" +
            "  </service>" + "\n" +
            "</config>";
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
                        config.Token.Group = IsDefault("groupToken", general.SelectSingleNode("./groupToken")?.Attributes?["text"]?.Value, "token", configFile);
                        config.Token.User = IsDefault("userToken", general.SelectSingleNode("./userToken").Attributes["text"].Value, "token", configFile);
                        break;
                    case "file":
                        config.File.Path = IsDefault("path", general.SelectSingleNode("./path").Attributes["text"].Value, "file", configFile);
                        config.File.Logs = IsDefault("logs", general.SelectSingleNode("./logs").Attributes["text"].Value, "file", configFile);
                        config.File.Headers = IsDefault("header", general.SelectSingleNode("./header").Attributes["text"].Value, "file", configFile);
                        config.File.Templates = IsDefault("template", general.SelectSingleNode("./template").Attributes["text"].Value, "file", configFile);
                        config.File.Attachments = IsDefault("attachment", general.SelectSingleNode("./attachment").Attributes["text"].Value, "file", configFile);
                        break;
                    case "service":
                        config.Service.Timer = IsDefault("timer", general.SelectSingleNode("./timer").Attributes["text"].Value, "service", configFile);
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

            Console.Write($"Enter {text}: ", Encoding.UTF8);
            expression = Console.ReadLine();
            FillConfig(text, expression, level, configFile);
            return IsDefault(text, expression, level, configFile);
        }
        protected static void CreateLog(int chat, string text, string[] attachment, string file)
        {
            var mes = $"{DateTime.Now} Chat {chat}: {text}, Attachment: {attachment[0]}_{attachment[1]}";
            Console.WriteLine($"{DateTime.Now} Chat {chat}: {text}\n", Encoding.UTF8);

            if (!System.IO.File.Exists(file))
                System.IO.File.Create(file);

            Write(mes, file);
        }
        protected static void CreateExpLog(string exp, string file)
        {
            var mes = $"{DateTime.Now} Exception: {exp} ";
            Console.WriteLine(mes, Encoding.UTF8);

            if (System.IO.File.Exists(file))
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
        public Token Token { get; set; } = new Token();
        public File File { get; set; } = new File();
        public Service Service { get; set; } = new Service();
    }
    class Token
    {
        public string Group { get; set; }
        public string User { get; set; }
    }
    class File
    {
        private static string path;
        private static string headers;
        private static string templates;
        private static string attachments;
        private static string logs;
        public string Path 
        {
            get
            {
                return path;
            }
            set {
                if(!Directory.Exists(value))
                    Directory.CreateDirectory(value);
                path = value;
            } 
        }
        public string Headers
        {
            get { return this.Path + headers; }
            set { headers = value; } 
        }
        public string Templates
        {
            get { return this.Path + templates; }
            set { templates = value; }
        }
        public string Attachments
        {
            get { return this.Path + attachments; }
            set { attachments = value; }
        }
        public string Logs
        {
            get { return this.Path + logs; }
            set { logs = value; }
        }
    }
    class Service
    {
        public string Timer { get; set; }
    }

}