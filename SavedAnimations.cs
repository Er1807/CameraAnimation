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
        public const float DefaultFieldOfView = 50.0f;
        private const string floatRegex = "([0-9-.]+);";
        private const string vector2Regex = floatRegex + floatRegex;
        private const string vector3Regex = vector2Regex + floatRegex;
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
                builder.AppendLine(GenerateStringFromVector(position.Position) + GenerateStringFromVector(position.EulerAngles) + position.FocalLength + ";");
            }
            return builder.ToString();
        }
        private string GenerateStringFromVector(Vector3 vector)
        {
            return $"{vector.x.ToString(CultureInfo.InvariantCulture)};{vector.y.ToString(CultureInfo.InvariantCulture)};{vector.z.ToString(CultureInfo.InvariantCulture)};";
        }


        private void LoadPositionsFromString(string text)
        {
            cameraAnimationMod.ClearAnimation();
            
            Regex lineRegex = new Regex(vector3Regex + vector3Regex + floatRegex + vector2Regex + vector2Regex);
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

                Quaternion rotation  = Quaternion.Euler(ParseVector3(match, 4));

                float focalLength = DefaultFieldOfView;

                if (float.TryParse(match.Groups[7].Value, out float parsed))
                {
                    focalLength = parsed;
                }

                Vector2 lensShift = ParseVector2(match, 7);

                Vector2 sensorSize = ParseVector2(match, 9);

                cameraAnimationMod.AddPosition(positions, rotation, focalLength, lensShift, sensorSize);
            }
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
