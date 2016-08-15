using System.Net.Sockets;
namespace Scorpio.Net {
    public interface ScorpioConnectionFactory {
        ScorpioConnection create();
    }
    public interface ScorpioHostBase {
        void OnDisconnect(ScorpioConnection connection);
    }
    public abstract class ScorpioConnection {
        private bool m_Closed = true;
        protected ScorpioSocket m_Socket;
        protected ScorpioHostBase m_Host;
        internal void SetSocket(ScorpioHostBase host, ScorpioSocket socket) {
            m_Host = host;
            m_Socket = socket;
            m_Socket.SetConnection(this);
            m_Closed = false;
            OnInitialize();
        }
        public void Send(byte type, short msgId, byte[] data) { m_Socket.Send(type, msgId, data); }
        public void Send(byte type, short msgId, byte[] data, int offset, int count) { m_Socket.Send(type, msgId, data, offset, count); }
        public void Disconnect() {
            if (m_Closed) { return; }
            m_Closed = true;
            try {
                using (var socket = m_Socket.GetSocket()) {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            } finally {
                if (m_Host != null) m_Host.OnDisconnect(this);
                OnDisconnect();
            }
        }
        public virtual void OnInitialize() { }
        public virtual void OnConnectError(string error) { }
        public virtual void OnDisconnect() { }
        public abstract void OnRecv(byte type, short msgId, int length, byte[] data);
    }
}
