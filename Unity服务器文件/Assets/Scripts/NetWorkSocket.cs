//========================================================
//作者:#AuthorName#
//创建时间:#CreateTime#
//备注:
//========================================================
using proto;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class NetWorkSocket : MonoBehaviour {

    #region 单例
    private static NetWorkSocket instance;

    public static NetWorkSocket Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject http = GameObject.Find("NetWorkSocket");
                if (http == null)
                {
                    http = new GameObject("NetWorkSocket");
                    instance = http.AddComponent<NetWorkSocket>();
                }
                else
                {
                    instance = http.GetComponent<NetWorkSocket>();
                }
                DontDestroyOnLoad(http);
            }
            return instance;
        }
    }
    #endregion
    #region 连接
    private Socket m_Client;

    public void OnConnected(string ip,int port)
    {

        if (m_Client != null && m_Client.Connected) return;

        m_Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            m_Client.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
            m_ClientSendMessageCallBack = SendMsgCallBack;
            ReceiveMessage();
        }
       catch(Exception e)
        {
            Debug.Log(e.Message);
        }
    }
    #endregion
    #region 发送数据

    private Queue<byte[]> m_SendMessageQueue = new Queue<byte[]>();

    private Action m_ClientSendMessageCallBack;

    private void SendMsgCallBack()
    {
        lock(m_SendMessageQueue)
        {
            if (m_SendMessageQueue.Count > 0 && m_Client != null && m_Client.Connected)
            {
                //发送消息
                SendMessageByClient(m_SendMessageQueue.Dequeue());
            }
        }
    }

    private void SendMessageByClient(byte[] msg)
    {
        m_Client.BeginSend(msg,0, msg.Length,SocketFlags.None, MessageSendCallBack, m_Client);
    }

    private void MessageSendCallBack(IAsyncResult ar)
    {
        int length= m_Client.EndSend(ar);

        SendMsgCallBack();
    }

    private byte[] MakeData(byte[] data,Type type)
    {

        string TypeName = type.Name;

        byte[] bodylength = BitConverter.GetBytes((ushort)data.Length);

        byte[] typeName = Encoding.UTF8.GetBytes(TypeName);

        byte[] typeNamelength = BitConverter.GetBytes((ushort)typeName.Length);
        //包体类型为包体长度+protobuf类名长度+类名+包体
        byte[] AllMessage = new byte[bodylength.Length + typeNamelength.Length + typeName.Length + data.Length];

        Array.Copy(bodylength, 0, AllMessage, 0, bodylength.Length);

        Array.Copy(typeNamelength, 0, AllMessage, bodylength.Length, typeNamelength.Length);

        Array.Copy(typeName, 0, AllMessage, bodylength.Length+ typeNamelength.Length, typeName.Length);

        Array.Copy(data, 0, AllMessage, bodylength.Length + typeNamelength.Length+ typeName.Length, data.Length);

        return AllMessage;
    }

    public void SendMsg(IExtensible msg)
    {
        byte[] bytemsg= ProtobufUntil.Serialize(msg);
        byte[] data= MakeData(bytemsg, msg.GetType());

        lock(m_SendMessageQueue)
        {
            m_SendMessageQueue.Enqueue(data);

            m_ClientSendMessageCallBack.BeginInvoke(null, null);
        }
    }
    #endregion
    #region 接收数据
    private byte[] buffer = new byte[10240];

    private void ReceiveMessage()
    {
        m_Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallBack, m_Client);
    }
    byte[] acheByte;
    private void ReceiveCallBack(IAsyncResult ar)
    {
        Socket tempSocket = ar.AsyncState as Socket;
        try
        {
            int length = tempSocket.EndReceive(ar);
            int startIndex = 0;
            //上一次缓存的不足4个包数据缓存
            if (acheByte != null)
            {
                byte[] data = new byte[acheByte.Length + length];
                Array.Copy(acheByte, 0, data, 0, acheByte.Length);

                Array.Copy(buffer, acheByte.Length, data, 0, length);
                length = acheByte.Length + length;
                buffer = data;
            }


            while (true)
            {
                //至少有一个不完整的包发送过来了
                if (length > 4)
                {
                    ushort bodyLength = BitConverter.ToUInt16(buffer, 0);
                    ushort typeLength = BitConverter.ToUInt16(buffer, 2);
                    //假如取出四位之后剩下的长度不够包体长度
                    if (length - 4 < bodyLength + typeLength)
                    {
                        acheByte = new byte[length];
                        Array.Copy(buffer, startIndex, acheByte, 0, length);
                        break;
                    }
                    byte[] typebyte = new byte[typeLength];
                    Array.Copy(buffer, startIndex+4, typebyte, 0, typeLength);
                    string typename = Encoding.UTF8.GetString(typebyte);
                    startIndex = 4 + bodyLength + typeLength;
                    length -= 4 + bodyLength + typeLength;
                }
                else
                {
                    acheByte = new byte[length];
                    Array.Copy(buffer, startIndex, acheByte, 0, length);
                    break;
                }
            }
            ReceiveMessage();
        }
        catch (Exception e)
        {
            Debug.Log(string.Format("{0}客户端因{1}断开连接", tempSocket.RemoteEndPoint.ToString(), e.Message));
        }

    }
    #endregion

    private void OnDestroy()
    {
        if (m_Client != null && m_Client.Connected)
        {
            m_Client.Shutdown(SocketShutdown.Both);
            m_Client.Close();
        }
    }
}
