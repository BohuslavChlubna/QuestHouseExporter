using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Minimal GLB exporter used by the project and by the GLTFUtility shim.
public static class SimpleGltfExporter
{
    // Export mesh to GLB. Returns true on success.
    public static bool Export(Mesh mesh, string path)
    {
        try
        {
            if (mesh == null) { Debug.LogError("SimpleGltfExporter: mesh is null"); return false; }

            if (mesh.vertexCount == 0)
            {
                Debug.LogError("SimpleGltfExporter: mesh has no vertices");
                return false;
            }

            var indices = mesh.triangles;
            if (indices == null || indices.Length == 0)
            {
                Debug.LogError("SimpleGltfExporter: mesh has no triangles/indices");
                return false;
            }

            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;

            int vertexCount = vertices.Length;
            int indexCount = indices.Length;

            bool useUshort = indexCount <= 65535;

            var posFloats = new float[vertexCount * 3];
            var normFloats = new float[vertexCount * 3];
            var uvFloats = new float[vertexCount * 2];
            for (int i = 0; i < vertexCount; i++)
            {
                var v = vertices[i]; posFloats[i * 3 + 0] = v.x; posFloats[i * 3 + 1] = v.y; posFloats[i * 3 + 2] = v.z;
                if (normals != null && normals.Length == vertexCount)
                { var n = normals[i]; normFloats[i * 3 + 0] = n.x; normFloats[i * 3 + 1] = n.y; normFloats[i * 3 + 2] = n.z; }
                else { normFloats[i * 3 + 0] = normFloats[i * 3 + 1] = normFloats[i * 3 + 2] = 0f; }
                if (uvs != null && uvs.Length == vertexCount) { var uv = uvs[i]; uvFloats[i * 2 + 0] = uv.x; uvFloats[i * 2 + 1] = uv.y; }
                else { uvFloats[i * 2 + 0] = 0f; uvFloats[i * 2 + 1] = 0f; }
            }

            byte[] posBytes = new byte[posFloats.Length * 4]; Buffer.BlockCopy(posFloats, 0, posBytes, 0, posBytes.Length);
            byte[] normBytes = new byte[normFloats.Length * 4]; Buffer.BlockCopy(normFloats, 0, normBytes, 0, normBytes.Length);
            byte[] uvBytes = new byte[uvFloats.Length * 4]; Buffer.BlockCopy(uvFloats, 0, uvBytes, 0, uvBytes.Length);

            byte[] idxBytes;
            if (useUshort)
            {
                var sidx = new ushort[indexCount];
                for (int i = 0; i < indexCount; i++) sidx[i] = (ushort)indices[i];
                idxBytes = new byte[sidx.Length * 2]; Buffer.BlockCopy(sidx, 0, idxBytes, 0, idxBytes.Length);
            }
            else
            {
                var uidx = new uint[indexCount];
                for (int i = 0; i < indexCount; i++) uidx[i] = (uint)indices[i];
                idxBytes = new byte[uidx.Length * 4]; Buffer.BlockCopy(uidx, 0, idxBytes, 0, idxBytes.Length);
            }

            int posOffset = 0;
            int normOffset = posOffset + posBytes.Length;
            int uvOffset = normOffset + normBytes.Length;
            int idxOffset = uvOffset + uvBytes.Length;

            int totalBin = posBytes.Length + normBytes.Length + uvBytes.Length + idxBytes.Length;

            var min = new float[3] { float.MaxValue, float.MaxValue, float.MaxValue };
            var max = new float[3] { float.MinValue, float.MinValue, float.MinValue };
            foreach (var v in vertices)
            {
                min[0] = Math.Min(min[0], v.x);
                min[1] = Math.Min(min[1], v.y);
                min[2] = Math.Min(min[2], v.z);
                max[0] = Math.Max(max[0], v.x);
                max[1] = Math.Max(max[1], v.y);
                max[2] = Math.Max(max[2], v.z);
            }

            var gltf = new Dictionary<string, object>();
            gltf["asset"] = new Dictionary<string, object>() { { "version", "2.0" } };
            gltf["buffers"] = new List<Dictionary<string, object>>() { new Dictionary<string, object>() { { "byteLength", totalBin } } };

            var bufferViews = new List<Dictionary<string, object>>();
            bufferViews.Add(new Dictionary<string, object>() { { "buffer", 0 }, { "byteOffset", posOffset }, { "byteLength", posBytes.Length } });
            bufferViews.Add(new Dictionary<string, object>() { { "buffer", 0 }, { "byteOffset", normOffset }, { "byteLength", normBytes.Length } });
            bufferViews.Add(new Dictionary<string, object>() { { "buffer", 0 }, { "byteOffset", uvOffset }, { "byteLength", uvBytes.Length } });
            bufferViews.Add(new Dictionary<string, object>() { { "buffer", 0 }, { "byteOffset", idxOffset }, { "byteLength", idxBytes.Length } });
            gltf["bufferViews"] = bufferViews;

            var accessors = new List<Dictionary<string, object>>();
            accessors.Add(new Dictionary<string, object>() { { "bufferView", 0 }, { "componentType", 5126 }, { "count", vertexCount }, { "type", "VEC3" }, { "min", new List<float>(min) }, { "max", new List<float>(max) } });
            accessors.Add(new Dictionary<string, object>() { { "bufferView", 1 }, { "componentType", 5126 }, { "count", vertexCount }, { "type", "VEC3" } });
            accessors.Add(new Dictionary<string, object>() { { "bufferView", 2 }, { "componentType", 5126 }, { "count", vertexCount }, { "type", "VEC2" } });
            accessors.Add(new Dictionary<string, object>() { { "bufferView", 3 }, { "componentType", useUshort ? 5123 : 5125 }, { "count", indexCount }, { "type", "SCALAR" } });
            gltf["accessors"] = accessors;

            var materials = new List<Dictionary<string, object>>();
            materials.Add(new Dictionary<string, object>() { { "pbrMetallicRoughness", new Dictionary<string, object>() { { "baseColorFactor", new List<float>() { 1f, 1f, 1f, 1f } }, { "metallicFactor", 0f }, { "roughnessFactor", 1f } } } });
            gltf["materials"] = materials;

            var prim = new Dictionary<string, object>();
            prim["attributes"] = new Dictionary<string, object>() { { "POSITION", 0 }, { "NORMAL", 1 }, { "TEXCOORD_0", 2 } };
            prim["indices"] = 3;
            prim["material"] = 0;
            var meshList = new List<Dictionary<string, object>>() { new Dictionary<string, object>() { { "primitives", new List<Dictionary<string, object>>() { prim } } } };
            gltf["meshes"] = meshList;
            gltf["nodes"] = new List<Dictionary<string, object>>() { new Dictionary<string, object>() { { "mesh", 0 } } };
            gltf["scenes"] = new List<Dictionary<string, object>>() { new Dictionary<string, object>() { { "nodes", new List<int>() { 0 } } } };
            gltf["scene"] = 0;

            string json = MiniJSON.Serialize(gltf);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            int jsonPad = (4 - (jsonBytes.Length % 4)) % 4;
            int binPad = (4 - (totalBin % 4)) % 4;

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(0x46546C67);
                bw.Write(2);
                int totalLength = 12 + 8 + (jsonBytes.Length + jsonPad) + 8 + (totalBin + binPad);
                bw.Write(totalLength);

                bw.Write(jsonBytes.Length + jsonPad);
                bw.Write(0x4E4F534A);
                bw.Write(jsonBytes);
                for (int i = 0; i < jsonPad; i++) bw.Write((byte)0x20);

                bw.Write(totalBin + binPad);
                bw.Write(0x004E4942);
                bw.Write(posBytes);
                bw.Write(normBytes);
                bw.Write(uvBytes);
                bw.Write(idxBytes);
                for (int i = 0; i < binPad; i++) bw.Write((byte)0);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("SimpleGltfExporter failed: " + ex.ToString());
            if (ex.InnerException != null) Debug.LogError("Inner: " + ex.InnerException.ToString());
            return false;
        }
    }

    public static bool Export(GameObject go, string path)
    {
        if (go == null) return false;
        var mf = go.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;
        return Export(mf.sharedMesh, path);
    }
}
