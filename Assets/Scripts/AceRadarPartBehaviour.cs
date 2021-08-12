using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class AceRadarPartBehaviour : Jundroo.SimplePlanes.ModTools.Parts.PartModifierBehaviour
{
    private AceRadarPart modifier;

    private MeshRenderer HitboxRenderer;

    public Canvas MapCanvas;
    public Transform MapRootTransform;
    public GameObject MapTargetsParent;        // Tag: Mod Tag 2
    public RectTransform MapRingsTransform;    // Tag: Mod Tag 1

    public GameObject MapFrameObject;    // Tag: Mod Tag 3
    public Image MapFrame;

    public float MapRadius = 16000f;
    public bool MapRadiusUsesInputController = false;

    private void Start()
    {
        modifier = (AceRadarPart)PartModifier;

        HitboxRenderer = GetComponentInChildren<MeshRenderer>();
        MapCanvas = GetComponentInChildren<Canvas>();
        MapRootTransform = MapCanvas.transform.GetChild(0);

        for (int i = 0; i < MapRootTransform.childCount; i++)
        {
            GameObject childObject = MapRootTransform.GetChild(i).gameObject;
            if (childObject.tag == "Mod Tag 2")
            {
                MapTargetsParent = childObject;
            }
            else if (childObject.tag == "Mod Tag 1")
            {
                MapRingsTransform = childObject.transform.GetChild(0).GetComponent<RectTransform>();
            }
            else if (childObject.tag == "Mod Tag 3")
            {
                MapFrameObject = childObject;
                MapFrame = MapFrameObject.GetComponent<Image>();
            }
        }

        HitboxRenderer.enabled = ServiceProvider.Instance.GameState.IsInDesigner;
        ApplyValues();

        MapRadiusUsesInputController = modifier.ScaleWithInput;
    }

    private void Update()
    {
        if (ServiceProvider.Instance.GameState.IsInDesigner)
        {
            ApplyValues();
        }
        else    // In sandbox
        {
            if (MapRadiusUsesInputController)
            {
                MapRadius = InputController.Value;
            }
            float scalar = 16000f / MapRadius;
            MapRingsTransform.localScale = new Vector3(scalar, scalar, 1);
        }
    }

    private void ApplyValues()
    {
        MapFrame.color = new Color(modifier.FrameColorRed, modifier.FrameColorGreen, modifier.FrameColorBlue, modifier.FrameColorAlpha);
        MapCanvas.sortingOrder = modifier.SortingOrder;
    }
}