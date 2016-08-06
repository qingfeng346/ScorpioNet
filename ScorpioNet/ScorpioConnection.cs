using System.Net.Sockets;
namespace Scorpio.Net {
    public interface ScorpioConnectionFactory {
        ScorpioConnection create();
    }
    public abstract class ScorpioConnection {
        protected ScorpioSocket m_Socket;
        internal void SetSocket(ScorpioSocket socket) {
            m_Socket = socket;
        }
        public void Send(byte type, short msgId, byte[] data) {
            m_Socket.Send(type, msgId, data);
        }
        public abstract void OnInitialize();
        public abstract void OnRecv(byte type, short msgId, int length, byte[] data);
        public abstract void Disconnect(SocketError error);

    }
}
