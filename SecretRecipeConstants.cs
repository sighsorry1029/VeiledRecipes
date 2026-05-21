namespace SecretRecipes;

internal static class SecretRecipeConstants
{
    internal const string StationInteractionPrefix = "SecretRecipes.InteractedStation.";
    internal const string UnknownNameFallback = "???";
    internal const string UnknownDescriptionFallback = "Not enough info";
    internal const string UnknownRequirementFallback = "?";
    internal const string NewRecipeMessage = "$msg_newrecipe";
    internal const string NewPieceMessage = "$msg_newpiece";
    internal const string NewDishMessage = "$msg_newdish";
    internal const string SkillUpMessagePrefix = "$msg_skillup ";
    internal const string MissingRequirementMessage = "$msg_missingrequirement";
    internal const string MenuNoneMessage = "$menu_none";
    internal const string CloneSuffix = "(Clone)";
    internal const int PieceCategoryBucketCount = 8;
    internal const string RecipeIconChild = "icon";
    internal const string RecipeNameChild = "name";
    internal const string DurabilityChild = "Durability";
    internal const string QualityLevelChild = "QualityLevel";
    internal const string RequirementIconChild = "res_icon";
    internal const string RequirementNameChild = "res_name";
    internal const string RequirementAmountChild = "res_amount";
    internal static readonly char[] PrefabBlacklistSeparators = [',', ';', '|', '\n', '\r'];
}
