namespace Intersect.Factories;

public interface IFactory<out TValue>
{
	TValue Create(params object[] args);
}
