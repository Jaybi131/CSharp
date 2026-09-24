#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Put in Assets/Editor. Works on a scene COPY; original imported meshes are never edited.
// Removes triangles, keeping vertex data, skin weights, bind poses and blend shapes.
// Unity 2020.3+ (GetAllBoneWeights). Not a geometric plane cutter: edges follow topology.
public class FPSArmsExtractor : EditorWindow
{
    GameObject target;
    float threshold = 0.5f;
    bool includeShoulders;
    bool keepBoundary = true;

    [MenuItem("Tools/FPS/Extract Arms")]
    static void Open() { GetWindow<FPSArmsExtractor>("Extract Arms"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Use a scene copy of man_10. Keeps geometry weighted to both arm branches. Undo before changing settings and rebuilding. Generated mesh assets remain after Undo.", MessageType.Info);
        target = (GameObject)EditorGUILayout.ObjectField("Character copy", target, typeof(GameObject), true);
        threshold = EditorGUILayout.Slider("Arm weight threshold", threshold, 0.01f, 1f);
        includeShoulders = EditorGUILayout.Toggle("Include shoulder bones", includeShoulders);
        keepBoundary = EditorGUILayout.Toggle("Keep boundary triangles", keepBoundary);
        EditorGUILayout.HelpBox("Higher threshold removes more near shoulders. Boundary ON keeps a triangle if any vertex qualifies; OFF requires all three. This does not cap the cut or reduce the skeleton.", MessageType.None);
        using (new EditorGUI.DisabledScope(target == null || EditorApplication.isPlaying))
            if (GUILayout.Button("Extract and save meshes")) Extract();
    }

    class Result
    {
        public SkinnedMeshRenderer renderer;
        public Mesh mesh;
    }

    void Extract()
    {
        var results = new List<Result>();
        string assetPath = null;
        int undoGroup = -1;
        try
        {
            if (EditorUtility.IsPersistent(target) || !target.scene.IsValid())
                throw new Exception("Drag a character COPY from Hierarchy, not the Project asset.");

            string leftName = includeShoulders ? "shoulder_L" : "upper_arm_L";
            string rightName = includeShoulders ? "shoulder_R" : "upper_arm_R";
            Transform left = null, right = null;
            foreach (var t in target.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == leftName) { if (left != null) throw new Exception("Duplicate left arm root. Select just one character."); left = t; }
                if (t.name == rightName) { if (right != null) throw new Exception("Duplicate right arm root. Select just one character."); right = t; }
            }
            if (left == null || right == null)
                throw new Exception("Could not find both arm roots: " + leftName + ", " + rightName);

            int totalTriangles = 0;
            // Build every mesh first. A failure here leaves the scene unchanged.
            foreach (var r in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh source = r.sharedMesh;
                if (source == null) continue;
                var item = new Result { renderer = r };
                results.Add(item);
                if (!source.isReadable)
                    throw new Exception(source.name + ": enable Read/Write in the source model Import Settings, then Apply.");

                bool[] armBones = new bool[r.bones.Length];
                for (int i = 0; i < armBones.Length; i++)
                {
                    Transform b = r.bones[i];
                    armBones[i] = b != null && (b == left || b.IsChildOf(left) || b == right || b.IsChildOf(right));
                }
                bool[] keep = new bool[source.vertexCount];
                var counts = source.GetBonesPerVertex();
                var weights = source.GetAllBoneWeights();
                try
                {
                    if (counts.Length != source.vertexCount)
                        throw new Exception(source.name + ": missing skin weights. Inspect the Skinned Mesh Renderer.");
                    int cursor = 0;
                    for (int v = 0; v < keep.Length; v++)
                    {
                        float sum = 0;
                        for (int n = 0; n < counts[v]; n++)
                        {
                            var w = weights[cursor++];
                            if (w.boneIndex >= 0 && w.boneIndex < armBones.Length && armBones[w.boneIndex]) sum += w.weight;
                        }
                        keep[v] = sum + 0.000001f >= threshold;
                    }
                }
                finally { counts.Dispose(); weights.Dispose(); }

                var triangles = new List<int>[source.subMeshCount];
                int kept = 0;
                for (int s = 0; s < source.subMeshCount; s++)
                {
                    if (source.GetTopology(s) != MeshTopology.Triangles)
                        throw new Exception(source.name + ": only triangle meshes are supported.");
                    int[] input = source.GetTriangles(s);
                    var output = new List<int>();
                    triangles[s] = output;
                    for (int i = 0; i < input.Length; i += 3)
                    {
                        bool a = keep[input[i]], b = keep[input[i + 1]], c = keep[input[i + 2]];
                        if (!(keepBoundary ? a || b || c : a && b && c)) continue;
                        output.Add(input[i]); output.Add(input[i + 1]); output.Add(input[i + 2]);
                        kept++;
                    }
                }
                if (kept == 0) continue;
                item.mesh = Instantiate(source);
                item.mesh.name = r.name + "_FPSArms";
                // Keep original indices, normals, UVs, blend shapes and skinning unchanged.
                for (int s = 0; s < triangles.Length; s++) item.mesh.SetTriangles(triangles[s], s, false);
                totalTriangles += kept;
            }
            if (totalTriangles == 0) throw new Exception("No arm triangles found. No changes were made.");

            assetPath = EditorUtility.SaveFilePanelInProject("Save extracted meshes", "FPS_Arms_Meshes", "asset", "Choose a NEW asset file.");
            if (string.IsNullOrEmpty(assetPath)) return;
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            bool first = true;
            foreach (var item in results)
            {
                if (item.mesh == null) continue;
                if (first) { AssetDatabase.CreateAsset(item.mesh, assetPath); first = false; }
                else AssetDatabase.AddObjectToAsset(item.mesh, assetPath);
            }
            AssetDatabase.SaveAssets();

            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Extract FPS arms");
            foreach (var item in results)
            {
                Undo.RecordObject(item.renderer, "Extract FPS arms");
                if (item.mesh != null) item.renderer.sharedMesh = item.mesh;
                else item.renderer.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.renderer);
            }
            // Static accessories have no arm weights, so hide them on this copy.
            foreach (var r in target.GetComponentsInChildren<MeshRenderer>(true))
            {
                Undo.RecordObject(r, "Hide static accessories");
                r.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();
            EditorUtility.DisplayDialog("Arms extracted", "Kept " + totalTriangles + " triangles. Inspect both sleeves and hands. Undo before trying another threshold. Bones and original model assets were preserved.", "OK");
        }
        catch (Exception e)
        {
            if (undoGroup >= 0) Undo.RevertAllDownToGroup(undoGroup);
            if (!string.IsNullOrEmpty(assetPath)) AssetDatabase.DeleteAsset(assetPath);
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Extraction stopped", e.Message, "OK");
        }
        finally
        {
            foreach (var item in results)
                if (item.mesh != null && !EditorUtility.IsPersistent(item.mesh)) DestroyImmediate(item.mesh);
        }
    }
}
#endif
