using UnityEngine;

public interface IAimSource
{
    Transform Ship { get; }
    Transform[] Muzzles { get; }
    Quaternion GetAimRotation(Transform muzzle);
}
