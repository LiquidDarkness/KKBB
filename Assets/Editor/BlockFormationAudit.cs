using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Says which block formations still hang off the block prefab and which have been unpacked into
// loose copies. Worth being able to check at a glance, because Unity unpacks nested prefabs on its
// own often enough: an unpacked block quietly ignores every later edit to the prefab it came from,
// and the ones in this project have already lost their physics material that way.
//
// Every level the game can actually load is covered, since the list is taken from the LevelData
// assets rather than from a folder - a formation nothing points at cannot break a run.
//
// Read-only. Missing materials are handed out at load time by LevelLoader.blockMaterial, so a
// formation flagged here still plays correctly; what the flag means is that the prefab is no
// longer the place to change that formation from.
public static class BlockFormationAudit
{
    private const string MenuPath = "Debug/Audit block formations";

    private class Row
    {
        public string path;
        public int blocks;
        public int unpacked;
        public int withoutMaterial;
    }

    [MenuItem(MenuPath, priority = 101)]
    private static void Audit()
    {
        List<Row> rows = new List<Row>();
        HashSet<GameObject> alreadySeen = new HashSet<GameObject>();
        int placeholders = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:LevelData"))
        {
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));

            if (level == null)
            {
                continue;
            }

            // The ending of every scenario is a content-less placeholder that is never loaded.
            if (level.content == null)
            {
                placeholders++;
                continue;
            }

            // Several LevelData can share one formation; count it once.
            if (!alreadySeen.Add(level.content))
            {
                continue;
            }

            rows.Add(Inspect(level.content));
        }

        Report(rows, placeholders);
    }

    private static Row Inspect(GameObject formation)
    {
        Row row = new Row { path = AssetDatabase.GetAssetPath(formation) };

        foreach (Block block in formation.GetComponentsInChildren<Block>(true))
        {
            row.blocks++;

            // Null means the object is not part of any prefab instance - it was pasted into this
            // formation and no longer has a source to inherit from.
            if (PrefabUtility.GetCorrespondingObjectFromSource(block.gameObject) == null)
            {
                row.unpacked++;
            }

            Collider2D blockCollider = block.GetComponent<Collider2D>();
            if (blockCollider == null || blockCollider.sharedMaterial == null)
            {
                row.withoutMaterial++;
            }
        }

        return row;
    }

    private static void Report(List<Row> rows, int placeholders)
    {
        StringBuilder report = new StringBuilder();
        List<Row> broken = rows.Where(r => r.unpacked > 0 || r.withoutMaterial > 0)
                               .OrderByDescending(r => r.unpacked)
                               .ToList();

        report.AppendLine($"Block formation audit: {rows.Count} formations, {rows.Sum(r => r.blocks)} blocks.");
        report.AppendLine($"  unpacked (deaf to the block prefab): {rows.Sum(r => r.unpacked)}");
        report.AppendLine($"  without a physics material of their own: {rows.Sum(r => r.withoutMaterial)}");
        report.AppendLine($"  clean formations: {rows.Count - broken.Count}; ending placeholders skipped: {placeholders}");

        if (broken.Count == 0)
        {
            report.AppendLine("Every formation is still linked to its prefab.");
            Debug.Log(report.ToString());
            return;
        }

        report.AppendLine();
        report.AppendLine("formation                                             blocks  unpacked  no material");

        foreach (Row row in broken)
        {
            string name = row.path.Replace("Assets/Stories/", string.Empty);
            report.AppendLine($"{name,-52} {row.blocks,6} {row.unpacked,9} {row.withoutMaterial,12}");
        }

        Debug.LogWarning(report.ToString());
    }
}
