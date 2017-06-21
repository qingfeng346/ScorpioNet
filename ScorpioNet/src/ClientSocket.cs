using System;
using System.Net;
using System.Net.Sockets;
namespace Scorpio.Net {
    public enum ClientState {
        None,           //无状态
        Connecting,     //正在连接
        Connected,      //已连接状态
    }
    public class ClientSocket : ScorpioHostBase {
        private ClientState m_State;                        //当前状态
        private ScorpioSocket m_Dispatcher;                 //网络信息处理
        private ScorpioConnection m_Connection;             //网络对象
        private Socket m_Socket = null;                     //Socket句柄
        private SocketAsyncEventArgs m_ConnectEvent;        //异步连接消息
        private bool m_LengthIncludesLengthFieldLength;     //数据总长度是否包含
        public ClientSocket(ScorpioConnectionFactory factory) : this(factory, true) { }
        public ClientSocket(ScorpioConnectionFactory factory, bool lengthIncludesLengthFieldLength) {
            m_State = ClientState.None;
            m_LengthIncludesLengthFieldLength = lengthIncludesLengthFieldLength;
            m_Connection = factory.create();
            m_ConnectEvent = new SocketAsyncEventArgs();
            m_ConnectEvent.Completed += ConnectionAsyncCompleted;
        }
        public ScorpioConnection GetConnection() {
            return m_Connection;
        }
        public void Connect(string host, int port) {
            if (m_State != ClientState.None) return;
            m_State = ClientState.Connecting;
            try {
#if SCORPIO_DNSENDPOINT
                m_ConnectEvent.RemoteEndPoint = new DnsEndPoint(m_Host, m_Port);
#else
                IPAddress address;
                if (host == "localhost") {
                    m_ConnectEvent.RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                    address = IPAddress.Parse("127.0.0.1");
                } else if (IPAddress.TryParse(host, out address)) {
                    m_ConnectEvent.RemoteEndPoint = new IPEndPoint(address, port);
                } else {
                    var addressList = Dns.GetHostEntry(host).AddressList;
                    address = addressList[0];
                    m_ConnectEvent.RemoteEndPoint = new IPEndPoint(address, port);
                }
#endif
                m_Socket = new Socket(m_ConnectEvent.RemoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                m_Socket.NoDelay = false;
                if (!m_Socket.ConnectAsync(m_ConnectEvent)) {
                    ConnectError("连接服务器出错 " + host + ":" + port);
                }
            } catch (System.Exception ex) {
                ConnectError("连接服务器出错 " + host + ":" + port + " " + ex.ToString());
            };
        }
        void ConnectionAsyncCompleted(object sender, SocketAsyncEventArgs e) {
            if (e.SocketError != SocketError.Success) {
                ConnectError("连接服务器出错 " + e.RemoteEndPoint.ToString() + " " + e.SocketError);
                return;
            }
            m_State = ClientState.Connected;
            m_Dispatcher = new ScorpioSocket(m_Socket, m_LengthIncludesLengthFieldLength);
            m_Connection.SetSocket(this, m_Dispatcher);
        }
        void ConnectError(string error) {
            m_State = ClientState.None;
            logger.error(error);
            m_Connection.OnConnectError(error);
        }
        public void OnDisconnect(ScorpioConnection connection) {

        }
    }
}