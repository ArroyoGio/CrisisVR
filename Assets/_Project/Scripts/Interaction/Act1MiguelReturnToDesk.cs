using System.Globalization;
using UnityEngine;

namespace CrisisVR
{
    /// <summary>
    /// Acto 1 (solo Acto1.unity): cuando Miguel termina de comunicar la nota y las observaciones (Act1Flow pasa a esperar la
    /// decisión), camina hasta su escritorio y se queda mirando hacia el aula. No modifica el flujo ni el aula: solo mueve
    /// a Miguel. Anota en el registro de eventos de la sesión cuándo empieza a retirarse y cuándo llega.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Act1MiguelReturnToDesk : MonoBehaviour
    {
        [SerializeField] Act1Flow flow;
        [Tooltip("Punto (en planta) junto a su escritorio al que se retira.")]
        [SerializeField] Transform deskPoint;
        [Tooltip("Hacia dónde mira al llegar (por defecto, la zona de la laptop del participante).")]
        [SerializeField] Transform faceTarget;
        [SerializeField, Min(.1f)] float walkSpeed = .9f;
        [SerializeField, Min(0f)] float startDelay = .8f;
        [SerializeField, Min(10f)] float turnSpeed = 200f;
        [SerializeField, Min(0f)] float bobHeight = .012f;

        bool started;
        bool arrived;
        bool finished;
        float elapsed;
        float delayLeft;
        float baseY;
        float walkPhase;

        public bool Started => started;
        public bool Arrived => arrived;
        public bool Finished => finished;

        void Awake()
        {
            if (flow == null) flow = FindAnyObjectByType<Act1Flow>();
            baseY = transform.position.y;
            delayLeft = startDelay;
        }

        void Update()
        {
            if (finished || flow == null || deskPoint == null) return;

            if (!started)
            {
                if (!flow.AwaitingDecision) return;
                started = true;
                elapsed = 0f;
                Log("ACTO1_MIGUEL_RETIRO_INICIO", "origen=" + Format(transform.position));
            }

            float dt = Time.deltaTime;
            elapsed += dt;

            if (delayLeft > 0f)
            {
                delayLeft -= dt;
                return;
            }

            if (!arrived)
            {
                Vector3 target = deskPoint.position;
                Vector3 flat = target - transform.position;
                flat.y = 0f;
                float distance = flat.magnitude;
                float step = walkSpeed * dt;
                if (distance <= step)
                {
                    transform.position = new Vector3(target.x, baseY, target.z);
                    arrived = true;
                    Log("ACTO1_MIGUEL_EN_ESCRITORIO", "t=" + Seconds(elapsed) + " destino=" + Format(transform.position));
                    return;
                }

                Vector3 direction = flat / distance;
                Vector3 next = transform.position + direction * step;
                walkPhase += dt * walkSpeed * 7f;
                next.y = baseY + Mathf.Abs(Mathf.Sin(walkPhase)) * bobHeight;
                transform.position = next;
                TurnTowards(direction, dt);
                return;
            }

            // Ya junto al escritorio: se gira hacia el aula y termina.
            transform.position = new Vector3(transform.position.x, baseY, transform.position.z);
            Vector3 look = faceTarget != null ? faceTarget.position - transform.position : Vector3.back;
            look.y = 0f;
            if (look.sqrMagnitude < .0001f) look = Vector3.back;
            Quaternion goal = Quaternion.LookRotation(look.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * dt);
            if (Quaternion.Angle(transform.rotation, goal) < .5f) finished = true;
        }

        void TurnTowards(Vector3 direction, float dt)
        {
            if (direction.sqrMagnitude < .0001f) return;
            Quaternion goal = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * dt);
        }

        static string Seconds(float s) => s.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        static string Format(Vector3 v) =>
            "(" + v.x.ToString("0.00", CultureInfo.InvariantCulture) + "," + v.z.ToString("0.00", CultureInfo.InvariantCulture) + ")";
        static void Log(string eventName, string detail) => SessionRuntime.LogEvent(eventName + " " + detail);
    }
}
