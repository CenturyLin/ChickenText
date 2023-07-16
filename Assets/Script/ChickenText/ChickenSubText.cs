using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[ExecuteAlways]
public class ChickenSubText : MaskableGraphic
{
    private Texture mainTex;
    public Texture MainTex
    {
        get
        {
            if (mainTex == null)
                mainTex = mainTexture;

            return mainTex;
        }
        set { mainTex = value; }
    }

    public string SubTextMaterialName { get; set; } = string.Empty;

    private List<UIVertex> vertsData;
    private List<int> indicesData;

    public static ChickenSubText AddSubText(Transform parent, Material defaultMat)
    {
        var go = new GameObject();
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity; 
        go.transform.localScale = Vector3.one; 
        go.layer = parent.gameObject.layer;

        var subMesh = go.AddComponent<ChickenSubText>();
        subMesh.SetMaterial(defaultMat);

        go.AddComponent<CanvasRenderer>();

        return subMesh;
    }

    public void SetMaterial(Material material)
    {
        if (material == null)
        {
            gameObject.name = "ChickenSubText_MatNull"; 
            this.material = material; 
            return;
        }

        gameObject.name = "ChickenSubText_" + material.name; // + ''+ DateTime.Now.ToString()

        Texture fontTex = material.GetTexture("_MainTex");
        if (fontTex != null)
        {
            if ((int) fontTex.graphicsFormat == 54)
            {
                material.SetVector("_TextureSampleAdd", new Vector4(1,1,1, 0));
            }
            else
            {
                material.SetVector("_TextureSampleAdd", new Vector4(0, 0, 0, 0));
            }
        }

        this.material = material;
    }

    public void AddQuadData(UIVertex[] verts)
    {
        if (verts == null)
        {
            Debug.LogError("ChickenSubText AddQuadData verts null");
        }

        if (verts.Length != 4)
        {
            Debug.LogError("ChickenSubText AddQuadData verts.Length; " + verts.Length);
            return;
        }

        int indicesStartIndex = vertsData.Count;
        indicesData.Add(indicesStartIndex);
        indicesData.Add(indicesStartIndex + 1);
        indicesData.Add(indicesStartIndex + 2);
        indicesData.Add(indicesStartIndex + 2);
        indicesData.Add(indicesStartIndex + 3);
        indicesData.Add(indicesStartIndex);

        foreach (var vert in verts)
        {
            vertsData.Add(vert);
        }
    }

    public void ClearVerticeData()
    {
        vertsData.Clear(); 
        indicesData.Clear();
    }

    protected override void UpdateMaterial()
    {
        if (!IsActive())
        {
            return;
        }

        canvasRenderer.materialCount = 1;
        canvasRenderer.SetMaterial(material,0);
        canvasRenderer.SetTexture(MainTex);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        vh.AddUIVertexStream(vertsData, indicesData);
    }

    protected override void Awake()
    {
        base.Awake();

        vertsData = new List<UIVertex>();
        indicesData = new List<int>();

        var tran = GetComponent<RectTransform>(); 
        tran.sizeDelta = Vector2.zero;
    }

    protected override void OnDestroy()
    {
        MainTex = null; 
        ClearVerticeData();

        base.OnDestroy();
    }
}
