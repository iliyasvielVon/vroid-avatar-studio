using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRM;

namespace AvatarStudio
{
    /// <summary>
    /// Runtime control over the avatar's cloth/hair physics. VRoid VRM 0.x exports VRMSpringBone for
    /// hair and skirt; UniVRM restores them on load and they simulate every frame on their own. This
    /// just collects them all and exposes their live parameters (stiffness / gravity / drag), an
    /// optional wind force, and a one-click body/leg collider pass so the skirt stops clipping through
    /// the legs during big motions.
    /// </summary>
    public class SpringBoneTuner : MonoBehaviour
    {
        VRMSpringBone[] _springs;
        Animator _animator;
        bool _wind;
        bool _collidersAdded;

        public int Count => _springs?.Length ?? 0;
        public bool CollidersAdded => _collidersAdded;

        // Seed values for the UI sliders, read from the model's own export.
        public float Stiffness { get; private set; } = 1.0f;
        public float Gravity { get; private set; }
        public float Drag { get; private set; } = 0.4f;

        public void Bind(GameObject root, Animator animator)
        {
            _animator = animator;
            _springs = root.GetComponentsInChildren<VRMSpringBone>(true);
            if (_springs.Length > 0)
            {
                Stiffness = _springs[0].m_stiffnessForce;
                Gravity = _springs[0].m_gravityPower;
                Drag = _springs[0].m_dragForce;
            }
        }

        public void SetStiffness(float v)
        {
            Stiffness = v;
            foreach (var s in _springs) if (s) s.m_stiffnessForce = v;
        }

        public void SetGravity(float v)
        {
            Gravity = v;
            foreach (var s in _springs) if (s) s.m_gravityPower = v;
        }

        public void SetDrag(float v)
        {
            Drag = v;
            foreach (var s in _springs) if (s) s.m_dragForce = v;
        }

        public void SetWind(bool on)
        {
            _wind = on;
            if (!on)
                foreach (var s in _springs) if (s) s.ExternalForce = Vector3.zero;
        }

        void Update()
        {
            if (!_wind || _springs == null) return;
            // A gusty horizontal breeze in world space. ExternalForce is a VRM-1.0 backport that
            // VRMSpringBone reads live each frame.
            float t = Time.time;
            var f = new Vector3(0.25f + Mathf.Sin(t * 2.1f) * 0.45f, 0f, Mathf.Cos(t * 1.3f) * 0.2f);
            foreach (var s in _springs) if (s) s.ExternalForce = f;
        }

        /// <summary>Add sphere colliders on hips + legs and feed them to every spring, so the skirt
        /// collides with the body instead of passing through it. Idempotent.</summary>
        public void AddBodyColliders()
        {
            if (_collidersAdded || _animator == null || !_animator.isHuman) return;

            var groups = new List<VRMSpringBoneColliderGroup>();
            AddCapsuleAsSpheres(HumanBodyBones.Hips, 0.11f, groups);
            AddCapsuleAsSpheres(HumanBodyBones.LeftUpperLeg, 0.08f, groups);
            AddCapsuleAsSpheres(HumanBodyBones.RightUpperLeg, 0.08f, groups);
            AddCapsuleAsSpheres(HumanBodyBones.LeftLowerLeg, 0.06f, groups);
            AddCapsuleAsSpheres(HumanBodyBones.RightLowerLeg, 0.06f, groups);
            if (groups.Count == 0) return;

            foreach (var s in _springs)
            {
                if (!s) continue;
                var merged = (s.ColliderGroups ?? new VRMSpringBoneColliderGroup[0]).ToList();
                merged.AddRange(groups);
                s.ColliderGroups = merged.ToArray();
                s.Setup(true); // re-init so the new colliders take effect immediately
            }
            _collidersAdded = true;
        }

        void AddCapsuleAsSpheres(HumanBodyBones bone, float radius, List<VRMSpringBoneColliderGroup> outGroups)
        {
            var tf = _animator.GetBoneTransform(bone);
            if (tf == null) return;
            var group = tf.gameObject.GetComponent<VRMSpringBoneColliderGroup>()
                        ?? tf.gameObject.AddComponent<VRMSpringBoneColliderGroup>();
            // Two spheres down the bone approximate a capsule covering the limb segment.
            group.Colliders = new[]
            {
                new VRMSpringBoneColliderGroup.SphereCollider { Offset = Vector3.zero, Radius = radius },
                new VRMSpringBoneColliderGroup.SphereCollider { Offset = new Vector3(0f, -0.18f, 0f), Radius = radius * 0.9f },
            };
            outGroups.Add(group);
        }
    }
}
