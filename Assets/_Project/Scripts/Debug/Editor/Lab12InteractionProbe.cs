#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CrisisVR.EditorTools
{
    /// <summary>
    /// LAB 12 – Sonda de interacción en Play Mode (Editor, sin visor). Ejecuta UNA configuración por sesión de Play:
    /// "A" = línea base (Occlusion OFF, Dynamic Batching OFF) o "D" = optimizado (Occlusion ON, Dynamic Batching ON).
    /// Mueve el NearFarInteractor derecho por código y usa un lector de botón falso (mismo mecanismo que CrisisVRGrabPlayTests).
    /// NO sustituye la prueba con mandos reales en Meta Quest 3.
    /// </summary>
    public static class Lab12InteractionProbe
    {
        const string OutDir = "Docs/evidence/Lab12";

        class FakeButton : IXRInputButtonReader
        {
            public bool pressed;
            public bool ReadIsPerformed() { return pressed; }
            public bool ReadWasPerformedThisFrame() { return pressed; }
            public bool ReadWasCompletedThisFrame() { return !pressed; }
            public float ReadValue() { return pressed ? 1f : 0f; }
            public bool TryReadValue(out float value) { value = ReadValue(); return true; }
        }

        static IEnumerator routine; static int wait; static string cfgId; static StringBuilder log;
        static Camera cam; static Transform origin; static CharacterController cc; static Transform camOffset;
        static UnityEngine.Rendering.RenderPipelineAsset rp; static System.Reflection.PropertyInfo dynProp;
        static bool origOcc, origDyn, origCc; static int origQ; static Vector3 origPos; static Quaternion origRot; static Quaternion origOffsetRot;
        static List<Behaviour> disabled = new List<Behaviour>();

        [MenuItem("CrisisVR/LAB12/Ejecutar sonda de interacción A (base)")] public static void RunA() { Run("A"); }
        [MenuItem("CrisisVR/LAB12/Ejecutar sonda de interacción D (optimizado)")] public static void RunD() { Run("D"); }

        public static void Run(string cfg)
        {
            if (!EditorApplication.isPlaying) { Debug.LogError("[LAB12-I] Entra a Play Mode primero."); return; }
            cfgId = cfg; log = new StringBuilder();
            cam = Camera.main; var go = GameObject.Find("Acto1 XR Origin");
            origin = go.transform; cc = go.GetComponent<CharacterController>();
            var xro = go.GetComponent<Unity.XR.CoreUtils.XROrigin>(); camOffset = xro != null && xro.CameraFloorOffsetObject != null ? xro.CameraFloorOffsetObject.transform : cam.transform.parent;
            origQ = QualitySettings.GetQualityLevel(); QualitySettings.SetQualityLevel(1, false);
            rp = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            dynProp = rp.GetType().GetProperty("supportsDynamicBatching");
            origDyn = (bool)dynProp.GetValue(rp); origOcc = cam.useOcclusionCulling;
            origPos = origin.position; origRot = origin.rotation; origOffsetRot = camOffset.localRotation; origCc = cc != null && cc.enabled;
            bool opt = cfg == "D";
            cam.useOcclusionCulling = opt; dynProp.SetValue(rp, opt);
            Directory.CreateDirectory(Path.Combine(Root(), OutDir, "shots"));
            routine = Routine(); wait = 0; disabled.Clear();
            EditorApplication.update += Tick;
            Debug.Log("[LAB12-I] Sonda " + cfg + " iniciada. occlusion=" + opt + " dynBatching=" + opt);
        }

        static string Root() { return Path.GetDirectoryName(Application.dataPath); }
        static void L(string s) { log.AppendLine(s); }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { Finish("Play Mode terminó antes."); return; }
            if (wait > 0) { wait--; return; }
            try { if (!routine.MoveNext()) Finish("OK"); else if (routine.Current is int) wait = (int)routine.Current; }
            catch (Exception e) { L("EXCEPCION: " + e); Finish("EXCEPCION"); }
        }

        static void Finish(string msg)
        {
            EditorApplication.update -= Tick;
            try
            {
                foreach (var b in disabled) if (b != null) b.enabled = true;
                dynProp.SetValue(rp, origDyn); cam.useOcclusionCulling = origOcc;
                origin.position = origPos; origin.rotation = origRot; camOffset.localRotation = origOffsetRot; if (cc != null) cc.enabled = origCc;
                QualitySettings.SetQualityLevel(origQ, false);
            }
            catch (Exception e) { L("restauración parcial: " + e.Message); }
            L("FIN: " + msg);
            File.WriteAllText(Path.Combine(Root(), OutDir, "lab12_interacciones_" + cfgId + ".txt"), log.ToString(), new UTF8Encoding(false));
            Debug.Log("[LAB12-I] Sonda " + cfgId + " terminada: " + msg);
        }

        static void Shot(string name) { ScreenCapture.CaptureScreenshot(Path.Combine(Root(), OutDir, "shots", name + ".png")); }

        static void LookAt(Vector3 target)
        {
            if (cc != null) cc.enabled = false;
            Vector3 eye = cam.transform.position; Vector3 d = target - eye;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            origin.rotation = Quaternion.Euler(0f, yaw, 0f);
            float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            camOffset.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        static string StatsLine()
        {
            return "batches=" + UnityStats.batches + " drawCalls=" + UnityStats.drawCalls + " setPass=" + UnityStats.setPassCalls + " tris=" + UnityStats.triangles + " dynBatchedDraws=" + UnityStats.dynamicBatchedDrawCalls;
        }

        static IEnumerator Routine()
        {
            L("LAB12 sonda de interacción - config " + cfgId + (cfgId == "A" ? " (base: Occlusion OFF, DynBatching OFF)" : " (optimizado: Occlusion ON, DynBatching ON)"));
            L("Fecha: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | Unity " + Application.unityVersion + " | Editor/PC, XR Interaction Simulator presente, SIN visor ni mandos reales");
            L("useOcclusionCulling=" + cam.useOcclusionCulling + " dynBatching(asset " + rp.name + ")=" + (bool)dynProp.GetValue(rp) + " quality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] + " umbraDataSize=" + StaticOcclusionCulling.umbraDataSize);
            yield return 30;

            // Poner al participante en el asiento.
            var seat = GameObject.Find("Spawn_Participante");
            if (cc != null) cc.enabled = false;
            if (seat != null) { origin.position = seat.transform.position; origin.rotation = seat.transform.rotation; }
            L("Pose participante: origin=" + origin.position.ToString("F2") + " yaw=" + origin.eulerAngles.y.ToString("F0") + " camara=" + cam.transform.position.ToString("F2"));
            yield return 20;

            // Interactor derecho
            var interactors = UnityEngine.Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(i => i.gameObject.activeInHierarchy).ToList();
            L("NearFarInteractors activos: " + string.Join(", ", interactors.Select(i => i.name + "@" + GetPath(i.transform))));
            var right = interactors.FirstOrDefault(i => GetPath(i.transform).ToLower().Contains("right")) ?? interactors.FirstOrDefault();
            if (right == null) { L("FALLO: no hay NearFarInteractor activo."); yield break; }
            L("Interactor usado: " + GetPath(right.transform));
            // Desactivar lo que sobreescribe la pose del mando (TrackedPoseDriver / controllers) solo durante la sonda.
            foreach (var b in right.GetComponentsInParent<Behaviour>(true))
            {
                var n = b.GetType().Name;
                if (b.enabled && (n.Contains("TrackedPoseDriver") || n.Contains("ActionBasedController") || n == "XRController")) { b.enabled = false; disabled.Add(b); L("  pose-driver desactivado temporalmente: " + n + " en " + b.gameObject.name); }
            }
            var fake = new FakeButton();
            if (right.selectInput == null) { L("FALLO: el interactor no tiene selectInput."); yield break; }
            right.selectInput.bypass = fake;

            // ---------- Prueba 1: tapa de laptop (agarre + giro de bisagra) ----------
            var lid = GameObject.Find("Tapa_Interactiva");
            if (lid == null) { L("T1 tapa: no existe Tapa_Interactiva."); }
            else
            {
                var grab = lid.GetComponent<XRGrabInteractable>(); var hinge = lid.transform.parent;
                var col = lid.GetComponent<Collider>();
                Quaternion closedRel = Quaternion.Inverse(hinge.rotation) * lid.transform.rotation;
                float r0 = Vector3.Distance(hinge.position, lid.transform.position);
                L("T1 tapa: grab.enabled=" + grab.enabled + " activa=" + lid.activeInHierarchy + " colliders=" + grab.colliders.Count + " renderer.enabled=" + lid.GetComponent<Renderer>().enabled);
                yield return 5;
                Vector3 target = col != null ? col.bounds.center : lid.transform.position;
                right.transform.position = target; right.transform.rotation = Quaternion.LookRotation(target - cam.transform.position);
                Physics.SyncTransforms(); yield return 15;
                L("T1 hover tras acercar: hovered=" + right.interactablesHovered.Count + " eslaTapa=" + right.interactablesHovered.Any(h => (h as Component) == grab));
                fake.pressed = true; yield return 15;
                bool sel = grab.isSelected;
                L("T1 SelectEnter: grab.isSelected=" + sel);
                if (sel)
                {
                    var attach = right.GetAttachTransform(grab);
                    Vector3 hl = hinge.InverseTransformPoint(attach.position);
                    float rr = Mathf.Sqrt(hl.y * hl.y + hl.z * hl.z); float th0 = Mathf.Atan2(-hl.y, hl.z) * Mathf.Rad2Deg;
                    foreach (float deg in new[] { 60f, 125f, 0f })
                    {
                        float th = (th0 - deg) * Mathf.Deg2Rad;
                        Vector3 world = hinge.TransformPoint(new Vector3(hl.x, -rr * Mathf.Sin(th), rr * Mathf.Cos(th)));
                        right.transform.position += world - attach.position;
                        Physics.SyncTransforms(); yield return 20;
                        float ang = Quaternion.Angle(closedRel, Quaternion.Inverse(hinge.rotation) * lid.transform.rotation);
                        float dr = Vector3.Distance(hinge.position, lid.transform.position) - r0;
                        L("T1 giro objetivo " + deg.ToString("F0") + " grados -> medido " + ang.ToString("F2") + " grados (error " + Mathf.Abs(ang - deg).ToString("F2") + "), variacion de radio al pivote " + dr.ToString("F5") + " m");
                        if (deg == 60f) { Shot("I1_tapa60_" + cfgId); yield return 6; }
                    }
                    // abrir de nuevo a 60 y soltar para comprobar que conserva el ángulo
                    { float th = (th0 - 60f) * Mathf.Deg2Rad; Vector3 world = hinge.TransformPoint(new Vector3(hl.x, -rr * Mathf.Sin(th), rr * Mathf.Cos(th))); right.transform.position += world - attach.position; Physics.SyncTransforms(); yield return 20; }
                    fake.pressed = false; yield return 20;
                    float angRel = Quaternion.Angle(closedRel, Quaternion.Inverse(hinge.rotation) * lid.transform.rotation);
                    L("T1 SelectExit: isSelected=" + grab.isSelected + " angulo conservado=" + angRel.ToString("F2") + " grados (esperado ~60)");
                }
                // volver a cerrar: recoger de nuevo y llevar a 0
                if (grab.isSelected == false && lid != null)
                {
                    right.transform.position = col.bounds.center; Physics.SyncTransforms(); yield return 10;
                    fake.pressed = true; yield return 12;
                    if (grab.isSelected)
                    {
                        var attach = right.GetAttachTransform(grab); Vector3 hl = hinge.InverseTransformPoint(attach.position);
                        float rr = Mathf.Sqrt(hl.y * hl.y + hl.z * hl.z); float th0 = Mathf.Atan2(-hl.y, hl.z) * Mathf.Rad2Deg;
                        float th = (th0 + 60f) * Mathf.Deg2Rad;
                        right.transform.position += hinge.TransformPoint(new Vector3(hl.x, -rr * Mathf.Sin(th), rr * Mathf.Cos(th))) - attach.position; Physics.SyncTransforms(); yield return 20;
                    }
                    fake.pressed = false; yield return 15;
                    L("T1 tapa devuelta: angulo final=" + Quaternion.Angle(closedRel, Quaternion.Inverse(hinge.rotation) * lid.transform.rotation).ToString("F2"));
                }
            }
            fake.pressed = false; yield return 10;

            // ---------- Prueba 2: teclas (hover/selección con alcance de 5 cm) ----------
            foreach (var keyName in new[] { "Tecla_020", "Tecla_049", "Tecla_080" })
            {
                var kgo = GameObject.Find(keyName);
                if (kgo == null) { L("T2 " + keyName + ": no existe."); continue; }
                var key = kgo.GetComponent<Act1KeyboardKey>(); var ki = kgo.GetComponent<XRSimpleInteractable>(); var kc = kgo.GetComponent<Collider>();
                int pressedEvents = 0; Action<Act1KeyboardKey> h = k => pressedEvents++; key.Pressed += h;
                Vector3 kt = kc.bounds.center;
                for (int it = 0; it < 3; it++)
                {
                    Vector3 co = right.transform.position;
                    var near = right.nearInteractionCaster as UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.InteractionCasterBase;
                    if (near != null && near.effectiveCastOrigin != null) co = near.effectiveCastOrigin.position;
                    right.transform.position += kt - co; Physics.SyncTransforms(); yield return 3;
                }
                yield return 12;
                bool hov = ki.isHovered; fake.pressed = true; yield return 15;
                bool sel = ki.isSelected || pressedEvents > 0; float depth = key.CurrentDepth;
                L("T2 " + keyName + ": hover=" + hov + " selectEventos=" + pressedEvents + " seleccionada=" + ki.isSelected + " profundidad=" + (depth * 1000f).ToString("F2") + " mm (max " + (key.MaxDepth * 1000f).ToString("F2") + " mm)");
                fake.pressed = false; yield return 40;
                L("T2 " + keyName + " tras soltar: IsPressed=" + key.IsPressed + " profundidad=" + (key.CurrentDepth * 1000f).ToString("F2") + " mm");
                key.Pressed -= h;
            }

            // ---------- Prueba 3: botones ENTREGAR / CORREGIR (solo hover por rayo; no se selecciona para no cambiar de acto) ----------
            foreach (var bn in new[] { "ENTREGAR", "CORREGIR PROYECTO" })
            {
                var bgo = GameObject.Find(bn);
                if (bgo == null) { L("T3 " + bn + ": no existe."); continue; }
                var bi = bgo.GetComponent<XRSimpleInteractable>(); var bcol = bgo.GetComponentInChildren<Collider>();
                Vector3 bt = bcol != null ? bcol.bounds.center : bgo.transform.position;
                Vector3 from = cam.transform.position + (bt - cam.transform.position).normalized * 0.15f;
                right.transform.position = from; right.transform.rotation = Quaternion.LookRotation(bt - from); Physics.SyncTransforms(); yield return 25;
                L("T3 " + bn + ": activo=" + bgo.activeInHierarchy + " interactable.enabled=" + bi.enabled + " distancia a camara=" + Vector3.Distance(cam.transform.position, bt).ToString("F2") + " m hovered=" + bi.isHovered);
                LookAt(bt); yield return 6; Shot("I3_" + bn.Split(' ')[0] + "_" + cfgId); yield return 6;
            }

            // ---------- Capturas de objetos interactivos desde el asiento (para comparar A vs D) ----------
            camOffset.localRotation = origOffsetRot;
            foreach (var pair in new[] { new[] { "I2_laptop", "Laptop_Participante" }, new[] { "I2_teclado", "Teclado_Fisico" } })
            {
                var t = GameObject.Find(pair[1]); if (t == null) continue;
                var rs = t.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                LookAt(b.center); yield return 20;
                L(pair[0] + " vista: " + StatsLine());
                Shot(pair[0] + "_" + cfgId); yield return 8;
            }
            L("FIN de pruebas de la sonda " + cfgId);
        }

        static string GetPath(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
    }
}
#endif
