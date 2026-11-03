using UnityEngine;

[CreateAssetMenu(fileName = "TrajectoryStyle", menuName = "Scriptable Objects/TrajectoryStyle")]
public class TrajectoryStyle : ScriptableObject
{
    public Material Material;
    public float Length = 100f;
    public float Width = 0.1f;
}
