using UnityEngine;
using UnityEditor;
using System.IO;
using SpaceShippers.Scripts.PCG;

[CustomEditor(typeof(ProceduralSphere), true)]
public class ProceduralSphereEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProceduralSphere sphereScript = (ProceduralSphere)target;

        GUILayout.Space(15);

        if (GUILayout.Button("Bake & Save Mesh & Textures Permanent"))
        {
            if (sphereScript is ProceduralPlanet pScript && pScript.dataCache != null)
            {
                pScript.dataCache.SaveData(null, null, null);
            }

            Mesh newMesh = sphereScript.GenerateSphereMesh();

            if (newMesh != null)
            {
                string baseName = sphereScript.gameObject.name;
                
                string meshPath = $"Assets/{baseName}_ProceduralMesh.asset";
                meshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);
                
                string directory = Path.GetDirectoryName(meshPath);
                string uniqueFileNameWithoutExtension = Path.GetFileNameWithoutExtension(meshPath);
                
                string texPath = Path.Combine(directory, uniqueFileNameWithoutExtension + "_Albedo.png").Replace("\\", "/");
                string normPath = Path.Combine(directory, uniqueFileNameWithoutExtension + "_Normal.png").Replace("\\", "/");
                string matPath = Path.Combine(directory, uniqueFileNameWithoutExtension + "_Material.mat").Replace("\\", "/");
                string cachePath = Path.Combine(directory, uniqueFileNameWithoutExtension + "_DataCache.asset").Replace("\\", "/");

                AssetDatabase.CreateAsset(newMesh, meshPath);

                if (sphereScript is ProceduralPlanet planetScript)
                {
                    PlanetDataCache newCache = ScriptableObject.CreateInstance<PlanetDataCache>();
                    newCache.SaveData(newMesh.vertices, newMesh.uv, newMesh.triangles);
                    AssetDatabase.CreateAsset(newCache, cachePath);
                    planetScript.dataCache = newCache;

                    Texture2D runtimeTex = planetScript.GetGeneratedTexture();
                    if (runtimeTex != null)
                    {
                        byte[] pngBytes = runtimeTex.EncodeToPNG();
                        File.WriteAllBytes(Application.dataPath + texPath.Substring(6), pngBytes);
                        AssetDatabase.ImportAsset(texPath);

                        TextureImporter albedoImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
                        if (albedoImporter != null)
                        {
                            albedoImporter.textureCompression = TextureImporterCompression.Uncompressed;
                            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
                        }
                    }

                    Texture2D runtimeNormal = planetScript.GetGeneratedNormalMap();
                    if (runtimeNormal != null)
                    {
                        byte[] pngBytes = runtimeNormal.EncodeToPNG();
                        File.WriteAllBytes(Application.dataPath + normPath.Substring(6), pngBytes);
                        AssetDatabase.ImportAsset(normPath);

                        TextureImporter normalImporter = AssetImporter.GetAtPath(normPath) as TextureImporter;
                        if (normalImporter != null)
                        {
                            normalImporter.textureType = TextureImporterType.NormalMap;
                            normalImporter.wrapMode = TextureWrapMode.Clamp;
                            normalImporter.filterMode = FilterMode.Bilinear;
                            normalImporter.textureCompression = TextureImporterCompression.Uncompressed;
                            AssetDatabase.ImportAsset(normPath, ImportAssetOptions.ForceUpdate);
                        }
                    }

                    Material uniqueMat = null;
                    string shaderName = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null 
                        ? "Universal Render Pipeline/Lit" 
                        : "Standard";
                    Shader targetShader = Shader.Find(shaderName);

                    if (targetShader != null)
                    {
                        uniqueMat = new Material(targetShader);
                        AssetDatabase.CreateAsset(uniqueMat, matPath);
                    }

                    if (uniqueMat != null)
                    {
                        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
                        string albedoProp = isURP ? "_BaseMap" : "_MainTex";
                        string normalProp = isURP ? "_BumpMap" : "_BumpMap";

                        Texture2D bakedAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                        Texture2D bakedNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);

                        uniqueMat.SetTexture(albedoProp, bakedAlbedo);
                        uniqueMat.SetTexture(normalProp, bakedNormal);
                        uniqueMat.EnableKeyword("_NORMALMAP");

                        MeshRenderer renderer = sphereScript.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            renderer.sharedMaterial = uniqueMat;
                        }
                        sphereScript.sphereMaterial = uniqueMat;

                        EditorUtility.SetDirty(uniqueMat);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                MeshFilter filter = sphereScript.GetComponent<MeshFilter>();
                if (filter != null)
                {
                    filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                }

                Debug.Log($"Baked distinct standalone cache configurations! Cache Asset created at: {cachePath}", newMesh);
            }
        }
    }
}