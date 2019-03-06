using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using proto;
using ProtoBuf;

namespace GameServerApp
{
    public class ClientSocket
    {
        //客户端Socket类
        private Socket m_Socket;
        //接收线程
        Thread m_ReceiveThread;
        //数据缓冲
        private byte[] buffer = new byte[10240];

        #region 构造函数
        public ClientSocket(Socket m_Socket)
        {
            this.m_Socket = m_Socket;

            //接收线程
            m_ReceiveThread = new Thread(ReceiveMessage);

            m_ReceiveThread.Start();
            //发送消息的回调注册
            m_ClientSendMessageCallBack = SendMsgCallBack;

        }
        #endregion
        #region 接收数据
        //开启异步接收数据
        private void ReceiveMessage()
        {
            m_Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallBack, m_Socket);
        }
        //数据缓存
        byte[] acheByte=null;

        //接收数据回调
        private void ReceiveCallBack(IAsyncResult ar)
        {
            Socket tempSocket = ar.AsyncState as Socket;
            try
            {
                //这里返回的是接收到的数据的长度
                int length = tempSocket.EndReceive(ar);
                //初始化读取位置
                int startIndex = 0;
                //上一次的数据缓存不为空的话就拷贝缓存数据到当前缓冲区的前部
                if(acheByte!=null)
                {
                    //数据长度为上次缓存的数据长度加上这次接收到的数据长度
                    byte[] data = new byte[acheByte.Length + length];
                    //拷贝数组
                    Array.Copy(acheByte, 0, data, 0, acheByte.Length);
                    Array.Copy(buffer, acheByte.Length, data, 0, length);
                    //设置本次要读取的长度
                    length = acheByte.Length + length;
                    buffer = data;
                }

                
                while(true)
                {
                    //至少有一个不完整的包发送过来了
                    if (length>4)
                    {
                        //取出前两位为包体长度
                        ushort bodyLength = BitConverter.ToUInt16(buffer, 0);
                        //取出第3，4位为protobuf类名长度
                        ushort typeLength = BitConverter.ToUInt16(buffer, 2);
                        //假如取出四位之后剩下的长度不够包体长度就缓存起来等待下次接收
                        if(length-4< bodyLength+typeLength)
                        {
                            acheByte = new byte[length];
                            Array.Copy(buffer, startIndex, acheByte, 0, length);
                            break;
                        }
                        //建立类名数组缓存区
                        byte[] typebyte = new byte[typeLength];
                        Array.Copy(buffer, startIndex+4, typebyte, 0, typeLength);
                        //转换得到类名字符串
                        string typename = Encoding.UTF8.GetString(typebyte);
                        //打印类名
                        Console.WriteLine(typename);
                        //建立包体缓冲区
                        byte[] bodybyte = new byte[bodyLength];
                        Array.Copy(buffer, startIndex + 4+typeLength, bodybyte, 0, bodyLength);
                        //根据类名反序列protobuf
                        switch (typename)
                        {
                            case "ReqLogin":
                                ReqLogin temp=  ProtobufUntil.Deserialize<ReqLogin>(bodybyte);
                                Console.WriteLine(temp.account+" "+ temp.password);
                                break;
                        }
                        //指针指向下一个包体
                        startIndex = 4 + bodyLength + typeLength;
                        //长度下为读取的字体长度
                        length -= 4 + bodyLength + typeLength;
                    }
                   else
                    {
                        //未到一个包头的缓存下来
                        acheByte = new byte[length];
                        Array.Copy(buffer, startIndex, acheByte, 0, length);
                        break;
                    }
                }
                //递归不停的接受消息
                ReceiveMessage();
            }
            catch(Exception e)
            {
                Console.WriteLine("{0}客户端因{1}断开连接", tempSocket.RemoteEndPoint.ToString(),e.Message);
            }
           
        }
        #endregion

        //建立一个队列发送消息
        private Queue<byte[]> m_SendMessageQueue = new Queue<byte[]>();

        //发送消息回调
        private Action m_ClientSendMessageCallBack;

        private void SendMsgCallBack()
        {
            lock (m_SendMessageQueue)
            {
                if (m_SendMessageQueue.Count > 0 && m_Socket != null && m_Socket.Connected)
                {
                    //发送消息
                    SendMessageByClient(m_SendMessageQueue.Dequeue());
                }
            }
        }
        //发送消息
        private void SendMessageByClient(byte[] msg)
        {
            m_Socket.BeginSend(msg, 0, msg.Length, SocketFlags.None, MessageSendCallBack, m_Socket);
        }
        //发送消息回调
        private void MessageSendCallBack(IAsyncResult ar)
        {
            int length = m_Socket.EndSend(ar);

            SendMsgCallBack();
        }
        //制作包体
        private byte[] MakeData(byte[] data, Type type)
        {

            string TypeName = type.Name;

            byte[] bodylength = BitConverter.GetBytes((ushort)data.Length);

            byte[] typeName = Encoding.UTF8.GetBytes(TypeName);

            byte[] typeNamelength = BitConverter.GetBytes((ushort)typeName.Length);
            //包体类型为包体长度+protobuf类名长度+类名+包体
            byte[] AllMessage = new byte[bodylength.Length + typeNamelength.Length + typeName.Length + data.Length];

            Array.Copy(bodylength, 0, AllMessage, 0, bodylength.Length);

            Array.Copy(typeNamelength, 0, AllMessage, bodylength.Length, typeNamelength.Length);

            Array.Copy(typeName, 0, AllMessage, bodylength.Length + typeNamelength.Length, typeName.Length);

            Array.Copy(data, 0, AllMessage, bodylength.Length + typeNamelength.Length + typeName.Length, data.Length);

            return AllMessage;
        }
        //发送消息
        public void SendMsg(IExtensible msg)
        {
            //序列化Protobuf
            byte[] bytemsg = ProtobufUntil.Serialize(msg);
            byte[] data = MakeData(bytemsg, msg.GetType());

            lock (m_SendMessageQueue)
            {
                //消息进入队列
                m_SendMessageQueue.Enqueue(data);
                //开始发送消息
                m_ClientSendMessageCallBack.BeginInvoke(null, null);
            }
        }

    }
}
