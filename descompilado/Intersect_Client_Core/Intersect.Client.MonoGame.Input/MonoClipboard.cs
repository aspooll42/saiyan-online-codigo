using Intersect.Client.Framework.Input;
using Intersect.Client.MonoGame.NativeInterop;

namespace Intersect.Client.MonoGame.Input;

public class MonoClipboard : GameClipboard
{
	public override bool IsEmpty => string.IsNullOrEmpty(GetText());

#if !ANDROID
	public override bool IsEnabled => Sdl2.IsClipboardSupported;
#else
	public override bool IsEnabled => false;
#endif

	public override void SetText(string data)
	{
#if !ANDROID
		Sdl2.SDL_SetClipboardText(data);
#endif
	}

	public override string GetText()
	{
#if !ANDROID
		return Sdl2.SDL_GetClipboardText();
#else
		return string.Empty;
#endif
	}
}
