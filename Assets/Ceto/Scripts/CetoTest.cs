using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//OYM:  用来取出内部的物品
public  class CetoTest : MonoBehaviour
{
    static CetoTest test;
    public List<Texture> pictureList;

    static void initial()
    {
        if(test==null)
        {
            GameObject go = new GameObject("Test");
            DontDestroyOnLoad(go);
            test = go.AddComponent<CetoTest>();
            test.pictureList = new List<Texture>();
        }
    }

    static public void AddPicture(Texture pic)
    {
        if (test==null)
        {
            initial();
        }
        test.pictureList.Add(pic);
    }
}
