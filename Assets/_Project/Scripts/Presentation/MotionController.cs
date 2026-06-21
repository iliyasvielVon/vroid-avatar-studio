using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AvatarStudio
{
    /// <summary>
    /// Plays motions on the avatar: either a built-in <see cref="ProceduralMotion"/> routine, or a
    /// real Humanoid <see cref="AnimationClip"/> (e.g. a Mixamo download) retargeted onto the VRoid
    /// skeleton via a PlayableGraph — no RuntimeAnimatorController asset required.
    ///
    /// Clips are discovered at runtime from Resources/Motions (drop Mixamo .fbx there; the editor
    /// importer sets them to Humanoid + looping). Switching to a clip disables ProceduralMotion so the
    /// two never write the same bones in the same frame; switching back to a routine re-enables it.
    /// VRM spring bones run after both (execution order 11000) so hair/skirt always sway.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class MotionController : MonoBehaviour
    {
        Animator _animator;
        ProceduralMotion _procedural;

        PlayableGraph _graph;
        AnimationClipPlayable _clipPlayable;
        AnimationClip _clip;
        bool _graphValid;
        float _clipStart;

        public readonly List<AnimationClip> Clips = new();
        public string ActiveLabel { get; private set; } = "待机";

        public void Bind(Animator animator, ProceduralMotion procedural)
        {
            _animator = animator;
            _procedural = procedural;
            LoadClips();
            PlayProcedural(MotionRoutine.Idle, "待机");
        }

        void LoadClips()
        {
            Clips.Clear();
            // Each Humanoid .fbx under a Resources/Motions folder contributes its AnimationClip(s).
            foreach (var c in Resources.LoadAll<AnimationClip>("Motions"))
            {
                if (c != null && !c.name.StartsWith("__preview")) Clips.Add(c);
            }
        }

        public void PlayProcedural(MotionRoutine routine, string label)
        {
            StopGraph();
            if (_procedural)
            {
                _procedural.enabled = true;
                _procedural.Routine = routine;
            }
            ActiveLabel = label;
        }

        public void PlayClip(AnimationClip clip)
        {
            if (clip == null || _animator == null) return;
            StopGraph();
            if (_procedural) _procedural.enabled = false; // let the graph own the skeleton
            _animator.applyRootMotion = false;            // keep the character centered on the turntable

            _graph = PlayableGraph.Create($"Motion::{clip.name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
            _clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            _clipPlayable.SetApplyFootIK(false);
            output.SetSourcePlayable(_clipPlayable);

            _clip = clip;
            _clipStart = Time.time;
            _graphValid = true;
            _graph.Play();
            ActiveLabel = clip.name;
        }

        void Update()
        {
            // Drive time ourselves so the clip loops regardless of its import loop flag.
            if (_graphValid && _clip != null && _clip.length > 0f)
                ((Playable)_clipPlayable).SetTime((Time.time - _clipStart) % _clip.length);
        }

        void StopGraph()
        {
            if (_graphValid)
            {
                if (_graph.IsValid()) _graph.Destroy();
                _graphValid = false;
                _clip = null;
            }
        }

        void OnDestroy() => StopGraph();
    }
}
