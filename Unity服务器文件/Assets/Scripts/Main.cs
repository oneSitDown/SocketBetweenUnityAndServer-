//========================================================
//作者:#AuthorName#
//创建时间:#CreateTime#
//备注:
//========================================================
using proto;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginAccount
{
    public int Type;

    public string UserName;

    public string Pwd;
}

public class Main : MonoBehaviour {
    ReqLogin req;
    // Use this for initialization
    void Start () {
        NetWorkSocket.Instance.OnConnected("127.0.0.1", 7777);
        req = new ReqLogin();
        req.account = "123";
        req.password = "456";

        NetWorkSocket.Instance.SendMsg(req);
       
    }

    // Update is called once per frame
    void Update () {

		if(Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("按下了空格");
            for (int i = 0; i < 10; i++)
            {
                NetWorkSocket.Instance.SendMsg(req);
            }
        }
	}
}
