using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class Miner : MonoBehaviour, IAimSource
{
    // Components
    [SerializeField] private TrajectoryPreview _trajectoryPreview;
    [SerializeField] private Transform _ship;
    [SerializeField] private Transform _muzzle;
    private Inventory _inventory;
    private Transform[] _muzzles;

    // Settings
    [SerializeField] private float _range = 3f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _interval = 0.2f;
    [SerializeField] private LayerMask _mask = ~0;

    // Status
    private bool _isMining;
    private float _nextTick;

    // Accessors
    public Transform Ship => _ship;
    public Transform[] Muzzles => _muzzles;
    public Quaternion GetAimRotation(Transform muzzle) => muzzle.rotation;


    private void Awake()
    {
        _inventory = GetComponentInParent<Inventory>();

        if (_ship == null)
            _ship = transform.parent != null ? transform.parent : transform;

        if (_muzzle == null) 
            _muzzle = transform;
        _muzzles = new[] { _muzzle };

        if (_trajectoryPreview == null)
            _trajectoryPreview = GetComponent<TrajectoryPreview>();
    } 

    private void Update()
    {
        if (!_isMining || Time.time < _nextTick) return;

        _nextTick = Time.time + _interval;
        Mine();
    }
    private void OnEnable()
    {
        ResourceNode.Harvested += OnHarvested;
    }

    private void OnDisable()
    {
        ResourceNode.Harvested -= OnHarvested;
        StopMining();
    }

    public void StartMining()
    {
        _isMining = true;
        if (_trajectoryPreview != null)
            _trajectoryPreview.enabled = true;
    }

    public void StopMining()
    {
        _isMining = false;
        if (_trajectoryPreview != null)
            _trajectoryPreview.enabled = false;
    }

    private void Mine()
    {
        if (Physics.Raycast(_muzzle.position, _muzzle.forward, out RaycastHit hit,
                            _range, _mask, QueryTriggerInteraction.Ignore))
        {
            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node == null) return;
            node.GetComponent<Health>().TakeDamage(_damage);
        }
    }

    private void OnHarvested(Item item, int amount)
    {
        _inventory.Add(item, amount);
    }
}
