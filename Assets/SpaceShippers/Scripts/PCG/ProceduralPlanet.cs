using UnityEngine;

public class ProceduralPlanet : ProceduralSphere
{
    private struct GPUVertexData
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }

    [Header("GPU Compute Configuration")] public ComputeShader planetComputeShader;

    [Header("Pre-Baked Model Cache")]
    [Tooltip("Drag a PlanetDataCache asset here to read/write raw structural memory arrays directly to disk.")]
    public PlanetDataCache dataCache;

    [Header("Planet Noise Structure")] [Range(1, 8)]
    public int octaves = 4;

    public float baseNoiseScale = 1.5f;
    public float maxNoiseStrength = 0.5f;
    public float noiseOffset = 0.0f;

    [Header("Fractal Modifiers")] [Range(1f, 4f)]
    public float lacunarity = 2.0f;

    [Range(0f, 1f)] public float persistence = 0.5f;

    [Header("Terrain Filters")] public bool flattenValleys = true;

    [Header("Procedural Texture Colors")] public Color oceanColor = new Color(0.05f, 0.2f, 0.6f);
    public Color sandColor = new Color(0.85f, 0.8f, 0.5f);
    public Color grassColor = new Color(0.2f, 0.6f, 0.2f);
    public Color rockColor = new Color(0.4f, 0.35f, 0.3f);
    public Color snowColor = new Color(0.95f, 0.95f, 0.95f);

    [Header("Texture Resolution")] public int textureSize = 512;

    [Header("Normal Map Details")] [Range(0.1f, 10f)]
    public float normalBumpStrength = 2.0f;

    private Texture2D generatedTexture;
    private Texture2D generatedNormalMap;

    public Texture2D GetGeneratedTexture() => generatedTexture;
    public Texture2D GetGeneratedNormalMap() => generatedNormalMap;

    public override Mesh GenerateSphereMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        sphereCollider = GetComponent<SphereCollider>();

        Mesh mesh = new Mesh();
        mesh.name = "GPU_ProceduralPlanet";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int vertexCount = (longitudeSegments + 1) * (latitudeSegments + 1);
        Vector3[] vertices;
        Vector2[] uvs;
        int[] triangles;

        if (dataCache != null && dataCache.HasData)
        {
            vertices = dataCache.cachedVertices;
            uvs = dataCache.cachedUVs;
            triangles = dataCache.cachedTriangles;
        }
        else
        {
            if (planetComputeShader == null) return base.GenerateSphereMesh();

            int numFaces = longitudeSegments * latitudeSegments;
            int triangleCount = numFaces * 6;

            GPUVertexData[] gpuOutputArray = new GPUVertexData[vertexCount];
            ComputeBuffer vertexBuffer = new ComputeBuffer(vertexCount, 32);
            vertexBuffer.SetData(gpuOutputArray);

            int vertKernel = planetComputeShader.FindKernel("CS_Vertices");
            planetComputeShader.SetBuffer(vertKernel, "vertexBuffer", vertexBuffer);
            planetComputeShader.SetFloat("radius", radius);
            planetComputeShader.SetInt("longitudeSegments", longitudeSegments);
            planetComputeShader.SetInt("latitudeSegments", latitudeSegments);
            planetComputeShader.SetFloat("noiseScale", baseNoiseScale);
            planetComputeShader.SetFloat("noiseStrength", maxNoiseStrength);
            planetComputeShader.SetFloat("noiseOffset", noiseOffset);
            planetComputeShader.SetInt("octaves", octaves);
            planetComputeShader.SetFloat("lacunarity", lacunarity);
            planetComputeShader.SetFloat("persistence", persistence);
            planetComputeShader.SetInt("flattenValleys", flattenValleys ? 1 : 0);

            int vertGroups = Mathf.CeilToInt(vertexCount / 256f);
            planetComputeShader.Dispatch(vertKernel, vertGroups, 1, 1);
            vertexBuffer.GetData(gpuOutputArray);

            triangles = new int[triangleCount];
            ComputeBuffer triBuffer = new ComputeBuffer(triangleCount, sizeof(int));
            triBuffer.SetData(triangles);

            int triKernel = planetComputeShader.FindKernel("CS_Triangles");
            planetComputeShader.SetBuffer(triKernel, "triangleBuffer", triBuffer);
            planetComputeShader.SetInt("longitudeSegments", longitudeSegments);
            planetComputeShader.SetInt("latitudeSegments", latitudeSegments);

            int triGroups = Mathf.CeilToInt(numFaces / 256f);
            planetComputeShader.Dispatch(triKernel, triGroups, 1, 1);
            triBuffer.GetData(triangles);

            vertexBuffer.Release();
            triBuffer.Release();

            vertices = new Vector3[vertexCount];
            uvs = new Vector2[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = gpuOutputArray[i].position;
                uvs[i] = gpuOutputArray[i].uv;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        meshFilter.sharedMesh = mesh;
        UpdateCollider();

        GenerateProceduralTextures();

        return mesh;
    }

    private void GenerateProceduralTextures()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        if (generatedTexture == null || generatedTexture.width != textureSize)
        {
            generatedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
            generatedTexture.wrapMode = TextureWrapMode.Clamp;
            generatedTexture.filterMode = FilterMode.Bilinear;

            generatedNormalMap = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
            generatedNormalMap.wrapMode = TextureWrapMode.Clamp;
            generatedNormalMap.filterMode = FilterMode.Bilinear;
        }

        Color[] colorPixels = new Color[textureSize * textureSize];
        Color[] normalPixels = new Color[textureSize * textureSize];
        float texelSize = 1f / textureSize;

        for (int y = 0; y < textureSize; y++)
        {
            float v = (float)y / textureSize;
            float latAngle = Mathf.PI * v;
            float sinLat = Mathf.Sin(latAngle);
            float cosLat = Mathf.Cos(latAngle);

            for (int x = 0; x < textureSize; x++)
            {
                float u = (float)x / textureSize;
                float lonAngle = 2f * Mathf.PI * u;

                Vector3 normal = new Vector3(Mathf.Cos(lonAngle) * sinLat, cosLat, Mathf.Sin(lonAngle) * sinLat);

                float heightPercent = GetNoiseElevationPercent(normal);
                colorPixels[y * textureSize + x] = EvaluateColorBands(heightPercent);

                float uOffset = u + texelSize;
                float vOffset = v + texelSize;

                Vector3 normalR = new Vector3(Mathf.Cos(2f * Mathf.PI * uOffset) * sinLat, cosLat,
                    Mathf.Sin(2f * Mathf.PI * uOffset) * sinLat);
                Vector3 normalU = new Vector3(Mathf.Cos(lonAngle) * Mathf.Sin(Mathf.PI * vOffset),
                    Mathf.Cos(Mathf.PI * vOffset), Mathf.Sin(lonAngle) * Mathf.PI * vOffset);

                float heightR = GetNoiseElevationPercent(normalR);
                float heightU = GetNoiseElevationPercent(normalU);

                float dX = (heightR - heightPercent) * normalBumpStrength;
                float dY = (heightU - heightPercent) * normalBumpStrength;

                Vector3 tangentNormal = new Vector3(-dX, -dY, 1.0f).normalized;

                float r = (tangentNormal.x * 0.5f) + 0.5f;
                float g = (tangentNormal.y * 0.5f) + 0.5f;
                float b = (tangentNormal.z * 0.5f) + 0.5f;

                normalPixels[y * textureSize + x] = new Color(r, g, b, 1.0f);
            }
        }

        generatedTexture.SetPixels(colorPixels);
        generatedTexture.Apply();

        generatedNormalMap.SetPixels(normalPixels);
        generatedNormalMap.Apply();

        if (meshRenderer.sharedMaterial != null)
        {
            bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            string albedoProp = isURP ? "_BaseMap" : "_MainTex";
            string normalProp = isURP ? "_BumpMap" : "_BumpMap";

            meshRenderer.sharedMaterial.SetTexture(albedoProp, generatedTexture);
            meshRenderer.sharedMaterial.SetTexture(normalProp, generatedNormalMap);
            meshRenderer.sharedMaterial.EnableKeyword("_NORMALMAP");
        }
    }

    private float GetNoiseElevationPercent(Vector3 normal)
    {
        float totalNoise = 0f;
        float amplitude = 1f;
        float frequency = baseNoiseScale;
        float maxVal = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (normal.x + noiseOffset) * frequency;
            float sampleY = (normal.y + noiseOffset) * frequency;
            float sampleZ = (normal.z + noiseOffset) * frequency;

            float noiseXY = Mathf.PerlinNoise(sampleX, sampleY);
            float noiseYZ = Mathf.PerlinNoise(sampleY, sampleZ);
            float noiseZX = Mathf.PerlinNoise(sampleZ, sampleX);

            float layerNoise = (noiseXY + noiseYZ + noiseZX) / 3.0f - 0.5f;
            totalNoise += layerNoise * amplitude;
            maxVal += 0.5f * amplitude;

            frequency *= lacunarity;
            amplitude *= persistence;
        }

        if (maxVal > 0) totalNoise = (totalNoise / maxVal) * 0.5f;
        if (flattenValleys && totalNoise < 0) totalNoise = 0;

        return Mathf.Clamp01(maxVal > 0 ? (totalNoise * maxNoiseStrength) / maxNoiseStrength : 0f);
    }

    private Color EvaluateColorBands(float elevationPercent)
    {
        if (elevationPercent < 0.05f) return oceanColor;
        if (elevationPercent < 0.12f) return Color.Lerp(sandColor, grassColor, (elevationPercent - 0.05f) / 0.07f);
        if (elevationPercent < 0.5f) return Color.Lerp(grassColor, rockColor, (elevationPercent - 0.12f) / 0.38f);

        return Color.Lerp(rockColor, snowColor, (elevationPercent - 0.5f) / 0.5f);
    }

    protected override void UpdateCollider()
    {
        if (sphereCollider != null)
        {
            sphereCollider.center = Vector3.zero;
            sphereCollider.radius = radius + (maxNoiseStrength * 0.5f);
            sphereCollider.includeLayers = Layers;
        }
    }
}


