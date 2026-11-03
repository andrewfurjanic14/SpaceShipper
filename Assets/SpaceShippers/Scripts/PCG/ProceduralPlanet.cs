using UnityEngine;

namespace SpaceShippers.Scripts.PCG
{
    public class ProceduralPlanet : ProceduralSphere
    {
        private static readonly int LongitudeSegments = Shader.PropertyToID("longitudeSegments");
        private static readonly int VertexBuffer = Shader.PropertyToID("vertexBuffer");
        private static readonly int Radius = Shader.PropertyToID("radius");
        private static readonly int LatitudeSegments = Shader.PropertyToID("latitudeSegments");
        private static readonly int NoiseScale = Shader.PropertyToID("noiseScale");
        private static readonly int NoiseStrength = Shader.PropertyToID("noiseStrength");
        private static readonly int NoiseOffset = Shader.PropertyToID("noiseOffset");
        private static readonly int Octaves = Shader.PropertyToID("octaves");
        private static readonly int Lacunarity = Shader.PropertyToID("lacunarity");
        private static readonly int Persistence = Shader.PropertyToID("persistence");
        private static readonly int FlattenValleys = Shader.PropertyToID("flattenValleys");
        private static readonly int TriangleBuffer = Shader.PropertyToID("triangleBuffer");

        private struct GPUVertexData
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 UV;
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

            if (dataCache && dataCache.HasData)
            {
                vertices = dataCache.cachedVertices;
                uvs = dataCache.cachedUVs;
                triangles = dataCache.cachedTriangles;
            }
            else
            {
                if (planetComputeShader) return base.GenerateSphereMesh();

                int numFaces = longitudeSegments * latitudeSegments;
                int triangleCount = numFaces * 6;

                GPUVertexData[] gpuOutputArray = new GPUVertexData[vertexCount];
                ComputeBuffer vertexBuffer = new ComputeBuffer(vertexCount, 32);
                vertexBuffer.SetData(gpuOutputArray);

                int vertKernel = planetComputeShader.FindKernel("CS_Vertices");
                planetComputeShader.SetBuffer(vertKernel, VertexBuffer, vertexBuffer);
                planetComputeShader.SetFloat(Radius, radius);
                planetComputeShader.SetInt(LongitudeSegments, longitudeSegments);
                planetComputeShader.SetInt(LatitudeSegments, latitudeSegments);
                planetComputeShader.SetFloat(NoiseScale, baseNoiseScale);
                planetComputeShader.SetFloat(NoiseStrength, maxNoiseStrength);
                planetComputeShader.SetFloat(NoiseOffset, noiseOffset);
                planetComputeShader.SetInt(Octaves, octaves);
                planetComputeShader.SetFloat(Lacunarity, lacunarity);
                planetComputeShader.SetFloat(Persistence, persistence);
                planetComputeShader.SetInt(FlattenValleys, flattenValleys ? 1 : 0);

                var vertGroups = Mathf.CeilToInt(vertexCount / 256f);
                planetComputeShader.Dispatch(vertKernel, vertGroups, 1, 1);
                vertexBuffer.GetData(gpuOutputArray);

                triangles = new int[triangleCount];
                var triBuffer = new ComputeBuffer(triangleCount, sizeof(int));
                triBuffer.SetData(triangles);

                var triKernel = planetComputeShader.FindKernel("CS_Triangles");
                planetComputeShader.SetBuffer(triKernel, TriangleBuffer, triBuffer);
                planetComputeShader.SetInt(LongitudeSegments, longitudeSegments);
                planetComputeShader.SetInt(LatitudeSegments, latitudeSegments);

                var triGroups = Mathf.CeilToInt(numFaces / 256f);
                planetComputeShader.Dispatch(triKernel, triGroups, 1, 1);
                triBuffer.GetData(triangles);

                vertexBuffer.Release();
                triBuffer.Release();

                vertices = new Vector3[vertexCount];
                uvs = new Vector2[vertexCount];

                for (var i = 0; i < vertexCount; i++)
                {
                    vertices[i] = gpuOutputArray[i].Position;
                    uvs[i] = gpuOutputArray[i].UV;
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
            if (meshRenderer) meshRenderer = GetComponent<MeshRenderer>();

            if (generatedTexture || generatedTexture.width != textureSize)
            {
                generatedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
                generatedTexture.wrapMode = TextureWrapMode.Clamp;
                generatedTexture.filterMode = FilterMode.Bilinear;

                generatedNormalMap = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
                generatedNormalMap.wrapMode = TextureWrapMode.Clamp;
                generatedNormalMap.filterMode = FilterMode.Bilinear;
            }

            var colorPixels = new Color[textureSize * textureSize];
            var normalPixels = new Color[textureSize * textureSize];
            var texelSize = 1f / textureSize;

            for (var y = 0; y < textureSize; y++)
            {
                var v = (float)y / textureSize;
                var latAngle = Mathf.PI * v;
                var sinLat = Mathf.Sin(latAngle);
                var cosLat = Mathf.Cos(latAngle);

                for (var x = 0; x < textureSize; x++)
                {
                    var u = (float)x / textureSize;
                    var lonAngle = 2f * Mathf.PI * u;

                    var normal = new Vector3(Mathf.Cos(lonAngle) * sinLat, cosLat, Mathf.Sin(lonAngle) * sinLat);

                    var heightPercent = GetNoiseElevationPercent(normal);
                    colorPixels[y * textureSize + x] = EvaluateColorBands(heightPercent);

                    var uOffset = u + texelSize;
                    var vOffset = v + texelSize;

                    var normalR = new Vector3(Mathf.Cos(2f * Mathf.PI * uOffset) * sinLat, cosLat,
                        Mathf.Sin(2f * Mathf.PI * uOffset) * sinLat);
                    var normalU = new Vector3(Mathf.Cos(lonAngle) * Mathf.Sin(Mathf.PI * vOffset),
                        Mathf.Cos(Mathf.PI * vOffset), Mathf.Sin(lonAngle) * Mathf.PI * vOffset);

                    var heightR = GetNoiseElevationPercent(normalR);
                    var heightU = GetNoiseElevationPercent(normalU);

                    var dX = (heightR - heightPercent) * normalBumpStrength;
                    var dY = (heightU - heightPercent) * normalBumpStrength;

                    var tangentNormal = new Vector3(-dX, -dY, 1.0f).normalized;

                    var r = (tangentNormal.x * 0.5f) + 0.5f;
                    var g = (tangentNormal.y * 0.5f) + 0.5f;
                    var b = (tangentNormal.z * 0.5f) + 0.5f;

                    normalPixels[y * textureSize + x] = new Color(r, g, b, 1.0f);
                }
            }

            generatedTexture.SetPixels(colorPixels);
            generatedTexture.Apply();

            generatedNormalMap.SetPixels(normalPixels);
            generatedNormalMap.Apply();

            if (meshRenderer.sharedMaterial)
            {
                var isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
                var albedoProp = isURP ? "_BaseMap" : "_MainTex";
                var normalProp = isURP ? "_BumpMap" : "_BumpMap";

                meshRenderer.sharedMaterial.SetTexture(albedoProp, generatedTexture);
                meshRenderer.sharedMaterial.SetTexture(normalProp, generatedNormalMap);
                meshRenderer.sharedMaterial.EnableKeyword("_NORMALMAP");
            }
        }

        private float GetNoiseElevationPercent(Vector3 normal)
        {
            var totalNoise = 0f;
            var amplitude = 1f;
            var frequency = baseNoiseScale;
            var maxVal = 0f;

            for (var i = 0; i < octaves; i++)
            {
                var sampleX = (normal.x + noiseOffset) * frequency;
                var sampleY = (normal.y + noiseOffset) * frequency;
                var sampleZ = (normal.z + noiseOffset) * frequency;

                var noiseXY = Mathf.PerlinNoise(sampleX, sampleY);
                var noiseYZ = Mathf.PerlinNoise(sampleY, sampleZ);
                var noiseZX = Mathf.PerlinNoise(sampleZ, sampleX);

                var layerNoise = (noiseXY + noiseYZ + noiseZX) / 3.0f - 0.5f;
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
            if (sphereCollider)
            {
                sphereCollider.center = Vector3.zero;
                sphereCollider.radius = radius + (maxNoiseStrength * 0.5f);
                sphereCollider.includeLayers = Layers;
            }
        }
    }
}


