using System.Collections.Generic;
using UnityEngine;

public class MultiEnemyTargetingHUD : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject targetingBoxPrefab; // A UI Panel/Image prefab for the bounding box
    public Transform uiCanvasTransform;   // The Canvas transform where boxes will be spawned
    public LayerMask enemyLayer;

    [Header("Settings")]
    [Range(0.05f, 0.5f)]
    public float centerZoneRatio = 0.15f; 
    public float maxCheckDistance = 100f;

    // Pool or active list of spawned UI boxes
    private List<RectTransform> activeBoxes = new List<RectTransform>();
    private Queue<RectTransform> boxPool = new Queue<RectTransform>();

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        UpdateTargetingBoxes();
    }

    void UpdateTargetingBoxes()
    {
        // 1. Return all active boxes to the pool
        foreach (var box in activeBoxes)
        {
            box.gameObject.SetActive(false);
            boxPool.Enqueue(box);
        }
        activeBoxes.Clear();

        // 2. Find all enemies in range
        Collider[] colliders = Physics.OverlapSphere(mainCamera.transform.position, maxCheckDistance, enemyLayer);

        foreach (Collider col in colliders)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(col.transform.position);

            // Check if enemy is in front of the camera
            if (screenPoint.z > 0)
            {
                float normX = (screenPoint.x / Screen.width) - 0.5f;
                float normY = (screenPoint.y / Screen.height) - 0.5f;

                // Check if the enemy is within the center 30% zone
                if (Mathf.Abs(normX) <= centerZoneRatio && Mathf.Abs(normY) <= centerZoneRatio)
                {
                    // Get a box from the pool or instantiate a new one if empty
                    RectTransform box = GetOrCreateBox();
                    
                    box.gameObject.SetActive(true);
                    box.position = screenPoint;

                    activeBoxes.Add(box);
                }
            }
        }
    }

    RectTransform GetOrCreateBox()
    {
        if (boxPool.Count > 0)
        {
            return boxPool.Dequeue();
        }
        else
        {
            // Instantiate a new UI box if the pool is empty
            GameObject newBox = Instantiate(targetingBoxPrefab, uiCanvasTransform);
            return newBox.GetComponent<RectTransform>();
        }
    }
}