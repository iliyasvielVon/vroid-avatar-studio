using UnityEngine;

namespace AvatarStudio
{
    /// <summary>Drag with the left mouse button to spin the avatar; optional slow auto-rotate.</summary>
    public class TurntableController : MonoBehaviour
    {
        public float DragSpeed = 0.3f;
        public float AutoSpinSpeed = 8f;
        public bool AutoSpin = false;

        float _yaw;
        bool _dragging;
        float _lastMouseX;

        void Start() => _yaw = transform.eulerAngles.y;

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && !UiPointer.IsOverUI())
            {
                _dragging = true;
                _lastMouseX = Input.mousePosition.x;
            }
            if (Input.GetMouseButtonUp(0)) _dragging = false;

            if (_dragging)
            {
                float dx = Input.mousePosition.x - _lastMouseX;
                _lastMouseX = Input.mousePosition.x;
                _yaw -= dx * DragSpeed;
            }
            else if (AutoSpin)
            {
                _yaw += AutoSpinSpeed * Time.deltaTime;
            }

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }
    }
}
