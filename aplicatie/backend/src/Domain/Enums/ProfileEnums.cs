namespace NutriScan.Domain.Enums;

public enum NutritionGoal
{
    LoseWeight = 1,
    MaintainWeight = 2,
    GainMuscle = 3
}

public enum Gender
{
    Male = 1,
    Female = 2
}

public enum ActivityLevel
{
    Sedentary = 1,       // 1.2
    LightlyActive = 2,   // 1.375
    ModeratelyActive = 3,// 1.55
    VeryActive = 4,      // 1.725
    ExtremelyActive = 5  // 1.9
}
