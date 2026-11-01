namespace Intersect.Configuration;

public interface IConfigurable<out TConfiguration> where TConfiguration : IConfiguration<TConfiguration>
{
	TConfiguration Configuration { get; }
}
