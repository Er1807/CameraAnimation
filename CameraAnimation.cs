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
using CameraSettings = MonoBehaviourPublicAcInCaInTeShInMaBoInUnique;
using CameraSlider = MonoBehaviourPublicAc2ObSicaObdotySlObUnique;
using CameraSliderEnum = EnumPublicSealedvaNoDoZoDo6vDoUnique;
using CameraFocusMode = EnumPublicSealedvaOfFuSeMa5vUnique;
using Il2CppSystem.Reflection;

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

        private GameObject _originalCamera;

        private GameObject originalCamera
        {
            get
            {
                if (_originalCamera != null) return _originalCamera;
                UserCameraController controller = UserCameraController.field_Internal_Static_UserCameraController_0;
                if (controller == null) return null;
                foreach (var prop in typeof(UserCameraController).GetProperties().Where(x => x.Name.StartsWith("field_Public_GameObject_")))
                {
                    var obj = prop.GetValue(controller) as GameObject;
                    if (obj != null && obj.name == "PhotoCamera")
                    {
                        _originalCamera = obj;
                        break;
                    }
                }

                return _originalCamera;
            }
        }

        private GameObject _originalVideoCamera;
        private GameObject originalVideoCamera
        {
            get
            {
                if (_originalVideoCamera != null) return _originalVideoCamera;
                UserCameraController controller = UserCameraController.field_Internal_Static_UserCameraController_0;
                if (controller == null) return null;
                foreach (var prop in typeof(UserCameraController).GetProperties().Where(x => x.Name.StartsWith("field_Public_GameObject_")))
                {
                    var obj = prop.GetValue(controller) as GameObject;
                    if (obj != null && obj.name == "VideoCamera")
                    {
                        _originalVideoCamera = obj;
                        break;
                    }
                }

                return _originalVideoCamera;
            }
        }

        private CameraSettings _cameraSettings;
        private CameraSettings cameraSettings
        {
            get
            {
                if (_cameraSettings != null) return _cameraSettings;
                if (originalCamera == null) return null;
                _cameraSettings = originalCamera.GetComponent<CameraSettings>();
                return _cameraSettings;
            }
        }

        public readonly List<StoreTransform> positions = new List<StoreTransform>();
        private bool loopMode = false;
        private bool syncCameraIcon = true;
        private bool constantSpeed = false;
        private bool videoCameraWasActive = false;
        private LineRenderer lineRenderer;
        private Animation anim = null;
        private bool shouldBePlaying = false;
        
        private SavedAnimations savedAnimations = null;

        public static CameraAnimationMod Instance;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void OnApplicationStart()
        {
            Instance = this;
            if (MelonHandler.Mods.Any(x => x.Info.Name == "TouchCamera"))
            {
                AddTochCameraHook();
            }
            else
            {
                MelonCoroutines.Start(WaitForCamera());
            }
            MelonCoroutines.Start(RetrieveCamerasettingsParameter());

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

            // allow all instances of StoreTransform to toggle the camera pickup
            // without linking vrc in the StoreTransform file
            StoreTransform.SetCameraPickupable = SetPickupable;
        }

        private void CreateActionMenu()
        {
            if (originalCamera == null) return;

            if (lineRenderer == null)
            {
                lineRenderer = new GameObject("CameraAnimations") { layer = LayerMask.NameToLayer("UI") }.AddComponent<LineRenderer>();
                lineRenderer.positionCount = 0;
                lineRenderer.SetWidth(Settings.UI.PathWidth, Settings.UI.PathWidth);
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
                    CustomSubMenu.AddRadialPuppet("Speed", (x) => Settings.Animation.Speed = x, Settings.Animation.Speed, LoadImage("speed"));
                    CustomSubMenu.AddToggle("Loop mode", loopMode, (x) => { loopMode = x; UpdateLineRenderer(); }, LoadImage("loop mode"));
                    CustomSubMenu.AddToggle("Sync Camera\nIcon", syncCameraIcon, (x) =>
                    {
                        syncCameraIcon = x;
                        if (Player.prop_Player_0 != null)
                            Player.prop_Player_0.gameObject.GetComponentInChildren<UserCameraIndicator>().enabled = syncCameraIcon;
                    }, LoadImage("sync camera icon"));

                    CustomSubMenu.AddToggle("Constant\nSpeed", constantSpeed, (x) => { constantSpeed = x; }, LoadImage("constant speed"));
                    CustomSubMenu.AddToggle("Enable\nKey Pickup", Settings.Interaction.AllowCameraPickup, (x) => { Settings.Interaction.AllowCameraPickup = x; UpdatePickupable(); }, LoadImage("constant speed"));
                    CustomSubMenu.AddToggle("Show\nPath", Settings.UI.ShowPath, (x) =>
                    {
                        Settings.UI.ShowPath = x;
                        lineRenderer.enabled = Settings.UI.ShowPath;
                    }, LoadImage("show path"));

                }, LoadImage("settings"));

            CustomSubMenu.AddSubMenu("Keying",
                delegate
                {
                    CustomSubMenu.AddToggle("Position", Settings.Keying.KeyPosition, (x) => { Settings.Keying.KeyPosition = x; }, LoadImage("save pos"));
                    CustomSubMenu.AddToggle("Rotation", Settings.Keying.KeyRotation, (x) => { Settings.Keying.KeyRotation = x;  }, LoadImage("save pos"));
                    CustomSubMenu.AddToggle("Zoom", Settings.Keying.KeyZoom, (x) => { Settings.Keying.KeyZoom = x; }, LoadImage("save pos"));
                    CustomSubMenu.AddToggle("Focus", Settings.Keying.KeyFocus, (x) => { Settings.Keying.KeyFocus = x; }, LoadImage("save pos"));
                    CustomSubMenu.AddRadialPuppet("Max Key Length", (x) => Settings.Keying.MaxTimeBetweenKeyFrames = x * (Settings.Keying.DefaultMaxTimeBetweenKeyFrames * 10 * 2), Settings.Keying.MaxTimeBetweenKeyFrames / 20, LoadImage("speed"));
                    CustomSubMenu.AddRadialPuppet("Min Key Length", (x) => Settings.Keying.MinTimeBetweenKeyFrames = x * (Settings.Keying.DefaultMinTimeBetweenKeyFrames * 10 * 2), Settings.Keying.MinTimeBetweenKeyFrames / 20, LoadImage("speed"));
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
                lineRenderer.SetWidth(Settings.UI.PathWidth, Settings.UI.PathWidth);
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
            var newTransform = new StoreTransform(originalCamera);

            AddPosition(newTransform);
        }

        public void AddPosition(StoreTransform position)
        {
            var oldValue = UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0;
            UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = UserCameraSpace.World;

            var photoCameraClone = GameObject.Instantiate(originalCamera, lineRenderer.transform);
            photoCameraClone.GetComponentInChildren<MeshRenderer>().gameObject.layer = LayerMask.NameToLayer("UI");
            
            var camera = photoCameraClone.GetComponent<Camera>();
            camera.enabled = false;


            photoCameraClone.GetComponent<FlareLayer>().enabled = false;
            photoCameraClone.GetComponentInChildren<MeshRenderer>().material = UserCameraController.field_Internal_Static_UserCameraController_0.field_Public_Material_3;
            GameObject.Destroy(photoCameraClone.transform.Find("VideoCamera").gameObject);
            photoCameraClone.transform.position = position.Position;
            photoCameraClone.transform.rotation = position.Rotation.ToQuaternion();

            position.Photocamera = photoCameraClone;

            positions.Add(position);


            UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = oldValue;
            UpdateLineRenderer();
        }

        /// <summary>
        /// Sets the pickupable status of the provided gameobject
        /// </summary>
        void SetPickupable(bool value, GameObject obj)
        {
            if (obj)
            {
                var pickup = obj.GetComponent<VRC.SDKBase.VRC_Pickup>();

                if (pickup)
                {
                    pickup.pickupable = value;
                }
            }
            
        }

        /// <summary>
        /// updates all StoreTransforms within positions to use the current allowKeyCameraPickup value;
        /// </summary>
        void UpdatePickupable()
        {
            foreach (var transform in positions)
            {
                transform.Pickupable = Settings.Interaction.AllowCameraPickup;
            }
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
            IVector4Curve Vector4Wrap(string prefix)
            {
                return new Vector4Curve()
                {
                    X = new CurveWrapper<Transform>(new AnimationCurve(), prefix + ".x"),
                    Y = new CurveWrapper<Transform>(new AnimationCurve(), prefix + ".y"),
                    Z = new CurveWrapper<Transform>(new AnimationCurve(), prefix + ".z"),
                    W = new CurveWrapper<Transform>(new AnimationCurve(), prefix + ".w")
                };
            }

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

            ICurve CameraSettingsCurve(string key)
            {
                return new CurveWrapper<CameraSettings>(new AnimationCurve(), key);
            }

            const string positionPrefix = nameof(Transform.localPosition);
            const string rotationPrefix = nameof(Transform.localRotation);


            string zoomKey = ZoomProps[0]?.Name ?? string.Empty;
            string apertureKey = ApertureProps[0]?.Name ?? string.Empty;
            string alternateApertureKey = ApertureProps[1]?.Name ?? string.Empty;
            string focalDistanceKey = FocalDistanceProps[0]?.Name ?? string.Empty;
            string alternateFocalDistanceKey = FocalDistanceProps[1]?.Name ?? string.Empty;

            return new CameraCurve()
            {
                Transform = new TransformCurve()
                {
                    Position = Vector3Wrap(positionPrefix),
                    Rotation = Vector4Wrap(rotationPrefix)
                },
                Apature = CameraSettingsCurve(apertureKey),
                ApatureAlternate = CameraSettingsCurve(alternateApertureKey),
                FocalDistance = CameraSettingsCurve(focalDistanceKey),
                FocalDistanceAlternate = CameraSettingsCurve(alternateFocalDistanceKey),
                Zoom = new CurveWrapper<CameraSettings>(new AnimationCurve(), zoomKey),
            };
        }

        public AnimationClip CreateClip()
        {
            currentCurve = CreateCurves();
            
            AnimationClip clip = new AnimationClip();
            clip.legacy = true;
            clip.name = "CameraAnimation";
            currentCurve.Set(clip);

            clip.EnsureQuaternionContinuity();

            return clip;
        }

        private ICameraCurve CreateCurves()
        {
            ICameraCurve curve = CreateCameraCurve();

            float time = 0;

            if (loopMode)
            {
                positions.Add(new StoreTransform(positions[0].Photocamera));
            }

            for (int i = 0; i < positions.Count; i++)
            {
                var transform = positions[i];

                if (transform.KeyZoom)
                {
                    curve.Zoom.Add(time, transform.Zoom);
                }

                if (transform.KeyFocus)
                {
                    curve.Apature.Add(time, transform.Aperture);
                    curve.ApatureAlternate.Add(time, transform.Aperture);
                    curve.FocalDistance.Add(time, transform.FocalDistance);
                    curve.FocalDistanceAlternate.Add(time, transform.FocalDistance);
                }

                if (transform.KeyPosition)
                {
                    curve.Transform.Position.Add(time, transform.Position);
                }

                if (transform.KeyRotation)
                {
                    curve.Transform.Rotation.Add(time, transform.Rotation);

                }

                if (i < positions.Count - 1 && constantSpeed)
                {
                    var nextTransform = positions[i + 1];
                    float distance = Vector3.Distance(transform.Position, nextTransform.Position);
                    time += distance * Mathf.Lerp(Settings.Keying.MinTimeBetweenKeyFrames, Settings.Keying.MaxTimeBetweenKeyFrames, 1 - Settings.Animation.Speed);
                }
                else
                {
                    time += Mathf.Lerp(Settings.Keying.MinTimeBetweenKeyFrames, Settings.Keying.MaxTimeBetweenKeyFrames, 1 - Settings.Animation.Speed);
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

            if (positions.Count == 0) return;

            UserCameraController.field_Internal_Static_UserCameraController_0.prop_UserCameraSpace_0 = UserCameraSpace.World;
            
            anim = originalCamera.GetComponent<Animation>();
            if (anim == null)
                anim = originalCamera.AddComponent<Animation>();

            var clip = CreateClip();
            anim.AddClip(clip, clip.name);

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


        public static List<FieldInfo> FocalDistanceProps = new List<FieldInfo>();
        public static List<FieldInfo> ApertureProps = new List<FieldInfo>();
        public static List<FieldInfo> ZoomProps = new List<FieldInfo>();

        private ICameraCurve currentCurve;

        public IEnumerator RetrieveCamerasettingsParameter() {
            while (originalCamera == null) yield return null;
            while (!originalCamera.gameObject.active) yield return null;

            LoggerInstance.Msg("Applying detection values");

            CameraSlider.field_Private_Static_Action_2_EnumPublicSealedvaNoDoZoDo6vDoUnique_Single_0.Invoke(CameraSliderEnum.DofFocalDistance, 5.124f);
            CameraSlider.field_Private_Static_Action_2_EnumPublicSealedvaNoDoZoDo6vDoUnique_Single_0.Invoke(CameraSliderEnum.DofAperature, 6.124f);
            CameraSlider.field_Private_Static_Action_2_EnumPublicSealedvaNoDoZoDo6vDoUnique_Single_0.Invoke(CameraSliderEnum.Zoom, 47.154f);
            cameraSettings.enabled = true;
            cameraSettings.field_Public_EnumPublicSealedvaOfFuSeMa5vUnique_0 = CameraFocusMode.Manual;

            yield return new WaitForSeconds(1);

            LoggerInstance.Msg("checking values");

            foreach (var field in Il2CppType.Of<CameraSettings>().GetFields().Where(x => x.FieldType.Name == "Single"))
            {
                //LoggerInstance.Msg("checking field " + field.Name +"With value " + field.GetValue(cameraSettings).Unbox<float>());
                if (field.GetValue(cameraSettings).Unbox<float>() == 5.124f)
                {
                    FocalDistanceProps.Add(field);
                    LoggerInstance.Msg("Found DofFocalDistance under " + field.Name);
                }

                if (field.GetValue(cameraSettings).Unbox<float>() == 6.124f)
                {
                    ApertureProps.Add(field);
                    LoggerInstance.Msg("Found DofAperature under " + field.Name);
                }

                if (field.GetValue(cameraSettings).Unbox<float>() == 47.154f)
                {
                    ZoomProps.Add(field);
                    LoggerInstance.Msg("Found Zoom under " + field.Name);
                }
            }

            LoggerInstance.Msg("Restoring values");

            CameraSlider.field_Private_Static_Action_2_EnumPublicSealedvaNoDoZoDo6vDoUnique_Single_0.Invoke(CameraSliderEnum.DofFocalDistance, 1.5f);
            CameraSlider.field_Private_Static_Action_2_EnumPublicSealedvaNoDoZoDo6vDoUnique_Single_0.Invoke(CameraSliderEnum.DofAperature, 15f);
            CameraSlider.field_Private_Static_Action_2_EnumPublicSealedvaNoDoZoDo6vDoUnique_Single_0.Invoke(CameraSliderEnum.Zoom, 45f);
            cameraSettings.field_Public_EnumPublicSealedvaOfFuSeMa5vUnique_0 = CameraFocusMode.FullAuto;
            cameraSettings.enabled = false;

            if (FocalDistanceProps.Count != 2)
                LoggerInstance.Error("Didnt find 2 DofFocalDistance attributes, found " + FocalDistanceProps.Count);
            if (ApertureProps.Count != 2)
                LoggerInstance.Error("Didnt find 2 DofAperature attributes, found " + ApertureProps.Count);
            if (ZoomProps.Count != 1)
                LoggerInstance.Error("Didnt find 1 Zoom attributes, found " + ApertureProps.Count);



        }
    }
}
