using System.Collections.Generic;
using UnityEngine;

namespace AvatarStudio
{
    public enum MotionRoutine { Idle, Sway, Bow, Nod, Wave }

    /// <summary>
    /// Drives the Humanoid skeleton directly (no animation clips) for the built-in 程序化 motions.
    /// Runs in the default LateUpdate slot; VRM spring bones (execution order 11000) then run after
    /// this and make hair/skirt sway from whatever pose we leave. Disable this component while a real
    /// Mixamo clip is playing through <see cref="MotionController"/> so the two never fight.
    ///
    /// Every routine only touches spine / chest / head / hips — bones whose Humanoid local axes are
    /// predictable across rigs — so the motion reads correctly on any VRoid model. Big arm-driven
    /// motions (proper dances, gestures) come from Mixamo instead.
    /// </summary>
    public class ProceduralMotion : MonoBehaviour
    {
        public MotionRoutine Routine = MotionRoutine.Idle;

        Transform _spine, _chest, _head, _hips, _lUpArm, _rUpArm, _lLoArm, _rLoArm;
        Quaternion _spine0, _chest0, _head0, _hips0, _lUp0, _rUp0, _lLo0, _rLo0;
        bool _ready;

        // Full humanoid rest pose, restored whenever this driver re-enables — otherwise bones a Mixamo
        // clip moved but our routines don't touch (e.g. the legs after a walk) stay frozen mid-pose.
        Transform[] _allBones;
        Quaternion[] _allRest;

        public void Bind(Animator animator)
        {
            if (animator == null || !animator.isHuman) return;

            var bones = new List<Transform>();
            var rest = new List<Quaternion>();
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var tf = animator.GetBoneTransform((HumanBodyBones)i);
                if (tf != null) { bones.Add(tf); rest.Add(tf.localRotation); }
            }
            _allBones = bones.ToArray();
            _allRest = rest.ToArray();

            _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            _spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            _chest = animator.GetBoneTransform(HumanBodyBones.Chest)
                     ?? animator.GetBoneTransform(HumanBodyBones.UpperChest);
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
            _lUpArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _rUpArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _lLoArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _rLoArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

            if (_hips) _hips0 = _hips.localRotation;
            if (_spine) _spine0 = _spine.localRotation;
            if (_chest) _chest0 = _chest.localRotation;
            if (_head) _head0 = _head.localRotation;
            if (_lUpArm) _lUp0 = _lUpArm.localRotation;
            if (_rUpArm) _rUp0 = _rUpArm.localRotation;
            if (_lLoArm) _lLo0 = _lLoArm.localRotation;
            if (_rLoArm) _rLo0 = _rLoArm.localRotation;
            _ready = true;
        }

        void OnEnable()
        {
            // Re-enabled after a Mixamo clip owned the skeleton — snap everything back to bind pose.
            if (_allBones == null) return;
            for (int i = 0; i < _allBones.Length; i++)
                if (_allBones[i]) _allBones[i].localRotation = _allRest[i];
        }

        void LateUpdate()
        {
            if (!_ready) return;

            // Reset to the captured rest pose every frame, then layer the active routine on top.
            if (_hips) _hips.localRotation = _hips0;
            if (_spine) _spine.localRotation = _spine0;
            if (_chest) _chest.localRotation = _chest0;
            if (_head) _head.localRotation = _head0;
            if (_lUpArm) _lUpArm.localRotation = _lUp0;
            if (_rUpArm) _rUpArm.localRotation = _rUp0;
            if (_lLoArm) _lLoArm.localRotation = _lLo0;
            if (_rLoArm) _rLoArm.localRotation = _rLo0;

            float t = Time.time;
            ApplyBreathing(t);          // always-on subtle life
            switch (Routine)
            {
                case MotionRoutine.Sway: ApplySway(t); break;
                case MotionRoutine.Bow: ApplyBow(t); break;
                case MotionRoutine.Nod: ApplyNod(t); break;
                case MotionRoutine.Wave: ApplyWave(t); break;
            }
        }

        static void Add(Transform b, float x, float y, float z)
        {
            if (b) b.localRotation = b.localRotation * Quaternion.Euler(x, y, z);
        }

        void ApplyBreathing(float t)
        {
            float breath = Mathf.Sin(t * 1.1f);
            float sway = Mathf.Sin(t * 0.5f);
            Add(_spine, breath * 0.4f, sway * 0.5f, 0f);
            Add(_chest, breath * 0.6f, 0f, 0f);
            Add(_head, sway * 1.0f, sway * 1.3f, 0f);
            // keep arms relaxed slightly out from the body so the silhouette reads
            Add(_lUpArm, 0f, 0f, 6f + sway);
            Add(_rUpArm, 0f, 0f, -6f - sway);
        }

        // 扭胯摇摆：hips and spine counter-rotate; clearly throws the skirt around.
        void ApplySway(float t)
        {
            float s = Mathf.Sin(t * 1.6f);
            float s2 = Mathf.Sin(t * 1.6f + 0.6f);
            Add(_hips, 0f, s * 7f, s * 5f);
            Add(_spine, 0f, -s2 * 6f, -s2 * 3f);
            Add(_chest, 0f, s * 4f, 0f);
            Add(_head, 0f, s2 * 4f, 0f);
        }

        // 鞠躬：bend forward at spine/chest with a slow bob, head follows.
        void ApplyBow(float t)
        {
            float b = (Mathf.Sin(t * 1.2f - Mathf.PI * 0.5f) * 0.5f + 0.5f); // 0..1 ease
            Add(_spine, b * 22f, 0f, 0f);
            Add(_chest, b * 12f, 0f, 0f);
            Add(_head, b * 10f, 0f, 0f);
        }

        // 点头致意：gentle repeated head nod.
        void ApplyNod(float t)
        {
            float n = Mathf.Sin(t * 3.2f);
            Add(_head, n * 9f + 4f, 0f, 0f);
            Add(_chest, n * 2f, 0f, 0f);
        }

        // 挥手：right forearm oscillates (rig-axis tolerant — small bend on the lower arm).
        void ApplyWave(float t)
        {
            float w = Mathf.Sin(t * 7f);
            Add(_rUpArm, 0f, 0f, -55f);    // lift the right arm out to the side
            Add(_rLoArm, 0f, w * 22f, w * 18f); // wave the hand
            Add(_chest, 0f, -6f, 0f);
        }
    }
}
