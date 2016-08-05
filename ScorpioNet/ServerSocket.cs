using System.Net;
using System.Net.Sockets;
namespace Scorpio.Net {
    public class ServerSocket {
        private ScorpioConnectionFactory m_Factory;
        private Socket m_Socket;
        private SocketAsyncEventArgs m_AcceptEvent = null;
        public ServerSocket(ScorpioConnectionFactory factory) {
            m_Factory = factory;
            m_AcceptEvent = new SocketAsyncEventArgs();
            m_AcceptEvent.Completed += AcceptAsyncCompleted;
        }
        public void Listen(int port) {
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_Socket.NoDelay = false;
            m_Socket.Bind(new IPEndPoint(IPAddress.Any, port));
            m_Socket.Listen(100);
            m_Socket.AcceptAsync(m_AcceptEvent);
        }
        void AcceptAsyncCompleted(object sender, SocketAsyncEventArgs e) {
            if (e.SocketError != SocketError.Success) {
                m_Socket.AcceptAsync(m_AcceptEvent);
                return;
            }
            var socket = new ScorpioSocket();
            socket.SetSocket(m_AcceptEvent.AcceptSocket, m_Factory.create());
            m_Socket.AcceptAsync(m_AcceptEvent);
        }
    }
}
