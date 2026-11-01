namespace Intersect.Configuration;

public interface IConfiguration<out TConfiguration>
{
	TConfiguration Load(string filePath, bool failQuietly = false);

	TConfiguration Save(string filePath, bool failQuietly = false);
}
