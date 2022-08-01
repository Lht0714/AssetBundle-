using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;

public class Util
{
    public static string[] GetPathAllFiles(string path, string[] screenList, bool flag = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        string[] allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        if (flag)
        {
            return allFiles.Where((f) => screenList.Contains(Path.GetExtension(f))).ToArray();
        }
        else
        {
            return allFiles.Where((f) => !screenList.Contains(Path.GetExtension(f))).ToArray();
        }
    }
}
public class AssetBundleEditor : Editor
{
    public static string outPath = Application.streamingAssetsPath + "/" + Application.version;
    [MenuItem("AB包/选择打包")]
    public static void SelectPack()
    {
        outPath = Application.streamingAssetsPath + "/" + Application.version;
        IsHasPath(outPath);
        string path = GetSelectedPath().Replace("Assets/", "");
        PackAssetBundle(path, true);
        BuildPipeline.BuildAssetBundles(outPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);

        CreatDepend();
        Process.Start(outPath);
    }
    [MenuItem("AB包/打开p目录")]
    public static void OpendPer()
    {
        Process.Start(Application.persistentDataPath);
    }
    private static void PackAssetBundle(string path, bool v)
    {
        string packPath = Application.dataPath + "/" + path;
        string[] allFiles = Util.GetPathAllFiles(packPath, new string[] { ".meta" });
        StringBuilder sb = new StringBuilder();
        foreach (var item in allFiles)
        {
            string f = item.Replace(@"\", "/");
            string f1 = f.Replace(Application.dataPath, "Assets");
            string abName = f1.Substring(f1.LastIndexOf('/') + 1).Split('.')[0];
            AssetImporter importer = AssetImporter.GetAtPath(f1);
            importer.assetBundleName = abName;
            importer.assetBundleVariant = "u3d";

            sb.Append(abName + "." + importer.assetBundleVariant + "/" + GetMD5(item) + "\r\n");
        }
        SaveResABList(sb.ToString());
        SaveGameVersion();
    }

    private static void SaveGameVersion()
    {
        File.WriteAllText(Application.streamingAssetsPath + "/GameVersion.txt", Application.version);
    }

    private static void SaveResABList(string content)
    {
        File.WriteAllText(Application.streamingAssetsPath + "/ResABList.txt", content);
    }

    private static string GetMD5(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        byte[] mdata = md5.ComputeHash(data);
        return BitConverter.ToString(mdata).Replace("-", "");
    }
    private static string GetSelectedPath()
    {
        foreach (var item in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
        {
            string path = AssetDatabase.GetAssetPath(item);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (Directory.Exists(path))
            {
                return path;
            }
        }
        return "Resources";
    }
    private static void IsHasPath(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }



    private static void CreatDepend()
    {
        string mfPath = Application.streamingAssetsPath + "/" + Application.version + "/" + Application.version;
        AssetBundle assetBundle = AssetBundle.LoadFromFile(mfPath);
        AssetBundleManifest abManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        string[] abList = abManifest.GetAllAssetBundles();

        StringBuilder sb = new StringBuilder();
        
        foreach (var item in abList)
        {
            string depend = "";
            string[] str = abManifest.GetAllDependencies(item);
            foreach (var it in str)
            {
                depend += it+"#";
            }
            sb.Append(item + "/" + depend+"\r\n");
        }
        File.WriteAllText(Application.streamingAssetsPath + "/" + "Depend.txt", sb.ToString());
    }
}
