using System;
using System.Collections.Generic;
using System.Net.Sockets;
namespace Scorpio.Net {
    public class ScorpioSocket {
        private const int MAX_BUFFER_LENGTH = 8192;                             // 缓冲区大小
        private const int HEAD_LENGTH = 2;                                      // 协议头长度
        private byte[] m_RecvTokenBuffer = new byte[MAX_BUFFER_LENGTH * 10];    // 已经接收的数据总缓冲
        private int m_RecvTokenSize = 0;                                        // 接收总数据的长度
        private bool m_Sending;                                                 // 是否正在发送消息
        private Socket m_Socket = null;                                         // Socket句柄
        private Queue<byte[]> m_SendQueue = new Queue<byte[]>();                // 消息堆栈
        private SocketAsyncEventArgs m_RecvEvent = null;                        // 异步接收消息
        private SocketAsyncEventArgs m_SendEvent = null;                        // 异步发送消息
        private ScorpioConnection m_Connection = null;                          // 链接
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
        public void Send(byte[] data) {
            short length = Convert.ToInt16(data.Length);
            byte[] buffer = new byte[length + HEAD_LENGTH];
            byte[] head = BitConverter.GetBytes(length + HEAD_LENGTH);
            Array.Copy(head, buffer, HEAD_LENGTH);
            Array.Copy(data, 0, buffer, HEAD_LENGTH, length);
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
                } catch (Exception ex) {
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
                OnSend();
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
                } catch (Exception ex) {
                    LogError("解析数据出错 : " + ex.ToString());
                    Disconnect(SocketError.SocketError);
                    return;
                }
                BeginReceive();
            }
        }
        void ParsePackage() {
            for (;;) {
                if (m_RecvTokenSize < HEAD_LENGTH) break;
                short size = BitConverter.ToInt16(m_RecvTokenBuffer, 0);
                if (m_RecvTokenSize < size) break;
                int length = size - HEAD_LENGTH;
                byte[] buffer = new byte[length];
                Array.Copy(m_RecvTokenBuffer, HEAD_LENGTH, buffer, 0, length);
                OnRecv(length, buffer);
                m_RecvTokenSize -= size;
                if (m_RecvTokenSize > 0) Array.Copy(m_RecvTokenBuffer, size, m_RecvTokenBuffer, 0, m_RecvTokenSize);
            }
        }
        void Disconnect(SocketError error) {
            m_Connection.Disconnect(error);
        }
        void OnSend() {
        }
        void OnRecv(int length, byte[] data) {
            m_Connection.OnRecv(length, data);
        }
        void LogError(string error) {
            logger.error(error);
        }
    }
}
