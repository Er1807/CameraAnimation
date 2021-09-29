using ActionMenuApi.Api;
using CameraAnimation;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;

[assembly: MelonInfo(typeof(CameraAnimationMod), "Camera Animations", "1.0.5", "Eric van Fandenfart")]
[assembly: MelonAdditionalDependencies("ActionMenuApi")]
[assembly: MelonGame]

namespace CameraAnimation
{

    public class CameraAnimationMod : MelonMod
    {
        private GameObject PhotoCamera;
        private GameObject CloneVideoCamera;
        private readonly List<StoreTransform> positions = new List<StoreTransform>();
        private float speed = 0.5f;
        private bool loopMode = false;
        private LineRenderer lineRenderer;
        private GameObject Lens;
        private GameObject PhotoCameraClone;
        private Animation anim = null;

        public override void OnApplicationStart()
        {
            VRCActionMenuPage.AddSubMenu(ActionMenuPage.Main,
                "Camera Animation",
                delegate {
                    FindCamera();
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

        

        private void FindCamera()
        {
            if (PhotoCameraClone != null) return;
            PhotoCamera = GameObject.Find("_Application/TrackingVolume/PlayerObjects/UserCamera/PhotoCamera");
            Lens = GameObject.Find("_Application/TrackingVolume/PlayerObjects/UserCamera/PhotoCamera/camera_lens_mesh");
            PhotoCameraClone = GameObject.Instantiate(GameObject.Find("_Application/TrackingVolume/PlayerObjects/UserCamera/PhotoCamera"));
            
            CloneVideoCamera = PhotoCameraClone.transform.Find("VideoCamera").gameObject;
            CloneVideoCamera.SetActive(true);
            lineRenderer = new GameObject("CameraPath") { layer = LayerMask.NameToLayer("UI") }.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.05f;
            lineRenderer.positionCount = 0;
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
            positions.Add(PhotoCamera.transform.Copy());
            
            GameObject tempModel = GameObject.Instantiate(Lens, lineRenderer.gameObject.transform, true);
            tempModel.layer = LayerMask.NameToLayer("UI");

            UpdateLineRenderer();
        }

        public void UpdateLineRenderer()
        {
            if (positions.Count == 0) { lineRenderer.positionCount = 0; return; }
            
            var (curveX,curveY,curveZ,_,_,_) = CreateCurves();
            float lasttime = curveX.keys[curveX.length - 1].time;
            int countPoints = positions.Count * 10;
            float fraction = lasttime / countPoints;
            lineRenderer.positionCount = countPoints;
            for (int i = 0; i <= countPoints; i++)
            {
                lineRenderer.SetPosition(i, new Vector3(curveX.Evaluate(fraction * i), curveY.Evaluate(fraction * i), curveZ.Evaluate(fraction * i)));
            }
            
        }
        
        public override void OnLateUpdate()
        {
            if (CloneVideoCamera == null || anim == null) return;




            if (anim.isPlaying)
            {
                PhotoCameraClone.active = true;
            }
            else if (loopMode)
            {
                PhotoCameraClone.active = true;
                PlayAnimation();
            }
            else { 
                PhotoCameraClone.active = false;
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
                positions.Add(positions[0].Clone());

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
                }else if (i != 0 && !positions[i].corrected)
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
                positions[i].corrected = true;
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
            anim = PhotoCameraClone.GetComponent<Animation>();
            if(anim == null)
                anim = PhotoCameraClone.AddComponent<Animation>();

            var clip = CreateClip();
            anim.AddClip(clip, clip.name);
            anim.Play(clip.name);
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
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public Vector3 eulerAngles;
        public bool corrected;
        public Vector3 eulerAnglesCorrected;

        public StoreTransform Clone()
        {
            return new StoreTransform()
            {
                position = position,
                rotation = rotation,
                localScale = localScale,
                eulerAngles = eulerAngles
            };
        }
    }

    public static class TransformSerializationExtension
    {
        public static StoreTransform Copy(this Transform aTransform)
        {
            return new StoreTransform()
            {
                position = aTransform.position,
                rotation = aTransform.rotation,
                localScale = aTransform.localScale,
                eulerAngles = aTransform.rotation.eulerAngles
            };
        }
        public static void FromCopy(this Transform aTransform, StoreTransform newPos)
        {
            aTransform.position = newPos.position;
            aTransform.rotation = newPos.rotation;
            aTransform.localScale = newPos.localScale;
        }
    }
}
