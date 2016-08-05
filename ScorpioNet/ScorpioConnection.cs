using System.Net.Sockets;
namespace Scorpio.Net {
    public interface ScorpioConnectionFactory {
        ScorpioConnection create();
    }
    public abstract class ScorpioConnection {
        public abstract void OnInitialize();
        public abstract void OnRecv(int length, byte[] data);
        public abstract void Disconnect(SocketError error);

    }
}
