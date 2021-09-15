using ActionMenuApi.Api;
using CameraAnimation;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;
using VRChatUtilityKit.Utilities;

[assembly: MelonInfo(typeof(CameraAnimationMod), "CameraAnimation", "1.0.0", "Eric van Fandenfart")]
[assembly: MelonGame]

namespace CameraAnimation
{

    public class CameraAnimationMod : MelonMod
    {
        private GameObject PhotoCamera;
        private GameObject CloneVideoCamera;
        private readonly List<StoreTransform> positions = new List<StoreTransform>();
        private int currentPosition = 0;
        private float percent = 0;
        private float speed = 0.5f;
        private float smoothingFactor = 0.2f;
        private bool animationActive = false;
        private LineRenderer lineRenderer;
        private GameObject Lens;

        public override void OnApplicationStart()
        {
            VRCActionMenuPage.AddSubMenu(ActionMenuPage.Main,
                "Camera Animation",
                delegate {
                    MelonLogger.Msg("Camera Animation Menu Opened");
                    CustomSubMenu.AddButton("Save Pos", () => AddCurrentPositionToList());
                    CustomSubMenu.AddButton("Play Anim", () => animationActive = true);
                    CustomSubMenu.AddButton("Clear Anim", () => 
                    { 
                        positions.Clear(); 
                        lineRenderer.positionCount = 0;
                        for (int i = 0; i < lineRenderer.transform.childCount; i++)
                        {
                            GameObject.Destroy(lineRenderer.transform.GetChild(i).gameObject);
                        }
                        
                    });
                    CustomSubMenu.AddRadialPuppet("Speed", (x) => speed = x, 0.5f);
                    CustomSubMenu.AddRadialPuppet("Smoothing", (x) => smoothingFactor = Mathf.Lerp(0.01f, 0.49f, x), 0.4f);
                }
            );
            VRCUtils.OnUiManagerInit += Init;
        }
        

        private void Init()
        {
            FindCamera();

            MelonLogger.Msg("Buttons sucessfully created");
        }

        private void FindCamera()
        {
            PhotoCamera = GameObject.Find("_Application/TrackingVolume/PlayerObjects/UserCamera/PhotoCamera");
            Lens = GameObject.Find("_Application/TrackingVolume/PlayerObjects/UserCamera/PhotoCamera/camera_lens_mesh");
            GameObject vidCam = GameObject.Find("_Application/TrackingVolume/PlayerObjects/UserCamera/PhotoCamera/VideoCamera");
            if (vidCam != null)
                CloneVideoCamera = GameObject.Instantiate(vidCam);

            lineRenderer = new GameObject("CameraPath") { layer = LayerMask.NameToLayer("UI") }.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.05f;
            lineRenderer.positionCount = 0;
        }
        public void AddCurrentPositionToList()
        {
            if (PhotoCamera == null) FindCamera();
            positions.Add(PhotoCamera.transform.Copy());
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPosition(positions.Count - 1, positions[positions.Count - 1].position);
            GameObject tempModel = GameObject.Instantiate(Lens, lineRenderer.gameObject.transform, true);
            tempModel.layer = LayerMask.NameToLayer("UI");

        }

        
        public override void OnLateUpdate()
        {
            if (CloneVideoCamera == null) FindCamera();

            if (CloneVideoCamera == null) return;

            if (animationActive)
            {
                CloneVideoCamera.active = true;
                PlayAnimation();
            }
            else
            {
                CloneVideoCamera.active = false;
            }
                
        }
        public float GetTransformedPercent()
        {

            return percent;
        }

        public void PlayAnimation()
        {
            percent += Time.deltaTime * Mathf.Lerp(0.1f, 1.9f, speed);
            if (percent >= 1)
            {
                percent -= 1;
                currentPosition++;
            }

            float tranformedPercent = GetTransformedPercent();
            if (currentPosition>= positions.Count - 1)
            {
                CloneVideoCamera.transform.FromCopy(positions[positions.Count - 1]);
                animationActive = false;
                currentPosition = 0; 
                percent = 0;
            }
            else
            {
                if(tranformedPercent > (1 - smoothingFactor) && currentPosition + 2 <= positions.Count - 1)
                {
                    Vector3 pos1 = Vector3.Lerp(positions[currentPosition].position, positions[currentPosition + 1].position, 1 - smoothingFactor);
                    Vector3 pos2 = Vector3.Lerp(positions[currentPosition+1].position, positions[currentPosition + 2].position, smoothingFactor);

                    Quaternion rot1 = Quaternion.Lerp(positions[currentPosition].rotation, positions[currentPosition + 1].rotation, 1 - smoothingFactor);
                    Quaternion rot2 = Quaternion.Lerp(positions[currentPosition +1].rotation, positions[currentPosition + 2].rotation, smoothingFactor);


                    CloneVideoCamera.transform.position = Vector3.Lerp(pos1, pos2, (tranformedPercent - (1 - smoothingFactor)) * (1 / smoothingFactor / 2));
                    CloneVideoCamera.transform.rotation = Quaternion.Lerp(rot1, rot2, (tranformedPercent - (1-smoothingFactor)) * (1 / smoothingFactor / 2));
                }else if(tranformedPercent < smoothingFactor && currentPosition !=0){
                    Vector3 pos1 = Vector3.Lerp(positions[currentPosition-1].position, positions[currentPosition].position, 1 - smoothingFactor);
                    Vector3 pos2 = Vector3.Lerp(positions[currentPosition].position, positions[currentPosition + 1].position, smoothingFactor);

                    Quaternion rot1 = Quaternion.Lerp(positions[currentPosition-1].rotation, positions[currentPosition ].rotation, 1 - smoothingFactor);
                    Quaternion rot2 = Quaternion.Lerp(positions[currentPosition].rotation, positions[currentPosition + 1].rotation, smoothingFactor);


                    CloneVideoCamera.transform.position = Vector3.Lerp(pos1, pos2, 0.5f + tranformedPercent * (1 / smoothingFactor / 2));
                    CloneVideoCamera.transform.rotation = Quaternion.Lerp(rot1, rot2, 0.5f + tranformedPercent * (1 / smoothingFactor / 2));
                }
                else
                {
                    CloneVideoCamera.transform.position = Vector3.Lerp(positions[currentPosition].position, positions[currentPosition + 1].position, tranformedPercent);
                    CloneVideoCamera.transform.rotation = Quaternion.Lerp(positions[currentPosition].rotation, positions[currentPosition + 1].rotation, tranformedPercent);
                }
            }
        }
    
    }

    public class StoreTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    public static class TransformSerializationExtension
    {
        public static StoreTransform Copy(this Transform aTransform)
        {
            return new StoreTransform()
            {
                position = aTransform.position,
                rotation = aTransform.rotation,
                localScale = aTransform.localScale
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
