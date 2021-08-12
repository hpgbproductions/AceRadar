using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Jundroo.SimplePlanes.ModTools.Parts;
using Jundroo.SimplePlanes.ModTools.Parts.Attributes;
using UnityEngine;

/// <summary>
/// A part modifier for SimplePlanes.
/// A part modifier is responsible for attaching a part modifier behaviour script to a game object within a part's hierarchy.
/// </summary>
[Serializable]
public class AceRadarPart : Jundroo.SimplePlanes.ModTools.Parts.PartModifier
{
    [SerializeField]
    [DesignerPropertyToggleButton("Default", "InputController", Label = "Scaling Mode", Order = 0)]
    private bool _scaleWithInput = false;

    [SerializeField]
    [DesignerPropertySlider(Label = "Sorting Order", MaxValue = 15, MinValue = 0, NumberOfSteps = 16, Order = 10)]
    private int _sortOrder = 0;

    [SerializeField]
    [DesignerPropertySlider(Label = "Frame Color Red", MaxValue = 1, MinValue = 0, NumberOfSteps = 101, Order = 100)]
    private float _frameColorRed = 1;

    [SerializeField]
    [DesignerPropertySlider(Label = "Frame Color Green", MaxValue = 1, MinValue = 0, NumberOfSteps = 101, Order = 110)]
    private float _frameColorGreen = 1;

    [SerializeField]
    [DesignerPropertySlider(Label = "Frame Color Blue", MaxValue = 1, MinValue = 0, NumberOfSteps = 101, Order = 120)]
    private float _frameColorBlue = 1;

    [SerializeField]
    [DesignerPropertySlider(Label = "Frame Color Alpha", MaxValue = 1, MinValue = 0, NumberOfSteps = 101, Order = 130)]
    private float _frameColorAlpha = 0.6f;

    public bool ScaleWithInput
    {
        get
        {
            return _scaleWithInput;
        }
    }

    public int SortingOrder
    {
        get
        {
            return _sortOrder;
        }
    }

    public float FrameColorRed
    {
        get
        {
            return _frameColorRed;
        }
    }

    public float FrameColorGreen
    {
        get
        {
            return _frameColorGreen;
        }
    }

    public float FrameColorBlue
    {
        get
        {
            return _frameColorBlue;
        }
    }

    public float FrameColorAlpha
    {
        get
        {
            return _frameColorAlpha;
        }
    }

    /// <summary>
    /// Called when this part modifiers is being initialized as the part game object is being created.
    /// </summary>
    /// <param name="partRootObject">The root game object that has been created for the part.</param>
    /// <returns>The created part modifier behaviour, or <c>null</c> if it was not created.</returns>
    public override Jundroo.SimplePlanes.ModTools.Parts.PartModifierBehaviour Initialize(UnityEngine.GameObject partRootObject)
    {
        // Attach the behaviour to the part's root object.
        var behaviour = partRootObject.AddComponent<AceRadarPartBehaviour>();
        return behaviour;
    }
}