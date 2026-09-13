using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
internal static class KnightAnimatorSetup
{
    private const string ControllerFolder = "Assets/Resources";
    private const string ControllerPath = ControllerFolder + "/KnightLocomotion.controller";

    static KnightAnimatorSetup()
    {
        EditorApplication.delayCall += EnsureControllerExists;
    }

    internal static void RunSetupNow()
    {
        EnsureControllerExists();
    }

    private static void EnsureControllerExists()
    {
        AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existingController != null)
        {
            EnsureAttackState(existingController);
            return;
        }

        AnimationClip idle = FindClip("Idle_A");
        AnimationClip run = FindClip("Running_A");
        AnimationClip cheer = FindClip("Cheering");
        if (idle == null || run == null || cheer == null)
        {
            Debug.LogWarning("Knight animation setup is waiting for KayKit animation clips to finish importing.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(ControllerFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Action", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle");
        AnimatorState runState = stateMachine.AddState("Run");
        AnimatorState actionState = stateMachine.AddState("Cheer");
        idleState.motion = idle;
        runState.motion = run;
        actionState.motion = cheer;
        stateMachine.defaultState = idleState;

        AnimatorStateTransition startRunning = idleState.AddTransition(runState);
        startRunning.hasExitTime = false;
        startRunning.duration = 0.12f;
        startRunning.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition stopRunning = runState.AddTransition(idleState);
        stopRunning.hasExitTime = false;
        stopRunning.duration = 0.12f;
        stopRunning.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        AnimatorStateTransition idleAction = idleState.AddTransition(actionState);
        idleAction.hasExitTime = false;
        idleAction.duration = 0.08f;
        idleAction.AddCondition(AnimatorConditionMode.If, 0f, "Action");

        AnimatorStateTransition runAction = runState.AddTransition(actionState);
        runAction.hasExitTime = false;
        runAction.duration = 0.08f;
        runAction.AddCondition(AnimatorConditionMode.If, 0f, "Action");

        AnimatorStateTransition finishAction = actionState.AddTransition(idleState);
        finishAction.hasExitTime = true;
        finishAction.exitTime = 0.95f;
        finishAction.duration = 0.12f;

        EnsureAttackState(controller);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Created Knight click-to-move animation controller.");
    }

    private static void EnsureAttackState(AnimatorController controller)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == "Attack")
            {
                return;
            }
        }

        AnimationClip attackClip = FindClip("Melee_2H_Attack_Slice");
        if (attackClip == null)
        {
            Debug.LogWarning("Knight setup could not find the two-handed attack animation.");
            return;
        }

        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState attackState = stateMachine.AddState("Attack 2H");
        attackState.motion = attackClip;

        AnimatorStateTransition enterAttack = stateMachine.AddAnyStateTransition(attackState);
        enterAttack.hasExitTime = false;
        enterAttack.duration = 0.06f;
        enterAttack.canTransitionToSelf = false;
        enterAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition finishAttack = attackState.AddTransition(stateMachine.defaultState);
        finishAttack.hasExitTime = true;
        finishAttack.exitTime = 0.92f;
        finishAttack.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Added two-handed attack animation to Knight controller.");
    }

    private static AnimationClip FindClip(string clipName)
    {
        string[] guids = AssetDatabase.FindAssets(clipName + " t:AnimationClip", new[]
        {
            "Assets/KayKit/Characters/Animations/Animations/Rig_Medium"
        });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && clip.name == clipName)
            {
                return clip;
            }
        }

        return null;
    }
}
