using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
// using UnityEngine.XR.MeshGenerationResult;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

public class ScannerExporter : MonoBehaviour
{
    [Header("Export Settings")]
    public string exportFolder = "QuestHouseExport";
    public bool exportOBJ = true;
    public bool exportGLB = true;
    [Header("HTTP Server")]
    public bool runHttpServer = false;
    public int httpPort = 8080;

    SimpleHttpServer httpServer;

    [Header("Scan Inputs")]
    // In a simple approach, user places this script in a scene where runtime meshes are present as GameObjects.
    // Rooms are expected to be top-level GameObjects named or with RoomMetadata component.
    public Transform roomsRoot;

    [Header("CSV & JSON")]
    public string houseName = "HouseExport";
    [Header("Google Drive Upload")]
    public bool enableDriveUpload = false;
    public GoogleDriveUploader driveUploader;

    public void ExportAll()
    {
        try
        {
            RuntimeLogger.Init(exportFolder);
            RuntimeLogger.WriteLine("ExportAll started");
        }
        catch { }

        string basePath = Path.Combine(Application.persistentDataPath, exportFolder);
        Directory.CreateDirectory(basePath);

        var rooms = new List<GameObject>();
        if (roomsRoot == null)
        {
            // find top-level rooms by tag or RoomMetadata
            foreach (var go in FindObjectsByType<RoomMetadata>(FindObjectsSortMode.None))
                rooms.Add(go.gameObject);
        }
        else
        {
            foreach (Transform t in roomsRoot)
            {
                rooms.Add(t.gameObject);
            }
        }

        var summaryLines = new List<string>();
        summaryLines.Add("RoomName,Area_m2,NumWindows,NumDoors,WindowsSizes,DoorsSizes");

        var jsonList = new List<Dictionary<string, object>>();

        RuntimeLogger.WriteLine($"Found {rooms.Count} candidate rooms");

        foreach (var room in rooms)
        {
            var meta = room.GetComponent<RoomMetadata>();
            string rname = (meta != null && !string.IsNullOrEmpty(meta.roomName)) ? meta.roomName : room.name;

            RuntimeLogger.WriteLine($"Processing room: {rname}");
            MeshFilter[] mfs = room.GetComponentsInChildren<MeshFilter>();
            int meshCount = 0;
            foreach (var mf in mfs) if (mf != null && mf.sharedMesh != null) meshCount++;
            RuntimeLogger.WriteLine($"  MeshFilter count: {mfs.Length}, meshes present: {meshCount}");

            var combined = CombineMeshes(mfs);
            if (combined == null)
            {
                RuntimeLogger.WriteLine($"  Combined mesh is null for room {rname}, skipping");
                continue;
            }

            string safeName = MakeSafeFileName(rname);
            // append timestamp to file names to avoid overwriting previous exports
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string safeNameTs = MakeSafeFileName(rname + "_" + timestamp);
            string objPath = Path.Combine(basePath, safeNameTs + ".obj");
            if (exportOBJ)
                ExportMeshToOBJ(combined, objPath);

            if (exportGLB)
            {
                string glbPath = Path.Combine(basePath, safeNameTs + ".glb");
                // Try export whole GameObject (preserve child windows/doors) if possible
                var roomGO = room;
                if (!GLTFExporterWrapper.TryExportGameObjectToGLB(roomGO, glbPath))
                    ExportMeshToGLB(combined, glbPath);
            }

            // Simple heuristics for windows/doors detection: look for child named "Window" or "Door"
            var windows = new List<Rect>();
            var doors = new List<Rect>();
            // First, detect openings from mesh boundaries
            var detected = DetectOpeningsFromMesh(combined);
            // merge overlapping detected openings
            var merged = MergeRects(detected);
            foreach (var d in merged)
            {
                if (d.isDoor) doors.Add(d.rect); else windows.Add(d.rect);
            }

            // Also include named placeholders if present
            foreach (Transform child in room.GetComponentsInChildren<Transform>())
            {
                string lower = child.name.ToLower();
                if (lower.Contains("window") || lower.Contains("okno"))
                {
                    var rect = EstimateRectFromTransform(child);
                    windows.Add(rect);
                }
                else if (lower.Contains("door") || lower.Contains("dvere") || lower.Contains("dveøe"))
                {
                    var rect = EstimateRectFromTransform(child);
                    doors.Add(rect);
                }
            }

            float area = EstimateArea(combined);
            string windowsSizes = String.Join(";", windows.ConvertAll(r => FormatRect(r)));
            string doorsSizes = String.Join(";", doors.ConvertAll(r => FormatRect(r)));

            summaryLines.Add($"{rname},{area:F2},{windows.Count},{doors.Count},\"{windowsSizes}\",\"{doorsSizes}\"");

            var roomJson = new Dictionary<string, object>();
            roomJson["name"] = rname;
            roomJson["area_m2"] = area;
            roomJson["windows"] = windows.ConvertAll(r => RectToDict(r));
            roomJson["doors"] = doors.ConvertAll(r => RectToDict(r));
            // TODO: include mesh reference
            jsonList.Add(roomJson);
        }

        File.WriteAllLines(Path.Combine(basePath, houseName + "_summary.csv"), summaryLines, Encoding.UTF8);

        string jsonPath = Path.Combine(basePath, houseName + "_rooms.json");
        File.WriteAllText(jsonPath, MiniJSON.Serialize(jsonList));

        RuntimeLogger.WriteLine($"Exported {rooms.Count} rooms to {basePath}");
        Debug.Log($"Exported {rooms.Count} rooms to {basePath}");
        // Also export a simple formatted Excel (SpreadsheetML) summary
        try
        {
            ExportSummaryToExcel(basePath, houseName, jsonList);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to export Excel summary: " + ex.Message);
        }

        if (enableDriveUpload && driveUploader != null && driveUploader.autoUpload && !string.IsNullOrEmpty(driveUploader.accessToken))
        {
            driveUploader.StartUploadDirectory(basePath);
        }
    }

    void ExportSummaryToExcel(string basePath, string houseName, List<Dictionary<string, object>> rooms)
    {
        string path = Path.Combine(basePath, houseName + "_summary.xls");
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">\n");
        sb.AppendLine("<Styles>");
        sb.AppendLine("  <Style ss:ID=\"sHeader\"><Font ss:Bold=\"1\"/></Style>");
        sb.AppendLine("  <Style ss:ID=\"sWrap\"><Alignment ss:WrapText=\"1\"/></Style>");
        sb.AppendLine("</Styles>");

        sb.AppendLine($"<Worksheet ss:Name=\"{XmlEscape(houseName)} Summary\">\n<Table>");

        // Header
        sb.AppendLine("  <Row ss:StyleID=\"sHeader\">");
        sb.AppendLine("    <Cell><Data ss:Type=\"String\">Room Name</Data></Cell>");
        sb.AppendLine("    <Cell><Data ss:Type=\"String\">Area (m2)</Data></Cell>");
        sb.AppendLine("    <Cell><Data ss:Type=\"String\"># Windows</Data></Cell>");
        sb.AppendLine("    <Cell><Data ss:Type=\"String\"># Doors</Data></Cell>");
        sb.AppendLine("    <Cell><Data ss:Type=\"String\">Windows (w x h)</Data></Cell>");
        sb.AppendLine("    <Cell><Data ss:Type=\"String\">Doors (w x h)</Data></Cell>");
        sb.AppendLine("  </Row>");

        foreach (var room in rooms)
        {
            string name = room.ContainsKey("name") ? room["name"].ToString() : "";
            string area = room.ContainsKey("area_m2") ? Convert.ToDouble(room["area_m2"]).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : "";
            var windows = room.ContainsKey("windows") ? room["windows"] as List<object> : null;
            var doors = room.ContainsKey("doors") ? room["doors"] as List<object> : null;

            int wcount = windows != null ? windows.Count : 0;
            int dcount = doors != null ? doors.Count : 0;

            string wdesc = "";
            if (windows != null)
            {
                var parts = new List<string>();
                foreach (var w in windows)
                {
                    if (w is Dictionary<string, object> wd && wd.ContainsKey("w") && wd.ContainsKey("h"))
                        parts.Add($"{wd["w"]}x{wd["h"]}");
                }
                wdesc = string.Join("; ", parts);
            }

            string ddesc = "";
            if (doors != null)
            {
                var parts = new List<string>();
                foreach (var d in doors)
                {
                    if (d is Dictionary<string, object> dd && dd.ContainsKey("w") && dd.ContainsKey("h"))
                        parts.Add($"{dd["w"]}x{dd["h"]}");
                }
                ddesc = string.Join("; ", parts);
            }

            sb.AppendLine("  <Row>");
            sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{XmlEscape(name)}</Data></Cell>");
            sb.AppendLine($"    <Cell><Data ss:Type=\"Number\">{area}</Data></Cell>");
            sb.AppendLine($"    <Cell><Data ss:Type=\"Number\">{wcount}</Data></Cell>");
            sb.AppendLine($"    <Cell><Data ss:Type=\"Number\">{dcount}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"sWrap\"><Data ss:Type=\"String\">{XmlEscape(wdesc)}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"sWrap\"><Data ss:Type=\"String\">{XmlEscape(ddesc)}</Data></Cell>");
            sb.AppendLine("  </Row>");
        }

        sb.AppendLine("</Table>\n</Worksheet>\n</Workbook>");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    string XmlEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
    }

    Mesh CombineMeshes(MeshFilter[] mfs)
    {
        if (mfs == null || mfs.Length == 0) return null;
        var combine = new CombineInstance[mfs.Length];
        for (int i = 0; i < mfs.Length; i++)
        {
            if (mfs[i].sharedMesh == null) continue;
            combine[i].mesh = mfs[i].sharedMesh;
            combine[i].transform = mfs[i].transform.localToWorldMatrix;
        }
        Mesh newMesh = new Mesh();
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        newMesh.CombineMeshes(combine, true, true);
        return newMesh;
    }

    void ExportMeshToOBJ(Mesh mesh, string path)
    {
        using (var sw = new StreamWriter(path))
        {
            sw.Write(MeshToOBJ(mesh));
        }
    }

    string MeshToOBJ(Mesh mesh)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Exported OBJ");
        foreach (var v in mesh.vertices)
            sb.AppendLine(string.Format("v {0} {1} {2}", v.x, v.y, v.z));
        foreach (var n in mesh.normals)
            sb.AppendLine(string.Format("vn {0} {1} {2}", n.x, n.y, n.z));
        foreach (var uv in mesh.uv)
            sb.AppendLine(string.Format("vt {0} {1}", uv.x, uv.y));
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            var triangles = mesh.GetTriangles(i);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                // OBJ indices are 1-based
                sb.AppendLine(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", triangles[t] + 1, triangles[t + 1] + 1, triangles[t + 2] + 1));
            }
        }
        return sb.ToString();
    }

    void ExportMeshToGLBPlaceholder(Mesh mesh, string path)
    {
        // Real GLB export requires glTF library. Here we write a simple placeholder file to indicate intent.
        File.WriteAllText(path, "GLB export not implemented. Use glTF exporter package.");
    }

    void OnEnable()
    {
        if (runHttpServer)
            StartHttpServer();
    }

    void OnDisable()
    {
        StopHttpServer();
    }

    public void StartHttpServer()
    {
        if (httpServer != null) return;
        string basePath = Path.Combine(Application.persistentDataPath, exportFolder);
        httpServer = new SimpleHttpServer(basePath, httpPort);
        httpServer.Start();
        Debug.Log($"HTTP server started on port {httpPort}, serving {basePath}");
    }

    public void StopHttpServer()
    {
        if (httpServer == null) return;
        httpServer.Stop();
        httpServer = null;
        Debug.Log("HTTP server stopped");
    }

    void ExportMeshToGLB(Mesh mesh, string path)
    {
        if (mesh == null) return;

        // Try external exporter first (if added to project)
        try
        {
            if (GLTFExporterWrapper.TryExportMeshToGLB(mesh, path)) return;
        }
        catch { }

        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var uvs = mesh.uv;
        var indices = mesh.triangles;

        int vertexCount = vertices.Length;
        int indexCount = indices.Length;

        bool useUshort = indexCount <= 65535;

        // Prepare float arrays
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
            else { uvFloats[i * 2 + 0] = uvFloats[i * 2 + 1] = 0f; }
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

        // Build JSON
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

        // basic material
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
            // GLB header
            bw.Write(0x46546C67); // magic 'glTF'
            bw.Write(2); // version
            int totalLength = 12 + 8 + (jsonBytes.Length + jsonPad) + 8 + (totalBin + binPad);
            bw.Write(totalLength);

            // JSON chunk
            bw.Write(jsonBytes.Length + jsonPad);
            bw.Write(0x4E4F534A); // 'JSON'
            bw.Write(jsonBytes);
            for (int i = 0; i < jsonPad; i++) bw.Write((byte)0x20);

            // BIN chunk
            bw.Write(totalBin + binPad);
            bw.Write(0x004E4942); // 'BIN'
            bw.Write(posBytes);
            bw.Write(normBytes);
            bw.Write(uvBytes);
            bw.Write(idxBytes);
            for (int i = 0; i < binPad; i++) bw.Write((byte)0);
        }
    }

    Rect EstimateRectFromTransform(Transform t)
    {
        // Heuristic: use local scale X (width) and Y (height), position
        Vector3 s = t.lossyScale;
        Vector3 p = t.position;
        return new Rect(p.x - s.x / 2f, p.y - s.y / 2f, s.x, s.y);
    }

    class DetectedOpening { public Rect rect; public bool isDoor; }

    List<DetectedOpening> DetectOpeningsFromMesh(Mesh mesh)
    {
        var res = new List<DetectedOpening>();
        if (mesh == null) return res;

        var verts = mesh.vertices;
        var tris = mesh.triangles;
        int triCount = tris.Length / 3;

        // Build edge dictionary
        var edgeMap = new Dictionary<long, List<int>>();
        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i], b = tris[i + 1], c = tris[i + 2];
            AddEdge(edgeMap, a, b, i / 3);
            AddEdge(edgeMap, b, c, i / 3);
            AddEdge(edgeMap, c, a, i / 3);
        }

        // Boundary edges: edges referenced by only one triangle
        var boundaryEdges = new Dictionary<int, List<int>>(); // vertex -> connected vertices
        foreach (var kv in edgeMap)
        {
            var list = kv.Value;
            if (list.Count == 1)
            {
                int key = (int)(kv.Key >> 32);
                int val = (int)(kv.Key & 0xffffffff);
                if (!boundaryEdges.ContainsKey(key)) boundaryEdges[key] = new List<int>();
                if (!boundaryEdges.ContainsKey(val)) boundaryEdges[val] = new List<int>();
                boundaryEdges[key].Add(val);
                boundaryEdges[val].Add(key);
            }
        }

        var visited = new HashSet<int>();
        foreach (var kv in boundaryEdges)
        {
            int start = kv.Key;
            if (visited.Contains(start)) continue;
            var loop = new List<int>();
            int cur = start; int prev = -1;
            while (cur != -1 && !visited.Contains(cur))
            {
                visited.Add(cur);
                loop.Add(cur);
                int next = -1;
                foreach (var n in boundaryEdges[cur]) if (n != prev) { next = n; break; }
                prev = cur; cur = next;
            }

            if (loop.Count < 4) continue;

            // Compute bounding box of loop
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var idx in loop)
            {
                var v = verts[idx];
                minX = Math.Min(minX, v.x); maxX = Math.Max(maxX, v.x);
                minY = Math.Min(minY, v.y); maxY = Math.Max(maxY, v.y);
            }
            float width = maxX - minX; float height = maxY - minY;
            if (height < 0.2f || width < 0.2f) continue;

            // Heuristics: door if height > 1.8m else window if elevated from floor
            bool isDoor = height >= 1.8f;
            // If bbox bottom y is above 0.6 then likely window
            float bottomY = minY;
            if (!isDoor && bottomY > 0.6f) isDoor = false;

            var rect = new Rect(minX, minY, width, height);
            res.Add(new DetectedOpening() { rect = rect, isDoor = isDoor });
        }
        return res;
    }

    List<DetectedOpening> MergeRects(List<DetectedOpening> detected)
    {
        var list = new List<DetectedOpening>(detected);
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    if (RectsOverlap(list[i].rect, list[j].rect))
                    {
                        var r = UnionRect(list[i].rect, list[j].rect);
                        bool isDoor = list[i].isDoor || list[j].isDoor;
                        list[i].rect = r; list[i].isDoor = isDoor;
                        list.RemoveAt(j);
                        changed = true;
                        goto nextIter;
                    }
                }
            }
        nextIter: ;
        }
        return list;
    }

    bool RectsOverlap(Rect a, Rect b)
    {
        return a.xMin <= b.xMax && a.xMax >= b.xMin && a.yMin <= b.yMax && a.yMax >= b.yMin;
    }

    Rect UnionRect(Rect a, Rect b)
    {
        float xMin = Math.Min(a.xMin, b.xMin);
        float yMin = Math.Min(a.yMin, b.yMin);
        float xMax = Math.Max(a.xMax, b.xMax);
        float yMax = Math.Max(a.yMax, b.yMax);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    void AddEdge(Dictionary<long, List<int>> map, int a, int b, int triIndex)
    {
        int vmin = Math.Min(a, b), vmax = Math.Max(a, b);
        long key = ((long)vmin << 32) | (uint)vmax;
        if (!map.TryGetValue(key, out var list)) { list = new List<int>(); map[key] = list; }
        list.Add(triIndex);
    }

    float EstimateArea(Mesh mesh)
    {
        // Approximate area by projecting to XZ and computing mesh bounds area
        if (mesh == null) return 0f;
        var b = mesh.bounds;
        return b.size.x * b.size.z;
    }

    string MakeSafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    string FormatRect(Rect r) => $"{r.width:F2}x{r.height:F2}@({r.x:F2},{r.y:F2})";
    Dictionary<string, object> RectToDict(Rect r)
    {
        return new Dictionary<string, object>()
        {
            {"x", r.x}, {"y", r.y}, {"w", r.width}, {"h", r.height}
        };
    }
}
