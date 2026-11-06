// TEMPORAL: sonda de diagnóstico para las teclas de Acto1. Se elimina al terminar la prueba.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace CrisisVR.TempDiag
{
    public sealed class TempKeyDiagProbe : MonoBehaviour
    {
        public sealed class Case
        {
            public string label, expected;
            public int hand; // 0 = izquierdo, 1 = derecho
            public Vector3 start, end;
            public int travel = 24, hold = 14, press = 14, release = 8, retreat = 10;
            public bool doPress = true;
        }

        public readonly List<string> Log = new List<string>();
        public readonly List<string> Results = new List<string>();
        public bool Busy => running || queue.Count > 0;
        public int Done;

        readonly Queue<Case> queue = new Queue<Case>();
        readonly Dictionary<string, bool> lastReach = new Dictionary<string, bool>();
        readonly HashSet<string> caseHover = new HashSet<string>();
        readonly HashSet<string> caseSelect = new HashSet<string>();
        readonly HashSet<string> casePressed = new HashSet<string>();
        readonly List<string> reachFlips = new List<string>();
        Act1KeyboardKey[] keys;
        MethodInfo inReach, distTo;
        Transform[] controllers = new Transform[2];
        string caseLabel = "-";
        bool running;

        sealed class Spy : IXRHoverFilter, IXRSelectFilter
        {
            public TempKeyDiagProbe probe; public Act1KeyboardKey key;
            public bool canProcess => true;
            public bool Process(IXRHoverInteractor i, IXRHoverInteractable a) { probe.Reach("hover", key, i); return true; }
            public bool Process(IXRSelectInteractor i, IXRSelectInteractable a) { probe.Reach("select", key, i); return true; }
        }

        static string Short(string n) => n.Replace("Tecla_", "");
        static string Hand(Component c)
        {
            for (var t = c.transform; t != null; t = t.parent)
            {
                if (t.name.StartsWith("Left Controller")) return "L";
                if (t.name.StartsWith("Right Controller")) return "R";
            }
            return "?";
        }
        static string Who(Component c) => Hand(c) + ":" + c.GetType().Name.Replace("Interactor", "");

        void Add(string s) => Log.Add($"f{Time.frameCount} [{caseLabel}] {s}");

        bool ReachOf(Act1KeyboardKey k, IXRInteractor i) => (bool)inReach.Invoke(k, new object[] { i });
        float DistOf(Act1KeyboardKey k, Vector3 p) => (float)distTo.Invoke(k, new object[] { p });

        void Reach(string kind, Act1KeyboardKey k, IXRInteractor i)
        {
            bool r = ReachOf(k, i);
            var comp = i as Component;
            string id = kind + "|" + k.GetInstanceID() + "|" + (comp != null ? comp.GetInstanceID() : 0);
            if (lastReach.TryGetValue(id, out bool prev) && prev == r) return;
            lastReach[id] = r;
            string d = comp != null ? (DistOf(k, comp.transform.position) * 1000f).ToString("F1") : "?";
            Add($"IsTheKeyInReach {kind} key={Short(k.name)} by={(comp != null ? Who(comp) : "?")} -> {r} (dist={d}mm)");
            reachFlips.Add($"{kind}:{Short(k.name)}@{(comp != null ? Who(comp) : "?")}={r}");
        }

        public void Begin()
        {
            keys = FindObjectsByType<Act1KeyboardKey>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            inReach = typeof(Act1KeyboardKey).GetMethod("IsTheKeyInReach", BindingFlags.NonPublic | BindingFlags.Instance);
            distTo = typeof(Act1KeyboardKey).GetMethod("DistanceTo", BindingFlags.NonPublic | BindingFlags.Instance);
            var origin = GameObject.Find("Acto1 XR Origin").transform;
            controllers[0] = FindDeep(origin, "Left Controller");
            controllers[1] = FindDeep(origin, "Right Controller");
            foreach (var c in controllers)
            {
                var tpd = c.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if (tpd != null) tpd.enabled = false;
                foreach (var nf in c.GetComponentsInChildren<NearFarInteractor>(true))
                    nf.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            }

            foreach (var k in keys)
            {
                var key = k;
                var ia = k.GetComponent<XRSimpleInteractable>();
                ia.hoverEntered.AddListener(a => { var c = a.interactorObject as Component; string s = Short(key.name) + "@" + Who(c); caseHover.Add(s); Add($"HOVER_ENTER key={Short(key.name)} by={Who(c)}"); });
                ia.hoverExited.AddListener(a => { var c = a.interactorObject as Component; Add($"HOVER_EXIT  key={Short(key.name)} by={Who(c)}"); });
                ia.selectEntered.AddListener(a => { var c = a.interactorObject as Component; string s = Short(key.name) + "@" + Who(c); caseSelect.Add(s); Add($"SELECT_ENTER key={Short(key.name)} by={Who(c)}"); });
                ia.selectExited.AddListener(a => { var c = a.interactorObject as Component; Add($"SELECT_EXIT  key={Short(key.name)} by={Who(c)}"); });
                k.Pressed += pk => { casePressed.Add(Short(pk.name)); Add($"PRESSED(key sinks) key={Short(pk.name)}"); };
            }
            StartCoroutine(Run());
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var r = FindDeep(c, name); if (r != null) return r; }
            return null;
        }

        public void Enqueue(Case c) => queue.Enqueue(c);

        public string Config()
        {
            var sb = new StringBuilder();
            foreach (var c in controllers)
            {
                sb.AppendLine("== " + c.name);
                foreach (var i in c.GetComponentsInChildren<XRBaseInteractor>(true))
                {
                    sb.Append($"  {i.GetType().Name} '{i.name}' enabled={i.enabled} layers={i.interactionLayers.value} pos={i.transform.position:F4}");
                    if (i is NearFarInteractor nf)
                    {
                        sb.Append($" near={nf.enableNearCasting} far={nf.enableFarCasting} region={nf.selectionRegion.Value} sortNear={nf.nearCasterSortingStrategy}");
                        var sc = nf.nearInteractionCaster as UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.SphereInteractionCaster;
                        if (sc != null) sb.Append($" nearCaster[radius={sc.castRadius} mask={sc.physicsLayerMask.value} trigger={sc.physicsTriggerInteraction}]");
                        var fc = nf.farInteractionCaster;
                        if (fc != null) sb.Append($" farCaster={fc.GetType().Name}");
                    }
                    if (i is XRPokeInteractor pk)
                        sb.Append($" pokeDepth={pk.pokeDepth} pokeWidth={pk.pokeWidth} pokeSelectWidth={pk.pokeSelectWidth} pokeHoverRadius={pk.pokeHoverRadius} requirePokeFilter={pk.requirePokeFilter}");
                    sb.AppendLine();
                }
            }
            var k0 = keys[0].GetComponent<XRSimpleInteractable>();
            sb.AppendLine($"Key sample '{keys[0].name}': layers={k0.interactionLayers.value} colliders={k0.colliders.Count} hasPokeFilter={keys[0].GetComponent<XRPokeFilter>() != null}");
            return sb.ToString();
        }

        string Snap(string tag, int hand)
        {
            var c = controllers[hand];
            Vector3 o = c.position;
            var nf = c.GetComponentInChildren<NearFarInteractor>(true);
            var sb = new StringBuilder();
            sb.Append($"SNAP[{tag}] origin={o:F4} near=[");
            var list = new List<(float d, Act1KeyboardKey k)>();
            foreach (var k in keys) { float d = DistOf(k, o); if (d <= 0.06f) list.Add((d, k)); }
            list.Sort((a, b) => a.d.CompareTo(b.d));
            for (int n = 0; n < list.Count && n < 7; n++)
                sb.Append($"{Short(list[n].k.name)}:{list[n].d * 1000f:F1}mm:{(ReachOf(list[n].k, nf) ? "R" : "-")} ");
            sb.Append("]");
            var cols = Physics.OverlapSphere(o, 0.05f, ~0, QueryTriggerInteraction.Collide);
            var names = new List<string>();
            foreach (var col in cols) names.Add(Short(col.name));
            names.Sort();
            sb.Append(" overlap(5cm)=[" + string.Join(",", names) + "]");
            foreach (var i in c.GetComponentsInChildren<XRBaseInteractor>(true))
            {
                if (!i.isActiveAndEnabled) continue;
                var vt = new List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable>();
                i.GetValidTargets(vt);
                var vtn = new List<string>(); foreach (var t in vt) vtn.Add(Short(((Component)t).name));
                var hn = new List<string>(); foreach (var h in i.interactablesHovered) hn.Add(Short(((Component)h).name));
                var sn = new List<string>(); foreach (var s in i.interactablesSelected) sn.Add(Short(((Component)s).name));
                sb.Append($"\n      {Who(i)} valid=[{string.Join(",", vtn)}] hover=[{string.Join(",", hn)}] select=[{string.Join(",", sn)}]");
            }
            return sb.ToString();
        }

        string HoverNow(int hand)
        {
            var parts = new List<string>();
            foreach (var i in controllers[hand].GetComponentsInChildren<XRBaseInteractor>(true))
                foreach (var h in i.interactablesHovered) parts.Add(Short(((Component)h).name) + "@" + Who(i));
            parts.Sort();
            return string.Join(",", parts);
        }

        string SelectNow(int hand)
        {
            var parts = new List<string>();
            foreach (var i in controllers[hand].GetComponentsInChildren<XRBaseInteractor>(true))
                foreach (var h in i.interactablesSelected) parts.Add(Short(((Component)h).name) + "@" + Who(i));
            parts.Sort();
            return string.Join(",", parts);
        }

        void Tick(int hand)
        {
            var o = controllers[hand].position;
            foreach (var i in controllers[hand].GetComponentsInChildren<XRBaseInteractor>(true))
            {
                if (!(i is NearFarInteractor) && !(i is XRPokeInteractor)) continue;
                foreach (var k in keys)
                {
                    float d = DistOf(k, i.transform.position);
                    string id = "poll|" + k.GetInstanceID() + "|" + i.GetInstanceID();
                    if (d <= 0.08f || lastReach.ContainsKey(id)) Reach("poll", k, i);
                }
            }
        }

        void Place(int hand, Vector3 p, Vector3 look)
        {
            var dir = look - p; if (dir.sqrMagnitude < 1e-8f) dir = Vector3.forward;
            controllers[hand].SetPositionAndRotation(p, Quaternion.LookRotation(dir.normalized, Vector3.up));
        }

        IEnumerator Run()
        {
            while (true)
            {
                if (queue.Count == 0) { running = false; yield return null; continue; }
                running = true;
                var cs = queue.Dequeue();
                caseLabel = cs.label;
                int h = cs.hand;
                caseHover.Clear(); caseSelect.Clear(); casePressed.Clear(); reachFlips.Clear();
                foreach (var nf in controllers[h].GetComponentsInChildren<NearFarInteractor>(true)) nf.selectInput.QueueManualState(false, 0f, false, false);
                Place(h, cs.start, cs.end); yield return null; yield return null;
                Add($"CASE_BEGIN expected={cs.expected} hand={(h == 0 ? "L" : "R")} start={cs.start:F4} end={cs.end:F4}");
                for (int i = 1; i <= cs.travel; i++) { Place(h, Vector3.Lerp(cs.start, cs.end, i / (float)cs.travel), cs.end); yield return null; Tick(h); }
                for (int i = 0; i < cs.hold; i++) { yield return null; Tick(h); }
                Add(Snap("ARRIVED", h));
                string hoverAtArrival = HoverNow(h);
                if (cs.doPress)
                {
                    for (int i = 0; i < cs.press; i++)
                    {
                        foreach (var nf in controllers[h].GetComponentsInChildren<NearFarInteractor>(true)) nf.selectInput.QueueManualState(true, 1f, i == 0, false);
                        yield return null; Tick(h);
                        if (i == cs.press / 2) Add(Snap("PRESSING", h));
                    }
                    string selNow = SelectNow(h);
                    foreach (var nf in controllers[h].GetComponentsInChildren<NearFarInteractor>(true)) nf.selectInput.QueueManualState(false, 0f, false, true);
                    yield return null;
                    foreach (var nf in controllers[h].GetComponentsInChildren<NearFarInteractor>(true)) nf.selectInput.QueueManualState(false, 0f, false, false);
                    for (int i = 0; i < cs.release; i++) yield return null;
                    Results.Add($"{cs.label} | expected={cs.expected} | hoverAtArrival=[{hoverAtArrival}] | selectedWhilePressing=[{selNow}] | everHover=[{string.Join(",", caseHover)}] | everSelect=[{string.Join(",", caseSelect)}] | keyPressed=[{string.Join(",", casePressed)}] | reachFlips=[{string.Join(" ", reachFlips)}]");
                }
                else
                {
                    Results.Add($"{cs.label} | expected={cs.expected} | hoverAtArrival=[{hoverAtArrival}] | everHover=[{string.Join(",", caseHover)}] | reachFlips=[{string.Join(" ", reachFlips)}]");
                }
                for (int i = 1; i <= cs.retreat; i++) { Place(h, Vector3.Lerp(cs.end, cs.start, i / (float)cs.retreat), cs.end); yield return null; Tick(h); }
                Add("CASE_END");
                Done++;
            }
        }
    }
}
