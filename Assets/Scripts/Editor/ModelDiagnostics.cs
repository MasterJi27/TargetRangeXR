using System.Text;
using UnityEditor;
using UnityEngine;

namespace TargetRange.EditorTools
{
    /// <summary>Debug helper: prints how Unity imported each OBJ (child objects and submeshes).</summary>
    public static class ModelDiagnostics
    {
        public static void Dump()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { AssetPipeline.ModelDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var sb = new StringBuilder($"DIAG {path}: ");
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                {
                    var mf = t.GetComponent<MeshFilter>();
                    sb.Append(t.name);
                    if (mf != null && mf.sharedMesh != null) sb.Append($"[sub={mf.sharedMesh.subMeshCount},v={mf.sharedMesh.vertexCount}]");
                    sb.Append(" / ");
                }
                Debug.Log(sb.ToString());
            }
        }
    }
}
