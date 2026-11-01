using System;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class CastSpellOn : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.CastSpellOn;

	public Guid SpellId { get; set; }

	public bool Self { get; set; }

	public bool PartyMembers { get; set; }

	public bool GuildMembers { get; set; }
}
