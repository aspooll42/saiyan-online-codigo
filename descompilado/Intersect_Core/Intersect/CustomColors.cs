using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect;

public static class CustomColors
{
	public sealed class NamesNamespace
	{
		public LabelColor Events = new LabelColor(Color.White, Color.Black, new Color(180, 0, 0, 0));

		public Dictionary<string, LabelColor> Npcs = new Dictionary<string, LabelColor>
		{
			{
				"Neutral",
				new LabelColor(new Color(255, 100, 230, 100), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Guard",
				new LabelColor(new Color(255, 240, 142, 105), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"AttackWhenAttacked",
				new LabelColor(new Color(255, 255, 255, 255), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"AttackOnSight",
				new LabelColor(new Color(255, 255, 200, 0), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Aggressive",
				new LabelColor(new Color(255, 255, 40, 0), Color.Black, new Color(180, 0, 0, 0))
			}
		};

		public Dictionary<string, LabelColor> Players = new Dictionary<string, LabelColor>
		{
			{
				"Normal",
				new LabelColor(new Color(255, 255, 255, 255), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Party",
				new LabelColor(new Color(255, 70, 192, 219), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Guild",
				new LabelColor(new Color(255, 5, 179, 26), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Hostile",
				new LabelColor(new Color(255, 255, 81, 0), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Moderator",
				new LabelColor(new Color(255, 0, 255, 255), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Admin",
				new LabelColor(new Color(255, 255, 255, 0), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Hero",
				new LabelColor(new Color(255, 245, 190, 90), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Suspect",
				new LabelColor(new Color(255, 242, 209, 75), Color.Black, new Color(180, 0, 0, 0))
			},
			{
				"Criminal",
				new LabelColor(new Color(255, 232, 64, 64), Color.Black, new Color(180, 0, 0, 0))
			}
		};
	}

	public sealed class ChatNamespace
	{
		public Color AdminChat = Color.Cyan;

		public Color AdminGlobalChat = new Color(255, 255, 255, 0);

		public Color AdminLocalChat = new Color(255, 255, 255, 0);

		public Color AnnouncementChat = Color.Yellow;

		public Color ChatBubbleText = Color.Black;

		public Color ChatBubbleTextOutline = Color.Transparent;

		public Color GlobalChat = new Color(255, 220, 220, 220);

		public Color GlobalMsg = new Color(255, 220, 220, 220);

		public Color LocalChat = new Color(255, 240, 240, 240);

		public Color ModGlobalChat = new Color(255, 0, 255, 255);

		public Color ModLocalChat = new Color(255, 0, 255, 255);

		public Color PartyChat = Color.Green;

		public Color PlayerMsg = new Color(255, 220, 220, 220);

		public Color PrivateChatFrom = Color.Magenta;

		public Color PrivateChatTo = new Color(190, 0, 190);

		public Color ProximityMsg = new Color(255, 220, 220, 220);

		public Color GuildChat = new Color(255, 255, 165, 0);

		public Color TradeChat = new Color(255, 110, 220, 160);
	}

	public sealed class QuestAlertNamespace
	{
		public Color Abandoned = Color.Red;

		public Color Completed = Color.Green;

		public Color Declined = Color.Red;

		public Color Started = Color.Cyan;

		public Color TaskUpdated = Color.Cyan;
	}

	public sealed class QuestWindowNamespace
	{
		public Color Completed = Color.Green;

		public Color InProgress = Color.Yellow;

		public Color NotStarted = Color.Red;

		public Color QuestDesc = Color.White;
	}

	public sealed class AlertsNamespace
	{
		public Color Accepted = Color.Green;

		public Color AdminJoined = new Color(255, 255, 255, 0);

		public Color Declined = Color.Red;

		public Color Error = Color.Red;

		public Color Info = Color.White;

		public Color ModJoined = new Color(255, 0, 255, 255);

		public Color RequestSent = Color.Yellow;

		public Color Success = Color.Green;
	}

	public sealed class CombatNamespace
	{
		public Color AddMana = new Color(255, 0, 0, 255);

		public Color Blocked = new Color(255, 0, 0, 255);

		public Color Cleanse = new Color(0, 255, 0, 0);

		public Color Critical = new Color(255, 255, 255, 0);

		public Color Dash = new Color(255, 0, 0, 255);

		public Color Heal = new Color(255, 0, 255, 0);

		public Color Invulnerable = new Color(255, 255, 0, 0);

		public Color LevelUp = Color.Cyan;

		public Color LevelLost = Color.Red;

		public Color MagicDamage = new Color(255, 255, 0, 255);

		public Color Missed = new Color(255, 255, 255, 255);

		public Color NoAmmo = Color.Red;

		public Color NoTarget = Color.Red;

		public Color PhysicalDamage = new Color(255, 255, 0, 0);

		public Color RemoveMana = new Color(255, 255, 127, 80);

		public Color StatPoints = Color.Cyan;

		public Color Status = new Color(255, 255, 255, 0);

		public Color TrueDamage = new Color(255, 255, 255, 255);
	}

	public sealed class ItemsNamespace
	{
		public Color Bound = Color.Red;

		public Color ConsumeExp = Color.White;

		public Color ConsumeHp = new Color(255, 0, 255, 0);

		public Color ConsumeMp = new Color(255, 0, 0, 255);

		public Color ConsumePoison = new Color(255, 255, 0, 0);

		public Dictionary<int, Color> Rarities = new Dictionary<int, Color>
		{
			{
				0,
				Color.White
			},
			{
				1,
				Color.Gray
			},
			{
				2,
				Color.Red
			},
			{
				3,
				Color.Blue
			},
			{
				4,
				Color.Green
			},
			{
				5,
				Color.Yellow
			}
		};

		public Dictionary<int, LabelColor> MapRarities = new Dictionary<int, LabelColor>
		{
			{
				0,
				new LabelColor(Color.White, Color.Black, new Color(100, 0, 0, 0))
			},
			{
				1,
				new LabelColor(Color.Gray, Color.Black, new Color(100, 0, 0, 0))
			},
			{
				2,
				new LabelColor(Color.Red, Color.Black, new Color(100, 0, 0, 0))
			},
			{
				3,
				new LabelColor(Color.Blue, Color.Black, new Color(100, 0, 0, 0))
			},
			{
				4,
				new LabelColor(Color.Gray, Color.Black, new Color(100, 0, 0, 0))
			},
			{
				5,
				new LabelColor(Color.Yellow, Color.Black, new Color(100, 0, 0, 0))
			}
		};
	}

	private sealed class RootNamespace
	{
		public readonly AlertsNamespace Alerts = new AlertsNamespace();

		public readonly ChatNamespace Chat = new ChatNamespace();

		public readonly CombatNamespace Combat = new CombatNamespace();

		public readonly ItemsNamespace Items = new ItemsNamespace();

		public readonly NamesNamespace Names = new NamesNamespace();

		public readonly QuestAlertNamespace QuestAlert = new QuestAlertNamespace();

		public readonly QuestWindowNamespace QuestWindow = new QuestWindowNamespace();
	}

	private static RootNamespace Root { get; set; }

	public static NamesNamespace Names => Root.Names;

	public static ChatNamespace Chat => Root.Chat;

	public static QuestAlertNamespace QuestAlert => Root.QuestAlert;

	public static QuestWindowNamespace QuestWindow => Root.QuestWindow;

	public static AlertsNamespace Alerts => Root.Alerts;

	public static CombatNamespace Combat => Root.Combat;

	public static ItemsNamespace Items => Root.Items;

	public static string Json()
	{
		return JsonConvert.SerializeObject(Root, Formatting.None, new ColorConverter());
	}

	public static void Load(string json)
	{
		Root = JsonConvert.DeserializeObject<RootNamespace>(json, new JsonConverter[1]
		{
			new ColorConverter()
		}) ?? Root;
	}

	public static bool Load()
	{
		Console.WriteLine("Loading custom colors...");
		string text = Path.Combine(Options.ResourcesDirectory, "colors.json");
		if (File.Exists(text))
		{
			string value = File.ReadAllText(text, Encoding.UTF8);
			try
			{
				Root = JsonConvert.DeserializeObject<RootNamespace>(value, new JsonConverter[1]
				{
					new ColorConverter()
				}) ?? Root;
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to deserialize '{Path}'", text);
				return false;
			}
		}
		return Save();
	}

	public static bool Save()
	{
		Console.WriteLine("Saving custom colors...");
		string text = Path.Combine(Options.ResourcesDirectory, "colors.json");
		try
		{
			Directory.CreateDirectory(Options.ResourcesDirectory);
			string contents = JsonConvert.SerializeObject(Root, Formatting.Indented, new ColorConverter());
			File.WriteAllText(text, contents, Encoding.UTF8);
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to save to '{Path}'", text);
			return false;
		}
	}

	static CustomColors()
	{
		Root = new RootNamespace();
	}
}
