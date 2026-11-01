using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

internal static class Levitation
{
	internal enum Pose
	{
		Ordinaire,
		Levite,
		Eveil
	}

	private sealed class Etat
	{
		public long Debut;

		public long Derniere;

		public long FlotteDepuis;

		public float HauteurAuDebut;

		public Pose Pose = Pose.Levite;
	}

	internal const int Ombre = 5;

	internal const float Amplitude = 3f;

	internal const int PeriodeMs = 3200;

	internal const int EveilMs = 400;

	internal const int ResteDeboutMs = 1200;

	private static readonly Dictionary<Guid, Etat> Etats = new Dictionary<Guid, Etat>();

	internal static Pose Decider(bool enDiscussion, ref long debut, ref long derniere, long maintenant, bool aUnEveil)
	{
		if (enDiscussion)
		{
			if (debut == 0L)
			{
				debut = maintenant;
			}
			derniere = maintenant;
		}
		if (debut == 0L)
		{
			return Pose.Levite;
		}
		long num = maintenant - derniere;
		if (enDiscussion || num < 1200)
		{
			if (!aUnEveil || maintenant - debut >= 400)
			{
				return Pose.Ordinaire;
			}
			return Pose.Eveil;
		}
		if (aUnEveil && num < 1600)
		{
			return Pose.Eveil;
		}
		debut = 0L;
		derniere = 0L;
		return Pose.Levite;
	}

	internal static float Phase(long maintenant, long flotteDepuis)
	{
		return (float)((maintenant - flotteDepuis) % 3200) / 3200f;
	}

	internal static float HauteurDuVaEtVient(float phase)
	{
		return 3f * (1f - MathF.Cos(phase * ((float)Math.PI * 2f))) / 2f;
	}

	public static void Tenir(Event evenement, long maintenant)
	{
		if (evenement.Passable)
		{
			Etats.Remove(evenement.Id);
		}
		else
		{
			if (!evenement.AnimatedTextures.TryGetValue(SpriteAnimations.Levite, out IGameTexture value) || value == null)
			{
				return;
			}
			if (!Etats.TryGetValue(evenement.Id, out Etat value2))
			{
				value2 = (Etats[evenement.Id] = new Etat());
			}
			bool enDiscussion = false;
			foreach (Dialog eventDialog in Globals.EventDialogs)
			{
				if (eventDialog.EventId == evenement.Id)
				{
					enDiscussion = true;
					break;
				}
			}
			bool flag = value2.Debut != 0;
			Pose pose = Decider(enDiscussion, ref value2.Debut, ref value2.Derniere, maintenant, evenement.AnimatedTextures.ContainsKey(SpriteAnimations.Eveil));
			if (!flag && value2.Debut != 0L)
			{
				value2.HauteurAuDebut = HauteurDuVaEtVient(Phase(maintenant, value2.FlotteDepuis));
			}
			if (pose == Pose.Levite && value2.Pose != Pose.Levite)
			{
				value2.FlotteDepuis = maintenant;
			}
			value2.Pose = pose;
			switch (pose)
			{
			case Pose.Levite:
				evenement.SpriteAnimation = SpriteAnimations.Levite;
				evenement.SpriteFrame = ((!(Phase(maintenant, value2.FlotteDepuis) < 0.5f) && evenement.SpriteFrames >= 2) ? 1 : 0);
				return;
			case Pose.Eveil:
				evenement.SpriteAnimation = SpriteAnimations.Eveil;
				evenement.SpriteFrame = 0;
				return;
			}
			if (evenement.SpriteAnimation == SpriteAnimations.Afk && evenement.AnimatedTextures.ContainsKey(SpriteAnimations.Idle))
			{
				evenement.SpriteAnimation = SpriteAnimations.Idle;
			}
		}
	}

	public static bool Couper(Entity entite, ref FloatRect source, out FloatRect ombre, out float hauteur)
	{
		ombre = default(FloatRect);
		hauteur = 0f;
		SpriteAnimations spriteAnimation = entite.SpriteAnimation;
		bool flag = (uint)(spriteAnimation - 12) <= 1u;
		if (!flag || !Etats.TryGetValue(entite.Id, out Etat value) || source.Height <= 5f)
		{
			return false;
		}
		ombre = new FloatRect(source.X, source.Y + source.Height - 5f, source.Width, 5f);
		source = new FloatRect(source.X, source.Y, source.Width, source.Height - 5f);
		long milliseconds = Timing.Global.Milliseconds;
		float num;
		if (value.Pose == Pose.Levite)
		{
			num = HauteurDuVaEtVient(Phase(milliseconds, value.FlotteDepuis));
		}
		else if (value.Debut != 0L && milliseconds - value.Debut < 400)
		{
			float num2 = 1f - (float)(milliseconds - value.Debut) / 400f;
			num = value.HauteurAuDebut * num2 * num2;
		}
		else
		{
			num = 0f;
		}
		float num3 = MathF.Max(1f, Graphics.ZoomDeLaVue);
		hauteur = 5f + MathF.Round(num * num3) / num3;
		return true;
	}

	public static void DessinerLOmbre(IGameTexture planche, FloatRect ombre, FloatRect corps, Color couleur)
	{
		Graphics.DrawGameTexture(planche, ombre, new FloatRect(corps.X, corps.Y + corps.Height - ombre.Height, ombre.Width, ombre.Height), couleur);
	}
}
