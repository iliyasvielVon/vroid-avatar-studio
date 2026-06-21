using UnityEngine;
using UnityEngine.EventSystems;

namespace AvatarStudio
{
    /// <summary>Small helper so 3D drag input ignores clicks that land on UI.</summary>
    public static class UiPointer
    {
        public static bool IsOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
