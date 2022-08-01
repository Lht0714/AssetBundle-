using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ABManage : Singleton<ABManage>
{
    //资源缓存
    Dictionary<string, MyAssetBundle> ABCache = new Dictionary<string, MyAssetBundle>();
    //AB资源的依赖项关系
    Dictionary<string, string[]> dependDic = new Dictionary<string, string[]>();

    string path;

    public void Init()
    {
        path = Application.persistentDataPath;

        InitDepend();
    }

    private void InitDepend()
    {
        string allStr = File.ReadAllText(Application.persistentDataPath + "/" + "Depend.txt");
        string[] all = allStr.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);
        foreach (var item in all)
        {
            string[] zy = item.Split('/');
            if (zy.Length > 1 && zy[1] != "")
            {
                string[] depend = zy[1].Split('#');
                string[] newdepend = new string[depend.Length - 1];
                for (int i = 0; i < newdepend.Length; i++)
                {
                    newdepend[i] = depend[i];
                }
                dependDic.Add(zy[0], newdepend);
            }
        }
    }

    public T LoadAsset<T>(string abName, int num = 0) where T : UnityEngine.Object
    {
        string bName = abName.ToLower() + ".u3d";
        if (dependDic.ContainsKey(bName))
        {
            string[] dependList = dependDic[bName];
            foreach (var item in dependList)
            {

                if (item != "")
                {
                    LoadAssetBundle(item);
                }
            }
        }
        MyAssetBundle ab = LoadAssetBundle(bName);
        return ab.ab.LoadAllAssets<T>()[num];
    }

    MyAssetBundle LoadAssetBundle(string abName)
    {
        string bName = this.path + "/" + abName;
        if (ABCache.ContainsKey(abName))
        {
            ABCache[abName].count++;
            return ABCache[abName];
        }
        else
        {
            AssetBundle ab = AssetBundle.LoadFromFile(bName);
            MyAssetBundle myAsset = new MyAssetBundle(ab);
            myAsset.count++;
            ABCache.Add(abName, myAsset);
            return myAsset;
        }
    }
    public void UnLoad(string abName)
    {
        if (dependDic.ContainsKey(abName))
        {
            var dependList = dependDic[abName];
            foreach (var depend in dependList)
            {
                UnLoadAssetBundle(depend);
            }
        }
        UnLoadAssetBundle(abName);
    }
    /// <summary>
    /// 删除AB包资源
    /// </summary>
    /// <param name="abName"></param>
    void UnLoadAssetBundle(string abName)
    {
        if (AbMode.ContainsKey(abName))
        {
            AbMode[abName].Count--;
            if (AbMode[abName].Count <= 0)
            {
                AbMode[abName].ab.Unload(false);
                AbMode.Remove(abName);
            }
        }
    }
}


public class MyAssetBundle
{
    public int count;
    public AssetBundle ab;

    public MyAssetBundle(AssetBundle ab)
    {
        this.ab = ab;
    }
}

public class Singleton<T> where T : class, new()
{
    private static T ins;
    public static T Ins
    {
        get
        {
            if (ins == null)
            {
                ins = new T();
            }
            return ins;
        }
    }
}