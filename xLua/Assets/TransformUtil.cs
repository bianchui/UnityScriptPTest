using UnityEngine;

public static class TransformUtil {
    public static void setPositionGO(GameObject gameObject, float x, float y, float z) {
        gameObject.transform.position = new Vector3(x, y, z);
    }
    public static void setPositionT(Transform transform, float x, float y, float z) {
        transform.position = new Vector3(x, y, z);
    }
}

