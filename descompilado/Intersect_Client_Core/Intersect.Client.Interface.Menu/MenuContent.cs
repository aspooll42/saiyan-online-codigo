using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Localization;
using Intersect.Configuration;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Client.Interface.Menu;

internal sealed class MenuContent
{
	private const string FileName = "menu-principal.json";

	private static MenuContent? _current;

	[JsonProperty("Version")]
	public string Version { get; set; } = "Alpha 0.0.1";

	[JsonProperty("Discord")]
	public string Discord { get; set; } = string.Empty;

	[JsonProperty("Classes")]
	public List<MenuClassInfo> Classes { get; set; } = new List<MenuClassInfo>();

	[JsonProperty("Nouveautes")]
	public List<MenuNewsInfo> News { get; set; } = new List<MenuNewsInfo>();

	[JsonProperty("Astuces")]
	public List<string> Tips { get; set; } = new List<string>();

	public static MenuContent Current => _current ?? (_current = Translate(Load()));

	private static MenuContent Translate(MenuContent content)
	{
		foreach (MenuClassInfo @class in content.Classes)
		{
			@class.Name = T(@class.Name);
			@class.Title = T(@class.Title);
			@class.Text = T(@class.Text);
			@class.Technique = T(@class.Technique);
			@class.Passive = T(@class.Passive);
			foreach (MenuColorInfo color in @class.Colors)
			{
				color.Name = T(color.Name);
			}
		}
		foreach (MenuNewsInfo item in content.News)
		{
			item.Title = T(item.Title);
			item.Text = T(item.Text);
		}
		content.Tips = content.Tips.Select(T).ToList();
		return content;
		static string T(string text)
		{
			return Traduction.Contenu(text);
		}
	}

	private static MenuContent Load()
	{
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "menu-principal.json");
		try
		{
			if (Coffre.Existe(text))
			{
				MenuContent menuContent = JsonConvert.DeserializeObject<MenuContent>(Coffre.LireTexte(text));
				if (menuContent != null)
				{
					if (menuContent.Classes.Count == 0)
					{
						menuContent.Classes = Defaults().Classes;
					}
					List<MenuClassInfo> classes = Defaults().Classes;
					foreach (MenuClassInfo info in menuContent.Classes)
					{
						MenuClassInfo menuClassInfo = classes.FirstOrDefault((MenuClassInfo candidate) => MenuClassInfo.SameName(candidate.Name, info.Name));
						if (menuClassInfo != null)
						{
							if (string.IsNullOrWhiteSpace(info.Passive))
							{
								info.Passive = menuClassInfo.Passive;
							}
							if (info.Colors.Count == 0)
							{
								info.Colors = menuClassInfo.Colors;
							}
						}
					}
					return menuContent;
				}
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Could not read {File}, using the menu defaults", "menu-principal.json");
			return Defaults();
		}
		MenuContent menuContent2 = Defaults();
		try
		{
			File.WriteAllText(text, JsonConvert.SerializeObject(menuContent2, Formatting.Indented));
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception2, "Could not write {File}", "menu-principal.json");
		}
		return menuContent2;
	}

	private static List<MenuColorInfo> Colors(params (string Name, string Cloth, string Second)[] colors)
	{
		return colors.Select(delegate((string Name, string Cloth, string Second) color)
		{
			MenuColorInfo menuColorInfo = new MenuColorInfo();
			(menuColorInfo.Name, menuColorInfo.Cloth, menuColorInfo.Second) = color;
			return menuColorInfo;
		}).ToList();
	}

	private static List<MenuColorInfo> GokuColors()
	{
		return Colors(("Orange · bleu", "#E8670E", "#3110AD"), ("Noir · rouge", "#3E3E4A", "#A81818"), ("Blanc · bleu", "#E0E6EE", "#203890"), ("Bleu · noir", "#2E50A8", "#34343E"));
	}

	private static MenuContent Defaults()
	{
		MenuContent menuContent = new MenuContent();
		int num = 8;
		List<MenuClassInfo> list = new List<MenuClassInfo>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<MenuClassInfo> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = new MenuClassInfo
		{
			Name = "Goku",
			ClassName = "Goku",
			Sprite = "Goku.png",
			Title = "Le cœur pur",
			Text = "Il ne vit que pour le prochain adversaire à sa mesure, et se relève toujours plus fort.",
			Technique = "Kamehameha",
			Passive = "Il s’échauffe en combattant : +1 % de dégâts toutes les 4 secondes, jusqu’à +6 %. Tout retombe une fois le combat fini.",
			Colors = GokuColors()
		};
		num2++;
		span[num2] = new MenuClassInfo
		{
			Name = "Vegeta",
			ClassName = "Végéta|Vegeta",
			Title = "L’orgueil saiyan",
			Text = "Prince d’un peuple disparu. Chaque défaite le rend plus dangereux.",
			Technique = "Final Flash",
			Passive = "Trop fier pour se protéger : +8 % de dégâts, mais –8 % de PV max.",
			Colors = Colors(("Bleu · blanc", "#1C1CB0", "#EEEEEE"), ("Noir · blanc", "#34343C", "#EEEEEE"), ("Blanc · bleu", "#DCE0E6", "#3652A8"), ("Rouge · blanc", "#C0302A", "#EEEEEE"))
		};
		num2++;
		span[num2] = new MenuClassInfo
		{
			Name = "Gohan petit",
			ClassName = "Gohan petit|Gohan",
			Sprite = "gohan_petit.png",
			Title = "La colère endormie",
			Text = "Un enfant timide. Quand sa rage éclate, aucun guerrier ne peut le suivre.",
			Technique = "Masenko",
			Passive = "Chaque coup donné ou reçu fait monter sa colère. Au 30e, elle éclate : +15 % de dégâts pendant 6 secondes, puis elle repart à zéro.",
			Colors = Colors(("Violet · rouge", "#3010A8", "#D01028"), ("Noir · rouge", "#3E3E4A", "#D01028"), ("Blanc · bleu", "#E0E6EE", "#3050C0"), ("Orange · bleu", "#F07000", "#3050C0"))
		};
		num2++;
		span[num2] = new MenuClassInfo
		{
			Name = "Gohan ado",
			ClassName = "Gohan ado",
			Title = "L’héritier",
			Text = "Formé par Piccolo et par son père, il porte le plus grand potentiel de sa lignée.",
			Technique = "Kamehameha père-fils",
			Passive = "Les poings de Piccolo, le ki de son père : après un coup au corps à corps, sa prochaine technique de ki coûte 20 % de ki en moins.",
			Colors = Colors(("Pantalon brun", "#EFEFEF", "#733900"), ("Pantalon noir", "#EFEFEF", "#34343E"), ("Pantalon gris", "#EFEFEF", "#A8AFBA"), ("Pantalon bleu", "#EFEFEF", "#2E50A8"))
		};
		num2++;
		span[num2] = new MenuClassInfo
		{
			Name = "Trunks petit",
			ClassName = "Trunks petit",
			Title = "L’insolent",
			Text = "Fils de Vegeta et de Bulma, il se bat comme il joue : sans jamais douter.",
			Technique = "Buster Cannon",
			Passive = "Il se bat comme il joue : son dash se recharge 25 % plus vite, et son premier coup après un dash fait +10 % de dégâts.",
			Colors = Colors(("Gris · orange", "#4A4A4A", "#D66300"), ("Noir · rouge", "#2A2A32", "#D02828"), ("Blanc · bleu", "#D0D6DE", "#3050C0"), ("Bleu · orange", "#2E50A8", "#D66300"))
		};
		num2++;
		span[num2] = new MenuClassInfo
		{
			Name = "Trunks du futur",
			ClassName = "Trunks du futur",
			Sprite = "TrunksFutur.png",
			Title = "L’épée du futur",
			Text = "Il a traversé le temps pour sauver un monde qu’il a déjà vu mourir.",
			Technique = "Burning Attack",
			Passive = "Avec son épée, ses coups portent un quart de pas plus loin.",
			Colors = Colors(("Bleu · gris", "#2A10A0", "#737373"), ("Noir · gris", "#3E3E4A", "#737373"), ("Kaki · gris", "#7A8446", "#737373"), ("Cuir · gris", "#82522C", "#737373"))
		};
		num2++;
		span[num2] = new MenuClassInfo
		{
			Name = "Goten",
			ClassName = "Goten",
			Title = "Le cadet",
			Text = "Le portrait de son père, en plus jeune et plus espiègle.",
			Technique = "Chou Kamehameha",
			Passive = "Espiègle : son premier coup sur chaque nouvel ennemi fait +15 % de dégâts.",
			Colors = GokuColors()
		};
		num2++;
		span[num2] = new MenuClassInfo
		{
			Name = "Broly",
			ClassName = string.Empty,
			Title = "Le Saiyan légendaire",
			Text = "Une puissance sans limite, qu’il contrôle à peine.",
			Technique = "Eraser Cannon",
			Aura = "verte"
		};
		num2++;
		menuContent.Classes = list;
		num2 = 3;
		List<MenuNewsInfo> list2 = new List<MenuNewsInfo>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		Span<MenuNewsInfo> span2 = CollectionsMarshal.AsSpan(list2);
		num = 0;
		span2[num] = new MenuNewsInfo
		{
			Icon = "poisson_paozu.png",
			Title = "La pêche au lancer",
			Text = "Lance ta ligne jusqu’à trois pas devant toi. Quand le bouchon plonge, ferre !"
		};
		num++;
		span2[num] = new MenuNewsInfo
		{
			Icon = "pierre.png",
			Title = "Le minage au poing",
			Text = "Fissures, éclats, éboulis : chaque coup se sent, jusqu’au rocher qui cède."
		};
		num++;
		span2[num] = new MenuNewsInfo
		{
			Icon = "meteo",
			Title = "Météo et jour-nuit",
			Text = "Pluie, neige et crépuscule selon la zone. Une journée dure deux heures."
		};
		num++;
		menuContent.News = list2;
		num = 3;
		List<string> list3 = new List<string>(num);
		CollectionsMarshal.SetCount(list3, num);
		Span<string> span3 = CollectionsMarshal.AsSpan(list3);
		num2 = 0;
		span3[num2] = "Astuce : maintiens V pour voir la carte des environs.";
		num2++;
		span3[num2] = "Astuce : un coup « Précis ! » fait avancer ta récolte deux fois plus vite.";
		num2++;
		span3[num2] = "Astuce : tiens Maj pour courir.";
		num2++;
		menuContent.Tips = list3;
		return menuContent;
	}
}
