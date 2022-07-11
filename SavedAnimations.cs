using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnhollowerRuntimeLib;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using ActionMenuApi.Api;
using UIExpansionKit.API;
using System.Reflection;

namespace CameraAnimation
{
    public class SavedAnimations
    {
        private const string floatRegex = "([0-9-.]+);";
        private const string boolRegex = "\\D{4,5};";
        private const string vector2Regex = floatRegex + floatRegex;
        private const string vector3Regex = vector2Regex + floatRegex;
        private const string vector4Regex = vector3Regex + floatRegex;
        private CameraAnimationMod cameraAnimationMod;
        private MethodInfo UseKeyboardOnlyForText;
        public SavedAnimations(CameraAnimationMod cameraAnimationMod)
        {
            this.cameraAnimationMod = cameraAnimationMod;
            Directory.CreateDirectory("UserData/CameraAnimations");
            UseKeyboardOnlyForText = typeof(VRCInputManager).GetMethods().First(mi => mi.Name.StartsWith("Method_Public_Static_Void_Boolean_0") && mi.GetParameters().Count() == 1);

        }


        public string[] AvailableSaves => Directory.GetFiles("UserData/CameraAnimations").Select(Path.GetFileNameWithoutExtension).ToArray();


        public void Save()
        {
            BuiltinUiUtils.ShowInputPopup("Save Name", "", InputField.InputType.Standard, false, "Save", (message, _, _2) =>
            {
                UseKeyboardOnlyForText.Invoke(null, new object[] { false });
                File.WriteAllText(GetSavePath(message), GenerateStringFromPositions(cameraAnimationMod.positions));
                AMUtils.RefreshActionMenu();
            }, () => { UseKeyboardOnlyForText.Invoke(null, new object[] { false }); });
        }

        public void Load(string saveName)
        {
            LoadPositionsFromString(File.ReadAllText(GetSavePath(saveName)));
        }


        public void CopyToClipBoard()
        {
            Clipboard = GenerateStringFromPositions(cameraAnimationMod.positions);
        }
        public void CopyToClipBoard(string saveName)
        {
            Clipboard = File.ReadAllText(GetSavePath(saveName));
        }
        public void CopyFromClipBoard()
        {
            LoadPositionsFromString(Clipboard);
        }

        public void Delete(string availableSave)
        {
            File.Delete(GetSavePath(availableSave));
            AMUtils.ResetMenu();
        }

        private string GetSavePath(string availableSave)
        {
            return $"UserData/CameraAnimations/{availableSave}.save";
        }

        private string GenerateStringFromPositions(List<StoreTransform> positions)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var position in positions)
            {
                position.Serialize(builder);
                builder.Append(Environment.NewLine);
            }
            return builder.ToString();
        }

        private void LoadPositionsFromString(string text)
        {
            cameraAnimationMod.ClearAnimation();
            
            Regex lineRegex = new Regex(vector3Regex + vector4Regex + floatRegex + vector2Regex + vector2Regex + boolRegex + boolRegex + boolRegex);
            foreach (var line in text.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                Match match = lineRegex.Match(line);
                if (!match.Success)
                {
                    cameraAnimationMod.LoggerInstance.Error($"Error while loading line {line}");
                    return;
                }
                Vector3 positions = ParseVector3(match, 1);
                Vector4 vectorRot = ParseVector4(match, 4);
                Quaternion rotation  = new Quaternion(vectorRot.x, vectorRot.y, vectorRot.z, vectorRot.w);

                float focalLength = Settings.Camera.DefaultFieldOfView;

                if (float.TryParse(match.Groups[8].Value, out float parsedFocalLength))
                {
                    focalLength = parsedFocalLength;
                }

                Vector2 lensShift = ParseVector2(match, 8);

                Vector2 sensorSize = ParseVector2(match, 10);


                float aperature = Settings.Camera.DefaultAperture;
                float focalDistance = Settings.Camera.DefaultFocalDistance;

                if (float.TryParse(match.Groups[11].Value, out float parsedApeture))
                {
                    aperature = parsedApeture;
                }

                if (float.TryParse(match.Groups[12].Value, out float parsedFocalDistance))
                {
                    focalDistance = parsedFocalDistance;
                }

                bool keyPosition = ParseBool(match, 13);
                bool keyRotation = ParseBool(match, 14);
                bool keyZoom = ParseBool(match, 15);
                bool keyFocus = ParseBool(match, 16);

                var newTransform = new StoreTransform(aperature, focalDistance, focalLength, lensShift, sensorSize, positions, rotation) { 
                    KeyPosition = keyPosition,
                    KeyRotation = keyRotation,
                    KeyZoom = keyZoom,
                    KeyFocus = keyFocus
                };

                cameraAnimationMod.AddPosition(newTransform);
            }
        }

        bool ParseBool(Match match, int offset)
        {
            if (bool.TryParse(match.Groups[offset].Value, out bool x))
            {
                return x;
            }
            return false;
        }

        Vector4 ParseVector4(Match match, int offset)
        {
            Vector4 result = new Vector4();

            if (float.TryParse(match.Groups[offset].Value, out float x))
            {
                result.x = x;
            }
            if (float.TryParse(match.Groups[offset + 1].Value, out float y))
            {
                result.y = y;
            }
            if (float.TryParse(match.Groups[offset + 2].Value, out float z))
            {
                result.z = z;
            }
            if (float.TryParse(match.Groups[offset + 3].Value, out float w))
            {
                result.w = w;
            }

            return result;
        }

        Vector3 ParseVector3(Match match, int offset)
        {
            Vector3 result = new Vector3();

            if(float.TryParse(match.Groups[offset].Value, out float x))
            {
                result.x = x;
            }
            if (float.TryParse(match.Groups[offset+1].Value, out float y))
            {
                result.y = y;
            }
            if (float.TryParse(match.Groups[offset+2].Value, out float z))
            {
                result.z = z;
            }

            return result;
        }

        Vector2 ParseVector2(Match match, int offset)
        {
            Vector2 result = new Vector2();

            if (float.TryParse(match.Groups[offset].Value, out float x))
            {
                result.x = x;
            }
            if (float.TryParse(match.Groups[offset+1].Value, out float y))
            {
                result.y = y;
            }

            return result;
        }

        internal static string Clipboard
        {
            //https://flystone.tistory.com/138
            get
            {
                return GUIUtility.systemCopyBuffer;
            }
            set
            {
                TextEditor _textEditor = new TextEditor
                { text = value };

                _textEditor.OnFocus();
                _textEditor.Copy();
            }
        }
    }
}
