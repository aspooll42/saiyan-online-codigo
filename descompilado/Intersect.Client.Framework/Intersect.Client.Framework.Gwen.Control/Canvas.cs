using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Anim;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Gwen.Control;

public class Canvas : Base
{
	private readonly List<IDisposable> _disposeQueue = new List<IDisposable>();

	private Color mBackgroundColor;

	internal Base? mFirstTab;

	internal Base? mNextTab;

	internal readonly LinkedList<Base> _tabQueue = new LinkedList<Base>();

	private bool mNeedsRedraw;

	private float mScale;

	private List<GameAudioInstance> mUISounds;

	public float Scale
	{
		get
		{
			return mScale;
		}
		set
		{
			if (mScale != value)
			{
				mScale = value;
				if (base.Skin != null && base.Skin.Renderer != null)
				{
					base.Skin.Renderer.Scale = mScale;
				}
				OnScaleChanged();
				Redraw();
			}
		}
	}

	public Color BackgroundColor
	{
		get
		{
			return mBackgroundColor;
		}
		set
		{
			mBackgroundColor = value;
		}
	}

	public bool NeedsRedraw
	{
		get
		{
			return mNeedsRedraw;
		}
		set
		{
			mNeedsRedraw = value;
		}
	}

	public Canvas(Intersect.Client.Framework.Gwen.Skin.Base skin, string? name = null)
		: base(null, name)
	{
		mUISounds = new List<GameAudioInstance>();
		SetBounds(0, 0, 10000, 10000);
		SetSkin(skin);
		Scale = 1f;
		BackgroundColor = Color.White;
		base.ShouldDrawBackground = false;
		base.IsTabable = false;
		base.MouseInputEnabled = false;
		_chronometrerLesEnfants = true;
	}

	protected override void Dispose(bool disposing)
	{
		ProcessDelayedDeletes();
		base.Dispose(disposing);
	}

	public override void Redraw()
	{
		NeedsRedraw = true;
		base.Redraw();
	}

	protected void Initialize()
	{
	}

	public void RenderCanvas(TimeSpan elapsed, TimeSpan total)
	{
		Chrono.Commencer();
		UpdateDataProviders(elapsed, total);
		Chrono.Tour("interface : fournisseurs de donnees");
		DoThink();
		Chrono.Tour("interface : pensee et mise en page");
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = base.Skin.Renderer;
		renderer.Begin();
		renderer.ClipRegion = base.Bounds;
		renderer.Scale = Scale;
		if (base.ShouldDrawBackground)
		{
			renderer.DrawColor = mBackgroundColor;
			renderer.DrawFilledRect(base.RenderBounds);
		}
		DoRender(base.Skin);
		Chrono.Tour("interface : dessin des controles");
		DragAndDrop.RenderOverlay(this, base.Skin);
		ToolTip.RenderToolTip(base.Skin);
		renderer.EndClip();
		renderer.End();
		Chrono.Tour("interface : fin du lot");
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		mNeedsRedraw = false;
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
		InvalidateChildren(recursive: true);
	}

	public void PlayAndAddSound(GameAudioInstance sound)
	{
		mUISounds.Add(sound);
		sound.Volume = 100;
		sound.Play();
	}

	private void RemoveAndDisposeDeadSounds()
	{
		mUISounds.RemoveAll(delegate(GameAudioInstance item)
		{
			if (item.State == GameAudioInstance.AudioInstanceState.Stopped || item.State == GameAudioInstance.AudioInstanceState.Disposed)
			{
				item.Dispose();
				return true;
			}
			return false;
		});
	}

	private void DoThink()
	{
		if (!base.IsHidden)
		{
			RemoveAndDisposeDeadSounds();
			Animation.GlobalThink();
			mNextTab = null;
			mFirstTab = null;
			ProcessDelayedDeletes();
			InvokeThreadQueue();
			RecurseLayout(base.Skin);
			if (mNextTab == null)
			{
				mNextTab = mFirstTab;
			}
			InputHandler.OnCanvasThink(this);
		}
	}

	public void AddDelayedDelete(Base control)
	{
		lock (_disposeQueue)
		{
			if (!_disposeQueue.Contains(control))
			{
				_disposeQueue.Add(control);
				control.Parent?.RemoveChild(control, dispose: false);
			}
		}
	}

	private void ProcessDelayedDeletes()
	{
		List<IDisposable> list;
		lock (_disposeQueue)
		{
			if (_disposeQueue.Count == 0)
			{
				return;
			}
			list = new List<IDisposable>(_disposeQueue);
			_disposeQueue.Clear();
		}
		foreach (IDisposable item in list)
		{
			try
			{
				item.Dispose();
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Error disposing control during ProcessDelayedDeletes");
			}
		}
	}

	public bool Input_MouseMoved(int x, int y, int dx, int dy)
	{
		if (base.IsHidden)
		{
			return false;
		}
		float num = 1f / Scale;
		x = (int)((float)x * num);
		y = (int)((float)y * num);
		dx = (int)((float)dx * num);
		dy = (int)((float)dy * num);
		InputHandler.OnMouseMoved(this, x, y, dx, dy);
		Base hoveredControl = InputHandler.HoveredControl;
		if (hoveredControl == null)
		{
			return false;
		}
		if (hoveredControl == this)
		{
			return false;
		}
		if (hoveredControl.Canvas != this)
		{
			return false;
		}
		hoveredControl.InputMouseMoved(x, y, dx, dy);
		hoveredControl.UpdateCursor();
		DragAndDrop.OnMouseMoved(hoveredControl, x, y);
		return true;
	}

	public bool Input_MouseButton(MouseButton button, bool down)
	{
		if (!base.IsHidden)
		{
			return InputHandler.OnMouseButtonStateChanged(this, button, down);
		}
		return false;
	}

	public bool Input_MouseScroll(int deltaX, int deltaY)
	{
		if (base.IsHidden)
		{
			return false;
		}
		return InputHandler.OnMouseScroll(this, deltaX, deltaY);
	}

	public bool Input_Key(Key key, bool down, bool shift = false)
	{
		if (base.IsHidden)
		{
			return false;
		}
		if (key <= Key.Invalid)
		{
			return false;
		}
		if (key >= Key.Count)
		{
			return false;
		}
		if (key == Key.Tab)
		{
			return OnKeyTab(down, shift);
		}
		return InputHandler.OnKeyEvent(this, key, down);
	}

	public bool Input_Character(char chr)
	{
		if (base.IsHidden)
		{
			return false;
		}
		if (char.IsControl(chr))
		{
			return false;
		}
		if (InputHandler.HandleAccelerator(this, chr))
		{
			return true;
		}
		if (InputHandler.KeyboardFocus == null)
		{
			return false;
		}
		if (InputHandler.KeyboardFocus.Canvas != this)
		{
			return false;
		}
		if (!InputHandler.KeyboardFocus.IsVisibleInTree)
		{
			return false;
		}
		return InputHandler.KeyboardFocus.InputChar(chr);
	}

	public bool Input_MouseWheel(int val)
	{
		if (base.IsHidden)
		{
			return false;
		}
		if (InputHandler.HoveredControl == null)
		{
			return false;
		}
		if (InputHandler.HoveredControl == this)
		{
			return false;
		}
		if (InputHandler.HoveredControl.Canvas != this)
		{
			return false;
		}
		return InputHandler.HoveredControl.InputMouseWheeled(val);
	}
}
