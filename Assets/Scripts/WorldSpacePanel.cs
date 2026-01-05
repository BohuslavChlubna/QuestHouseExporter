using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WorldSpacePanel : MonoBehaviour
{
    public float distanceFromCamera = 1.2f;
    public Vector2 panelSize = new Vector2(0.5f, 0.4f);
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    public Color buttonColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color buttonHoverColor = new Color(0.7f, 0.9f, 1f, 1f);

    class PanelButton
    {
        public GameObject go;
        public BoxCollider col;
        public TextMesh text;
        public Action onClick;
        public bool hovered;
    }

    List<PanelButton> buttons = new List<PanelButton>();
    GameObject background;
    LineRenderer laser;

    void Start()
    {
        CreatePanel();
    }

    void CreatePanel()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var root = new GameObject("WorldSpacePanel_Root");
        root.transform.SetParent(transform, false);

        // position in front of camera
        root.transform.position = cam.transform.position + cam.transform.forward * distanceFromCamera;
        root.transform.rotation = Quaternion.LookRotation(root.transform.position - cam.transform.position);

        // background
        background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "WSP_Background";
        background.transform.SetParent(root.transform, false);
        background.transform.localScale = new Vector3(panelSize.x, panelSize.y, 1f);
        var mr = background.GetComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Standard"));
        mr.sharedMaterial.color = backgroundColor;
        DestroyImmediate(background.GetComponent<Collider>());

        // create buttons vertically
        float btnH = 0.08f; float spacing = 0.02f;
        float startY = panelSize.y / 2f - btnH / 2f - 0.05f;

        AddButton(root.transform, new Vector2(0f, startY - 0f * (btnH + spacing)), "Export", OnExportClicked);
        AddButton(root.transform, new Vector2(0f, startY - 1 * (btnH + spacing)), "Toggle View Mode", OnToggleServerClicked);
        AddButton(root.transform, new Vector2(0f, startY - 2 * (btnH + spacing)), "Upload to Drive", OnUploadClicked);
        AddButton(root.transform, new Vector2(0f, startY - 3 * (btnH + spacing)), "Start Google Auth", OnStartAuthClicked);

        // laser
        var lrGo = new GameObject("WSP_Laser");
        lrGo.transform.SetParent(root.transform, false);
        laser = lrGo.AddComponent<LineRenderer>();
        laser.startWidth = 0.0025f; laser.endWidth = 0.001f;
        laser.material = new Material(Shader.Find("Unlit/Color"));
        laser.material.color = Color.cyan;
        laser.positionCount = 2;
    }

    void AddButton(Transform parent, Vector2 localPos, string label, Action onClick)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "WSP_Button_" + label.Replace(' ', '_');
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(panelSize.x * 0.9f, 0.08f, 1f);
        go.transform.localPosition = new Vector3(localPos.x, localPos.y, -0.01f);
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Standard"));
        mr.sharedMaterial.color = buttonColor;

        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 1f, 0.01f);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var tm = txtGO.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.characterSize = 0.01f;
        tm.color = Color.black;

        buttons.Add(new PanelButton() { go = go, col = col, text = tm, onClick = onClick, hovered = false });
    }

    PanelButton currentHover = null;

    public void HandlePointer(Vector3 origin, Vector3 dir, bool primaryEdge)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, 10f))
        {
            laser.enabled = true;
            laser.SetPosition(0, origin);
            laser.SetPosition(1, hit.point);

            var pb = buttons.Find(b => b.col != null && b.col == hit.collider);
            if (pb != null)
            {
                if (currentHover != pb)
                {
                    if (currentHover != null) SetButtonHover(currentHover, false);
                    currentHover = pb;
                    SetButtonHover(currentHover, true);
                }
                if (primaryEdge && currentHover != null)
                {
                    currentHover.onClick?.Invoke();
                }
            }
            else
            {
                if (currentHover != null) { SetButtonHover(currentHover, false); currentHover = null; }
            }
        }
        else
        {
            laser.enabled = false;
            if (currentHover != null) { SetButtonHover(currentHover, false); currentHover = null; }
        }
    }

    void SetButtonHover(PanelButton b, bool hover)
    {
        b.hovered = hover;
        var mr = b.go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial.color = hover ? buttonHoverColor : buttonColor;
    }

    // Button callbacks
    void OnExportClicked() 
    { 
        var se = FindFirstObjectByType<MRUKRoomExporter>(); 
        if (se != null) se.ExportAll(); 
    }
    void OnToggleServerClicked() 
    { 
        var vm = FindFirstObjectByType<ViewModeController>(); 
        if (vm != null) vm.ToggleMode(); 
    }
    void OnUploadClicked() 
    { 
        var se = FindFirstObjectByType<MRUKRoomExporter>(); 
        if (se != null && se.driveUploader != null) 
        { 
            var path = System.IO.Path.Combine(Application.persistentDataPath, se.exportFolder); 
            se.driveUploader.StartUploadDirectory(path); 
        } 
    }
    void OnStartAuthClicked() 
    { 
        var auth = FindFirstObjectByType<GoogleDeviceAuth>(); 
        if (auth != null) auth.StartDeviceAuth(); 
    }
}
