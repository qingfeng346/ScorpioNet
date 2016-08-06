using System.Net.Sockets;
namespace Scorpio.Net {
    public interface ScorpioConnectionFactory {
        ScorpioConnection create();
    }
    public abstract class ScorpioConnection {
        private bool m_Closed = false;
        protected ScorpioSocket m_Socket;
        internal void SetSocket(ScorpioSocket socket) { m_Socket = socket; }
        public void Send(byte type, short msgId, byte[] data) { m_Socket.Send(type, msgId, data); }
        public void Disconnect() {
            if (m_Closed) { return; }
            m_Closed = true;
            try {
                using (var socket = m_Socket.GetSocket()) {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            } finally {
                OnDisconnect();
            }
        }
        public virtual void OnInitialize() { }
        public virtual void OnDisconnect() { }
        public abstract void OnRecv(byte type, short msgId, int length, byte[] data);
    }
}
