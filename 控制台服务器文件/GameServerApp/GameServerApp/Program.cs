using proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServerApp
{
    class Program
    {
        private static string m_ip = "127.0.0.1";
        private static int m_port = 7777;
        private static Socket m_ServerSocket;


        static void Main(string[] args)
        {
            m_ServerSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

            m_ServerSocket.Bind(new IPEndPoint(IPAddress.Parse(m_ip), m_port));

            m_ServerSocket.Listen(3000);

            Console.WriteLine("启动监听{0}成功", m_ServerSocket.LocalEndPoint.ToString());

            Thread StartAccept = new Thread(ListenClientCallBack);

            StartAccept.Start();

            ReqLogin account = new ReqLogin();

            Console.ReadKey();
        }

        private static void ListenClientCallBack()
        {
            while(true)
            {
                //服务器接收客户端socket对象
                Socket socket=  m_ServerSocket.Accept();
                Console.WriteLine("{0}连接成功", socket.RemoteEndPoint.ToString());

                ClientSocket clientSocket = new ClientSocket(socket);
            }
        }
    }
}
