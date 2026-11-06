using System.Collections.Generic;
using CrisisVR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrisisVR.Editor
{
    /// <summary>Prepara la malla existente de la laptop como cuerpo fijo y tapa con bisagra para Acto 1.</summary>
    public static class Act1LaptopInteractionSetup
    {
        const string ScenePath = "Assets/Scenes/Acto1.unity";
        const string MeshFolder = "Assets/_Project/Generated/Act1Laptop";
        const float HingeZ = -0.37f;
        const float LidSurfaceOffset = 0.045f;

        [MenuItem("CrisisVR/Acto 1/Preparar bisagra física de laptop")]
        public static void Prepare()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform laptop = FindInScene(scene, "Laptop_Participante");
            if (laptop == null)
                throw new System.InvalidOperationException("No se encontró Laptop_Participante en Acto1.");

            var sourceFilter = laptop.GetComponent<MeshFilter>();
            var sourceRenderer = laptop.GetComponent<MeshRenderer>();
            if (sourceFilter == null || sourceRenderer == null || sourceFilter.sharedMesh == null)
                throw new System.InvalidOperationException("La laptop no tiene la malla esperada para separar tapa y cuerpo.");

            if (PrefabUtility.IsPartOfPrefabInstance(laptop.gameObject))
                PrefabUtility.UnpackPrefabInstance(laptop.gameObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);

            // El primer intento de preparación puede haber asignado ya la malla del cuerpo antes de fallar.
            Mesh source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/school/Prefabs/props/a laptop.prefab")
                .GetComponent<MeshFilter>().sharedMesh;
            var lidTriangles = new List<int>();
            var bodyTriangles = new List<int>();
            int[] triangles = source.triangles;
            Vector3[] vertices = source.vertices;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 center = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f;
                bool isLid = center.y > 0.05f || center.z < -0.40f;
                var destination = isLid ? lidTriangles : bodyTriangles;
                destination.Add(triangles[i]);
                destination.Add(triangles[i + 1]);
                destination.Add(triangles[i + 2]);
            }

            if (lidTriangles.Count == 0 || bodyTriangles.Count == 0)
                throw new System.InvalidOperationException("La separación geométrica no encontró ambas partes de la laptop.");

            Transform hinge = laptop.Find("Bisagra_Laptop");
            Transform existingLid = hinge != null ? hinge.Find("Tapa_Interactiva") : null;
            Vector3 lidOriginInHinge = existingLid != null ? existingLid.localPosition : Vector3.zero;
            BoxCollider existingLidCollider = existingLid != null ? existingLid.GetComponent<BoxCollider>() : null;
            lidOriginInHinge += existingLidCollider != null
                ? existingLidCollider.center
                : new Vector3(0f, LidSurfaceOffset, 0.20f);

            EnsureFolder(MeshFolder);
            Mesh bodyMesh = SavePartMesh(source, bodyTriangles, "Laptop_Cuerpo.asset", false, Vector3.zero);
            Mesh lidMesh = SavePartMesh(source, lidTriangles, "Laptop_Tapa_Cerrada.asset", true, lidOriginInHinge);
            Vector3[] lidVertices = lidMesh.vertices;
            for (int i = 0; i < lidVertices.Length; i++) lidVertices[i].y += LidSurfaceOffset;
            lidMesh.vertices = lidVertices;
            lidMesh.RecalculateBounds();
            EditorUtility.SetDirty(lidMesh);

            laptop.gameObject.isStatic = false;
            sourceFilter.sharedMesh = bodyMesh;

            var bodyCollider = laptop.GetComponent<BoxCollider>();
            if (bodyCollider == null)
                bodyCollider = laptop.gameObject.AddComponent<BoxCollider>();
            bodyCollider.center = new Vector3(-0.05f, 0.005f, -0.18f);
            bodyCollider.size = new Vector3(0.52f, 0.05f, 0.43f);

            if (hinge == null)
            {
                var hingeObject = new GameObject("Bisagra_Laptop");
                Undo.RegisterCreatedObjectUndo(hingeObject, "Crear pivote de bisagra");
                hinge = hingeObject.transform;
                hinge.SetParent(laptop, false);
            }
            hinge.localPosition = new Vector3(-0.05f, 0f, HingeZ);
            hinge.localRotation = Quaternion.identity;
            hinge.localScale = Vector3.one;

            Transform lid = existingLid;
            GameObject lidObject;
            if (lid == null)
            {
                lidObject = new GameObject("Tapa_Interactiva");
                Undo.RegisterCreatedObjectUndo(lidObject, "Crear tapa interactiva");
                lid = lidObject.transform;
                lid.SetParent(hinge, false);
            }
            else
                lidObject = lid.gameObject;

            // Conserva el ajuste de posición del modelo y elimina solo la inclinación inicial.
            lid.localRotation = Quaternion.identity;

            var lidFilter = lidObject.GetComponent<MeshFilter>();
            if (lidFilter == null) lidFilter = lidObject.AddComponent<MeshFilter>();
            var lidRenderer = lidObject.GetComponent<MeshRenderer>();
            if (lidRenderer == null) lidRenderer = lidObject.AddComponent<MeshRenderer>();
            lidFilter.sharedMesh = lidMesh;
            lidRenderer.sharedMaterials = sourceRenderer.sharedMaterials;

            var lidCollider = lidObject.GetComponent<BoxCollider>();
            if (lidCollider == null)
            {
                lidCollider = lidObject.AddComponent<BoxCollider>();
                lidCollider.center = new Vector3(0f, LidSurfaceOffset, 0.20f);
                lidCollider.size = new Vector3(0.52f, 0.025f, 0.40f);
            }

            // Sitúa el origen de XRGrabInteractable en el centro del collider, manteniendo
            // sus hijos exactamente en su pose visual. Bisagra_Laptop sigue siendo el pivote.
            Vector3 colliderOffset = lidCollider.center;
            if (colliderOffset.sqrMagnitude > 0.000001f)
            {
                foreach (Transform child in lid) child.localPosition -= colliderOffset;
                lid.localPosition += colliderOffset;
                lidCollider.center = Vector3.zero;
            }

            Transform screen = FindInScene(scene, "Laptop_Pantalla");
            if (screen != null && screen != lid && screen.parent != lid)
            {
                // El panel fue colocado para coincidir con la laptop abierta en el modelo original.
                // Guardamos ese encaje en el espacio de la tapa y lo hacemos hijo del objeto articulado.
                Quaternion openRotation = Quaternion.AngleAxis(-120f, Vector3.right);
                Vector3 screenPositionInHinge = hinge.InverseTransformPoint(screen.position);
                Quaternion screenRotationInHinge = Quaternion.Inverse(hinge.rotation) * screen.rotation;
                Vector3 worldScale = screen.lossyScale;
                screen.SetParent(lid, false);
                screen.localPosition = Quaternion.Inverse(openRotation) * (screenPositionInHinge - lidOriginInHinge);
                screen.localRotation = Quaternion.Inverse(openRotation) * screenRotationInHinge;
                Vector3 lidScale = lid.lossyScale;
                screen.localScale = new Vector3(worldScale.x / lidScale.x, worldScale.y / lidScale.y, worldScale.z / lidScale.z);
            }

            var bodyRigidbody = laptop.GetComponent<Rigidbody>();
            if (bodyRigidbody == null) bodyRigidbody = laptop.gameObject.AddComponent<Rigidbody>();
            bodyRigidbody.isKinematic = true;
            bodyRigidbody.useGravity = false;

            var lidRigidbody = lidObject.GetComponent<Rigidbody>();
            if (lidRigidbody == null) lidRigidbody = lidObject.AddComponent<Rigidbody>();
            lidRigidbody.isKinematic = true;
            lidRigidbody.useGravity = false;
            lidRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            var grab = lidObject.GetComponent<XRGrabInteractable>();
            if (grab == null) grab = lidObject.AddComponent<XRGrabInteractable>();
            grab.movementType = XRGrabInteractable.MovementType.Kinematic;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            // Mantiene la tapa bajo su pivote y evita escalarla respecto al padre durante el agarre.
            grab.unparentTransformOnGrab = false;
            grab.retainTransformParent = true;
            grab.selectMode = InteractableSelectMode.Single;

            var transformer = lidObject.GetComponent<Act1LaptopHingeGrabTransformer>();
            if (transformer == null) transformer = lidObject.AddComponent<Act1LaptopHingeGrabTransformer>();
            var serializedTransformer = new SerializedObject(transformer);
            serializedTransformer.FindProperty("hinge").objectReferenceValue = hinge;
            serializedTransformer.FindProperty("maxOpenAngle").floatValue = 125f;
            serializedTransformer.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(laptop.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Laptop Acto 1 preparada: {bodyTriangles.Count / 3} triángulos en cuerpo, {lidTriangles.Count / 3} en tapa; bisagra limitada a 125°.");
        }

        static Transform FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == objectName)
                        return child;
            return null;
        }

        static Mesh SavePartMesh(Mesh source, List<int> triangles, string fileName, bool flattenLid, Vector3 originInHingeSpace)
        {
            string path = MeshFolder + "/" + fileName;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(fileName) };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            Vector3[] sourceVertices = source.vertices;
            Vector3[] sourceNormals = source.normals;
            Vector2[] sourceUv = source.uv;
            Vector4[] sourceTangents = source.tangents;
            Color[] sourceColors = source.colors;
            var remap = new Dictionary<int, int>();
            var partVertices = new List<Vector3>();
            var partNormals = new List<Vector3>();
            var partUv = new List<Vector2>();
            var partTangents = new List<Vector4>();
            var partColors = new List<Color>();
            var partTriangles = new List<int>(triangles.Count);
            Quaternion flattenRotation = Quaternion.AngleAxis(120f, Vector3.right);
            Vector3 hingePosition = new Vector3(-0.05f, 0f, HingeZ);
            foreach (int sourceIndex in triangles)
            {
                if (!remap.TryGetValue(sourceIndex, out int targetIndex))
                {
                    targetIndex = partVertices.Count;
                    remap.Add(sourceIndex, targetIndex);
                    Vector3 vertex = sourceVertices[sourceIndex];
                    partVertices.Add(flattenLid ? flattenRotation * (vertex - hingePosition) - originInHingeSpace : vertex);
                    if (sourceNormals.Length == sourceVertices.Length)
                        partNormals.Add(flattenLid ? flattenRotation * sourceNormals[sourceIndex] : sourceNormals[sourceIndex]);
                    if (sourceUv.Length == sourceVertices.Length) partUv.Add(sourceUv[sourceIndex]);
                    if (sourceTangents.Length == sourceVertices.Length) partTangents.Add(sourceTangents[sourceIndex]);
                    if (sourceColors.Length == sourceVertices.Length) partColors.Add(sourceColors[sourceIndex]);
                }
                partTriangles.Add(targetIndex);
            }

            mesh.SetVertices(partVertices);
            if (partNormals.Count == partVertices.Count) mesh.SetNormals(partNormals);
            if (partUv.Count == partVertices.Count) mesh.SetUVs(0, partUv);
            if (partTangents.Count == partVertices.Count) mesh.SetTangents(partTangents);
            if (partColors.Count == partVertices.Count) mesh.SetColors(partColors);
            mesh.SetTriangles(partTriangles, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Substring("Assets/".Length).Split('/'))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
