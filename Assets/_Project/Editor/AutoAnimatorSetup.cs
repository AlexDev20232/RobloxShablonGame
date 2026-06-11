using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoAnimatorSetup
{
    private const string ControllerPath = "Assets/_Project/Art/Characters/Animations/CharacterController.controller";
    private const string IdleClipPath = "Assets/_Project/Art/Characters/Animations/Idle.anim";
    private const string RunClipPath = "Assets/_Project/Art/Characters/Animations/Run.anim";
    private const string PlayerName = "bacon@T-Pose (1)";
    private const string ModelPath = "Assets/_Project/Art/Characters/Models/bacon@T-Pose (1).fbx";

    static AutoAnimatorSetup()
    {
        EditorApplication.delayCall += TrySetup;
    }

    private static void TrySetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        var controller = EnsureController();
        if (controller == null)
        {
            return;
        }

        AssignControllerToPlayer(controller);
    }

    private static AnimatorController EnsureController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        if (ControllerLooksValid(controller))
        {
            return controller;
        }

        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        var runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunClipPath);
        if (idleClip == null || runClip == null)
        {
            Debug.LogError("[AutoAnimatorSetup] Missing Idle.anim or Run.anim. Controller not built.");
            return controller;
        }

        BuildController(controller, idleClip, runClip);
        return controller;
    }

    private static bool ControllerLooksValid(AnimatorController controller)
    {
        if (controller == null || controller.layers.Length == 0)
        {
            return false;
        }

        if (!HasParameter(controller, "Speed") || !HasParameter(controller, "RunSpeedMultiplier"))
        {
            return false;
        }

        var sm = controller.layers[0].stateMachine;
        if (sm == null)
        {
            return false;
        }

        bool hasIdle = false;
        bool hasRun = false;
        foreach (var child in sm.states)
        {
            if (child.state == null)
            {
                continue;
            }

            if (child.state.name == "Idle")
            {
                hasIdle = true;
            }
            else if (child.state.name == "Run")
            {
                hasRun = true;
            }
        }

        return hasIdle && hasRun;
    }

    private static bool HasParameter(AnimatorController controller, string name)
    {
        foreach (var parameter in controller.parameters)
        {
            if (parameter.name == name)
            {
                return true;
            }
        }

        return false;
    }

    private static void BuildController(AnimatorController controller, AnimationClip idleClip, AnimationClip runClip)
    {
        controller.parameters = new AnimatorControllerParameter[0];
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("RunSpeedMultiplier", AnimatorControllerParameterType.Float);

        var layer = controller.layers[0];
        var sm = layer.stateMachine;

        sm.states = new ChildAnimatorState[0];
        sm.anyStateTransitions = new AnimatorStateTransition[0];
        sm.entryTransitions = new AnimatorTransition[0];
        sm.stateMachines = new ChildAnimatorStateMachine[0];

        var idleState = sm.AddState("Idle", new Vector3(200f, 80f, 0f));
        idleState.motion = idleClip;

        var runState = sm.AddState("Run", new Vector3(200f, 200f, 0f));
        runState.motion = runClip;
        runState.speedParameterActive = true;
        runState.speedParameter = "RunSpeedMultiplier";

        sm.defaultState = idleState;

        var toRun = idleState.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.hasFixedDuration = true;
        toRun.duration = 0.1f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = runState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.hasFixedDuration = true;
        toIdle.duration = 0.1f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static void AssignControllerToPlayer(AnimatorController controller)
    {
        bool found = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            var player = FindByName(scene.GetRootGameObjects(), PlayerName);
            if (player == null)
            {
                continue;
            }

            found = true;
            var animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                animator = player.AddComponent<Animator>();
                var avatar = LoadAvatar();
                if (avatar != null)
                {
                    animator.avatar = avatar;
                }
            }

            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                EditorUtility.SetDirty(animator);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[AutoAnimatorSetup] Player '{PlayerName}' not found in any loaded scene.");
        }
    }

    private static Avatar LoadAvatar()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        foreach (var asset in assets)
        {
            if (asset is Avatar avatar)
            {
                return avatar;
            }
        }

        return null;
    }

    private static GameObject FindByName(GameObject[] roots, string name)
    {
        foreach (var root in roots)
        {
            var found = FindInChildren(root.transform, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindInChildren(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent.gameObject;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindInChildren(parent.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
