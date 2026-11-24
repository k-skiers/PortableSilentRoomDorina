using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class QuestTerrainVegetationPruner : MonoBehaviour
{
    private const float ScenicRadius = 600f;

    private static readonly Vector2[] ScenicCenters =
    {
        new Vector2(-27.966f, 256.6f),
        new Vector2(-27.1f, 249.01f),
        new Vector2(-751.505981f, 480.959991f),
        new Vector2(-830.1067f, 371.21f),
        new Vector2(-325.5f, 450f)
    };

    private void Awake()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        QualitySettings.terrainQualityOverrides =
            TerrainQualityOverrides.PixelError |
            TerrainQualityOverrides.DetailDensity |
            TerrainQualityOverrides.DetailDistance |
            TerrainQualityOverrides.TreeDistance |
            TerrainQualityOverrides.BillboardStart |
            TerrainQualityOverrides.MaxTrees;

        Terrain[] terrains = FindObjectsOfType<Terrain>(true);
        if (terrains.Length == 0)
        {
            Debug.LogWarning("Quest terrain vegetation pruning found no Terrain components.", this);
            return;
        }

        var protectedData = new Dictionary<TerrainData, bool>();
        foreach (Terrain terrain in terrains)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            TerrainData data = terrain.terrainData;
            bool keepVegetation = IsWithinScenicArea(terrain);
            if (protectedData.TryGetValue(data, out bool alreadyProtected))
            {
                protectedData[data] = alreadyProtected || keepVegetation;
            }
            else
            {
                protectedData.Add(data, keepVegetation);
            }
        }

        var emptyLayers = new Dictionary<Vector2Int, int[,]>();
        int prunedTerrainData = 0;
        int removedTrees = 0;
        int clearedDetailLayers = 0;

        foreach (KeyValuePair<TerrainData, bool> entry in protectedData)
        {
            TerrainData data = entry.Key;
            data.wavingGrassStrength = 0f;
            data.wavingGrassAmount = 0f;
            data.wavingGrassSpeed = 0f;

            if (entry.Value)
            {
                continue;
            }

            TreeInstance[] trees = data.treeInstances;
            removedTrees += trees.Length;
            if (trees.Length > 0)
            {
                data.SetTreeInstances(new TreeInstance[0], true);
            }

            DetailPrototype[] detailPrototypes = data.detailPrototypes;
            int width = data.detailWidth;
            int height = data.detailHeight;
            if (detailPrototypes.Length > 0 && width > 0 && height > 0)
            {
                var dimensions = new Vector2Int(width, height);
                if (!emptyLayers.TryGetValue(dimensions, out int[,] emptyLayer))
                {
                    emptyLayer = new int[height, width];
                    emptyLayers.Add(dimensions, emptyLayer);
                }

                for (int layer = 0; layer < detailPrototypes.Length; layer++)
                {
                    data.SetDetailLayer(0, 0, layer, emptyLayer);
                    clearedDetailLayers++;
                }
            }

            prunedTerrainData++;
        }

        foreach (Terrain terrain in terrains)
        {
            if (terrain != null)
            {
                terrain.Flush();
            }
        }

        WindZone[] windZones = FindObjectsOfType<WindZone>(true);
        foreach (WindZone windZone in windZones)
        {
            if (windZone != null)
            {
                windZone.windMain = 0f;
                windZone.windTurbulence = 0f;
            }
        }

        Debug.Log(
            $"Quest vegetation pruning: cleared {removedTrees} trees and {clearedDetailLayers} detail layers " +
            $"on {prunedTerrainData} terrain data assets; retained scenery within {ScenicRadius}m of teleport areas.",
            this);
    }

    private static bool IsWithinScenicArea(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        Vector3 size = data.size;
        Vector3[] corners =
        {
            terrain.transform.TransformPoint(0f, 0f, 0f),
            terrain.transform.TransformPoint(size.x, 0f, 0f),
            terrain.transform.TransformPoint(0f, 0f, size.z),
            terrain.transform.TransformPoint(size.x, 0f, size.z)
        };

        float minX = corners[0].x;
        float maxX = corners[0].x;
        float minZ = corners[0].z;
        float maxZ = corners[0].z;
        for (int i = 1; i < corners.Length; i++)
        {
            minX = Mathf.Min(minX, corners[i].x);
            maxX = Mathf.Max(maxX, corners[i].x);
            minZ = Mathf.Min(minZ, corners[i].z);
            maxZ = Mathf.Max(maxZ, corners[i].z);
        }

        float radiusSquared = ScenicRadius * ScenicRadius;
        foreach (Vector2 center in ScenicCenters)
        {
            float dx = Mathf.Max(Mathf.Max(minX - center.x, 0f), center.x - maxX);
            float dz = Mathf.Max(Mathf.Max(minZ - center.y, 0f), center.y - maxZ);
            if (dx * dx + dz * dz <= radiusSquared)
            {
                return true;
            }
        }

        return false;
    }
}
