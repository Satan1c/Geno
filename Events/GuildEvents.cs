using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Geno.Utils;

namespace Geno.Events;

public class GuildEvents
{
    private readonly IDiscordClient m_client;
    public GuildEvents(DiscordShardedClient client)
    {
        m_client = client;
    }
    
    public async Task MessageReceived(SocketMessage message)
    {
        if (message.Source != MessageSource.User)
            return;
        var userMessage = (message as SocketUserMessage)!;

        var isLink = message.HasLink();
        var isInvite = await m_client.HasInvite(userMessage, true, true).ConfigureAwait(false);

        if (!isLink && !isInvite)
            return;

        // await (message as SocketUserMessage).ReplyAsync(
        //     isInvite ? "An invite was found" : "A link was found",
        //     allowedMentions: AllowedMentions.None
        // );

        await (userMessage.Author as SocketGuildUser)!.SetTimeOutAsync(TimeSpan.FromSeconds(15));
    }
}