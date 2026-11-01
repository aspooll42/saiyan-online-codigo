using System;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class HeritageDecorVuOptions
{
	public int ArretArbreMs { get; set; } = 100;

	public int ArretRocherMs { get; set; } = 115;

	public int ArretFalaiseMs { get; set; } = 130;

	public int ArretPetitMs { get; set; } = 60;

	public int SecousseArbreDixiemes { get; set; } = 40;

	public int SecousseRocherDixiemes { get; set; } = 50;

	public int SecousseFalaiseDixiemes { get; set; } = 60;

	public int SecousseMs { get; set; } = 260;

	public int EclatsVieMs { get; set; } = 4500;

	public int CimeMs { get; set; } = 520;

	public int Feuilles { get; set; } = 24;

	public int SonVoix { get; set; } = 3;

	public bool Marques { get; set; } = true;

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Borner();
	}

	internal void Borner()
	{
		ArretArbreMs = Math.Max(0, ArretArbreMs);
		ArretRocherMs = Math.Max(0, ArretRocherMs);
		ArretFalaiseMs = Math.Max(0, ArretFalaiseMs);
		ArretPetitMs = Math.Max(0, ArretPetitMs);
		SecousseArbreDixiemes = Math.Max(0, SecousseArbreDixiemes);
		SecousseRocherDixiemes = Math.Max(0, SecousseRocherDixiemes);
		SecousseFalaiseDixiemes = Math.Max(0, SecousseFalaiseDixiemes);
		SecousseMs = Math.Max(0, SecousseMs);
		EclatsVieMs = Math.Max(0, EclatsVieMs);
		CimeMs = Math.Max(0, CimeMs);
		Feuilles = Math.Max(0, Feuilles);
		SonVoix = Math.Max(0, SonVoix);
	}
}
