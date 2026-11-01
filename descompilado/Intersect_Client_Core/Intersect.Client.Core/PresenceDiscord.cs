using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Database;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Core;
using Intersect.Framework.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Core;

public static class PresenceDiscord
{
	private const string Application = "1564100464293974172";

	private const string Images = "https://maj.saiyanonline.com/discord/";

	private const string LienDiscord = "https://discord.gg/PapMWhqZWH";

	private const string LienSite = "https://saiyanonline.com";

	private const int EcartEntreEnvois = 5000;

	private const int EcartEntreConnexions = 20000;

	private static readonly Regex Corps = new Regex("^saiyan-[a-z]+-[a-z]+-[a-z]+(?<forme>_(ssj1|ssj2|ssg|ui))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	private static readonly object Verrou = new object();

	private static Thread? _fil;

	private static string _voulu = string.Empty;

	private static long _prochainReleve;

	private static long _debut;

	public static void Suivre()
	{
		long tickCount = Environment.TickCount64;
		if (tickCount < _prochainReleve || !OperatingSystem.IsWindows())
		{
			return;
		}
		_prochainReleve = tickCount + 2000;
		try
		{
			GameDatabase? database = Globals.Database;
			string text = ((database == null || database.PresenceDiscord) ? Activite().ToString(Formatting.None) : string.Empty);
			lock (Verrou)
			{
				_voulu = text;
				if (_fil == null && text.Length > 0)
				{
					_fil = new Thread(Boucle)
					{
						IsBackground = true,
						Name = "Presence Discord"
					};
					_fil.Start();
				}
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Presence Discord : etat du joueur illisible");
		}
	}

	private static JObject Activite()
	{
		if (_debut == 0L)
		{
			_debut = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		}
		JObject jObject = new JObject
		{
			["large_image"] = "https://maj.saiyanonline.com/discord/logo.png",
			["large_text"] = "Saiyan Online"
		};
		JObject jObject2 = new JObject();
		jObject2["timestamps"] = new JObject { ["start"] = _debut };
		jObject2["assets"] = jObject;
		jObject2["buttons"] = new JArray(new JObject
		{
			["label"] = Borne(Strings.PresenceDiscord.BoutonDiscord, 32),
			["url"] = "https://discord.gg/PapMWhqZWH"
		}, new JObject
		{
			["label"] = Borne(Strings.PresenceDiscord.BoutonSite, 32),
			["url"] = "https://saiyanonline.com"
		});
		JObject jObject3 = jObject2;
		if (Globals.GameState == GameStates.InGame)
		{
			Player me = Globals.Me;
			if (me != null)
			{
				bool flag = me.CombatTimer > Timing.Global.Milliseconds;
				jObject3["details"] = Borne((flag ? Strings.PresenceDiscord.NiveauEnCombat : Strings.PresenceDiscord.Niveau).ToString(me.Level), 128);
				string text = MapInstance.Get(me.MapId)?.Name;
				if (text != null && text.Length >= 2)
				{
					jObject3["state"] = Borne(text, 128);
				}
				string text2 = Path.GetFileNameWithoutExtension(string.IsNullOrWhiteSpace(me.TransformedSprite) ? me.Sprite : Transformation.TextureNameFor(me.Sprite, me.TransformedSprite)) ?? string.Empty;
				Match match = Corps.Match(text2);
				if (match != null && match.Success)
				{
					jObject["small_image"] = "https://maj.saiyanonline.com/discord/tetes/" + text2.ToLowerInvariant() + ".png";
					JObject jObject4 = jObject;
					jObject4["small_text"] = (match.Groups["forme"].Value.ToLowerInvariant() switch
					{
						"_ssj1" => Strings.PresenceDiscord.SuperSaiyan, 
						"_ssj2" => Strings.PresenceDiscord.SuperSaiyan2, 
						"_ssg" => Strings.PresenceDiscord.SuperSaiyanGod, 
						"_ui" => Strings.PresenceDiscord.UltraInstinct, 
						_ => Strings.PresenceDiscord.Saiyan, 
					}).ToString();
				}
				return jObject3;
			}
		}
		jObject3["details"] = Borne(Strings.PresenceDiscord.Menu, 128);
		return jObject3;
	}

	private static string Borne(string texte, int longueur)
	{
		if (texte.Length > longueur)
		{
			return texte.Substring(0, longueur);
		}
		return texte;
	}

	private static void Boucle()
	{
		NamedPipeClientStream tuyau = null;
		string text = string.Empty;
		long num = 0L;
		long num2 = 0L;
		while (true)
		{
			try
			{
				string voulu;
				lock (Verrou)
				{
					voulu = _voulu;
				}
				long tickCount = Environment.TickCount64;
				if (voulu.Length == 0)
				{
					Fermer(ref tuyau);
					text = string.Empty;
				}
				else if (tuyau == null)
				{
					if (tickCount >= num)
					{
						num = tickCount + 20000;
						tuyau = Connecter();
						text = string.Empty;
					}
				}
				else if (voulu != text && tickCount >= num2)
				{
					num2 = tickCount + 5000;
					Envoyer(tuyau, voulu);
					text = voulu;
				}
			}
			catch (Exception ex)
			{
				ApplicationContext.Context.Value?.Logger.LogInformation("Presence Discord : liaison perdue ({Raison})", ex.Message);
				Fermer(ref tuyau);
				text = string.Empty;
			}
			Thread.Sleep(250);
		}
	}

	private static NamedPipeClientStream? Connecter()
	{
		for (int i = 0; i < 10; i++)
		{
			NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "discord-ipc-" + i, PipeDirection.InOut, PipeOptions.Asynchronous);
			try
			{
				namedPipeClientStream.Connect(100);
				Ecrire(namedPipeClientStream, 0, new JObject
				{
					["v"] = 1,
					["client_id"] = "1564100464293974172"
				});
				var (num, jObject) = Lire(namedPipeClientStream);
				if (num == 1 && jObject?["evt"]?.ToString() == "READY")
				{
					ApplicationContext.Context.Value?.Logger.LogInformation("Presence Discord : reliee (discord-ipc-{Numero})", i);
					return namedPipeClientStream;
				}
				ApplicationContext.Context.Value?.Logger.LogWarning("Presence Discord : refusee par Discord ({Reponse})", jObject?.ToString(Formatting.None));
			}
			catch (Exception)
			{
			}
			namedPipeClientStream.Dispose();
		}
		return null;
	}

	private static void Envoyer(Stream tuyau, string activite)
	{
		Ecrire(tuyau, 1, new JObject
		{
			["cmd"] = "SET_ACTIVITY",
			["nonce"] = Guid.NewGuid().ToString(),
			["args"] = new JObject
			{
				["pid"] = Environment.ProcessId,
				["activity"] = JObject.Parse(activite)
			}
		});
		for (int i = 0; i < 4; i++)
		{
			var (num, jObject) = Lire(tuyau);
			switch (num)
			{
			case 3:
				break;
			case 2:
				throw new IOException("Discord ferme la liaison : " + jObject?.ToString(Formatting.None));
			default:
				if (jObject?["evt"]?.ToString() == "ERROR")
				{
					ApplicationContext.Context.Value?.Logger.LogWarning("Presence Discord : activite refusee ({Reponse})", jObject["data"]?.ToString(Formatting.None));
				}
				return;
			}
			Ecrire(tuyau, 4, jObject);
		}
	}

	private static void Ecrire(Stream tuyau, int op, JObject? corps)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(corps?.ToString(Formatting.None) ?? "{}");
		byte[] array = new byte[8 + bytes.Length];
		BitConverter.GetBytes(op).CopyTo(array, 0);
		BitConverter.GetBytes(bytes.Length).CopyTo(array, 4);
		bytes.CopyTo(array, 8);
		tuyau.Write(array);
		tuyau.Flush();
	}

	private static (int Op, JObject? Corps) Lire(Stream tuyau)
	{
		byte[] value = LireExactement(tuyau, 8);
		int item = BitConverter.ToInt32(value, 0);
		int num = BitConverter.ToInt32(value, 4);
		if ((num < 0 || num > 1048576) ? true : false)
		{
			throw new IOException("trame de Discord illisible");
		}
		byte[] bytes = LireExactement(tuyau, num);
		return (Op: item, Corps: (num == 0) ? null : JObject.Parse(Encoding.UTF8.GetString(bytes)));
	}

	private static byte[] LireExactement(Stream tuyau, int longueur)
	{
		byte[] array = new byte[longueur];
		Task<int> task;
		for (int i = 0; i < longueur; i += task.Result)
		{
			task = tuyau.ReadAsync(array, i, longueur - i);
			if (!task.Wait(5000))
			{
				throw new IOException("Discord ne repond pas");
			}
			if (task.Result == 0)
			{
				throw new IOException("Discord a ferme le tuyau");
			}
		}
		return array;
	}

	private static void Fermer(ref NamedPipeClientStream? tuyau)
	{
		try
		{
			tuyau?.Dispose();
		}
		catch
		{
		}
		tuyau = null;
	}
}
