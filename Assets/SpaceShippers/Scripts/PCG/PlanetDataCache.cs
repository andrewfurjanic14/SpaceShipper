using UnityEngine;

[CreateAssetMenu(fileName = "NewPlanetDataCache", menuName = "Procedural/Planet Data Cache")]
public class PlanetDataCache : ScriptableObject
{
    [HideInInspector] public Vector3[] cachedVertices;
    [HideInInspector] public Vector2[] cachedUVs;
    [HideInInspector] public int[] cachedTriangles;

    public bool HasData => cachedVertices != null && cachedVertices.Length > 0;

    public void SaveData(Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        cachedVertices = vertices;
        cachedUVs = uvs;
        cachedTriangles = triangles;
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}