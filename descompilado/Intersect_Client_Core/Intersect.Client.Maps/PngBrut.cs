using System;
using System.IO;
using System.IO.Compression;

namespace Intersect.Client.Maps;

internal static class PngBrut
{
	private static ReadOnlySpan<byte> Signature => new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 };

	public static (byte[] Rgba, int L, int H)? Lire(Stream flux)
	{
		using MemoryStream memoryStream = new MemoryStream();
		flux.CopyTo(memoryStream);
		byte[] buffer = memoryStream.GetBuffer();
		int num = (int)memoryStream.Length;
		if (num < 8 || !buffer.AsSpan(0, 8).SequenceEqual(Signature))
		{
			return null;
		}
		int num2 = 0;
		int num3 = 0;
		byte b = 0;
		byte b2 = 0;
		byte b3 = 0;
		byte[] array = null;
		byte[] array2 = null;
		using MemoryStream memoryStream2 = new MemoryStream();
		int num4 = 8;
		while (num4 + 12 <= num)
		{
			int num5 = (buffer[num4] << 24) | (buffer[num4 + 1] << 16) | (buffer[num4 + 2] << 8) | buffer[num4 + 3];
			int num6 = num4 + 8;
			if (num5 < 0 || num6 + num5 + 4 > num)
			{
				return null;
			}
			string text = string.Concat(new ReadOnlySpan<char>((char)buffer[num4 + 4]), new ReadOnlySpan<char>((char)buffer[num4 + 5]), new ReadOnlySpan<char>((char)buffer[num4 + 6]), new ReadOnlySpan<char>((char)buffer[num4 + 7]));
			switch (text)
			{
			case "IHDR":
				if (num5 >= 13)
				{
					num2 = (buffer[num6] << 24) | (buffer[num6 + 1] << 16) | (buffer[num6 + 2] << 8) | buffer[num6 + 3];
					num3 = (buffer[num6 + 4] << 24) | (buffer[num6 + 5] << 16) | (buffer[num6 + 6] << 8) | buffer[num6 + 7];
					b = buffer[num6 + 8];
					b2 = buffer[num6 + 9];
					b3 = buffer[num6 + 12];
				}
				break;
			case "PLTE":
				array = buffer.AsSpan(num6, num5).ToArray();
				break;
			case "tRNS":
				array2 = buffer.AsSpan(num6, num5).ToArray();
				break;
			case "IDAT":
				memoryStream2.Write(buffer, num6, num5);
				break;
			}
			if (text == "IEND")
			{
				break;
			}
			num4 = num6 + num5 + 4;
		}
		int num7 = b2 switch
		{
			0 => 1, 
			2 => 3, 
			3 => 1, 
			4 => 2, 
			6 => 4, 
			_ => 0, 
		};
		bool flag = num2 <= 0 || num3 <= 0 || num2 > 16384 || num3 > 16384 || b3 != 0 || num7 == 0;
		bool flag2 = flag;
		if (!flag2)
		{
			bool flag3;
			switch (b)
			{
			case 1:
			case 2:
			case 4:
			case 8:
			case 16:
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = !flag3;
		}
		bool flag4 = flag2;
		if (!flag4)
		{
			bool flag3;
			switch (b2)
			{
			case 2:
			case 4:
			case 6:
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag4 = flag3 && b < 8;
		}
		if (flag4 || (b2 == 3 && (array == null || b == 16)))
		{
			return null;
		}
		int num8 = num7 * b;
		int num9 = (num2 * num8 + 7) / 8;
		int bpp = Math.Max(1, num8 / 8);
		byte[] array3 = new byte[(long)num3 * (long)(num9 + 1)];
		memoryStream2.Position = 0L;
		using (ZLibStream zLibStream = new ZLibStream(memoryStream2, CompressionMode.Decompress))
		{
			int num10;
			for (int i = 0; i < array3.Length; i += num10)
			{
				num10 = zLibStream.Read(array3, i, array3.Length - i);
				if (num10 <= 0)
				{
					return null;
				}
			}
		}
		byte[] array4 = new byte[(long)num2 * (long)num3 * 4];
		byte[] array5 = new byte[num9];
		byte[] array6 = new byte[num9];
		int num11 = (1 << (int)b) - 1;
		for (int j = 0; j < num3; j++)
		{
			int num12 = j * (num9 + 1);
			byte filtre = array3[num12];
			Buffer.BlockCopy(array3, num12 + 1, array6, 0, num9);
			if (!Defiltrer(filtre, array6, array5, bpp))
			{
				return null;
			}
			int num13 = j * num2 * 4;
			for (int k = 0; k < num2; k++)
			{
				byte b4 = byte.MaxValue;
				byte b5;
				byte b6;
				byte b7;
				switch (b2)
				{
				case 6:
					b5 = Octet(array6, k, 0, 4, b);
					b6 = Octet(array6, k, 1, 4, b);
					b7 = Octet(array6, k, 2, 4, b);
					b4 = Octet(array6, k, 3, 4, b);
					break;
				case 2:
					b5 = Octet(array6, k, 0, 3, b);
					b6 = Octet(array6, k, 1, 3, b);
					b7 = Octet(array6, k, 2, 3, b);
					if (array2 != null && array2.Length >= 6 && Brut16(array6, k, 0, 3, b) == ((array2[0] << 8) | array2[1]) && Brut16(array6, k, 1, 3, b) == ((array2[2] << 8) | array2[3]) && Brut16(array6, k, 2, 3, b) == ((array2[4] << 8) | array2[5]))
					{
						b4 = 0;
					}
					break;
				case 4:
					b5 = (b6 = (b7 = Octet(array6, k, 0, 2, b)));
					b4 = Octet(array6, k, 1, 2, b);
					break;
				case 0:
				{
					int num15 = Echantillon(array6, k, b);
					b5 = (b6 = (b7 = ((b == 16) ? ((byte)(num15 >> 8)) : ((byte)(num15 * 255 / num11)))));
					if (array2 != null && array2.Length >= 2 && num15 == ((array2[0] << 8) | array2[1]))
					{
						b4 = 0;
					}
					break;
				}
				default:
				{
					int num14 = Echantillon(array6, k, b);
					if (num14 * 3 + 2 >= array.Length)
					{
						b5 = (b6 = (b7 = 0));
						b4 = 0;
						break;
					}
					b5 = array[num14 * 3];
					b6 = array[num14 * 3 + 1];
					b7 = array[num14 * 3 + 2];
					b4 = ((array2 != null && num14 < array2.Length) ? array2[num14] : byte.MaxValue);
					break;
				}
				}
				if (b4 == 0)
				{
					b5 = (b6 = (b7 = 0));
				}
				array4[num13 + k * 4] = b5;
				array4[num13 + k * 4 + 1] = b6;
				array4[num13 + k * 4 + 2] = b7;
				array4[num13 + k * 4 + 3] = b4;
			}
			byte[] array7 = array6;
			array6 = array5;
			array5 = array7;
		}
		return (array4, num2, num3);
	}

	private static bool Defiltrer(byte filtre, byte[] ligne, byte[] precedente, int bpp)
	{
		switch (filtre)
		{
		case 0:
			return true;
		case 1:
		{
			for (int l = bpp; l < ligne.Length; l++)
			{
				ligne[l] += ligne[l - bpp];
			}
			return true;
		}
		case 2:
		{
			for (int j = 0; j < ligne.Length; j++)
			{
				ligne[j] += precedente[j];
			}
			return true;
		}
		case 3:
		{
			for (int k = 0; k < ligne.Length; k++)
			{
				int num8 = ((k >= bpp) ? ligne[k - bpp] : 0);
				ligne[k] = (byte)(ligne[k] + (num8 + precedente[k] >> 1));
			}
			return true;
		}
		case 4:
		{
			for (int i = 0; i < ligne.Length; i++)
			{
				int num = ((i >= bpp) ? ligne[i - bpp] : 0);
				byte b = precedente[i];
				int num2 = ((i >= bpp) ? precedente[i - bpp] : 0);
				int num3 = num + b - num2;
				int num4 = Math.Abs(num3 - num);
				int num5 = Math.Abs(num3 - b);
				int num6 = Math.Abs(num3 - num2);
				int num7 = ((num4 <= num5 && num4 <= num6) ? num : ((num5 <= num6) ? b : num2));
				ligne[i] = (byte)(ligne[i] + num7);
			}
			return true;
		}
		default:
			return false;
		}
	}

	private static byte Octet(byte[] ligne, int x, int canal, int canaux, int profondeur)
	{
		if (profondeur != 16)
		{
			return ligne[x * canaux + canal];
		}
		return ligne[(x * canaux + canal) * 2];
	}

	private static int Brut16(byte[] ligne, int x, int canal, int canaux, int profondeur)
	{
		if (profondeur != 16)
		{
			return ligne[x * canaux + canal];
		}
		return (ligne[(x * canaux + canal) * 2] << 8) | ligne[(x * canaux + canal) * 2 + 1];
	}

	private static int Echantillon(byte[] ligne, int x, int profondeur)
	{
		switch (profondeur)
		{
		case 8:
			return ligne[x];
		case 16:
			return (ligne[x * 2] << 8) | ligne[x * 2 + 1];
		default:
		{
			int num = x * profondeur;
			int num2 = 8 - profondeur - num % 8;
			return (ligne[num / 8] >> num2) & ((1 << profondeur) - 1);
		}
		}
	}
}
