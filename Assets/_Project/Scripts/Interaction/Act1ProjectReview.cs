using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrisisVR
{
    /// <summary>
    /// Pantalla de revisión del Acto 1 (solo Acto1.unity): al terminar de hablar Miguel, la laptop muestra el proyecto que el
    /// estudiante presentó y su informe de evaluación (08/20 y las dos observaciones). No hay botones ni mensajes que inviten a
    /// corregir: el estudiante puede explorar las secciones del proyecto o las observaciones por iniciativa propia.
    /// Si elige un apartado observado (Diseño o Pruebas, en el proyecto o en el informe) se desbloquea la interfaz de corrección
    /// ya existente (Act1ProjectCorrection.Unlock). Si no lo hace, el tiempo de espera de Act1Flow vence y el proyecto queda como está.
    /// Registra en el registro de eventos de la sesión (SessionRuntime.LogEvent) cada acción observable y sus tiempos.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Act1ProjectReview : MonoBehaviour
    {
        // PROPUESTA – VALIDAR CON CLIENTA: todos los textos de este panel.
        const string PanelTitle = "Proyecto final";
        const string PanelSubtitle = "Portal académico";
        const string ReportHeading = "INFORME DE EVALUACIÓN";
        const string GradeLabel = "Calificación";
        const string GradeValue = "08 / 20";
        const string ObservationsHeading = "OBSERVACIONES DEL PROFESOR";
        const string BackLabel = "VER INFORME";
        const string NoObservations = "Sin observaciones del profesor.";

        static readonly string[] Sections = { "1. Introducción", "2. Diseño", "3. Implementación", "4. Pruebas" };
        // Apartados con observación del profesor (Diseño y Pruebas), como en Act1ProjectCorrection.
        static readonly bool[] Observed = { false, true, false, true };
        static readonly string[] ObservationTags = { "Diseño", "Pruebas" };
        static readonly string[] Observations =
        {
            "Falta justificar algunas decisiones de diseño.",
            "No se incluyen pruebas de funcionamiento.",
        };
        static readonly int[] SectionOfObservation = { 1, 3 };

        static readonly Color PlateColor = new Color(.040f, .060f, .090f);
        static readonly Color HeaderColor = new Color(.075f, .105f, .145f);
        static readonly Color CardColor = new Color(.085f, .115f, .155f);
        static readonly Color RowIdle = new Color(.105f, .140f, .185f);
        static readonly Color Amber = new Color(.78f, .50f, .12f);
        static readonly Color GradeColor = new Color(1f, .72f, .30f);
        static readonly Color TextMain = new Color(.93f, .95f, .97f);
        static readonly Color TextMuted = new Color(.62f, .70f, .78f);
        static readonly Color TextDark = new Color(.04f, .09f, .15f);
        static readonly Color NeutralIdle = new Color(.22f, .30f, .40f);
        static readonly Color ChipHover = new Color(.42f, .76f, .95f);
        static readonly Color PageColor = new Color(.93f, .94f, .95f);
        static readonly Color PageHover = new Color(.78f, .89f, .97f);
        static readonly Color PageSelected = new Color(.84f, .88f, .93f);
        static readonly Color PageText = new Color(.10f, .14f, .20f);
        static readonly Color SkeletonDone = new Color(.78f, .83f, .87f);
        static readonly Color SkeletonDark = new Color(.20f, .27f, .36f);
        static readonly Color DotNeutral = new Color(.55f, .62f, .70f);

        // Espacio local de Laptop_Pantalla (metros). El espectador mira desde -Z, como el Overlay existente.
        const float ZPlate = -.0045f, ZCard = -.0060f, ZBlock = -.0068f, ZItem = -.0075f, ZText = -.0090f;
        // Los colliders llegan más adelante que la tapa agarrable para que el rayo nunca agarre la tapa por error.
        const float ColliderDepth = .012f;
        const float ColliderOffset = -.004f;
        // Una permanencia del rayo sobre un elemento solo se registra si dura al menos esto (segundos).
        const float MinHoverLogged = .4f;

        sealed class Element
        {
            public string id;
            public GameObject root;
            public Renderer background;
            public TextMeshPro label;
            public Color idle;
            public Color hover;
            public Color selected;
            public Color labelIdle;
            public Color labelHover;
            public int hoverCount;
            public float hoverStart;
            public bool isSelected;
        }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static Mesh quad;

        [SerializeField] Act1Flow flow;
        [SerializeField] Act1ProjectCorrection correction;

        Transform lid;
        Material material;
        MaterialPropertyBlock block;
        GameObject panel;
        GameObject reportGroup;
        GameObject previewGroup;
        TextMeshPro previewTitle;
        bool built;

        readonly Element[] sectionBlocks = new Element[4];
        readonly Element[] observationRows = new Element[2];
        Element backButton;
        readonly List<Element> elements = new List<Element>();

        int openedSection = -1;
        bool unlocked;
        bool awaitedOnce;
        bool panelShownOnce;
        bool lidWasOpen;
        bool lidKnown;
        bool closedLogged;
        bool firstInteractionLogged;
        int interactions;
        int sectionExplorations;
        int reportViews;
        int observationOpens;
        float windowSeconds;           // desde que el acto espera la decisión (misma referencia que Act1ProjectCorrection)
        float panelSeconds;            // con el panel de revisión visible
        float firstInteractionSeconds = -1f;
        float discoverySeconds = -1f;
        readonly bool[] sectionSeen = new bool[4];

        public bool Discovered => unlocked;
        public bool PanelVisible => built && panel.activeInHierarchy;
        public int InteractionCount => interactions;
        public int SectionExplorations => sectionExplorations;
        public float WindowSeconds => windowSeconds;
        public float PanelSeconds => panelSeconds;
        public float FirstInteractionSeconds => firstInteractionSeconds;
        public float DiscoverySeconds => discoverySeconds;
        /// <summary>Sección abierta en la vista previa (0 a 3) o -1 si se ve el informe.</summary>
        public int OpenedSection => openedSection;

        void Awake()
        {
            if (flow == null) flow = FindAnyObjectByType<Act1Flow>();
            if (correction == null) correction = GetComponentInParent<Act1ProjectCorrection>();
            if (correction == null) correction = FindAnyObjectByType<Act1ProjectCorrection>();
            var hinge = GetComponentInParent<Act1LaptopHingeGrabTransformer>();
            if (hinge != null) lid = hinge.transform;
        }

        // Se construye en Start (después de todos los Awake): así los colliders nuevos no se suman a los de la tapa agarrable.
        void Start()
        {
            if (flow == null || lid == null || correction == null)
            {
                Debug.LogError("Act1ProjectReview necesita Act1Flow, Act1ProjectCorrection y la tapa de la laptop (Act1LaptopHingeGrabTransformer).", this);
                enabled = false;
                return;
            }
            Build();
        }

        void OnDisable()
        {
            if (panel != null) panel.SetActive(false);
        }

        void Update()
        {
            if (!built) return;

            bool awaiting = flow.AwaitingDecision;
            if (awaiting)
            {
                awaitedOnce = true;
                windowSeconds += Time.deltaTime;
            }

            bool open = IsLidOpen();
            if (awaiting && (!lidKnown || open != lidWasOpen))
            {
                // El primer estado también se anota: indica si el estudiante ya tenía la laptop abierta al terminar Miguel.
                lidKnown = true;
                lidWasOpen = open;
                Log("ACTO1_REVISION_TAPA", "estado=" + (open ? "abierta" : "cerrada") + " t=" + Seconds(windowSeconds));
            }

            bool show = awaiting && open && !unlocked;
            if (show != panel.activeSelf)
            {
                if (!show) FlushHover();
                panel.SetActive(show);
                if (show) Refresh();
            }
            if (!show) return;

            panelSeconds += Time.deltaTime;
            if (!panelShownOnce)
            {
                panelShownOnce = true;
                Log("ACTO1_REVISION_ABIERTA", "t=" + Seconds(windowSeconds));
            }
        }

        void LateUpdate()
        {
            // La espera terminó (presentó la corrección o venció el tiempo): se deja constancia una sola vez.
            if (!built || closedLogged || !awaitedOnce) return;
            if (flow.AwaitingDecision || !flow.DecisionRegistrada) return;
            closedLogged = true;
            FlushHover();
            if (!unlocked) Log("ACTO1_REVISION_SIN_DESCUBRIR", Summary());
            Log("ACTO1_REVISION_CIERRE", Summary());
        }

        bool IsLidOpen() => Quaternion.Angle(lid.localRotation, Quaternion.identity) > 5f;

        // ---------------------------------------------------------------- interacción
        // Estos métodos son el único punto de entrada: el rayo (XRSimpleInteractable) y las pruebas pasan por aquí.

        bool Blocked => !built || unlocked || GamePause.IsPaused || !panel.activeInHierarchy;

        public void SelectSection(int section)
        {
            if (Blocked || section < 0 || section >= Sections.Length) return;
            NoteInteraction("seccion_" + (section + 1));

            if (Observed[section])
            {
                Discover("seccion_" + (section + 1));
                return;
            }

            openedSection = section;
            sectionExplorations++;
            sectionSeen[section] = true;
            Log("ACTO1_REVISION_APARTADO_EXPLORADO",
                "seccion=" + (section + 1) + " observado=no veces=" + sectionExplorations + " t=" + Seconds(windowSeconds));
            Refresh();
        }

        public void SelectObservation(int observation)
        {
            if (Blocked || observation < 0 || observation >= Observations.Length) return;
            NoteInteraction("observacion_" + (observation + 1));
            observationOpens++;
            Log("ACTO1_REVISION_OBSERVACION_ABIERTA",
                "observacion=" + (observation + 1) + " seccion=" + (SectionOfObservation[observation] + 1) + " t=" + Seconds(windowSeconds));
            Discover("observacion_" + (observation + 1));
        }

        public void ShowReport()
        {
            if (Blocked || openedSection < 0) return;
            NoteInteraction("ver_informe");
            openedSection = -1;
            reportViews++;
            Log("ACTO1_REVISION_INFORME_VISTO", "veces=" + reportViews + " t=" + Seconds(windowSeconds));
            Refresh();
        }

        void NoteInteraction(string element)
        {
            interactions++;
            if (firstInteractionLogged) return;
            firstInteractionLogged = true;
            firstInteractionSeconds = windowSeconds;
            Log("ACTO1_REVISION_PRIMERA_INTERACCION", "elemento=" + element + " t=" + Seconds(windowSeconds));
        }

        void Discover(string via)
        {
            if (unlocked) return;
            unlocked = true;
            discoverySeconds = windowSeconds;
            Log("ACTO1_REVISION_APARTADO_OBSERVADO_SELECCIONADO",
                "via=" + via + " t=" + Seconds(windowSeconds) + " interacciones=" + interactions +
                " secciones_exploradas=" + sectionExplorations + " tiempo_panel=" + Seconds(panelSeconds));
            FlushHover();
            // El panel de revisión se oculta en el siguiente Update y la corrección existente toma su lugar.
            correction.Unlock();
        }

        string Summary() =>
            "descubrio=" + (unlocked ? "si" : "no") +
            " interacciones=" + interactions + " secciones_exploradas=" + SeenList() +
            " informe_vistas=" + reportViews + " observaciones_abiertas=" + observationOpens +
            " primera_interaccion=" + (firstInteractionSeconds < 0f ? "-" : Seconds(firstInteractionSeconds)) +
            " descubrimiento=" + (discoverySeconds < 0f ? "-" : Seconds(discoverySeconds)) +
            " tiempo_total=" + Seconds(windowSeconds) + " tiempo_panel=" + Seconds(panelSeconds);

        string SeenList()
        {
            var seen = new List<string>();
            for (int i = 0; i < sectionSeen.Length; i++) if (sectionSeen[i]) seen.Add((i + 1).ToString(CultureInfo.InvariantCulture));
            return seen.Count == 0 ? "-" : string.Join(",", seen);
        }

        static string Seconds(float s) => s.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        static void Log(string eventName, string detail) => SessionRuntime.LogEvent(eventName + " " + detail);

        // ---------------------------------------------------------------- hover (tiempos de permanencia del rayo)

        void OnHoverEntered(Element e)
        {
            if (++e.hoverCount == 1) e.hoverStart = windowSeconds;
            Apply(e);
        }

        void OnHoverExited(Element e)
        {
            if (e.hoverCount > 0 && --e.hoverCount == 0) LogHover(e);
            Apply(e);
        }

        void LogHover(Element e)
        {
            float dwell = windowSeconds - e.hoverStart;
            if (dwell < MinHoverLogged) return;
            Log("ACTO1_REVISION_HOVER", "elemento=" + e.id + " duracion=" + Seconds(dwell) + " t=" + Seconds(windowSeconds));
        }

        // Al ocultarse el panel el rayo ya no está sobre sus elementos: se cierran las permanencias abiertas.
        void FlushHover()
        {
            foreach (var e in elements)
            {
                if (e.hoverCount <= 0) continue;
                e.hoverCount = 0;
                LogHover(e);
                Apply(e);
            }
        }

        // ---------------------------------------------------------------- estado visual

        void Refresh()
        {
            for (int i = 0; i < sectionBlocks.Length; i++)
            {
                sectionBlocks[i].isSelected = openedSection == i;
                Apply(sectionBlocks[i]);
            }
            bool preview = openedSection >= 0;
            reportGroup.SetActive(!preview);
            previewGroup.SetActive(preview);
            if (preview) previewTitle.text = Sections[openedSection];
            foreach (var e in elements) if (e.root != null && !e.root.activeInHierarchy) e.hoverCount = 0;
        }

        void Apply(Element e)
        {
            bool hovered = e.hoverCount > 0;
            Paint(e.background, hovered ? e.hover : e.isSelected ? e.selected : e.idle);
            if (e.label != null) e.label.color = hovered ? e.labelHover : e.labelIdle;
        }

        void Paint(Renderer target, Color color)
        {
            target.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            target.SetPropertyBlock(block);
        }

        // ---------------------------------------------------------------- construcción

        void Build()
        {
            material = FindMaterial();
            if (material == null)
            {
                Debug.LogError("Act1ProjectReview: no se encontró el material de la pantalla.", this);
                enabled = false;
                return;
            }
            block = new MaterialPropertyBlock();

            panel = new GameObject("Panel_Revision");
            panel.transform.SetParent(transform, false);

            // Placa que cubre el Overlay existente y que, con su collider, evita que el rayo agarre la tapa entre elementos.
            var plate = new GameObject("Fondo_Panel").transform;
            plate.SetParent(panel.transform, false);
            MakeQuad("Placa", plate, 0f, 0f, .32f, .20f, ZPlate, PlateColor);
            var plateCollider = plate.gameObject.AddComponent<BoxCollider>();
            plateCollider.size = new Vector3(.32f, .20f, .008f);
            plateCollider.center = new Vector3(0f, 0f, ZPlate - .004f);
            plate.gameObject.AddComponent<XRSimpleInteractable>();

            MakeQuad("Encabezado", panel.transform, 0f, .084f, .304f, .016f, ZCard, HeaderColor);
            MakeText("Titulo", panel.transform, PanelTitle, .105f, TextMain, TextAlignmentOptions.Left, -.0760f, .084f, .150f, .014f, FontStyles.Bold, false);
            MakeText("Subtitulo", panel.transform, PanelSubtitle, .072f, TextMuted, TextAlignmentOptions.Right, .0760f, .084f, .150f, .014f, FontStyles.Normal, false);

            BuildProjectPage();
            BuildRightPane();

            built = true;
            Refresh();
            panel.SetActive(false);
        }

        void BuildProjectPage()
        {
            var page = new GameObject("Proyecto").transform;
            page.SetParent(panel.transform, false);
            page.localPosition = new Vector3(-.112f, .001f, 0f);
            MakeQuad("Hoja", page, 0f, 0f, .080f, .144f, ZCard, PageColor);
            MakeText("Nombre", page, "Proyecto final", .078f, PageText, TextAlignmentOptions.Left, 0f, .063f, .070f, .011f, FontStyles.Bold, false);
            MakeQuad("Separador", page, 0f, .0555f, .068f, .0015f, ZItem, SkeletonDone);

            for (int i = 0; i < Sections.Length; i++)
            {
                float top = .048f - i * .0335f;
                int section = i;
                var e = MakeInteractive("Seccion_" + (i + 1), page, 0f, top - .0105f, .074f, .026f, "seccion_" + (i + 1), () => SelectSection(section));
                e.background = MakeQuad("Fondo", e.root.transform, 0f, 0f, .074f, .026f, ZBlock, PageColor);
                e.idle = PageColor;
                e.hover = PageHover;
                e.selected = PageSelected;
                e.labelIdle = PageText;
                e.labelHover = PageText;
                MakeQuad("Estado", e.root.transform, -.0305f, .006f, .0065f, .0065f, ZItem, DotNeutral);
                e.label = MakeText("Nombre", e.root.transform, Sections[i], .064f, PageText, TextAlignmentOptions.Left, .0035f, .006f, .062f, .009f, FontStyles.Normal, false);
                MakeQuad("Linea_a", e.root.transform, 0f, -.003f, .064f, .002f, ZItem, SkeletonDone);
                MakeQuad("Linea_b", e.root.transform, -.008f, -.008f, .048f, .002f, ZItem, SkeletonDone);
                sectionBlocks[i] = e;
                Apply(e);
            }
        }

        void BuildRightPane()
        {
            var pane = new GameObject("Panel_Derecho").transform;
            pane.SetParent(panel.transform, false);
            pane.localPosition = new Vector3(.042f, .001f, 0f);
            MakeQuad("Tarjeta", pane, 0f, 0f, .220f, .144f, ZCard, CardColor);

            // Informe de evaluación (vista inicial).
            reportGroup = new GameObject("Informe");
            reportGroup.transform.SetParent(pane, false);
            var report = reportGroup.transform;
            MakeText("Titulo", report, ReportHeading, .060f, TextMuted, TextAlignmentOptions.Left, 0f, .0615f, .200f, .008f, FontStyles.Bold, false);
            MakeText("Calificacion_Etiqueta", report, GradeLabel, .066f, TextMuted, TextAlignmentOptions.Left, 0f, .047f, .200f, .009f, FontStyles.Normal, false);
            MakeText("Calificacion", report, GradeValue, .150f, GradeColor, TextAlignmentOptions.Left, 0f, .0295f, .200f, .022f, FontStyles.Bold, false);
            MakeText("Observaciones_Titulo", report, ObservationsHeading, .060f, TextMuted, TextAlignmentOptions.Left, 0f, .008f, .200f, .008f, FontStyles.Bold, false);
            for (int k = 0; k < Observations.Length; k++)
            {
                int observation = k;
                float y = -.014f - k * .034f;
                var e = MakeInteractive("Observacion_" + (k + 1), report, 0f, y, .204f, .030f, "observacion_" + (k + 1), () => SelectObservation(observation));
                e.background = MakeQuad("Fondo", e.root.transform, 0f, 0f, .204f, .030f, ZItem, RowIdle);
                e.idle = RowIdle;
                e.hover = ChipHover;
                e.selected = RowIdle;
                e.labelIdle = TextMain;
                e.labelHover = TextDark;
                MakeQuad("Acento", e.root.transform, -.1015f, 0f, .002f, .030f, ZItem - .0004f, Amber);
                MakeQuad("Etiqueta_Fondo", e.root.transform, -.080f, 0f, .034f, .013f, ZItem - .0006f, Amber);
                MakeText("Etiqueta", e.root.transform, ObservationTags[k], .056f, TextDark, TextAlignmentOptions.Center, -.080f, 0f, .034f, .013f, FontStyles.Bold, false);
                e.label = MakeText("Texto", e.root.transform, Observations[k], .074f, TextMain, TextAlignmentOptions.Left, .017f, 0f, .150f, .026f, FontStyles.Normal, true);
                observationRows[k] = e;
                Apply(e);
            }

            // Vista previa de una sección sin observaciones (solo aparece al explorar Introducción o Implementación).
            previewGroup = new GameObject("Vista_Previa");
            previewGroup.transform.SetParent(pane, false);
            var preview = previewGroup.transform;
            previewTitle = MakeText("Titulo", preview, string.Empty, .095f, TextMain, TextAlignmentOptions.Left, -.0275f, .0585f, .145f, .012f, FontStyles.Bold, false);
            float[] widths = { .190f, .170f, .190f, .120f, .190f, .160f, .110f };
            for (int i = 0; i < widths.Length; i++)
                MakeQuad("Linea_" + (i + 1), preview, -.100f + widths[i] / 2f, .036f - i * .0085f, widths[i], .002f, ZItem, SkeletonDark);
            MakeText("Sin_Observaciones", preview, NoObservations, .074f, TextMuted, TextAlignmentOptions.Left, 0f, -.050f, .200f, .010f, FontStyles.Italic, false);

            backButton = MakeInteractive("Ver_Informe", preview, .0695f, .0585f, .061f, .016f, "ver_informe", ShowReport);
            backButton.background = MakeQuad("Fondo", backButton.root.transform, 0f, 0f, .061f, .016f, ZItem, NeutralIdle);
            backButton.idle = NeutralIdle;
            backButton.hover = ChipHover;
            backButton.selected = NeutralIdle;
            backButton.labelIdle = TextMain;
            backButton.labelHover = TextDark;
            backButton.label = MakeText("Texto", backButton.root.transform, BackLabel, .062f, TextMain, TextAlignmentOptions.Center, 0f, 0f, .057f, .014f, FontStyles.Bold, true);
            Apply(backButton);
        }

        Element MakeInteractive(string name, Transform parent, float x, float y, float w, float h, string id, System.Action onSelect)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(x, y, 0f);

            var element = new Element { id = id, root = root };

            // El collider va antes que el interactable: XRI toma los colliders al despertar.
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(w, h, ColliderDepth);
            collider.center = new Vector3(0f, 0f, ZItem + ColliderOffset);
            var interactable = root.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => onSelect());
            interactable.hoverEntered.AddListener(_ => OnHoverEntered(element));
            interactable.hoverExited.AddListener(_ => OnHoverExited(element));
            elements.Add(element);
            return element;
        }

        Renderer MakeQuad(string name, Transform parent, float x, float y, float w, float h, float z, Color color)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localScale = new Vector3(w, h, 1f);
            go.GetComponent<MeshFilter>().sharedMesh = Quad();
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            Paint(renderer, color);
            return renderer;
        }

        TextMeshPro MakeText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align,
            float x, float y, float w, float h, FontStyles style, bool wrap)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, ZText);
            var label = go.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(w, h);
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.fontStyle = style;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;
            label.text = text;
            label.ForceMeshUpdate();
            return label;
        }

        Material FindMaterial()
        {
            // Mismo material sin iluminación que el Overlay de la pantalla (así el shader seguro está en el build).
            Transform overlayBackground = transform.parent != null ? transform.parent.Find("Overlay/Fondo") : null;
            if (overlayBackground == null) overlayBackground = transform.Find("Overlay/Fondo");
            var renderer = overlayBackground != null ? overlayBackground.GetComponent<Renderer>() : null;
            if (renderer != null && renderer.sharedMaterial != null) return renderer.sharedMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            return shader != null ? new Material(shader) : null;
        }

        // Quad que mira hacia -Z, como el Quad integrado y el Overlay existente.
        static Mesh Quad()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "Act1Revision_Quad" };
            quad.vertices = new[]
            {
                new Vector3(-.5f, -.5f, 0f), new Vector3(.5f, -.5f, 0f), new Vector3(.5f, .5f, 0f), new Vector3(-.5f, .5f, 0f),
            };
            quad.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            quad.triangles = new[] { 0, 3, 2, 0, 2, 1 };
            quad.RecalculateBounds();
            return quad;
        }
    }
}
