using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scorpio.Net;
using System.Net.Sockets;
using System.Threading;
namespace ScorpioNet {
    public class Log : ILog {
        public void debug(string format) {
            Console.WriteLine("debug : " + format);
        }
        public void info(string format) {
            Console.WriteLine("info : " + format);
        }
        public void warn(string format) {
            Console.WriteLine("warn : " + format);
        }
        public void error(string format) {
            Console.WriteLine("error : " + format);
        }
    }
    public class ServerFactory : ScorpioConnectionFactory {
        public ScorpioConnection create() {
            return new ServerConnection();
        }
    }
    public class ServerConnection : ScorpioConnection {
        public override void OnInitialize() {
            Console.WriteLine("有新连接进入 " + m_Socket.GetSocket().RemoteEndPoint.ToString());
        }
        public override void OnRecv(byte type, short msgId, int length, byte[] data) {
            Console.WriteLine("服务器收到消息 类型 " + type + "  msgId " + msgId + "  数据 : " + Encoding.UTF8.GetString(data));
            m_Socket.Send(type, msgId, data);
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
            using (FileStream stream = File.OpenRead(@"E:\sgspad_1202.apk")) {
                using (BinaryReader reader = new BinaryReader(stream)) {
                    long length = stream.Length;
                    long cur = 0;
                    Send(0, 0, Encoding.UTF8.GetBytes(string.Format("{{name:\"{0}\", length : {1}}}", Path.GetFileName(stream.Name), length)));
                    byte[] data = new byte[8192];
                    int count = 0;
                    while ((count = stream.Read(data, 0, 8192)) > 0) {
                        cur += count;
                        System.GC.Collect();
                        Send(1, 0, data, 0, count);
                        //Console.WriteLine("已发送 " + cur + " / " + length);
                    }
                    Send(2, 0, new byte[0]);
                }
            }
        }
        public override void OnRecv(byte type, short msgId, int length, byte[] data) {
            Console.WriteLine("客户端收到消息 类型 " + type + "  msgId " + msgId + "  数据 : " + Encoding.UTF8.GetString(data));
        }
        public override void OnDisconnect() {
            Console.WriteLine("断开连接");
        }
    }
    class Program {
        static void Main(string[] args) {
            logger.SetLog(new Log());
            ServerSocket server = new ServerSocket(new ServerFactory());
            server.Listen(9999);
            ClientSocket client = new ClientSocket(new ClientFactory());
            client.Connect("localhost", 28123);
            //FileStream stream = File.OpenRead("E:/迅雷下载/007：幽灵党.BD1280高清中英双字.mp4");
            while (true) {
                string str = Console.ReadLine();
                ClientConnection.GetInstance().Send(100, 9999, Encoding.UTF8.GetBytes(str));
            }
        }
    }
}
