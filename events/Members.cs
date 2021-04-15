using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Geno.events
{
    internal class Members
    {
        public static async Task Add(DiscordClient client, GuildMemberAddEventArgs args)
        {
            var cfg = await utils.Utils.GetConfig(args.Guild);
            if (!cfg.clearNicknames) return;

            await utils.Utils.Rename(args.Member);
            GC.Collect();
        }

        public static async Task Update(DiscordClient client, GuildMemberUpdateEventArgs args)
        {
            var cfg = await utils.Utils.GetConfig(args.Guild);

            if (!cfg.clearNicknames) return;
            if (cfg.allowManualNicknameChange)
            {
                var audit = await args.Guild.GetAuditLogsAsync(action_type: AuditLogActionType.MemberUpdate, limit: 1);
                var resp = await args.Guild.GetMemberAsync(audit.Count > 0 ? audit[0].UserResponsible.Id : args.Member.Id);

                if (!resp.IsBot && args.Member.Roles.Any((x) => (x.Permissions & Permissions.Administrator) != 0 || (x.Permissions & Permissions.ManageNicknames) != 0)) return;
            }

            await utils.Utils.Rename(args.Member);
            GC.Collect();
        }
    }
}