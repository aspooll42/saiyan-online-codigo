using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Framework.Gwen.Anim;

public class Animation
{
	private static readonly Dictionary<Base, List<Animation>> Animations = new Dictionary<Base, List<Animation>>();

	protected Base mControl;

	public virtual bool Finished
	{
		get
		{
			throw new InvalidOperationException("Pure virtual function call");
		}
	}

	protected virtual void Think()
	{
	}

	public static void Add(Base control, Animation animation)
	{
		animation.mControl = control;
		if (!Animations.ContainsKey(control))
		{
			Animations[control] = new List<Animation>();
		}
		Animations[control].Add(animation);
	}

	public static void Cancel(Base control)
	{
		if (Animations.ContainsKey(control))
		{
			Animations[control].Clear();
			Animations.Remove(control);
		}
	}

	internal static void GlobalThink()
	{
		foreach (KeyValuePair<Base, List<Animation>> animation in Animations)
		{
			List<Animation> list = animation.Value.FindAll((Animation x) => true);
			foreach (Animation item in list)
			{
				item.Think();
				if (item.Finished)
				{
					animation.Value.Remove(item);
				}
			}
		}
	}
}
