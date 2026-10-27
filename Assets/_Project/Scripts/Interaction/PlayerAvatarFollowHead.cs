using UnityEngine;

namespace CrisisVR
{
    /// <summary>
    /// Mantiene el punto del modelo del jugador alineado con la posición horizontal y el giro del visor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAvatarFollowHead : MonoBehaviour
    {
        [SerializeField] Transform trackingOrigin;
        [SerializeField] Transform head;

        void Awake()
        {
            if (trackingOrigin == null)
                trackingOrigin = transform.parent;

            if (head == null && trackingOrigin != null)
            {
                var camera = trackingOrigin.GetComponentInChildren<Camera>(true);
                if (camera != null)
                    head = camera.transform;
            }
        }

        void LateUpdate()
        {
            if (trackingOrigin == null || head == null)
                return;

            var headLocalPosition = trackingOrigin.InverseTransformPoint(head.position);
            var anchorPosition = transform.localPosition;
            anchorPosition.x = headLocalPosition.x;
            anchorPosition.z = headLocalPosition.z;
            transform.localPosition = anchorPosition;

            var localForward = trackingOrigin.InverseTransformDirection(head.forward);
            localForward = Vector3.ProjectOnPlane(localForward, Vector3.up);
            if (localForward.sqrMagnitude > 0.0001f)
                transform.localRotation = Quaternion.LookRotation(localForward.normalized, Vector3.up);
        }
    }
}
