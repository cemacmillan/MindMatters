using System;

namespace MindMatters;

[Flags]
public enum DynamicNeedsBitmap : ulong
{
    None = 0,
    SeeArtwork = 1 << 0,
    VerifyStockLevels = 1 << 1,
    NothingOnFloor = 1 << 2,
    FreshFruit = 1 << 3,
    Formality = 1 << 4,
    Constraint = 1 << 5,
    SeeWildAnimals = 1 << 6,
    SeePetAnimals = 1 << 7,
    MountAnimals = 1 << 8,
    CareForAnimals = 1 << 9,
    CareForHumanLike = 1 << 10,
}
