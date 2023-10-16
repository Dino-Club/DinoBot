using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SammBot.Bot.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SammBot.Bot.Services;

public class RelayService
{
    private IServiceProvider ServiceProvider { get; }
    private DiscordShardedClient ShardedClient { get; }
    private Logger BotLogger { get; }

    public RelayService(IServiceProvider Services)
    {
            ServiceProvider = Services;
            ShardedClient = ServiceProvider.GetRequiredService<DiscordShardedClient>();
            BotLogger = ServiceProvider.GetRequiredService<Logger>();
    }

    private readonly Dictionary<ulong, ulong> messageMapping = new Dictionary<ulong, ulong>();

    public async Task OnRelayMessageReceivedAsync(SocketMessage ReceivedMessage)
    {
        try
        {
            // Check if the message is from a bot to avoid an infinite loop.
            if (ReceivedMessage.Author.IsBot)
                return;

            // Define an array of channel IDs that you want to relay messages to.
            ulong[] relayChannelIds = { 1147530468208693258, 1147574268725563433 };

            // Define an array of moderator user IDs
            ulong[] moderatorUserIds = { 596773775404564481, 503277868168642560, 997397495695024130 };

            // Define an array of developer user IDs
            ulong[] devUserIds = { 596773775404564481 };

            if (relayChannelIds.Contains(ReceivedMessage.Channel.Id))
            {
                await ReceivedMessage.DeleteAsync();

                foreach (var guild in ShardedClient!.Guilds)
                {
                    var relayChannels = guild.TextChannels
                                             .Where(Channel => relayChannelIds.Contains(Channel.Id))
                                             .ToList();

                    foreach (var relayChannel in relayChannels)
                    {
                        EmbedBuilder relayEmbed = new EmbedBuilder();
                        SocketGuildChannel? receivedChannel = ReceivedMessage.Channel as SocketGuildChannel;
                        SocketGuild receivedGuild = receivedChannel!.Guild;

                        // Check if the message author is a moderator or developer
                        string authorName = ReceivedMessage.Author.GlobalName;
                        ulong authorId = ReceivedMessage.Author.Id;
                        string serverFooter;

                        if (moderatorUserIds.Contains(ReceivedMessage.Author.Id) && devUserIds.Contains(ReceivedMessage.Author.Id))
                        {
                            authorName = authorName + "🛡️ [MOD] ⚙️ [DEV]";
                        }
                        else if (moderatorUserIds.Contains(ReceivedMessage.Author.Id))
                        {
                            authorName = authorName + " 🛡️ [MOD]";
                        }
                        else if (devUserIds.Contains(ReceivedMessage.Author.Id))
                        {
                            authorName = authorName + " ⚙️ [DEV]";
                        }
                        else
                        {
                            authorName = authorName + " • " + "(" + authorId + ")";
                        }

                        if (receivedGuild.Owner.Id == ReceivedMessage.Author.Id)
                        {
                            serverFooter = receivedGuild.Name + " (Owner)";
                        }
                        else
                        {
                            serverFooter = receivedGuild.Name;
                        }

                        relayEmbed.WithAuthor($"{authorName}", ReceivedMessage.Author.GetAvatarUrl());
                        relayEmbed.WithTitle($"New Message from {receivedGuild.Name}");
                        relayEmbed.WithDescription(ReceivedMessage.Content);
                        relayEmbed.WithFooter(serverFooter, receivedGuild.IconUrl);
                        relayEmbed.WithCurrentTimestamp();

                        // Send the relay message
                        await relayChannel.SendMessageAsync(null, false, relayEmbed.Build());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            BotLogger.LogException(ex);
        }
    }
}