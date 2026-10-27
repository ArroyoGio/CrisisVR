using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CrisisVR.Editor
{
    public static class TutorialSequenceUpgrade
    {
        public static readonly TutorialStep[] StepOrder = {
            TutorialStep.Interface, TutorialStep.Orientation, TutorialStep.Movement, TutorialStep.Turn,
            TutorialStep.Object, TutorialStep.Options, TutorialStep.Grab, TutorialStep.Wait,
            TutorialStep.PauseInfo, TutorialStep.Integration, TutorialStep.Finished
        };

        internal static void Configure(TutorialFlow flow, Material material)
        {
            var texts=TutorialSceneSetup.ReadNodes("Assets/_Project/Yarn/Marco.yarn");
            flow.exercises=StepOrder.Select(step=>{
                var lines=texts["Tutorial_"+step];
                return new TutorialExercise{step=step,title=lines[0],instruction=lines[1],help=lines[2]};
            }).ToArray();
            var player=flow.input.playerOrigin;
            flow.input.interactors=player!=null ? player.GetComponentsInChildren<NearFarInteractor>(true) : flow.GetComponentsInChildren<NearFarInteractor>(true);
            foreach (var cue in flow.objectCues)
            {
                var body=cue.GetComponent<Rigidbody>();
                if (body==null) body=cue.gameObject.AddComponent<Rigidbody>();
                body.useGravity=false; body.isKinematic=true;
                var grab=cue.GetComponent<XRGrabInteractable>();
                if (grab==null) grab=cue.gameObject.AddComponent<XRGrabInteractable>();
                grab.throwOnDetach=false;
                grab.movementType=XRBaseInteractable.MovementType.Kinematic;
                grab.useDynamicAttach=true;
                grab.enabled=false;
            }
            var demo=flow.controllerCue.aimDemonstration;
            demo.buttonTarget.localScale=Vector3.one;
            const string cubePath="Assets/_Project/Materials/Tutorial/DemoObjectLit.mat";
            var cubeMaterial=AssetDatabase.LoadAssetAtPath<Material>(cubePath);
            if(cubeMaterial==null)
            {
                cubeMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                cubeMaterial.color=new Color(0.24f,0.52f,0.70f); cubeMaterial.SetFloat("_Smoothness",0.15f);
                AssetDatabase.CreateAsset(cubeMaterial,cubePath);
            }
            demo.objectExample=Cube("ExampleSelectableObject",demo.transform,new Vector3(0.76f,2.28f,2.85f),cubeMaterial);
            demo.objectRenderer=demo.objectExample.GetComponent<Renderer>();
            demo.secondObjectExample=Cube("ExampleAlternativeObject",demo.transform,new Vector3(1.08f,2.28f,2.85f),cubeMaterial);
            demo.objectExample.SetActive(false); demo.secondObjectExample.SetActive(false);
            var cueController=flow.controllerCue;
            demo.emphasizedSurface=cueController.rightTriggerHighlight.GetComponent<Renderer>();
            var grip=cueController.rightModel.GetComponentsInChildren<Transform>(true).First(t=>t.name.EndsWith("_b_trigger_grip"));
            cueController.rightGripHighlight=TutorialVisualUpgrade.Highlight(cueController.rightModel,grip,"GripRight",material);
            cueController.grabIcon=TutorialVisualUpgrade.ImportIcon("hand");
            var existing=flow.transform.Find("GrabDemonstration");
            var root=existing!=null ? existing.gameObject : new GameObject("GrabDemonstration");
            root.transform.SetParent(flow.transform,false);
            var grabDemo=root.GetComponent<TutorialGrabDemonstration>();
            if (grabDemo==null) grabDemo=root.AddComponent<TutorialGrabDemonstration>();
            grabDemo.flow=flow; grabDemo.controller=cueController.rightModel.transform; grabDemo.grip=grip;
            grabDemo.gripSurface=cueController.rightGripHighlight.GetComponent<Renderer>();
            grabDemo.sample=Cube("ExampleHeldObject",root.transform,new Vector3(0.83f,2.03f,2.4f),cubeMaterial).transform;
            cueController.grabDemonstration=grabDemo;
            root.SetActive(false);
        }
        static GameObject Cube(string name, Transform parent, Vector3 position, Material material)
        {
            var found=parent.Find(name);
            if(found!=null)
            {
                found.GetComponent<Renderer>().sharedMaterial=material;
                found.localPosition=position; found.localScale=Vector3.one*0.18f;
                found.localRotation=Quaternion.Euler(15,30,0);
                return found.gameObject;
            }
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name=name;
            cube.transform.SetParent(parent,false); cube.transform.localPosition=position; cube.transform.localScale=Vector3.one*0.18f;
            cube.transform.localRotation=Quaternion.Euler(15,30,0);
            cube.GetComponent<Collider>().enabled=false;
            cube.GetComponent<Renderer>().sharedMaterial=material;
            UnityEditor.Undo.RegisterCreatedObjectUndo(cube,"T-23 objeto de demostración");
            return cube;
        }
    }
}
