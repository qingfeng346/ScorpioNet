using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
namespace Scorpio.Net {
    public enum ServerState {
        None,           //无状态
        Listened,       //监听状态
    }
    public class ServerSocket : ScorpioHostBase {
        private ScorpioConnectionFactory m_Factory;
        private ServerState m_State;
        private Socket m_Socket;
        private List<ScorpioConnection> m_Connects = new List<ScorpioConnection>();
        private SocketAsyncEventArgs m_AcceptEvent = null;
        private bool m_LengthIncludesLengthFieldLength;     //数据总长度是否包含
        public ServerSocket(ScorpioConnectionFactory factory) : this (factory, true) { }
        public ServerSocket(ScorpioConnectionFactory factory, bool lengthIncludesLengthFieldLength) {
            m_Factory = factory;
            m_LengthIncludesLengthFieldLength = lengthIncludesLengthFieldLength;
            m_State = ServerState.None;
        }
        public void Listen(int port) {
            if (m_State != ServerState.None) return;
            try {
                m_AcceptEvent = new SocketAsyncEventArgs();
                m_AcceptEvent.Completed += AcceptAsyncCompleted;
                m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_Socket.NoDelay = false;
                m_Socket.Bind(new IPEndPoint(IPAddress.Any, port));
                m_Socket.Listen(10);
                m_State = ServerState.Listened;
                BeginAccept();
            } catch (System.Exception ex) {
                m_State = ServerState.None;
                logger.error("服务器监听失败 [" + port + "] " + ex.ToString());
            };
        }
        void BeginAccept() {
            bool completed = false;
            try {
                completed = m_Socket.AcceptAsync(m_AcceptEvent);
            } catch (System.Exception ex) {
                logger.error("Accept出现错误 : " + ex.ToString());
            }
            if (!completed) ScorpioThreadPool.CreateThread(() => { BeginAccept(); });
        }
        void AcceptAsyncCompleted(object sender, SocketAsyncEventArgs e) {
            var error = e.SocketError;
            if (error != SocketError.Success) {
                if (error == SocketError.OperationAborted || error == SocketError.Interrupted || error == SocketError.NotSocket)
                    return;
                m_Socket.AcceptAsync(m_AcceptEvent);
                return;
            }
            var connection = m_Factory.create();
            connection.SetSocket(this, new ScorpioSocket(m_AcceptEvent.AcceptSocket, m_LengthIncludesLengthFieldLength));
            m_Connects.Add(connection);
            m_AcceptEvent.AcceptSocket = null;
            BeginAccept();
        }
        public void OnDisconnect(ScorpioConnection connection) {
            m_Connects.Remove(connection);
        }
        public void Shutdown() {
            if (m_State == ServerState.None) { return; }
            m_State = ServerState.None;
            try {
                m_AcceptEvent.Completed -= AcceptAsyncCompleted;
                m_AcceptEvent.Dispose();
                m_AcceptEvent = null;
                m_Socket.Close();
                while (m_Connects.Count > 0) {
                    m_Connects[0].Disconnect();
                }
            } finally {
                m_Socket = null;
            }
        }
    }
}
