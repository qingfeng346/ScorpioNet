using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scorpio.Net;
using System.Net.Sockets;
namespace ScorpioNet {
    public class ServerFactory : ScorpioConnectionFactory {
        public ScorpioConnection create() {
            return new ServerConnection();
        }
    }
    public class ServerConnection : ScorpioConnection {
        public override void OnInitialize() {
            Console.WriteLine("有新连接进入 " + m_Socket.GetSocket().RemoteEndPoint.ToString());
        }
        public override void OnRecv(byte type, int length, byte[] data) {
            Console.WriteLine("服务器收到消息 类型 " + type + "  数据 : " + Encoding.UTF8.GetString(data));
            m_Socket.Send(type, data);
        }
        public override void Disconnect(SocketError error) {

        }
    }
    public class ClientFactory : ScorpioConnectionFactory {
        public ScorpioConnection create() {
            return ClientConnection.GetInstance();
        }
    }
    public class ClientConnection : ScorpioConnection {
        private static ClientConnection instance;
        public static ClientConnection GetInstance() {
            if (instance == null)
                instance = new ClientConnection();
            return instance;
        }
        public override void OnInitialize() {
            Console.WriteLine("连接成功");
        }
        public override void OnRecv(byte type, int length, byte[] data) {
            Console.WriteLine("客户端收到消息 类型 " + type + "  数据 : " + Encoding.UTF8.GetString(data));
        }
        public override void Disconnect(SocketError error) {

        }
    }
    class Program {
        static void Main(string[] args) {
            ServerSocket server = new ServerSocket(new ServerFactory());
            server.Listen(9999);
            ClientSocket client = new ClientSocket(new ClientFactory());
            client.Connect("localhost", 9999);
            while (true) {
                string str = Console.ReadLine();
                ClientConnection.GetInstance().Send(100, Encoding.UTF8.GetBytes(str));
            }
        }
    }
}
