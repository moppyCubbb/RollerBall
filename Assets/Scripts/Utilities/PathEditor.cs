using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*  Built game cannot use the UnityEditor namespace as it comes with the UnityEditor.dll
    which is not compatible with any build
    need to add #if UNITY_EDITOR for any editor method
*/
#if UNITY_EDITOR
[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor
{
    PathCreator creator;
    Path Path
    {
        get
        {
            return creator.path;
        }
    }

    const float segmentSelectDistanceThreshold = 0.1f;
    int selectedSegmentIndex = -1;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("Create new"))
        {
            creator.CreatePath();
            SceneView.RepaintAll();
        }

        bool isClosed = GUILayout.Toggle(Path.IsClosed, "Toggle closed");
        if (isClosed != Path.IsClosed)
        {
            Undo.RecordObject(creator, "Toggle closed");
            Path.IsClosed = isClosed;
        }

        bool autoSetControlPoints = GUILayout.Toggle(Path.AutoSetControlPoints, "Auto Set Control Points");
        if (autoSetControlPoints != Path.AutoSetControlPoints)
        {
            Undo.RecordObject(creator, "Toggle auto set controls");
            Path.AutoSetControlPoints = autoSetControlPoints;
        }

        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }

    private void OnEnable()
    {
        creator = (PathCreator)target;
        if (creator.path == null)
        {
            creator.CreatePath();
        }
    }

    private void OnSceneGUI()
    {
        Input();
        Draw();
    }

    private void Draw()
    {
        for (int i = 0; i < Path.SegmentCount; i++)
        {
            Vector2[] points = Path.GetPointsInSegments(i);
            if (creator.displayControlPoints)
            {
                Handles.color = creator.controlHandColor;
                Handles.DrawLine(points[1], points[0]);
                Handles.DrawLine(points[2], points[3]);
            }
            
            Color segmentColor = (i == selectedSegmentIndex && Event.current.shift) ? creator.selectedSegmentColor : creator.segmentColor;
            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColor, null, 2);
        }

        for (int i = 0; i < Path.PointCount; i++)
        {
            Handles.color = i % 3 == 0 ? creator.anchorColor : creator.controlColor;
            float handleSize = i % 3 == 0 ? creator.anchorDiameter : creator.controlDiameter;
            
            Vector2 newPos = Handles.FreeMoveHandle(Path[i], Quaternion.identity, handleSize, Vector2.zero, Handles.CylinderHandleCap);
            if (Path[i] != newPos)
            {
                Undo.RecordObject(creator, "Move point");
                Path.MovePoint(i, newPos);
            }
        }
    }

    private void Input()
    {
        Event guiEvent = Event.current;
        Vector2 mousePosition = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
        {
            if (selectedSegmentIndex != -1)
            {
                Undo.RecordObject(creator, "Split segment");
                Path.SplitSegment(mousePosition, selectedSegmentIndex);
            }
            else
            {
                Undo.RecordObject(creator, "Add segment");
                Path.AddSegment(mousePosition);
            }
        }
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
        {
            float minDistanceToAnchor = creator.anchorDiameter * 0.5f;
            int cloestAnchorIndex = -1;

            for (int i = 0; i < Path.PointCount; i += 3)
            {
                float distance = Vector2.Distance(mousePosition, Path[i]);
                if (distance < minDistanceToAnchor)
                {
                    minDistanceToAnchor = distance;
                    cloestAnchorIndex = i;
                }
            }

            if (cloestAnchorIndex != -1)
            {
                Undo.RecordObject(creator, "Delete segment");
                Path.DeleteSegment(cloestAnchorIndex);
            }
        }

        if (guiEvent.type == EventType.MouseMove)
        {
            float minDistanceToSegment = segmentSelectDistanceThreshold;
            int newSelectedSegmentIndex = -1;

            for (int i = 0; i < Path.SegmentCount; i++)
            {
                Vector2[] points = Path.GetPointsInSegments(i);
                float distance = HandleUtility.DistancePointBezier(mousePosition, points[0], points[3], points[1], points[2]);

                if (distance < minDistanceToSegment)
                {
                    minDistanceToSegment = distance;
                    newSelectedSegmentIndex = i;
                }
            }

            if (newSelectedSegmentIndex != selectedSegmentIndex)
            {
                selectedSegmentIndex = newSelectedSegmentIndex;
                HandleUtility.Repaint();
            }

        }

        HandleUtility.AddDefaultControl(0); //if nothing selected, the invisible defualt control will be selected and be able to select thing behind the mesh 
    }
}
#endif
