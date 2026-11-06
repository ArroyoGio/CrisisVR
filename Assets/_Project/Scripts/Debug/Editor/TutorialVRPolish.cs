using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrisisVR.Editor
{
    // Pulido visual VR de la escena Tutorial. Idempotente y limitado a Tutorial.unity:
    // crea sprites y materiales NUEVOS (no toca los compartidos con MainMenu/Welcome) y
    // no modifica TutorialFlow, TutorialInputRouter ni ninguna otra escena.
    public static class TutorialVRPolish
    {
        const string Root = "Assets/_Project/";
        const string UiDir = Root + "Art/UI/Tutorial/";
        const string MatDir = Root + "Materials/TutorialVR/";

        class Sprites { public Sprite round, outline, circle, pill, check, lockIcon, info, ring, chevron; }
        class Mats { public Material floor, carpet, wall, led, pedTop, pedBody, cueN, cueH, cueA, outline; }

        [MenuItem("CrisisVR/T-23/Pulido visual VR del tutorial")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Tutorial.unity")
                throw new InvalidOperationException("Pulido VR: requiere la escena Tutorial activa, fuera de Play.");
            var flow = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TutorialFlow>(true)).Single();
            Directory.CreateDirectory(UiDir);
            Directory.CreateDirectory(MatDir);
            var sp = BuildSprites();
            var mt = BuildMaterials();
            StylePanel(flow, sp);
            BuildWorld(scene, flow, sp, mt);
            RemapEnvironment(scene, mt);
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("PULIDO_VR_TUTORIAL: aplicado");
        }

        // ---------- Sprites procedurales (SDF, sin dependencias externas) ----------
        static float Cov(float sd) { return Mathf.Clamp01(0.5f - sd); }
        static float Len(float x, float y) { return Mathf.Sqrt(x * x + y * y); }
        static float SdBox(float px, float py, float hx, float hy, float r)
        {
            float dx = Mathf.Abs(px) - (hx - r), dy = Mathf.Abs(py) - (hy - r);
            float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }
        static float SdSeg(float px, float py, float ax, float ay, float bx, float by)
        {
            float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            return Len(pax - bax * h, pay - bay * h);
        }

        static Sprite Make(string name, int w, int h, Func<float, float, float> alpha, Vector4 border)
        {
            string path = UiDir + name + ".png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f)) * 255f);
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.spriteBorder = border;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprites BuildSprites()
        {
            var s = new Sprites();
            var b48 = new Vector4(48, 48, 48, 48);
            s.round = Make("ui_round", 128, 128, (x, y) => Cov(SdBox(x - 64, y - 64, 63.5f, 63.5f, 44f)), b48);
            s.outline = Make("ui_round_outline", 128, 128, (x, y) =>
            {
                float sd = SdBox(x - 64, y - 64, 63.5f, 63.5f, 44f);
                return Mathf.Clamp01(Cov(sd) - Cov(sd + 7f));
            }, b48);
            s.circle = Make("ui_circle", 128, 128, (x, y) => Cov(Len(x - 64, y - 64) - 63.5f), Vector4.zero);
            s.pill = Make("ui_pill", 64, 16, (x, y) => Cov(SdBox(x - 32, y - 8, 31.5f, 7.5f, 7.5f)), Vector4.zero);
            s.check = Make("ui_check", 96, 96, (x, y) =>
                Cov(Mathf.Min(SdSeg(x, y, 20, 50, 40, 28), SdSeg(x, y, 40, 28, 76, 70)) - 6.5f), Vector4.zero);
            s.lockIcon = Make("ui_lock", 96, 96, (x, y) =>
            {
                float body = SdBox(x - 48, y - 34, 27, 21, 6);
                float ring = y < 56 ? 999f : Mathf.Abs(Len(x - 48, y - 56) - 17f) - 5.5f;
                float legL = SdSeg(x, y, 30.5f, 56, 30.5f, 46) - 5.5f;
                float legR = SdSeg(x, y, 65.5f, 56, 65.5f, 46) - 5.5f;
                float sd = Mathf.Min(body, Mathf.Min(ring, Mathf.Min(legL, legR)));
                float hole = Mathf.Min(Len(x - 48, y - 37) - 5.5f, SdSeg(x, y, 48, 37, 48, 27) - 3f);
                return Cov(sd) * (1f - Cov(hole));
            }, Vector4.zero);
            s.info = Make("ui_info", 96, 96, (x, y) =>
            {
                float ring = Mathf.Abs(Len(x - 48, y - 48) - 40f) - 3.5f;
                float dot = Len(x - 48, y - 68) - 5.5f;
                float stem = SdSeg(x, y, 48, 30, 48, 52) - 4.5f;
                return Cov(Mathf.Min(ring, Mathf.Min(dot, stem)));
            }, Vector4.zero);
            s.ring = Make("ui_ring_soft", 256, 256, (x, y) =>
            {
                float r = Len(x - 128, y - 128);
                float d = Mathf.Abs(r - 88f);
                float core = Mathf.Exp(-(d / 7f) * (d / 7f));
                float glow = 0.22f * Mathf.Exp(-(d / 26f) * (d / 26f));
                float edge = 1f - Mathf.SmoothStep(108f, 127f, r);
                return Mathf.Max(core, glow) * edge;
            }, Vector4.zero);
            s.chevron = Make("ui_chevron_down", 128, 128, (x, y) =>
                Cov(Mathf.Min(SdSeg(x, y, 22, 84, 64, 42), SdSeg(x, y, 64, 42, 106, 84)) - 11f), Vector4.zero);
            return s;
        }

        // ---------- Materiales nuevos (los existentes se comparten con otras escenas) ----------
        static Material GetOrCreate(string name, Func<Material> create)
        {
            string path = MatDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = create(); AssetDatabase.CreateAsset(m, path); }
            return m;
        }
        static Material Lit(string name, string sourcePath, Color c, float smoothness)
        {
            var m = GetOrCreate(name, () =>
            {
                var src = sourcePath != null ? AssetDatabase.LoadAssetAtPath<Material>(sourcePath) : null;
                return src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            });
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }
        static Material Unlit(string name, Color c, bool cullFront)
        {
            var m = GetOrCreate(name, () => new Material(Shader.Find("Universal Render Pipeline/Unlit")));
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", cullFront ? 1f : 2f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Mats BuildMaterials()
        {
            var t = Root + "Materials/Tutorial/";
            return new Mats
            {
                floor = Lit("TVR_Suelo", t + "Suelo roble suave.mat", new Color(0.30f, 0.24f, 0.19f), 0.25f),
                carpet = Lit("TVR_Alfombra", t + "Alfombra arena.mat", new Color(0.13f, 0.19f, 0.25f), 0.05f),
                wall = Lit("TVR_Pared", t + "Pared marfil.mat", new Color(0.74f, 0.79f, 0.80f), 0.1f),
                led = Unlit("TVR_LedTurquesa", new Color(0.12f, 0.62f, 0.68f), false),
                pedTop = Lit("TVR_PedestalTapa", null, new Color(0.11f, 0.16f, 0.21f), 0.4f),
                pedBody = Lit("TVR_PedestalCuerpo", null, new Color(0.07f, 0.11f, 0.15f), 0.3f),
                cueN = Unlit("TVR_CueNeutro", new Color(1f, 0.72f, 0.20f), false),
                cueH = Unlit("TVR_CueHover", new Color(1f, 0.93f, 0.62f), false),
                cueA = Unlit("TVR_CueActivo", new Color(0.30f, 0.90f, 0.62f), false),
                outline = Unlit("TVR_Contorno", new Color(1f, 0.97f, 0.85f), true)
            };
        }

        // ---------- Panel flotante ----------
        static RectTransform Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
            r.localScale = Vector3.one;
            return r;
        }
        static void Stretch(RectTransform r, float inset)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(inset, inset); r.offsetMax = new Vector2(-inset, -inset);
            r.localScale = Vector3.one;
        }
        static Image NewImage(string name, Transform parent, Sprite sprite, Image.Type type, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var im = go.GetComponent<Image>();
            im.sprite = sprite; im.type = type; im.color = color; im.raycastTarget = false;
            return im;
        }
        static void Remove(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
        }
        static void Text(TMP_Text t, float size, TextAlignmentOptions align, Color? color = null, FontStyles style = FontStyles.Normal)
        {
            t.enableAutoSizing = false; t.fontSize = size; t.alignment = align; t.fontStyle = style;
            if (color.HasValue) t.color = color.Value;
            t.raycastTarget = false;
        }

        static void StyleButton(Button b, Sprites sp, Color bg, float x, float y, float w, float h, float font, float hoverScale, FontStyles style)
        {
            Place((RectTransform)b.transform, x, y, w, h);
            var img = b.GetComponent<Image>();
            img.sprite = sp.round; img.type = Image.Type.Sliced; img.color = bg; img.pixelsPerUnitMultiplier = 1f;
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = Color.white; cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.80f, 0.88f, 0.93f, 1f); cb.disabledColor = Color.white;
            cb.colorMultiplier = 1f; cb.fadeDuration = 0.06f;
            b.colors = cb;
            var label = b.GetComponentInChildren<TMP_Text>(true);
            Stretch(label.rectTransform, 10f);
            Text(label, font, TextAlignmentOptions.Center, Color.white, style);
            Remove(b.transform, "Aro");
            var ring = NewImage("Aro", b.transform, sp.outline, Image.Type.Sliced, new Color(0.92f, 0.99f, 1f, 0.95f));
            Stretch(ring.rectTransform, -8f);
            ring.gameObject.SetActive(false);
            var fb = b.GetComponent<TutorialButtonFeedback>();
            if (fb == null) fb = b.gameObject.AddComponent<TutorialButtonFeedback>();
            fb.ring = ring; fb.hoverScale = hoverScale;
            EditorUtility.SetDirty(fb);
        }

        static void StylePanel(TutorialFlow flow, Sprites sp)
        {
            var panel = flow.panel;
            var pt = panel.transform;
            foreach (var n in new[] { "PanelBorde", "Progreso", "InsigniaAccion", "BarraMensaje", "PasoProgreso" }) Remove(pt, n);

            var teal = TutorialPanelStyle.Teal;
            var navy = new Color(0.035f, 0.07f, 0.12f, 1f);
            var slate = new Color(0.13f, 0.20f, 0.27f, 1f);
            var secondary = new Color(0.14f, 0.24f, 0.32f, 1f);

            // Fondo redondeado y borde turquesa (opaco: sin sobredibujo transparente).
            var bg = pt.Find("Background").GetComponent<Image>();
            bg.sprite = sp.round; bg.type = Image.Type.Sliced; bg.color = navy; bg.pixelsPerUnitMultiplier = 1f; bg.raycastTarget = true;
            var border = NewImage("PanelBorde", pt, sp.outline, Image.Type.Sliced, new Color(teal.r, teal.g, teal.b, 0.70f));
            Stretch(border.rectTransform, 0f);
            border.transform.SetSiblingIndex(bg.transform.GetSiblingIndex() + 1);
            var accent = pt.Find("TopAccent");
            if (accent != null) accent.gameObject.SetActive(false);

            // Cabecera: marca a la izquierda, "Paso X de Y" a la derecha, progreso segmentado debajo.
            var brand = pt.Find("Brand").GetComponent<TMP_Text>();
            Place(brand.rectTransform, 0, 398, 1480, 50);
            Text(brand, 30, TextAlignmentOptions.MidlineLeft, teal, FontStyles.Bold);
            brand.characterSpacing = 4f;

            var progressGo = new GameObject("PasoProgreso", typeof(RectTransform));
            progressGo.layer = pt.gameObject.layer; progressGo.transform.SetParent(pt, false);
            // (corregido) el texto de progreso se crea directamente como GameObject con RectTransform
            // TextMeshProUGUI agrega su propio CanvasRenderer
            var ptmp = progressGo.AddComponent<TextMeshProUGUI>();
            ptmp.font = brand.font; ptmp.fontSharedMaterial = brand.fontSharedMaterial;
            Place(ptmp.rectTransform, 0, 398, 1480, 50);
            Text(ptmp, 32, TextAlignmentOptions.MidlineRight, new Color(0.70f, 0.80f, 0.87f, 1f), FontStyles.Bold);
            ptmp.text = "Paso 1 de " + flow.exercises.Length;
            panel.progress = ptmp;

            var bar = new GameObject("Progreso", typeof(RectTransform));
            bar.layer = pt.gameObject.layer; bar.transform.SetParent(pt, false);
            Place((RectTransform)bar.transform, 0, 342, 1420, 14);
            int total = flow.exercises.Length;
            var segments = new Image[total];
            float pitch = total > 1 ? 1420f / total : 1420f;
            for (int i = 0; i < total; i++)
            {
                var seg = NewImage("Seg" + i.ToString("00"), bar.transform, sp.pill, Image.Type.Simple, new Color(0.16f, 0.23f, 0.30f, 1f));
                Place(seg.rectTransform, -710f + pitch * (i + 0.5f), 0, pitch - 12f, 12f);
                segments[i] = seg;
            }

            // Título con la insignia de la acción a la izquierda.
            var heading = pt.Find("Heading").GetComponent<TMP_Text>();
            Place(heading.rectTransform, 100, 258, 1400, 100);
            Text(heading, 62, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold);
            var icon = pt.Find("ActionIcon").GetComponent<Image>();
            var badge = NewImage("InsigniaAccion", pt, sp.circle, Image.Type.Simple, slate);
            Place(badge.rectTransform, -675, 258, 112, 112);
            badge.transform.SetSiblingIndex(icon.transform.GetSiblingIndex());
            Place(icon.rectTransform, -675, 258, 76, 76);

            // Instrucción (columna izquierda; la derecha queda libre para la demostración 3D de mandos).
            var instruction = pt.Find("Instruction").GetComponent<TMP_Text>();
            Place(instruction.rectTransform, -380, 55, 720, 250);
            Text(instruction, 42, TextAlignmentOptions.TopLeft, new Color(0.86f, 0.92f, 0.96f, 1f));

            // Barra de mensajes: ayuda / confirmación de logro / avance de la espera fija.
            var msg = NewImage("BarraMensaje", pt, sp.round, Image.Type.Sliced, new Color(0.09f, 0.16f, 0.22f, 1f));
            msg.pixelsPerUnitMultiplier = 1f;
            Place(msg.rectTransform, -380, -120, 720, 150);
            var hint = panel.hint;
            msg.transform.SetSiblingIndex(hint.transform.GetSiblingIndex());
            Place(hint.rectTransform, -350, -120, 620, 130);
            Text(hint, 30, TextAlignmentOptions.MidlineLeft, new Color(0.86f, 0.92f, 0.96f, 1f));
            hint.enableAutoSizing = true; hint.fontSizeMin = 26f; hint.fontSizeMax = 30f;
            var msgIcon = NewImage("Icono", msg.transform, sp.info, Image.Type.Simple, teal);
            Place(msgIcon.rectTransform, -320, 0, 44, 44);
            var wait = new GameObject("EsperaPista", typeof(RectTransform));
            wait.layer = pt.gameObject.layer; wait.transform.SetParent(msg.transform, false);
            Place((RectTransform)wait.transform, 0, 0, 640, 14);
            var track = NewImage("Pista", wait.transform, sp.pill, Image.Type.Simple, new Color(0.18f, 0.27f, 0.35f, 1f));
            Stretch(track.rectTransform, 0f);
            var fill = NewImage("Avance", wait.transform, sp.pill, Image.Type.Filled, teal);
            fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 0f;
            Stretch(fill.rectTransform, 0f);
            wait.SetActive(false);
            msg.gameObject.SetActive(false);

            // Botón de práctica con halo ámbar mientras la acción está pendiente.
            var practice = panel.practice;
            StyleButton(practice, sp, secondary, -380, -250, 720, 76, 34, 1.03f, FontStyles.Bold);
            Remove(practice.transform, "Brillo");
            var glowP = NewImage("Brillo", practice.transform, sp.outline, Image.Type.Sliced, TutorialPanelStyle.Amber);
            Stretch(glowP.rectTransform, -12f);
            glowP.gameObject.SetActive(false);

            // Etiqueta de mano (columna derecha, abajo).
            var hand = panel.hand;
            Place(hand.rectTransform, 380, -250, 640, 60);
            Text(hand, 30, TextAlignmentOptions.Center, teal);

            // Botones inferiores: Ayuda y Repetir secundarios; Continuar protagonista.
            StyleButton(pt.Find("HelpButton").GetComponent<Button>(), sp, secondary, -600, -392, 260, 90, 34, 1.04f, FontStyles.Normal);
            StyleButton(pt.Find("RepeatButton").GetComponent<Button>(), sp, secondary, -310, -392, 260, 90, 34, 1.04f, FontStyles.Normal);
            var cont = panel.continueButton;
            StyleButton(cont, sp, slate, 290, -392, 640, 100, 36, 1.0f, FontStyles.Bold);
            var contLabel = cont.GetComponentInChildren<TMP_Text>(true);
            contLabel.rectTransform.offsetMin = new Vector2(76f, 8f);
            contLabel.rectTransform.offsetMax = new Vector2(-20f, -8f);
            Remove(cont.transform, "Candado"); Remove(cont.transform, "Check"); Remove(cont.transform, "Brillo");
            var lockImg = NewImage("Candado", cont.transform, sp.lockIcon, Image.Type.Simple, new Color(0.62f, 0.70f, 0.76f, 1f));
            Place(lockImg.rectTransform, -270, 0, 48, 48);
            var checkImg = NewImage("Check", cont.transform, sp.check, Image.Type.Simple, new Color(0.03f, 0.07f, 0.12f, 1f));
            Place(checkImg.rectTransform, -270, 0, 48, 48);
            var glowC = NewImage("Brillo", cont.transform, sp.outline, Image.Type.Sliced, TutorialPanelStyle.Green);
            Stretch(glowC.rectTransform, -12f);
            glowC.gameObject.SetActive(false);

            var style = pt.GetComponent<TutorialPanelStyle>();
            if (style == null) style = pt.gameObject.AddComponent<TutorialPanelStyle>();
            style.flow = flow; style.panel = panel; style.segments = segments;
            style.actionIcon = icon; style.actionBadge = badge;
            style.continueBackground = cont.GetComponent<Image>(); style.continueGlow = glowC;
            style.continueLock = lockImg; style.continueCheck = checkImg; style.continueLabel = contLabel;
            style.practiceGlow = glowP;
            style.feedbackBar = msg; style.feedbackFill = fill; style.feedbackIcon = msgIcon;
            style.infoSprite = sp.info; style.checkSprite = sp.check;
            EditorUtility.SetDirty(style);
            EditorUtility.SetDirty(panel);
        }

        // ---------- Mundo 3D: mesa de práctica, resaltado de objetos, guías de suelo y trazos LED ----------
        static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider, bool shadows)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshRenderer>().shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            go.GetComponent<MeshRenderer>().receiveShadows = shadows;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static SpriteRenderer SpriteObj(string name, Transform parent, Sprite sprite, Vector3 pos, Vector3 euler, float scale, Color color)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.color = color;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            return sr;
        }
        static Bounds EnvBounds(GameObject env, string name)
        {
            return env.GetComponentsInChildren<Renderer>(true).First(r => r.name == name).bounds;
        }

        static void BuildWorld(Scene scene, TutorialFlow flow, Sprites sp, Mats mt)
        {
            foreach (var r in scene.GetRootGameObjects()) if (r.name == "PulidoVR_Tutorial") UnityEngine.Object.DestroyImmediate(r);
            var env = scene.GetRootGameObjects().First(g => g.name == "TutorialVisualEnvironment");
            var root = new GameObject("PulidoVR_Tutorial");
            var cues = root.AddComponent<TutorialWorldCues>();
            cues.flow = flow;

            var floor = EnvBounds(env, "Piso continuo");
            var carpet = EnvBounds(env, "Alfombra de zona de trabajo");
            var front = EnvBounds(env, "Muro de fondo");
            var back = EnvBounds(env, "Muro posterior");
            var left = EnvBounds(env, "Muro izquierdo");
            var right = EnvBounds(env, "Muro derecho");
            var ceiling = EnvBounds(env, "Techo");

            // Mesa de práctica: los cubos dejan de flotar y ganan un fondo oscuro de alto contraste.
            var cueA = flow.objectCues[0].transform;
            var cueB = flow.objectCues[1].transform;
            float cx = (cueA.position.x + cueB.position.x) * 0.5f, cz = cueA.position.z;
            float top = cueA.position.y - cueA.lossyScale.y * 0.5f;
            float h = top - 0.05f - floor.max.y;
            var ped = Child(root.transform, "Mesa de practica");
            Box("Tapa", ped, new Vector3(cx, top - 0.025f, cz), new Vector3(1.30f, 0.05f, 0.62f), mt.pedTop, true, true);
            Box("Lateral izquierdo", ped, new Vector3(cx - 0.60f, floor.max.y + h * 0.5f, cz), new Vector3(0.05f, h, 0.52f), mt.pedBody, true, false);
            Box("Lateral derecho", ped, new Vector3(cx + 0.60f, floor.max.y + h * 0.5f, cz), new Vector3(0.05f, h, 0.52f), mt.pedBody, true, false);
            Box("Fondo", ped, new Vector3(cx, floor.max.y + h * 0.5f, cz + 0.24f), new Vector3(1.15f, h, 0.04f), mt.pedBody, true, false);
            Box("Canto LED", ped, new Vector3(cx, top - 0.05f + 0.006f, cz - 0.312f), new Vector3(1.30f, 0.012f, 0.012f), mt.led, false, false);

            // Trazos LED turquesa: marcan el aula y delimitan la zona de práctica (Unlit, sin luces).
            var led = Child(root.transform, "Trazos LED");
            float sy = 0.40f, ez = 0.012f;
            Box("Base frontal", led, new Vector3(front.center.x, sy, front.min.z - ez), new Vector3(front.size.x - 0.2f, 0.03f, 0.02f), mt.led, false, false);
            Box("Base posterior", led, new Vector3(back.center.x, sy, back.max.z + ez), new Vector3(back.size.x - 0.2f, 0.03f, 0.02f), mt.led, false, false);
            Box("Base izquierda", led, new Vector3(left.max.x + ez, sy, left.center.z), new Vector3(0.02f, 0.03f, left.size.z - 0.2f), mt.led, false, false);
            Box("Base derecha", led, new Vector3(right.min.x - ez, sy, right.center.z), new Vector3(0.02f, 0.03f, right.size.z - 0.2f), mt.led, false, false);
            Box("Cornisa frontal", led, new Vector3(front.center.x, ceiling.min.y - 0.08f, front.min.z - ez), new Vector3(front.size.x - 0.2f, 0.04f, 0.02f), mt.led, false, false);
            float zy = carpet.max.y + 0.006f;
            Box("Zona frente", led, new Vector3(carpet.center.x, zy, carpet.max.z), new Vector3(carpet.size.x, 0.012f, 0.04f), mt.led, false, false);
            Box("Zona fondo", led, new Vector3(carpet.center.x, zy, carpet.min.z), new Vector3(carpet.size.x, 0.012f, 0.04f), mt.led, false, false);
            Box("Zona izquierda", led, new Vector3(carpet.min.x, zy, carpet.center.z), new Vector3(0.04f, 0.012f, carpet.size.z), mt.led, false, false);
            Box("Zona derecha", led, new Vector3(carpet.max.x, zy, carpet.center.z), new Vector3(0.04f, 0.012f, carpet.size.z), mt.led, false, false);

            // Guías de suelo: referencias para Movimiento/Giro y punto de inicio.
            var origin = flow.input.playerOrigin.transform.position;
            float ry = carpet.max.y + 0.012f;
            var rings = Child(root.transform, "Guias de suelo");
            var floorRings = new[]
            {
                SpriteObj("Referencia frente", rings, sp.ring, new Vector3(origin.x, ry, origin.z + 1.3f), new Vector3(90, 0, 0), 0.30f, TutorialPanelStyle.Amber),
                SpriteObj("Referencia izquierda", rings, sp.ring, new Vector3(origin.x - 2.4f, ry, origin.z - 0.8f), new Vector3(90, 0, 0), 0.30f, TutorialPanelStyle.Amber),
                SpriteObj("Referencia derecha", rings, sp.ring, new Vector3(origin.x + 2.4f, ry, origin.z - 0.8f), new Vector3(90, 0, 0), 0.30f, TutorialPanelStyle.Amber),
                SpriteObj("Referencia atras", rings, sp.ring, new Vector3(origin.x, ry, origin.z - 2.7f), new Vector3(90, 0, 0), 0.30f, TutorialPanelStyle.Amber)
            };
            foreach (var r in floorRings) r.gameObject.SetActive(false);
            cues.floorRings = floorRings;
            cues.startRing = SpriteObj("Punto de inicio", rings, sp.ring, new Vector3(origin.x, ry - 0.002f, origin.z), new Vector3(90, 0, 0), 0.26f, TutorialPanelStyle.Teal);

            // Resaltado de los objetos interactivos: material ámbar, contorno, anillo sobre la mesa y flecha.
            var guideRoot = Child(root.transform, "Guias de objeto");
            var guides = new TutorialWorldCues.ObjectGuide[flow.objectCues.Length];
            for (int i = 0; i < flow.objectCues.Length; i++)
            {
                var cue = flow.objectCues[i];
                cue.normal = mt.cueN; cue.hover = mt.cueH; cue.activated = mt.cueA;
                if (cue.target != null) cue.target.sharedMaterial = mt.cueN;
                Remove(cue.transform, "Contorno");
                var shell = new GameObject("Contorno", typeof(MeshFilter), typeof(MeshRenderer));
                shell.layer = cue.gameObject.layer;
                shell.transform.SetParent(cue.transform, false);
                shell.transform.localScale = Vector3.one * 1.12f;
                shell.GetComponent<MeshFilter>().sharedMesh = cue.GetComponent<MeshFilter>().sharedMesh;
                var mr = shell.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mt.outline;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                var p = cue.transform.position;
                var ring = SpriteObj("Anillo " + (i + 1), guideRoot, sp.ring, new Vector3(p.x, top + 0.004f, p.z), new Vector3(90, 0, 0), 0.24f, TutorialPanelStyle.Amber);
                var marker = SpriteObj("Flecha " + (i + 1), guideRoot, sp.chevron, new Vector3(p.x, p.y + cue.transform.lossyScale.y * 0.5f + 0.22f, p.z), Vector3.zero, 0.12f, TutorialPanelStyle.Amber);
                ring.gameObject.SetActive(false); marker.gameObject.SetActive(false);
                guides[i] = new TutorialWorldCues.ObjectGuide
                {
                    cue = cue, grab = cue.GetComponent<XRGrabInteractable>(),
                    outline = shell.transform, ring = ring, marker = marker
                };
                EditorUtility.SetDirty(cue);
            }
            cues.guides = guides;
            EditorUtility.SetDirty(cues);
        }

        // Solo en esta escena: los materiales compartidos con otras escenas no se modifican.
        static void RemapEnvironment(Scene scene, Mats mt)
        {
            var env = scene.GetRootGameObjects().First(g => g.name == "TutorialVisualEnvironment");
            foreach (var r in env.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material m = null;
                switch (r.name)
                {
                    case "Piso continuo": m = mt.floor; break;
                    case "Alfombra de zona de trabajo": m = mt.carpet; break;
                    case "Muro de fondo": case "Muro posterior": case "Muro izquierdo": case "Muro derecho":
                    case "Techo": case "Ventana travesaño": case "Ventana montante": m = mt.wall; break;
                }
                if (m == null) continue;
                r.sharedMaterial = m;
                EditorUtility.SetDirty(r);
            }
        }
    }
}
