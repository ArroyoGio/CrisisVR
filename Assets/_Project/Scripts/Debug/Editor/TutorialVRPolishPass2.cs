using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrisisVR.Editor
{
    // Pulido visual VR del tutorial, pasada 2. Idempotente (usa valores absolutos) y limitada a Tutorial.unity.
    // Solo toca la capa visual: no modifica la lógica de TutorialFlow ni de las demás escenas.
    // Corrige: tarjeta del mando 3D invadiendo el título, etiqueta de mano fuera de contexto,
    // cubos que tapaban los botones del panel, contorno desfasado de los cubos y paleta de la sala.
    public static class TutorialVRPolishPass2
    {
        const string MatDir = "Assets/_Project/Materials/TutorialVR/";
        const float U = 0.0017f; // metros por unidad de canvas del panel

        // Composición del escenario 3D del panel (unidades de canvas, ya con la perspectiva del jugador).
        static readonly Vector2 StageCenter = new Vector2(430f, -40f);
        static readonly Vector2 StageSize = new Vector2(620f, 460f);
        static readonly Vector2 ControllerAt = new Vector2(410f, -55f);
        static readonly Vector2 TargetAt = new Vector2(545f, 95f);
        static readonly Vector2 SecondAt = new Vector2(665f, 95f);
        const float ControllerDepth = 0.75f, TargetDepth = 0.25f;
        const float ControllerScale = 0.040f, ExampleObjectScale = 0.14f, ExampleButtonScale = 0.85f;

        // Mesa de práctica: altura real (cubo a 1.00 m del suelo del mundo) y flecha pegada al cubo.
        const float CubeLocalY = 0.94f;      // relativo al origen de TutorialWorld
        const float MarkerOffset = 0.09f;

        [MenuItem("CrisisVR/T-23/Pulido visual VR del tutorial (pasada 2)")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Tutorial.unity")
                throw new InvalidOperationException("Pulido VR pasada 2: requiere la escena Tutorial activa, fuera de Play.");
            var flow = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TutorialFlow>(true)).Single();
            var pulido = scene.GetRootGameObjects().First(g => g.name == "PulidoVR_Tutorial");
            var env = scene.GetRootGameObjects().First(g => g.name == "TutorialVisualEnvironment");

            LayoutStage(flow);
            PlaceDemonstration(flow);
            LowerPracticeTable(flow, pulido, env);
            StyleMaterials();

            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("PULIDO_VR_TUTORIAL_P2: aplicado");
        }

        // ---------- Tarjeta del escenario 3D: ya no invade la fila del título ----------
        static void LayoutStage(TutorialFlow flow)
        {
            var panel = (RectTransform)flow.panel.transform;
            foreach (var n in new[] { "StageCard", "StageOutline" })
            {
                var t = panel.Find(n) as RectTransform;
                if (t == null) continue;
                t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
                t.pivot = new Vector2(0.5f, 0.5f);
                t.anchoredPosition = StageCenter;
                t.sizeDelta = StageSize;
                EditorUtility.SetDirty(t);
            }
            var hand = flow.panel.hand;
            if (hand != null)
            {
                var r = hand.rectTransform;
                r.anchoredPosition = new Vector2(StageCenter.x, StageCenter.y - StageSize.y * 0.5f + 27f);
                r.sizeDelta = new Vector2(StageSize.x - 20f, 44f);
                hand.fontSize = 28f;
                EditorUtility.SetDirty(hand);
            }
        }

        // Proyecta un punto «aparente» del plano del panel a una profundidad más cercana al jugador,
        // para que el objeto 3D se vea justo en ese lugar del panel desde la posición de partida.
        static Vector3 Project(TutorialFlow flow, Vector2 apparent, float depthInFront)
        {
            var eye = flow.transform.position + new Vector3(0f, 1.60f, 0f);
            var p0 = flow.panel.transform.position;
            var a = p0 + new Vector3(apparent.x * U, apparent.y * U, 0f);
            float zPanel = p0.z - eye.z;
            float s = (zPanel - depthInFront) / zPanel;
            return eye + (a - eye) * s;
        }

        static void PlaceDemonstration(TutorialFlow flow)
        {
            var world = flow.transform;
            var cc = world.Find("ControllerCue");
            if (cc != null)
            {
                cc.position = Project(flow, ControllerAt, ControllerDepth);
                foreach (var n in new[] { "RightControllerReference", "LeftControllerReference" })
                {
                    var m = cc.Find(n);
                    if (m != null) m.localScale = Vector3.one * ControllerScale;
                }
                EditorUtility.SetDirty(cc);
            }
            var aim = world.Find("AimDemonstration");
            if (aim == null) return;
            var target = Project(flow, TargetAt, TargetDepth);
            var second = Project(flow, SecondAt, TargetDepth);
            var btn = aim.Find("ExampleButton");
            if (btn != null) { btn.position = target; btn.localScale = Vector3.one * ExampleButtonScale; EditorUtility.SetDirty(btn); }
            var o1 = aim.Find("ExampleSelectableObject");
            if (o1 != null) { o1.position = target; o1.localScale = Vector3.one * ExampleObjectScale; EditorUtility.SetDirty(o1); }
            var o2 = aim.Find("ExampleAlternativeObject");
            if (o2 != null) { o2.position = second; o2.localScale = Vector3.one * ExampleObjectScale; EditorUtility.SetDirty(o2); }
        }

        // ---------- Mesa de práctica a altura real; los cubos dejan de tapar el panel ----------
        static void SetPart(Transform ped, string name, Vector3 pos, Vector3? scale)
        {
            var t = ped.Find(name);
            if (t == null) return;
            t.position = pos;
            if (scale.HasValue) t.localScale = scale.Value;
            EditorUtility.SetDirty(t);
        }

        static void LowerPracticeTable(TutorialFlow flow, GameObject pulido, GameObject env)
        {
            var origin = flow.transform.position;
            var cueA = flow.objectCues[0].transform;
            var cueB = flow.objectCues[1].transform;
            float x0 = -0.24f, x1 = 0.24f, z = 2.06f;
            cueA.position = origin + new Vector3(x0, CubeLocalY, z);
            cueB.position = origin + new Vector3(x1, CubeLocalY, z);
            EditorUtility.SetDirty(cueA); EditorUtility.SetDirty(cueB);

            float floorTop = env.GetComponentsInChildren<Renderer>(true).First(r => r.name == "Piso continuo").bounds.max.y;
            float cx = (cueA.position.x + cueB.position.x) * 0.5f, cz = cueA.position.z;
            float top = cueA.position.y - cueA.lossyScale.y * 0.5f;
            float h = top - 0.05f - floorTop;
            var ped = pulido.transform.Find("Mesa de practica");
            if (ped != null)
            {
                SetPart(ped, "Tapa", new Vector3(cx, top - 0.025f, cz), null);
                SetPart(ped, "Lateral izquierdo", new Vector3(cx - 0.60f, floorTop + h * 0.5f, cz), new Vector3(0.05f, h, 0.52f));
                SetPart(ped, "Lateral derecho", new Vector3(cx + 0.60f, floorTop + h * 0.5f, cz), new Vector3(0.05f, h, 0.52f));
                SetPart(ped, "Fondo", new Vector3(cx, floorTop + h * 0.5f, cz + 0.24f), new Vector3(1.15f, h, 0.04f));
                SetPart(ped, "Canto LED", new Vector3(cx, top - 0.05f + 0.006f, cz - 0.312f), null);
            }

            var cues = pulido.GetComponent<TutorialWorldCues>();
            if (cues == null || cues.guides == null) return;
            foreach (var g in cues.guides)
            {
                if (g == null || g.cue == null) continue;
                var c = g.cue.transform;
                float half = c.lossyScale.y * 0.5f;
                if (g.ring != null)
                    g.ring.transform.position = new Vector3(c.position.x, c.position.y - half + 0.004f, c.position.z);
                if (g.marker != null)
                    g.marker.transform.position = new Vector3(c.position.x, c.position.y + half + MarkerOffset, c.position.z);
                // El «casco invertido» se desfasa en cubos con perspectiva; el brillo propio del material lo sustituye.
                var shell = c.Find("Contorno");
                if (shell != null) UnityEngine.Object.DestroyImmediate(shell.gameObject);
                g.outline = null;
            }
            EditorUtility.SetDirty(cues);
        }

        // ---------- Materiales exclusivos del tutorial (no se comparten con otras escenas) ----------
        static void LitGlow(string name, Color c, float emission)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + name + ".mat");
            if (m == null) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null && m.shader != lit) m.shader = lit;
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emission);
            EditorUtility.SetDirty(m);
        }

        static void Tint(string name, Color c)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + name + ".mat");
            if (m == null) return;
            m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
        }

        static void StyleMaterials()
        {
            LitGlow("TVR_CueNeutro", new Color(1f, 0.72f, 0.20f), 0.35f);
            LitGlow("TVR_CueHover", new Color(1f, 0.90f, 0.55f), 0.45f);
            LitGlow("TVR_CueActivo", new Color(0.30f, 0.90f, 0.62f), 0.40f);
            // Sala más luminosa: el panel azul noche resalta mejor y la mesa oscura contrasta con el suelo.
            Tint("TVR_Pared", new Color(0.90f, 0.87f, 0.81f));
            Tint("TVR_Suelo", new Color(0.47f, 0.37f, 0.28f));
        }
    }
}
