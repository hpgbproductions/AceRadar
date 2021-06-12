using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AceRadarBackend : MonoBehaviour
{
    private Component Controller;
    private Type ControllerType;
    private Type RadarTargetType;

    private FieldInfo InfoDefaultBlipColor;
    private FieldInfo InfoIntervalCheckNewItems;

    private MethodInfo InfoAddTargetItem;
    private MethodInfo InfoRemoveTargetItem;
    private MethodInfo InfoModifyTargetBlip;
    private MethodInfo InfoFindTargetFromComponent;
    private MethodInfo InfoFindTargetFromGameObject;

    private void Start()
    {
        Component[] components = FindObjectsOfType<Component>();
        foreach (Component c in components)
        {
            if (c.GetType().FullName == "AceRadar.MapController")
            {
                Controller = c;
                ControllerType = c.GetType();
            }
        }

        if (Controller == null)
        {
            Debug.LogError("AceRadarBackend cannot locate MapController! (" + Assembly.GetExecutingAssembly().GetName() + ")\nTroubleshooting:\n" +
                "- Ensure that Mod Load Priority is more than 900.\n" +
                "- If manually enabling mods, enable AceRadar before this mod.");
        }

        InfoDefaultBlipColor = ControllerType.GetField("DefaultBlipColor");
        InfoIntervalCheckNewItems = ControllerType.GetField("IntervalCheckNewItems");

        InfoAddTargetItem = ControllerType.GetMethod("AddTargetItem");
        InfoRemoveTargetItem = ControllerType.GetMethod("RemoveTargetItem");
        InfoModifyTargetBlip = ControllerType.GetMethod("ModifyTargetBlip");

        InfoFindTargetFromComponent = ControllerType.GetMethod("FindTargetFromComponent");
        InfoFindTargetFromGameObject = ControllerType.GetMethod("FindTargetFromGameObject");
        RadarTargetType = InfoFindTargetFromGameObject.ReturnType;
    }

    public void SetDefaultBlipColor(AceRadarColors color)
    {
        InfoDefaultBlipColor.SetValue(Controller, GetAceRadarColor(color));
    }

    public void SetCheckInterval(int fcount)
    {
        InfoIntervalCheckNewItems.SetValue(Controller, fcount);
    }

    /// <summary>
    /// Creates a new RadarTarget and registers it into AceRadar.
    /// </summary>
    /// <param name="component">A component attached to the GameObject, which Transform is to be monitored.</param>
    /// <param name="sprite">Sprite for the radar blip.</param>
    /// <param name="color">Color for the radar blip.</param>
    /// <param name="rotatable">Does the heading of the target affect the rotation of the blip?</param>
    public object AddTargetItem(Component component, AceRadarSprites sprite, AceRadarColors color, bool rotatable = false)
    {
        return InfoAddTargetItem.Invoke(Controller, new object[] { component, (int)sprite, GetAceRadarColor(color), rotatable });
    }

    /// <summary>
    /// Unregisters the RadarTarget.
    /// </summary>
    /// <param name="obj">RadarTarget, or the Component or GameObject associated with the RadarTarget.</param>
    public void RemoveTargetItem(object obj)
    {
        object radarTarget = FindTargetFromObject(obj);
        InfoRemoveTargetItem.Invoke(Controller, new object[] { radarTarget });
    }

    /// <summary>
    /// Set the blip style of a RadarTarget.
    /// </summary>
    /// <param name="obj">RadarTarget, or the Component or GameObject associated with the RadarTarget.</param>
    /// <param name="sprite">Sprite for the radar blip.</param>
    /// <param name="color">Color for the radar blip.</param>
    /// <param name="rotatable">Does the heading of the target affect the rotation of the blip?</param>
    public void ModifyTargetBlip(object obj, AceRadarSprites sprite, AceRadarColors color, bool rotatable = false)
    {
        object radarTarget = FindTargetFromObject(obj);
        InfoModifyTargetBlip.Invoke(Controller, new object[] { radarTarget, (int)sprite, GetAceRadarColor(color), rotatable });
    }

    public object GetRadarTargetField(object obj, string fieldName)
    {
        object radarTarget = FindTargetFromObject(obj);
        FieldInfo field = RadarTargetType.GetField(fieldName);
        return field.GetValue(radarTarget);
    }

    public void SetRadarTargetField(object obj, string fieldName, object value)
    {
        object radarTarget = FindTargetFromObject(obj);
        FieldInfo field = RadarTargetType.GetField(fieldName);
        field.SetValue(radarTarget, value);
    }

    /// <summary>
    /// Finds a RadarTarget from a given object.
    /// Returns the associated RadarTarget if obj is a Component or GameObject, otherwise returns obj.
    /// </summary>
    /// <param name="obj">Object used to identify the RadarTarget.</param>
    /// <returns></returns>
    public object FindTargetFromObject(object obj)
    {
        Type objType = obj.GetType();
        if (objType == typeof(Component) || objType.IsSubclassOf(typeof(Component)))
            return InfoFindTargetFromComponent.Invoke(Controller, new object[] { obj });
        else if (objType == typeof(GameObject) || objType.IsSubclassOf(typeof(GameObject)))
            return InfoFindTargetFromGameObject.Invoke(Controller, new object[] { obj });
        else
            return obj;
    }

    /// <summary>
    /// Corresponds to preset colors used by AceRadar.
    /// </summary>
    public enum AceRadarColors
    {
        White, Red, Blue, Green, Yellow, FullWhite
    }

    private Color GetAceRadarColor(AceRadarColors c)
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

    /// <summary>
    /// Corresponds to preset sprites used by AceRadar.
    /// </summary>
    public enum AceRadarSprites { Aircraft, AircraftCircled, Ground, GroundCircled, WeaponLine };
}
