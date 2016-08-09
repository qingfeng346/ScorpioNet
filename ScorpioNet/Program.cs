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
        private Dictionary<short, string> m_Files = new Dictionary<short, string>();
        private object m_Sync = new object();
        public override void OnInitialize() {
            Console.WriteLine("有新连接进入 " + m_Socket.GetSocket().RemoteEndPoint.ToString());
        }
        public override void OnRecv(byte type, short msgId, int length, byte[] data) {
            lock (m_Sync) {
                if (type == 0) {
                    string name = Path.Combine(Environment.CurrentDirectory, Encoding.UTF8.GetString(data));
                    if (File.Exists(name)) File.Delete(name);
                    m_Files[msgId] = name;
                    Console.WriteLine("开始收取 [" + name + "] 文件");
                } else if (type == 1) {
                    if (m_Files.ContainsKey(msgId)) {
                        string name = m_Files[msgId];
                        using (FileStream stream = new FileStream(name, FileMode.Append)) {
                            stream.Write(data, 0, data.Length);
                        }
                    }
                } else if (type == 2) {
                    if (m_Files.ContainsKey(msgId)) {
                        string name = m_Files[msgId];
                        m_Files.Remove(msgId);
                        Console.WriteLine("收取文件 [" + name + "] 完成");
                    }
                } else if (type == 3) {
                    string str = Encoding.UTF8.GetString(data);
                    Console.WriteLine("服务器执行命令 " + str);
                } else {
                    Console.WriteLine("服务器收到消息 类型 " + type + "  msgId " + msgId + "  数据 : " + Encoding.UTF8.GetString(data));
                    m_Socket.Send(type, msgId, data);
                }
            }
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
        private short FILE_ID = 0;
        public override void OnInitialize() {
            Console.WriteLine("连接成功");
        }
        public void SendFile(string file) {
            if (!File.Exists(file)) {
                Console.WriteLine("文件 [" + file + "] 不存在");
                return;
            }
            short fileID = FILE_ID++;
            using (FileStream stream = File.OpenRead(file)) {
                Send(0, fileID, Encoding.UTF8.GetBytes(Path.GetFileName(stream.Name)));
                byte[] data = new byte[8192];
                int count = 0;
                while ((count = stream.Read(data, 0, 8192)) > 0) {
                    Send(1, fileID, data, 0, count);
                }
                Send(2, fileID, null);
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
            client.Connect("localhost", 9999);
            while (true) {
                string str = Console.ReadLine();
                if (str.StartsWith("file ")) {
                    ClientConnection.GetInstance().SendFile(str.Replace("file ", ""));
                } else if (str.StartsWith("cmd ")) {
                    ClientConnection.GetInstance().Send(3, 0, Encoding.UTF8.GetBytes(str.Replace("cmd ", "")));
                } else {
                    ClientConnection.GetInstance().Send(100, 9999, Encoding.UTF8.GetBytes(str));
                }
            }
        }
    }
}
