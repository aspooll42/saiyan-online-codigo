using System;
using System.Linq;
using Intersect.Client.Core.Controls;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Inventory;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Spells;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Utilities;

namespace Intersect.Client.Interface.Game.Hotbar;

public class HotbarItem : SlotItem
{
	public const int CharteSize = 50;

	public const int CharteLift = 3;

	public const int CharteRankGap = 3;

	public const int CharteRankHeight = 3;

	private const int IconSize = 32;

	private const int IconTop = 3;

	private const int KeyTop = 37;

	private const int KeyLine = 11;

	private const int KeySize = 9;

	private static readonly Color WellTop = new Color(255, 17, 24, 41);

	private static readonly Color WellBottom = new Color(255, 13, 19, 34);

	private static readonly Color WellEdge = new Color(255, 26, 34, 54);

	private static readonly Color HotTop = new Color(255, 23, 32, 54);

	private static readonly Color HotBottom = new Color(255, 18, 26, 44);

	private static readonly Color HotEdge = new Color(255, 58, 72, 103);

	private static readonly Color EmptyWell = new Color(255, 10, 15, 26);

	private static readonly Color EmptyEdge = new Color(255, 22, 29, 46);

	private static readonly Color KeyDim = new Color(255, 52, 61, 85);

	private static readonly Color RankOff = new Color(255, 26, 33, 51);

	private static readonly Color VeilInk = new Color(173, 4, 6, 11);

	private bool _charte;

	private string? _keyText;

	private bool _showQuantity;

	private bool _showEquip;

	private bool _showCooldownText;

	private float _veilRatio;

	private float _flashAlpha;

	private float _lift;

	private long _liftAt;

	private readonly Label _cooldownLabel;

	private readonly Label _equipLabel;

	private readonly Label _keyLabel;

	private const long ReadyFlashMs = 450L;

	private readonly ImagePanel _cooldownVeil;

	private readonly ImagePanel _readyFlash;

	private long _cooldownLongest;

	private bool _wasCoolingDown;

	private long _readyFlashAt = -1L;

	private Guid _currentId = Guid.Empty;

	private ItemDescriptor? _currentItem;

	private SpellDescriptor? _currentSpell;

	private bool _isEquipped;

	private bool _isFaded;

	private readonly Intersect.Client.Framework.Gwen.Control.Base _hotbarWindow;

	private ControlBinding? _hotKey;

	private Item? _inventoryItem;

	private int _inventoryItemIndex = -1;

	private Label _quantityLabel;

	private Spell? _spellBookItem;

	private bool _textureLoaded;

	protected override bool ShouldClip => !_charte;

	private bool IsFilled
	{
		get
		{
			if (_currentSpell == null)
			{
				return _currentItem != null;
			}
			return true;
		}
	}

	private bool IsHot
	{
		get
		{
			if (Icon.IsHovered || IsHovered || (Manette.EnUsage && Manette.TechniquesArmees))
			{
				return true;
			}
			if (_lift < 0.5f)
			{
				return false;
			}
			Point point = CanvasPosToLocal(InputHandler.MousePosition);
			if (point.X >= 0 && point.X < base.Width && point.Y >= -3)
			{
				return point.Y < base.Height;
			}
			return false;
		}
	}

	internal void ApplyCharte()
	{
		_charte = true;
		base.Texture = null;
		base.TextureFilename = null;
		Icon.SetBounds(0, 0, 50, 50);
		Icon.ImageRect = new Rectangle(9, 3, 32, 32);
		_readyFlash.SetBounds(0, 0, 50, 50);
	}

	private void CaptureCharteState()
	{
		if (_charte)
		{
			_showQuantity = !_quantityLabel.IsHidden && !string.IsNullOrEmpty(_quantityLabel.Text);
			_showEquip = !_equipLabel.IsHidden;
			_showCooldownText = !_cooldownLabel.IsHidden && !string.IsNullOrEmpty(_cooldownLabel.Text);
			_keyText = ((_hotKey == null || _hotKey.Key == Keys.None) ? null : _keyLabel.Text);
			_veilRatio = (_cooldownVeil.IsHidden ? 0f : Math.Clamp((float)_cooldownVeil.Height / (float)Math.Max(1, base.Height - 2), 0f, 1f));
			_flashAlpha = (_readyFlash.IsHidden ? 0f : ((float)(int)_readyFlash.RenderColor.A / 255f));
			_quantityLabel.IsHidden = true;
			_equipLabel.IsHidden = true;
			_cooldownLabel.IsHidden = true;
			_keyLabel.IsHidden = true;
			_cooldownVeil.IsHidden = true;
			_readyFlash.IsHidden = true;
			AdvanceLift();
		}
	}

	private void AdvanceLift()
	{
		long milliseconds = Timing.Global.Milliseconds;
		float num = ((_liftAt == 0L) ? 0f : Math.Min(0.1f, (float)(milliseconds - _liftAt) / 1000f));
		_liftAt = milliseconds;
		int num2 = ((IsFilled && !Icon.IsDragging && IsHot) ? 3 : 0);
		_lift += ((float)num2 - _lift) * (1f - MathF.Exp((0f - num) * 18f));
		int num3 = 3 - (int)MathF.Round(_lift);
		if (Icon.IsDragging)
		{
			return;
		}
		Rectangle? imageRect = Icon.ImageRect;
		if (imageRect.HasValue)
		{
			Rectangle valueOrDefault = imageRect.GetValueOrDefault();
			if (valueOrDefault.Y != num3)
			{
				Icon.ImageRect = new Rectangle(valueOrDefault.X, num3, valueOrDefault.Width, valueOrDefault.Height);
			}
		}
	}

	private Color CharteAccent()
	{
		if (_currentSpell != null)
		{
			return TechniqueArt.FamilyColor(SpellMastery.FamilyOf(_currentSpell));
		}
		if (_currentItem != null && CustomColors.Items.Rarities.TryGetValue(JetDObjet.Rarete(_currentItem, _inventoryItem?.ItemProperties), out var value) && value != null)
		{
			return value;
		}
		return TechniqueArt.Soft;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (_charte)
		{
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			int num = -(int)MathF.Round(_lift);
			Rectangle rectangle = new Rectangle(0, num, base.Width, base.Height);
			if (!IsFilled)
			{
				CharteFill(renderer, rectangle, EmptyWell);
				CharteOutline(renderer, rectangle, EmptyEdge);
				return;
			}
			bool isHot = IsHot;
			CharteFill(renderer, new Rectangle(0, num, base.Width, base.Height / 2), isHot ? HotTop : WellTop);
			CharteFill(renderer, new Rectangle(0, num + base.Height / 2, base.Width, base.Height - base.Height / 2), isHot ? HotBottom : WellBottom);
			CharteOutline(renderer, rectangle, isHot ? HotEdge : WellEdge);
		}
	}

	protected override void RenderOver(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.RenderOver(skin);
		if (!_charte)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		int num = -(int)MathF.Round(_lift);
		bool isFilled = IsFilled;
		bool flag = isFilled && IsHot;
		Color color = CharteAccent();
		if (_veilRatio > 0f)
		{
			CharteFill(renderer, new Rectangle(1, num + 1, base.Width - 2, (int)MathF.Ceiling((float)(base.Height - 2) * _veilRatio)), VeilInk);
		}
		if (_showCooldownText)
		{
			int num2 = num + 3 + 16;
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, 13, _cooldownLabel.Text, base.Width / 2 + 1, num2 + 1, new Color(210, 0, 0, 0));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, 13, _cooldownLabel.Text, base.Width / 2, num2, TechniqueArt.White);
		}
		if (_flashAlpha > 0f && isFilled)
		{
			TechniqueArt.Glow(renderer, (float)base.Width / 2f, (float)num + (float)base.Height / 2f, (float)base.Width * 0.7f, TechniqueArt.Alpha(color, 0.45f * _flashAlpha));
			CharteOutline(renderer, new Rectangle(0, num, base.Width, base.Height), TechniqueArt.Alpha(color, _flashAlpha));
		}
		if (_showQuantity)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, 10, _quantityLabel.Text, base.Width - 2, num + 8, new Color(220, 0, 0, 0));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, 10, _quantityLabel.Text, base.Width - 3, num + 7, TechniqueArt.White);
		}
		if (_showEquip)
		{
			Rectangle rect = new Rectangle(base.Width - 13, num + 2, 11, 11);
			TechniqueArt.Image(renderer, "tech_point.png", rect, TechniqueArt.Gold);
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, 7, "E", rect.X + rect.Width / 2, rect.Y + rect.Height / 2, new Color(255, 26, 18, 6));
		}
		Manette.Bouton? bouton = (Manette.TechniquesArmees ? ((Manette.Bouton?)null) : Manette.BoutonDirectDeCase(SlotIndex));
		if (Manette.EnUsage)
		{
			Manette.Bouton? bouton2 = bouton ?? Manette.BoutonDeCase(SlotIndex);
			if (bouton2.HasValue)
			{
				Manette.Bouton valueOrDefault = bouton2.GetValueOrDefault();
				bool flag2 = (uint)(valueOrDefault - 4) <= 1u;
				Rectangle rect2 = (flag2 ? new Rectangle(-4, num - 5, 22, 13) : new Rectangle(-4, num - 5, 17, 17));
				bool flag3 = Manette.TechniquesArmees || bouton.HasValue;
				TechniqueArt.Image(renderer, Manette.Glyphe(valueOrDefault), rect2, TechniqueArt.Alpha(TechniqueArt.White, flag3 ? 1f : 0.6f));
				goto IL_0397;
			}
		}
		if (!string.IsNullOrEmpty(_keyText))
		{
			Color color2 = ((!isFilled || _veilRatio > 0f) ? KeyDim : (flag ? TechniqueArt.Soft : TechniqueArt.Dim));
			int size = ((SaiyanHud.Measure(renderer, SaiyanHud.SmallFont, 9, _keyText).X > base.Width - 4) ? 7 : 9);
			SaiyanHud.TextCenter(renderer, SaiyanHud.SmallFont, size, _keyText, 0, num + 37, base.Width, 11, color2, shadow: false);
		}
		goto IL_0397;
		IL_0397:
		DessinerLaCibleDuGlisser(renderer, num);
		if (!isFilled)
		{
			return;
		}
		int y = base.Height + 3;
		Color color3 = TechniqueArt.Alpha(color, (flag || _flashAlpha > 0f) ? 1f : 0.7f);
		if (_currentSpell != null && SpellMastery.HasMastery(_currentSpell) && _spellBookItem != null)
		{
			int num3 = SpellMastery.Clamp(_spellBookItem.Rank);
			int num4 = 5;
			int num5 = (base.Width - (num4 - 1) * 2) / num4;
			int num6 = (base.Width - (num4 * num5 + (num4 - 1) * 2)) / 2;
			for (int i = 1; i <= num4; i++)
			{
				Rectangle rectangle = new Rectangle(num6 + (i - 1) * (num5 + 2), y, num5, 3);
				CharteFill(renderer, rectangle, (i <= num3) ? color3 : RankOff);
			}
		}
		else
		{
			CharteFill(renderer, new Rectangle(0, y, base.Width, 3), color3);
		}
	}

	private void DessinerLaCibleDuGlisser(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int top)
	{
		Intersect.Client.Framework.Gwen.Control.Base obj = null;
		FenetreCharte fenetreCharte = InputHandler.MouseFocus as FenetreCharte;
		if (fenetreCharte == null || !fenetreCharte.ObjetEnVol)
		{
			fenetreCharte = null;
			Draggable draggable = default(Draggable);
			int num;
			if (DragAndDrop.IsDragging)
			{
				draggable = DragAndDrop.SourceControl as Draggable;
				num = ((draggable == null) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			bool flag = (byte)num != 0;
			bool flag2 = flag;
			if (!flag2)
			{
				Intersect.Client.Framework.Gwen.Control.Base parent = draggable.Parent;
				bool flag3 = ((parent is HotbarItem || parent is InventoryItem) ? true : false);
				flag2 = !flag3;
			}
			if (flag2)
			{
				return;
			}
			obj = draggable.Parent;
		}
		Rectangle rectangle = new Rectangle(0, top, base.Width, base.Height);
		Color color = ((obj is HotbarItem hotbarItem) ? hotbarItem.CharteAccent() : (fenetreCharte?.CouleurEnVol ?? TechniqueArt.Soft));
		if (obj == this)
		{
			IGameTexture texture = Icon.Texture;
			if (texture != null)
			{
				Rectangle? imageRect = Icon.ImageRect;
				if (imageRect.HasValue)
				{
					Rectangle valueOrDefault = imageRect.GetValueOrDefault();
					SaiyanHud.Draw(renderer, texture, valueOrDefault.X, valueOrDefault.Y, valueOrDefault.Width, valueOrDefault.Height, TechniqueArt.Alpha(TechniqueArt.White, 0.28f));
				}
			}
			CharteOutline(renderer, rectangle, TechniqueArt.Alpha(color, 0.5f));
		}
		else
		{
			Point point = CanvasPosToLocal(InputHandler.MousePosition);
			if (point.X >= 0 && point.X < base.Width && point.Y >= -3 && point.Y < base.Height)
			{
				TechniqueArt.Glow(renderer, (float)base.Width / 2f, (float)top + (float)base.Height / 2f, (float)base.Width * 0.8f, TechniqueArt.Alpha(color, 0.55f));
				CharteOutline(renderer, rectangle, TechniqueArt.Mix(color, TechniqueArt.White, 0.8f));
				CharteOutline(renderer, new Rectangle(1, top + 1, base.Width - 2, base.Height - 2), color);
			}
			else
			{
				float num2 = 0.5f - 0.5f * MathF.Cos((float)Timing.Global.MillisecondsUtc / 900f * ((float)Math.PI * 2f));
				CharteOutline(renderer, rectangle, TechniqueArt.Alpha(color, 0.3f + 0.3f * num2));
			}
		}
	}

	private static void CharteFill(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rectangle, Color color)
	{
		renderer.DrawColor = color;
		renderer.DrawFilledRect(rectangle);
	}

	private static void CharteOutline(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rectangle, Color color)
	{
		renderer.DrawColor = color;
		renderer.DrawFilledRect(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1));
		renderer.DrawFilledRect(new Rectangle(rectangle.X, rectangle.Bottom - 1, rectangle.Width, 1));
		renderer.DrawFilledRect(new Rectangle(rectangle.X, rectangle.Y + 1, 1, rectangle.Height - 2));
		renderer.DrawFilledRect(new Rectangle(rectangle.Right - 1, rectangle.Y + 1, 1, rectangle.Height - 2));
	}

	public HotbarItem(int hotbarSlotIndex, Intersect.Client.Framework.Gwen.Control.Base hotbarWindow)
		: base(hotbarWindow, $"HotbarContainer{hotbarSlotIndex}", hotbarSlotIndex, null)
	{
		_hotbarWindow = hotbarWindow;
		int num = hotbarSlotIndex % 10;
		int num2 = hotbarSlotIndex / 10;
		base.X = 4 + num * 40;
		base.Y = 4 + num2 * 40;
		base.Width = 36;
		base.Height = 36;
		base.Margin = new Margin((num > 0) ? 4 : 0, (num2 > 0) ? 4 : 0, 0, 0);
		base.RestrictToParent = false;
		base.TextureFilename = "hotbaritem.png";
		Icon.Name = $"{"HotbarItem"}{SlotIndex}";
		Icon.SetPosition(1, 1);
		Icon.HoverEnter += Icon_HoverEnter;
		Icon.HoverLeave += Icon_HoverLeave;
		Icon.Clicked += Icon_Clicked;
		Icon.DoubleClicked += Icon_DoubleClicked;
		IFont font = GameContentManager.Current.GetFont("rajdhanibold");
		_equipLabel = new Label(this, $"EquipLabel{hotbarSlotIndex}")
		{
			Alignment = new Alignments[2]
			{
				Alignments.Top,
				Alignments.Left
			},
			X = 26,
			Y = 0,
			Width = 10,
			Height = 11,
			BackgroundTemplateName = "equipped.png",
			Font = font,
			FontSize = 9,
			IsHidden = true,
			Padding = Padding.FourH,
			Text = Strings.Inventory.EquippedSymbol,
			TextColorOverride = Color.White
		};
		_quantityLabel = new Label(this, $"QuantityLabel{hotbarSlotIndex}")
		{
			Alignment = new Alignments[2]
			{
				Alignments.Top,
				Alignments.Right
			},
			X = 32,
			Y = 0,
			Width = 4,
			Height = 11,
			BackgroundTemplateName = "quantity.png",
			Font = font,
			FontSize = 9,
			IsHidden = true,
			Padding = Padding.FourH,
			TextColorOverride = Color.White
		};
		_cooldownLabel = new Label(this, $"CooldownLabel{hotbarSlotIndex}")
		{
			Alignment = new Alignments[1] { Alignments.Center },
			TextAlign = Pos.Center,
			X = 16,
			Y = 12,
			Width = 4,
			Height = 11,
			BackgroundTemplateName = "quantity.png",
			Font = font,
			FontSize = 9,
			IsHidden = true,
			Padding = Padding.FourH,
			TextColorOverride = Color.White
		};
		_keyLabel = new Label(this, $"KeyLabel{hotbarSlotIndex}")
		{
			Alignment = new Alignments[2]
			{
				Alignments.Bottom,
				Alignments.Right
			},
			X = 31,
			Y = 25,
			Width = 5,
			Height = 11,
			BackgroundTemplateName = "hotbar_label.png",
			Font = font,
			FontSize = 9,
			Padding = Padding.FourH,
			TextColorOverride = Color.White
		};
		_cooldownVeil = new ImagePanel(this, $"CooldownVeil{hotbarSlotIndex}")
		{
			MouseInputEnabled = false,
			IsHidden = true,
			TextureFilename = "hud_cooldown.png",
			TextureNinePatchMargin = new Margin(1, 1, 1, 3)
		};
		_readyFlash = new ImagePanel(this, $"ReadyFlash{hotbarSlotIndex}")
		{
			MouseInputEnabled = false,
			IsHidden = true,
			TextureFilename = "hud_ready.png"
		};
		_readyFlash.SetBounds(0, 0, 36, 36);
		_cooldownLabel.BringToFront();
		_keyLabel.BringToFront();
	}

	public void Activate()
	{
		if (Globals.InputManager.IsMouseButtonDown(MouseButton.Right) || DragAndDrop.IsDragging || !(_currentId != Guid.Empty) || Globals.Me == null)
		{
			return;
		}
		if (_currentItem != null)
		{
			if (_inventoryItemIndex > -1)
			{
				Globals.Me.TryUseItem(_inventoryItemIndex);
			}
		}
		else if (_currentSpell != null)
		{
			Globals.Me.TryUseSpell(_currentSpell.Id);
		}
	}

	private void Icon_Clicked(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState arguments)
	{
#if !ANDROID
		if (arguments.MouseButton == MouseButton.Right)
		{
			Globals.Me?.AddToHotbar(SlotIndex, -1, -1);
		}
#else
		if (arguments.MouseButton == MouseButton.Left)
		{
			Activate();
		}
#endif
	}

	private void Icon_DoubleClicked(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState arguments)
	{
#if !ANDROID
		if (arguments.MouseButton == MouseButton.Left)
		{
			Activate();
		}
#endif
	}

	private void Icon_HoverLeave(Intersect.Client.Framework.Gwen.Control.Base sender, EventArgs arguments)
	{
		Interface.GameUi.ItemDescriptionWindow?.Hide();
	}

	private void Icon_HoverEnter(Intersect.Client.Framework.Gwen.Control.Base sender, EventArgs arguments)
	{
		if (InputHandler.MouseFocus != null || Globals.Me == null || Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
		{
			return;
		}
		if (_currentItem != null && _inventoryItem != null)
		{
			int amount = 1;
			if (_currentItem.IsStackable)
			{
				amount = Globals.Me.GetQuantityOfItemInInventory(_currentItem.Id);
			}
			Interface.GameUi.ItemDescriptionWindow?.Show(_currentItem, amount, _inventoryItem.ItemProperties);
		}
		else if (_currentSpell != null)
		{
			Interface.GameUi.SpellDescriptionWindow?.Show(_currentSpell.Id);
		}
	}

	public override bool DragAndDrop_HandleDrop(Package package, int x, int y)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return false;
		}
		for (Intersect.Client.Framework.Gwen.Control.Base obj = Interface.FindComponentUnderCursor(); obj != null; obj = obj.Parent)
		{
			if (obj is HotbarItem hotbarItem)
			{
				me.HotbarSwap(SlotIndex, hotbarItem.SlotIndex);
				return true;
			}
		}
		return false;
	}

	public override void Update()
	{
		if (Globals.Me == null || Controls.ActiveControls == null)
		{
			return;
		}
		Control control = (Control)(4096 + SlotIndex + 1);
		ControlBinding controlBinding = null;
		if (Controls.ActiveControls.TryGetMappingFor(control, out ControlMapping mapping))
		{
			controlBinding = mapping.Bindings.FirstOrDefault();
		}
		if (_hotKey == null || _hotKey.Modifier != (controlBinding?.Modifier ?? Keys.None) || _hotKey.Key != (controlBinding?.Key ?? Keys.None))
		{
			Keys? keys = controlBinding?.Key;
			if ((!keys.HasValue || keys.GetValueOrDefault() == Keys.None) ? true : false)
			{
				_keyLabel.IsVisibleInTree = false;
			}
			else
			{
				string text = controlBinding.Key.GetKeyId().ToLowerInvariant();
				if (!Strings.Keys.KeyDictionary.TryGetValue(text, out LocalizedString value))
				{
					value = text;
				}
				string text2 = value;
				Keys modifier = controlBinding.Modifier;
				if (modifier != Keys.None)
				{
					string text3 = modifier.GetKeyId(isModifier: true).ToLowerInvariant();
					LocalizedString value2;
					string text4 = (Strings.Keys.KeyDictionary.TryGetValue(text3, out value2) ? ((string)value2) : text3);
					text2 = Strings.Keys.KeyNameWithModifier.ToString(text4, text2);
				}
				_keyLabel.Text = text2;
				_keyLabel.IsVisibleInTree = true;
			}
			_hotKey = ((controlBinding == null) ? null : new ControlBinding(controlBinding));
		}
		HotbarInstance hotbarInstance = Globals.Me.Hotbar[SlotIndex];
		bool flag = _currentId != hotbarInstance.ItemOrSpellId || !_textureLoaded;
		if (_currentId != hotbarInstance.ItemOrSpellId)
		{
			_currentItem = null;
			_currentSpell = null;
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(hotbarInstance.ItemOrSpellId);
			SpellDescriptor spellDescriptor = DatabaseObject<SpellDescriptor>.Get(hotbarInstance.ItemOrSpellId);
			if (itemDescriptor != null)
			{
				_currentItem = itemDescriptor;
			}
			if (spellDescriptor != null)
			{
				_currentSpell = spellDescriptor;
			}
			_currentId = hotbarInstance.ItemOrSpellId;
		}
		_spellBookItem = null;
		_inventoryItem = null;
		_inventoryItemIndex = -1;
		if (_currentItem != null)
		{
			int num = Globals.Me.FindHotbarItem(hotbarInstance);
			if (num > -1)
			{
				_inventoryItemIndex = num;
				_inventoryItem = (Item)Globals.Me.Inventory[num];
			}
		}
		else if (_currentSpell != null)
		{
			int num2 = Globals.Me.FindHotbarSpell(hotbarInstance);
			if (num2 > -1)
			{
				_spellBookItem = Globals.Me.Spells[num2];
			}
		}
		if (_currentItem != null)
		{
			if (_inventoryItem == null && !_isFaded)
			{
				flag = true;
			}
			if (_inventoryItem != null && Globals.Me.IsEquipped(_inventoryItemIndex) != _isEquipped)
			{
				flag = true;
			}
			if (_inventoryItem != null && Globals.Me.IsItemOnCooldown(_inventoryItemIndex))
			{
				flag = true;
			}
			if (_inventoryItem != null && Globals.Me.IsItemOnCooldown(_inventoryItemIndex) != _isFaded)
			{
				flag = true;
			}
			string text5 = Strings.FormatQuantityAbbreviated(Globals.Me.GetQuantityOfItemInInventory(_currentItem.Id));
			if (_inventoryItem != null && _quantityLabel.Text != text5)
			{
				_quantityLabel.Text = text5;
				flag = true;
			}
		}
		if (_currentSpell != null)
		{
			if (_spellBookItem == null)
			{
				Globals.Me.AddToHotbar(SlotIndex, -1, -1);
				flag = true;
			}
			if (_spellBookItem != null && Globals.Me.GetSpellCooldown(_spellBookItem.Id) > Timing.Global.Milliseconds)
			{
				flag = true;
			}
			if (_spellBookItem != null && Globals.Me.GetSpellCooldown(_spellBookItem.Id) > Timing.Global.Milliseconds != _isFaded)
			{
				flag = true;
			}
		}
		bool isDragging = Icon.IsDragging;
		bool flag2 = _inventoryItemIndex < 0;
		if (isDragging)
		{
			_equipLabel.IsHidden = true;
			_quantityLabel.IsHidden = true;
			_cooldownLabel.IsHidden = true;
		}
		else
		{
			_equipLabel.IsHidden = !_isEquipped || flag2;
			Label quantityLabel = _quantityLabel;
			ItemDescriptor? currentItem = _currentItem;
			quantityLabel.IsHidden = (currentItem != null && !currentItem.Stackable) || flag2;
			_cooldownLabel.IsHidden = !_isFaded || flag2;
		}
		if (flag)
		{
			if (_currentItem != null)
			{
				Icon.IsVisibleInTree = !isDragging;
				Icon.Texture = GameContentManager.Current.GetTexture(TextureType.Item, _currentItem.Icon);
				_equipLabel.IsHidden = true;
				_quantityLabel.IsHidden = true;
				_cooldownLabel.IsHidden = true;
				if (!flag2)
				{
					_isFaded = Globals.Me.IsItemOnCooldown(_inventoryItemIndex);
					_isEquipped = Globals.Me.IsEquipped(_inventoryItemIndex);
					if (_isFaded && !isDragging)
					{
						_cooldownLabel.IsHidden = false;
						_cooldownLabel.Text = TimeSpan.FromMilliseconds(Globals.Me.GetItemRemainingCooldown(_inventoryItemIndex)).WithSuffix();
					}
				}
				else
				{
					_equipLabel.IsHidden = true;
					_quantityLabel.IsHidden = true;
					_cooldownLabel.IsHidden = true;
					_isEquipped = false;
					_isFaded = true;
				}
				_textureLoaded = true;
			}
			else if (_currentSpell != null)
			{
				Icon.IsVisibleInTree = !isDragging;
				Icon.Texture = Globals.ContentManager.GetTexture(TextureType.Spell, _currentSpell.Icon);
				_equipLabel.IsHidden = true;
				_quantityLabel.IsHidden = true;
				_cooldownLabel.IsHidden = true;
				if (_spellBookItem != null)
				{
					int slot = Globals.Me.FindHotbarSpell(hotbarInstance);
					_isFaded = Globals.Me.IsSpellOnCooldown(slot);
					if (_isFaded && !isDragging)
					{
						_cooldownLabel.IsHidden = false;
						long spellRemainingCooldown = Globals.Me.GetSpellRemainingCooldown(slot);
						_cooldownLabel.Text = TimeSpan.FromMilliseconds(spellRemainingCooldown).WithSuffix("0.0");
					}
				}
				else
				{
					_isFaded = true;
				}
				_textureLoaded = true;
				_isEquipped = false;
			}
			else
			{
				Icon.Hide();
				_textureLoaded = true;
				_isEquipped = false;
				_equipLabel.IsHidden = true;
				_quantityLabel.IsHidden = true;
				_cooldownLabel.IsHidden = true;
			}
			if (_isFaded)
			{
				if (_currentSpell != null)
				{
					Icon.RenderColor = new Color(60, 255, 255, 255);
				}
				if (_currentItem != null)
				{
					Icon.RenderColor = new Color(60, _currentItem.Color.R, _currentItem.Color.G, _currentItem.Color.B);
				}
			}
			else
			{
				if (_currentSpell != null)
				{
					Icon.RenderColor = Color.White;
				}
				if (_currentItem != null)
				{
					Icon.RenderColor = _currentItem.Color;
				}
			}
		}
		UpdateCooldownVeil(isDragging);
		CaptureCharteState();
	}

	private void UpdateCooldownVeil(bool isDragging)
	{
		long num = 0L;
		if (_currentItem != null && _inventoryItemIndex > -1 && Globals.Me.IsItemOnCooldown(_inventoryItemIndex))
		{
			num = Globals.Me.GetItemRemainingCooldown(_inventoryItemIndex);
		}
		else if (_currentSpell != null && _spellBookItem != null)
		{
			int num2 = Globals.Me.FindHotbarSpell(Globals.Me.Hotbar[SlotIndex]);
			if (num2 > -1 && Globals.Me.IsSpellOnCooldown(num2))
			{
				num = Globals.Me.GetSpellRemainingCooldown(num2);
			}
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (num > 0)
		{
			_cooldownLongest = Math.Max(_cooldownLongest, num);
			int val = (int)Math.Ceiling((double)(base.Height - 2) * ((double)num / (double)_cooldownLongest));
			_cooldownVeil.SetBounds(1, 1, base.Width - 2, Math.Max(4, val));
			_cooldownVeil.IsHidden = isDragging;
			_wasCoolingDown = true;
		}
		else
		{
			_cooldownVeil.IsHidden = true;
			_cooldownLongest = 0L;
			if (_wasCoolingDown)
			{
				_wasCoolingDown = false;
				_readyFlashAt = milliseconds;
			}
		}
		long num3 = ((_readyFlashAt < 0) ? long.MaxValue : (milliseconds - _readyFlashAt));
		if (num3 < 450 && !isDragging)
		{
			_readyFlash.IsHidden = false;
			_readyFlash.RenderColor = new Color((int)(255f * (1f - (float)num3 / 450f)), 255, 255, 255);
		}
		else
		{
			_readyFlash.IsHidden = true;
		}
		if (Heritage.Actif)
		{
			Heritage.Moment.Eclairer(_readyFlash, _currentSpell?.Id ?? Guid.Empty, isDragging);
		}
	}
}
