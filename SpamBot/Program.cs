using System.Text;
using System.Xml;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace SpamBot
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program : Service
    {
        private static readonly VkApi? Api = new VkApi();

        private static ((string? @group, string? user) token, (string? header, string? pattern, string? logs) file, (
            string? path, string? timer) service) _defaults;

        private static void Main()
        {
            _defaults = CheckConfig();
            if (string.IsNullOrEmpty(_defaults.token.group))
            {
                Console.Write("Enter token: ");
                _defaults.token.group = Console.ReadLine();
            }
            Api?.Authorize(new ApiAuthParams() {AccessToken = _defaults.token.group});
            Spam(Api, "", _defaults.service.path + _defaults.file.logs, (-213080157, 457239019));
        }

        private static void Spam(IVkApiCategories? api, string mes, string? logFile, (int author, int id) attachment)
        {
            for (var i = 2;; i++)
            {
                try
                {
                    Console.WriteLine("Next message");
                    SendMessage(api, mes, i, GetPhoto(attachment));
                    if (_defaults.service.timer == null)
                        FilConfig("timer", out _defaults.service.timer, "service");
                    Thread.Sleep(int.Parse(_defaults.service.timer!));
                    CreateLog(i, mes, _defaults.file.logs);
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
            vkApi.Authorize(new ApiAuthParams() {AccessToken = _defaults.token.user});

            var (author, id) = attachment;
            var photo = vkApi.Photo.Get(new PhotoGetParams
            {
                OwnerId = author,
                AlbumId = PhotoAlbumType.Profile,
                PhotoIds = new List<string>() {id.ToString()},
            });
            
            return photo;
        }

        /*
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
        */

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
    }

    internal class Service
    {
        private const string ConfigFile = "../../../Resources/config.xml";

        private static (XmlDocument document, XmlElement? root) OpenConfig(string config)
        {
            var document = new XmlDocument();
            document.Load(config);
            var root = document.DocumentElement;

            return (document, root);
        }

        //void CreateConfig()
        //{
            // StreamReader streamReader =
            // new(IDefaultSettings.DefaultPath + IDefaultSettings.Config, false, Encoding.UTF8);
        //}

        protected static ((string? group, string? user) token, (string? header, string? pattern, string? logs) file, (
            string? path, string? timer) service)
            CheckConfig()
        {
            ((string? group, string? user) token, (string? header, string? pattern, string? logs) file, (string? path,
                string? timer) service) config = default;

            if (!File.Exists(ConfigFile))
                throw new FileNotFoundException(message: "Config file is not exists");

            foreach (XmlNode general in OpenConfig(ConfigFile).root!)
            {
                switch (general.Name)
                {
                    case "token":
                        config.token.group = general.SelectSingleNode("./groupToken")?.Attributes?["text"]?.Value;
                        config.token.user = general.SelectSingleNode("./userToken")?.Attributes?["text"]?.Value;
                        break;
                    case "file":
                        config.file.header = general.SelectSingleNode("./header")?.Attributes?["text"]?.Value;
                        config.file.pattern = general.SelectSingleNode("./pattern")?.Attributes?["text"]?.Value;
                        config.file.logs = general.SelectSingleNode("./logs")?.Attributes?["text"]?.Value;
                        break;
                    case "service":
                        config.service.timer = general.SelectSingleNode("./timer")?.Attributes?["text"]?.Value;
                        config.service.path = general.SelectSingleNode("./path")?.Attributes?["text"]?.Value;
                        break;
                }
            }
            
            return config;
        }

        protected static void FilConfig(string text, out string? expression, string level)
        {
            Console.Write($"Enter {text}");
            expression = Console.ReadLine();
            
            var (document, root) = OpenConfig(ConfigFile);
            
            foreach (var general in root!.Cast<XmlNode>().Where(general => general.Name == text))
                general.SelectSingleNode($"./{level}/{text}")!.Attributes!["text"]!.Value = expression;
            document.Save(ConfigFile);
        }

        protected static void CreateLog(int chat, string text, string? file)
        {
            var mes = $"{DateTime.Now} Chat {chat}: {text}";
            Write(mes, file);
            Console.WriteLine(mes);
        }

        private static void Write(string text, string? file)
        {
            using StreamWriter stream = new(file!, true, Encoding.UTF8);
            stream.WriteLine(text);
            stream.Close();
        }
    }
}
