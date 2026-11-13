using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrisisVR
{
    /// <summary>
    /// Corrección del proyecto en la pantalla de la laptop del Acto 1.
    /// Con la laptop abierta y el acto esperando la decisión, muestra el proyecto, las dos observaciones del profesor y,
    /// bajo cada una, un apartado incompleto que se completa eligiendo entre respuestas predefinidas con el rayo y el
    /// gatillo (XRSimpleInteractable, igual que ENTREGAR y CORREGIR PROYECTO). No hay teclado ni escritura libre.
    /// Las selecciones se pueden cambiar hasta presentar; «Presentar proyecto» aparece al completar ambos apartados y
    /// llama a Act1Flow.Correct(), que registra el reintento y continúa el flujo existente.
    /// Las selecciones, los cambios y los tiempos se anotan en el registro de eventos de la sesión (SessionRuntime.LogEvent).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Act1ProjectCorrection : MonoBehaviour
    {
        // PROPUESTA – VALIDAR CON CLIENTA: todos los textos de este panel.
        const string PanelTitle = "Corrección del proyecto";
        const string PanelSubtitle = "Observaciones del profesor";
        const string ProjectName = "Proyecto final";
        const string PresentLabel = "Presentar proyecto";
        const string StatusPending = "Completa los dos apartados ({0} de 2).";
        const string StatusReady = "Listo. Puedes cambiar tus respuestas.";
        const string StatusDone = "Proyecto presentado.";

        static readonly string[] Sections = { "1. Introducción", "2. Diseño", "3. Implementación", "4. Pruebas" };
        // Sección del proyecto que completa cada apartado (Diseño y Pruebas).
        static readonly int[] SectionOfApartado = { 1, 3 };
        static readonly string[] Observations =
        {
            "Falta justificar algunas decisiones de diseño.",
            "No se incluyen pruebas de funcionamiento.",
        };
        // El apartado es una frase incompleta; la opción elegida completa el final.
        static readonly string[] StemStart =
        {
            "Estas decisiones de diseño se tomaron porque ",
            "El funcionamiento se comprobó con ",
        };
        static readonly string[][] Options =
        {
            new[] { "son fáciles de ampliar", "hacen clara la interfaz", "facilitan el mantenimiento" },
            new[] { "pruebas de cada función", "casos con datos de ejemplo", "una revisión completa" },
        };

        // Colores: azul = activo, celeste = el rayo lo apunta, gris = inactivo (guía v1.0, como SelectableHighlight).
        static readonly Color PlateColor = new Color(.040f, .060f, .090f);
        static readonly Color HeaderColor = new Color(.075f, .105f, .145f);
        static readonly Color CardColor = new Color(.085f, .115f, .155f);
        static readonly Color Amber = new Color(.78f, .50f, .12f);
        static readonly Color TextMain = new Color(.93f, .95f, .97f);
        static readonly Color TextMuted = new Color(.62f, .70f, .78f);
        static readonly Color TextDark = new Color(.04f, .09f, .15f);
        static readonly Color ChipIdle = new Color(.13f, .38f, .78f);
        static readonly Color ChipHover = new Color(.42f, .76f, .95f);
        static readonly Color ChipSelected = new Color(.08f, .47f, .32f);
        static readonly Color ChipDisabled = new Color(.30f, .33f, .37f);
        static readonly Color PresentIdle = new Color(.10f, .55f, .36f);
        static readonly Color PageColor = new Color(.93f, .94f, .95f);
        static readonly Color PageText = new Color(.10f, .14f, .20f);
        static readonly Color SkeletonDone = new Color(.78f, .83f, .87f);
        static readonly Color SkeletonPending = new Color(.98f, .84f, .55f);
        static readonly Color DotDone = new Color(.15f, .65f, .40f);
        const string AmberHex = "#FFB84D";
        const string MintHex = "#9EFFC7";

        // Espacio local de Laptop_Pantalla (metros). El espectador mira desde -Z, como el Overlay existente (Mensaje en z = -0.002).
        const float ZPlate = -.0045f, ZCard = -.0060f, ZItem = -.0075f, ZText = -.0090f;
        // La tapa (collider del agarre) sobresale unos 5 mm de la pantalla hacia el espectador: los colliders de los botones
        // llegan más adelante para que el rayo los alcance antes y nunca agarre la tapa por error.
        const float ColliderDepth = .012f;
        const float ColliderOffset = -.004f;

        sealed class PanelButton
        {
            public GameObject root;
            public Renderer background;
            public TextMeshPro label;
            public Color idle;
            public bool hovered;
            public bool selected;
            public bool disabled;
        }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static Mesh quad;

        [SerializeField] Act1Flow flow;
        [Tooltip("Si está activo, el panel no aparece hasta que otro componente llame a Unlock() (p. ej. Act1ProjectReview al elegir un apartado observado). Apagado = comportamiento original.")]
        [SerializeField] bool requireUnlock;

        Transform lid;
        Material material;
        MaterialPropertyBlock block;
        GameObject panel;
        bool built;

        readonly PanelButton[,] chips = new PanelButton[2, 3];
        readonly TextMeshPro[] stems = new TextMeshPro[2];
        readonly Renderer[] sectionDots = new Renderer[4];
        readonly Renderer[][] sectionBars = new Renderer[4][];
        PanelButton presentButton;
        TextMeshPro status;

        readonly int[] selected = { -1, -1 };
        readonly int[] picks = new int[2];
        int changes;
        bool presented;
        bool unlocked;
        bool openedLogged;
        bool completeLogged;
        bool closedLogged;
        float windowSeconds;   // desde que el acto espera la decisión (se congela en pausa, como la espera de Act1Flow)
        float panelSeconds;    // con el panel abierto

        public bool Presented => presented;
        public bool IsUnlocked => !requireUnlock || unlocked;

        /// <summary>Permite que el panel de corrección se muestre (solo tiene efecto con requireUnlock).</summary>
        public void Unlock() => unlocked = true;
        public bool Complete => selected[0] >= 0 && selected[1] >= 0;
        public int SelectionCount => picks[0] + picks[1];
        public int ChangeCount => changes;
        public float WindowSeconds => windowSeconds;
        public float PanelSeconds => panelSeconds;
        /// <summary>Opción elegida del apartado (0 a 2) o -1 si aún no eligió.</summary>
        public int GetSelection(int apartado) => selected[apartado];

        void Awake()
        {
            if (flow == null) flow = FindAnyObjectByType<Act1Flow>();
            var hinge = GetComponentInParent<Act1LaptopHingeGrabTransformer>();
            if (hinge != null) lid = hinge.transform;
        }

        // Se construye en Start (después de todos los Awake): así los colliders nuevos no se suman a los de la tapa agarrable.
        void Start()
        {
            if (flow == null || lid == null)
            {
                Debug.LogError("Act1ProjectCorrection necesita Act1Flow y la tapa de la laptop (Act1LaptopHingeGrabTransformer).", this);
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
            if (awaiting) windowSeconds += Time.deltaTime;

            bool show = IsLidOpen() && IsUnlocked && (awaiting || (presented && !flow.Finished));
            if (show != panel.activeSelf) panel.SetActive(show);
            if (!show) return;

            if (!presented) panelSeconds += Time.deltaTime;
            if (!openedLogged)
            {
                openedLogged = true;
                Log("ACTO1_CORRECCION_ABIERTA", "t=" + Seconds(windowSeconds));
            }
        }

        void LateUpdate()
        {
            // La espera terminó sin presentar (venció el tiempo o se usó el botón CORREGIR PROYECTO): se deja constancia una vez.
            if (!built || presented || closedLogged || !openedLogged) return;
            if (flow.AwaitingDecision || !flow.DecisionRegistrada) return;
            closedLogged = true;
            Log("ACTO1_CORRECCION_SIN_PRESENTAR", Summary());
        }

        bool IsLidOpen() => Quaternion.Angle(lid.localRotation, Quaternion.identity) > 5f;

        // ---------------------------------------------------------------- interacción

        void OnOption(int apartado, int option)
        {
            if (presented || GamePause.IsPaused || !panel.activeInHierarchy) return;
            int previous = selected[apartado];
            if (previous == option) return;

            selected[apartado] = option;
            picks[apartado]++;
            if (previous >= 0) changes++;
            Log("ACTO1_CORRECCION_SELECCION",
                "apartado=" + (apartado + 1) + " opcion=" + Letter(option) + " anterior=" + Letter(previous) + " t=" + Seconds(windowSeconds));
            Refresh();

            if (completeLogged || !Complete) return;
            completeLogged = true;
            Log("ACTO1_CORRECCION_COMPLETA", "t=" + Seconds(windowSeconds));
        }

        void OnPresent()
        {
            // Mismas condiciones que Act1Flow.Correct(): así lo registrado aquí siempre coincide con el flujo.
            if (presented || !Complete || GamePause.IsPaused || !flow.AwaitingDecision) return;
            presented = true;
            Log("ACTO1_CORRECCION_PRESENTADA", "presentaciones=1 " + Summary());
            Refresh();
            flow.Correct();
        }

        string Summary() =>
            "apartado1=" + Letter(selected[0]) + " apartado2=" + Letter(selected[1]) +
            " selecciones=" + SelectionCount + " cambios=" + changes +
            " tiempo_total=" + Seconds(windowSeconds) + " tiempo_panel=" + Seconds(panelSeconds);

        static string Letter(int option) => option < 0 ? "-" : ((char)('A' + option)).ToString();
        static string Seconds(float s) => s.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        static void Log(string eventName, string detail) => SessionRuntime.LogEvent(eventName + " " + detail);

        // ---------------------------------------------------------------- estado visual

        void Refresh()
        {
            for (int a = 0; a < 2; a++)
            {
                stems[a].text = StemStart[a] + (selected[a] < 0
                    ? "<color=" + AmberHex + ">______</color>"
                    : "<b><color=" + MintHex + ">" + Options[a][selected[a]] + "</color></b>") + ".";

                for (int o = 0; o < 3; o++)
                {
                    var chip = chips[a, o];
                    chip.selected = selected[a] == o;
                    chip.disabled = presented && !chip.selected;
                    Apply(chip);
                }

                int section = SectionOfApartado[a];
                bool done = selected[a] >= 0;
                Paint(sectionDots[section], done ? DotDone : Amber);
                foreach (var bar in sectionBars[section]) Paint(bar, done ? SkeletonDone : SkeletonPending);
            }

            int completed = (selected[0] >= 0 ? 1 : 0) + (selected[1] >= 0 ? 1 : 0);
            status.text = presented ? StatusDone : Complete ? StatusReady : string.Format(CultureInfo.InvariantCulture, StatusPending, completed);
            status.color = presented ? new Color(.62f, 1f, .78f) : TextMuted;
            presentButton.root.SetActive(Complete && !presented);
        }

        void Apply(PanelButton b)
        {
            Color fill = b.disabled ? ChipDisabled : b.hovered ? ChipHover : b.selected ? ChipSelected : b.idle;
            Paint(b.background, fill);
            b.label.color = b.hovered && !b.disabled ? TextDark : b.disabled ? TextMuted : TextMain;
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
                Debug.LogError("Act1ProjectCorrection: no se encontró el material de la pantalla.", this);
                enabled = false;
                return;
            }
            block = new MaterialPropertyBlock();

            panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);

            // Placa que cubre el Overlay existente y que, con su collider, evita que el rayo agarre la tapa entre botones.
            var plate = new GameObject("Fondo_Panel").transform;
            plate.SetParent(panel.transform, false);
            MakeQuad("Placa", plate, 0f, 0f, .32f, .20f, ZPlate, PlateColor);
            var plateCollider = plate.gameObject.AddComponent<BoxCollider>();
            plateCollider.size = new Vector3(.32f, .20f, .008f);
            plateCollider.center = new Vector3(0f, 0f, ZPlate - .004f);
            plate.gameObject.AddComponent<XRSimpleInteractable>();

            // Encabezado.
            MakeQuad("Encabezado", panel.transform, 0f, .084f, .304f, .016f, ZCard, HeaderColor);
            MakeText("Titulo", panel.transform, PanelTitle, .105f, TextMain, TextAlignmentOptions.Left, -.0760f, .084f, .150f, .014f, FontStyles.Bold, false);
            MakeText("Subtitulo", panel.transform, PanelSubtitle, .072f, TextMuted, TextAlignmentOptions.Right, .0760f, .084f, .150f, .014f, FontStyles.Normal, false);

            BuildProjectPage();
            for (int a = 0; a < 2; a++) BuildApartado(a, a == 0 ? .038f : -.036f);

            // Pie: estado y «Presentar proyecto».
            status = MakeText("Estado", panel.transform, "", .085f, TextMuted, TextAlignmentOptions.Left, -.0535f, -.083f, .197f, .014f, FontStyles.Normal, false);
            presentButton = MakeButton("Presentar", panel.transform, .0995f, -.083f, .105f, .016f, PresentLabel, .095f, PresentIdle, OnPresent);

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
            MakeText("Nombre", page, ProjectName, .078f, PageText, TextAlignmentOptions.Left, 0f, .063f, .070f, .011f, FontStyles.Bold, false);
            MakeQuad("Separador", page, 0f, .0555f, .068f, .0015f, ZItem, SkeletonDone);

            for (int i = 0; i < Sections.Length; i++)
            {
                float top = .048f - i * .0335f;
                sectionDots[i] = MakeQuad("Estado_" + (i + 1), page, -.0305f, top - .0045f, .0065f, .0065f, ZItem, DotDone);
                MakeText("Seccion_" + (i + 1), page, Sections[i], .064f, PageText, TextAlignmentOptions.Left, .0035f, top - .0045f, .062f, .009f, FontStyles.Normal, false);
                sectionBars[i] = new[]
                {
                    MakeQuad("Linea_" + (i + 1) + "a", page, 0f, top - .0135f, .064f, .002f, ZItem, SkeletonDone),
                    MakeQuad("Linea_" + (i + 1) + "b", page, -.008f, top - .0185f, .048f, .002f, ZItem, SkeletonDone),
                };
            }
        }

        void BuildApartado(int a, float centerY)
        {
            const float width = .220f, height = .070f, centerX = .042f;
            var group = new GameObject("Apartado_" + (a + 1)).transform;
            group.SetParent(panel.transform, false);
            group.localPosition = new Vector3(centerX, centerY, 0f);

            MakeQuad("Tarjeta", group, 0f, 0f, width, height, ZCard, CardColor);
            MakeQuad("Acento", group, -width / 2f + .001f, 0f, .002f, height, ZItem, Amber);

            var observation = MakeText("Observacion", group, Observations[a], .09f, TextMain, TextAlignmentOptions.Left, .002f, .0285f, .212f, .011f, FontStyles.Bold, false);
            observation.enableAutoSizing = true;
            observation.fontSizeMin = .07f;
            observation.fontSizeMax = .09f;

            stems[a] = MakeText("Apartado", group, "", .088f, TextMain, TextAlignmentOptions.TopLeft, .002f, .0105f, .212f, .020f, FontStyles.Normal, true);

            for (int o = 0; o < 3; o++)
            {
                int apartado = a, option = o;
                chips[a, o] = MakeButton("Opcion_" + Letter(o), group, .002f + (o - 1) * .0715f, -.019f, .0675f, .026f,
                    Options[a][o], .078f, ChipIdle, () => OnOption(apartado, option));
            }
        }

        PanelButton MakeButton(string name, Transform parent, float x, float y, float w, float h, string text, float fontSize, Color idle, System.Action onSelect)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(x, y, 0f);

            var button = new PanelButton { root = root, idle = idle };
            button.background = MakeQuad("Fondo", root.transform, 0f, 0f, w, h, ZItem, idle);
            button.label = MakeText("Texto", root.transform, text, fontSize, TextMain, TextAlignmentOptions.Center, 0f, 0f, w - .004f, h - .002f, FontStyles.Bold, true);

            // El collider va antes que el interactable: XRI toma los colliders al despertar.
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(w, h, ColliderDepth);
            collider.center = new Vector3(0f, 0f, ZItem + ColliderOffset);
            var interactable = root.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => onSelect());
            interactable.hoverEntered.AddListener(_ => { button.hovered = true; Apply(button); });
            interactable.hoverExited.AddListener(_ => { button.hovered = false; Apply(button); });
            return button;
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
            var overlayBackground = transform.parent != null ? transform.parent.Find("Overlay/Fondo") : null;
            var renderer = overlayBackground != null ? overlayBackground.GetComponent<Renderer>() : null;
            if (renderer != null && renderer.sharedMaterial != null) return renderer.sharedMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            return shader != null ? new Material(shader) : null;
        }

        // Quad que mira hacia -Z, como el Quad integrado y el Overlay existente.
        static Mesh Quad()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "Act1Correccion_Quad" };
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
