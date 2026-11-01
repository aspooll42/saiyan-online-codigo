using System.Buffers;
using Ceras;

namespace Intersect.Network;

public sealed class CerasDefaultBufferPool : ICerasBufferPool
{
	public byte[] RentBuffer(int minimumSize)
	{
		return ArrayPool<byte>.Shared.Rent(minimumSize);
	}

	public void Return(byte[] buffer)
	{
		ArrayPool<byte>.Shared.Return(buffer);
	}
}
