#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrisisVR.EditorTools
{
    /// <summary>
    /// LAB 12 – Mide Occlusion Culling y Dynamic Batching en la escena Acto1 (Play Mode, Editor).
    /// Matriz 2x2 (Occlusion off/on x Dynamic Batching off/on) en varias poses fijas del XR Origin.
    /// Solo cambia estado en memoria (Camera.useOcclusionCulling y la propiedad del asset URP) y lo restaura al terminar.
    /// Uso: entrar a Play Mode con la escena Acto1 y ejecutar CrisisVR/LAB12/Ejecutar benchmark.
    /// OJO: son métricas del Editor en PC, NO del Meta Quest 3.
    /// </summary>
    public static class Lab12Benchmark
    {
        const int Warmup = 20;
        const int Samples = 60;
        const int Reps = 2;
        const string OutDir = "Docs/evidence/Lab12";

        struct Pose { public string id, label; public Vector3 pos; public float yaw; }
        struct Cfg { public string id; public bool occ, dyn; }

        static readonly Cfg[] Cfgs =
        {
            new Cfg { id = "A_base",     occ = false, dyn = false },
            new Cfg { id = "B_occ",      occ = true,  dyn = false },
            new Cfg { id = "C_dyn",      occ = false, dyn = true  },
            new Cfg { id = "D_occ_dyn",  occ = true,  dyn = true  },
        };

        static readonly string[] Metrics =
        {
            "batches","drawCalls","setPassCalls","triangles","vertices",
            "dynBatchedDrawCalls","dynBatches","staticBatchedDrawCalls","instancedDrawCalls",
            "shadowCasters","frameMs","cpuFrameMs","gpuFrameMs"
        };

        class Row { public string rep, pose, cfg; public bool occ, dyn; public Vector3 camPos; public Dictionary<string, List<float>> v = new Dictionary<string, List<float>>(); }

        // estado
        static List<Pose> poses; static List<Row> rows = new List<Row>();
        static int rep, pi, ci, phase, frames, shotWait;
        static Row cur; static Camera cam; static Transform origin; static CharacterController cc;
        static UnityEngine.Rendering.RenderPipelineAsset rp; static System.Reflection.PropertyInfo dynProp; static SerializedObject rpSo;
        static bool origOcc, origDyn; static int origQuality; static Vector3 origPos; static Quaternion origRot; static bool origCc;
        static string log;

        [MenuItem("CrisisVR/LAB12/Ejecutar benchmark (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) { Debug.LogError("[LAB12] Entra a Play Mode primero."); return; }
            cam = Camera.main; var go = GameObject.Find("Acto1 XR Origin");
            if (cam == null || go == null) { Debug.LogError("[LAB12] Falta Main Camera o Acto1 XR Origin."); return; }
            origin = go.transform; cc = go.GetComponent<CharacterController>();
            origQuality = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(1, false); // Low = Performance URP Config (perfil que usa el Quest)
            rp = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            dynProp = rp.GetType().GetProperty("supportsDynamicBatching");
            if (dynProp == null || !dynProp.CanWrite) { Debug.LogError("[LAB12] VERIFICAR: supportsDynamicBatching no es escribible en " + rp.GetType().Name); QualitySettings.SetQualityLevel(origQuality, false); return; }
            origDyn = (bool)dynProp.GetValue(rp); origOcc = cam.useOcclusionCulling;
            origPos = origin.position; origRot = origin.rotation; origCc = cc != null && cc.enabled;

            poses = new List<Pose>
            {
                new Pose { id = "P1", label = "Exterior: spawn mirando a la fachada", pos = new Vector3(39.5f, 0.02f, -7f), yaw = 270f },
                new Pose { id = "P2", label = "Vestíbulo mirando al pasillo", pos = new Vector3(21f, 0.02f, -7f), yaw = 270f },
                new Pose { id = "P3", label = "Pasillo medio", pos = new Vector3(8f, 0.02f, -7f), yaw = 270f },
            };
            var seat = GameObject.Find("Spawn_Participante");
            if (seat != null)
            {
                var y = seat.transform.eulerAngles.y;
                poses.Add(new Pose { id = "P4", label = "Aula: participante (asiento)", pos = seat.transform.position, yaw = y });
                poses.Add(new Pose { id = "P5", label = "Aula: participante mirando atrás", pos = seat.transform.position, yaw = y + 180f });
            }
            poses.Add(new Pose { id = "P6", label = "Exterior: spawn mirando hacia afuera (control)", pos = new Vector3(39.5f, 0.02f, -7f), yaw = 90f });

            Directory.CreateDirectory(Path.Combine(Root(), OutDir, "shots"));
            rows.Clear(); rep = 0; pi = 0; ci = 0; phase = 0; frames = 0; shotWait = 0; cur = null;
            log = "";
            EditorApplication.update += Tick;
            Debug.Log("[LAB12] Benchmark iniciado: " + poses.Count + " poses x " + Cfgs.Length + " configs x " + Reps + " reps. Quality=" + QualitySettings.names[1] + " RP=" + rp.name);
        }

        static string Root() { return Path.GetDirectoryName(Application.dataPath); }

        static Cfg CfgFor(int r, int i) { return r % 2 == 0 ? Cfgs[i] : Cfgs[Cfgs.Length - 1 - i]; }

        static void Apply()
        {
            var p = poses[pi]; var c = CfgFor(rep, ci);
            if (cc != null) cc.enabled = false;
            origin.position = p.pos; origin.rotation = Quaternion.Euler(0f, p.yaw, 0f);
            cam.useOcclusionCulling = c.occ;
            dynProp.SetValue(rp, c.dyn);
            cur = new Row { rep = "R" + (rep + 1), pose = p.id, cfg = c.id, occ = c.occ, dyn = c.dyn };
            foreach (var m in Metrics) cur.v[m] = new List<float>();
            frames = 0; phase = 1;
        }

        static void Sample()
        {
            cur.camPos = cam.transform.position;
            Add("batches", UnityStats.batches); Add("drawCalls", UnityStats.drawCalls); Add("setPassCalls", UnityStats.setPassCalls);
            Add("triangles", UnityStats.triangles); Add("vertices", UnityStats.vertices);
            Add("dynBatchedDrawCalls", UnityStats.dynamicBatchedDrawCalls); Add("dynBatches", UnityStats.dynamicBatches);
            Add("staticBatchedDrawCalls", UnityStats.staticBatchedDrawCalls); Add("instancedDrawCalls", UnityStats.instancedBatchedDrawCalls);
            Add("shadowCasters", UnityStats.shadowCasters);
            Add("frameMs", Time.unscaledDeltaTime * 1000f);
            var t = new FrameTiming[1];
            if (FrameTimingManager.GetLatestTimings(1, t) > 0) { Add("cpuFrameMs", (float)t[0].cpuFrameTime); Add("gpuFrameMs", (float)t[0].gpuFrameTime); }
            else { Add("cpuFrameMs", 0f); Add("gpuFrameMs", 0f); }
        }
        static void Add(string k, float v) { cur.v[k].Add(v); }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { Finish(false, "Play Mode terminó antes de acabar."); return; }
            try
            {
                FrameTimingManager.CaptureFrameTimings();
                switch (phase)
                {
                    case 0: Apply(); break;
                    case 1: if (++frames >= Warmup) { frames = 0; phase = 2; } break;
                    case 2:
                        Sample();
                        if (++frames >= Samples)
                        {
                            rows.Add(cur);
                            if (rep == 0)
                            {
                                var f = Path.Combine(Root(), OutDir, "shots", cur.pose + "_" + cur.cfg + ".png");
                                ScreenCapture.CaptureScreenshot(f); shotWait = 3; phase = 3;
                            }
                            else Next();
                        }
                        break;
                    case 3: if (--shotWait <= 0) Next(); break;
                }
            }
            catch (Exception e) { Finish(false, e.ToString()); }
        }

        static void Next()
        {
            phase = 0;
            if (++ci >= Cfgs.Length) { ci = 0; if (++pi >= poses.Count) { pi = 0; if (++rep >= Reps) { Finish(true, "OK"); } } }
        }

        static void Finish(bool ok, string msg)
        {
            EditorApplication.update -= Tick;
            // restaurar
            try
            {
                dynProp.SetValue(rp, origDyn); cam.useOcclusionCulling = origOcc;
                origin.position = origPos; origin.rotation = origRot; if (cc != null) cc.enabled = origCc;
                QualitySettings.SetQualityLevel(origQuality, false);
            }
            catch (Exception e) { Debug.LogWarning("[LAB12] restauración parcial: " + e.Message); }
            if (rows.Count > 0) WriteOutputs(ok, msg);
            Debug.Log("[LAB12] Fin. ok=" + ok + " " + msg + " filas=" + rows.Count);
        }

        static float Median(List<float> l) { var s = l.OrderBy(x => x).ToList(); int n = s.Count; return n == 0 ? 0 : (n % 2 == 1 ? s[n / 2] : (s[n / 2 - 1] + s[n / 2]) / 2f); }
        static float P95(List<float> l) { var s = l.OrderBy(x => x).ToList(); return s.Count == 0 ? 0 : s[Mathf.Clamp(Mathf.CeilToInt(s.Count * 0.95f) - 1, 0, s.Count - 1)]; }
        static string F(float f) { return f.ToString("0.###", CultureInfo.InvariantCulture); }

        static void WriteOutputs(bool ok, string msg)
        {
            var dir = Path.Combine(Root(), OutDir);
            var sb = new StringBuilder();
            sb.AppendLine("rep,pose,cfg,occlusion,dynBatching,metric,mean,median,min,max,p95,n,camX,camY,camZ");
            foreach (var r in rows)
                foreach (var m in Metrics)
                {
                    var l = r.v[m]; if (l.Count == 0) continue;
                    sb.AppendLine(string.Join(",", new[] { r.rep, r.pose, r.cfg, r.occ ? "ON" : "OFF", r.dyn ? "ON" : "OFF", m, F(l.Average()), F(Median(l)), F(l.Min()), F(l.Max()), F(P95(l)), l.Count.ToString(), F(r.camPos.x), F(r.camPos.y), F(r.camPos.z) }));
                }
            File.WriteAllText(Path.Combine(dir, "lab12_metrics.csv"), sb.ToString(), new UTF8Encoding(false));

            var env = new StringBuilder();
            env.AppendLine("LAB12 benchmark - entorno");
            env.AppendLine("Fecha: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            env.AppendLine("Resultado: ok=" + ok + " " + msg);
            env.AppendLine("Unity: " + Application.unityVersion + " | Target activo: " + EditorUserBuildSettings.activeBuildTarget + " (medido en Editor/PC, NO en Quest)");
            env.AppendLine("Escena: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            env.AppendLine("Quality medido: " + QualitySettings.names[1] + " | Quality original restaurado: " + QualitySettings.names[origQuality]);
            env.AppendLine("URP asset medido: " + rp.name + " | DynamicBatching original=" + origDyn + " | useOcclusionCulling original=" + origOcc);
            env.AppendLine("umbraDataSize: " + StaticOcclusionCulling.umbraDataSize + " B");
            env.AppendLine("GPU: " + SystemInfo.graphicsDeviceName + " | API: " + SystemInfo.graphicsDeviceType + " | CPU: " + SystemInfo.processorType);
            env.AppendLine("Game view / screen: " + Screen.width + "x" + Screen.height + " | vSync=" + QualitySettings.vSyncCount + " | targetFrameRate=" + Application.targetFrameRate);
            env.AppendLine("Warmup=" + Warmup + " cuadros, muestras=" + Samples + " cuadros por celda, repeticiones=" + Reps + " (la 2a repetición invierte el orden de configs)");
            env.AppendLine("Poses:");
            foreach (var p in poses) env.AppendLine("  " + p.id + " " + p.label + " pos=" + p.pos.ToString("F2") + " yaw=" + p.yaw.ToString("F0"));
            File.WriteAllText(Path.Combine(dir, "lab12_entorno.txt"), env.ToString(), new UTF8Encoding(false));
        }
    }
}
#endif
