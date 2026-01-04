using System;
using System.Reflection;
using UnityEngine;

public static class GLTFExporterWrapper
{
    // Try to find a glTF export method in loaded assemblies and invoke it.
    // Returns true if export was handled by external library.
    public static bool TryExportMeshToGLB(Mesh mesh, string path)
    {
        if (mesh == null) return false;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in asm.GetTypes())
                {
                    // common exporter type patterns
                    if (type.Name.IndexOf("GLTF", StringComparison.OrdinalIgnoreCase) >= 0 || type.Name.IndexOf("Gltf", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // look for static Export or Save methods
                        var m = type.GetMethod("Export", BindingFlags.Public | BindingFlags.Static) ?? type.GetMethod("Save", BindingFlags.Public | BindingFlags.Static);
                        if (m != null)
                        {
                            var parameters = m.GetParameters();
                            try
                            {
                                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(Mesh) && parameters[1].ParameterType == typeof(string))
                                {
                                    m.Invoke(null, new object[] { mesh, path });
                                    Debug.Log("Exported GLB via " + type.FullName);
                                    return true;
                                }
                                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                                {
                                    // maybe requires a GameObject or scene. Create temporary GO
                                    var go = new GameObject("_gltf_temp");
                                    var mf = go.AddComponent<MeshFilter>();
                                    var mr = go.AddComponent<MeshRenderer>();
                                    mf.sharedMesh = mesh;
                                    m.Invoke(null, new object[] { path });
                                    GameObject.DestroyImmediate(go);
                                    Debug.Log("Exported GLB via " + type.FullName);
                                    return true;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log full details including inner exception if any to help debugging
                                Debug.LogError("GLTF exporter invoke failed: " + ex.ToString());
                                if (ex is System.Reflection.TargetInvocationException && ex.InnerException != null)
                                {
                                    Debug.LogError("Inner exception: " + ex.InnerException.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return false;
    }

    public static bool TryExportGameObjectToGLB(GameObject go, string path)
    {
        if (go == null) return false;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.Name.IndexOf("GLTF", StringComparison.OrdinalIgnoreCase) >= 0 || type.Name.IndexOf("Gltf", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var m = type.GetMethod("Save", BindingFlags.Public | BindingFlags.Static) ?? type.GetMethod("Export", BindingFlags.Public | BindingFlags.Static);
                        if (m != null)
                        {
                            var parameters = m.GetParameters();
                            try
                            {
                                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(GameObject) && parameters[1].ParameterType == typeof(string))
                                {
                                    m.Invoke(null, new object[] { go, path });
                                    Debug.Log("Exported GLB via " + type.FullName + " (GameObject)");
                                    return true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError("GLTF exporter invoke failed: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return false;
    }
}
