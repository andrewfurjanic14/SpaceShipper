using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(SphereCollider))]
public class ProceduralSphere : MonoBehaviour
{
    [Header("Physics Settings")]
    public LayerMask Layers; 
    
    [Header("Sphere Settings")]
    public float radius = 1.0f;
    
    [Range(4, 512)]
    public int longitudeSegments = 24; 
    
    [Range(4, 512)]
    public int latitudeSegments = 16;  

    [Header("Material Settings")]
    public Texture2D sphereTexture;
    public Material sphereMaterial;

    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;
    protected SphereCollider sphereCollider; 
    private Mesh mesh;

    // --- Made virtual so subclasses can handle placement initialization ---
    protected virtual void Reset()
    {
        SetupMaterial();
        GenerateSphereMesh();
        Debug.Log("Procedural Sphere generated automatically on placement.");
    }

    protected virtual void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        sphereCollider = GetComponent<SphereCollider>();

        SetupMaterial();

        if (meshFilter.sharedMesh == null)
        {
            GenerateSphereMesh();
        }
    }

    protected virtual void OnValidate()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        
        if (!Application.isPlaying)
        {
            GenerateSphereMesh();
        }
        else if (mesh != null)
        {
            GenerateSphereMesh();
        }
    }

    private void SetupMaterial()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        // If an asset file is already assigned, stick with it!
        if (sphereMaterial != null)
        {
            meshRenderer.sharedMaterial = sphereMaterial;
            return;
        }

        // Live preview safety check: prevents planets from sharing properties in the scene layout
        if (!Application.isPlaying && meshRenderer.sharedMaterial != null && meshRenderer.sharedMaterial.name.Contains("(Clone)"))
        {
            return; 
        }

        string shaderName = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null 
            ? "Universal Render Pipeline/Lit" 
            : "Standard";

        Shader defaultShader = Shader.Find(shaderName);
        if (defaultShader != null)
        {
            // Instantiates an temporary isolated variant for this specific planet instance to tweak color sliders
            Material instanceMat = new Material(defaultShader);
            instanceMat.name = gameObject.name + "_PreviewMaterial (Clone)";
        
            meshRenderer.sharedMaterial = instanceMat;
        }
    }


    // --- Made Virtual: Subclasses can extend the calculation or assignment pipeline ---
    public virtual Mesh GenerateSphereMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        sphereCollider = GetComponent<SphereCollider>();

        mesh = new Mesh();
        mesh.name = "ProceduralSphere";
        
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

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
                
                // --- HOOK FOR SUBCLASSES: Calculates position using a dedicated virtual function ---
                vertices[index] = CalculateVertexPosition(normal); 
                
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

                triangles[triIndex++] = current;
                triangles[triIndex++] = next;          
                triangles[triIndex++] = bottomCurrent;  

                triangles[triIndex++] = next;
                triangles[triIndex++] = bottomNext;     
                triangles[triIndex++] = bottomCurrent;  
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        // Recalculate physical characteristics because noise deforms raw normals
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        meshFilter.sharedMesh = mesh; 

        UpdateCollider();

        return mesh;
    }

    // --- Virtual Helper: Ensures sub-classes can modify collider shapes to fit peaks/valleys ---
    protected virtual void UpdateCollider()
    {
        if (sphereCollider != null)
        {
            sphereCollider.center = Vector3.zero; 
            sphereCollider.radius = radius;       
            sphereCollider.includeLayers = Layers;
        }
    }

    // --- Virtual Helper: Dictates position math based on the base normal orientation ---
    protected virtual Vector3 CalculateVertexPosition(Vector3 normal)
    {
        return normal * radius;
    }
}
