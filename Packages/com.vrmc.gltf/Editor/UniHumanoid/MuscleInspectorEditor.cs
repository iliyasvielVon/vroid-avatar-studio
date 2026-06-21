using UnityEditor;

namespace UniHumanoid
{
    // The original muscle TreeView inspector relied on UnityEditor.IMGUI.Controls.TreeView,
    // which Unity 6000 marks obsolete-as-error (CS0619). It is an editor-only debug tool and
    // is not required by the runtime VRM pipeline, so it is reduced to the default inspector
    // to keep the project compiling on Unity 6.5.
    [CustomEditor(typeof(MuscleInspector))]
    public class MuscleInspectorEditor : Editor
    {
    }
}
