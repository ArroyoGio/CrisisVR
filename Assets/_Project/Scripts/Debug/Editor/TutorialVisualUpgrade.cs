using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrisisVR.Editor
{
    public static class TutorialVisualUpgrade
    {
        const string RootPath = "Assets/_Project/";
        const string MeshPath = RootPath + "Art/MetaTouchPlus/Highlights/";
        const string Icons = RootPath + "Art/Icons/Lucide/";

        [MenuItem("CrisisVR/T-23/Aplicar ayudas de contraste y apuntado")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Tutorial.unity") throw new InvalidOperationException("T-23: requiere Tutorial activa fuera de Play.");
            var flow = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TutorialFlow>(true)).Single();
            var cue = flow.controllerCue;
            Undo.RecordObject(cue, "T-23 ayudas visuales");
            var material = AmberMaterial();
            Directory.CreateDirectory(MeshPath);
            AssetDatabase.Refresh();
            cue.leftJoystickHighlight = Highlight(cue.leftModel, cue.leftJoystick, "JoystickLeft", material);
            cue.rightJoystickHighlight = Highlight(cue.rightModel, cue.rightJoystick, "JoystickRight", material);
            cue.leftTriggerHighlight = Highlight(cue.leftModel, cue.leftTrigger, "TriggerLeft", material);
            cue.rightTriggerHighlight = Highlight(cue.rightModel, cue.rightTrigger, "TriggerRight", material);
            cue.moveIcon = ImportIcon("move"); cue.turnIcon = ImportIcon("rotate-ccw");
            cue.selectIcon = ImportIcon("mouse-pointer-click"); cue.pauseIcon = ImportIcon("pause");
            var existingIcon = flow.panel.transform.Find("ActionIcon");
            var iconObject = existingIcon != null ? existingIcon.gameObject : new GameObject("ActionIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(flow.panel.transform,false);
            var rect = iconObject.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(-580,-104); rect.sizeDelta = new Vector2(112,112);
            cue.actionIcon = iconObject.GetComponent<Image>(); cue.actionIcon.raycastTarget = false; cue.actionIcon.preserveAspect = true;
            // La ayuda larga permanece a la izquierda; el icono no tapa sus líneas.
            flow.panel.hint.rectTransform.anchoredPosition = new Vector2(-250,-119);
            flow.panel.hint.rectTransform.sizeDelta = new Vector2(490,140);
            cue.aimDemonstration = BuildAim(flow, material);
            TutorialSequenceUpgrade.Configure(flow,material);
            flow.panel.Show(flow.exercises[0],false,true);
            cue.Show(TutorialStep.Interface,false);
            EditorUtility.SetDirty(cue);
            // Con el jugador separado, las referencias cruzadas pertenecen a la escena.
            if (flow.input.playerOrigin == null || flow.input.playerOrigin.transform.IsChildOf(flow.transform))
                UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(flow.gameObject,RootPath+"Prefabs/Tutorial/TutorialRoot.prefab",InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        static Material AmberMaterial()
        {
            string path = RootPath + "Materials/Tutorial/ControlAmber.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = new Color(1f,0.70f,0.12f);
            AssetDatabase.CreateAsset(material,path); return material;
        }
        internal static GameObject Highlight(GameObject model, Transform bone, string name, Material material)
        {
            var existing = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
            var source = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>Array.IndexOf(r.bones,bone)>=0);
            int index = Array.IndexOf(source.bones,bone);
            var weights = source.sharedMesh.boneWeights;
            var triangles = source.sharedMesh.triangles;
            var selected = new List<int>();
            bool Uses(int vertex)
            {
                var w = weights[vertex];
                return (w.boneIndex0==index && w.weight0>0.4f) || (w.boneIndex1==index && w.weight1>0.4f) || (w.boneIndex2==index && w.weight2>0.4f) || (w.boneIndex3==index && w.weight3>0.4f);
            }
            for (int i=0;i<triangles.Length;i+=3)
                if (Uses(triangles[i]) && Uses(triangles[i+1]) && Uses(triangles[i+2]))
                { selected.Add(triangles[i]); selected.Add(triangles[i+1]); selected.Add(triangles[i+2]); }
            if (selected.Count==0) throw new InvalidOperationException("T-23: hueso sin geometría identificable: "+bone.name);
            string path=MeshPath+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var indices=selected.Distinct().ToArray();
            var remap=indices.Select((old,i)=>new{old,i}).ToDictionary(v=>v.old,v=>v.i);
            var originalVertices=source.sharedMesh.vertices; var originalNormals=source.sharedMesh.normals;
            var compact=new Mesh{name=name};
            compact.vertices=indices.Select(i=>originalVertices[i]+originalNormals[i]*0.008f).ToArray();
            compact.normals=indices.Select(i=>originalNormals[i]).ToArray();
            compact.boneWeights=indices.Select(i=>weights[i]).ToArray(); compact.bindposes=source.sharedMesh.bindposes;
            compact.triangles=selected.Select(i=>remap[i]).ToArray(); compact.RecalculateBounds();
            if(mesh==null) { mesh=compact; AssetDatabase.CreateAsset(mesh,path); }
            else { EditorUtility.CopySerialized(compact,mesh); UnityEngine.Object.DestroyImmediate(compact); EditorUtility.SetDirty(mesh); }
            var go=existing!=null ? existing.gameObject : new GameObject(name,typeof(SkinnedMeshRenderer)); go.transform.SetParent(source.transform,false);
            var renderer=go.GetComponent<SkinnedMeshRenderer>(); renderer.sharedMesh=mesh;
            renderer.bones=source.bones; renderer.rootBone=source.rootBone; renderer.localBounds=mesh.bounds;
            renderer.sharedMaterial=material; renderer.updateWhenOffscreen=true;
            Undo.RegisterCreatedObjectUndo(go,"T-23 resaltar control"); return go;
        }
        internal static Sprite ImportIcon(string name)
        {
            string path=Icons+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("T-23: icono no importado: "+path);
            if(importer.textureType!=TextureImporterType.Sprite)
            {
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static TutorialAimDemonstration BuildAim(TutorialFlow flow, Material material)
        {
            var previous=flow.transform.Find("AimDemonstration");
            if(previous!=null)
            {
                var existing=previous.GetComponent<TutorialAimDemonstration>();
                existing.emphasizedControl=flow.controllerCue.rightTrigger;
                existing.buttonIcon=previous.Find("ExampleButton/SelectionIcon").GetComponent<Image>();
                existing.buttonTarget=previous.Find("ExampleButton");
                return existing;
            }
            var root=new GameObject("AimDemonstration"); root.transform.SetParent(flow.transform,false);
            var demo=root.AddComponent<TutorialAimDemonstration>(); demo.flow=flow;
            demo.controller=flow.controllerCue.rightModel.transform;
            demo.emitter=demo.controller.GetComponentsInChildren<Transform>(true).First(t=>t.name.EndsWith("laser_begin"));
            demo.emphasizedControl=flow.controllerCue.rightTrigger;
            var canvasObject=new GameObject("ExampleButton",typeof(RectTransform),typeof(Canvas),typeof(Image));
            canvasObject.transform.SetParent(root.transform,false);
            canvasObject.transform.localPosition=new Vector3(0.76f,2.28f,2.85f);
            canvasObject.transform.localScale=Vector3.one;
            canvasObject.GetComponent<RectTransform>().sizeDelta=new Vector2(0.30f,0.20f);
            var canvas=canvasObject.GetComponent<Canvas>(); canvas.renderMode=RenderMode.WorldSpace;
            demo.target=canvasObject.transform;
            demo.buttonTarget=demo.target;
            demo.buttonFace=canvasObject.GetComponent<Image>(); demo.buttonFace.raycastTarget=false;
            demo.buttonFace.color=demo.resting;
            var icon=new GameObject("SelectionIcon",typeof(RectTransform),typeof(Image)); icon.transform.SetParent(canvasObject.transform,false);
            icon.GetComponent<RectTransform>().sizeDelta=new Vector2(0.11f,0.11f);
            var image=icon.GetComponent<Image>(); image.sprite=flow.controllerCue.selectIcon; image.raycastTarget=false; image.preserveAspect=true;
            demo.buttonIcon=image;
            var line=new GameObject("ExampleRay",typeof(LineRenderer)); line.transform.SetParent(root.transform,false);
            demo.ray=line.GetComponent<LineRenderer>(); demo.ray.sharedMaterial=material;
            demo.ray.positionCount=2; demo.ray.startWidth=0.006f; demo.ray.endWidth=0.006f; demo.ray.useWorldSpace=true;
            root.SetActive(false);
            Undo.RegisterCreatedObjectUndo(root,"T-23 demostración de apuntado"); return demo;
        }
    }
}
