using UnityEditor;
public static class TreasureRunnerCampaignTests
{
    // Preserve the familiar menu entry while running tests for the current layout and hit rules.
    [MenuItem("TreasureRunner/Kampagne vollständig testen")]
    public static void Run()=>TreasureRunnerAdventureTests.Run();
}
