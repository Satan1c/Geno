using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Geno.Utils;

public static class Extensions
{
    private static readonly Regex m_inviteRegex = new(
        "discord(?:\\.com|app\\.com|\\.gg)[\\/invite\\/]?(?:[a-zA-Z0-9\\-]{2,32})",
        RegexOptions.Compiled
    );

    private static readonly Regex m_linkRegex = new(
        "(http|ftp|https):\\/\\/([\\w_-]+(?:(?:\\.[\\w_-]+)+))([\\w.,@?^=%&:\\/~+#-]*[\\w@?^=%&\\/~+#-])",
        RegexOptions.Compiled
    );

    // [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    // internal static bool HasMode<T>(this T services, T target)
    // {
    //     return ((byte) services & (byte) target) != 0;
    // }

    internal static bool HasLink(this SocketMessage message) 
        => m_linkRegex.IsMatch(message.Content);

    public static string UserTag(this IUser user) 
        => user.Username + '#' + user.Discriminator;

    internal static bool TryGetRole(this SocketGuild guild, ulong id, out SocketRole role)
    {
        try
        {
            role = guild.GetRole(id);
            return true;
        }
        catch
        {
            role = null!;
            return false;
        }
    }

    internal static bool TryGetInvite(this BaseSocketClient client, string code, out RestInviteMetadata invite)
    {
        invite = null!;

        try
        {
            if (client.GetInviteAsync(code).Result is not { } res)
                return false;

            invite = res;
            return true;
        }
        catch { return false; }
    }
    
    internal static bool TryGetGuild(this DiscordRestClient client, ulong id, out RestGuild guild)
    {
        guild = null!;

        try
        {
            if (client.GetGuildAsync(id).Result is not RestGuild res)
                return false;

            guild = res;
            return true;
        }
        catch { return false; }
    }
    
    internal static bool TryGetUser(this DiscordRestClient client, ulong id, out RestUser user)
    {
        user = null!;

        try
        {
            if (client.GetUserAsync(id).Result is not RestUser res)
                return false;

            user = res;
            return true;
        }
        catch { return false; }
    }

    internal static bool TryGetGuildUser(this DiscordRestClient client, ulong guildId, ulong userId, out RestGuildUser user)
    {
        user = null!;

        try
        {
            if (client.GetGuildUserAsync(guildId, userId).Result is not RestGuildUser res)
                return false;
            
            user = res;
            return true;
        }
        catch { return false; }
    }
    
    internal static async ValueTask<bool> HasInvite(this IDiscordClient client, SocketUserMessage message,
        bool fetchForValidation = false,
        bool ignoreCurrentServer = false)
    {
        var match = m_inviteRegex.Match(message.Content);

        if (!match.Success)
            return false;

        if (!fetchForValidation)
            return true;

        var inviteId = match.Value.Split("/")[^1];
        var invite = await client.GetInviteAsync(inviteId);

        if (invite.Code == null)
            return false;

        return !ignoreCurrentServer || invite.GuildId != (message.Author as SocketGuildUser)?.Guild.Id;
    }
}