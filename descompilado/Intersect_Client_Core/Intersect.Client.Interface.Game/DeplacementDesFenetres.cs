using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Interface.Shared;

namespace Intersect.Client.Interface.Game;

internal static class DeplacementDesFenetres
{
	private const int HauteurEnTete = 50;

	private static WindowControl? _fenetre;

	private static Point _prise;

	private static bool _deplacee;

	private static bool _tenu;

	private static readonly Dictionary<WindowControl, Point> Origines = new Dictionary<WindowControl, Point>();

	public static bool EnCours => _fenetre != null;

	public static void Update()
	{
		bool isLeftMouseDown = InputHandler.IsLeftMouseDown;
		if (isLeftMouseDown && !_tenu)
		{
			Prendre();
		}
		else if (!isLeftMouseDown)
		{
			_fenetre = null;
		}
		if (isLeftMouseDown && _fenetre != null)
		{
			Suivre();
		}
		_tenu = isLeftMouseDown;
		RendreLesPlaces();
	}

	private static void Prendre()
	{
		_fenetre = null;
		_deplacee = false;
		Base hoveredControl = InputHandler.HoveredControl;
		if (hoveredControl == null)
		{
			return;
		}
		WindowControl windowControl = FenetreDe(hoveredControl);
		if (windowControl == null || !Deplacable(windowControl))
		{
			return;
		}
		if (Dans(hoveredControl, windowControl.Titlebar))
		{
			Origines.TryAdd(windowControl, new Point(windowControl.X, windowControl.Y));
			return;
		}
		Point prise = windowControl.CanvasPosToLocal(InputHandler.MousePosition);
		if (prise.Y >= 0 && prise.Y < 50 && Passif(hoveredControl, windowControl))
		{
			_fenetre = windowControl;
			_prise = prise;
		}
	}

	private static void Suivre()
	{
		WindowControl fenetre = _fenetre;
		if (fenetre != null && !fenetre.IsHidden)
		{
			Base parent = fenetre.Parent;
			if (parent != null)
			{
				Point point = parent.CanvasPosToLocal(InputHandler.MousePosition);
				int num = Math.Clamp(point.X - _prise.X, 0, Math.Max(0, parent.Width - fenetre.Width));
				int num2 = Math.Clamp(point.Y - _prise.Y, 0, Math.Max(0, parent.Height - fenetre.Height));
				if (_deplacee || Math.Abs(num - fenetre.X) + Math.Abs(num2 - fenetre.Y) >= 4)
				{
					if (!_deplacee)
					{
						_deplacee = true;
						Origines.TryAdd(fenetre, new Point(fenetre.X, fenetre.Y));
					}
					if (num != fenetre.X || num2 != fenetre.Y)
					{
						fenetre.SetPosition(num, num2);
					}
				}
				return;
			}
		}
		_fenetre = null;
	}

	private static void RendreLesPlaces()
	{
		if (Origines.Count == 0)
		{
			return;
		}
		List<WindowControl> list = null;
		foreach (var (windowControl2, point2) in Origines)
		{
			if (windowControl2 != _fenetre && (windowControl2.IsHidden || windowControl2.Parent == null))
			{
				if (windowControl2.Parent != null)
				{
					windowControl2.SetPosition(point2.X, point2.Y);
				}
				(list ?? (list = new List<WindowControl>())).Add(windowControl2);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (WindowControl item in list)
		{
			Origines.Remove(item);
		}
	}

	private static WindowControl? FenetreDe(Base? controle)
	{
		for (Base obj = controle; obj != null; obj = obj.Parent)
		{
			if (obj is WindowControl result)
			{
				return result;
			}
		}
		return null;
	}

	private static bool Deplacable(WindowControl fenetre)
	{
		bool flag = !fenetre.IsHidden && fenetre.Titlebar.MouseInputEnabled;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = ((fenetre is ConfirmCardWindow || fenetre is RequestCardWindow || fenetre is WorldMapWindow) ? true : false);
			flag2 = !flag3;
		}
		return flag2;
	}

	private static bool Dans(Base controle, Base ancetre)
	{
		for (Base obj = controle; obj != null; obj = obj.Parent)
		{
			if (obj == ancetre)
			{
				return true;
			}
		}
		return false;
	}

	private static bool Passif(Base cible, WindowControl fenetre)
	{
		Base obj = cible;
		while (obj != null && obj != fenetre)
		{
			if ((obj is Button || obj is TextBox || obj is ScrollBar || obj is Slider || obj is LabeledCheckBox || obj is ListBox || obj is ListBoxRow || obj is TreeNode || obj is Dragger) ? true : false)
			{
				return false;
			}
			obj = obj.Parent;
		}
		return true;
	}
}
