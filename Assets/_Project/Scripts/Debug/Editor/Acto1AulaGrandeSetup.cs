using System;
using System.Linq;
using System.Reflection;
using CrisisVR;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Yarn.Unity;

namespace CrisisVR.Editor
{
    // Versión 2 de Acto1SceneSetup: aula grande y cerrada (13,3 x 10,7 m), 14 alumnos, mobiliario y ambientación.
    // Regenera Assets/Scenes/Acto1.unity desde cero. No modifica Act1.unity ni ninguna otra escena, y deja intacto
    // Acto1SceneSetup.cs (versión 1, aula pequeña). Reutiliza Act1Flow, Act1ProfessorMotion, Act1TransitionCard,
    // Yarn Spinner (CrisisVR.yarnproject), el XR Origin y el simulador de XRI, los prefabs de Assets/school
    // y los materiales de Assets/_Project/Materials/Act1.
    public static class Acto1AulaGrandeSetup
    {
        const string ScenePath = "Assets/Scenes/Acto1.unity";
        const string RigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
        const string FloorPath = "Assets/school/Prefabs/road/floor.prefab";
        const string WallPath = "Assets/school/Prefabs/props/wall.prefab";
        const string BoardPath = "Assets/school/Prefabs/props/board.prefab";
        const string DoorPath = "Assets/school/Prefabs/props/a door.prefab";
        const string TablePath = "Assets/school/Prefabs/props/table3.prefab";
        const string ChairPath = "Assets/school/Prefabs/props/chair.prefab";
        const string LaptopPath = "Assets/school/Prefabs/props/a laptop.prefab";
        const string LockerPath = "Assets/school/Prefabs/props/locker_1.prefab";
        const string RackPath = "Assets/school/Prefabs/props/rack.prefab";
        const string ShowcasePath = "Assets/school/Prefabs/props/showcase.prefab";
        const string SpeakerPath = "Assets/school/Prefabs/props/speaker.prefab";
        const string ProjectorPath = "Assets/school/Prefabs/props/projector.prefab";
        const string SheetPath = "Assets/school/Prefabs/props/sheet.prefab";
        const string YarnProjectPath = "Assets/_Project/Yarn/CrisisVR.yarnproject";
        const string YarnPrefabGuid = "52571f68872914e24837210513edea1d";
        const string MaterialFolder = "Assets/_Project/Materials/Acto1";

        // Aula de 13,26 x 10,68 m centrada en el origen (3 x 2 baldosas de piso School). El participante mira hacia +Z (Miguel y la pizarra).
        const float HalfX = 6.63f;
        const float HalfZ = 5.34f;
        const float WallHeightScale = .7f;
        const float CeilingY = 3.1f;

        static readonly Vector3 ParticipantDeskCenter = new Vector3(0f, 0f, 0.6f);
        static readonly Vector3 ChairCenter = new Vector3(0f, 0f, -0.1f);
        static readonly Vector3 ProfessorDeskCenter = new Vector3(0f, 0f, 3.95f);
        static readonly Vector3 MiguelPosition = new Vector3(1.4f, 0f, 2.55f);

        // Filas (z, de adelante hacia atrás) y columnas (x) de pupitres. P = participante, 1 = alumno, 0 = silla vacía.
        static readonly float[] StudentRowsZ = { 0.6f, -0.9f, -2.4f, -3.9f };
        static readonly float[] StudentColsX = { -4f, -2f, 0f, 2f, 4f };
        static readonly string[] SeatMask = { "11P01", "11101", "10111", "01110" };

        [MenuItem("CrisisVR/Acto 1/Crear escena Acto1 (aula grande)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de crear Acto1.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var open = SceneManager.GetSceneAt(i);
                if (open.isDirty && open.path != ScenePath)
                    throw new InvalidOperationException("Guarda o descarta los cambios pendientes de la escena abierta antes de crear Acto1.");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureFolder("Assets/_Project/Materials", "Acto1");

            var aula = new GameObject("Acto1 Aula");
            var alumnos = new GameObject("Acto1 Alumnos");
            var interaccion = new GameObject("Acto1 Interaccion");
            var zonas = new GameObject("Acto1 Zonas VR");

            BuildShell(scene, aula.transform);
            BuildLighting(aula.transform);
            BuildDecor(scene, aula.transform);
            BuildFurniture(scene, interaccion.transform);
            BuildStudents(scene, alumnos.transform);
            BuildInteractiveProps(interaccion.transform);
            var miguel = BuildMiguel(interaccion.transform);
            BuildZones(zonas.transform);

            MakeStatic(aula.transform);
            MakeStatic(alumnos.transform);
            MakeStatic(interaccion.transform.Find("Mobiliario"));

            var flow = interaccion.AddComponent<Act1Flow>();
            var rig = BuildPlayer(scene);
            BuildYarnAndTransition(scene, rig, flow, miguel);
            WireFlow(flow, interaccion.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Acto1 (aula grande) creada en " + ScenePath + ": aula cerrada 13x10 m, escritorios, laptop, Miguel, 14 alumnos, XR Origin + simulador, Yarn y Act1Flow.");
        }

        // ---------- Aula ----------

        static void BuildShell(Scene scene, Transform parent)
        {
            var shell = Group(parent, "Estructura");
            // Piso: 3 x 2 baldosas del prefab School; la cara superior queda en y = 0.
            foreach (var x in new[] { -4.42f, 0f, 4.42f })
                foreach (var z in new[] { -2.67f, 2.67f })
                    Place(scene, shell, "Piso " + x.ToString("0.0") + "," + z.ToString("0.0"), FloorPath, Vector3.one, 0, new Vector3(x, 0, z), -.12f);

            var wallScaleSide = new Vector3(1f, WallHeightScale, 1f);
            var wallScaleEnd = new Vector3(1f, WallHeightScale, .83f);
            foreach (var z in new[] { -2.67f, 2.67f })
            {
                Place(scene, shell, "Muro izquierdo " + z.ToString("0.0"), WallPath, wallScaleSide, 0, new Vector3(-HalfX - .06f, 0, z), 0);
                Place(scene, shell, "Muro derecho " + z.ToString("0.0"), WallPath, wallScaleSide, 180, new Vector3(HalfX + .06f, 0, z), 0);
            }
            foreach (var x in new[] { -4.42f, 0f, 4.42f })
            {
                Place(scene, shell, "Muro frontal " + x.ToString("0.0"), WallPath, wallScaleEnd, 90, new Vector3(x, 0, HalfZ + .06f), 0);
                Place(scene, shell, "Muro posterior " + x.ToString("0.0"), WallPath, wallScaleEnd, -90, new Vector3(x, 0, -HalfZ - .06f), 0);
            }

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Techo";
            ceiling.transform.SetParent(shell, false);
            ceiling.transform.position = new Vector3(0, CeilingY + .05f, 0);
            ceiling.transform.localScale = new Vector3(HalfX * 2 + .3f, .1f, HalfZ * 2 + .3f);
            ceiling.GetComponent<Renderer>().sharedMaterial = Mat("Acto1 techo", new Color(.86f, .87f, .88f));

            // Telón exterior luminoso: cierra por completo el espacio (nada de cielo ni vacío tras ventanas o rendijas).
            var outside = Group(shell, "Exterior cerrado");
            var haze = UnlitMat("Acto1 exterior luz", new Color(.80f, .86f, .94f));
            var lenZ = HalfZ * 2 + 3f;
            var lenX = HalfX * 2 + 3f;
            Slab(outside, "Telón izquierdo", new Vector3(-HalfX - .8f, 1.55f, 0), new Vector3(.1f, 3.8f, lenZ), haze);
            Slab(outside, "Telón derecho", new Vector3(HalfX + .8f, 1.55f, 0), new Vector3(.1f, 3.8f, lenZ), haze);
            Slab(outside, "Telón frontal", new Vector3(0, 1.55f, HalfZ + .8f), new Vector3(lenX, 3.8f, .1f), haze);
            Slab(outside, "Telón posterior", new Vector3(0, 1.55f, -HalfZ - .8f), new Vector3(lenX, 3.8f, .1f), haze);
            Slab(outside, "Telón suelo", new Vector3(0, -.3f, 0), new Vector3(lenX, .1f, lenZ), Mat("Acto1 techo", new Color(.5f, .5f, .5f)));

            // Pizarra grande en el muro frontal (detrás de Miguel) y puerta en el muro posterior.
            Place(scene, shell, "Pizarra", BoardPath, Vector3.one * .8f, 180, new Vector3(0, 0, HalfZ - .14f), .95f);
            Place(scene, shell, "Puerta", DoorPath, Vector3.one, 0, new Vector3(HalfX - 1.0f, 0, -HalfZ + .08f), 0);
        }

        static void BuildLighting(Transform parent)
        {
            var lights = Group(parent, "Iluminación");
            var sun = new GameObject("Luz direccional tenue", typeof(Light));
            sun.transform.SetParent(lights, false);
            sun.transform.rotation = Quaternion.Euler(60, 20, 0);
            var sunLight = sun.GetComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = .3f;
            sunLight.color = new Color(1f, .96f, .88f);
            sunLight.shadows = LightShadows.None;

            // Solo 4 luces puntuales sin sombras (límite razonable de luces por objeto en Quest 3).
            foreach (var x in new[] { -3.3f, 3.3f })
                foreach (var z in new[] { -2.6f, 2.2f })
                {
                    var go = new GameObject("Luz techo " + x + "," + z, typeof(Light));
                    go.transform.SetParent(lights, false);
                    go.transform.position = new Vector3(x, CeilingY - .4f, z);
                    var l = go.GetComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 10f;
                    l.intensity = 1.7f;
                    l.color = new Color(1f, .97f, .9f);
                    l.shadows = LightShadows.None;
                }

            // Paneles de luz emisivos (solo visuales): dan la sensación de aula real sin costo de luces.
            var panel = EmissiveMat("Acto1 panel luz", new Color(1f, .98f, .92f));
            var panels = Group(lights, "Paneles de techo");
            foreach (var x in new[] { -4f, 0f, 4f })
                foreach (var z in new[] { -3.6f, -.6f, 2.4f })
                    Slab(panels, "Panel " + x + "," + z, new Vector3(x, CeilingY - .02f, z), new Vector3(1.5f, .04f, .5f), panel);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f, .49f, .52f);
        }

        // ---------- Ambientación (prefabs School) ----------

        static void BuildDecor(Scene scene, Transform parent)
        {
            var decor = Group(parent, "Ambientación");

            // Casilleros altos contra el muro izquierdo.
            for (int i = 0; i < 9; i++)
                Place(scene, decor, "Casillero_" + (i + 1), LockerPath, Vector3.one * .8f, 90, new Vector3(-HalfX + .35f, 0, -4.6f + i * .56f), 0);

            // Estantes contra el muro derecho y vitrina al fondo.
            Place(scene, decor, "Estante_1", RackPath, Vector3.one * .8f, -90, new Vector3(HalfX - .45f, 0, -2.6f), 0);
            Place(scene, decor, "Estante_2", RackPath, Vector3.one * .8f, -90, new Vector3(HalfX - .45f, 0, 0.2f), 0);
            Place(scene, decor, "Vitrina", ShowcasePath, Vector3.one * .9f, 180, new Vector3(-4.6f, 0, -HalfZ + .75f), 0);

            // Parlantes en las esquinas del frente y proyector en el techo apuntando a la pizarra.
            Place(scene, decor, "Parlante_Izq", SpeakerPath, Vector3.one * 1.4f, 180, new Vector3(-HalfX + .6f, 0, HalfZ - .3f), 2.5f);
            Place(scene, decor, "Parlante_Der", SpeakerPath, Vector3.one * 1.4f, 180, new Vector3(HalfX - .6f, 0, HalfZ - .3f), 2.5f);
            Place(scene, decor, "Proyector", ProjectorPath, Vector3.one * .25f, 0, new Vector3(0, 0, -1.8f), CeilingY - .38f);

            // Corcho con avisos sobre el muro posterior.
            Slab(decor, "Corcho", new Vector3(-1.2f, 1.55f, -HalfZ + .04f), new Vector3(2.4f, 1.0f, .04f), Mat("Acto1 corcho", new Color(.62f, .46f, .28f)));
            for (int i = 0; i < 6; i++)
                Place(scene, decor, "Aviso_" + (i + 1), SheetPath, Vector3.one * .5f, 0,
                    new Vector3(-2.1f + i * .36f, 0, -HalfZ + .08f), 1.3f + (i % 2) * .22f);
        }

        // ---------- Mobiliario (prefabs School) ----------

        static void BuildFurniture(Scene scene, Transform parent)
        {
            var furniture = Group(parent, "Mobiliario");
            // El escritorio de Miguel y el del participante comparten prefab; el del participante es algo más bajo.
            Place(scene, furniture, "Escritorio_Profesor", TablePath, Vector3.one * .75f, 0, ProfessorDeskCenter, 0);
            Place(scene, furniture, "Escritorio_Participante", TablePath, Vector3.one * .65f, 0, ParticipantDeskCenter, 0);
            Place(scene, furniture, "Silla_Participante", ChairPath, Vector3.one, 0, ChairCenter, 0);
        }

        // ---------- Alumnos ----------

        static void BuildStudents(Scene scene, Transform parent)
        {
            var seats = Group(parent, "Pupitres");
            var people = Group(parent, "Alumnos");
            var shirts = new[]
            {
                new Color(.72f, .28f, .26f), new Color(.20f, .45f, .62f), new Color(.85f, .70f, .25f),
                new Color(.30f, .55f, .38f), new Color(.50f, .35f, .60f), new Color(.82f, .82f, .80f)
            };
            var skins = new[] { new Color(.78f, .58f, .45f), new Color(.62f, .43f, .32f), new Color(.88f, .70f, .58f), new Color(.45f, .30f, .22f) };
            var pants = Mat("Acto1 pantalón alumno", new Color(.17f, .20f, .28f));
            var rnd = new System.Random(11);
            int count = 0;

            for (int r = 0; r < StudentRowsZ.Length; r++)
                for (int c = 0; c < StudentColsX.Length; c++)
                {
                    char m = SeatMask[r][c];
                    if (m == 'P') continue;
                    float x = StudentColsX[c], z = StudentRowsZ[r];
                    var desk = Place(scene, seats, "Escritorio_" + (r + 1) + "_" + (c + 1), TablePath, Vector3.one * .65f, 0, new Vector3(x, 0, z), 0);
                    var chairPos = new Vector3(x, 0, z - .7f);
                    Place(scene, seats, "Silla_" + (r + 1) + "_" + (c + 1), ChairPath, Vector3.one, 0, chairPos, 0);
                    if (m != '1') continue;

                    count++;
                    Place(scene, seats, "Laptop_" + (r + 1) + "_" + (c + 1), LaptopPath, Vector3.one * .7f, 180,
                        new Vector3(x, 0, z + .12f), TopOf(desk.transform) + .002f);

                    // Alumno sentado (placeholder con la misma lógica que Miguel; sustituible por un modelo real).
                    var s = new GameObject("Alumno_" + count.ToString("00"));
                    s.transform.SetParent(people, false);
                    s.transform.position = chairPos;
                    s.transform.rotation = Quaternion.Euler(0, (float)(rnd.NextDouble() * 30 - 15), 0);
                    var shirt = Mat("Acto1 camisa " + (count % shirts.Length), shirts[count % shirts.Length]);
                    var skin = Mat("Acto1 piel " + (count % skins.Length), skins[(count * 3) % skins.Length]);
                    Prim(PrimitiveType.Capsule, s.transform, "Torso", new Vector3(0, .80f, -.05f), new Vector3(.38f, .27f, .22f), shirt);
                    Prim(PrimitiveType.Sphere, s.transform, "Cabeza", new Vector3(0, 1.24f, -.02f), new Vector3(.23f, .27f, .23f), skin);
                    Prim(PrimitiveType.Cube, s.transform, "Muslos", new Vector3(0, .52f, .20f), new Vector3(.32f, .12f, .42f), pants);
                    Prim(PrimitiveType.Cube, s.transform, "Pierna_Izq", new Vector3(-.09f, .27f, .40f), new Vector3(.12f, .46f, .12f), pants);
                    Prim(PrimitiveType.Cube, s.transform, "Pierna_Der", new Vector3(.09f, .27f, .40f), new Vector3(.12f, .46f, .12f), pants);
                    Prim(PrimitiveType.Cube, s.transform, "Brazo_Izq", new Vector3(-.21f, .86f, .15f), new Vector3(.08f, .08f, .40f), shirt);
                    Prim(PrimitiveType.Cube, s.transform, "Brazo_Der", new Vector3(.21f, .86f, .15f), new Vector3(.08f, .08f, .40f), shirt);
                }
        }

        static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // ---------- Interactivos ----------

        static void BuildInteractiveProps(Transform parent)
        {
            var desk = parent.Find("Mobiliario/Escritorio_Participante");
            if (desk == null) throw new InvalidOperationException("Falta Escritorio_Participante.");
            float top = TopOf(desk);
            var props = Group(parent, "Interactivos");

            // Laptop del participante; las teclas y la pantalla miran al participante (−Z).
            Place(SceneManager.GetActiveScene(), props, "Laptop_Participante", LaptopPath, Vector3.one * .7f, 180,
                new Vector3(ParticipantDeskCenter.x, 0, ParticipantDeskCenter.z + .12f), top + .002f);

            var green = Mat("Acto1 verde botón", new Color(.17f, .48f, .34f));
            var amber = Mat("Acto1 ámbar botón", new Color(.78f, .5f, .12f));
            var paper = Mat("Acto1 papel", new Color(.94f, .93f, .87f));
            float buttonZ = ParticipantDeskCenter.z - .22f;
            MakeButton(props, "ENTREGAR", "ENTREGAR", new Vector3(-.15f, top, buttonZ), green, true);
            MakeButton(props, "CORREGIR PROYECTO", "CORREGIR PROYECTO", new Vector3(.15f, top, buttonZ), amber, true);

            var card = new GameObject("grade_card");
            card.transform.SetParent(props, false);
            card.transform.position = new Vector3(-.36f, top + .006f, ParticipantDeskCenter.z + .12f);
            var baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCube.name = "Base";
            UnityEngine.Object.DestroyImmediate(baseCube.GetComponent<Collider>());
            baseCube.transform.SetParent(card.transform, false);
            baseCube.transform.localScale = new Vector3(.3f, .012f, .22f);
            baseCube.GetComponent<Renderer>().sharedMaterial = paper;
            var cardText = MakeLabel(card.transform, "CALIFICACIÓN\n08 / 20", new Vector3(0, .0075f, 0), new Vector2(.28f, .2f), new Color(.16f, .13f, .11f));
            cardText.fontSizeMax = .35f;
        }

        static GameObject MakeButton(Transform parent, string name, string label, Vector3 position, Material material, bool withInteractable)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position + Vector3.up * .02f;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Base";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localScale = new Vector3(.26f, .04f, .13f);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            if (withInteractable) root.AddComponent<XRSimpleInteractable>();
            MakeLabel(root.transform, label, new Vector3(0, .0225f, 0), new Vector2(.24f, .11f), Color.white);
            return root;
        }

        // Etiqueta TMP tendida sobre la superficie, legible desde el lado del participante (−Z mirando a +Z).
        static TextMeshPro MakeLabel(Transform parent, string text, Vector3 localPosition, Vector2 size, Color color)
        {
            var go = new GameObject("Etiqueta", typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var t = go.GetComponent<TextMeshPro>();
            t.text = text;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            t.enableAutoSizing = true;
            t.fontSizeMin = .08f;
            t.fontSizeMax = .22f;
            t.enableWordWrapping = true;
            t.rectTransform.sizeDelta = size;
            return t;
        }

        // ---------- Miguel ----------

        static GameObject BuildMiguel(Transform parent)
        {
            // Placeholder con la misma jerarquía que usa Act1ProfessorMotion (Cuerpo/Cabeza).
            // En Assets/school no hay ningún modelo de personaje; al importar el de Miguel basta con
            // asignarlo como hijo y apuntar head/torso en Act1ProfessorMotion.
            var miguel = new GameObject("Miguel");
            miguel.transform.SetParent(parent, false);
            miguel.transform.position = MiguelPosition;
            var toParticipant = new Vector3(0f, 0f, ChairCenter.z) - MiguelPosition;
            toParticipant.y = 0;
            miguel.transform.rotation = Quaternion.LookRotation(toParticipant);
            var body = Primitive(PrimitiveType.Capsule, miguel.transform, "Cuerpo", new Vector3(0, .82f, 0), new Vector3(.55f, .82f, .38f), new Color(.22f, .3f, .43f));
            var head = Primitive(PrimitiveType.Sphere, miguel.transform, "Cabeza", new Vector3(0, 1.70f, 0), new Vector3(.34f, .38f, .34f), new Color(.7f, .5f, .39f));
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            miguel.AddComponent<Act1ProfessorMotion>();
            return miguel;
        }

        static GameObject Primitive(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat("Acto1 " + name, color);
            return go;
        }

        // ---------- Zonas VR ----------

        static void BuildZones(Transform parent)
        {
            // Marcadores vacíos para ubicar al participante y reservar espacio libre alrededor de la interacción.
            Marker(parent, "Spawn_Participante", new Vector3(0, 0, ChairCenter.z - .1f));
            Marker(parent, "Zona_Interaccion_Laptop", new Vector3(0, 0, ParticipantDeskCenter.z - .5f));
            Marker(parent, "Zona_Miguel", MiguelPosition);
            Marker(parent, "Zona_Pizarra", new Vector3(0, 0, HalfZ - .6f));
            Marker(parent, "Zona_Alumnos", new Vector3(0, 0, -2.4f));
        }

        static void Marker(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
        }

        // ---------- Jugador XR ----------

        static GameObject BuildPlayer(Scene scene)
        {
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            var simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (rigPrefab == null || simulatorPrefab == null)
                throw new InvalidOperationException("No se encontraron los prefabs del XR Origin y del simulador de XRI 3.5.1.");

            var manager = new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
            SceneManager.MoveGameObjectToScene(manager, scene);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
            rig.name = "Acto1 XR Origin";
            rig.transform.SetPositionAndRotation(new Vector3(0, .02f, ChairCenter.z - .1f), Quaternion.identity);
            PrefabUtility.InstantiatePrefab(simulatorPrefab, scene);
            return rig;
        }

        static void BuildYarnAndTransition(Scene scene, GameObject rig, Act1Flow flow, GameObject miguel)
        {
            var origin = rig.GetComponent<XROrigin>();
            var camera = origin != null ? origin.Camera : null;
            if (camera == null) throw new InvalidOperationException("El XR Origin no tiene cámara asignada.");

            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(YarnProjectPath);
            if (project == null || project.Program == null)
                throw new InvalidOperationException("CrisisVR.yarnproject no se importó o tiene errores de Yarn.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(YarnPrefabGuid));
            if (prefab == null) throw new InvalidOperationException("No se encontró el prefab Dialogue System de Yarn Spinner.");

            var system = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            system.name = "CrisisVR Yarn Dialogue System";
            var runner = system.GetComponent<DialogueRunner>();
            if (runner == null) throw new InvalidOperationException("El prefab de Yarn Spinner no contiene DialogueRunner.");
            runner.SetProject(project);
            system.transform.SetParent(camera.transform, false);
            system.transform.localPosition = new Vector3(0, -.28f, 1.3f);
            system.transform.localRotation = Quaternion.identity;
            system.transform.localScale = Vector3.one;

            // Se reutiliza la configuración del panel VR ya validada en Act 1 (métodos privados, sin modificarlos).
            Invoke(typeof(YarnDialogueSetup), "ConfigureCanvas", system, camera);
            Invoke(typeof(YarnDialogueSetup), "ConfigureContinueButton", system, runner);
            foreach (var es in system.GetComponentsInChildren<EventSystem>(true)) es.gameObject.SetActive(false);

            var so = new SerializedObject(flow);
            so.FindProperty("dialogueRunner").objectReferenceValue = runner;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Tarjeta de transición con fundido (misma que Act 1).
            Invoke(typeof(Act1SceneSetup), "CreateTransitionCard", camera.transform);
        }

        static void WireFlow(Act1Flow flow, Transform interaccion)
        {
            var interactables = interaccion.GetComponentsInChildren<XRSimpleInteractable>(true);
            var deliver = interactables.Single(b => b.gameObject.name == "ENTREGAR");
            var correct = interactables.Single(b => b.gameObject.name == "CORREGIR PROYECTO");
            var so = new SerializedObject(flow);
            so.FindProperty("deliverInteractable").objectReferenceValue = deliver;
            so.FindProperty("correctInteractable").objectReferenceValue = correct;
            so.FindProperty("deliverButton").objectReferenceValue = deliver.gameObject;
            so.FindProperty("correctButton").objectReferenceValue = correct.gameObject;
            so.FindProperty("gradeCard").objectReferenceValue = interaccion.Find("Interactivos/grade_card").gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Utilidades ----------

        static object Invoke(Type type, string method, params object[] args)
        {
            var m = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (m == null) throw new InvalidOperationException("No se encontró " + type.Name + "." + method);
            return m.Invoke(null, args);
        }

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void MakeStatic(Transform root)
        {
            if (root == null) return;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        static GameObject Slab(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // Coloca un prefab de School por sus bounds de renderer: centro X/Z y base (bottomY) exactos.
        static GameObject Place(Scene scene, Transform parent, string name, string prefabPath, Vector3 scale, float yaw, Vector3 center, float bottomY)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new InvalidOperationException("No se encontró el prefab School: " + prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            instance.transform.localScale = scale;
            var b = BoundsOf(instance.transform);
            instance.transform.position = new Vector3(center.x - b.center.x, bottomY - b.min.y, center.z - b.center.z);
            return instance;
        }

        static Bounds BoundsOf(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException(t.name + " no tiene renderers.");
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        static float TopOf(Transform t) => BoundsOf(t).max.y;

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
        }

        // Reutiliza un material Act1 con el mismo nombre si existe; si no, crea uno propio en Materials/Acto1.
        static Material Mat(string name, Color color)
        {
            var reused = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Act1/" + name.Replace("Acto1", "Act1") + ".mat");
            if (reused != null) return reused;
            var path = MaterialFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Material UnlitMat(string name, Color color)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Material EmissiveMat(string name, Color color)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name, color = color };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.8f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
