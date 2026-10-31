using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class Miner : MonoBehaviour
{
    [SerializeField] private float _range = 3f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _interval = 0.2f;
    [SerializeField] private LayerMask _mask = ~0;

    [SerializeField] private Transform _muzzle;
    [SerializeField] private TrajectoryPreview _trajectoryPreview;

    private Inventory _inventory;
    private bool _isMining;
    private float _nextTick;


    void Awake()
    {
        _inventory = GetComponent<Inventory>();

        if (_muzzle == null)
            _muzzle = transform;

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

    public void Mine()
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
