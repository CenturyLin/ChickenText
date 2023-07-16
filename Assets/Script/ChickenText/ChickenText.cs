using System;
using System.Collections;
using UnityEngine;
using System.Text;
using UnityEngine.UI;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(Canvas))]
public class ChickenText : Text
{
    public static string fontMatPath = "TextMat/";

    public string OrigionText
    {
        get { return origionText; }
        set { SetOriginText(value); }
    }

    [TextArea(3, 10)] [SerializeField] protected string origionText = string.Empty;

    [SerializeField] protected bool areaMaskUV = true;

    int[] charMatUse = new int[0];

    List<string> matList = new List<string>();

    Dictionary<string, int> findMatIndex = new Dictionary<string, int>();

    Dictionary<int, Vector2> uv1Dic = new Dictionary<int, Vector2>();

    Dictionary<string, ChickenSubText> subTextDic = new Dictionary<string, ChickenSubText>();

    Dictionary<string, Material> cacheMatDic = new Dictionary<string, Material>();

    private bool UseAsyncLoad
    {
        get
        {
            if (Application.isPlaying)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public override Material materialForRendering
    {
        get
        {
            if (material != null)
            {
                return material;
            }
            else if (font != null && font.material != null)
            {
                return font.material;
            }
            else
            {
                return null;
            }
        }
    }

    public void SetOriginText(string str)
    {
        origionText = str;
        UpdateTextFromOriginText();
    }

    public void UpdateTextFromOriginText()
    {
        text = ParseOriginText(origionText);
        StartLoadMat(); 
        SetSubMesh();
    }

    public void UpdateSubTextFontTex()
    {
        ClearAll();
        UpdateTextFromOriginText();
    }

    public void ClearAll()
    {
        ClearHelpData();
        ClearSubText(); 
        ClearMatCache();
    }

    public void ClearHelpData()
    {
        matList.Clear(); 
        findMatIndex.Clear();
        uv1Dic.Clear();
        
        textSb.Clear(); 
        areaMatIndexStack.Clear(); 
        areaStartIndexStack.Clear();

        emptySubText.Clear();
    }

    public void ClearMatCache()
    {
        foreach (var mat in cacheMatDic.Values)
        {
            if (mat != null && mat != materialForRendering)
            {
                DestoryObject(mat);
            }
        }

        cacheMatDic.Clear();
    }

    public void ClearSubText()
    {
        foreach (var subText in subTextDic.Values)
        {
            if (subText != null && subText.gameObject != null)
            {
                DestoryObject(subText.gameObject);
            }
        }

        subTextDic.Clear();
    }

    protected override void Awake()
    {
        gameObject.GetComponent<Canvas>().additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;

        ChickenSubText[] subTextObjects = GetComponentsInChildren<ChickenSubText>();
        for (int i = 0; i < subTextObjects.Length; i++)
        {
            string name = "" + i;
            subTextDic.Add(name, subTextObjects[i]);
        }

        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateTextFromOriginText();
    }

    protected override void OnDisable()
    {
        StopAllCoroutines();

        ClearHelpData();
        ClearSubText();

        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        ClearAll(); 
        base.OnDestroy();
    }

    UIVertex[] tempVertexQuad = new UIVertex[4] { new UIVertex(), new UIVertex(), new UIVertex(), new UIVertex() };
    UIVertex zeroVertex = new UIVertex() { position = Vector3.zero };

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        base.OnPopulateMesh(toFill);

        foreach (var subText in subTextDic.Values)
        {
            subText.ClearVerticeData();
        }

        for (int i = 0; i < text.Length; i++)
        {
            int matIndex = charMatUse[i];
            if (matIndex > -1)
            {
                string matName = matList[matIndex];
                if (subTextDic.ContainsKey(matName))
                {
                    var subText = subTextDic[matName];

                    if (i * 4 + 3 < cachedTextGenerator.vertexCount)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            int vertexIndex = i * 4 + j;
                            toFill.PopulateUIVertex(ref tempVertexQuad[j], vertexIndex);

                            if (uv1Dic.ContainsKey(vertexIndex))
                            {
                                tempVertexQuad[j].uv1 = uv1Dic[vertexIndex];
                            }

                            toFill.SetUIVertex(zeroVertex, vertexIndex);
                        }

                        subText.AddQuadData(tempVertexQuad);
                    }
                }
            }
        }

        StartCoroutine(SetSubTextDirty());
    }

    IEnumerator SetSubTextDirty()
    {
        yield return null;

        if (subTextDic != null && subTextDic.Count > 0)
        {
            foreach (var subText in subTextDic.Values)
            {
                if (subText != null)
                {
                    subText.SetAllDirty();
                }
            }
        }
    }

    StringBuilder textSb = new StringBuilder(); 
    Stack<int> areaMatIndexStack = new Stack<int>(); 
    Stack<int> areaStartIndexStack = new Stack<int>();
    private string ParseOriginText(string inputText)
    {
        textSb.Clear(); 
        areaMatIndexStack.Clear(); 
        areaStartIndexStack.Clear();

        findMatIndex.Clear();
        matList.Clear(); 
        uv1Dic.Clear();

        if (inputText.Length < 1)
        {
            return inputText;
        }

        if (charMatUse.Length < inputText.Length)
        {
            Array.Resize(ref charMatUse, 2 * inputText.Length);
        }

        for (int i = 0; i < inputText.Length; i++)
        {
            int end; 
            string tagString;

            char c = inputText[i];
            if (c == '<' && GetTag(inputText, i, out end, out tagString))
            {
                int tagLength = end - i + 1; 
                int tagContentLength = end - i - 1;
                string tagContent = inputText.Substring(i + 1, tagContentLength);

                i = end;

                if (tagContent.StartsWith("/"))
                {
                    tagContent = tagContent.Substring(1);

                    if (tagContentLength > 1 && findMatIndex.ContainsKey(tagContent) && areaMatIndexStack.Count > 0 &&
                        findMatIndex[tagContent] == areaMatIndexStack.Peek())
                    {
                        int areaMatIndx = areaMatIndexStack.Pop();
                        EndTagSetUV1(areaMatIndx);
                    }
                    else
                    {
                        AppendStringAndSetMatIndex(tagString, tagLength);
                    }
                }
                else if (tagContentLength > 0)
                {
                    int matIndex = 0;
                    if (findMatIndex.ContainsKey(tagContent))
                    {
                        matIndex = findMatIndex[tagContent];
                    }
                    else
                    {
                        matIndex = matList.Count; 
                        findMatIndex.Add(tagContent, matIndex); 
                        matList.Add(tagContent);
                    }

                    areaMatIndexStack.Push(matIndex);
                    areaStartIndexStack.Push(textSb.Length);
                }
                else
                {
                    AppendStringAndSetMatIndex(tagString, tagLength);
                }
            }
            else
            {
                AppendCharAndSetMatIndex(c);
            }
        }

        while (areaStartIndexStack.Count > 0 && areaMatIndexStack.Count > 0)
        {
            int areaMatIndx = areaMatIndexStack.Pop();
            EndTagSetUV1(areaMatIndx);
        }

        return textSb.ToString();
    }

    private void EndTagSetUV1(int areaMatIndx)
    {
        int areaStart = areaStartIndexStack.Pop();
        int currentCharIndex = textSb.Length - 1;
        float areaDenom = currentCharIndex - areaStart;
        for (int j = areaStart; j <= currentCharIndex; j++)
        {
            if (charMatUse[j] == areaMatIndx)
            {
                int regionIndex = j - areaStart - 1;

                if (!uv1Dic.ContainsKey(j * 4) && !uv1Dic.ContainsKey(j * 4 + 1) 
                    && !uv1Dic.ContainsKey(j * 4 + 2) && !uv1Dic.ContainsKey(j * 4 + 3))
                {
                    if (areaMaskUV)
                    {
                        uv1Dic.Add(j * 4, new Vector2(regionIndex / areaDenom, 1)); 
                        uv1Dic.Add(j * 4 + 1, new Vector2((regionIndex + 1) / areaDenom, 1));
                        uv1Dic.Add(j * 4 + 2, new Vector2((regionIndex + 1) / areaDenom, 0));
                        uv1Dic.Add(j * 4 + 3, new Vector2(regionIndex / areaDenom, 0));
                    }
                    else
                    {
                        uv1Dic.Add(j * 4, new Vector2(0, 1));
                        uv1Dic.Add(j * 4 + 1, new Vector2(1, 1));
                        uv1Dic.Add(j * 4 + 2, new Vector2(1, 0));
                        uv1Dic.Add(j * 4 + 3, new Vector2(0, 0));
                    }
                }
            }
        }
    }

    private void AppendCharAndSetMatIndex(char c)
    {
        textSb.Append(c);
        charMatUse[textSb.Length - 1] = areaMatIndexStack.Count != 0 ? areaMatIndexStack.Peek() : -1;
    }

    private void AppendStringAndSetMatIndex(string s, int length)
    {
        int startIndex = textSb.Length;

        textSb.Append(s);

        for (int i = 0; i < length; i++)
        {
            charMatUse[startIndex + i] = areaMatIndexStack.Count != 0 ? areaMatIndexStack.Peek() : -1;
        }
    }

    private bool GetTag(string matText, int start, out int end, out string tagContent)
    {
        tagContent = "";
        end = 0;

        if (start + 1 >= matText.Length)
        {
            return false;
        }

        for (int i = start + 1; i < matText.Length; i++)
        {
            char c = matText[i];
            if (c == '>')
            {
                end = i; 
                tagContent = matText.Substring(start, i - start + 1);
                return true;
            }
        }

        return false;
    }



    private void StartLoadMat()
    {
        if (matList.Count < 1)
        {
            return;
        }

        foreach (var matName in matList)
        {
            if (!cacheMatDic.ContainsKey(matName))
            {
                cacheMatDic.Add(matName, null);

                void load_callback(UnityEngine.Object ob)
                {
                    Material mat = ob as Material;
                    if (mat == null)
                    {
                        return;
                    }

                    if (!cacheMatDic.ContainsKey(matName))
                    {
                        cacheMatDic.Add(matName, mat);
                    }
                    else
                    {
                        cacheMatDic[matName] = mat;
                    }

                    if (subTextDic != null)
                    {
                        foreach (var subText in subTextDic.Values)
                        {
                            if (subText.SubTextMaterialName == matName)
                            {
                                subText.SetMaterial(mat);
                            }
                        }
                    }
                }

                string matPath = fontMatPath + matName;

                if (UseAsyncLoad)
                {
                    AssetLoader.Instance.LoadAssetAsync<Material>(matPath, load_callback);
                }
                else
                {
                    var loadMat = AssetLoader.Instance.LoadAssetSync<Material>(matPath);
                    if (loadMat != null)
                    {
                        load_callback(loadMat);
                    }
                }
            }
        }
    }

    List<string> emptySubText = new List<string>(); 
    private void SetSubMesh()
    {
        foreach (var pair in subTextDic)
        {
            var subText = pair.Value;
            if (!findMatIndex.ContainsKey(subText.SubTextMaterialName))
            {
                emptySubText.Add(pair.Key);
            }
        }

        foreach (var matName in findMatIndex.Keys)
        {
            var cacheMat = cacheMatDic[matName] != null ? cacheMatDic[matName] :  materialForRendering;
            if (!subTextDic.ContainsKey(matName))
            {
                if (emptySubText.Count > 0)
                {
                    int emptyIndex = emptySubText.Count - 1; 
                    string emptySubMeshName = emptySubText[emptyIndex];

                    var subText = subTextDic[emptySubMeshName]; 
                    subText.SetMaterial(cacheMat); 
                    subText.SubTextMaterialName = matName; 
                    subText.MainTex = mainTexture;

                    emptySubText.RemoveAt(emptyIndex);
                    subTextDic.Remove(emptySubMeshName);
                    subTextDic.Add(matName, subText);
                }
                else
                {
                    ChickenSubText cst = ChickenSubText.AddSubText(transform, cacheMat);
                    cst.SubTextMaterialName = matName;
                    cst.MainTex = mainTexture; 
                    subTextDic.Add(matName, cst);
                }
            }
            else
            {
                if (subTextDic[matName] == null)
                {
                    ChickenSubText cst = ChickenSubText.AddSubText(transform, cacheMat); 
                    cst.SubTextMaterialName = matName; 
                    cst.MainTex = mainTexture; 
                    subTextDic[matName] = cst;
                }
            }
        }

        if (emptySubText.Count > 0)
        {
            foreach (var matName in emptySubText)
            {
                if (subTextDic.ContainsKey(matName))
                {
                    DestoryObject(subTextDic[matName].gameObject);
                    subTextDic.Remove(matName);
                }
            }

            emptySubText.Clear();
        }
    }

    private void DestoryObject(UnityEngine.Object ob)
    {
        if (ob == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(ob);
        }
        else
        {
            DestroyImmediate(ob);
        }
    }

    public void GetCacheMatData(ref List<string> key, ref List<Material> value)
    {
        foreach (var pair in cacheMatDic)
        {
            key.Add(pair.Key); 
            value.Add(pair.Value);
        }
    }

#if UNITY_EDITOR
    private void DisplayMatRefArray()
    {
        string displaystr = "";
        for (int i = 0; i < textSb.Length; i++)
        {
            displaystr += charMatUse[i] + " ";
        }

        Debug.LogWarning("MatArray: "+ displaystr);
    }
#endif
}

