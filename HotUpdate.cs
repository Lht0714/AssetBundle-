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
    string serverPath = "127.0.0.1/LHT";//��Դ�������ĵ�ַ
    Version serverVer;//�������İ汾��
    Version localVer;//�ͻ��˵İ汾��
    Queue<ABItem> downLoadResQue = new Queue<ABItem>();//��Ҫ���£����أ�����Դ

    string serverVersionStr;//�������汾
    string resABListStr;//�������嵥
    Dictionary<string, ABItem> serverABItemList = new Dictionary<string, ABItem>();//����������Դ�б�
    Dictionary<string, ABItem> localABItemList = new Dictionary<string, ABItem>();//�ͻ�����Դ�б�

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
             if (File.Exists(localVerFile))//�ļ����ڣ����Ը�����Դ��
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
             else//�ļ������ڣ���Ҫ������Դ��
            {
                 GameResAllDownLoad();
             }
         });
    }
    //������Դ
    private void GameHotUpdateResDownLoad()
    {
        string path = $"{serverPath}/ResABList.txt";
        DownLoadRes(path, (data) =>
        {
            resABListStr = UTF8Encoding.UTF8.GetString(data);//��������Դ�嵥
            ResToSave(resABListStr, serverABItemList);//���浽�������ֵ�
            string localResABListStr = File.ReadAllText(Application.persistentDataPath + "/ResABList.txt");//������Դ�嵥
            ResToSave(localResABListStr, localABItemList);//���浽�����ֵ�
            CompareResToList();//�Ա�����md5��
            DownloadAsset(downLoadResQue.Dequeue());
        });
    }
    //�Ա�
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
    //����ʱ������Դdic
    private void ResToSave(string resABListStr, Dictionary<string, ABItem> resABList)
    {
        string[] abListStr = resABListStr.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);
        foreach (var item in abListStr)
        {
            ABItem ab = new ABItem(item);
            resABList.Add(ab.resName, ab);
        }
    }

    //������Ϸ
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
    //������Դ
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
            Debug.Log("ItemPath=" + itemPath + "���سɹ���");
            if (downLoadResQue.Count > 0)
            {
                DownloadAsset(downLoadResQue.Dequeue());
            }
            else
            {
                Debug.Log("�������");
                SaveGameVersion();//����汾��
                SaveResABList();//������Դ
                SaveInventory();//������Դ�嵥
            }
        });
    }
    //��Դ�嵥
    private void SaveInventory()
    {
        string path = $"{serverPath}/Depend.txt";
        DownLoadRes(path, (data) =>
        {
            string str = UTF8Encoding.UTF8.GetString(data);
            File.WriteAllText($"{Application.persistentDataPath}/Depend.txt", str);
        });
    }
    //��Դ����
    private void SaveResABList()
    {
        File.WriteAllText($"{Application.persistentDataPath}/ResABList.txt", resABListStr);
    }
    //�汾�ű���
    private void SaveGameVersion()
    {
        File.WriteAllText(Application.persistentDataPath + "/GameVersion.txt", serverVer.ver);
    }

    //��ȡ����
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
        yield return new WaitForSeconds(0.1f);//�ȴ�������
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
