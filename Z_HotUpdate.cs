using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class Z_HotUpdate : MonoBehaviour
{
    /// <summary>
    /// 资源服务器的地址
    /// </summary>
    string serverPath = "127.0.0.1/LHT";

    /// <summary>
    /// 服务器版本号
    /// </summary>
    Version serverVer;

    /// <summary>
    /// 本地版本号
    /// </summary>
    Version localVer;

    /// <summary>
    /// 服务器的资源清单(String)
    /// </summary>
    string resABlistStr;

    /// <summary>
    /// 需要下载的资源队列
    /// </summary>
    Queue<ABItem> downLoadResQue = new Queue<ABItem>();

    /// <summary>
    /// 服务器的资源Item列表
    /// </summary>
    Dictionary<string, ABItem> serverABItemList = new Dictionary<string, ABItem>();

    /// <summary>
    /// 本地的资源Item列表
    /// </summary>
    Dictionary<string, ABItem> localABItemList = new Dictionary<string, ABItem>();

    /// <summary>
    /// 热更新UI
    /// </summary>
    public UIHotUpdata hotUpdata;

    /// <summary>
    /// 加载进度UI
    /// </summary>
    public UILoading loading;

    // Start is called before the first frame update
    void Start()
    {
        DownLoadVersion();
    }
    /// <summary>
    /// 先下载游戏的版本号
    /// </summary>
    private void DownLoadVersion()
    {
        string path = $"{serverPath}/GameVersion.txt";
        DownLoadRes(path, (data) =>
        {
            //服务器的版本文件读取
            string serverVersionStr = Encoding.UTF8.GetString(data);
            serverVer = new Version(serverVersionStr);
            //本地目录的版本文件
            string localVerFile = $"{Application.persistentDataPath}/GameVersion.txt";
            //如果本地目录存在游戏版本文件存在，代表已经下载过此文件
            if (File.Exists(localVerFile))
            {
                string localVersionStr = File.ReadAllText(localVerFile);
                localVer = new Version(localVersionStr);
                //对比两端的版本号是否不同
                if (serverVer.middle != localVer.middle)
                {
                    //更新游戏资源

                }
                else
                {
                    if (serverVer.small != localVer.small)
                    {
                        hotUpdata.gameObject.SetActive(true);
                        //更新游戏资源
                        hotUpdata.actionComfirm += GameHotUpdataResDownLoad;
                        //
                        hotUpdata.actionCancel += InGame;
                    }
                }

            }
            //没有下载过游戏文件
            else
            {
                //整体游戏资源下载
                GameResAllDownLoad();
            }

        });
    }

    private void InGame()
    {
        hotUpdata.gameObject.SetActive(false);
    }



    /// <summary>
    /// 热更新资源
    /// </summary>
    private void GameHotUpdataResDownLoad()
    {
        hotUpdata.gameObject.SetActive(false);
        loading.gameObject.SetActive(true);

        string path = $"{serverPath}/ResABList.txt";
        //读取服务器的资源清单
        DownLoadRes(path, (data) =>
        {
            //服务器的资源清单
            resABlistStr = Encoding.UTF8.GetString(data);
            //服务器的资源Item存储
            ResToSave(resABlistStr, serverABItemList);
            //本地的资源清单
            string localResABListStr = File.ReadAllText(Application.persistentDataPath + "/ResABList.txt");
            //本地的资源Item存储
            ResToSave(localResABListStr, localABItemList);
            //对比两端的资源清单，筛选出不同MD5的需要下载的资源
            CompareResToList();
            loading.maxCount = downLoadResQue.Count;
            //下载AB包资源
            DownLoadAsset(downLoadResQue.Dequeue());
        });
    }
    /// <summary>
    /// 对比两端的资源清单，筛选出不同MD5的需要下载的资源
    /// </summary>
    private void CompareResToList()
    {
        foreach (var abItem in serverABItemList)
        {
            //本地的资源清单含有这个资源
            if (localABItemList.ContainsKey(abItem.Key))
            {
                //相同的资源，不同的MD5，才会加入下载队列
                if (abItem.Value.md5 != localABItemList[abItem.Key].md5)
                {
                    downLoadResQue.Enqueue(abItem.Value);
                }
            }
            //本地的资源清单不含有这个资源，代表是新的需要更新的资源
            else
            {
                downLoadResQue.Enqueue(abItem.Value);
            }
        }
    }

    /// <summary>
    /// 服务器与本地的资源Item存储
    /// </summary>
    /// <param name="resStr"></param>
    /// <param name="resABList"></param>
    void ResToSave(string resStr, Dictionary<string, ABItem> resABList)
    {
        //去掉清单字符串的头尾部的空格部分，并截取换行
        string[] abListStr = resStr.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

        foreach (var item in abListStr)
        {
            ABItem ab = new ABItem(item);
            resABList.Add(ab.resName, ab);
        }
    }

    /// <summary>
    /// 整体游戏资源下载
    /// </summary>
    private void GameResAllDownLoad()
    {
        string path = $"{serverPath}/ResABList.txt";
        //读取服务器的资源清单
        DownLoadRes(path, (data) =>
        {
            resABlistStr = Encoding.UTF8.GetString(data);
            //去掉清单字符串的头尾部的空格部分，并截取换行
            string[] abListStr = resABlistStr.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

            foreach (var item in abListStr)
            {
                ABItem ab = new ABItem(item);
                downLoadResQue.Enqueue(ab);
            }
            DownLoadAsset(downLoadResQue.Dequeue());
        });
    }
    /// <summary>
    /// 
    /// (x)=>{
    /// 
    /// }  == function()
    /// 
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="onComplete"></param>
    /// <param name="onError"></param>
    void DownLoadRes(string path, Action<byte[]> onComplete, Action onError = null)
    {
        StartCoroutine(WebDownLoad(path, onComplete, onError));
    }
    /// <summary>
    /// 网络下载资源数据
    /// </summary>
    /// <param name="path">请求下载的路径</param>
    /// <param name="onComplete">下载成功的数据回调执行</param>
    /// <param name="onError">错误提示的回调执行</param>
    /// <returns></returns>
    IEnumerator WebDownLoad(string path, Action<byte[]> onComplete, Action onError = null)
    {
        //Http 网络请求访问下载) Url
        UnityWebRequest web = UnityWebRequest.Get(path);

        UnityWebRequestAsyncOperation op = web.SendWebRequest();

        op.completed += (x) =>
        {
            //网络读取错误
            if (web.isHttpError || web.isNetworkError)
            {
                return;
            }
            //资源访问下载成功
            if (op.isDone)
            {
                onComplete?.Invoke(web.downloadHandler.data);
                return;
            }
        };
        yield return new WaitForSeconds(0.1f);
    }
    float loadIndex = 0;
    /// <summary>
    /// 下载AB包资源
    /// </summary>
    /// <param name="item"></param>
    void DownLoadAsset(ABItem item)
    {
        string path = $"{serverPath}/{serverVer.ver}/{item.resName}";
        //path = 路径，下载的是什么资源
        //(data)=>{} 资源下载完成后执行的方法 data == 就是下载完资源数据
        DownLoadRes(path, (data) =>
        {
            //本地下载的游戏资源(移动端)
            string itemPath = $"{Application.persistentDataPath}/{item.resName}";
            //资源是否存在，存在删除
            if (File.Exists(itemPath))
            {
                File.Delete(itemPath);
            }
            //获取资源的前面的文件夹路径
            string filePath = Path.GetDirectoryName(itemPath);
            //文件夹路径不存在
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            //将读取成功的数据写入硬盘路径内
            File.WriteAllBytes(itemPath, data);
            loadIndex++;
            Debug.Log("itemPath = " + itemPath + "  加载成功 --" + loadIndex);
            loading.currCount = loadIndex;

            if (downLoadResQue.Count > 0)
            {
                DownLoadAsset(downLoadResQue.Dequeue());
            }
            else
            {
                //loading.gameObject.SetActive(false);
                SaveGameVersion();
                SaveGameABList();
                Debug.Log("资源加载完成");
            }
        });
    }
    /// <summary>
    /// 更新最新版本的版本号
    /// </summary>
    void SaveGameVersion()
    {
        File.WriteAllText(Application.persistentDataPath + "/GameVersion.txt", serverVer.ver);
    }
    /// <summary>
    /// 更新最新版本资源清单
    /// </summary>
    void SaveGameABList()
    {
        File.WriteAllText(Application.persistentDataPath + "/ResABList.txt", resABlistStr);
    }
}

/// <summary>
/// 版本号
/// </summary>
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
/// <summary>
/// AB包资源数据
/// </summary>
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