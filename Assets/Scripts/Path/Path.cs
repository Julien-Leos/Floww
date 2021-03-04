using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path {
    [SerializeField, HideInInspector]
    public List<Vector3> handles;

    [SerializeField, HideInInspector]
    public List<Vector3> segmentPoints;

    [SerializeField, HideInInspector]
    private int pointsBySegment = 20;

    [SerializeField, HideInInspector]
    private bool isLoop = false;

    [SerializeField, HideInInspector]
    private bool isSmooth = false;

    public Path(Vector3 center) {
        handles = new List<Vector3>
        {
            center + Vector3.left,
            center + Vector3.left * .5f,
            center + Vector3.right * .5f,
            center + Vector3.right
        };

        segmentPoints = new List<Vector3>(new Vector3[pointsBySegment]);
        UpdateSegmentPoints(0);
    }

    public Vector3 this[int i] {
        get {
            return handles[LoopHandle(i)];
        }
    }

    public int NumHandles {
        get {
            return handles.Count;
        }
    }

    public int NumSegments {
        get {
            return handles.Count / 3;
        }
    }

    public int PointsBySegment {
        get {
            return pointsBySegment;
        }
        set {
            if (pointsBySegment != value) {
                pointsBySegment = value;
                segmentPoints = new List<Vector3>(new Vector3[NumSegments * pointsBySegment]);
                UpdateAllSegmentsPoints();
            }
        }
    }

    public bool IsLoop {
        get {
            return isLoop;
        }
        set {
            if (isLoop != value) {
                isLoop = value;
                ToogleLoop();
            }
        }
    }

    public bool IsSmooth {
        get {
            return isSmooth;
        }
        set {
            if (isSmooth != value) {
                isSmooth = value;
                if (isSmooth) {
                    SmoothAllAnchors();
                }
            }
        }
    }

    public List<Vector3> GetSegmentHandles(int i) {
        return new List<Vector3> { handles[i * 3], handles[i * 3 + 1], handles[i * 3 + 2], handles[LoopHandle(i * 3 + 3)] };
    }

    public List<Vector3> GetSegmentPoints(int i) {
        return segmentPoints.GetRange(i * pointsBySegment, pointsBySegment);
    }

    public void AddSegment(Vector3 pos) {
        handles.Add(handles[handles.Count - 1] * 2 - handles[handles.Count - 2]);
        handles.Add((handles[handles.Count - 1] + pos) / 2);
        handles.Add(pos);
        segmentPoints.AddRange(new Vector3[pointsBySegment]);

        if (isSmooth) {
            SmoothNeighbourAnchors(handles.Count - (isLoop ? 3 : 1));
            UpdateSegmentPoints(NumSegments - 2);
        }
        UpdateSegmentPoints(NumSegments - 1);
    }

    public void SplitSegment(Vector3 anchorPos, int segmentIndex) {
        handles.InsertRange(segmentIndex * 3 + 2, new List<Vector3> { Vector3.zero, anchorPos, Vector3.zero });
        segmentPoints.InsertRange(segmentIndex * pointsBySegment, new Vector3[pointsBySegment]);
        if (isSmooth) {
            SmoothNeighbourAnchors(segmentIndex * 3 + 3);
        } else {
            SmoothAnchor(segmentIndex * 3 + 3);
        }
    }

    public void DeleteSegment(int anchorIndex) {
        if (NumSegments > 2 || !isLoop && NumSegments > 1) {
            if (anchorIndex == 0) {
                if (isLoop) {
                    handles[handles.Count - 1] = handles[2];
                }
                handles.RemoveRange(0, 3);
                segmentPoints.RemoveRange(0, pointsBySegment);
                UpdateSegmentPoints(NumSegments - 1);
            } else if (anchorIndex == handles.Count - 1 && !isLoop) {
                handles.RemoveRange(anchorIndex - 2, 3);
                segmentPoints.RemoveRange((anchorIndex / 3 - 1) * pointsBySegment, pointsBySegment);
            } else {
                handles.RemoveRange(anchorIndex - 1, 3);
                segmentPoints.RemoveRange(anchorIndex / 3 * pointsBySegment, pointsBySegment);
                UpdateSegmentPoints(anchorIndex / 3 - 1);
            }
            if (isSmooth) {
                SmoothNeighbourAnchors(anchorIndex);
            }
        }
    }

    public void MovePoint(int handleIndex, Vector3 pos) {
        if (handleIndex % 3 == 0 || !isSmooth) {
            Vector3 deltaMove = pos - handles[handleIndex];
            handles[handleIndex] = pos;

            if (isSmooth) {
                SmoothNeighbourAnchors(handleIndex);
            } else {
                if (handleIndex % 3 == 0) {
                    if (handleIndex + 1 < handles.Count || isLoop) {
                        handles[LoopHandle(handleIndex + 1)] += deltaMove;
                    }
                    if (handleIndex - 1 >= 0 || isLoop) {
                        handles[LoopHandle(handleIndex - 1)] += deltaMove;
                    }
                } else {
                    bool isNextHandleAnchor = (handleIndex + 1) % 3 == 0;
                    int polarHandleIndex = isNextHandleAnchor ? handleIndex + 2 : handleIndex - 2;
                    handleIndex = isNextHandleAnchor ? handleIndex + 1 : handleIndex - 1;

                    if (polarHandleIndex < handles.Count && polarHandleIndex >= 0 || isLoop) {
                        float dist = (handles[LoopHandle(handleIndex)] - handles[LoopHandle(polarHandleIndex)]).magnitude;
                        Vector3 dir = (handles[LoopHandle(handleIndex)] - pos).normalized;
                        handles[LoopHandle(polarHandleIndex)] = handles[LoopHandle(handleIndex)] + dir * dist;
                    }
                }
            }
        }

        if (handleIndex / 3 < NumSegments || isLoop) {
            UpdateSegmentPoints(LoopSegment(handleIndex / 3));
        }
        if (handleIndex / 3 - 1 >= 0 || isLoop) {
            UpdateSegmentPoints(LoopSegment(handleIndex / 3 - 1));
        }
    }

    public void Flatten() {
        for (int i = 0; i < handles.Count; i += 3) {
            MovePoint(i, new Vector3(handles[i].x, 0, handles[i].z));
        }

        if (!isSmooth) {
            for (int i = 0; i < handles.Count; i++) {
                if (i % 3 != 0) {
                    MovePoint(i, new Vector3(handles[i].x, 0, handles[i].z));
                }
            }
        }
    }

    private void ToogleLoop() {
        if (isLoop) {
            handles.Add(handles[handles.Count - 1] * 2 - handles[handles.Count - 2]);
            handles.Add(handles[0] * 2 - handles[1]);
            segmentPoints.AddRange(new Vector3[pointsBySegment]);

            if (isSmooth) {
                SmoothAnchor(0);
                SmoothAnchor(handles.Count - 3);
            }
            UpdateSegmentPoints(NumSegments - 1);
        } else {
            handles.RemoveRange(handles.Count - 2, 2);
            segmentPoints.RemoveRange(segmentPoints.Count - pointsBySegment, pointsBySegment);

            if (isSmooth) {
                SmoothStartAndEndHandles();
            }
        }
    }

    private void SmoothAllAnchors() {
        for (int i = 0; i < handles.Count; i += 3) {
            SmoothAnchor(i);
        }
        SmoothStartAndEndHandles();
    }

    private void SmoothNeighbourAnchors(int centralAnchorIndex) {
        for (int i = centralAnchorIndex - 3; i <= centralAnchorIndex + 3; i += 3) {
            if (i >= 0 && i < handles.Count || isLoop) {
                SmoothAnchor(LoopHandle(i));
            }
        }
        SmoothStartAndEndHandles();
    }

    private void SmoothStartAndEndHandles() {
        if (!isLoop) {
            handles[1] = (handles[0] + handles[2]) * .5f;
            handles[handles.Count - 2] = (handles[handles.Count - 1] + handles[handles.Count - 3]) * .5f;

            UpdateSegmentPoints(0);
            UpdateSegmentPoints(NumSegments - 1);
        }
    }

    private void SmoothAnchor(int anchorIndex) {
        Vector3 anchorPos = handles[anchorIndex];
        Vector3 dir = Vector3.zero;
        float[] polarHandlesDist = new float[2];

        if (anchorIndex - 3 >= 0 || isLoop) {
            Vector3 offset = handles[LoopHandle(anchorIndex - 3)] - anchorPos;
            dir += offset.normalized;
            polarHandlesDist[0] = offset.magnitude;
        }
        if (anchorIndex + 3 < handles.Count || isLoop) {
            Vector3 offset = handles[LoopHandle(anchorIndex + 3)] - anchorPos;
            dir -= offset.normalized;
            polarHandlesDist[1] = -offset.magnitude;
        }

        dir.Normalize();
        for (int i = 0; i < 2; i++) {
            int handleIndex = anchorIndex + i * 2 - 1;
            if (handleIndex >= 0 && handleIndex < handles.Count || isLoop) {
                handles[LoopHandle(handleIndex)] = anchorPos + dir * polarHandlesDist[i] * .5f;
            }
        }

        UpdateSegmentPoints(LoopSegment(anchorIndex / 3));
        UpdateSegmentPoints(LoopSegment(anchorIndex / 3 - 1));
    }

    private void UpdateAllSegmentsPoints() {
        for (int i = 0; i < NumSegments; i++) {
            UpdateSegmentPoints(i);
        }
    }

    private void UpdateSegmentPoints(int i) {
        List<Vector3> handles = GetSegmentHandles(i);
        for (int j = 0; j < pointsBySegment; j++) {
            float t = (1f / (pointsBySegment + 1)) * (j + 1);
            segmentPoints[(i * pointsBySegment) + j] = MathUtility.BezierPoint(handles, t);
        }
    }

    private int LoopHandle(int i) {
        return (i + handles.Count) % handles.Count;
    }
    private int LoopSegment(int i) {
        return (i + NumSegments) % NumSegments;
    }
}
