using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vytvoøí statický dollhouse podle zadaných parametrù (4x5m, šikmý strop, 2 okna, 1 dveøe).
/// Pøi spuštìní scény smaže starý dollhouse a vytvoøí nový.
/// </summary>
public class StaticDollhouseBuilder : MonoBehaviour
{
    [Header("Room Parameters")]
    public float width = 4f;
    public float length = 5f;
    public float wallHeight = 2.5f;
    public float ceilingSlope = 0.5f; // Výška rozdílu šikmého stropu
    public Material wallMaterial;
    public Material windowMaterial;
    public Material doorMaterial;
    public Color wallColor = new Color(0.6f, 0.6f, 0.7f, 0.8f);
    public Color windowColor = new Color(0.2f, 0.5f, 1.0f, 0.6f);
    public Color doorColor = new Color(0.6f, 0.3f, 0.1f, 0.8f);
    public string dollhouseRootName = "Dollhouse_Static";

    void Start()
    {
        RemoveOldDollhouse();
        BuildDollhouse();
    }

    void RemoveOldDollhouse()
    {
        var old = GameObject.Find(dollhouseRootName);
        if (old != null)
        {
            DestroyImmediate(old);
        }
    }

    void BuildDollhouse()
    {
        var root = new GameObject(dollhouseRootName);
        root.transform.SetParent(transform, false);

        // Floor boundary (4x5m)
        var floor = new List<Vector3>
        {
            new Vector3(-width/2, 0, -length/2),
            new Vector3(width/2, 0, -length/2),
            new Vector3(width/2, 0, length/2),
            new Vector3(-width/2, 0, length/2)
        };

        // Ceiling boundary (šikmý strop)
        var ceiling = new List<Vector3>
        {
            new Vector3(-width/2, wallHeight, -length/2),
            new Vector3(width/2, wallHeight + ceilingSlope, -length/2),
            new Vector3(width/2, wallHeight + ceilingSlope, length/2),
            new Vector3(-width/2, wallHeight, length/2)
        };

        // Walls (double-sided)
        CreateWall(root.transform, floor[0], floor[1], ceiling[1], ceiling[0], wallMaterial, wallColor); // Front
        CreateWall(root.transform, floor[1], floor[2], ceiling[2], ceiling[1], wallMaterial, wallColor); // Right
        CreateWall(root.transform, floor[2], floor[3], ceiling[3], ceiling[2], wallMaterial, wallColor); // Back
        CreateWall(root.transform, floor[3], floor[0], ceiling[0], ceiling[3], wallMaterial, wallColor); // Left

        // Floor
        CreateQuad(root.transform, floor, wallMaterial, wallColor, "Floor");
        // Ceiling
        CreateQuad(root.transform, ceiling, wallMaterial, wallColor, "Ceiling");

        // Door (front wall, center)
        CreateBox(root.transform, new Vector3(0, 1.0f, -length/2), new Vector3(0.9f, 2.0f, 0.1f), doorMaterial, doorColor, "Door");
        // Window 1 (back wall, left)
        CreateBox(root.transform, new Vector3(-width/4, 1.5f, length/2), new Vector3(1.2f, 1.0f, 0.1f), windowMaterial, windowColor, "Window1");
        // Window 2 (back wall, right)
        CreateBox(root.transform, new Vector3(width/4, 1.5f, length/2), new Vector3(1.2f, 1.0f, 0.1f), windowMaterial, windowColor, "Window2");
    }

    void CreateWall(Transform parent, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, Material mat, Color color)
    {
        var go = new GameObject("Wall");
        go.transform.SetParent(parent, false);
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { bl, br, tr, tl, br, bl, tl, tr };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2, 4, 6, 5, 4, 7, 6 }; // double-sided
        mesh.RecalculateNormals();
        go.AddComponent<MeshFilter>().mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = mat != null ? mat : new Material(Shader.Find("QuestHouse/UnlitColor"));
        mr.material.color = color;
    }

    void CreateQuad(Transform parent, List<Vector3> verts, Material mat, Color color, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        go.AddComponent<MeshFilter>().mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = mat != null ? mat : new Material(Shader.Find("QuestHouse/UnlitColor"));
        mr.material.color = color;
    }

    void CreateBox(Transform parent, Vector3 pos, Vector3 size, Material mat, Color color, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        var mr = go.GetComponent<MeshRenderer>();
        mr.material = mat != null ? mat : new Material(Shader.Find("QuestHouse/UnlitColor"));
        mr.material.color = color;
    }
}
