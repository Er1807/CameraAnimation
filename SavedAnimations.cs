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
                builder.AppendLine(GenerateStringFromVector(position.Position) + GenerateStringFromVector(position.EulerAngles));
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
            Regex lineRegex = new Regex("([0-9-.]+);([0-9-.]+);([0-9-.]+);([0-9-.]+);([0-9-.]+);([0-9-.]+);");
            foreach (var line in text.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                Match match = lineRegex.Match(line);
                if (!match.Success)
                {
                    cameraAnimationMod.LoggerInstance.Error($"Error while loading line {line}");
                    return;
                }
                Vector3 positions = new Vector3(float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                                                float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                                                float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));

                Quaternion rotation  = Quaternion.Euler(float.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
                                                float.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture),
                                                float.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture));

                cameraAnimationMod.AddPosition(positions, rotation);
            }
        }


        internal static string Clipboard
        {
            //https://flystone.tistory.com/138
            get
            {
                TextEditor _textEditor = new TextEditor();
                _textEditor.Paste();
                return _textEditor.text;
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
