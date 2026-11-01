using System.Runtime.Serialization;
using Intersect.Framework.Annotations;

namespace Intersect.Config;

[RequiresRestart]
public class InstancingOptions
{
	public const int DefaultInstanceLives = 3;

	public bool SharedInstanceRespawnInInstance { get; set; } = true;

	public bool RejoinableSharedInstances { get; set; }

	public int MaxSharedInstanceLives { get; set; } = 3;

	public bool BootAllFromInstanceWhenOutOfLives { get; set; } = true;

	public bool LoseExpOnInstanceDeath { get; set; }

	public bool BlockPartyInvitesInInstance { get; set; }

	public bool KickPartyMembersOnKick { get; set; } = true;

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Validate();
	}

	private void Validate()
	{
	}
}
