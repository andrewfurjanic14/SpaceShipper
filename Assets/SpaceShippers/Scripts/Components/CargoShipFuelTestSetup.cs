using UnityEngine;

[RequireComponent(typeof(CargoShipMovement))]
public class CargoShipFuelTestSetup : MonoBehaviour
{
    [SerializeField, Min(0)] private int _startingFuel = 2;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("The cargo ship fuel setup requires a Player-tagged object.", this);
            return;
        }

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("The cargo ship fuel setup requires an Inventory component on the player.", player);
            return;
        }

        Item fuel = Resources.Load<Item>("Fuel");
        if (fuel == null)
        {
            Debug.LogError("The cargo ship fuel setup could not load the Fuel item from Resources.", this);
            return;
        }
        inventory.Add(fuel, _startingFuel);
        Debug.Log($"Cargo ship ready with {_startingFuel} fuel. Hold G and release while aiming at a planet to launch.", this);
    }
}
