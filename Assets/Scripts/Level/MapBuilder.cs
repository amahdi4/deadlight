using UnityEngine;
using Deadlight.Data;
using Deadlight.Visuals;

namespace Deadlight.Level
{
    public static class MapBuilder
    {
        public static void BuildEnvironment(Transform parent, MapConfig config, bool useProceduralSprites)
        {
            if (config == null) config = MapConfig.GetConfigForType(MapType.TownCenter);

            switch (config.mapType)
            {
                case MapType.Industrial: BuildIndustrial(parent, config, useProceduralSprites); break;
                case MapType.Suburban: BuildSuburban(parent, config, useProceduralSprites); break;
                default: BuildTownCenter(parent, config, useProceduralSprites); break;
            }
        }

        private static void BuildTownCenter(Transform parent, MapConfig cfg, bool procedural)
        {
            var root = new GameObject("TownCenter");
            root.transform.SetParent(parent);

            // place buildings in a loose grid using streetGridSpacing where available
            float spacing = cfg.streetGridSpacing > 1f ? cfg.streetGridSpacing : 10f;
            int cols = Mathf.Max(2, Mathf.FloorToInt((cfg.halfWidth * 2) / spacing));
            int rows = Mathf.Max(2, Mathf.FloorToInt((cfg.halfHeight * 2) / spacing));

            int placed = 0;
            for (int r = 0; r < rows && placed < cfg.houseCount; r++)
            {
                for (int c = 0; c < cols && placed < cfg.houseCount; c++)
                {
                    float x = -cfg.halfWidth + spacing * (c + 0.5f);
                    float y = -cfg.halfHeight + spacing * (r + 0.5f);
                    Vector3 pos = new Vector3(x + Random.Range(-1f, 1f), y + Random.Range(-1f, 1f), 0);
                    SpawnBuilding(root.transform, pos, Random.Range(1.4f, 2.0f), placed % 3);
                    placed++;
                }
            }

            // trees, rocks, crates and barrels distributed with slight clustering
            ScatterObjects(root.transform, cfg, procedural);

            // add some alley walls to create dead-ends
            CreateAlleyPocket(root.transform, new Vector3(cfg.halfWidth * 0.5f, 0, 0));
            CreateAlleyPocket(root.transform, new Vector3(-cfg.halfWidth * 0.45f, -cfg.halfHeight * 0.3f, 0));
        }

        private static void BuildIndustrial(Transform parent, MapConfig cfg, bool procedural)
        {
            var root = new GameObject("Industrial");
            root.transform.SetParent(parent);

            // warehouses in rows
            int rows = Mathf.Max(3, cfg.halfHeight / 4);
            float startY = cfg.halfHeight - 4f;
            for (int r = 0; r < rows; r++)
            {
                int cols = Mathf.Max(3, cfg.halfWidth / 4);
                for (int c = 0; c < cols; c++)
                {
                    float x = -cfg.halfWidth + 4f + c * (cfg.halfWidth * 2f / cols) + Random.Range(-0.5f, 0.5f);
                    float y = startY - r * 6f + Random.Range(-0.6f, 0.6f);
                    SpawnBuilding(root.transform, new Vector3(x, y, 0), Random.Range(1.8f, 2.6f), (r + c) % 3);
                }
            }

            ScatterObjects(root.transform, cfg, procedural);

            // create container lanes (long thin walls)
            for (int i = -1; i <= 1; i++)
            {
                var lane = new GameObject($"ContainerLane_{i}");
                lane.transform.SetParent(root.transform);
                lane.transform.position = new Vector3(i * (cfg.halfWidth * 0.33f), 0, 0);
                var sr = lane.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreateWallSprite(true, Mathf.RoundToInt(cfg.halfWidth * 16));
                sr.sortingOrder = -5;
                var col = lane.AddComponent<BoxCollider2D>();
                col.size = new Vector2(cfg.halfWidth * 1.2f, 0.5f);
            }
        }

        private static void BuildSuburban(Transform parent, MapConfig cfg, bool procedural)
        {
            var root = new GameObject("Suburban");
            root.transform.SetParent(parent);

            // place houses spread out with yards and occasional cul-de-sac
            int placed = 0;
            int perRow = Mathf.Max(3, cfg.halfWidth / 4);
            float y = cfg.halfHeight * 0.4f;
            for (int r = 0; r < 4 && placed < cfg.houseCount; r++)
            {
                for (int c = 0; c < perRow && placed < cfg.houseCount; c++)
                {
                    float x = -cfg.halfWidth + (c + 0.5f) * (cfg.halfWidth * 2f / perRow) + Random.Range(-1.5f, 1.5f);
                    Vector3 pos = new Vector3(x, y + Random.Range(-1f, 1f), 0);
                    SpawnBuilding(root.transform, pos, Random.Range(1.2f, 1.8f), placed % 3);
                    placed++;
                }
                y -= 8f;
            }

            ScatterObjects(root.transform, cfg, procedural);

            // small cul-de-sac
            CreateCulDeSac(root.transform, new Vector3(cfg.halfWidth * 0.6f, -cfg.halfHeight * 0.35f, 0));
        }

        private static void ScatterObjects(Transform parent, MapConfig cfg, bool procedural)
        {
            // trees
            for (int i = 0; i < cfg.treeCount; i++)
            {
                Vector3 p = RandomPointInBounds(cfg);
                SpawnTree(parent, p);
            }

            // rocks
            for (int i = 0; i < cfg.rockCount; i++)
            {
                Vector3 p = RandomPointInBounds(cfg);
                SpawnRock(parent, p);
            }

            // crates
            for (int i = 0; i < cfg.crateCount; i++)
            {
                Vector3 p = RandomPointNearStructures(parent, cfg);
                SpawnCrate(parent, p);
            }

            // barrels
            for (int i = 0; i < cfg.barrelCount; i++)
            {
                Vector3 p = RandomPointInBounds(cfg);
                SpawnBarrel(parent, p);
            }

            // cars
            for (int i = 0; i < cfg.carCount; i++)
            {
                Vector3 p = RandomPointInBounds(cfg);
                SpawnCar(parent, p, Random.Range(-20f, 20f));
            }
        }

        private static Vector3 RandomPointInBounds(MapConfig cfg)
        {
            float x = Random.Range(-cfg.halfWidth + 1f, cfg.halfWidth - 1f);
            float y = Random.Range(-cfg.halfHeight + 1f, cfg.halfHeight - 1f);
            return new Vector3(x, y, 0);
        }

        private static Vector3 RandomPointNearStructures(Transform parent, MapConfig cfg)
        {
            // try to find an existing child to cluster near, otherwise random
            if (parent.childCount > 0)
            {
                var child = parent.GetChild(Random.Range(0, parent.childCount));
                Vector3 offset = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(-1.2f, 1.2f), 0);
                return child.position + offset;
            }
            return RandomPointInBounds(cfg);
        }

        private static void CreateAlleyPocket(Transform parent, Vector3 center)
        {
            // create three walls forming a dead-end pocket
            for (int i = 0; i < 3; i++)
            {
                var wall = new GameObject($"AlleyWall_{i}");
                wall.transform.SetParent(parent);
                wall.transform.position = center + new Vector3((i - 1) * 1.2f, -0.8f, 0);
                var sr = wall.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreateWallSprite(false, 32);
                sr.sortingOrder = -2;
                var col = wall.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.6f, 1.8f);
            }
        }

        private static void CreateCulDeSac(Transform parent, Vector3 center)
        {
            var ring = new GameObject("CulDeSac");
            ring.transform.SetParent(parent);
            ring.transform.position = center;
            var sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateFilledCircleSprite(28, new Color(0.72f, 0.72f, 0.74f));
            sr.sortingOrder = -10;
            ring.transform.localScale = Vector3.one * 0.8f;
        }

        #region Spawners
        private static void SpawnBuilding(Transform parent, Vector3 pos, float scale, int variant)
        {
            var b = new GameObject("Building");
            b.transform.SetParent(parent);
            b.transform.position = pos;
            b.transform.localScale = Vector3.one * scale;
            var sr = b.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateBuildingSprite(variant % 3);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            var col = b.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.2f);
        }

        private static void SpawnTree(Transform parent, Vector3 pos)
        {
            var t = new GameObject("Tree");
            t.transform.SetParent(parent);
            t.transform.position = pos;
            t.transform.localScale = Vector3.one * Random.Range(1.0f, 2f);
            var sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateTreeSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 1;
            var col = t.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;
            col.offset = new Vector2(0, -0.3f);
        }

        private static void SpawnRock(Transform parent, Vector3 pos)
        {
            var r = new GameObject("Rock");
            r.transform.SetParent(parent);
            r.transform.position = pos;
            r.transform.localScale = Vector3.one * Random.Range(0.7f, 1.4f);
            var sr = r.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateRockSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            var col = r.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;
        }

        private static void SpawnCrate(Transform parent, Vector3 pos)
        {
            var c = new GameObject("Crate");
            c.transform.SetParent(parent);
            c.transform.position = pos;
            var sr = c.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            var col = c.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
        }

        private static void SpawnBarrel(Transform parent, Vector3 pos)
        {
            var b = new GameObject("Barrel");
            b.transform.SetParent(parent);
            b.transform.position = pos;
            var sr = b.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateBarrelSprite(Random.value > 0.7f);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            var col = b.AddComponent<CircleCollider2D>();
            col.radius = 0.25f;
        }

        private static void SpawnCar(Transform parent, Vector3 pos, float angle = 0f)
        {
            var c = new GameObject("Car");
            c.transform.SetParent(parent);
            c.transform.position = pos;
            c.transform.rotation = Quaternion.Euler(0, 0, angle);
            var sr = c.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateCarSprite(Random.Range(0, 4));
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            var col = c.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 0.7f);
        }
        #endregion
    }
}
