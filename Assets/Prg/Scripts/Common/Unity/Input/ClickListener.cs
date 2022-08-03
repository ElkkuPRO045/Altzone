using Prg.Scripts.Common.PubSub;
using UnityEngine;

namespace Prg.Scripts.Common.Unity.Input
{
    public class ClickListener : MonoBehaviour
    {
        [Header("Settings"), SerializeField] private Camera _camera;
        [SerializeField] private string _clickableTagName;
        [SerializeField] private LayerMask _clickableLayers;

        [Header("Debug"), SerializeField] private int _layerMask;

        private void OnEnable()
        {
            _layerMask = _clickableLayers.value;
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            this.Subscribe<InputManager.ClickDownEvent>(OnClickDownEvent);
        }

        private void OnDisable()
        {
            this.Unsubscribe();
        }

        private void OnClickDownEvent(InputManager.ClickDownEvent data)
        {
            if (data.ClickCount > 1)
            {
                return;
            }
            var ray = _camera.ScreenPointToRay(data.ScreenPosition);
            var hit = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit.collider == null)
            {
                return;
            }
            var hitObject = hit.collider.gameObject;
            var layer = hitObject.layer;
            var hasTag = !string.IsNullOrEmpty(_clickableTagName) && hitObject.CompareTag(_clickableTagName);
            var hasLayer = hasTag || _layerMask == (_layerMask | (1 << layer)); // unity3d check if layer mask contains layer

            //Debug.Log($"CLICK {hitObject.GetFullPath()} tag {hitObject.tag} ({hasTag}) layer {_layer} {LayerMask.LayerToName(_layer)} ({hasLayer})");
            if (hasTag || hasLayer)
            {
                this.Publish(new ClickObjectEvent(data.ScreenPosition, hitObject));
            }
        }

        public class ClickObjectEvent
        {
            public readonly Vector2 ScreenPosition;
            public readonly GameObject GameObject;

            public ClickObjectEvent(Vector2 screenPosition, GameObject gameObject)
            {
                ScreenPosition = screenPosition;
                GameObject = gameObject;
            }

            public override string ToString()
            {
                return $"{nameof(ScreenPosition)}: {ScreenPosition}, {nameof(GameObject)}: {GameObject.GetFullPath()}";
            }
        }
    }
}
