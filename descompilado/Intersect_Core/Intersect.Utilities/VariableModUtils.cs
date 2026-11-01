using System.Collections.Immutable;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Utilities;

public static class VariableModUtils
{
	public static readonly ImmutableArray<VariableModType> SetMods = ImmutableArray.Create<VariableModType>(VariableModType.Set, VariableModType.DupPlayerVar, VariableModType.DupGlobalVar, VariableModType.DupGuildVar, VariableModType.DuplicateUserVariable);

	public static readonly ImmutableArray<VariableModType> AddMods = ImmutableArray.Create<VariableModType>(VariableModType.Add, VariableModType.AddPlayerVar, VariableModType.AddGlobalVar, VariableModType.AddGuildVar, VariableModType.AddUserVariable);

	public static readonly ImmutableArray<VariableModType> SubMods = ImmutableArray.Create<VariableModType>(VariableModType.Subtract, VariableModType.SubtractPlayerVar, VariableModType.SubtractGlobalVar, VariableModType.SubtractGuildVar, VariableModType.SubtractUserVariable);

	public static readonly ImmutableArray<VariableModType> MultMods = ImmutableArray.Create<VariableModType>(VariableModType.Multiply, VariableModType.MultiplyPlayerVar, VariableModType.MultiplyGlobalVar, VariableModType.MultiplyGuildVar, VariableModType.MultiplyUserVariable);

	public static readonly ImmutableArray<VariableModType> DivideMods = ImmutableArray.Create<VariableModType>(VariableModType.Divide, VariableModType.DividePlayerVar, VariableModType.DivideGlobalVar, VariableModType.DivideGuildVar, VariableModType.DivideUserVariable);

	public static readonly ImmutableArray<VariableModType> LShiftMods = ImmutableArray.Create<VariableModType>(VariableModType.LeftShift, VariableModType.LeftShiftPlayerVar, VariableModType.LeftShiftGlobalVar, VariableModType.LeftShiftGuildVar, VariableModType.LeftShiftUserVariable);

	public static readonly ImmutableArray<VariableModType> RShiftMods = ImmutableArray.Create<VariableModType>(VariableModType.RightShift, VariableModType.RightShiftPlayerVar, VariableModType.RightShiftGlobalVar, VariableModType.RightShiftGuildVar, VariableModType.RightShiftUserVariable);
}
