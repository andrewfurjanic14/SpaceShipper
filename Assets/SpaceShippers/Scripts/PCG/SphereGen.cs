using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(SphereCollider))]
public class SphereGen : MonoBehaviour
{
    [Header("Physics Settings")]
    public LayerMask Layers; // Added LayerMask for SphereCollider
    
    [Header("Sphere Settings")]
    public float radius = 1.0f;
    
    [Range(4, 64)]
    public int longitudeSegments = 24; 
    
    [Range(4, 64)]
    public int latitudeSegments = 16;  

    [Header("Material Settings")]
    public Texture2D sphereTexture;
    public Material sphereMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private SphereCollider sphereCollider; // Changed from MeshCollider
    private Mesh mesh;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        // 1. Grab the required SphereCollider component
        sphereCollider = GetComponent<SphereCollider>();

        // 2. Fix the pink material issue
        SetupMaterial();

        // 3. Generate geometry and apply physics
        GenerateSphereMesh();
    }

    void OnValidate()
    {
        if (Application.isPlaying && mesh != null)
        {
            GenerateSphereMesh();
        }
    }

    private void SetupMaterial()
    {
        if (sphereMaterial != null)
        {
            meshRenderer.material = sphereMaterial;
            return;
        }

        string shaderName = "Standard"; 
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
        {
            shaderName = "Universal Render Pipeline/Lit"; 
        }

        Shader defaultShader = Shader.Find(shaderName);
        
        if (defaultShader != null)
        {
            sphereMaterial = new Material(defaultShader);
            if (sphereTexture != null)
            {
                sphereMaterial.mainTexture = sphereTexture;
            }
            meshRenderer.material = sphereMaterial;
        }
    }

    public void GenerateSphereMesh()
    {
        mesh = new Mesh();
        mesh.name = "ProceduralSphere";

        int vertexCount = (longitudeSegments + 1) * (latitudeSegments + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        int index = 0;
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float latAngle = Mathf.PI * lat / latitudeSegments;
            float sinLat = Mathf.Sin(latAngle);
            float cosLat = Mathf.Cos(latAngle);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float lonAngle = 2f * Mathf.PI * lon / longitudeSegments;
                float sinLon = Mathf.Sin(lonAngle);
                float cosLon = Mathf.Cos(lonAngle);

                Vector3 normal = new Vector3(cosLon * sinLat, cosLat, sinLon * sinLat);
                
                normals[index] = normal;
                vertices[index] = normal * radius; 
                uvs[index] = new Vector2((float)lon / longitudeSegments, (float)lat / latitudeSegments);
                
                index++;
            }
        }

        int numFaces = longitudeSegments * latitudeSegments;
        int numTriangles = numFaces * 2;
        int[] triangles = new int[numTriangles * 3];

        var triIndex = 0;
        for (var lat = 0; lat < latitudeSegments; lat++)
        {
            for (var lon = 0; lon < longitudeSegments; lon++)
            {
                var current = lat * (longitudeSegments + 1) + lon;
                var next = current + 1;
                var bottomCurrent = current + (longitudeSegments + 1);
                var bottomNext = bottomCurrent + 1;

                // --- FIXED: Swapped the winding order to face outward ---
                // First Triangle
                triangles[triIndex++] = current;
                triangles[triIndex++] = next;          // Swapped with bottomCurrent
                triangles[triIndex++] = bottomCurrent;  // Swapped with next

                // Second Triangle
                triangles[triIndex++] = next;
                triangles[triIndex++] = bottomNext;     // Swapped with bottomCurrent
                triangles[triIndex++] = bottomCurrent;  // Swapped with bottomNext
            }
        }


        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        meshFilter.mesh = mesh;

        // 4. Update the standard Sphere Collider to match the mesh radius
        if (sphereCollider != null)
        {
            sphereCollider.center = Vector3.zero; // Keeps it perfectly centered
            sphereCollider.radius = radius;       // Automatically scales with your mesh variable
            sphereCollider.includeLayers = Layers;
        }
    }
}
