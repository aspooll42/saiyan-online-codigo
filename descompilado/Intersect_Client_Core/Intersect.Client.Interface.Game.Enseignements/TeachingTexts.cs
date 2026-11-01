using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Intersect.Client.Localization;
using Intersect.GameObjects;

namespace Intersect.Client.Interface.Game.Enseignements;

public static class TeachingTexts
{
	public sealed record MasterText(TeachingMaster Master, string Name, string Role, string Mission, Color Color);

	public sealed record NodeText(string Name, string Icon, Func<double, string> Effect);

	public static readonly MasterText[] Masters = new MasterText[4]
	{
		Master(TeachingMaster.Piccolo, new Color(255, 111, 227, 161)),
		Master(TeachingMaster.Tortue, new Color(255, 255, 138, 61)),
		Master(TeachingMaster.Tenshinhan, new Color(255, 111, 211, 255)),
		Master(TeachingMaster.Krillin, new Color(255, 185, 166, 255))
	};

	private static readonly Dictionary<string, NodeText> Nodes = new Dictionary<string, NodeText>
	{
		["provocation"] = Node("provocation", "rempart", (double v) => N(2.0 + v)),
		["peau"] = Node("peau", "bouclier", P),
		["garde"] = Node("garde", "carapace", P),
		["interposition"] = Node("interposition", "groupe", P),
		["riposte"] = Node("riposte", "combo", P),
		["ancrage"] = Node("ancrage", "planete", P),
		["regeneration"] = Node("regeneration", "coeur", P),
		["cape"] = Node("cape", "souffle", P),
		["rempart"] = Node("rempart", "groupe", P),
		["volonte"] = Node("volonte", "poingleve", (double v) => N(300.0 - v)),
		["mur-namek"] = Node("mur-namek", "rempart"),
		["poings"] = Node("poings", "poing", P),
		["enchainement"] = Node("enchainement", "combo", P),
		["plexus"] = Node("plexus", "impact", P),
		["assaut"] = Node("assaut", "fleche", P),
		["grace"] = Node("grace", "fleche", P),
		["oeil"] = Node("oeil", "oeil", P),
		["brise"] = Node("brise", "brise", P),
		["duel"] = Node("duel", "cible", P),
		["second"] = Node("second", "souffle", P),
		["rythme"] = Node("rythme", "chrono", N),
		["style-tortue"] = Node("style-tortue", "onde"),
		["affute"] = Node("affute", "ki", P),
		["concentration"] = Node("concentration", "cible", P),
		["economie"] = Node("economie", "goutte", P),
		["portee"] = Node("portee", "fleche", (double v) => Teachings.RangeBonus(v).ToString(CultureInfo.InvariantCulture)),
		["percant"] = Node("percant", "perce", P),
		["eclat"] = Node("eclat", "impact", P),
		["rafale"] = Node("rafale", "double", P),
		["distance"] = Node("distance", "aile", P),
		["calme"] = Node("calme", "lotus", P),
		["fuite"] = Node("fuite", "flamme", P),
		["tri-faisceau"] = Node("tri-faisceau", "perce"),
		["partage"] = Node("partage", "ki", P),
		["pas"] = Node("pas", "plume", P),
		["repos"] = Node("repos", "goutte", P),
		["deviation"] = Node("deviation", "bouclier", P),
		["encouragement"] = Node("encouragement", "etoile", P),
		["reflexes"] = Node("reflexes", "oeil", P),
		["reserve"] = Node("reserve", "haricot", P),
		["relais"] = Node("relais", "souffle", P),
		["taiyoken"] = Node("taiyoken", "etoile", P),
		["veille"] = Node("veille", "lotus", P),
		["haricot-magique"] = Node("haricot-magique", "haricot")
	};

	private static string Key(TeachingMaster master)
	{
		return master switch
		{
			TeachingMaster.Piccolo => "piccolo", 
			TeachingMaster.Tortue => "tortue", 
			TeachingMaster.Tenshinhan => "tenshinhan", 
			_ => "krillin", 
		};
	}

	private static MasterText Master(TeachingMaster master, Color color)
	{
		return new MasterText(master, Strings.Teachings.MasterNames[Key(master)], Strings.Teachings.MasterRoles[Key(master)], Strings.Teachings.MasterMissions[Key(master)], color);
	}

	public static MasterText Of(TeachingMaster master)
	{
		return Masters.First((MasterText text) => text.Master == master);
	}

	private static string P(double value)
	{
		return Math.Round(value, 1).ToString("0.#", CultureInfo.GetCultureInfo("fr-FR")) + " %";
	}

	private static string N(double value)
	{
		return Math.Round(value, 1).ToString("0.#", CultureInfo.GetCultureInfo("fr-FR"));
	}

	private static NodeText Node(string id, string icone, Func<double, string>? chiffre = null)
	{
		return new NodeText(Strings.Teachings.NodeNames[id], icone, (double v) => (chiffre != null) ? Strings.Teachings.NodeEffects[id].ToString(chiffre(v)) : Strings.Teachings.NodeEffects[id].ToString());
	}

	public static NodeText Of(string id)
	{
		return Nodes[id];
	}

	public static string EffectAt(TeachingNode node, int rank)
	{
		return Nodes[node.Id].Effect((double)rank * node.PerRank);
	}
}
