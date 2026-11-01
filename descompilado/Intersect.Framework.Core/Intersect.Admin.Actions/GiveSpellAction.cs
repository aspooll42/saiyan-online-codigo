using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class GiveSpellAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.GiveSpell;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public Guid SpellId { get; set; }

	[Key(4)]
	public bool Remove { get; set; }

	public GiveSpellAction()
	{
	}

	public GiveSpellAction(string name, Guid spellId, bool remove)
	{
		Name = name;
		SpellId = spellId;
		Remove = remove;
	}
}
