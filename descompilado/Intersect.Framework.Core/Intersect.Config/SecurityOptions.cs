using System.Collections.Generic;

namespace Intersect.Config;

public class SecurityOptions
{
	public List<string> IpBlacklist { get; set; } = new List<string>();

	public PacketSecurityOptions Packets { get; set; } = new PacketSecurityOptions();

	public bool CheckIp(string ip)
	{
		ip = ip.Trim();
		string[] array = ip.Split('.');
		if (array.Length != 4)
		{
			return false;
		}
		foreach (string item in IpBlacklist)
		{
			string text = item.Trim();
			string[] array2 = text.Split('.');
			if (array2.Length != 4)
			{
				continue;
			}
			bool flag = true;
			for (int i = 0; i < 4; i++)
			{
				if (array2[i] != "*" && array2[i] != array[i])
				{
					flag = false;
				}
			}
			if (flag)
			{
				return false;
			}
		}
		return true;
	}
}
