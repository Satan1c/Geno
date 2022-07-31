
using Discord;
using Discord.Interactions;
using Geno.Utils;

namespace Geno.Commands;

[Group("other", "other command group")]
public class Other : InteractionModuleBase<ShardedInteractionContext>
{
    [Group("fetch", "fetch commands sub group")]
    public class FetchCommands : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("guild", "fetch guild information by invite")]
        public async Task FetchGuild(string inviteCode)
        {
            if (!Context.Client.TryGetInvite(inviteCode.Split("/")[^1], out var invite))
            {
                await RespondAsync("Can't get info about this guild",
                    allowedMentions: AllowedMentions.None,
                    ephemeral: true);

                return;
            }

            var embed = new EmbedBuilder().ApplyData(invite);
        
            if (Context.Client.Rest.TryGetGuild(invite.GuildId ?? 0, out var guild))
                embed = embed.ApplyData(guild);

            await RespondAsync(embed: embed.Build(),
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }

        [SlashCommand("user", "fetch user information by id")]
        public async Task FetchUser(IUser rawUser)
        {
            if (!Context.Client.Rest.TryGetUser(rawUser.Id, out var user))
            {
                await RespondAsync("Can't get info about this user",
                    allowedMentions: AllowedMentions.None,
                    ephemeral: true);

                return;
            }

            var embed = new EmbedBuilder().ApplyData(user);
        
            if (Context.Client.Rest.TryGetGuildUser(Context.Guild.Id, user.Id, out var guildUser))
                embed = embed.ApplyData(guildUser);

            await RespondAsync(embed: embed.Build(),
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
    }
}