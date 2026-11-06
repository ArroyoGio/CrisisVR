using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace CrisisVR
{
    /// <summary>Conserva el pivote de la laptop y convierte el movimiento del mando en giro de bisagra.</summary>
    public sealed class Act1LaptopHingeGrabTransformer : XRBaseGrabTransformer
    {
        [SerializeField] Transform hinge;
        [SerializeField, Range(0f, 180f)] float maxOpenAngle = 125f;
        Vector3 closedPositionInHingeSpace;
        Quaternion closedRotationInHingeSpace;
        bool hasClosedPose;
        [SerializeField, Range(0f, 15f), Tooltip("Por debajo de este ángulo de apertura la tapa se asienta cerrada del todo.")] float closeSnapAngle = 4f;
        float currentAngle;
        float angleAtGrab;
        float handAngleAtGrab;
        bool grabbing;

        protected override RegistrationMode registrationMode => RegistrationMode.Single;

        void Awake() => CacheClosedPose();

        void CacheClosedPose()
        {
            if (hinge == null)
                hinge = transform.parent;
            if (hinge == null)
                return;

            closedPositionInHingeSpace = hinge.InverseTransformPoint(transform.position);
            closedRotationInHingeSpace = Quaternion.Inverse(hinge.rotation) * transform.rotation;
            hasClosedPose = true;
        }

public override void Process(XRGrabInteractable grabInteractable,
            XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            if (hinge == null || grabInteractable.interactorsSelecting.Count == 0)
            {
                grabbing = false;
                return;
            }
            if (!hasClosedPose)
                CacheClosedPose();

            var interactor = grabInteractable.interactorsSelecting[0];
            var attach = interactor.GetAttachTransform(grabInteractable);
            if (attach == null)
                return;

            Vector3 handInHingeSpace = hinge.InverseTransformPoint(attach.position);
            // Cerrada, la mano queda hacia el borde frontal (+Z); al levantarla, el ángulo es negativo.
            float handAngle = Mathf.Atan2(-handInHingeSpace.y, handInHingeSpace.z) * Mathf.Rad2Deg;

            // El ángulo es relativo al punto donde se agarró la tapa: así no salta al agarrarla
            // y se puede cerrar del todo aunque la mano quede por encima de la superficie.
            if (!grabbing)
            {
                grabbing = true;
                angleAtGrab = currentAngle;
                handAngleAtGrab = handAngle;
            }

            float angle = angleAtGrab + Mathf.DeltaAngle(handAngleAtGrab, handAngle);
            angle = Mathf.Clamp(angle, -maxOpenAngle, 0f);
            if (angle > -closeSnapAngle)
                angle = 0f;
            currentAngle = angle;

            Quaternion hingeRotation = Quaternion.AngleAxis(angle, Vector3.right);
            targetPose.position = hinge.TransformPoint(hingeRotation * closedPositionInHingeSpace);
            targetPose.rotation = hinge.rotation * hingeRotation * closedRotationInHingeSpace;
        }

        public override void OnLink(XRGrabInteractable grabInteractable)
        {
            base.OnLink(grabInteractable);
            if (hinge == null)
                hinge = grabInteractable.transform.parent;
            if (!hasClosedPose)
                CacheClosedPose();
        }
    }
}
