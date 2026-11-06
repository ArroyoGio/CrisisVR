using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

namespace CrisisVR
{
    /// <summary>
    /// Tecla física del teclado de la laptop de Acto 1. Se presiona acercando el mando y accionando el
    /// gatillo (acción de selección del Near-Far Interactor). La tecla solo se mueve a lo largo de su eje
    /// vertical y nunca más allá de su recorrido, por lo que no puede desprenderse ni atravesar la base.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class Act1KeyboardKey : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly List<Act1KeyboardKey> AllKeys = new List<Act1KeyboardKey>();
        static readonly HashSet<int> CasterInstalledScenes = new HashSet<int>();
        const float DistanceTieEpsilon = 0.00000001f;

        [Header("Recorrido")]
        [SerializeField, Range(0.0003f, 0.003f), Tooltip("Hundimiento máximo en metros (mundo). Se limita al hueco real entre la tecla y la base.")]
        float travel = 0.0012f;
        [SerializeField, Tooltip("Velocidad de bajada en m/s.")] float pressSpeed = 0.05f;
        [SerializeField, Tooltip("Velocidad de retorno en m/s.")] float releaseSpeed = 0.02f;
        [SerializeField, Tooltip("Tiempo mínimo hundida, para que un toque rápido se vea.")] float minHoldTime = 0.1f;
        [SerializeField, Tooltip("Si el gatillo sigue apretado, la tecla vuelve igualmente tras este tiempo.")] float maxHoldTime = 0.35f;

        [Header("Alcance")]
        [SerializeField, Range(0.01f, 0.15f), Tooltip("Distancia máxima en metros del origen del mando a la superficie de la tecla. Solo se activa la tecla más cercana.")]
        float reachDistance = 0.05f;

        [Header("Respuesta")]
        [SerializeField] Color hoverColor = new Color(0.2f, 0.9f, 1f, 1f);
        [SerializeField] Color pressedColor = new Color(0.13f, 0.38f, 0.78f, 1f);
        [SerializeField, Range(0f, 1f)] float hapticAmplitude = 0.35f;
        [SerializeField, Range(0f, 0.2f)] float hapticDuration = 0.04f;

        XRSimpleInteractable interactable;
        Collider keyCollider;
        Renderer keyRenderer;
        AudioSource keyboardAudio;
        MaterialPropertyBlock block;
        Color baseColor = Color.white;

        Vector3 restLocalPosition;
        Quaternion restLocalRotation;
        float parentScaleY = 1f;
        float maxDepth;
        float depth;

        bool pressed;
        bool releaseRequested;
        float pressTime;

        /// <summary>Se dispara al hundirse la tecla. Pensado para la escritura de texto futura.</summary>
        public event Action<Act1KeyboardKey> Pressed;
        /// <summary>Se dispara al empezar a regresar la tecla.</summary>
        public event Action<Act1KeyboardKey> Released;

        public string KeyId => name;
        public int PhysicalKeyIndex { get; private set; }
        public bool IsPressed => pressed;
        /// <summary>Hundimiento actual en metros (mundo).</summary>
        public float CurrentDepth => depth;
        /// <summary>Recorrido máximo permitido en metros (mundo).</summary>
        public float MaxDepth => maxDepth;

        bool IXRHoverFilter.canProcess => isActiveAndEnabled;
        bool IXRSelectFilter.canProcess => isActiveAndEnabled;

        void Awake()
        {
            if (name.StartsWith("Tecla_", StringComparison.Ordinal) &&
                int.TryParse(name.Substring(6), out int keyIndex))
                PhysicalKeyIndex = keyIndex;

            interactable = GetComponent<XRSimpleInteractable>();
            keyCollider = GetComponent<Collider>();
            keyRenderer = GetComponent<Renderer>();
            keyboardAudio = transform.parent != null ? transform.parent.GetComponent<AudioSource>() : null;
            block = new MaterialPropertyBlock();
            if (keyRenderer != null && keyRenderer.sharedMaterial != null && keyRenderer.sharedMaterial.HasProperty(BaseColorId))
                baseColor = keyRenderer.sharedMaterial.GetColor(BaseColorId);

            restLocalPosition = transform.localPosition;
            restLocalRotation = transform.localRotation;
            parentScaleY = transform.parent != null ? Mathf.Max(0.0001f, transform.parent.lossyScale.y) : 1f;
            maxDepth = ComputeMaxDepth();

            var rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }
        }

        // El recorrido nunca supera el hueco entre la parte baja de la tecla y la cara superior de la base.
        float ComputeMaxDepth()
        {
            float wanted = travel;
            if (transform.parent == null)
                return wanted;

            Transform baseTransform = transform.parent.Find("Base_Visual_Teclado");
            var baseFilter = baseTransform != null ? baseTransform.GetComponent<MeshFilter>() : null;
            var keyFilter = GetComponent<MeshFilter>();
            if (baseFilter == null || baseFilter.sharedMesh == null || keyFilter == null || keyFilter.sharedMesh == null)
                return wanted;

            float baseTop = baseTransform.localPosition.y + baseTransform.localScale.y * baseFilter.sharedMesh.bounds.max.y;
            float keyBottom = restLocalPosition.y + transform.localScale.y * keyFilter.sharedMesh.bounds.min.y;
            float clearanceWorld = (keyBottom - baseTop) * parentScaleY;
            const float skin = 0.0001f;
            float allowed = Mathf.Max(0f, clearanceWorld - skin);
            if (allowed < wanted)
                Debug.LogWarning($"{name}: el hueco con la base es {clearanceWorld * 1000f:F2} mm; recorrido limitado a {allowed * 1000f:F2} mm.", this);
            return Mathf.Min(wanted, allowed);
        }

        void OnEnable()
        {
            AllKeys.Add(this);
            EnsureKeyboardCasters();
            EnsureGuidedKeyboard();
            interactable.hoverFilters.Add(this);
            interactable.selectFilters.Add(this);
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
        }

        void EnsureGuidedKeyboard()
        {
            if (gameObject.scene.path != "Assets/Scenes/Acto1.unity")
                return;

            Transform keyboardRoot = transform.parent;
            if (keyboardRoot == null || keyboardRoot.name != "Teclado_Fisico" ||
                keyboardRoot.GetComponent<Act1GuidedKeyboard>() != null)
                return;

            keyboardRoot.gameObject.AddComponent<Act1GuidedKeyboard>();
        }

        void OnDisable()
        {
            AllKeys.Remove(this);
            if (interactable != null)
            {
                interactable.hoverFilters.Remove(this);
                interactable.selectFilters.Remove(this);
                interactable.selectEntered.RemoveListener(OnSelectEntered);
                interactable.selectExited.RemoveListener(OnSelectExited);
            }
            pressed = false;
            releaseRequested = false;
            depth = 0f;
            ApplyPose();
            if (keyRenderer != null)
                SetColor(baseColor);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Cada nueva entrada de selección cuenta como pulsación, incluso si la tecla aún termina de subir.
            if (pressed)
                depth = 0f;
            pressed = true;
            releaseRequested = false;
            pressTime = Time.unscaledTime;
            SetColor(pressedColor);
            if (keyboardAudio != null && keyboardAudio.clip != null)
                keyboardAudio.PlayOneShot(keyboardAudio.clip);
            SendHaptic(args.interactorObject);
            Pressed?.Invoke(this);
        }

        void OnSelectExited(SelectExitEventArgs args) => releaseRequested = true;

        void Update()
        {
            if (pressed)
            {
                float held = Time.unscaledTime - pressTime;
                if (held >= minHoldTime && (releaseRequested || held >= maxHoldTime))
                {
                    pressed = false;
                    releaseRequested = false;
                    SetColor(interactable.isHovered ? hoverColor : baseColor);
                    Released?.Invoke(this);
                }
            }

            float target = pressed ? maxDepth : 0f;
            float speed = pressed ? pressSpeed : releaseSpeed;
            depth = Mathf.MoveTowards(depth, target, speed * Time.unscaledDeltaTime);
            ApplyPose();
        }

        // La pose se reescribe completa cada fotograma: solo cambia la altura, el resto queda fijo.
        void LateUpdate() => ApplyPose();

        void ApplyPose()
        {
            float clamped = Mathf.Clamp(depth, 0f, maxDepth);
            Vector3 down = restLocalRotation * Vector3.down;
            transform.localPosition = restLocalPosition + down * (clamped / parentScaleY);
            transform.localRotation = restLocalRotation;
        }

        void SetColor(Color color)
        {
            if (keyRenderer == null)
                return;
            keyRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            keyRenderer.SetPropertyBlock(block);
        }

        void SendHaptic(IXRSelectInteractor interactor)
        {
            var component = interactor as Component;
            if (component == null)
                return;

            var provider = component.GetComponentInParent<IXRHapticImpulseProvider>();
            var channel = provider != null ? provider.GetChannelGroup()?.GetChannel() : null;
            if (channel != null && channel.SendHapticImpulse(hapticAmplitude, hapticDuration))
                return;

            var player = component.GetComponentInParent<HapticImpulsePlayer>();
            if (player == null && component.transform.parent != null)
                player = component.transform.parent.GetComponentInChildren<HapticImpulsePlayer>();
            if (player == null)
                return;
            player.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }

        // --- Filtros: solo la tecla mejor situada bajo el mando y dentro del alcance. ---

        bool IXRHoverFilter.Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) =>
            IsTheKeyInReach(interactor);

        bool IXRSelectFilter.Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            // Una pulsación ya iniciada no se cancela por moverse un poco.
            if (pressed && this.interactable.interactorsSelecting.Contains(interactor))
                return true;
            return IsTheKeyInReach(interactor);
        }

        bool IsTheKeyInReach(IXRInteractor interactor)
        {
            // Usa el mismo origen que el caster Near para no medir desde un pivote distinto del mando.
            Vector3 origin = GetInteractorOrigin(interactor);
            float ownDistance = DistanceToSquared(origin);
            float maxDistance = reachDistance * reachDistance;
            if (ownDistance > maxDistance)
                return false;

            float ownCenterDistance = CenterDistanceToSquared(origin);

            for (int i = 0; i < AllKeys.Count; i++)
            {
                Act1KeyboardKey other = AllKeys[i];
                if (other == this || other.transform.parent != transform.parent)
                    continue;

                float otherDistance = other.DistanceToSquared(origin);
                if (otherDistance + DistanceTieEpsilon < ownDistance)
                    return false;

                // Si los colliders se solapan y ambos contienen el mando, favorece la tecla cuyo
                // centro está más cerca. El nombre con índice fijo resuelve únicamente empates exactos.
                if (Mathf.Abs(otherDistance - ownDistance) <= DistanceTieEpsilon)
                {
                    float otherCenterDistance = other.CenterDistanceToSquared(origin);
                    if (otherCenterDistance + DistanceTieEpsilon < ownCenterDistance)
                        return false;

                    if (Mathf.Abs(otherCenterDistance - ownCenterDistance) <= DistanceTieEpsilon)
                    {
                        int nameOrder = string.CompareOrdinal(other.name, name);
                        bool otherPrecedes = nameOrder < 0 ||
                            (nameOrder == 0 && other.transform.GetSiblingIndex() < transform.GetSiblingIndex());
                        if (otherPrecedes)
                            return false;
                    }
                }
            }
            return true;
        }

        static Vector3 GetInteractorOrigin(IXRInteractor interactor)
        {
            var nearInteractor = interactor as NearFarInteractor;
            if (nearInteractor != null && nearInteractor.nearInteractionCaster is InteractionCasterBase caster &&
                caster.effectiveCastOrigin != null)
                return caster.effectiveCastOrigin.position;

            return interactor.transform.position;
        }

        float DistanceToSquared(Vector3 point)
        {
            if (keyCollider == null)
                return float.PositiveInfinity;
            return (point - keyCollider.ClosestPoint(point)).sqrMagnitude;
        }

        float CenterDistanceToSquared(Vector3 point)
        {
            if (keyCollider == null)
                return float.PositiveInfinity;
            return (point - keyCollider.bounds.center).sqrMagnitude;
        }

        void EnsureKeyboardCasters()
        {
            int sceneHandle = gameObject.scene.handle;
            if (!CasterInstalledScenes.Add(sceneHandle))
                return;

            NearFarInteractor[] interactors = FindObjectsByType<NearFarInteractor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < interactors.Length; i++)
            {
                NearFarInteractor near = interactors[i];
                if (near.gameObject.scene.handle != sceneHandle ||
                    near.nearInteractionCaster is Act1KeyboardSphereInteractionCaster)
                    continue;

                var currentCaster = near.nearInteractionCaster;
                SphereInteractionCaster source = currentCaster as SphereInteractionCaster;
                if (source == null)
                    source = near.GetComponent<SphereInteractionCaster>();
                if (source == null && currentCaster != null)
                    continue;

                var replacement = near.gameObject.AddComponent<Act1KeyboardSphereInteractionCaster>();
                if (source != null)
                {
                    replacement.castOrigin = source.castOrigin != null ? source.castOrigin : near.transform;
                    replacement.enableStabilization = source.enableStabilization;
                    replacement.positionStabilization = source.positionStabilization;
                    replacement.angleStabilization = source.angleStabilization;
                    replacement.aimTarget = source.aimTarget;
                    replacement.PhysicsLayerMask = source.physicsLayerMask;
                    replacement.PhysicsTriggerInteraction = source.physicsTriggerInteraction;
                    replacement.CastRadius = source.castRadius;
                    source.enabled = false;
                }

                near.nearInteractionCaster = replacement;
            }
        }
    }
}
