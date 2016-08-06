using System;
using System.Collections.Generic;
using System.Net.Sockets;
namespace Scorpio.Net {
    public class ScorpioSocket {
        private const int MAX_BUFFER_LENGTH = 8192;                             // 缓冲区大小
        private byte[] m_RecvTokenBuffer = new byte[MAX_BUFFER_LENGTH * 10];    // 已经接收的数据总缓冲
        private int m_RecvTokenSize = 0;                                        // 接收总数据的长度
        private bool m_Sending;                                                 // 是否正在发送消息
        private Socket m_Socket = null;                                         // Socket句柄
        private Queue<byte[]> m_SendQueue = new Queue<byte[]>();                // 发送消息队列
        private SocketAsyncEventArgs m_RecvEvent = null;                        // 异步接收消息
        private SocketAsyncEventArgs m_SendEvent = null;                        // 异步发送消息
        private ScorpioConnection m_Connection = null;                          // 连接
        public ScorpioSocket() {
            m_SendEvent = new SocketAsyncEventArgs();
            m_SendEvent.Completed += SendAsyncCompleted;
            m_RecvEvent = new SocketAsyncEventArgs();
            m_RecvEvent.SetBuffer(new byte[MAX_BUFFER_LENGTH], 0, MAX_BUFFER_LENGTH);
            m_RecvEvent.Completed += RecvAsyncCompleted;
        }
        //设置socket句柄
        public void SetSocket(Socket socket, ScorpioConnection connection) {
            m_Socket = socket;
            m_Connection = connection;
            m_Connection.SetSocket(this);
            m_Sending = false;
            m_RecvTokenSize = 0;
            Array.Clear(m_RecvTokenBuffer, 0, m_RecvTokenBuffer.Length);
            Array.Clear(m_RecvEvent.Buffer, 0, m_RecvEvent.Buffer.Length);
            m_SendQueue.Clear();
            BeginReceive();
            m_Connection.OnInitialize();
        }
        public Socket GetSocket() {
            return m_Socket;
        }
        //发送协议
        public void Send(byte type, short msgId, byte[] data) {
            int length = data.Length;
            int count = length + 5;                                             //协议头长度5  数据长度short(2个字节) + 数据类型byte(1个字节) + 协议IDshort(2个字节)
            byte[] buffer = new byte[count];
            Array.Copy(BitConverter.GetBytes((short)count), buffer, 2);         //写入数据长度
            buffer[2] = type;                                                   //写入数据类型
            Array.Copy(BitConverter.GetBytes((short)msgId), 0, buffer, 3, 2);   //写入数据ID
            Array.Copy(data, 0, buffer, 5, length);                             //写入数据内容
            lock (m_SendQueue) { m_SendQueue.Enqueue(buffer); }
            BeginSend();
        }
        void BeginSend() {
            if (m_Sending || m_SendQueue.Count <= 0) return;
            m_Sending = true;
            byte[] data = null;
            lock (m_SendQueue) { data = m_SendQueue.Dequeue(); }
            SendInternal(data, 0, data.Length);
        }
        void SendInternal(byte[] data, int offset, int length) {
            lock (m_SendEvent) {
                var completedAsync = false;
                try {
                    if (data == null)
                        m_SendEvent.SetBuffer(offset, length);
                    else
                        m_SendEvent.SetBuffer(data, offset, length);
                    completedAsync = m_Socket.SendAsync(m_SendEvent);
                } catch (System.Exception ex) {
                    LogError("发送数据出错 : " + ex.ToString());
                    Disconnect(SocketError.SocketError);
                }
                if (!completedAsync) {
                    m_SendEvent.SocketError = SocketError.Fault;
                    SendAsyncCompleted(this, m_SendEvent);
                }
            }
        }
        void SendAsyncCompleted(object sender, SocketAsyncEventArgs e) {
            if (e.SocketError != SocketError.Success) {
                LogError("发送数据出错 : " + e.SocketError);
                Disconnect(e.SocketError);
                return;
            }
            if (e.Offset + e.BytesTransferred < e.Count) {
                SendInternal(null, e.Offset + e.BytesTransferred, e.Count - e.BytesTransferred - e.Offset);
            } else {
                m_Sending = false;
                BeginSend();
            }
        }
        //开始接收消息
        void BeginReceive() {
            m_Socket.ReceiveAsync(m_RecvEvent);
        }
        void RecvAsyncCompleted(object sender, SocketAsyncEventArgs e) {
            if (e.SocketError != SocketError.Success) {
                LogError("接收数据出错 : " + e.SocketError);
                Disconnect(e.SocketError);
            } else {
                Array.Copy(e.Buffer, 0, m_RecvTokenBuffer, m_RecvTokenSize, e.BytesTransferred);
                m_RecvTokenSize += e.BytesTransferred;
                try {
                    ParsePackage();
                } catch (System.Exception ex) {
                    LogError("解析数据出错 : " + ex.ToString());
                    Disconnect(SocketError.SocketError);
                    return;
                }
                BeginReceive();
            }
        }
        void ParsePackage() {
            for ( ; ; ) {
                if (m_RecvTokenSize < 2) break;
                short size = BitConverter.ToInt16(m_RecvTokenBuffer, 0);
                if (m_RecvTokenSize < size) break;
                byte type = m_RecvTokenBuffer[2];
                short msgId = BitConverter.ToInt16(m_RecvTokenBuffer, 3);
                int length = size - 5;
                byte[] buffer = new byte[length];
                Array.Copy(m_RecvTokenBuffer, 5, buffer, 0, length);
                OnRecv(type, msgId, length, buffer);
                m_RecvTokenSize -= size;
                if (m_RecvTokenSize > 0) Array.Copy(m_RecvTokenBuffer, size, m_RecvTokenBuffer, 0, m_RecvTokenSize);
            }
        }
        void Disconnect(SocketError error) {
            m_Connection.Disconnect();
        }
        void OnRecv(byte type, short msgId, int length, byte[] data) {
            m_Connection.OnRecv(type, msgId, length, data);
        }
        void LogError(string error) {
            logger.error(error);
        }
    }
}
