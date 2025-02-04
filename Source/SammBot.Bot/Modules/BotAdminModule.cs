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
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Discord.Interactions;
using JetBrains.Annotations;
using DinoBot.Core;
using DinoBot.Library;
using DinoBot.Library.Attributes;
using DinoBot.Library.Preconditions;

namespace DinoBot.Modules;

[PrettyName("Bot Administration")]
[Group("badmin", "Bot management commands. Bot owner only.")]
[ModuleEmoji("\U0001f4be")]
[RequireTeam]
public class BotAdminModule : InteractionModuleBase<ShardedInteractionContext>
{
    [UsedImplicitly] public Logger Logger { get; init; } = default!;

    [SlashCommand("listservers", "Shows a list of all the servers the bot is in.")]
    [DetailedDescription("Shows a list of the servers the bot is in, and their corresponding IDs.")]
    [RateLimit(3, 1)]
    public async Task<RuntimeResult> ServersAsync()
    {
        string builtMessage = "I am invited in the following servers:\n";
        string codeBlock = string.Empty;

        int i = 1;
        foreach (SocketGuild targetGuild in Context.Client.Guilds)
        {
            codeBlock += $"{i}. {targetGuild.Name} (ID {targetGuild.Id}) [Owner {targetGuild.Owner}]\n";
            i++;
        }

        builtMessage += Format.Code(codeBlock);

        await RespondAsync(builtMessage, ephemeral: true, allowedMentions: BotGlobals.Instance.AllowOnlyUsers);

        return ExecutionResult.Succesful();
    }

    [SlashCommand("shutdown", "Shuts the bot down.")]
    [DetailedDescription("Shuts the bot down. That's it.")]
    [RateLimit(1, 1)]
    public async Task<RuntimeResult> ShutdownAsync()
    {
        await RespondAsync($"{SettingsManager.BOT_NAME} will shut down.", ephemeral: true, allowedMentions: BotGlobals.Instance.AllowOnlyUsers);

        Logger.Log($"{SettingsManager.BOT_NAME} will shut down.\n\n", LogSeverity.Warning);

        Environment.Exit(0);

        return ExecutionResult.Succesful();
    }

    [SlashCommand("restart", "Restarts the bot.")]
    [DetailedDescription("Restarts the bot. That's it.")]
    [RateLimit(1, 1)]
    public async Task<RuntimeResult> RestartAsync()
    {
        await RespondAsync($"{SettingsManager.BOT_NAME} will restart.", ephemeral: true, allowedMentions: BotGlobals.Instance.AllowOnlyUsers);

        Logger.Log($"{SettingsManager.BOT_NAME} will restart.\n\n", LogSeverity.Warning);

        BotGlobals.RestartBot();

        return ExecutionResult.Succesful();
    }

    [SlashCommand("leaveserver", "Leaves the specified server.")]
    [DetailedDescription("Forces the bot to leave the specified guild.")]
    [RateLimit(3, 1)]
    public async Task<RuntimeResult> LeaveAsync([Summary(description: "The ID of the guild you want the bot to leave.")] string serverId)
    {
        if (!ulong.TryParse(serverId, out ulong guildId))
        {
            // Handle the case where the provided string is not a valid ulong.
            return ExecutionResult.FromError("Invalid server ID. Please provide a valid numeric ID.");
        }

        SocketGuild targetGuild = Context.Client.GetGuild(guildId);
        if (targetGuild == null)
        {
            return ExecutionResult.FromError("I am not currently in this guild!");
        }

        string targetGuildName = targetGuild.Name;
        await targetGuild.LeaveAsync();

        await RespondAsync($"Left the server \"{targetGuildName}\".", ephemeral: true, allowedMentions: BotGlobals.Instance.AllowOnlyUsers);

        return ExecutionResult.Succesful();
    }
}