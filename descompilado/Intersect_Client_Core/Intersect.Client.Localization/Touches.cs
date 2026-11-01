using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using Intersect.Client.Core.Controls;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Localization;

public static partial class Touches
{
	[GeneratedRegex("\\{touche:([A-Za-z]+)\\}")]
	private static partial Regex Jeton();

	public static string Remplacer(string? texte)
	{
		if (string.IsNullOrEmpty(texte) || !texte.Contains("{touche:", StringComparison.Ordinal))
		{
			return texte ?? string.Empty;
		}
		return Jeton().Replace(texte, (Match jeton) => Nom(jeton.Groups[1].Value) ?? jeton.Value);
	}

	private static string? Nom(string controle)
	{
		if (!Enum.TryParse<Control>(controle, ignoreCase: true, out var result))
		{
			return null;
		}
		return NomDe(result);
	}

	public static string? NomDe(Control control)
	{
		ControlMapping mapping;
		IEnumerable<ControlBinding> source = (Controls.ActiveControls.TryGetMappingFor(control, out mapping) ? ((IEnumerable<ControlBinding>)mapping.Bindings) : ((IEnumerable<ControlBinding>)new List<ControlBinding>()));
		ControlBinding controlBinding = source.FirstOrDefault((ControlBinding candidate) => candidate != null && !candidate.IsMouseKey && candidate.Key != Keys.None) ?? source.FirstOrDefault((ControlBinding candidate) => candidate.Key != Keys.None);
		try
		{
			return (controlBinding == null) ? Strings.Keys.FormatKeyName(Keys.None, Keys.None) : Strings.Keys.FormatKeyName(controlBinding.Modifier, controlBinding.Key);
		}
		catch (KeyNotFoundException)
		{
			return null;
		}
	}
}
