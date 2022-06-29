using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathCreator : MonoBehaviour
{
    [HideInInspector]
    public Path path;

    public Color anchorColor = Color.red;
    public Color controlColor = Color.white;
    public Color controlHandColor = Color.black;
    public Color segmentColor = Color.green;
    public Color selectedSegmentColor = Color.yellow;
    public float anchorDiameter = 0.5f;
    public float controlDiameter = 0.375f;
    public bool displayControlPoints = true;


    public void CreatePath()
    {
        path = new Path(transform.position);
    }

    private void Reset()
    {
        CreatePath();
    }
}
