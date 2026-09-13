using UnityEditor;

[InitializeOnLoad]
internal static class KnightAttackControllerUpgrade
{
    static KnightAttackControllerUpgrade()
    {
        EditorApplication.delayCall += KnightAnimatorSetup.RunSetupNow;
    }
}
