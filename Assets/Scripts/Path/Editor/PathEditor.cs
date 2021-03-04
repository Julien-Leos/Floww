using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor {
    PathCreator creator;
    Path path;

    int closestAnchorIndex = -1;
    int closestSegmentIndex = -1;
    const float minDistFromElement = .5f;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();

        if (GUILayout.Button("Reset")) {
            Undo.RecordObject(creator, "Reset Path");
            creator.CreatePath();
            path = creator.path;
        }

        if (GUILayout.Button("Flatten")) {
            Undo.RecordObject(creator, "Flatten Path");
            path.Flatten();
        }

        bool isLoop = GUILayout.Toggle(path.IsLoop, "Loop");
        if (isLoop != path.IsLoop) {
            Undo.RecordObject(creator, "Toggle Loop");
            path.IsLoop = isLoop;
        }

        bool isSmooth = GUILayout.Toggle(path.IsSmooth, "Smooth Path");
        if (isSmooth != path.IsSmooth) {
            Undo.RecordObject(creator, "Toggle Smooth Path");
            path.IsSmooth = isSmooth;
        }

        int pointsBySegment = EditorGUILayout.IntField("Points by Segment", path.PointsBySegment);
        if (pointsBySegment != path.PointsBySegment) {
            Undo.RecordObject(creator, "Update Number of Points by Segment");
            path.PointsBySegment = pointsBySegment;
        }

        if (EditorGUI.EndChangeCheck()) {
            SceneView.RepaintAll();
        }
    }

    void OnSceneGUI() {
        Input();
        Draw();
    }

    void Input() {
        RaycastHit hit;
        bool mouseHit = MouseUtility.GetMouseWorldPosition(out hit);

        if (Event.current.type == EventType.MouseDown && mouseHit) {
            if (Event.current.button == 0 && Event.current.shift) {
                if (closestSegmentIndex != -1) {
                    Undo.RecordObject(creator, "Split segment");
                    path.SplitSegment(hit.point, closestSegmentIndex);
                } else if (!path.IsLoop) {
                    Undo.RecordObject(creator, "Add segment");
                    path.AddSegment(hit.point);
                }
            }

            if (Event.current.button == 1 && Event.current.shift) {
                if (closestAnchorIndex != -1) {
                    Undo.RecordObject(creator, "Delete segment");
                    path.DeleteSegment(closestAnchorIndex);
                }
            }
        }

        if (Event.current.type == EventType.MouseMove) {
            List<float> distToSegment = new List<float>();
            List<float> distToAnchor = new List<float>();

            for (int i = 0; i < path.NumSegments + (path.IsLoop ? 0 : 1); i++) {
                List<Vector3> handles = path.GetSegmentHandles(i % path.NumSegments);
                distToSegment.Add(HandleUtility.DistancePointBezier(hit.point, handles[0], handles[3], handles[1], handles[2]));
                distToAnchor.Add(Vector3.Distance(hit.point, path[i * 3]));
            }
            int newClosestSegmentIndex = distToSegment.IndexOf(distToSegment.Min());
            int newClosestAnchorIndex = distToAnchor.IndexOf(distToAnchor.Min()) * 3;
            closestSegmentIndex = distToSegment.Min() < minDistFromElement ? (closestSegmentIndex != newClosestSegmentIndex ? newClosestSegmentIndex : closestSegmentIndex) : -1;
            closestAnchorIndex = distToAnchor.Min() < minDistFromElement ? (closestAnchorIndex != newClosestAnchorIndex ? newClosestAnchorIndex : closestAnchorIndex) : -1;
        }
    }

    void Draw() {
        for (int i = 0; i < path.NumSegments; i++) {
            List<Vector3> handles = path.GetSegmentHandles(i);
            Handles.color = Color.black;
            Handles.DrawLine(handles[1], handles[0]);
            Handles.DrawLine(handles[2], handles[3]);
            Color segmentColor = (i == closestSegmentIndex && Event.current.shift) ? Color.yellow : Color.green;
            Handles.DrawBezier(handles[0], handles[3], handles[1], handles[2], segmentColor, null, 4);

            List<Vector3> points = path.GetSegmentPoints(i);
            Handles.color = Color.white;
            foreach (Vector3 point in points) {
                Handles.FreeMoveHandle(point, Quaternion.identity, 0.05f, Vector3.zero, Handles.SphereHandleCap);
            }
        }

        for (int i = 0; i < path.NumHandles; i++) {
            bool isAnchor = i % 3 == 0;
            Handles.color = isAnchor ? ((i == closestAnchorIndex && Event.current.shift) ? Color.yellow : Color.red) : Color.blue;
            Vector3 handlePos = Handles.FreeMoveHandle(path[i], Quaternion.identity, isAnchor ? .2f : 0.1f, Vector3.zero, Handles.SphereHandleCap);
            if (path[i] != handlePos) {
                Undo.RecordObject(creator, "Move point");
                path.MovePoint(i, handlePos);
            }
        }
    }

    void OnEnable() {
        creator = (PathCreator)target;
        if (creator.path == null) {
            creator.CreatePath();
        }
        path = creator.path;
    }
}
