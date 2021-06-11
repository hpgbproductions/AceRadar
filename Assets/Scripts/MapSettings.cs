namespace AceRadar
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;

    public class MapSettings : MonoBehaviour
    {
        // Map settings
        private float px = 0f;
        private float py = 0f;
        private float scale = 0.5f;
        private Color frcol;

        // ! Set the key combination in the Editor !
        [SerializeField] private KeyCode[] ZoomIn;
        [SerializeField] private KeyCode[] ZoomOut;

        [SerializeField] private RectTransform MapCanvasRect;
        [SerializeField] private RectTransform MapRootRect;
        [SerializeField] private Image MapFrameRenderer;

        // Save functionality
        private string AceRadarPath;
        private string SettingsPath;
        private string SettingsName = "ACERADAR.DAT";
        private string SettingsHeader = "NACHSAVEACERADAR";

        private string FileZoomIn = "KCZIN.TXT";
        private string FileZoomOut = "KCZOUT.TXT";

        private void Start()
        {
            // Register commands
            ServiceProvider.Instance.DevConsole.RegisterCommand<float, float>("AceRadar_Position", SetPosition);
            ServiceProvider.Instance.DevConsole.RegisterCommand<float>("AceRadar_Scale", SetScale);
            ServiceProvider.Instance.DevConsole.RegisterCommand<float, float, float>("AceRadar_FrameColor", SetFrameColor);

            frcol = MapFrameRenderer.color;

            // Load settings
            AceRadarPath = Path.Combine(Application.persistentDataPath, "NACHSAVE", "ACERADAR");
            Directory.CreateDirectory(AceRadarPath);

            SettingsPath = Path.Combine(AceRadarPath, SettingsName);
            if (File.Exists(SettingsPath))
            {
                Debug.Log("Loading settings file...\nPath: " + SettingsPath);
                try
                {
                    Stream instream = File.OpenRead(SettingsPath);
                    using (BinaryReader reader = new BinaryReader(instream))
                    {
                        string FileHeader = reader.ReadString();
                        if (FileHeader == SettingsHeader)
                        {
                            float rpx = reader.ReadSingle();
                            float rpy = reader.ReadSingle();
                            float rsc = reader.ReadSingle();
                            float rfc_r = reader.ReadSingle();
                            float rfc_g = reader.ReadSingle();
                            float rfc_b = reader.ReadSingle();

                            px = rpx;
                            py = rpy;
                            scale = rsc;
                            frcol.r = rfc_r;
                            frcol.g = rfc_g;
                            frcol.b = rfc_b;

                            Debug.Log("Successfully read settings file\nPath: " + SettingsPath);
                        }
                        else
                        {
                            Debug.LogError("Unrecognized header " + FileHeader + " in settings file. Initializing with default settings.\nPath: " + SettingsPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    Debug.LogError("Error in reading settings file. Initializing with default settings.\nPath: " + SettingsPath);
                }
            }
            else
            {
                Debug.LogWarning("Settings file does not exist. Initializing with default settings.\nPath: " + SettingsPath);
            }

            KeyCodeImportFormat importZin = ImportKeyCode(Path.Combine(AceRadarPath, FileZoomIn), KeyControls.ZoomIn);
            if (importZin.success)
                ZoomIn = importZin.keys;
            else
                ExportKeyCode(Path.Combine(AceRadarPath, FileZoomIn), ZoomIn);

            KeyCodeImportFormat importZout = ImportKeyCode(Path.Combine(AceRadarPath, FileZoomOut), KeyControls.ZoomOut);
            if (importZout.success)
                ZoomOut = importZout.keys;
            else
                ExportKeyCode(Path.Combine(AceRadarPath, FileZoomOut), ZoomOut);

            // Apply settings

            ApplyPosition(px, py);
            ApplyScale(scale);
            ApplyFrameColor(frcol);
        }

        private void OnApplicationQuit()
        {
            Stream outstream = File.Create(SettingsPath);
            using (BinaryWriter writer = new BinaryWriter(outstream))
            {
                writer.Write(SettingsHeader);

                writer.Write(px);
                writer.Write(py);
                writer.Write(scale);
                writer.Write(frcol.r);
                writer.Write(frcol.g);
                writer.Write(frcol.b);
            }
        }

        private void SetPosition(float x, float y)
        {
            px = x;
            py = y;
            ApplyPosition(px, py);
            Debug.Log(string.Format("AceRadar position set to {0:F0},{1:F0}", px, py));
        }

        private void ApplyPosition(float x, float y)
        {
            MapRootRect.anchoredPosition = new Vector2(
                x - MapCanvasRect.sizeDelta.x / 2,
                y - MapCanvasRect.sizeDelta.y / 2);
        }

        private void SetScale(float s)
        {
            scale = s;
            ApplyScale(scale);
            Debug.Log(string.Format("AceRadar scale set to {0:F3}", scale));
        }

        private void ApplyScale(float s)
        {
            MapRootRect.localScale = new Vector3(s, s, 1f);
        }

        private void SetFrameColor(float r, float g, float b)
        {
            frcol = new Color(r, g, b, frcol.a);
            ApplyFrameColor(frcol);
            Debug.Log(string.Format("AceRadar frame color set to {0:F3},{1:F3},{2:F3}", frcol.r, frcol.g, frcol.b));
        }

        private void ApplyFrameColor(Color c)
        {
            MapFrameRenderer.color = c;
        }

        public bool GetKeyControlDown(KeyControls control)
        {
            KeyCode[] keys;
            switch (control)
            {
                case KeyControls.ZoomIn:
                    keys = ZoomIn;
                    break;
                case KeyControls.ZoomOut:
                    keys = ZoomOut;
                    break;
                default:
                    Debug.LogError("Invalid or unimplemented KeyControls!");
                    return false;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                if (!Input.GetKeyDown(keys[i]) && keys[i] != KeyCode.None)
                {
                    return false;
                }
            }
            return true;
        }

        private KeyCodeImportFormat ImportKeyCode(string fp, KeyControls control)
        {
            if (File.Exists(fp))
            {
                try
                {
                    Stream s = File.OpenRead(fp);
                    using (StreamReader reader = new StreamReader(s))
                    {
                        List<KeyCode> keys = new List<KeyCode>();
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            keys.Add((KeyCode)Enum.Parse(typeof(KeyCode), line, true));
                        }
                        return new KeyCodeImportFormat(true, keys.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    Debug.LogError("Error loading KeyCode file for " + control.ToString() + ".");
                    return new KeyCodeImportFormat(false, new KeyCode[] { });
                }
            }
            else
            {
                Debug.LogWarning("KeyCode file for " + control.ToString() + " does not exist! Created file with default values.");
                return new KeyCodeImportFormat(false, new KeyCode[] { });
            }
        }

        private void ExportKeyCode(string fp, KeyCode[] keys)
        {
            if (keys.Length < 1)
            {
                Debug.LogError("Invalid KeyCode array!");
                return;
            }
            Stream s = File.Create(fp);
            using (StreamWriter writer = new StreamWriter(s))
            {
                foreach (KeyCode k in keys)
                {
                    writer.WriteLine(k.ToString());
                }
            }
        }

        

        public enum KeyControls { ZoomIn, ZoomOut }

        private struct KeyCodeImportFormat
        {
            public KeyCode[] keys;
            public bool success;

            public KeyCodeImportFormat(bool s, KeyCode[] k)
            {
                success = s;
                if (success)
                {
                    keys = k;
                }
                else
                {
                    keys = new KeyCode[] { };
                }
            }
        }
    }
}