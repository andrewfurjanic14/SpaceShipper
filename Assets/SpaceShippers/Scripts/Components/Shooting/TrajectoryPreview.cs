using UnityEngine;
public class TrajectoryPreview : MonoBehaviour
{
    [SerializeField] private Material _lineMaterial;
    [SerializeField] private float _length = 100f;
    [SerializeField] private float _width = 0.03f;

    private IAimSource _aimSource;
    private LineRenderer[] _lines;

    void Awake()
    {
        _aimSource = GetComponent<IAimSource>();
        CreateLines();
        enabled = false;
    }

    private void OnEnable() => SetLinesEnabled(true);
    private void OnDisable() => SetLinesEnabled(false);

    public void Update()
    {
        Transform ship = _aimSource.Ship;
        Transform[] muzzles = _aimSource.Muzzles;

        for (int i = 0; i < _lines.Length; i++)
        {
            if (muzzles[i] == null || _lines[i] == null) continue;

            Quaternion rotation = _aimSource.GetAimRotation(muzzles[i]);
            Vector3 start = ship.InverseTransformPoint(muzzles[i].position);
            Vector3 dir = ship.InverseTransformDirection(rotation * Vector3.forward);

            _lines[i].SetPosition(0, start);
            _lines[i].SetPosition(1, start + dir * _length);
        }
    }

    private void CreateLines()
    {
        Transform[] muzzles = _aimSource.Muzzles;
        _lines = new LineRenderer[muzzles.Length];

        for (int i = 0; i < muzzles.Length; i++)
        {
            if (muzzles[i] == null) continue;

            var lineObject = new GameObject($"Trajectory Preview {i + 1}");
            lineObject.transform.SetParent(_aimSource.Ship, false);

            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = line.endWidth = _width;
            line.sharedMaterial = _lineMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            _lines[i] = line;
        }
    }

    private void SetLinesEnabled(bool value)
    {
        if (_lines == null) return;
        foreach (var line in _lines)
            if (line != null) line.enabled = value;
    }
}
