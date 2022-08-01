using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Image image;
    void Start()
    {
        //GameObject go = Instantiate(ABManage.Ins.LoadAsset<GameObject>("cube"));
        //SpriteAtlas atlas = ABManage.Ins.LoadAsset<SpriteAtlas>("atlas");
        //image.sprite = atlas.GetSprite("0");
    }
    public static void ABMangerInit()
    {
        ABManage.Ins.Init();
    }


    /// <summary>
    /// 实例化物体
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Object CreatInstateMode(string name)
    {
        var obj = Instantiate(ABManage.Ins.LoadAsset<GameObject>(name));
        return obj;
    }

    /// <summary>
    /// 加载图集
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static SpriteAtlas LoadMode(string name)
    {
        SpriteAtlas obj = ABManage.Ins.LoadAsset<SpriteAtlas>(name);
        return obj;
    }
}
