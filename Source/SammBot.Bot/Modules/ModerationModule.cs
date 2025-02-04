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
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using DinoBot.Core;
using DinoBot.Database;
using DinoBot.Library;
using DinoBot.Library.Attributes;
using DinoBot.Library.Database.Models;
using DinoBot.Library.Extensions;
using DinoBot.Library.Preconditions;

namespace DinoBot.Modules;

[PrettyName("Moderation")]
[Group("mod", "Moderation commands like kick, ban, mute, etc.")]
[ModuleEmoji("\U0001f9d1\u200D\u2696\uFE0F")]
public class ModerationModule : InteractionModuleBase<ShardedInteractionContext>
{
    [SlashCommand("ban", "Bans a user with a reason.")]
    [DetailedDescription("Bans a user from the server with the set reason.")]
    [RateLimit(1, 2)]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task<RuntimeResult> BanUserAsync([Summary(description: "The user you want to ban.")] SocketGuildUser TargetUser,
        [Summary(description: "The amount of days the bot will delete.")] int PruneDays,
        [Summary(description: "The reason of the ban.")] string? Reason = null)
    {
        string banReason = Reason ?? "No reason specified.";

        await Context.Guild.AddBanAsync(TargetUser, PruneDays, banReason);

        EmbedBuilder replyEmbed = new EmbedBuilder().BuildSuccessEmbed(Context)
                                                    .WithDescription($"Successfully banned user `{TargetUser.GetFullUsername()}`.");

        replyEmbed.AddField("\U0001f914 Reason", banReason);
        replyEmbed.AddField("\U0001f5d3\uFE0F Prune Days", $"{PruneDays} day(s).");

        await RespondAsync(embed: replyEmbed.Build(), allowedMentions: BotGlobals.Instance.AllowOnlyUsers);

        return ExecutionResult.Succesful();
    }

    [SlashCommand("kick", "Kicks a user with a reason.")]
    [DetailedDescription("Kicks a user from the server with the set reason.")]
    [RateLimit(1, 2)]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.KickMembers)]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task<RuntimeResult> KickUserAsync([Summary(description: "The user you want to kick.")] SocketGuildUser TargetUser,
        [Summary(description: "The reason of the kick.")] string? Reason = null)
    {
        string kickReason = Reason ?? "No reason specified.";

        await TargetUser.KickAsync(kickReason);

        EmbedBuilder replyEmbed = new EmbedBuilder().BuildSuccessEmbed(Context)
                                                    .WithDescription($"Successfully kicked user `{TargetUser.GetFullUsername()}`.");

        replyEmbed.AddField("\U0001f914 Reason", kickReason);

        await RespondAsync(embed: replyEmbed.Build(), allowedMentions: BotGlobals.Instance.AllowOnlyUsers);

        return ExecutionResult.Succesful();
    }

    [SlashCommand("warn", "Warns a user with a reason.")]
    [DetailedDescription("Warns a user with a reason. Warnings will be stored in the bot's database, and you will be able to list them afterwards.")]
    [RateLimit(1, 2)]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task<RuntimeResult> WarnUserAsync([Summary(description: "The user you want to warn.")] SocketGuildUser TargetUser,
        [Summary(description: "The reason of the warn.")] string Reason)
    {
        if (Reason.Length > 512)
            return ExecutionResult.FromError("Warning reason must not exceed 512 characters.");

        await DeferAsync();

        using (BotDatabase botDatabase = new BotDatabase())
        {
            UserWarning newWarning = new UserWarning
            {
                UserId = TargetUser.Id,
                GuildId = Context.Guild.Id,
                Reason = Reason,
                Date = Context.Interaction.CreatedAt.ToUnixTimeSeconds()
            };

            await botDatabase.UserWarnings.AddAsync(newWarning);
            await botDatabase.SaveChangesAsync();

            EmbedBuilder replyEmbed = new EmbedBuilder().BuildSuccessEmbed(Context)
                                                        .WithDescription($"Successfully warned user <@{TargetUser.Id}>.");

            replyEmbed.AddField("\U0001f914 Reason", Reason);
            replyEmbed.AddField("\U0001f6c2 Warn ID", newWarning.Id);

            //DM the user about it.
            try
            {
                EmbedBuilder directMessageEmbed = new EmbedBuilder().BuildDefaultEmbed(Context)
                                                                    .WithTitle("\u26A0\uFE0F You have been warned")
                                                                    .WithDescription("You may see all of your warnings with the `/mod warns` command in the server.")
                                                                    .WithColor(Constants.BadColor);

                directMessageEmbed.AddField("\U0001faaa Server", Context.Guild.Name);
                directMessageEmbed.AddField("\U0001f914 Reason", Reason);
                directMessageEmbed.AddField("\U0001f6c2 Warn ID", newWarning.Id);

                await TargetUser.SendMessageAsync(embed: directMessageEmbed.Build());
            }
            catch (Exception)
            {
                replyEmbed.Description += "\nI could not message the user about this warning.";
            }

            await FollowupAsync(embed: replyEmbed.Build(), allowedMentions: BotGlobals.Instance.AllowOnlyUsers);
        }

        return ExecutionResult.Succesful();
    }

    [SlashCommand("unwarn", "Removes a warn from a user.")]
    [DetailedDescription("Removes the warning with the specified ID.")]
    [RateLimit(1, 2)]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task<RuntimeResult> RemoveWarnAsync([Summary(description: "The ID of the warn you want to remove.")] int WarningId)
    {
        await DeferAsync(true);
            
        using (BotDatabase botDatabase = new BotDatabase())
        {
            UserWarning? specificWarning = await botDatabase.UserWarnings.SingleOrDefaultAsync(x => x.Id == WarningId && x.GuildId == Context.Guild.Id);

            if (specificWarning == default(UserWarning))
                return ExecutionResult.FromError("There are no warnings with the specified ID.");

            botDatabase.UserWarnings.Remove(specificWarning);

            await botDatabase.SaveChangesAsync();

            await FollowupAsync($":white_check_mark: Removed warning \"{WarningId}\" from user <@{specificWarning.UserId}>.",
                allowedMentions: BotGlobals.Instance.AllowOnlyUsers);
        }

        return ExecutionResult.Succesful();
    }

    [SlashCommand("warns", "Lists all of the warns given to a user.")]
    [DetailedDescription("Replies with a list of warnings given to the specified user.")]
    [RateLimit(2, 1)]
    [RequireContext(ContextType.Guild)]
    public async Task<RuntimeResult> ListWarnsAsync([Summary(description: "The user you want to list the warns for.")] SocketGuildUser TargetUser)
    {
        await DeferAsync();
            
        using (BotDatabase botDatabase = new BotDatabase())
        {
            List<UserWarning> filteredWarnings = botDatabase.UserWarnings.Where(x => x.UserId == TargetUser.Id && x.GuildId == Context.Guild.Id).ToList();

            if (!filteredWarnings.Any())
                return ExecutionResult.FromError("This user has no warnings.");

            EmbedBuilder replyEmbed = new EmbedBuilder().BuildDefaultEmbed(Context);

            replyEmbed.Title = "📃 List of Warnings";
            replyEmbed.Description = "Reasons longer than 48 characters will be truncated.\n\n";

            foreach (UserWarning warning in filteredWarnings)
            {
                replyEmbed.Description += $"⚠️ **ID**: `{warning.Id}`\n";
                replyEmbed.Description += $"**· Creation Date**: <t:{warning.Date}:F>\n";
                replyEmbed.Description += $"**· Reason**: {warning.Reason.Truncate(48)}\n\n";
            }

            await FollowupAsync(embed: replyEmbed.Build(), allowedMentions: BotGlobals.Instance.AllowOnlyUsers);
        }

        return ExecutionResult.Succesful();
    }

    [SlashCommand("viewwarn", "Lists a specific warn.")]
    [DetailedDescription("Lists a warning, and the full reason.")]
    [RateLimit(2, 1)]
    [RequireContext(ContextType.Guild)]
    public async Task<RuntimeResult> ListWarnAsync([Summary(description: "The ID of the warn you want to view.")] int WarningId)
    {
        await DeferAsync();
            
        using (BotDatabase botDatabase = new BotDatabase())
        {
            UserWarning? specificWarning = await botDatabase.UserWarnings.SingleOrDefaultAsync(x => x.Id == WarningId && x.GuildId == Context.Guild.Id);

            if (specificWarning == default(UserWarning))
                return ExecutionResult.FromError("There are no warnings with the specified ID.");

            EmbedBuilder replyEmbed = new EmbedBuilder().BuildDefaultEmbed(Context);

            replyEmbed.Title = "Warning Details";
            replyEmbed.Description = $"Details for the warning \"{specificWarning.Id}\".\n";

            replyEmbed.AddField("User", $"<@{specificWarning.UserId}> (ID: {specificWarning.UserId})");
            replyEmbed.AddField("Date", $"<t:{specificWarning.Date}:F>");
            replyEmbed.AddField("Reason", specificWarning.Reason);

            await FollowupAsync(embed: replyEmbed.Build(), allowedMentions: BotGlobals.Instance.AllowOnlyUsers);
        }

        return ExecutionResult.Succesful();
    }

    [SlashCommand("mute", "Mutes a user for an amount of time with a reason.")]
    [DetailedDescription("Mutes the specified user for an amount of time with the specified reason. The reason is optional.")]
    [RateLimit(1, 2)]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task<RuntimeResult> MuteUserAsync([Summary(description: "The user you want to mute.")] SocketGuildUser TargetUser,
        [Summary(description: "The duration of the mute.")] TimeSpan Duration,
        [Summary(description: "The reason of the mute.")] string? Reason = null)
    {
        string muteReason = Reason ?? "No reason specified.";

        if (Duration < TimeSpan.Zero)
            return ExecutionResult.FromError("Mute duration must not be negative.");

        await TargetUser.SetTimeOutAsync(Duration, new RequestOptions() { AuditLogReason = muteReason });

        string days = Format.Bold(Duration.ToString("%d"));
        string hours = Format.Bold(Duration.ToString("%h"));
        string minutes = Format.Bold(Duration.ToString("%m"));
        string seconds = Format.Bold(Duration.ToString("%s"));

        long untilDate = (DateTimeOffset.Now + Duration).ToUnixTimeSeconds();

        EmbedBuilder replyEmbed = new EmbedBuilder().BuildSuccessEmbed(Context)
                                                    .WithDescription($"Successfully timed out user `{TargetUser.GetFullUsername()}`.");

        replyEmbed.AddField("\U0001f914 Reason", muteReason);
        replyEmbed.AddField("\u23F1\uFE0F Duration", $"{days} day(s), {hours} hour(s), {minutes} minute(s) and {seconds} second(s).");
        replyEmbed.AddField("\u23F0 Expires In", $"<t:{untilDate}:F>");

        await RespondAsync(embed: replyEmbed.Build(), allowedMentions: BotGlobals.Instance.AllowOnlyUsers);

        return ExecutionResult.Succesful();
    }

    [SlashCommand("purge", "Deletes an amount of messages.")]
    [DetailedDescription("Deletes the provided amount of messages.")]
    [RateLimit(2, 2)]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task<RuntimeResult> PurgeMessagesAsync([Summary(description: "The amount of messages you want to purge.")] int Count)
    {
        IEnumerable<IMessage> retrievedMessages = await Context.Interaction.Channel.GetMessagesAsync(Count + 1).FlattenAsync();

        await (Context.Channel as SocketTextChannel)!.DeleteMessagesAsync(retrievedMessages);

        await RespondAsync($":white_check_mark: Cleared `{Count}` message/s.", ephemeral: true, allowedMentions: BotGlobals.Instance.AllowOnlyUsers);

        return ExecutionResult.Succesful();
    }
}