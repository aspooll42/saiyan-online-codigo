using System;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Framework.Eventing;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class Checkbox : Button, ICheckbox
{
	public enum ControlState
	{
		Normal,
		Disabled,
		CheckedNormal,
		CheckedDisabled
	}

	private bool mChecked;

	private IGameTexture mCheckedDisabledImage;

	private string mCheckedDisabledImageFilename;

	private IGameTexture mCheckedNormalImage;

	private string mCheckedNormalImageFilename;

	private string mCheckSound;

	private IGameTexture mDisabledImage;

	private string mDisabledImageFilename;

	private IGameTexture mNormalImage;

	private string mNormalImageFilename;

	private string mUncheckedSound;

	public bool IsChecked
	{
		get
		{
			return mChecked;
		}
		set
		{
			if (mChecked != value)
			{
				mChecked = value;
				OnCheckChanged(value);
			}
		}
	}

	protected virtual bool AllowUncheck => true;

	public event EventHandler<ICheckbox, EventArgs>? Checked;

	public event EventHandler<ICheckbox, EventArgs>? Unchecked;

	public event EventHandler<ICheckbox, ValueChangedEventArgs<bool>>? CheckChanged;

	public Checkbox(Base parent, string? name = null, bool disableText = true)
		: base(parent, name, disableText)
	{
		base.MinimumSize = new Point(22, 22);
		base.Size = new Point(22, 22);
		base.IsToggle = true;
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("NormalImage", GetImageFilename(ControlState.Normal));
		json.Add("CheckedImage", GetImageFilename(ControlState.CheckedNormal));
		json.Add("DisabledImage", GetImageFilename(ControlState.Disabled));
		json.Add("CheckedDisabledImage", GetImageFilename(ControlState.CheckedDisabled));
		json.Add("CheckedSound", mCheckSound);
		json.Add("UncheckedSound", mUncheckedSound);
		return base.FixJson(json);
	}

	public override void LoadJson(JToken obj, bool isRoot = false)
	{
		base.LoadJson(obj, isRoot: false);
		if (obj["NormalImage"] != null)
		{
			SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)obj["NormalImage"]), (string?)obj["NormalImage"], ControlState.Normal);
		}
		if (obj["CheckedImage"] != null)
		{
			SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)obj["CheckedImage"]), (string?)obj["CheckedImage"], ControlState.CheckedNormal);
		}
		if (obj["DisabledImage"] != null)
		{
			SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)obj["DisabledImage"]), (string?)obj["DisabledImage"], ControlState.Disabled);
		}
		if (obj["CheckedDisabledImage"] != null)
		{
			SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)obj["CheckedDisabledImage"]), (string?)obj["CheckedDisabledImage"], ControlState.CheckedDisabled);
		}
		if (obj["CheckedSound"] != null)
		{
			mCheckSound = (string?)obj["CheckedSound"];
		}
		if (obj["UncheckedSound"] != null)
		{
			mUncheckedSound = (string?)obj["UncheckedSound"];
		}
	}

	public override void Toggle()
	{
		base.Toggle();
		IsChecked = !IsChecked;
		if (IsChecked)
		{
			PlaySound(mCheckSound);
		}
		else
		{
			PlaySound(mUncheckedSound);
		}
	}

	protected virtual void OnCheckChanged(bool isChecked)
	{
		if (isChecked)
		{
			this.Checked?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			this.Unchecked?.Invoke(this, EventArgs.Empty);
		}
		this.CheckChanged?.Invoke(this, new ValueChangedEventArgs<bool>
		{
			Value = isChecked,
			OldValue = !isChecked
		});
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawCheckBox(this, mChecked, IsHovered, base.IsActive);
	}

	public void SetCheckSize(int w, int h)
	{
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		if (!IsDisabledByTree && (!IsChecked || AllowUncheck))
		{
			base.OnMouseClicked(mouseButton, mousePosition, userAction);
		}
	}

	public void SetImage(IGameTexture texture, string fileName, ControlState state)
	{
		switch (state)
		{
		case ControlState.Normal:
			mNormalImageFilename = fileName;
			mNormalImage = texture;
			break;
		case ControlState.Disabled:
			mDisabledImageFilename = fileName;
			mDisabledImage = texture;
			break;
		case ControlState.CheckedNormal:
			mCheckedNormalImageFilename = fileName;
			mCheckedNormalImage = texture;
			break;
		case ControlState.CheckedDisabled:
			mCheckedDisabledImageFilename = fileName;
			mCheckedDisabledImage = texture;
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public IGameTexture GetImage(ControlState state)
	{
		return state switch
		{
			ControlState.Normal => mNormalImage, 
			ControlState.Disabled => mDisabledImage, 
			ControlState.CheckedNormal => mCheckedNormalImage, 
			ControlState.CheckedDisabled => mCheckedDisabledImage, 
			_ => null, 
		};
	}

	public string GetImageFilename(ControlState state)
	{
		return state switch
		{
			ControlState.Normal => mNormalImageFilename, 
			ControlState.Disabled => mDisabledImageFilename, 
			ControlState.CheckedNormal => mCheckedNormalImageFilename, 
			ControlState.CheckedDisabled => mCheckedDisabledImageFilename, 
			_ => null, 
		};
	}
}
