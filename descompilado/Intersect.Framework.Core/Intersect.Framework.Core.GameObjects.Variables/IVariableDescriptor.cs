using Intersect.Enums;
using Intersect.Models;

namespace Intersect.Framework.Core.GameObjects.Variables;

public interface IVariableDescriptor : IDatabaseObject, INamedObject, IObject, IFolderable
{
	VariableDataType DataType { get; set; }

	string TextId { get; set; }
}
