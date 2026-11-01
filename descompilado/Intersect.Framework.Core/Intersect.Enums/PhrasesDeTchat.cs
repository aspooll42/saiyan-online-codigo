using System;
using System.Collections.Generic;

namespace Intersect.Enums;

public static class PhrasesDeTchat
{
	public const int NomMax = 64;

	public static readonly IReadOnlyList<PhraseDeTchat> Toutes = new global::_003C_003Ez__ReadOnlyArray<PhraseDeTchat>(new PhraseDeTchat[14]
	{
		new PhraseDeTchat("g1", FamilleDePhrase.Groupe, TrouDePhrase.Lieu),
		new PhraseDeTchat("g2", FamilleDePhrase.Groupe, TrouDePhrase.Aucun),
		new PhraseDeTchat("g3", FamilleDePhrase.Groupe, TrouDePhrase.Aucun),
		new PhraseDeTchat("g4", FamilleDePhrase.Groupe, TrouDePhrase.Aucun),
		new PhraseDeTchat("a1", FamilleDePhrase.Aide, TrouDePhrase.Aucun),
		new PhraseDeTchat("a2", FamilleDePhrase.Aide, TrouDePhrase.Pnj),
		new PhraseDeTchat("a3", FamilleDePhrase.Aide, TrouDePhrase.Aucun),
		new PhraseDeTchat("c1", FamilleDePhrase.Commerce, TrouDePhrase.Objet),
		new PhraseDeTchat("c2", FamilleDePhrase.Commerce, TrouDePhrase.Objet),
		new PhraseDeTchat("c3", FamilleDePhrase.Commerce, TrouDePhrase.Aucun),
		new PhraseDeTchat("p1", FamilleDePhrase.Politesse, TrouDePhrase.Aucun),
		new PhraseDeTchat("p2", FamilleDePhrase.Politesse, TrouDePhrase.Aucun),
		new PhraseDeTchat("p3", FamilleDePhrase.Politesse, TrouDePhrase.Aucun),
		new PhraseDeTchat("p4", FamilleDePhrase.Politesse, TrouDePhrase.Aucun)
	});

	public static PhraseDeTchat? Trouver(string? id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		foreach (PhraseDeTchat toute in Toutes)
		{
			if (string.Equals(toute.Id, id, StringComparison.Ordinal))
			{
				return toute;
			}
		}
		return null;
	}
}
