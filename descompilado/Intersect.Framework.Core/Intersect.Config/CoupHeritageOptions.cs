using System;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class CoupHeritageOptions
{
	public int NiveauRequis { get; set; } = 1;

	public int DureePourcent { get; set; } = 100;

	public int PlancherMs { get; set; } = 280;

	public int ImpactMs { get; set; }

	public int TenueMs { get; set; }

	public int ArmementTenuMs { get; set; }

	public int SansMarcheMs { get; set; }

	public int DegatsPourcent { get; set; } = 100;

	public int KiPourcent { get; set; }

	public int ReculPx { get; set; }

	public int ReculMs { get; set; }

	public int ReculCotePx { get; set; }

	public int ReculLourdPx { get; set; }

	public bool PousseePredite { get; set; }

	public int ElanPx { get; set; }

	public int ElanMs { get; set; }

	public int EcartPx { get; set; }

	public int AllongePx { get; set; }

	public int ArretMs { get; set; }

	public int SecousseDixiemes { get; set; }

	public int SecousseMs { get; set; }

	public int SonCentiemes { get; set; }

	public bool Souffle { get; set; }

	public int SouffleCentiemes { get; set; }

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Borner();
	}

	internal void Borner()
	{
		NiveauRequis = Math.Max(1, NiveauRequis);
		DureePourcent = Math.Max(10, DureePourcent);
		PlancherMs = Math.Max(150, PlancherMs);
		ImpactMs = Math.Clamp(ImpactMs, 0, PlancherMs - 120);
		TenueMs = Math.Max(0, TenueMs);
		ArmementTenuMs = Math.Max(0, ArmementTenuMs);
		SansMarcheMs = Math.Clamp(SansMarcheMs, 0, 300);
		DegatsPourcent = Math.Max(0, DegatsPourcent);
		KiPourcent = Math.Max(0, KiPourcent);
		ReculPx = Math.Clamp(ReculPx, 0, 64);
		ReculMs = Math.Max(0, ReculMs);
		ReculCotePx = Math.Clamp(ReculCotePx, 0, 64);
		ReculLourdPx = Math.Clamp(ReculLourdPx, 0, 64);
		ElanPx = Math.Clamp(ElanPx, 0, 64);
		ElanMs = Math.Max(0, ElanMs);
		EcartPx = Math.Clamp(EcartPx, 0, 64);
		AllongePx = Math.Clamp(AllongePx, 0, 32);
		ArretMs = Math.Max(0, ArretMs);
		SecousseDixiemes = Math.Max(0, SecousseDixiemes);
		SecousseMs = Math.Max(0, SecousseMs);
	}
}
