using ActionMenuApi.Api;
using CameraAnimation;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;
using VRC.UserCamera;
using VRCSDK2;

[assembly: MelonInfo(typeof(CameraAnimationMod), "Camera Animations", "1.1.0", "Eric van Fandenfart")]
[assembly: MelonAdditionalDependencies("ActionMenuApi")]
[assembly: MelonGame]

namespace CameraAnimation
{

    public class CameraAnimationMod : MelonMod
    {
        private GameObject originalCamera;
        private GameObject originalVideoCamera;
        private readonly List<StoreTransform> positions = new List<StoreTransform>();
        private float speed = 0.5f;
        private bool loopMode = false;
        private LineRenderer lineRenderer;
        private Animation anim = null;
        private bool shouldBePlaying = false;

        public override void OnApplicationStart()
        {
            VRCActionMenuPage.AddSubMenu(ActionMenuPage.Main,
                "Camera Animation",
                delegate {

                    originalCamera = GameObject.Find("_Application/PhotoCamera") ?? GameObject.Find("_Application/UserCamera/PhotoCamera") ?? GameObject.Find("_Application/TrackingVolume/PlayerObjects/UserCamera/PhotoCamera");
                    if (originalCamera == null) return;

                    originalVideoCamera = originalCamera.transform.Find("VideoCamera").gameObject;

                    if (lineRenderer == null)
                    {
                        lineRenderer = new GameObject("CameraAnimations") { layer = LayerMask.NameToLayer("UI") }.AddComponent<LineRenderer>();
                        lineRenderer.SetWidth(0.05f, 0.05f);
                        GameObject.DontDestroyOnLoad(lineRenderer);
                    }

                    CustomSubMenu.AddButton("Update Linerenderer", () => UpdateLineRenderer());
                    CustomSubMenu.AddButton("Save Pos", () => AddCurrentPositionToList());
                    CustomSubMenu.AddButton("Delete last Pos", () => RemoveLastPosition());
                    CustomSubMenu.AddButton("Play Anim", () => PlayAnimation());
                    CustomSubMenu.AddButton("Stop Anim", () => StopAnimation());
                    CustomSubMenu.AddButton("Clear Anim", () => 
                    { 
                        positions.Clear(); 
                        lineRenderer.positionCount = 0;
                        for (int i = 0; i < lineRenderer.transform.childCount; i++)
                        {
                            GameObject.Destroy(lineRenderer.transform.GetChild(i).gameObject);
                        }
                        
                    });
                    CustomSubMenu.AddRadialPuppet("Speed", (x) => speed = x, speed);
                    CustomSubMenu.AddToggle("Loop mode", loopMode, (x) => { loopMode = x; UpdateLineRenderer(); });
                }
            );
            MelonLogger.Msg("Actionmenu initialised");
        }

        

        
        public void RemoveLastPosition()
        {
            if (positions.Count == 0) return;
            GameObject.Destroy(lineRenderer.transform.GetChild(positions.Count - 1).gameObject);
            positions.RemoveAt(positions.Count - 1);
            UpdateLineRenderer();
        }

        public void AddCurrentPositionToList()
        {
            var oldValue = UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0;
            UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = UserCameraSpace.World;

            var photoCameraClone =  GameObject.Instantiate(originalCamera, lineRenderer.transform);
            photoCameraClone.GetComponent<Camera>().enabled = false;
            photoCameraClone.GetComponent<VRC_Pickup>().pickupable = true;
            photoCameraClone.GetComponentInChildren<MeshRenderer>().material = UserCameraController.field_Internal_Static_UserCameraController_0.field_Public_Material_3;
            photoCameraClone.transform.position = originalCamera.transform.position;
            photoCameraClone.transform.rotation = originalCamera.transform.rotation;
            positions.Add(new StoreTransform(photoCameraClone));

            UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = oldValue;
            UpdateLineRenderer();
        }

        public void UpdateLineRenderer()
        {
            if (positions.Count == 0) { lineRenderer.positionCount = 0; return; }
            
            var (curveX,curveY,curveZ,_,_,_) = CreateCurves();
            float lasttime = curveX.keys[curveX.length - 1].time;
            int countPoints = positions.Count * 10;
            float fraction = lasttime / countPoints;
            lineRenderer.positionCount = countPoints+1;
            for (int i = 0; i <= countPoints; i++)
            {
                lineRenderer.SetPosition(i, new Vector3(curveX.Evaluate(fraction * i), curveY.Evaluate(fraction * i), curveZ.Evaluate(fraction * i)));
            }
            
        }
        
        public override void OnLateUpdate()
        {
            if (anim == null || originalCamera == null) return;




            if (anim.isPlaying)
            {
                originalVideoCamera.active = true;
            }
            else if (loopMode)
            {
                originalVideoCamera.active = true;
                PlayAnimation();
            }
            else if(shouldBePlaying){
                originalVideoCamera.active = false;
                shouldBePlaying = false;
                UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = UserCameraSpace.Attached;
            }
                
        }

        public AnimationClip CreateClip()
        {
            
            AnimationCurve curveX, curveY, curveZ, curveRotX, curveRotY, curveRotZ;

            (curveX, curveY, curveZ, curveRotX, curveRotY, curveRotZ) = CreateCurves();


            AnimationClip clip = new AnimationClip();
            clip.legacy = true;

            var type = UnhollowerRuntimeLib.Il2CppType.Of<Transform>();
            clip.SetCurve("", type, "localPosition.x", curveX);
            clip.SetCurve("", type, "localPosition.y", curveY);
            clip.SetCurve("", type, "localPosition.z", curveZ);

            clip.SetCurve("", type, "localEulerAngles.x", curveRotX);
            clip.SetCurve("", type, "localEulerAngles.y", curveRotY);
            clip.SetCurve("", type, "localEulerAngles.z", curveRotZ);

            clip.EnsureQuaternionContinuity();


            return clip;


        }

        private (AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveRotX, AnimationCurve curveRotY, AnimationCurve curveRotZ) CreateCurves()
        {
            AnimationCurve curveX = new AnimationCurve();
            AnimationCurve curveY = new AnimationCurve();
            AnimationCurve curveZ = new AnimationCurve();
            AnimationCurve curveRotX = new AnimationCurve();
            AnimationCurve curveRotY = new AnimationCurve();
            AnimationCurve curveRotZ = new AnimationCurve();
            float time = 0;

            if (loopMode)
                positions.Add(new StoreTransform(positions[0].photocamera));

            for (int i = 0; i < positions.Count; i++)
            {
                curveX.AddKey(time, positions[i].position.x);
                curveY.AddKey(time, positions[i].position.y);
                curveZ.AddKey(time, positions[i].position.z);

                float rotX = positions[i].eulerAngles.x;
                float rotY = positions[i].eulerAngles.y;
                float rotZ = positions[i].eulerAngles.z;
                if (i == 0)
                {
                    positions[i].eulerAnglesCorrected = new Vector3(rotX, rotY, rotZ);
                }else if (i != 0)
                {
                    //correct rotations to be negative if needed and writeback for next itteration
                    float lastRotX = positions[i - 1].eulerAngles.x;
                    float lastRotY = positions[i - 1].eulerAngles.y;
                    float lastRotZ = positions[i - 1].eulerAngles.z;

                    float lastRotXAdj = positions[i - 1].eulerAnglesCorrected.x;
                    float lastRotYAdj = positions[i - 1].eulerAnglesCorrected.y;
                    float lastRotZAdj = positions[i - 1].eulerAnglesCorrected.z;

                    float diffX = rotX - lastRotX;
                    float diffY = rotY - lastRotY;
                    float diffZ = rotZ - lastRotZ;

                    if (diffX > 180)
                        diffX = diffX - 360;
                    if (diffY > 180)
                        diffY = diffY - 360;
                    if (diffZ > 180)
                        diffZ = diffZ - 360;

                    if (diffX < -180)
                        diffX = 360 + diffX;
                    if (diffY < -180)
                        diffY = 360 + diffY;
                    if (diffZ < -180)
                        diffZ = 360 + diffZ;

                    rotX = lastRotXAdj + diffX;
                    rotY = lastRotYAdj + diffY;
                    rotZ = lastRotZAdj + diffZ;


                    positions[i].eulerAnglesCorrected = new Vector3(rotX, rotY, rotZ);
                } // 342 -> 60   360+60 = 420
                rotX = positions[i].eulerAnglesCorrected.x;
                rotY = positions[i].eulerAnglesCorrected.y;
                rotZ = positions[i].eulerAnglesCorrected.z;

                curveRotX.AddKey(time, rotX);
                curveRotY.AddKey(time, rotY);
                curveRotZ.AddKey(time, rotZ);

                time += Mathf.Lerp(0.5f, 5f, 1 - speed);
                
            }
            if (loopMode)
                positions.RemoveAt(positions.Count - 1);

            return (curveX, curveY, curveZ, curveRotX, curveRotY, curveRotZ);
        }

        public void PlayAnimation()
        {
            UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = UserCameraSpace.World;
            anim = originalCamera.GetComponent<Animation>();
            if(anim == null)
                anim = originalCamera.AddComponent<Animation>();

            var clip = CreateClip();
            anim.AddClip(clip, clip.name);
            anim.Play(clip.name);
            shouldBePlaying = true;
        }
        private void StopAnimation()
        {
            if (anim != null)
            {
                anim.Stop();
            }
        }

    }
    
    public class StoreTransform
    {
        public GameObject photocamera;
        public Vector3 position => photocamera.transform.position;
        public Vector3 eulerAngles => photocamera.transform.eulerAngles;
        public Vector3 eulerAnglesCorrected;

        public StoreTransform(GameObject camera)
        {
            photocamera = camera;
        } 
    }
}
