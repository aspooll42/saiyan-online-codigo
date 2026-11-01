namespace Intersect.Client.Framework.Gwen.Control;

public class TextBoxPassword : TextBox
{
	private string mMask;

	private char mMaskCharacter;

	public char MaskCharacter
	{
		get
		{
			return mMaskCharacter;
		}
		set
		{
			mMaskCharacter = value;
		}
	}

	public TextBoxPassword(Base parent, string? name = null)
		: base(parent, name)
	{
		mMaskCharacter = '*';
	}

	protected override void OnTextChanged()
	{
		mMask = new string(MaskCharacter, Text?.Length ?? 0);
		base.TextOverride = mMask;
		base.OnTextChanged();
	}
}
