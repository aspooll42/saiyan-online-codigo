namespace Intersect.GameObjects;

public sealed record TeachingNode(string Id, TeachingMaster Master, int Tier, int MaxRank, int Cost, double PerRank, bool Implemented);
