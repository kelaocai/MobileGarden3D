using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
internal static class PolarBearAnimatorSetup
{
    private const string ControllerPath = "Assets/Resources/PolarBearLocomotion.controller";
    private const string AnimationFolder = "Assets/Teddybear/animations/";

    static PolarBearAnimatorSetup()
    {
        EditorApplication.delayCall += EnsureController;
    }

    [MenuItem("Tools/Mobile Garden/Update Polar Animator")]
    private static void EnsureController()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        AnimationClip idle1 = LoadClip("idle1.FBX");
        AnimationClip idle2 = LoadClip("idle2.FBX");
        AnimationClip idle3 = LoadClip("idle3.FBX");
        AnimationClip walk = LoadClip("walk.FBX");
        AnimationClip attack = LoadClip("clap.FBX");
        if (idle1 == null || walk == null)
        {
            Debug.LogWarning("Polar locomotion controller could not be created because idle1 or walk is missing.");
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null)
        {
            EnsureState(controller.layers[0].stateMachine, "attack", attack);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return;
        }

        controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState defaultState = machine.AddState("idle1");
        defaultState.motion = idle1;
        machine.defaultState = defaultState;

        AnimatorState secondIdle = AddOptionalState(machine, "idle2", idle2);
        AnimatorState thirdIdle = AddOptionalState(machine, "idle3", idle3);
        AnimatorState walkState = machine.AddState("walk");
        walkState.motion = walk;
        AddOptionalState(machine, "attack", attack);

        AddStartWalkingTransition(defaultState, walkState);
        AddStartWalkingTransition(secondIdle, walkState);
        AddStartWalkingTransition(thirdIdle, walkState);

        AnimatorStateTransition stopWalking = walkState.AddTransition(defaultState);
        stopWalking.hasExitTime = false;
        stopWalking.duration = 0.15f;
        stopWalking.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Created PolarBearLocomotion controller.");
    }

    private static void EnsureState(AnimatorStateMachine machine, string stateName, AnimationClip clip)
    {
        if (clip == null) return;
        foreach (ChildAnimatorState child in machine.states)
            if (child.state.name == stateName) return;
        machine.AddState(stateName).motion = clip;
    }

    private static AnimatorState AddOptionalState(
        AnimatorStateMachine machine,
        string stateName,
        AnimationClip clip)
    {
        if (clip == null)
        {
            return null;
        }

        AnimatorState state = machine.AddState(stateName);
        state.motion = clip;
        return state;
    }

    private static void AddStartWalkingTransition(AnimatorState idleState, AnimatorState walkState)
    {
        if (idleState == null)
        {
            return;
        }

        AnimatorStateTransition transition = idleState.AddTransition(walkState);
        transition.hasExitTime = false;
        transition.duration = 0.15f;
        transition.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
    }

    private static AnimationClip LoadClip(string fileName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AnimationFolder + fileName);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }

        return null;
    }
}
