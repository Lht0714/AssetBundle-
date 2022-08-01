using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class HotUpdate : MonoBehaviour
{
    string serverPath = "127.0.0.1/LHT";//资源服务器的地址
    Version serverVer;//服务器的版本号
    Version localVer;//客户端的版本号
    Queue<ABItem> downLoadResQue = new Queue<ABItem>();//需要更新（下载）的资源

    string serverVersionStr;//服务器版本
    string resABListStr;//服务器清单
    Dictionary<string, ABItem> serverABItemList = new Dictionary<string, ABItem>();//服务器的资源列表
    Dictionary<string, ABItem> localABItemList = new Dictionary<string, ABItem>();//客户单资源列表

    void Start()
    {
        DownLoadVersion();
    }

    private void DownLoadVersion()
    {
        string path = $"{serverPath}/GameVersion.txt";
        DownLoadRes(path, (data) =>
         {
             serverVersionStr = Encoding.UTF8.GetString(data);
             serverVer = new Version(serverVersionStr);

             string localVerFile = $"{Application.persistentDataPath}/GameVersion.txt";
             if (File.Exists(localVerFile))//文件存在（可以更新资源）
            {
                 string localVersionStr = File.ReadAllText(localVerFile);
                 localVer = new Version(localVersionStr);
                 if (serverVer.middle != localVer.middle)
                 {
                    //GameHotUpdateResDownLoad();
                }
                 else
                 {
                     if (serverVer.small != localVer.small)
                     {
                         GameHotUpdateResDownLoad();
                     }
                 }
             }
             else//文件不存在（需要下载资源）
            {
                 GameResAllDownLoad();
             }
         });
    }
    //更新资源
    private void GameHotUpdateResDownLoad()
    {
        string path = $"{serverPath}/ResABList.txt";
        DownLoadRes(path, (data) =>
        {
            resABListStr = UTF8Encoding.UTF8.GetString(data);//服务器资源清单
            ResToSave(resABListStr, serverABItemList);//保存到服务器字典
            string localResABListStr = File.ReadAllText(Application.persistentDataPath + "/ResABList.txt");//本地资源清单
            ResToSave(localResABListStr, localABItemList);//保存到本地字典
            CompareResToList();//对比两个md5码
            DownloadAsset(downLoadResQue.Dequeue());
        });
    }
    //对比
    private void CompareResToList()
    {
        foreach (var item in serverABItemList)
        {
            if (localABItemList.ContainsKey(item.Key))
            {
                if (item.Value.md5 != localABItemList[item.Key].md5)
                {
                    downLoadResQue.Enqueue(item.Value);
                }
            }
            else
            {
                downLoadResQue.Enqueue(item.Value);
            }
        }
    }
    //更新时保存资源dic
    private void ResToSave(string resABListStr, Dictionary<string, ABItem> resABList)
    {
        string[] abListStr = resABListStr.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);
        foreach (var item in abListStr)
        {
            ABItem ab = new ABItem(item);
            resABList.Add(ab.resName, ab);
        }
    }

    //下载游戏
    private void GameResAllDownLoad()
    {
        string path = $"{serverPath}/ResABList.txt";
        DownLoadRes(path, (data) =>
        {
            resABListStr = UTF8Encoding.UTF8.GetString(data);

            string[] abListStr = resABListStr.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

            foreach (var item in abListStr)
            {
                ABItem ab = new ABItem(item);
                downLoadResQue.Enqueue(ab);
            }
            DownloadAsset(downLoadResQue.Dequeue());
        });
    }
    //加载资源
    private void DownloadAsset(ABItem ab)
    {
        string path = $"{serverPath}/{serverVer.ver}/{ab.resName}";

        DownLoadRes(path, (data) =>
        {
            string itemPath = $"{Application.persistentDataPath}/{ab.resName}";
            if (File.Exists(itemPath))
            {
                File.Delete(itemPath);
            }
            string filePath = Path.GetDirectoryName(itemPath);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            File.WriteAllBytes(itemPath, data);
            Debug.Log("ItemPath=" + itemPath + "加载成功！");
            if (downLoadResQue.Count > 0)
            {
                DownloadAsset(downLoadResQue.Dequeue());
            }
            else
            {
                Debug.Log("加载完毕");
                SaveGameVersion();//保存版本号
                SaveResABList();//保存资源
                SaveInventory();//保存资源清单
            }
        });
    }
    //资源清单
    private void SaveInventory()
    {
        string path = $"{serverPath}/Depend.txt";
        DownLoadRes(path, (data) =>
        {
            string str = UTF8Encoding.UTF8.GetString(data);
            File.WriteAllText($"{Application.persistentDataPath}/Depend.txt", str);
        });
    }
    //资源保存
    private void SaveResABList()
    {
        File.WriteAllText($"{Application.persistentDataPath}/ResABList.txt", resABListStr);
    }
    //版本号保存
    private void SaveGameVersion()
    {
        File.WriteAllText(Application.persistentDataPath + "/GameVersion.txt", serverVer.ver);
    }

    //获取内容
    private void DownLoadRes(string path, Action<byte[]> onComplete)
    {
        StartCoroutine(WebDownLoad(path, onComplete));
    }

    IEnumerator WebDownLoad(string path, Action<byte[]> onComple)
    {
        UnityWebRequest web = UnityWebRequest.Get(path);
        UnityWebRequestAsyncOperation op = web.SendWebRequest();

        Thread.Sleep(200);
        if (web.isHttpError || web.isNetworkError)
        {
            yield return null;
        }
        if (op.isDone)
        {
            onComple?.Invoke(web.downloadHandler.data);
        }
        yield return new WaitForSeconds(0.1f);//等待几秒钟
    }
}

public class Version
{
    public string big;
    public string middle;
    public string small;
    public string ver;
    public Version(string ver)
    {
        this.ver = ver;
        string[] verStr = ver.Split('.');
        this.big = verStr[0];
        this.middle = verStr[1];
        this.small = verStr[2];
    }
}
public class ABItem
{
    public string resName;
    public string md5;
    public ABItem(string resStr)
    {
        string[] resList = resStr.Split('/');
        this.resName = resList[0];
        this.md5 = resList[1];
    }
}
