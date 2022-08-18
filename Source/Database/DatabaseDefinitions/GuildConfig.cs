﻿using System.ComponentModel.DataAnnotations;

namespace SammBotNET.Database
{
	public enum WarnLimitAction
	{
		Kick,
		Ban,
		None
	}

	public class GuildConfig
	{
		[Key]
		public ulong GuildId { get; set; }
		public int WarningLimit { get; set; }
		public WarnLimitAction WarningLimitAction { get; set; }
	}
}
