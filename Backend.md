# Backend Guide

The `AceRadarBackend.cs` script performs reflection behind the scenes, making Ace Radar functionality more accessible to modders.

### Important Notes

**[ Terms of Use ]**

- Users should not include AceRadar directly into SPMODs. Instead, AceRadarBackend should be used.
- Users are free to use and modify AceRadarBackend for SPMODs.
- Users may freely distribute AceRadarBackend or its derivatives.
- **If values in AceRadar are modified when entering a level, please reset them when exiting.**

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

