namespace AceRadar
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.UI;

    public class MapController : MonoBehaviour
    {
        [SerializeField] private MapSettings MapSettings;

        [SerializeField] private Canvas MapCanvas;
        [SerializeField] private GameObject MapTargetsParent;
        [SerializeField] private GameObject TargetBlipPrefab;
        [SerializeField] private Transform PlayerAircraftTransform;
        [SerializeField] private RectTransform MapRingsTransform;

        [SerializeField] private Sprite Air_s;
        [SerializeField] private Sprite Air_l;
        [SerializeField] private Sprite Gnd_s;
        [SerializeField] private Sprite Gnd_l;
        [SerializeField] private Sprite SLine;

        // Targets
        public List<RadarTarget> RadarTargets;
        public List<GameObject> RadarBlipObjects;
        public List<Image> RadarBlipComponents;

        private string[] SupportedGroundTargetTypes = new string[]
        {
            "RotatingMissileLauncherScript",
            "AntiAircraftTankScript",
            "SimpleGroundVehicleScript",
            "SinkableShipScript",
            "BombTargetScript",
            "FracturedObject",
            "RingScript"
        };

        private string[] SupportedAirTargetTypes = new string[]
        {
            "AiControlledAircraftScript"
        };

        private string[] SupportedWeaponTargetTypes = new string[]
        {
            "AntiAircraftMissileScript",
            "SamScript",
            "MissileScript",
            "BombScript",
            "RocketScript"
        };

        private string[] SupportedTargetTypes;

        public Color DefaultBlipColor;

        // Zoom settings
        public float MapRadius = 16000f;
        private int TargetMapSizeIndex = 1;
        private float[] MapSizes = new float[] { 8000f, 16000f, 32000f };
        private float[] MapScaleRates = new float[] { 80000f, 160000f };
        private float MapScaleRate = 0f;
        private float ResizeCooldownRequired = 0.1f;
        private float ResizeCooldownTimer = 0f;

        // Timing to check for new items
        public int IntervalCheckNewItems = 20;
        private int NextCheckNewItems = 1;

        // Auto-hide map outside of sandbox
        private bool InSandboxCurrent = false;
        private bool InSandboxPrevious = true;
        private bool InLevel = false;
        private bool InDesigner = false;

        private void Start()
        {
            RadarTargets = new List<RadarTarget>(16);
            RadarBlipObjects = new List<GameObject>(16);
            RadarBlipComponents = new List<Image>(16);

            SupportedTargetTypes = new string[SupportedGroundTargetTypes.Length + SupportedAirTargetTypes.Length + SupportedWeaponTargetTypes.Length];
            Array.Copy(SupportedGroundTargetTypes, SupportedTargetTypes, SupportedGroundTargetTypes.Length);
            Array.Copy(SupportedAirTargetTypes, 0, SupportedTargetTypes, SupportedGroundTargetTypes.Length, SupportedAirTargetTypes.Length);
            Array.Copy(SupportedWeaponTargetTypes, 0, SupportedTargetTypes, SupportedGroundTargetTypes.Length + SupportedAirTargetTypes.Length, SupportedWeaponTargetTypes.Length);

            DefaultBlipColor = GetAceRadarColor(AceRadarColors.White);
        }

        private void Update()
        {
            InLevel = ServiceProvider.Instance.GameState.IsInLevel;
            InDesigner = ServiceProvider.Instance.GameState.IsInDesigner;
            InSandboxCurrent = InLevel && !InDesigner;

            // Entered the sandbox
            if (InSandboxCurrent && !InSandboxPrevious)
            {
                MapCanvas.enabled = true;
            }
            // Exited the sandbox
            else if (!InSandboxCurrent && InSandboxPrevious)
            {
                MapCanvas.enabled = false;

                // Clear item list
                Debug.Log(string.Format("Cleared {0} RadarTargets", RadarTargets.Count));
                ClearAllListItems();
            }
            InSandboxPrevious = InSandboxCurrent;

            if (InSandboxCurrent)
            {
                if (NextCheckNewItems <= 0)
                {
                    CheckNewItems();
                    NextCheckNewItems = IntervalCheckNewItems;
                }
                else
                {
                    NextCheckNewItems--;
                }

                SmoothResizeMap();

                // Update positions of radar blips
                // (Draws all targets that are not destroyed)
                PlayerAircraftTransform.position = ServiceProvider.Instance.PlayerAircraft.MainCockpitPosition;
                PlayerAircraftTransform.eulerAngles = new Vector3(0f, ServiceProvider.Instance.PlayerAircraft.MainCockpitRotation.y, 0f);
                foreach(RadarTarget target in RadarTargets.ToArray())
                {
                    if (target.CheckDeleted())
                    {
                        RemoveTargetItem(target);
                    }
                    else if (target.CheckShouldHideBlip())
                    {
                        target.blipObject.SetActive(false);
                    }
                    else
                    {
                        target.blipObject.SetActive(true);
                        Vector3 targetPos = target.GetPosition();
                        Vector3 targetRelPos = PlayerAircraftTransform.InverseTransformVector(targetPos - PlayerAircraftTransform.position);
                        target.blipObject.transform.localPosition = new Vector3(
                            targetRelPos.x * 256f / MapRadius,
                            targetRelPos.z * 256f / MapRadius,
                            0f);

                        // Rotation for supported sprites
                        if (target.blipRotatable)
                        {
                            float targetRot = target.GetEulerAngles().y;
                            target.blipObject.transform.localEulerAngles = new Vector3(
                                0f,
                                0f,
                                PlayerAircraftTransform.eulerAngles.y - targetRot);
                        }
                    }
                }
            }
        }

        // Registers a single target.
        // May be manually called to register non-supported component types.
        public void AddTargetItem(Component c, Sprite s, Color sc, bool rot = false)
        {
            GameObject newRadarBlip = Instantiate(TargetBlipPrefab, MapTargetsParent.transform);
            RadarBlipObjects.Add(newRadarBlip);
            Image newRadarBlipComponent = newRadarBlip.GetComponent<Image>();
            newRadarBlipComponent.sprite = s;
            newRadarBlipComponent.color = sc;
            RadarBlipComponents.Add(newRadarBlipComponent);

            RadarTarget newRadarTarget = new RadarTarget(c, newRadarBlip, newRadarBlipComponent, rot);
            RadarTargets.Add(newRadarTarget);

            Debug.Log(string.Format("Registered new RadarTarget: {0} ({1})", newRadarTarget.gameObject.name, newRadarTarget.gameScriptType.Name));
        }

        // Checks for supported items that are not in RadarTargets
        private void CheckNewItems()
        {
            Component[] allComponents = FindObjectsOfType<Component>();
            foreach(Component c in allComponents)
            {
                if (FindTargetFromComponent(c) == null)
                {
                    string cTypeName = c.GetType().Name;
                    if (SupportedTargetTypes.Contains(cTypeName))
                    {
                        SpriteInfo spriteInfo = AutoAssignTargetSprite(c);
                        AddTargetItem(c, spriteInfo.sprite, spriteInfo.color, spriteInfo.isRotatable);
                    }
                }
            }
        }

        // Modifies blip of a single target
        public void ModifyTargetBlip(Component c, Sprite s, Color sc)
        {
            RadarTarget t = FindTargetFromComponent(c);
            t.blipComponent.sprite = s;
            t.blipComponent.color = sc;
        }

        // Removes a single target
        public void RemoveTargetItem(RadarTarget t)
        {
            if (t.gameObject != null)
            {
                Debug.Log(string.Format("Unregistered a RadarTarget: {0} ({1})", t.gameObject.name, t.gameScriptType.Name));
            }
            else
            {
                Debug.Log("Unregistered a RadarTarget as its associated GameObject no longer exists");
            }
            
            RadarTargets.Remove(t);
            RadarBlipObjects.Remove(t.blipObject);
            RadarBlipComponents.Remove(t.blipComponent);
            Destroy(t.blipObject);
        }

        // Removes all listed targets
        private void ClearAllListItems()
        {
            foreach (RadarTarget t in RadarTargets.ToArray())
            {
                RemoveTargetItem(t);
            }
        }

        // Removes items that have their associated GameObject deleted
        private void CleanListItems()
        {
            foreach(RadarTarget t in RadarTargets.ToArray())
            {
                if (t.CheckDeleted())
                {
                    RemoveTargetItem(t);
                }
            }
        }

        // Find the registered target associated with a given component
        // Returns null if no target is associated with the component
        private RadarTarget FindTargetFromComponent(Component c)
        {
            foreach (RadarTarget t in RadarTargets)
            {
                if (c.gameObject == t.gameObject)
                {
                    return t;
                }
            }
            return null;
        }

        // Selects SpriteInfo based on a given type name and properties
        private SpriteInfo AutoAssignTargetSprite(Component c)
        {
            Type ctype = c.GetType();
            string ctypename = ctype.Name;
            if (SupportedWeaponTargetTypes.Contains(ctypename))
            {
                return new SpriteInfo(SLine, GetAceRadarColor(AceRadarColors.FullWhite), true);
            }
            else if (SupportedAirTargetTypes.Contains(ctypename))
            {
                return new SpriteInfo(Air_s, DefaultBlipColor, true);
            }
            else
            {
                return new SpriteInfo(Gnd_s, DefaultBlipColor, false);
            }
        }

        // Sets the map radius and scales the rings accordingly
        private void SmoothResizeMap()
        {
            bool ActZoomIn = MapSettings.GetKeyControlDown(MapSettings.KeyControls.ZoomIn);
            bool ActZoomOut = MapSettings.GetKeyControlDown(MapSettings.KeyControls.ZoomOut);

            // Only use the key binds if resize is ready
            if (ResizeCooldownTimer < ResizeCooldownRequired)
            {
                ResizeCooldownTimer += Time.unscaledDeltaTime;
            }
            else    // Notice: No action if (ActZoomIn && ActZoomOut)
            {
                if (ActZoomIn && !ActZoomOut && TargetMapSizeIndex > 0)
                {
                    ResizeCooldownTimer = 0;
                    TargetMapSizeIndex--;
                    MapScaleRate = MapScaleRates[TargetMapSizeIndex];
                }
                else if (ActZoomOut && !ActZoomIn && TargetMapSizeIndex < MapSizes.Length - 1)
                {
                    ResizeCooldownTimer = 0;
                    MapScaleRate = MapScaleRates[TargetMapSizeIndex];
                    TargetMapSizeIndex++;
                }
            }

            MapRadius = Mathf.MoveTowards(MapRadius, MapSizes[TargetMapSizeIndex], MapScaleRate * Time.unscaledDeltaTime);

            float scalar = 16000f / MapRadius;
            MapRingsTransform.localScale = new Vector3(scalar, scalar, 1);
        }

        public object GetComponentProperty(Component c, string property)
        {
            Type ctype = c.GetType();
            PropertyInfo propertyInfo = ctype.GetProperty(property);
            return propertyInfo.GetValue(c);
        }

        public enum AceRadarColors
        {
            White, Red, Blue, Green, Yellow, FullWhite
        }

        public Color GetAceRadarColor(AceRadarColors c)
        {
            Color output;
            switch (c)
            {
                case AceRadarColors.White:
                    output = new Color(1f, 1f, 1f, 0.5f);
                    break;
                case AceRadarColors.Red:
                    output = new Color(1f, 0f, 0f, 0.5f);
                    break;
                case AceRadarColors.Blue:
                    output = new Color(0f, 0.5f, 1f, 0.5f);
                    break;
                case AceRadarColors.Green:
                    output = new Color(0f, 1f, 0f, 0.5f);
                    break;
                case AceRadarColors.Yellow:
                    output = new Color(1f, 1f, 0f, 0.5f);
                    break;
                case AceRadarColors.FullWhite:
                    output = Color.white;
                    break;
                default:
                    output = Color.magenta;
                    break;
            }
            return output;   
        }
    }

    public class RadarTarget
    {
        public GameObject gameObject;
        public Transform transform;

        public Component gameScript;
        public Type gameScriptType;
        public Component refGameScript;
        public Type refGameScriptType;

        public GameObject blipObject;
        public Image blipComponent;
        public bool blipRotatable;

        public bool? forceShouldHideBlip;

        public RadarTarget(Component script, GameObject bo, Image bc, bool rot = false)
        {
            gameObject = script.gameObject;
            transform = script.transform;

            gameScript = script;
            gameScriptType = script.GetType();

            blipObject = bo;
            blipComponent = bc;
            blipRotatable = rot;

            forceShouldHideBlip = null;

            if (gameScriptType.Name == "AiControlledAircraftScript")
            {
                Component[] objAllComponents = gameObject.GetComponents<Component>();
                foreach(Component c in objAllComponents)
                {
                    if (c.GetType().Name == "AircraftScript")
                    {
                        refGameScript = c;
                        refGameScriptType = c.GetType();
                    }
                }
            }
        }

        // Should the drawing script hide the radar blip associated with this RadarTarget?
        public bool CheckShouldHideBlip()
        {
            if (forceShouldHideBlip != null)
            {
                return (bool)forceShouldHideBlip;
            }
            else if (CheckDeleted() || !gameObject.activeSelf)
            {
                return false;
            }
            else if (gameScriptType.Name == "RotatingMissileLauncherScript")
            {
                return (bool)GetComponentProperty("IsDisabled");
            }
            else if (gameScriptType.Name == "AntiAircraftTankScript")
            {
                return (bool)GetComponentProperty("IsDead");
            }
            else if (gameScriptType.Name == "SimpleGroundVehicleScript")
            {
                return (bool)GetComponentProperty("IsDestroyed");
            }
            else if (gameScriptType.Name == "SinkableShipScript")
            {
                return (bool)GetComponentProperty("IsCriticallyDamaged");
            }
            else if (gameScriptType.Name == "AiControlledAircraftScript" && refGameScriptType.Name == "AircraftScript")
            {
                return (bool)GetRefComponentProperty("CriticallyDamaged");
            }
            else if (gameScriptType.Name == "MissileScript" || gameScriptType.Name == "BombScript")
            {
                return !(bool)GetComponentProperty("Fired") || (bool)GetComponentProperty("IsDestroyed");
            }
            else if (gameScriptType.Name == "RocketScript")
            {
                return !(bool)GetComponentProperty("IsLaunched") || (bool)GetComponentProperty("HasExploded");
            }
            else if (gameScriptType.Name == "FracturedObject")
            {
                return gameObject.transform.childCount < 3;
            }
            else
            {
                // Unsupported target component type
                return false;
            }
        }

        public bool CheckDeleted()
        {
            if (gameObject == null || gameScript == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public float GetDistanceFrom(Vector3 otherPos)
        {
            return Vector3.Distance(GetPosition(), otherPos);
        }

        public Vector3 GetPosition()
        {
            if (gameScriptType.Name == "AiControlledAircraftScript")
            {
                return (Vector3)GetRefComponentProperty("Position");
            }
            else
            {
                return gameObject.transform.position;
            }
        }

        public Vector3 GetEulerAngles()
        {
            if (gameScriptType.Name == "AiControlledAircraftScript")
            {
                return (Vector3)GetRefComponentProperty("Rotation");
            }
            else
            {
                return gameObject.transform.eulerAngles;
            }
        }

        public object GetComponentProperty(string property)
        {
            PropertyInfo propertyInfo = gameScriptType.GetProperty(property);
            return propertyInfo.GetValue(gameScript);
        }

        public object GetRefComponentProperty(string property)
        {
            PropertyInfo propertyInfo = refGameScriptType.GetProperty(property);
            return propertyInfo.GetValue(refGameScript);
        }
    }

    public struct SpriteInfo
    {
        public Sprite sprite;
        public Color color;
        public bool isRotatable;

        public SpriteInfo(Sprite s, Color c, bool rot)
        {
            sprite = s;
            color = c;
            isRotatable = rot;
        }
    }
}