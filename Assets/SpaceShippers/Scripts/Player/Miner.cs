using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Inventory))]
public class Miner : MonoBehaviour, IAimSource
{
    [Header("Transforms")]
    [SerializeField] private Transform _ship;
    [SerializeField] private Transform _muzzle;
    private Transform[] _muzzles;
    private Inventory _inventory;

    [Header("Optional: Trajectory Visualization")]
    [SerializeField] private TrajectoryPreview _trajectoryPreview;
    [SerializeField] private TrajectoryStyle _trajectoryStyle;

    [Header("Miner")]
    [SerializeField] private float _range = 3f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _interval = 0.2f;
    [SerializeField] private LayerMask _mask = ~0;

    // Runtime
    public bool IsMining { get; private set; }
    private float _nextTick;

    // IAimSource Accessors
    public Transform Ship => _ship;
    public Transform[] Muzzles => _muzzles;
    public Quaternion GetAimRotation(Transform muzzle) => muzzle.rotation;


    private void Awake()
    {
        _inventory = GetComponent<Inventory>();

        if (_ship == null)
            _ship = transform.parent != null ? transform.parent : transform;

        if (_muzzle == null) 
            _muzzle = transform;
        _muzzles = new[] { _muzzle };

        if (_trajectoryPreview == null)
            _trajectoryPreview = GetComponent<TrajectoryPreview>();
        
        if (_trajectoryPreview != null)
        {
            _trajectoryStyle.Length = _range; 
            _trajectoryPreview.Register(this, _trajectoryStyle);
        }
    } 

    private void Update()
    {
        if (!IsMining || Time.time < _nextTick) return;

        _nextTick = Time.time + _interval;
        Mine();
    }

    public void StartMining()
    {
        IsMining = true;
        if (_trajectoryPreview != null)
            _trajectoryPreview.SetVisible(this, true);
    }

    public void StopMining()
    {
        IsMining = false;
        if (_trajectoryPreview != null)
            _trajectoryPreview.SetVisible(this, false);
    }

    private void Mine()
    {
        if (Physics.Raycast(_muzzle.position, _muzzle.forward, out RaycastHit hit,
                            _range, _mask, QueryTriggerInteraction.Ignore))
        {
            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node)
                node.YieldHarvest(_damage, _inventory);
        }
    }
}
