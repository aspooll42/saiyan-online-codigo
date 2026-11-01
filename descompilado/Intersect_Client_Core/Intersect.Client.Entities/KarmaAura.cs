using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Config;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class KarmaAura
{
	private const int FlameFrames = 8;

	private const long FlameFrameMs = 85L;

	private const float ReferenceSpriteWidth = 32f;

	private const float FlameFeetY = 32f;

	private static readonly Color HeroSpark = new Color(255, 245, 190, 90);

	private static readonly Color HeroSparkLight = new Color(255, 255, 236, 180);

	private static readonly Color CriminalSpark = new Color(255, 165, 107, 232);

	private static readonly Color CriminalSparkLight = new Color(255, 214, 178, 255);

	public static (Color Main, Color Light) SparkColors(Entity entity, Color main, Color light)
	{
		if (entity is Player { KarmaTier: var karmaTier })
		{
			switch (karmaTier)
			{
			case KarmaTier.Hero:
				return (Main: HeroSpark, Light: HeroSparkLight);
			case KarmaTier.Criminal:
				return (Main: CriminalSpark, Light: CriminalSparkLight);
			}
		}
		return (Main: main, Light: light);
	}

	public static void DrawCriminalFlames(Entity entity, FloatRect bounds)
	{
		if (entity is Player { KarmaTier: KarmaTier.Criminal })
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "criminal_flames.png");
			if (texture != null)
			{
				long milliseconds = Timing.Global.Milliseconds;
				int num = texture.Width / 8;
				int height = texture.Height;
				int num2 = entity.Id.GetHashCode() & 0x7FFF;
				int num3 = (int)((milliseconds / 85 + num2) % 8);
				float num4 = bounds.Width / 32f;
				float num5 = (float)num * num4;
				float h = (float)height * num4;
				float num6 = bounds.X + bounds.Width / 2f;
				float num7 = bounds.Y + bounds.Height;
				Graphics.DrawGameTexture(texture, new FloatRect(num3 * num, 0f, num, height), new FloatRect(num6 - num5 / 2f, num7 - 32f * num4, num5, h), Color.White);
			}
		}
	}
}
