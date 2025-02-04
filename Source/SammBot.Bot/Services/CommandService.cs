﻿#region License Information (GPLv3)
/*
 * Samm-Bot - A lightweight Discord.NET bot for moderation and other purposes.
 * Copyright (C) 2021-2023 Analog Feelings
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
#endregion

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using DinoBot.Core;
using DinoBot.Library.Extensions;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DinoBot.Services;

public class CommandService
{
    private DiscordShardedClient ShardedClient { get; }
    private IServiceProvider ServiceProvider { get; }
    private Logger BotLogger { get; }

    private InteractionService InteractionService { get; }
    private EventLoggingService EventLoggingService { get; }
    private RelayService RelayService { get; }

    public CommandService(IServiceProvider Services)
    {
        ServiceProvider = Services;
        
        InteractionService = ServiceProvider.GetRequiredService<InteractionService>();
        ShardedClient = ServiceProvider.GetRequiredService<DiscordShardedClient>();
        BotLogger = ServiceProvider.GetRequiredService<Logger>();
        EventLoggingService = ServiceProvider.GetRequiredService<EventLoggingService>();
        RelayService = ServiceProvider.GetRequiredService<RelayService>();
    }

    public async Task InitializeHandlerAsync()
    {
        await InteractionService.AddModulesAsync(Assembly.GetEntryAssembly(), ServiceProvider);

        ShardedClient.InteractionCreated += HandleInteractionAsync;
            
        InteractionService.InteractionExecuted += OnInteractionExecutedAsync;

        AddEventHandlersAsync();
    }

    private async Task OnInteractionExecutedAsync(ICommandInfo SlashCommand, IInteractionContext Context, IResult Result)
    {
        try
        {
            if (!Result.IsSuccess)
            {
                string finalMessage;

                EmbedBuilder replyEmbed = new EmbedBuilder().BuildErrorEmbed((ShardedInteractionContext)Context);

                switch (Result.Error)
                {
                    case InteractionCommandError.BadArgs:
                        finalMessage = $"You provided an incorrect number of parameters!\nUse the `/help " +
                                       $"{SlashCommand.Module.Name} {SlashCommand.Name}` command to see all of the parameters.";
                        break;
                    default:
                        finalMessage = Result.ErrorReason;
                        break;
                }

                replyEmbed.Description = finalMessage;

                if (Context.Interaction.HasResponded)
                    await Context.Interaction.FollowupAsync(embed: replyEmbed.Build(), ephemeral: true, allowedMentions: BotGlobals.Instance.AllowOnlyUsers);
                else
                    await Context.Interaction.RespondAsync(embed: replyEmbed.Build(), ephemeral: true, allowedMentions: BotGlobals.Instance.AllowOnlyUsers);
            }
        }
        catch (Exception ex)
        {
            BotLogger.LogException(ex);
        }
    }

    private async Task HandleInteractionAsync(SocketInteraction Interaction)
    {
        ShardedInteractionContext context = new ShardedInteractionContext(ShardedClient, Interaction);

        if (SettingsManager.Instance.LoadedConfig.OnlyOwnerMode)
        {
            IApplication botApplication = await ShardedClient.GetApplicationInfoAsync();

            if (Interaction.User.Id != botApplication.Owner.Id) return;
        }

        string formattedLog = SettingsManager.Instance.LoadedConfig.CommandLogFormat.Replace("%username%", Interaction.User.GetFullUsername())
            .Replace("%channelname%", Interaction.Channel.Name);
        
        BotLogger.Log(formattedLog, LogSeverity.Debug);

        await InteractionService.ExecuteCommandAsync(context, ServiceProvider);
    }
        
    private void AddEventHandlersAsync()
    {
        ShardedClient.UserJoined += EventLoggingService.OnUserJoinedAsync;
        ShardedClient.UserLeft += EventLoggingService.OnUserLeftAsync;

        ShardedClient.MessageReceived += RelayService.OnRelayMessageReceivedAsync;
        ShardedClient.MessageDeleted += EventLoggingService.OnMessageDeleted;
        ShardedClient.MessagesBulkDeleted += EventLoggingService.OnMessagesBulkDeleted;
            
        ShardedClient.RoleCreated += EventLoggingService.OnRoleCreated;
        ShardedClient.RoleUpdated += EventLoggingService.OnRoleUpdated;

        ShardedClient.UserBanned += EventLoggingService.OnUserBanned;
        ShardedClient.UserUnbanned += EventLoggingService.OnUserUnbanned;
    }
}