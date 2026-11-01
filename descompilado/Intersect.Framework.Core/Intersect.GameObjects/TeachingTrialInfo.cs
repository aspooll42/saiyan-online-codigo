using Intersect.Config;

namespace Intersect.GameObjects;

public sealed record TeachingTrialInfo(string Id, TeachingMaster Master, int Level, string Name, bool Implemented, TechniqueDeMaitre Attaque = TechniqueDeMaitre.Aucune);
