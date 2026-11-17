// CharacterAnimation.cs
// Karakter animasyon denetleyicilerini kurar (Assets/_Game/Animation):
// - Character.controller: "Speed" değerine göre Idle → Walk → Run arasında yumuşak geçen tek bir karışım (blend tree).
// - Her karakter için bir "override": aynı denetleyici, o karakterin kendi Idle/Walk/Run klipleriyle.
// - Yüzen/dokunaç düşmanlar için tek klipli basit denetleyici.
// Taşıma animasyonu pakette olmadığı için taşıma pozu kodla yapılır (CharacterVisual).

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class CharacterAnimation
    {
        public const string Folder = BuilderUtil.Root + "/Animation";
        public const string BaseControllerPath = Folder + "/Character.controller";
        public const float WalkThreshold = 0.4f; // Speed (hız / koşu hızı) bu değerde tam yürüme

        public static AnimationClip FindClip(string fbxPath, string shortName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")
                    && ThirdPartyImportProcessor.ShortName(clip.name) == shortName)
                {
                    return clip;
                }
            }
            return null;
        }

        // Ortak denetleyici; klipler kaynak karakterden (oyuncu modeli) alınır.
        public static AnimatorController GetBaseController(string sourceFbx)
        {
            BuilderUtil.EnsureFolder(Folder);
            AnimatorController existing = BuilderUtil.Load<AnimatorController>(BaseControllerPath);
            if (existing != null)
            {
                EnsureCheer(existing, sourceFbx);
                return existing;
            }

            AnimationClip idle = FindClip(sourceFbx, "Idle");
            AnimationClip walk = FindClip(sourceFbx, "Walk");
            AnimationClip run = FindClip(sourceFbx, "Run");
            if (idle == null || walk == null || run == null)
            {
                Debug.LogWarning("Kayıp Ada: Karakter animasyonları bulunamadı (" + sourceFbx + "). Modeller içe aktarıldı mı?");
                return null;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BaseControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, WalkThreshold);
            tree.AddChild(run, 1f);
            EnsureCheer(controller, sourceFbx);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // Sevinç (final töreni): "Cheer" açıkken CheerKind'e göre "Yes" / "Wave" / "Jump" klibi döngüde çalar.
        // Eski denetleyiciye de eklenir.
        private static void EnsureCheer(AnimatorController controller, string sourceFbx)
        {
            EnsureCheerBase(controller, sourceFbx);
            EnsureCheerKinds(controller, sourceFbx);
        }

        private static void EnsureCheerKinds(AnimatorController controller, string sourceFbx)
        {
            foreach (AnimatorControllerParameter p in controller.parameters) if (p.name == "CheerKind") return;
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState move = sm.defaultState, yes = null;
            foreach (ChildAnimatorState c in sm.states) if (c.state.name == "Cheer") yes = c.state;
            if (move == null || yes == null) return;
            controller.AddParameter("CheerKind", AnimatorControllerParameterType.Int);
            foreach (AnimatorStateTransition t in move.transitions)
                if (t.destinationState == yes) t.AddCondition(AnimatorConditionMode.Equals, 0f, "CheerKind");
            string[] clips = { "Wave", "Jump" };
            for (int k = 0; k < clips.Length; k++)
            {
                AnimationClip clip = FindClip(sourceFbx, clips[k]);
                if (clip == null) continue;
                AnimatorState state = sm.AddState("Cheer" + clips[k]);
                state.motion = clip;
                AnimatorStateTransition to = move.AddTransition(state);
                to.AddCondition(AnimatorConditionMode.If, 0f, "Cheer");
                to.AddCondition(AnimatorConditionMode.Equals, k + 1, "CheerKind");
                to.duration = 0.15f;
                to.hasExitTime = false;
                AnimatorStateTransition back = state.AddTransition(move);
                back.AddCondition(AnimatorConditionMode.IfNot, 0f, "Cheer");
                back.duration = 0.15f;
                back.hasExitTime = false;
            }
            EditorUtility.SetDirty(controller);
        }

        private static void EnsureCheerBase(AnimatorController controller, string sourceFbx)
        {
            foreach (AnimatorControllerParameter p in controller.parameters) if (p.name == "Cheer") return;
            AnimationClip cheer = FindClip(sourceFbx, "Yes") ?? FindClip(sourceFbx, "Wave");
            if (cheer == null) return;
            controller.AddParameter("Cheer", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState move = sm.defaultState;
            AnimatorState cheerState = sm.AddState("Cheer");
            cheerState.motion = cheer;
            AnimatorStateTransition to = move.AddTransition(cheerState);
            to.AddCondition(AnimatorConditionMode.If, 0f, "Cheer");
            to.duration = 0.15f;
            to.hasExitTime = false;
            AnimatorStateTransition back = cheerState.AddTransition(move);
            back.AddCondition(AnimatorConditionMode.IfNot, 0f, "Cheer");
            back.duration = 0.15f;
            back.hasExitTime = false;
            EditorUtility.SetDirty(controller);
        }

        // Bu karakterin kendi klipleriyle ortak denetleyici.
        public static RuntimeAnimatorController GetOverride(AnimatorController baseController, string fbxPath, string key)
        {
            if (baseController == null) return null;
            string path = Folder + "/" + key + ".overrideController";

            AnimatorOverrideController controller = BuilderUtil.Load<AnimatorOverrideController>(path);
            if (controller == null)
            {
                controller = new AnimatorOverrideController(baseController);
                AssetDatabase.CreateAsset(controller, path);
            }
            controller.runtimeAnimatorController = baseController;

            List<KeyValuePair<AnimationClip, AnimationClip>> pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(pairs);
            for (int i = 0; i < pairs.Count; i++)
            {
                AnimationClip own = FindClip(fbxPath, ThirdPartyImportProcessor.ShortName(pairs[i].Key.name));
                pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, own);
            }
            controller.ApplyOverrides(pairs);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // Düşman denetleyicisi: Speed ile bekleme/yürüme, "Attack" açıkken saldırı (döngü), "Die" ile ölme.
        public static RuntimeAnimatorController GetEnemyController(string fbxPath, string key)
        {
            BuilderUtil.EnsureFolder(Folder);
            string path = Folder + "/" + key + ".controller";
            AnimatorController existing = BuilderUtil.Load<AnimatorController>(path);
            if (existing != null) return existing;

            AnimationClip idle = FindClip(fbxPath, "Idle");
            AnimationClip walk = FindClip(fbxPath, "Walk");
            AnimationClip attack = FindClip(fbxPath, "Sword") ?? FindClip(fbxPath, "Punch");
            AnimationClip death = FindClip(fbxPath, "Death");
            if (idle == null || walk == null) return null;

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            AnimatorState move = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 0.5f);
            sm.defaultState = move;

            if (attack != null)
            {
                AnimatorState attackState = sm.AddState("Attack");
                attackState.motion = attack;
                AnimatorStateTransition toAttack = move.AddTransition(attackState);
                toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
                toAttack.duration = 0.1f;
                toAttack.hasExitTime = false;
                AnimatorStateTransition back = attackState.AddTransition(move);
                back.AddCondition(AnimatorConditionMode.IfNot, 0f, "Attack");
                back.duration = 0.1f;
                back.hasExitTime = false;
            }
            if (death != null)
            {
                AnimatorState deathState = sm.AddState("Death");
                deathState.motion = death;
                AnimatorStateTransition toDeath = sm.AddAnyStateTransition(deathState);
                toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Die");
                toDeath.duration = 0.1f;
                toDeath.canTransitionToSelf = false;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // Tek klip çalan denetleyici (köpek balığı yüzme, dokunaç bekleme).
        public static RuntimeAnimatorController GetSingleClip(string fbxPath, string clipName, string key)
        {
            BuilderUtil.EnsureFolder(Folder);
            string path = Folder + "/" + key + ".controller";
            AnimatorController existing = BuilderUtil.Load<AnimatorController>(path);
            if (existing != null) return existing;

            AnimationClip clip = FindClip(fbxPath, clipName);
            if (clip == null) return null;
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorState state = controller.layers[0].stateMachine.AddState(clipName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }
    }
}
