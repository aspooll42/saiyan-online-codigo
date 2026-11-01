using System;
using System.Runtime.Serialization;

namespace Intersect.Config.Guilds;

public class GuildOptions
{
	public const int DefaultBankSlots = 50;

	public bool AllowGuildMemberPvp { get; set; }

	public bool ShowGuildNameTagsOverMembers { get; set; } = true;

	public int GuildUpdateInterval { get; set; } = 10000;

	public int DeleteStaleGuildsAfterDays { get; set; } = -1;

	public int InitialBankSlots { get; set; } = 50;

	public int CreationCost { get; set; } = 50000;

	public int CreationLevel { get; set; } = 20;

	public GuildRank[] Ranks { get; set; } = new GuildRank[4]
	{
		new GuildRank
		{
			Title = "Master",
			Limit = 1,
			Permissions = new GuildPermissions
			{
				Invite = true,
				Kick = true,
				Demote = true,
				Promote = true,
				BankDeposit = true,
				BankMove = true,
				BankRetrieve = true
			}
		},
		new GuildRank
		{
			Title = "Officer",
			Limit = 10,
			Permissions = new GuildPermissions
			{
				Invite = true,
				Kick = true,
				Demote = true,
				Promote = true,
				BankDeposit = true,
				BankMove = true,
				BankRetrieve = true
			}
		},
		new GuildRank
		{
			Title = "Member",
			Limit = -1,
			Permissions = new GuildPermissions
			{
				Invite = false,
				Kick = false,
				Demote = false,
				Promote = false,
				BankDeposit = true,
				BankMove = false,
				BankRetrieve = false
			}
		},
		new GuildRank
		{
			Title = "Newbie",
			Limit = -1,
			Permissions = new GuildPermissions
			{
				Invite = false,
				Kick = false,
				Demote = false,
				Promote = false,
				BankDeposit = false,
				BankMove = false,
				BankRetrieve = false
			}
		}
	};

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Validate();
	}

	public void Validate()
	{
		if (Ranks.Length < 2)
		{
			throw new Exception("Must have at least 2 guild ranks defined!");
		}
		if (Ranks[0].Limit != 1)
		{
			throw new Exception(Ranks[0].Title + " is considered a guild leader (first rank) so the limit should be 1.");
		}
		Ranks[0].Permissions.Invite = true;
		Ranks[0].Permissions.Kick = true;
	}
}
