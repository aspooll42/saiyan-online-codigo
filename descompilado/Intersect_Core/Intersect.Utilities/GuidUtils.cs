using System;
using System.Security.Cryptography;
using System.Text;

namespace Intersect.Utilities;

public static class GuidUtils
{
	public static Guid CreateNamed(Guid namespaceId, string name)
	{
		return CreateNamed(namespaceId, Encoding.UTF8.GetBytes(name));
	}

	public static Guid CreateNamed(Guid namespaceId, byte[] name)
	{
		if (name.Length < 1)
		{
			throw new ArgumentOutOfRangeException("name");
		}
		byte[] array = namespaceId.ToByteArray();
		ValueUtils.Swap(ref array[0], ref array[3]);
		ValueUtils.Swap(ref array[2], ref array[1]);
		ValueUtils.Swap(ref array[4], ref array[5]);
		ValueUtils.Swap(ref array[6], ref array[7]);
		byte[] array2 = new byte[array.Length + name.Length];
		Buffer.BlockCopy(array, 0, array2, 0, array.Length);
		Buffer.BlockCopy(name, 0, array2, array.Length, name.Length);
		byte[] src;
		using (SHA1 sHA = SHA1.Create())
		{
			src = sHA?.ComputeHash(array2) ?? throw new InvalidOperationException();
		}
		byte[] array3 = new byte[16];
		Buffer.BlockCopy(src, 0, array3, 0, 16);
		array3[6] &= 15;
		array3[6] |= 80;
		array3[8] &= 63;
		array3[8] |= 128;
		ValueUtils.Swap(ref array3[0], ref array3[3]);
		ValueUtils.Swap(ref array3[2], ref array3[1]);
		ValueUtils.Swap(ref array3[4], ref array3[5]);
		ValueUtils.Swap(ref array3[6], ref array3[7]);
		return new Guid(array3);
	}
}
