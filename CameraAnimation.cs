using ActionMenuApi.Api;
using CameraAnimation;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using TouchCamera;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRC;
using VRC.SDKBase;
using VRC.UserCamera;
using VRCSDK2;
using CameraButton = MonoBehaviourPublicObGaCaTMImReImRaReSpUnique;

[assembly: MelonInfo(typeof(CameraAnimationMod), "Camera Animations", "2.3.0", "Eric van Fandenfart")]
[assembly: MelonAdditionalDependencies("ActionMenuApi", "UIExpansionKit")]
[assembly: MelonOptionalDependencies("TouchCamera")]
[assembly: MelonGame]

namespace CameraAnimation
{

    public class CameraAnimationMod : MelonMod
    {
        private static AssetBundle iconsAssetBundle;
        static CameraAnimationMod()
        {
            try
            {
                //Adapted from knah's JoinNotifier mod found here: https://github.com/knah/VRCMods/blob/master/JoinNotifier/JoinNotifierMod.cs 
                using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CameraAnimation.icons-camera"))
                using (var tempStream = new MemoryStream((int)stream.Length))
                {
                    stream.CopyTo(tempStream);
                    iconsAssetBundle = AssetBundle.LoadFromMemory_Internal(tempStream.ToArray(), 0);
                    iconsAssetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Consider checking for newer version as mod possibly no longer working, Exception occured OnAppStart(): " + e.Message);
            }
        }

        public Texture2D LoadImage(string name)
        {
            return iconsAssetBundle.LoadAsset_Internal($"Assets/icons-camera/{name}.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
        }

        private GameObject originalCamera
        {
            get
            {
                UserCameraController controller = UserCameraController.field_Internal_Static_UserCameraController_0;
                if (controller == null) return null;
                foreach (var prop in typeof(UserCameraController).GetProperties().Where(x => x.Name.StartsWith("field_Public_GameObject_")))
                {
                    var obj = prop.GetValue(controller) as GameObject;
                    if (obj != null && obj.name == "PhotoCamera")
                        return obj;
                }

                return null;
            }
        }

        private GameObject originalVideoCamera
        {
            get
            {
                UserCameraController controller = UserCameraController.field_Internal_Static_UserCameraController_0;
                if (controller == null) return null;
                foreach (var prop in typeof(UserCameraController).GetProperties().Where(x => x.Name.StartsWith("field_Public_GameObject_")))
                {
                    var obj = prop.GetValue(controller) as GameObject;
                    if (obj != null && obj.name == "VideoCamera")
                        return obj;
                }

                return null;
            }
        }

        public readonly List<StoreTransform> positions = new List<StoreTransform>();
        private float speed = 0.5f;
        private float lineRendererWidth = 0.025f;
        private bool allowKeyCameraPickup = true;
        private bool loopMode = false;
        private bool syncCameraIcon = true;
        private bool constantSpeed = false;
        private bool showPath = true;
        private bool videoCameraWasActive = false;
        private LineRenderer lineRenderer;
        private Animation anim = null;
        private bool shouldBePlaying = false;



        private SavedAnimations savedAnimations = null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void OnApplicationStart()
        {
            if (MelonHandler.Mods.Any(x => x.Info.Name == "TouchCamera"))
            {
                AddTochCameraHook();
            }
            else
            {
                MelonCoroutines.Start(WaitForCamera());
            }

            savedAnimations = new SavedAnimations(this);
            var icon = LoadImage("play");
            icon.hideFlags |= HideFlags.DontUnloadUnusedAsset;


            var category = MelonPreferences.CreateCategory("Camera Animation");
            MelonPreferences_Entry<bool> showInModMenu = category.CreateEntry("UseModMenu", false, "Use the AM Mods Category");
            if (showInModMenu.Value)
                AMUtils.AddToModsFolder("Camera Animation", CreateActionMenu, icon);
            else
                VRCActionMenuPage.AddSubMenu(ActionMenuPage.Main, "Camera Animation", CreateActionMenu, icon);

            LoggerInstance.Msg("Actionmenu initialised");
        }

        private void CreateActionMenu()
        {
            if (originalCamera == null) return;

            if (lineRenderer == null)
            {
                lineRenderer = new GameObject("CameraAnimations") { layer = LayerMask.NameToLayer("UI") }.AddComponent<LineRenderer>();
                lineRenderer.positionCount = 0;
                lineRenderer.SetWidth(lineRendererWidth, lineRendererWidth);
                GameObject.DontDestroyOnLoad(lineRenderer);
            }

            CustomSubMenu.AddButton("Save Pos", AddCurrentPositionToList, LoadImage("save pos"));
            CustomSubMenu.AddButton("Delete last Pos", RemoveLastPosition, LoadImage("delete last pos"));
            CustomSubMenu.AddButton("Play Anim", PlayAnimation, LoadImage("play"));
            CustomSubMenu.AddButton("Stop Anim", StopAnimation, LoadImage("stop"));
            CustomSubMenu.AddButton("Update Linerenderer", UpdateLineRenderer, LoadImage("update linerenderer"));
            CustomSubMenu.AddButton("Clear Anim", ClearAnimation, LoadImage("clear anim"));
            CustomSubMenu.AddSubMenu("Settings",
                delegate
                {
                    CustomSubMenu.AddRadialPuppet("Speed", (x) => speed = x, speed, LoadImage("speed"));
                    CustomSubMenu.AddToggle("Loop mode", loopMode, (x) => { loopMode = x; UpdateLineRenderer(); }, LoadImage("loop mode"));
                    CustomSubMenu.AddToggle("Sync Camera\nIcon", syncCameraIcon, (x) =>
                    {
                        syncCameraIcon = x;
                        if (Player.prop_Player_0 != null)
                            Player.prop_Player_0.gameObject.GetComponentInChildren<UserCameraIndicator>().enabled = syncCameraIcon;
                    }, LoadImage("sync camera icon"));

                    CustomSubMenu.AddToggle("Constant\nSpeed", constantSpeed, (x) => { constantSpeed = x; }, LoadImage("constant speed"));
                    CustomSubMenu.AddToggle("Enable\nKey Pickup", allowKeyCameraPickup, (x) => { allowKeyCameraPickup = x; }, LoadImage("constant speed"));
                    CustomSubMenu.AddToggle("Show\nPath", showPath, (x) =>
                    {
                        showPath = x;
                        lineRenderer.enabled = showPath;
                    }, LoadImage("show path"));

                }, LoadImage("settings"));

            CustomSubMenu.AddSubMenu("Saved",
                delegate
                {
                    CustomSubMenu.AddButton("Save\nCurrent", () => savedAnimations.Save(), LoadImage("save current"));
                    CustomSubMenu.AddButton("Load from Clipboard", () => savedAnimations.CopyFromClipBoard(), LoadImage("load from clipboard"));
                    CustomSubMenu.AddButton("Copy to Clipboard", () => savedAnimations.CopyToClipBoard(), LoadImage("copy to clipboard"));
                    foreach (string availableSave in savedAnimations.AvailableSaves)
                    {
                        CustomSubMenu.AddSubMenu(availableSave,
                            delegate
                            {
                                CustomSubMenu.AddButton("Load", () => savedAnimations.Load(availableSave), LoadImage("load"));
                                CustomSubMenu.AddButton("Delete", () => savedAnimations.Delete(availableSave), LoadImage("delete"));
                                CustomSubMenu.AddButton("Copy to Clipboard", () => savedAnimations.CopyToClipBoard(availableSave), LoadImage("copy to clipboard"));

                            }, LoadImage("save current"));
                    }

                }, LoadImage("saved"));
        }

        private IEnumerator WaitForCamera()
        {
            while (UserCameraController.field_Internal_Static_UserCameraController_0 == null)
                yield return null;
            var cameraobj = UserCameraController.field_Internal_Static_UserCameraController_0.transform;

            while (cameraobj.Find("ViewFinder/PhotoControls/Primary /ControlGroup_Main/ControlGroup_Space/Scroll View/Viewport/Content/Attached/Icon")?.GetComponent<CanvasRenderer>()?.GetMaterial()?.shader == null)
                yield return null;

            TouchCameraMod_CameraReadyEvent();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddTochCameraHook()
        {
            TouchCameraMod.CameraReadyEvent += TouchCameraMod_CameraReadyEvent;
        }

        private void TouchCameraMod_CameraReadyEvent()
        {
            if (lineRenderer == null)
            {
                lineRenderer = new GameObject("CameraAnimations") { layer = LayerMask.NameToLayer("UI") }.AddComponent<LineRenderer>();
                lineRenderer.SetWidth(lineRendererWidth, lineRendererWidth);
                lineRenderer.positionCount = 0;
                GameObject.DontDestroyOnLoad(lineRenderer);
            }

            var cameraobj = UserCameraController.field_Internal_Static_UserCameraController_0.transform;
            var buttonToClone = cameraobj.Find("ViewFinder/PhotoControls/Primary /ControlGroup_Main/Scroll View/Viewport/Content/Space/");
            var copyMainButton = GameObject.Instantiate(buttonToClone, buttonToClone.parent);
            copyMainButton.transform.SetSiblingIndex(4);
            cameraobj.Find("ViewFinder/PhotoControls/Primary /ControlGroup_Main/Scroll View/Viewport/Content/Pins/").transform.SetSiblingIndex(7);
            var menuToClone = cameraobj.Find("ViewFinder/PhotoControls/Primary /ControlGroup_Main/ControlGroup_Pins/");
            var copyMenu = GameObject.Instantiate(menuToClone, menuToClone.parent);

            copyMainButton.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Camera Animations";

            copyMenu.transform.Find("Scroll View/Viewport/Content/0/Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Add Pos";
            copyMenu.transform.Find("Scroll View/Viewport/Content/1/Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Play Animation";
            copyMenu.transform.Find("Scroll View/Viewport/Content/2/Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Delete Last Pos";

            copyMenu.transform.Find("Scroll View/Viewport/Content/0").GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            copyMenu.transform.Find("Scroll View/Viewport/Content/1").GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            copyMenu.transform.Find("Scroll View/Viewport/Content/2").GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            copyMenu.transform.Find("Scroll View/Viewport/Content/0").GetComponent<Button>().onClick.AddListener(new Action(() => AddCurrentPositionToList()));
            copyMenu.transform.Find("Scroll View/Viewport/Content/1").GetComponent<Button>().onClick.AddListener(new Action(() => PlayAnimation()));
            copyMenu.transform.Find("Scroll View/Viewport/Content/2").GetComponent<Button>().onClick.AddListener(new Action(() => RemoveLastPosition()));

            var cameraButton = copyMainButton.GetComponent<CameraButton>();

            cameraButton.field_Public_CanvasGroup_0 = copyMenu.GetComponent<CanvasGroup>();
            cameraButton.field_Public_GameObject_0 = copyMenu.gameObject;

        }

        public void ClearAnimation()
        {
            positions.Clear();
            lineRenderer.positionCount = 0;
            for (int i = 0; i < lineRenderer.transform.childCount; i++)
            {
                GameObject.Destroy(lineRenderer.transform.GetChild(i).gameObject);
            }

            loopMode = false;
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
            var camera = originalCamera.GetComponent<Camera>();
            AddPosition(originalCamera.transform.position, 
                originalCamera.transform.rotation, 
                camera.focalLength,
                camera.lensShift,
                camera.sensorSize);
        }

        public void AddPosition(Vector3 position, Quaternion rotation, float focalLength, Vector2 lensShift, Vector2 sensorSize)
        {
            var oldValue = UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0;
            UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = UserCameraSpace.World;

            var photoCameraClone = GameObject.Instantiate(originalCamera, lineRenderer.transform);
            photoCameraClone.GetComponentInChildren<MeshRenderer>().gameObject.layer = LayerMask.NameToLayer("UI");
            
            var camera = photoCameraClone.GetComponent<Camera>();
            camera.enabled = false;
            camera.focalLength = focalLength;
            camera.lensShift = lensShift;
            camera.sensorSize = sensorSize;

            photoCameraClone.GetComponent<FlareLayer>().enabled = false;
            photoCameraClone.GetComponent<VRC.SDKBase.VRC_Pickup>().pickupable = allowKeyCameraPickup;
            photoCameraClone.GetComponentInChildren<MeshRenderer>().material = UserCameraController.field_Internal_Static_UserCameraController_0.field_Public_Material_3;
            GameObject.Destroy(photoCameraClone.transform.Find("VideoCamera").gameObject);
            photoCameraClone.transform.position = position;
            photoCameraClone.transform.rotation = rotation;
            positions.Add(new StoreTransform(photoCameraClone));

            UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = oldValue;
            UpdateLineRenderer();
        }

        public void UpdateLineRenderer()
        {
            if (positions.Count == 0) { lineRenderer.positionCount = 0; return; }

            ICameraCurve curve = CreateCurves();
            float lasttime = curve.Transform.Position.X.Last.time;
            int countPoints = positions.Count * 10;
            float fraction = lasttime / countPoints;
            lineRenderer.positionCount = countPoints + 1;
            for (int i = 0; i <= countPoints; i++)
            {
                float time = fraction * i;
                lineRenderer.SetPosition(i, curve.Transform.Position.Evaluate(time) );
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
            else if (shouldBePlaying)
            {
                if (!videoCameraWasActive)
                    originalVideoCamera.active = false;
                shouldBePlaying = false;
                UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = UserCameraSpace.Attached;
            }

        }

        public ICameraCurve CreateCameraCurve()
        {
            IVector3Curve Vector3Wrap(string prefix)
            {
                return new Vector3Curve()
                {
                    X = new CurveWrapper<Transform>(new AnimationCurve(), prefix + ".x"),
                    Y = new CurveWrapper<Transform>(new AnimationCurve(), prefix + ".y"),
                    Z = new CurveWrapper<Transform>(new AnimationCurve(), prefix + ".z")
                };
            }

            IVector2Curve Vector2Wrap(string prefix)
            {
                return new Vector2Curve() {
                    X = new CurveWrapper<Camera>(new AnimationCurve(), prefix + ".x"),
                    Y = new CurveWrapper<Camera>(new AnimationCurve(), prefix + ".y"),
                };
            }

            const string positionPrefix = nameof(Transform.localPosition);
            const string rotationPrefix = nameof(Transform.localEulerAngles);

            const string focalLengthKey = "m_FocalLength";
            const string lensShiftKey = "m_LensShift";
            const string sensorSizeKey = "m_SensorSize";

            return new CameraCurve()
            {
                Transform = new TransformCurve()
                {
                    Position = Vector3Wrap(positionPrefix),
                    Rotation = Vector3Wrap(rotationPrefix)
                },
                FocalLength = new CurveWrapper<Camera>(new AnimationCurve(), focalLengthKey),
                LensShift = Vector2Wrap(lensShiftKey),
                SensorSize = Vector2Wrap(sensorSizeKey),
            };
        }

        public AnimationClip CreateClip()
        {
            ICameraCurve curve = CreateCurves();
            
            AnimationClip clip = new AnimationClip();
            clip.legacy = true;

            curve.Set(clip);

            clip.EnsureQuaternionContinuity();

            return clip;
        }

        private ICameraCurve CreateCurves()
        {
            ICameraCurve curve = CreateCameraCurve();

            float time = 0;

            if (loopMode)
                positions.Add(new StoreTransform(positions[0].Photocamera));

            for (int i = 0; i < positions.Count; i++)
            {
                var transform = positions[i];
                var position = transform.Position;
                var rotation = transform.EulerAngles;

                curve.FocalLength.Add(time, transform.FocalLength);
                curve.LensShift.Add(time, transform.LensShift);
                curve.SensorSize.Add(time, transform.SensorSize);

                curve.Transform.Position.Add(time, position.x, position.y, position.z);

                float rotX = rotation.x;
                float rotY = rotation.y;
                float rotZ = rotation.z;

                if (i == 0)
                {
                    transform.EulerAnglesCorrected = new Vector3(rotX, rotY, rotZ);
                }
                else if (i != 0)
                {
                    var previousTransform = positions[i - 1];
                    var previousRotation = previousTransform.EulerAngles;

                    float diffX = rotX - previousRotation.x;
                    float diffY = rotY - previousRotation.y;
                    float diffZ = rotZ - previousRotation.z;

                    if (diffX > 180)
                        diffX -= 360;
                    if (diffY > 180)
                        diffY -= 360;
                    if (diffZ > 180)
                        diffZ -= 360;

                    if (diffX < -180)
                        diffX = 360 + diffX;
                    if (diffY < -180)
                        diffY = 360 + diffY;
                    if (diffZ < -180)
                        diffZ = 360 + diffZ;

                    //correct rotations to be negative if needed and writeback for next itteration
                    float lastRotXAdj = previousTransform.EulerAnglesCorrected.x;
                    float lastRotYAdj = previousTransform.EulerAnglesCorrected.y;
                    float lastRotZAdj = previousTransform.EulerAnglesCorrected.z;

                    rotX = lastRotXAdj + diffX;
                    rotY = lastRotYAdj + diffY;
                    rotZ = lastRotZAdj + diffZ;


                    transform.EulerAnglesCorrected = new Vector3(rotX, rotY, rotZ);
                } // 342 -> 60   360+60 = 420

                curve.Transform.Rotation.Add(time, transform.EulerAnglesCorrected);

                if (i < positions.Count - 1 && constantSpeed)
                {
                    var nextTransform = positions[i + 1];
                    float distance = Vector3.Distance(transform.Position, nextTransform.Position);
                    time += distance * Mathf.Lerp(0.2f, 5f, 1 - speed);
                }
                else
                {
                    time += Mathf.Lerp(0.5f, 5f, 1 - speed);
                }
            }

            if (loopMode)
                positions.RemoveAt(positions.Count - 1);

            return curve;
        }

        public void PlayAnimation()
        {
            if (!shouldBePlaying)
                videoCameraWasActive = originalVideoCamera.active;

            UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = UserCameraSpace.World;
            
            anim = originalCamera.GetComponent<Animation>();
            if (anim == null)
                anim = originalCamera.AddComponent<Animation>();

            var clip = CreateClip();
            anim.AddClip(clip, clip.name);

            // set the focal length to match the first frame
            // before we play the animation
            var camera = originalCamera.GetComponent<Camera>();

            camera.focalLength = positions[0].FocalLength;
            camera.sensorSize = positions[0].SensorSize;
            camera.lensShift = positions[0].LensShift;

            anim.Play(clip.name);

            if(MelonHandler.Mods.Any(x => x.Info.Name == "FreezeFrame"))
            {
                var cloneHolder = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(x => x.name == "Avatar Clone Holder");
                if (cloneHolder != null)
                    foreach (var item in cloneHolder.GetComponentsInChildren<Animation>())
                    {
                        item.Stop();
                    }
            }

            shouldBePlaying = true;
        }
        private void StopAnimation()
        {
            anim?.Stop();
        }
    }
}
