using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrisisVR
{
    /// <summary>
    /// Interfaz de entrega del proyecto en la pantalla de la laptop del Acto 1 (ANTES del primer envío).
    /// Con la laptop abierta y el acto esperando la entrega (Act1Flow.AwaitingDelivery) muestra los datos del proyecto,
    /// un visor del documento (ilustrativo), tres requisitos seleccionables y «ENVIAR PROYECTO». El envío pide una
    /// confirmación y, al confirmar, llama a Act1Flow.Deliver(): continúa el flujo existente (Miguel comunica 08/20 y sus
    /// observaciones) y después aparece el panel de corrección (Act1ProjectCorrection), que no se toca.
    /// Todo se maneja con el rayo y el gatillo (XRSimpleInteractable). Sin teclado, sin escritura libre, sin archivos reales.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Act1ProjectSubmission : MonoBehaviour
    {
        // PROPUESTA – VALIDAR CON CLIENTA: todos los textos de este panel (incluidos curso y nombre del estudiante).
        const string PanelTitle = "Entrega de proyecto";
        const string PanelSubtitle = "Portal académico";
        const string DataHeading = "DATOS DEL PROYECTO";
        static readonly string[] DataLabels = { "Proyecto", "Curso", "Estudiante" };
        static readonly string[] DataValues = { "Proyecto final", "Ingeniería de Software", "Participante" };
        const string DocHeading = "DOCUMENTO DEL PROYECTO";
        const string DocFile = "proyecto_final.pdf";
        const string DocUnread = "Sin revisar";
        const string DocRead = "Revisado";
        const string OpenLabel = "ABRIR DOCUMENTO";
        const string CloseLabel = "VOLVER A LA ENTREGA";
        const string ReqHeading = "REQUISITOS DE ENTREGA";
        static readonly string[] ReqTitles =
        {
            "Justificación del diseño",
            "Evidencias de funcionamiento",
            "Archivo del proyecto adjunto",
        };
        static readonly string[] ReqDetails =
        {
            "Explica por qué tomaste tus decisiones.",
            "Pruebas o capturas del proyecto en uso.",
            "proyecto_final.zip",
        };
        const string SendLabel = "ENVIAR PROYECTO";
        const string StatusForm = "Revisa el documento y marca los requisitos antes de enviar.";
        const string StatusViewer = "Vista previa del documento del proyecto.";
        const string StatusModal = "Confirma el envío para continuar.";
        const string ViewerTitle = "Vista previa";
        const string ViewerBody = "Revisa el contenido de tu proyecto antes de enviarlo.";
        const string ViewerNote = "Documento ilustrativo del prototipo.";
        const string ModalTitle = "Confirmar envío";
        const string ModalBody = "Vas a enviar «Proyecto final» para que el profesor lo revise.";
        const string ModalWarning = "Hay requisitos sin marcar.";
        const string CancelLabel = "CANCELAR";
        const string ConfirmLabel = "CONFIRMAR ENVÍO";
        static readonly string[] Sections = { "1. Introducción", "2. Diseño", "3. Implementación", "4. Pruebas" };

        // Misma paleta que Act1ProjectCorrection (guía v1.0): azul = activo, celeste = el rayo lo apunta.
        static readonly Color PlateColor = new Color(.040f, .060f, .090f);
        static readonly Color ModalPlateColor = new Color(.020f, .030f, .050f);
        static readonly Color HeaderColor = new Color(.075f, .105f, .145f);
        static readonly Color CardColor = new Color(.085f, .115f, .155f);
        static readonly Color Amber = new Color(.78f, .50f, .12f);
        static readonly Color TextMain = new Color(.93f, .95f, .97f);
        static readonly Color TextMuted = new Color(.62f, .70f, .78f);
        static readonly Color TextDark = new Color(.04f, .09f, .15f);
        static readonly Color DetailDark = new Color(.14f, .22f, .32f);
        static readonly Color ChipIdle = new Color(.13f, .38f, .78f);
        static readonly Color ChipHover = new Color(.42f, .76f, .95f);
        static readonly Color NeutralIdle = new Color(.22f, .30f, .40f);
        static readonly Color SendIdle = new Color(.10f, .55f, .36f);
        static readonly Color RowIdle = new Color(.105f, .140f, .185f);
        static readonly Color RowOn = new Color(.070f, .200f, .170f);
        static readonly Color DotDone = new Color(.15f, .65f, .40f);
        static readonly Color PageColor = new Color(.93f, .94f, .95f);
        static readonly Color PageText = new Color(.10f, .14f, .20f);
        static readonly Color PageMuted = new Color(.38f, .44f, .52f);
        static readonly Color SkeletonLine = new Color(.78f, .83f, .87f);
        const string AmberHex = "#FFB84D";
        const string MintHex = "#9EFFC7";

        // Espacio local de la pantalla (metros). El espectador mira desde -Z, como el Overlay existente.
        // Capas: base (formulario y visor) y modal (confirmación), delante de la base.
        const float ZPlate = -.0045f, ZCard = -.0060f, ZItem = -.0075f, ZText = -.0090f;
        const float ZModalPlate = -.0100f, ZModalCard = -.0115f, ZModalItem = -.0130f;
        // Los colliders de los botones llegan más adelante que la tapa agarrable para que el rayo nunca agarre la tapa por error.
        const float ColliderDepth = .012f;
        const float ColliderOffset = -.004f;

        sealed class Control
        {
            public GameObject root;
            public Renderer background;
            public Renderer box;
            public GameObject[] marks;
            public TextMeshPro label;
            public TextMeshPro detail;
            public Color idle;
            public bool isRow;
            public bool hovered;
            public bool on;
        }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static Mesh quad;

        [SerializeField] Act1Flow flow;
        [Tooltip("Botón físico ENTREGAR del escritorio. Con 'Replace Physical Button' activo se oculta y se desactiva en ejecución: la entrega pasa por esta interfaz y su confirmación. Act1Flow lo sigue necesitando activo, por eso no se desactiva el objeto.")]
        [SerializeField] XRSimpleInteractable physicalDeliverButton;
        [SerializeField] bool replacePhysicalButton = true;

        Transform lid;
        Material material;
        MaterialPropertyBlock block;
        GameObject panel;
        GameObject form;
        GameObject viewer;
        GameObject modal;
        bool built;

        readonly Control[] rows = new Control[3];
        readonly List<Control> controls = new List<Control>();
        readonly List<XRSimpleInteractable> blockable = new List<XRSimpleInteractable>();
        Control openButton;
        Control sendButton;
        Control closeButton;
        Control cancelButton;
        Control confirmButton;
        TextMeshPro docStatus;
        TextMeshPro summaryTitle;
        TextMeshPro summaryHint;
        TextMeshPro status;
        TextMeshPro modalSummary;
        TextMeshPro modalWarning;

        readonly bool[] marked = new bool[3];
        bool docOpen;
        bool docReviewed;
        bool modalOpen;
        bool submitted;
        bool interactiveState = true;
        bool openedLogged;
        int docOpenCount;
        int markChanges;
        int cancelCount;
        float windowSeconds;   // desde que el acto espera la entrega
        float panelSeconds;    // con el panel visible
        float docSeconds;      // con el documento abierto

        public bool Submitted => submitted;
        public int CheckedCount => (marked[0] ? 1 : 0) + (marked[1] ? 1 : 0) + (marked[2] ? 1 : 0);
        public bool DocumentReviewed => docReviewed;
        public int DocumentOpenCount => docOpenCount;
        public float WindowSeconds => windowSeconds;

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
                Debug.LogError("Act1ProjectSubmission necesita Act1Flow y la tapa de la laptop (Act1LaptopHingeGrabTransformer).", this);
                enabled = false;
                return;
            }
            Build();
            if (built) ReplacePhysicalButton();
        }

        void OnDisable()
        {
            if (panel != null) panel.SetActive(false);
        }

        void ReplacePhysicalButton()
        {
            if (!replacePhysicalButton || physicalDeliverButton == null) return;
            physicalDeliverButton.enabled = false;
            foreach (var r in physicalDeliverButton.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (var c in physicalDeliverButton.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }

        void Update()
        {
            if (!built) return;

            bool awaiting = flow.AwaitingDelivery && !submitted;
            if (awaiting) windowSeconds += Time.deltaTime;

            bool show = awaiting && IsLidOpen();
            if (show != panel.activeSelf)
            {
                panel.SetActive(show);
                if (!show) ResetTransient();
            }
            if (!show) return;

            panelSeconds += Time.deltaTime;
            if (docOpen) docSeconds += Time.deltaTime;
            if (!openedLogged)
            {
                openedLogged = true;
                Log("ACTO1_ENTREGA_PANEL_ABIERTO", "t=" + Seconds(windowSeconds));
            }
        }

        bool IsLidOpen() => Quaternion.Angle(lid.localRotation, Quaternion.identity) > 5f;

        // Si se cierra la tapa se abandona la confirmación y el visor; las marcas de requisitos se conservan.
        void ResetTransient()
        {
            docOpen = false;
            modalOpen = false;
            Refresh();
        }

        // ---------------------------------------------------------------- interacción

        bool Blocked => submitted || GamePause.IsPaused || !panel.activeInHierarchy;

        void OnRow(int index)
        {
            if (Blocked || modalOpen || docOpen) return;
            marked[index] = !marked[index];
            markChanges++;
            Log("ACTO1_ENTREGA_REQUISITO",
                "requisito=" + (index + 1) + " marcado=" + (marked[index] ? "si" : "no") + " total=" + CheckedCount + " t=" + Seconds(windowSeconds));
            Refresh();
        }

        void OnOpenDocument()
        {
            if (Blocked || modalOpen || docOpen) return;
            docOpen = true;
            docReviewed = true;
            docOpenCount++;
            Log("ACTO1_ENTREGA_DOCUMENTO_ABIERTO", "veces=" + docOpenCount + " t=" + Seconds(windowSeconds));
            Refresh();
        }

        void OnCloseDocument()
        {
            if (Blocked || modalOpen || !docOpen) return;
            docOpen = false;
            Log("ACTO1_ENTREGA_DOCUMENTO_CERRADO", "tiempo_documento=" + Seconds(docSeconds));
            Refresh();
        }

        void OnSend()
        {
            if (Blocked || modalOpen || docOpen) return;
            modalOpen = true;
            Log("ACTO1_ENTREGA_CONFIRMACION_ABIERTA", Summary());
            Refresh();
        }

        void OnCancel()
        {
            if (Blocked || !modalOpen) return;
            modalOpen = false;
            cancelCount++;
            Log("ACTO1_ENTREGA_CANCELADA", "cancelaciones=" + cancelCount + " " + Summary());
            Refresh();
        }

        void OnConfirm()
        {
            // Mismas condiciones que Act1Flow.Deliver(): lo registrado aquí siempre coincide con el flujo.
            if (submitted || !modalOpen || GamePause.IsPaused || !flow.AwaitingDelivery) return;
            submitted = true;
            modalOpen = false;
            Log("ACTO1_ENTREGA_CONFIRMADA", Summary());
            // El panel se oculta en el siguiente Update (submitted); Deliver() registra ACTO1_ENTREGA_ENVIADA y sigue el flujo.
            flow.Deliver();
        }

        string Summary() =>
            "requisitos=" + CheckedCount + "/3 [" + Flag(0) + Flag(1) + Flag(2) + "] documento=" + (docReviewed ? "revisado" : "sin_revisar") +
            " aperturas_doc=" + docOpenCount + " cambios=" + markChanges +
            " tiempo_total=" + Seconds(windowSeconds) + " tiempo_panel=" + Seconds(panelSeconds) + " tiempo_documento=" + Seconds(docSeconds);

        string Flag(int index) => marked[index] ? "x" : "-";
        static string Seconds(float s) => s.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        static void Log(string eventName, string detail) => SessionRuntime.LogEvent(eventName + " " + detail);

        // ---------------------------------------------------------------- estado visual

        void Refresh()
        {
            int count = CheckedCount;
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].on = marked[i];
                Apply(rows[i]);
            }

            docStatus.text = docReviewed ? "<color=" + MintHex + ">" + DocRead + "</color>" : DocUnread;
            summaryTitle.text = "Requisitos: " + count + " de 3";
            summaryHint.text = count == 3 ? "Todo listo para enviar." : "Revisa antes de enviar.";
            modalSummary.text = "Requisitos marcados: " + count + " de 3.";
            modalWarning.text = count < 3 ? "<color=" + AmberHex + ">" + ModalWarning + "</color>" : string.Empty;
            status.text = modalOpen ? StatusModal : docOpen ? StatusViewer : StatusForm;

            form.SetActive(!docOpen);
            viewer.SetActive(docOpen);
            modal.SetActive(modalOpen);
            SetInteractive(!modalOpen);
            ClearHover();
        }

        // Con la confirmación abierta, los controles de atrás no responden.
        void SetInteractive(bool value)
        {
            if (interactiveState == value) return;
            interactiveState = value;
            foreach (var interactable in blockable)
                if (interactable != null) interactable.enabled = value;
        }

        void ClearHover()
        {
            foreach (var control in controls)
            {
                if (!control.hovered) continue;
                control.hovered = false;
                Apply(control);
            }
        }

        void Apply(Control c)
        {
            Color fill = c.hovered ? ChipHover : c.isRow ? (c.on ? RowOn : RowIdle) : c.idle;
            Paint(c.background, fill);
            c.label.color = c.hovered ? TextDark : TextMain;
            if (c.detail != null) c.detail.color = c.hovered ? DetailDark : TextMuted;
            if (c.box != null)
            {
                Paint(c.box, c.on ? DotDone : PlateColor);
                foreach (var mark in c.marks) mark.SetActive(c.on);
            }
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
                Debug.LogError("Act1ProjectSubmission: no se encontró el material de la pantalla.", this);
                enabled = false;
                return;
            }
            block = new MaterialPropertyBlock();

            panel = new GameObject("Panel_Entrega");
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
            MakeText("Titulo", panel.transform, PanelTitle, .105f, TextMain, TextAlignmentOptions.Left, -.0760f, .084f, .150f, .014f, FontStyles.Bold, false, ZText);
            MakeText("Subtitulo", panel.transform, PanelSubtitle, .072f, TextMuted, TextAlignmentOptions.Right, .0760f, .084f, .150f, .014f, FontStyles.Normal, false, ZText);

            form = new GameObject("Formulario");
            form.transform.SetParent(panel.transform, false);
            BuildData(form.transform);
            BuildDocumentCard(form.transform);
            BuildRequirements(form.transform);
            BuildSummary(form.transform);

            viewer = new GameObject("Visor_Documento");
            viewer.transform.SetParent(panel.transform, false);
            BuildViewer(viewer.transform);

            modal = new GameObject("Confirmacion");
            modal.transform.SetParent(panel.transform, false);
            BuildModal(modal.transform);

            status = MakeText("Estado", panel.transform, string.Empty, .075f, TextMuted, TextAlignmentOptions.Left, 0f, -.0875f, .300f, .012f, FontStyles.Normal, false, ZText);

            built = true;
            Refresh();
            panel.SetActive(false);
        }

        void BuildData(Transform parent)
        {
            MakeQuad("Tarjeta_Datos", parent, -.084f, .043f, .136f, .054f, ZCard, CardColor);
            MakeText("Datos_Titulo", parent, DataHeading, .060f, TextMuted, TextAlignmentOptions.Left, -.081f, .0635f, .130f, .008f, FontStyles.Bold, false, ZText);
            for (int i = 0; i < DataLabels.Length; i++)
            {
                float y = .047f - i * .0115f;
                MakeText("Dato_Etiqueta_" + (i + 1), parent, DataLabels[i], .062f, TextMuted, TextAlignmentOptions.Left, -.128f, y, .036f, .009f, FontStyles.Normal, false, ZText);
                MakeText("Dato_Valor_" + (i + 1), parent, DataValues[i], .070f, TextMain, TextAlignmentOptions.Left, -.063f, y, .090f, .009f, FontStyles.Normal, false, ZText);
            }
        }

        void BuildDocumentCard(Transform parent)
        {
            MakeQuad("Tarjeta_Documento", parent, -.084f, -.031f, .136f, .086f, ZCard, CardColor);
            MakeText("Documento_Titulo", parent, DocHeading, .060f, TextMuted, TextAlignmentOptions.Left, -.081f, .0055f, .130f, .008f, FontStyles.Bold, false, ZText);

            // Miniatura de la primera página.
            MakeQuad("Miniatura", parent, -.125f, -.0385f, .042f, .064f, ZItem, PageColor);
            MakeQuad("Miniatura_Titulo", parent, -.1325f, -.0135f, .026f, .004f, ZItem - .0004f, PageMuted);
            float[] lines = { -.021f, -.025f, -.029f, -.0375f, -.0415f, -.0455f, -.0545f, -.0585f };
            for (int i = 0; i < lines.Length; i++)
                MakeQuad("Miniatura_Linea_" + (i + 1), parent, -.125f, lines[i], i % 4 == 3 ? .020f : .030f, .0018f, ZItem - .0004f, SkeletonLine);

            MakeText("Documento_Archivo", parent, DocFile, .066f, TextMain, TextAlignmentOptions.Left, -.059f, -.019f, .078f, .010f, FontStyles.Bold, false, ZText);
            docStatus = MakeText("Documento_Estado", parent, DocUnread, .058f, TextMuted, TextAlignmentOptions.Left, -.059f, -.028f, .078f, .009f, FontStyles.Normal, false, ZText);
            openButton = MakeButton("Abrir_Documento", parent, -.059f, -.050f, .074f, .022f, OpenLabel, .062f, ChipIdle, ZItem, OnOpenDocument, true);
        }

        void BuildRequirements(Transform parent)
        {
            MakeQuad("Tarjeta_Requisitos", parent, .070f, .020f, .164f, .100f, ZCard, CardColor);
            MakeText("Requisitos_Titulo", parent, ReqHeading, .060f, TextMuted, TextAlignmentOptions.Left, .069f, .0625f, .150f, .008f, FontStyles.Bold, false, ZText);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                rows[i] = MakeRow("Requisito_" + (i + 1), parent, .070f, .042f - i * .028f, ReqTitles[i], ReqDetails[i], () => OnRow(index));
            }
        }

        void BuildSummary(Transform parent)
        {
            MakeQuad("Tarjeta_Resumen", parent, .070f, -.054f, .164f, .040f, ZCard, CardColor);
            summaryTitle = MakeText("Resumen_Titulo", parent, string.Empty, .068f, TextMain, TextAlignmentOptions.Left, .033f, -.0475f, .078f, .010f, FontStyles.Bold, false, ZText);
            summaryHint = MakeText("Resumen_Detalle", parent, string.Empty, .056f, TextMuted, TextAlignmentOptions.Left, .033f, -.0600f, .078f, .009f, FontStyles.Normal, false, ZText);
            sendButton = MakeButton("Enviar_Proyecto", parent, .113f, -.054f, .070f, .022f, SendLabel, .070f, SendIdle, ZItem, OnSend, true);
        }

        void BuildViewer(Transform parent)
        {
            // Página del documento (ilustrativa): título y cuatro secciones.
            MakeQuad("Hoja", parent, -.058f, -.002f, .150f, .142f, ZCard, PageColor);
            MakeText("Hoja_Titulo", parent, "Proyecto final", .090f, PageText, TextAlignmentOptions.Left, -.062f, .0585f, .130f, .011f, FontStyles.Bold, false, ZText);
            MakeText("Hoja_Subtitulo", parent, "Documento del proyecto", .058f, PageMuted, TextAlignmentOptions.Left, -.062f, .0485f, .130f, .009f, FontStyles.Normal, false, ZText);
            MakeQuad("Hoja_Separador", parent, -.058f, .0435f, .138f, .0015f, ZItem, SkeletonLine);
            for (int i = 0; i < Sections.Length; i++)
            {
                float top = .037f - i * .028f;
                MakeText("Hoja_Seccion_" + (i + 1), parent, Sections[i], .068f, PageText, TextAlignmentOptions.Left, -.067f, top - .003f, .120f, .009f, FontStyles.Bold, false, ZText);
                MakeQuad("Hoja_Linea_" + (i + 1) + "a", parent, -.062f, top - .0115f, .130f, .002f, ZItem, SkeletonLine);
                MakeQuad("Hoja_Linea_" + (i + 1) + "b", parent, -.077f, top - .0170f, .100f, .002f, ZItem, SkeletonLine);
            }

            // Barra lateral.
            MakeQuad("Lateral", parent, .089f, -.002f, .126f, .142f, ZCard, CardColor);
            MakeText("Lateral_Titulo", parent, ViewerTitle, .080f, TextMain, TextAlignmentOptions.Left, .089f, .0575f, .114f, .011f, FontStyles.Bold, false, ZText);
            MakeText("Lateral_Texto", parent, ViewerBody, .062f, TextMuted, TextAlignmentOptions.TopLeft, .089f, .036f, .114f, .030f, FontStyles.Normal, true, ZText);
            MakeText("Lateral_Nota", parent, ViewerNote, .056f, TextMuted, TextAlignmentOptions.TopLeft, .089f, .010f, .114f, .020f, FontStyles.Italic, true, ZText);
            closeButton = MakeButton("Cerrar_Documento", parent, .089f, -.055f, .108f, .022f, CloseLabel, .062f, ChipIdle, ZItem, OnCloseDocument, true);
        }

        void BuildModal(Transform parent)
        {
            // Placa sólida que cubre la interfaz de atrás; los controles de atrás se desactivan mientras está abierta.
            MakeQuad("Placa_Modal", parent, 0f, 0f, .32f, .20f, ZModalPlate, ModalPlateColor);
            MakeQuad("Dialogo", parent, 0f, 0f, .190f, .100f, ZModalCard, CardColor);
            MakeQuad("Dialogo_Acento", parent, -.094f, 0f, .002f, .100f, ZModalItem, Amber);

            float zText = ZModalItem - .0015f;
            MakeText("Dialogo_Titulo", parent, ModalTitle, .095f, TextMain, TextAlignmentOptions.Left, .002f, .036f, .176f, .012f, FontStyles.Bold, false, zText);
            MakeText("Dialogo_Texto", parent, ModalBody, .072f, TextMain, TextAlignmentOptions.TopLeft, .002f, .018f, .176f, .020f, FontStyles.Normal, true, zText);
            modalSummary = MakeText("Dialogo_Resumen", parent, string.Empty, .070f, TextMuted, TextAlignmentOptions.Left, .002f, -.0045f, .176f, .010f, FontStyles.Bold, false, zText);
            modalWarning = MakeText("Dialogo_Aviso", parent, string.Empty, .070f, TextMain, TextAlignmentOptions.Left, .002f, -.0145f, .176f, .010f, FontStyles.Normal, false, zText);

            cancelButton = MakeButton("Cancelar", parent, -.042f, -.0355f, .070f, .022f, CancelLabel, .066f, NeutralIdle, ZModalItem, OnCancel, false);
            confirmButton = MakeButton("Confirmar_Envio", parent, .050f, -.0355f, .082f, .022f, ConfirmLabel, .064f, SendIdle, ZModalItem, OnConfirm, false);
        }

        Control MakeButton(string name, Transform parent, float x, float y, float w, float h, string text, float fontSize, Color idle,
            float zItem, System.Action onSelect, bool blocksWithModal)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(x, y, 0f);

            var control = new Control { root = root, idle = idle };
            control.background = MakeQuad("Fondo", root.transform, 0f, 0f, w, h, zItem, idle);
            control.label = MakeText("Texto", root.transform, text, fontSize, TextMain, TextAlignmentOptions.Center, 0f, 0f, w - .004f, h - .002f, FontStyles.Bold, true, zItem - .0015f);
            AttachInteractable(control, root, w, h, zItem, onSelect, blocksWithModal);
            controls.Add(control);
            return control;
        }

        Control MakeRow(string name, Transform parent, float x, float y, string title, string detail, System.Action onSelect)
        {
            const float w = .156f, h = .024f;
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(x, y, 0f);

            var control = new Control { root = root, idle = RowIdle, isRow = true };
            control.background = MakeQuad("Fondo", root.transform, 0f, 0f, w, h, ZItem, RowIdle);

            // Casilla de verificación: marco, interior y marca de verificación (dos trazos).
            const float boxX = -.064f;
            MakeQuad("Casilla_Marco", root.transform, boxX, 0f, .0105f, .0105f, ZItem - .0004f, TextMuted);
            control.box = MakeQuad("Casilla", root.transform, boxX, 0f, .0085f, .0085f, ZItem - .0008f, PlateColor);
            var shortStroke = MakeQuad("Marca_A", root.transform, boxX - .0017f, -.00115f, .0026f, .0010f, ZItem - .0012f, TextMain);
            shortStroke.transform.localRotation = Quaternion.Euler(0f, 0f, -46.5f);
            var longStroke = MakeQuad("Marca_B", root.transform, boxX + .00095f, .00005f, .0055f, .0010f, ZItem - .0012f, TextMain);
            longStroke.transform.localRotation = Quaternion.Euler(0f, 0f, 51f);
            control.marks = new[] { shortStroke.gameObject, longStroke.gameObject };

            float zText = ZItem - .0015f;
            control.label = MakeText("Titulo", root.transform, title, .072f, TextMain, TextAlignmentOptions.Left, .011f, .0052f, .126f, .010f, FontStyles.Bold, false, zText);
            control.detail = MakeText("Detalle", root.transform, detail, .060f, TextMuted, TextAlignmentOptions.Left, .011f, -.0054f, .126f, .009f, FontStyles.Normal, false, zText);
            AttachInteractable(control, root, w, h, ZItem, onSelect, true);
            controls.Add(control);
            return control;
        }

        void AttachInteractable(Control control, GameObject root, float w, float h, float zItem, System.Action onSelect, bool blocksWithModal)
        {
            // El collider va antes que el interactable: XRI toma los colliders al despertar.
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(w, h, ColliderDepth);
            collider.center = new Vector3(0f, 0f, zItem + ColliderOffset);
            var interactable = root.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => onSelect());
            interactable.hoverEntered.AddListener(_ => { control.hovered = true; Apply(control); });
            interactable.hoverExited.AddListener(_ => { control.hovered = false; Apply(control); });
            if (blocksWithModal) blockable.Add(interactable);
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
            float x, float y, float w, float h, FontStyles style, bool wrap, float z)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, z);
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
            quad = new Mesh { name = "Act1Entrega_Quad" };
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
