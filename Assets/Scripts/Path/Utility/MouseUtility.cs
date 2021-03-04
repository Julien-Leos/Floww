using UnityEngine;
using UnityEditor;

public static class MouseUtility {
    public static bool GetMouseWorldPosition(out RaycastHit hit) {
        Vector3 mousePosition = Event.current.mousePosition;
        mousePosition.y = Camera.current.pixelHeight - mousePosition.y * EditorGUIUtility.pixelsPerPoint;
        mousePosition.x *= EditorGUIUtility.pixelsPerPoint;

        return Physics.Raycast(Camera.current.ScreenPointToRay(mousePosition), out hit);
    }

}
