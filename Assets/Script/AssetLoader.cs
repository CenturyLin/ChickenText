using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class AssetLoader
{
    public delegate void LoadAssetCallback(UnityEngine.Object ob);

    private static AssetLoader ins;
    public static AssetLoader Instance
    {
        get
        {
            if (ins == null)
            {
                ins = new AssetLoader();
            }

            return ins;
        }
    }

    private CoroutineHelper ch;

    private CoroutineHelper CH
    {
        get
        {
            if (ch == null)
            {
                GameObject chGo = GameObject.Find("CoroutineHelper");
                if (chGo == null)
                {
                    chGo = new GameObject("CoroutineHelper");
                    ch = chGo.AddComponent<CoroutineHelper>();
                }
                else
                {
                    ch = chGo.GetComponent<CoroutineHelper>();
                    if (ch == null)
                    {
                        ch = chGo.AddComponent<CoroutineHelper>();
                    }
                }
            }

            return ch;
        }
    }

    public T LoadAssetSync<T>(string assetPath) where T :UnityEngine.Object
    {
        T asset = Resources.Load<T>(assetPath);
        if (asset == null)
        {
            return null;
        }

        var assetIns = GameObject.Instantiate(asset);
        assetIns.name = asset.name;
       
        Resources.UnloadAsset(asset);

        return assetIns;
    }

    public void LoadAssetAsync<T>(string assetPath, LoadAssetCallback lac) where T : UnityEngine.Object
    {
        CH.StartCoroutine(AsyncLoadResources<T>(assetPath,lac));
    }

    IEnumerator AsyncLoadResources<T>(string assetPath, LoadAssetCallback lac) where T : UnityEngine.Object
    {
        ResourceRequest rq = Resources.LoadAsync<Material>(assetPath); //"TextMat/TestFontMat"
        while (!rq.isDone)
        {
            yield return null;
        }

        T asset = rq.asset as T;
        if (asset == null)
        {
            lac(null);
            yield break;
        }

        var assetIns = GameObject.Instantiate(asset);
        assetIns.name = asset.name;

        Resources.UnloadAsset(asset);

        lac(assetIns);
    }
}

public class CoroutineHelper : MonoBehaviour
{
    void Start()
    {
        DontDestroyOnLoad(this);
    }
}