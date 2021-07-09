using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Geno
{
    internal class Bot
    {
        #region variables

        public DiscordClient Client { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public Dictionary<string, string> cfg { get; set; }

        public const double smallCD = 5;
        public const double middleCD = 10;
        public const double largeCD = 15;
        public static string defPrefix { get; private set; }
        public static string token { get; private set; }
        public static MongoClient mongo { get; private set; }
        public static DiscordChannel reportChannel { get; set; }
        public static HttpClient HTTPClient { get; set; }
        public static Dictionary<string, string> help = new Dictionary<string, string>();

        #endregion variables

        public async Task RunAsync()
        {

            #region cfg
            /*
            using (var file = new StreamReader("config.json", new UTF8Encoding(false)))
            {
                var raw = await file.ReadToEndAsync().ConfigureAwait(false);
                cfg = JsonConvert.DeserializeObject<Dictionary<string, string>>(raw);
                defPrefix = cfg["prefix"];
                token = cfg["token"];
                mongo = new MongoClient(cfg["mongo"]);
            };
            */

            defPrefix = "g-";

            //Test
            token = "NzMxNTE1ODI3NjcyNzExMTk4.XwnLNA.6ECjGt-ZCRvqNGVpy0ehf9Nra1M";

            //Release
            //token = "NjQ4NTcwMzQxOTc0NzM2OTI2.XdwKMw.vnTwJNSOzT9jhKdtgQgX9sK8Hn4";

            mongo = new MongoClient("mongodb+srv://Geno:Atlas23Game@genodb.wrqdw.mongodb.net/myFirstDatabase?retryWrites=true&w=majority");

            #endregion cfg

            #region client

            HTTPClient = new HttpClient();

            Client = new DiscordClient(new DiscordConfiguration
            {
                AlwaysCacheMembers = false,
                MinimumLogLevel = LogLevel.None,
                Token = token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
            });

            Client.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromMinutes(3)
            });

            #endregion client

            #region commands

            Commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDefaultHelp = false,
                UseDefaultCommandHandler = false
            });

            Commands.SetHelpFormatter<commands.CustomHelp>();
            Commands.RegisterCommands<commands.Other>();
            Commands.RegisterCommands<commands.Moderation>();
            Commands.RegisterCommands<commands.Options>();

            #endregion commands

            #region events

            Client.MessageCreated += events.Commands.CommandHandler;
            Client.MessageCreated += events.Guilds.OnMessage;

            Client.Ready += events.Client.OnReady;
            Client.ClientErrored += events.Client.OnError;

            Client.GuildMemberAdded += events.Members.Add;
            Client.GuildMemberUpdated += events.Members.Update;

            Commands.CommandErrored += events.Commands.OnComError;

            #endregion events

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }
    }
}