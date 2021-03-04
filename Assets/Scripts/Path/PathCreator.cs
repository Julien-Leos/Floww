using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PathCreator : MonoBehaviour {
    [HideInInspector]
    public Path path;

    public void CreatePath() {
        path = new Path(transform.position);
    }
}
