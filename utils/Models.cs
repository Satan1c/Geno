using DSharpPlus.Entities;
using System;
using System.Collections.Generic;

namespace Geno.models
{
    internal class Server
    {
        public string _id { get; set; }
        public string prefix { get; set; }
        public string muteRole { get; set; }

        public bool clearNicknames { get; set; }
        public bool allowManualNicknameChange { get; set; }
        public bool antiInvite { get; set; }

        public int antiSpamMode { get; set; }
        public int defenceLevel { get; set; }
        public byte warnsLimit { get; set; }

        public Server()
        {
        }

        public Server(DiscordGuild guild)
        {
            this._id = guild.Id.ToString();
            this.prefix = Bot.defPrefix;
            this.muteRole = string.Empty;

            this.clearNicknames = false;
            this.allowManualNicknameChange = true;
            this.antiInvite = false;

            this.antiSpamMode = (int)utils.AntiSpamMode.disabled;
            this.defenceLevel = (int)utils.DefenceLevel.soft;
            this.warnsLimit = 3;
        }
    }

    internal class User
    {
        public string _id { get; set; }
        public string messagesCount { get; set; }
        public string charsCount { get; set; }

        public int warns { get; set; }
        public int spamMsgCount { get; set; }

        public DateTime lastMsg { get; set; }
        public Dictionary<string, int> localWarns { get; set; }

        public User()
        {
        }

        public User(DiscordUser user)
        {
            this._id = user.Id.ToString();
            this.messagesCount = "0";
            this.charsCount = "0";
            this.warns = 0;
            this.spamMsgCount = 0;
            this.lastMsg = DateTime.MinValue;
            this.localWarns = new Dictionary<string, int>();
        }
    }
}