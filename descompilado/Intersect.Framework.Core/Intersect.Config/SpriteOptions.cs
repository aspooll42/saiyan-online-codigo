using Intersect.Framework.Annotations;
using Newtonsoft.Json;

namespace Intersect.Config;

[RequiresRestart]
public class SpriteOptions
{
	public int AttackFrames { get; set; } = 4;

	public int AfkFrames { get; set; } = 4;

	[DoesNotRequireRestart]
	public int AfkFrameDuration { get; set; } = 200;

	[DoesNotRequireRestart]
	public int AfkStartDelay { get; set; } = 20000;

	public int CastFrames { get; set; } = 4;

	public int IdleFrames { get; set; } = 4;

	[DoesNotRequireRestart]
	public int IdleFrameDuration { get; set; } = 200;

	[DoesNotRequireRestart]
	public int IdleStartDelay { get; set; } = 4000;

	public int NormalFrames { get; set; } = 4;

	public int NormalAttackFrame { get; set; } = 3;

	public int NormalBlockFrame { get; set; } = 2;

	public int NormalCastFrame { get; set; } = 2;

	public int NormalChargeFrame { get; set; }

	public int NormalDashFrame { get; set; } = 1;

	[DoesNotRequireRestart]
	public int MovingFrameDuration { get; set; } = 200;

	public int MaxAnimationVariants { get; set; } = 3;

	public int RunFrames { get; set; } = 4;

	[DoesNotRequireRestart]
	public int RunFrameDuration { get; set; } = 120;

	public int KiFrames { get; set; } = 4;

	[DoesNotRequireRestart]
	public int KiFrameDuration { get; set; } = 100;

	public int BlockFrames { get; set; } = 4;

	[DoesNotRequireRestart]
	public int BlockFrameDuration { get; set; } = 60;

	public int TransformFrames { get; set; } = 6;

	public int ShootFrames { get; set; } = 4;

	public int WeaponFrames { get; set; } = 4;

	[JsonIgnore]
	public int Directions => 4;
}
