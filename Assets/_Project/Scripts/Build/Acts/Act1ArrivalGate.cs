using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace CrisisVR
{
    // Recorrido de llegada del Acto 1: el participante empieza fuera del instituto y camina (joystick) hasta su carpeta.
    // Act1Flow se retiene (no ejecuta Start) hasta que la cabeza del participante entra en la zona junto a su carpeta;
    // entonces se activa el Acto 1 con su tarjeta de transicion y el participante queda en la pose original (Spawn_Participante).
    public sealed class Act1ArrivalGate : MonoBehaviour
    {
        [SerializeField] Act1Flow flow;
        [SerializeField] XROrigin origin;
        [SerializeField] Transform seatSpawn;
        [SerializeField] Transform arrivalZone;
        [SerializeField, Min(.2f)] float arrivalRadius = .5f;
        [SerializeField] GameObject guideRoot;
        [SerializeField] Transform guideBob;
        [SerializeField] Transform guideText;
        [SerializeField] Transform zoneRing;

        bool arrived;
        float bobBaseY;

        public bool Arrived => arrived;

        void Awake()
        {
            // Se desactiva antes de que Act1Flow.Start arranque el acto; se reactiva al llegar a la carpeta.
            if (flow != null) flow.enabled = false;
            if (guideBob != null) bobBaseY = guideBob.localPosition.y;
        }

        void Update()
        {
            if (arrived) return;
            Transform head = origin != null && origin.Camera != null ? origin.Camera.transform : null;
            AnimateGuide(head);
            if (head == null || arrivalZone == null) return;
            Vector3 d = head.position - arrivalZone.position;
            d.y = 0f;
            if (d.sqrMagnitude <= arrivalRadius * arrivalRadius) Arrive();
        }

        void AnimateGuide(Transform head)
        {
            float t = Time.time;
            if (guideBob != null)
            {
                Vector3 p = guideBob.localPosition;
                p.y = bobBaseY + Mathf.Sin(t * 2.2f) * .07f;
                guideBob.localPosition = p;
            }
            if (zoneRing != null) zoneRing.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * .06f);
            if (guideText != null && head != null)
            {
                Vector3 away = guideText.position - head.position;
                away.y = 0f;
                if (away.sqrMagnitude > .001f) guideText.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
            }
        }

        [ContextMenu("Llegar a la carpeta (debug)")]
        public void Arrive()
        {
            if (arrived) return;
            arrived = true;
            if (guideRoot != null) guideRoot.SetActive(false);
            StartCoroutine(SitAndStartAct());
        }

        IEnumerator SitAndStartAct()
        {
            if (flow != null) flow.enabled = true;
            // La tarjeta de transicion del Acto 1 cubre la vista; se acomoda al participante mientras esta en negro.
            var card = FindAnyObjectByType<Act1TransitionCard>();
            CanvasGroup cg = card != null ? card.GetComponent<CanvasGroup>() : null;
            float waited = 0f;
            yield return null;
            while (cg != null && cg.alpha < .98f && waited < .75f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            SnapToSeat();
        }

        void SnapToSeat()
        {
            if (origin == null || origin.Camera == null || seatSpawn == null) return;
            var cc = origin.GetComponent<CharacterController>();
            bool hadCc = cc != null && cc.enabled;
            if (hadCc) cc.enabled = false;
            Vector3 fwd = seatSpawn.forward;
            fwd.y = 0f;
            origin.MatchOriginUpCameraForward(Vector3.up, fwd.normalized);
            Vector3 delta = seatSpawn.position - origin.Camera.transform.position;
            delta.y = 0f;
            origin.transform.position += delta;
            if (hadCc) cc.enabled = true;
            LockLocomotion();
        }

        // Sentado durante todo el Acto 1: la locomocion se bloquea al sentarse y se libera cuando el acto termina.
        readonly System.Collections.Generic.List<Behaviour> lockedProviders = new System.Collections.Generic.List<Behaviour>();
        bool seatedLock;
        Vector3 seatOffset;

        [SerializeField, Min(0f), Tooltip("Cuanto puede moverse/inclinarse la cabeza (m) sin salir del asiento. 0 = sin limite.")]
        float seatLeashRadius = .35f;

        // Para otros componentes del Acto 1 (p. ej. Act1LaptopZoom): participante sentado y desplazamiento actual respecto al asiento.
        public bool IsSeated => seatedLock;
        public Vector3 SeatBase => seatSpawn != null ? seatSpawn.position : (origin != null ? origin.transform.position : Vector3.zero);
        public Vector3 SeatOffset => seatOffset;
        public event System.Action SeatReleased;

        // Desplaza horizontalmente al participante y el centro del limite de cabeza a la vez, para que el limite no lo regrese al asiento.
        public void ShiftSeat(Vector3 delta)
        {
            if (!seatedLock || origin == null) return;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0f) return;
            MoveOrigin(delta);
            seatOffset += delta;
        }

        void MoveOrigin(Vector3 delta)
        {
            var cc = origin.GetComponent<CharacterController>();
            bool hadCc = cc != null && cc.enabled;
            if (hadCc) cc.enabled = false;
            origin.transform.position += delta;
            if (hadCc) cc.enabled = true;
        }

        void LockLocomotion()
        {
            if (origin == null || seatedLock) return;
            seatedLock = true;
            seatOffset = Vector3.zero;
            foreach (var provider in origin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>(true))
            {
                // La gravedad se deja activa para que no flote.
                if (provider.GetType().Name == "GravityProvider" || !provider.enabled) continue;
                provider.enabled = false;
                lockedProviders.Add(provider);
            }
            if (flow != null) flow.ActCompleted += UnlockLocomotion;
        }

        void UnlockLocomotion()
        {
            if (flow != null) flow.ActCompleted -= UnlockLocomotion;
            // Si el participante estaba acercado a la laptop vuelve al asiento (la tarjeta de salida del acto cubre la vista).
            if (origin != null && seatOffset.sqrMagnitude > 0f) MoveOrigin(-seatOffset);
            seatOffset = Vector3.zero;
            seatedLock = false;
            foreach (var provider in lockedProviders)
                if (provider != null) provider.enabled = true;
            lockedProviders.Clear();
            SeatReleased?.Invoke();
        }

        void OnDestroy()
        {
            if (flow != null) flow.ActCompleted -= UnlockLocomotion;
        }

        // Mantiene al participante en el asiento aunque se desplace fisicamente (o con el simulador XR):
        // si la cabeza se aleja mas de seatLeashRadius del asiento, se reubica el XR Origin para compensar.
        void LateUpdate()
        {
            if (!seatedLock || seatLeashRadius <= 0f || origin == null || origin.Camera == null || seatSpawn == null) return;
            Vector3 off = origin.Camera.transform.position - (seatSpawn.position + seatOffset);
            off.y = 0f;
            float excess = off.magnitude - seatLeashRadius;
            if (excess <= 0f) return;
            MoveOrigin(-off.normalized * excess);
        }
    }
}
