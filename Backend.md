# Backend Guide

The `AceRadarBackend.cs` script performs reflection behind the scenes, making Ace Radar functionality more accessible to modders.

### Important Notes

**[ Terms of Use ]**

- Users should not include AceRadar directly into SPMODs. Instead, AceRadarBackend or direct reflection should be used.
- Users are free to use and modify AceRadarBackend for SPMODs.
- Users may freely distribute AceRadarBackend, its derivatives, and original scripts that modify AceRadar.

**[ Using the Backend ]**
- **If values in AceRadar are modified when entering a level, please reset them when exiting.**
- **Do not use FindObjectOfType to search for AceRadarBackend.** Instead:
  - Assign a reference to AceRadarBackend in the Editor, or
  - Place AceRadarBackend in the same GameObject as your script, or another GameObject in your mod, and use `GetComponent<AceRadarBackend>()`.
- Ensure that the mod loading order is more than 900.
- Do not get values from AceRadar in Awake().
- Do not add, modify, or remove radar blip GameObjects directly.
  - Other GameObjects (e.g., map background, grid, information, etc.) can be added. Remember to remove them when exiting levels.
- Refer to the section below for more details.

**[ Ace Radar Processes ]**

- Initialization of AceRadar and AceRadarBackend are both done in `Awake()`. Fields, default values, and references are obtained here. **Do not get their values in `Awake()`.**
- Every `IntervalCheckNewItems` frames in the simulation, AceRadar checks for supported targets and adds them to the RadarTargets list.
  - When a suitable target component is detected, it can take anywhere from zero to `IntervalCheckNewItems` frames, inclusive, for the target to be added.
  - [AceRadarVanillaAddon/LevelMapScripts.cs](https://github.com/hpgbproductions/AceRadarVanillaAddon/blob/main/Assets/Scripts/LevelMapScripts.cs) demonstrates use in a Persistent Object, to modify blips of targets when they appear.
  - [TK2/../AllyMarkers.cs](https://github.com/hpgbproductions/TK2/blob/main/Scripts/AceRadar/AllyMarkers.cs) demonstrates use in a component loaded when a level is started, to modify blips of specific targets some time after the level starts.
- Refer to the [RadarTargets](https://github.com/hpgbproductions/AceRadar/blob/9b4bbb7fcadb7406756d217298b3b35a80c0e3f2/Assets/Scripts/MapController.cs#L407) class for fields and methods.
  - `CheckDeleted()` is true if the GameObject or Component no longer exists. The RadarTarget will then be deleted.
  - `CheckShouldHideBlip()` controls whether the target blip is hidden. The value is as follows:
    - The value of `forceShouldHideBlip` if it is not null. This is useful for RadarTargets registered with custom or other unsupported Components.
    - Else, if the target is deleted or its GameObject is inactive in hierarchy, the value is True.
    - Else, if the Component type is one of the defined types, the value is calculated from property values of the Component.
    - Else, false.
    - Note: `CheckShouldHideBlip()` is not used in the calculation of `CheckDeleted()`.
- The RadarTarget list is automatically cleared when exiting the simulation.

# Backend Reference

### Public Fields

At the time of writing, the copy of AceRadarBackend provided in this repository is identical to the one in Teki no Kichi 2 Ver1.1. The copy in AceRadar Vanilla Addon is older and has fewer features.

**Initialized**

- `public bool Initialized`
- True if AceRadar is loaded, and AceRadarBackend is able to locate it.
- Check this before using any AceRadar functionality in `Start()`, `Update()`, etc. Do not perform the check in `Awake()`.

### Public Enumerations

**AceRadarColors**

- `public enum AceRadarColors { White, Red, Blue, Green, Yellow, FullWhite }`
- Each value corresponds to a preset color.

**AceRadarSprites**

- `public enum AceRadarSprites { Aircraft, AircraftCircled, Ground, GroundCircled, WeaponLine }`
- Each value corresponds to a preset sprite.

### Public Methods

**FindAndModifyTargetBlip**

- `public void FindAndModifyTargetBlip(PrefabProxy enemyProxy, AceRadarSprites sprite, AceRadarColors color, bool rotatable = false)`
- A convenient method that sets the marker of a target proxy object. A must-learn for map and custom level modders seeking to include Ace Radar functionality.
- `enemyProxy`: Ship, missile launcher, or AA tank proxy.
- `sprite`: Blip sprite.
- `color`: Blip color.
- `rotatable`: Whether the blip rotates according to the target's relative heading.

**SetDefaultBlipColor**

- `public void SetDefaultBlipColor(AceRadarColors color)`
- `color`: Preset color.

**SetCheckInterval**

- `public void SetCheckInterval(int fcount)`
- `fcount`: AceRadar checks for new targets every this number of frames.

**GetCheckInterval**
- `public int GetCheckInterval()`
- AceRadar checks for new targets every this number of frames.

**AddTargetItem**

- `public object AddTargetItem(Component component, AceRadarSprites sprite, AceRadarColors color, bool rotatable = false)`
- Manually creates a new RadarTarget and adds it to the list. Use this to register unsupported types that are not automatically registered by Ace Radar.
- Corresponds to `AceRadar.AddTargetItem( ... )`.
- `component`: Component to register.
- `sprite`: Blip sprite.
- `color`: Blip color.
- `rotatable`: Whether the blip rotates according to the target's relative heading.

**RemoveTargetItem**

- `public void RemoveTargetItem(object obj)`
- Manually removes a RadarTarget from the list. Use this to manually remove unsupported types added by Ace Radar, without deleting its associated GameObject or Component.
- Corresponds to `AceRadar.RemoveTargetItem( ... )`.
- `obj`: RadarTarget, or the Component or GameObject associated with the RadarTarget.

**ModifyTargetBlip**

- `public void ModifyTargetBlip(object obj, AceRadarSprites sprite, AceRadarColors color, bool rotatable = false)`
- Selects a RadarTarget and changes its blip style.
- Corresponds to `AceRadar.ModifyTargetBlip( ... )`.
- `obj`: RadarTarget, or the Component or GameObject associated with the RadarTarget.
- `sprite`: Blip sprite.
- `color`: Blip color.
- `rotatable`: Whether the blip rotates according to the target's relative heading.

**GetRadarTargetField**

- `public object GetRadarTargetField(object obj, string fieldName)`
- Get the value of a field in the given RadarTarget.
- `obj`: RadarTarget, or the Component or GameObject associated with the RadarTarget.
- `fieldName`: Name of the field to retrieve.

**SetRadarTargetField**

- `public void SetRadarTargetField(object obj, string fieldName, object value)`
- Set the value of a field in the given RadarTarget.
- `obj`: RadarTarget, or the Component or GameObject associated with the RadarTarget.
- `fieldName`: Name of the field to select.
- `value`: Value to write to the field.

**FindTargetFromObject**

- `public object FindTargetFromObject(object obj)`
- Returns the RadarTarget associated with the object, or `obj` if none exists (useful for when `obj` is a RadarTarget).
- This is used in most of the methods listed above, allowing a RadarTarget, or its associated Component or GameObject, to be passed.
- `obj`: The Component or GameObject associated with the RadarTarget.
