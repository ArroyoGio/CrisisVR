using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

namespace CrisisVR.Editor
{
    // Adaptación explícita al aula existente; no se ejecuta automáticamente.
    public static class TutorialWorldLayout
    {
        [MenuItem("CrisisVR/T-23/Separar jugador y adaptar al aula")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Tutorial.unity")
                throw new InvalidOperationException("T-23: requiere Tutorial activa, fuera de Play.");
            var flow = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TutorialFlow>(true)).Single();
            var world = flow.transform;
            var origin = flow.input.playerOrigin != null ? flow.input.playerOrigin : world.GetComponentInChildren<XROrigin>(true);
            if (origin == null) throw new InvalidOperationException("T-23: falta XR Origin.");
            // Resolver superficies del aula antes de modificar la jerarquía.
            var renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => r.gameObject.activeInHierarchy && !r.transform.IsChildOf(world) && !r.transform.IsChildOf(origin.transform)).ToArray();
            var table = renderers.Where(r => r.name.StartsWith("table2") && r.bounds.center.x > 64 && r.bounds.center.x < 77)
                .OrderBy(r => Mathf.Abs(r.bounds.center.x - 70.98f) + Mathf.Abs(r.bounds.center.z + 14.16f)).FirstOrDefault();
            var floor = renderers.FirstOrDefault(r => r.name == "floor (2)");
            if (table == null || floor == null || table.GetComponent<Collider>() == null || floor.GetComponent<Collider>() == null)
                throw new InvalidOperationException("T-23: revisar suelo y mesa central del aula; no se crea geometría sustituta.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("T-23 adaptar tutorial al aula");
            if (PrefabUtility.IsPartOfPrefabInstance(world.gameObject))
                PrefabUtility.UnpackPrefabInstance(world.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);

            Undo.RecordObject(world, "T-23 posición del contenido");
            var spawn = new Vector3(table.bounds.center.x, floor.bounds.max.y, table.bounds.min.z - 1.1f);
            world.SetPositionAndRotation(spawn, Quaternion.identity);
            Undo.RecordObject(world.gameObject, "T-23 nombre del contenido");
            world.name = "TutorialWorld";
            Undo.SetTransformParent(origin.transform, null, "T-23 separar jugador");
            Undo.RecordObject(origin.transform, "T-23 inicio del jugador");
            origin.transform.SetPositionAndRotation(spawn, Quaternion.identity);
            Undo.RecordObject(origin.gameObject, "T-23 nombre del jugador");
            origin.name = "TutorialPlayer";
            Undo.RecordObject(origin, "T-23 espacio XR");
            origin.Origin = origin.gameObject;
            origin.CameraYOffset = 1.6f;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            Undo.RecordObject(origin.CameraFloorOffsetObject.transform, "T-23 altura inicial");
            origin.CameraFloorOffsetObject.transform.localPosition = new Vector3(0, 1.6f, 0);
            Undo.RecordObject(flow.input, "T-23 referencia independiente al jugador");
            flow.input.playerOrigin = origin;

            var legacy = world.Find("LegacyTutorialGeometry");
            if (legacy == null)
            {
                var go = new GameObject("LegacyTutorialGeometry");
                Undo.RegisterCreatedObjectUndo(go, "T-23 conservar geometría anterior");
                go.transform.SetParent(world, false);
                legacy = go.transform;
            }
            foreach (var child in world.Cast<Transform>().ToArray())
                if (child.name == "PracticeFloor" || child.name == "Desk" || child.name == "DeskLeft" || child.name == "DeskRight" ||
                    child.name.StartsWith("PracticeBoundary") || child.name.StartsWith("MovementReference"))
                    Undo.SetTransformParent(child, legacy, "T-23 usar geometría del aula");
            Undo.RecordObject(legacy.gameObject, "T-23 desactivar geometría duplicada");
            legacy.gameObject.SetActive(false);
            for (int i = 0; i < flow.objectCues.Length; i++)
            {
                var target = flow.objectCues[i].transform;
                Undo.RecordObject(target, "T-23 apoyar práctica en mesa existente");
                target.position = new Vector3(table.bounds.center.x + (i == 0 ? -0.24f : 0.24f),
                    table.bounds.max.y + target.lossyScale.y * 0.5f + 0.01f, table.bounds.center.z - 0.35f);
            }
            EditorUtility.SetDirty(flow.input);
            EditorUtility.SetDirty(origin);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = origin.gameObject;
        }
    }
}
