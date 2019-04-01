using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// �ײ�Cache Socket
    /// </summary>
    internal class MSocket
    {
        private static LogAdapter logger = LogAdapter.GetLogger(typeof(MSocket));

        private HostNode socketPool;
        /// <summary>
        /// ���ص�Socket�ء�
        /// </summary>
        public HostNode SocketPool
        {
            get
            {
                return socketPool;
            }
        }
        private Socket socket;
        private Stream stream;
        public readonly DateTime CreateTime;
        /// <summary>
        /// ������չ�����ԣ�����Redis��
        /// </summary>
        public uint DB = 0;
        private int sendReceiveTimeout = 5000;
        private int connectTimeout = 3000;
        public MSocket(HostNode socketPool, IPEndPoint endPoint)
        {
            this.socketPool = socketPool;
            CreateTime = DateTime.Now;

            //Set up the socket.
            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, sendReceiveTimeout);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, sendReceiveTimeout);
            socket.ReceiveTimeout = sendReceiveTimeout;
            socket.SendTimeout = sendReceiveTimeout;

            //socket.SendBufferSize = 1024 * 1024;

            //Do not use Nagle's Algorithm
            //socket.NoDelay = true;

            //Establish connection asynchronously to enable connect timeout.
            IAsyncResult result = socket.BeginConnect(endPoint, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(connectTimeout, false);
            if (!success)
            {
                try { socket.Close(); }
                catch { }
                throw new SocketException();
            }
            socket.EndConnect(result);

            //Wraps two layers of streams around the socket for communication.
            stream = new BufferedStream(new NetworkStream(socket, false));
        }

        /// <summary>
        /// �ع� Socket��
        /// </summary>
        public void ReturnPool()
        {
            if (socketPool != null)
            {
                socketPool.Return(this);
            }
        }

        /// <summary>
        /// This method closes the underlying stream and socket.
        /// �ر�Socket���ͷ������Դ��
        /// </summary>
        public void Close()
        {
            if (stream != null)
            {
                try
                {
                    stream.Close();
                }
                catch (Exception e)
                {
                    logger.Error("Error closing stream: " + socketPool.Host, e);
                }
                stream = null;
            }
            if (socket != null)
            {
                //try 
                //{
                //    socket.Shutdown(SocketShutdown.SocketShutdown.Both); 
                //}
                //catch (Exception e)
                //{
                //    logger.Error("Error shutting down socket: " + socketPool.Host, e);
                //}
                try
                {
                    socket.Close();
                }
                catch (Exception e)
                {
                    logger.Error("Error closing socket: " + socketPool.Host, e);
                }
                socket = null;
            }
        }

        /// <summary>
        /// Checks if the underlying socket and stream is connected and available.
        /// </summary>
        public bool IsAlive
        {
            get { return socket != null && socket.Connected && stream.CanRead; }
        }

        /// <summary>
        /// Writes a string to the socket encoded in UTF8 format.
        /// </summary>
        public void Write(string str)
        {
            Write(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Writes an array of bytes to the socket and flushes the stream.
        /// </summary>
        public void Write(byte[] bytes)
        {
            try
            {
                //stream.Flush();//�������ǰ�棬�����кܴ�������ͬʱ����Redis�ط������ݣ���
                stream.Write(bytes, 0, bytes.Length);

            }
            catch (Exception e)
            {
                logger.Error("Error socket Write : bytes length " + bytes.Length, e);
            }
        }
        /// <summary>
        /// Reads from the socket until the sequence '\r\n' is encountered, 
        /// and returns everything up to but not including that sequence as a UTF8-encoded string
        /// ����Null��û�������ˣ�
        /// </summary>
        public string ReadLine()
        {
            byte[] data = ReadLineBytes();
            if (data != null && data.Length > 0)
            {
                return Encoding.UTF8.GetString(data);
            }
            return null;
        }
        /// <summary>
        /// ��һ�е�����
        /// </summary>
        /// <returns></returns>
        public byte[] ReadLineBytes()
        {
            MemoryStream buffer = new MemoryStream();
            int b;
            bool gotReturn = false;
            while ((b = stream.ReadByte()) != -1)
            {
                if (gotReturn)
                {
                    if (b == 10)//\n
                    {
                        break;
                    }
                    else
                    {
                        buffer.WriteByte(13);
                        gotReturn = false;
                    }
                }
                if (b == 13)//\r
                {
                    gotReturn = true;
                }
                else
                {
                    buffer.WriteByte((byte)b);
                }
            }
            return buffer.ToArray();
        }

        /// <summary>
        /// ��һ�е�����
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes(int maxLen)
        {
            MemoryStream buffer = new MemoryStream();
            int b;
            int i = 0;
            while ((b = stream.ReadByte()) != -1)
            {
                buffer.WriteByte((byte)b);
                i++;
                if (i >= maxLen)
                {
                    try
                    {
                        stream.ReadByte();//13
                        stream.ReadByte();//10
                    }
                    catch { }
                    break;
                }
            }
            return buffer.ToArray();

        }

        /// <summary>
        /// Reads a response line from the socket, checks for general memcached errors, and returns the line.
        /// If an error is encountered, this method will throw an exception.
        /// </summary>
        public string ReadResponse()
        {
            string response = ReadLine();

            if (String.IsNullOrEmpty(response))
            {
                Error.Throw("Received empty response.");
            }

            if (response.StartsWith("-ERR")
                || response.StartsWith("ERROR")
                || response.StartsWith("CLIENT_ERROR")
                || response.StartsWith("SERVER_ERROR"))
            {
                Error.Throw("Server returned " + response);
            }

            return response;
        }

        /// <summary>
        /// Fills the given byte array with data from the socket.
        /// </summary>
        public void Read(byte[] bytes)
        {
            if (bytes == null)
            {
                return;
            }

            int readBytes = 0;
            while (readBytes < bytes.Length)
            {
                readBytes += stream.Read(bytes, readBytes, (bytes.Length - readBytes));
            }
        }

        /// <summary>
        /// Reads from the socket until the sequence '\r\n' is encountered.
        /// </summary>
        public void SkipToEndOfLine()
        {
            int b;
            bool gotReturn = false;
            while ((b = stream.ReadByte()) != -1)
            {
                if (gotReturn)
                {
                    if (b == 10)//\n
                    {
                        break;
                    }
                    else
                    {
                        gotReturn = false;
                    }
                }
                if (b == 13)
                {
                    gotReturn = true;
                }
            }
        }
        public void SkipToEndOfLine(int cmdCount)
        {
            for (int i = 0; i < cmdCount; i++)
            {
                SkipToEndOfLine();
            }
        }

        /// <summary>
        /// Resets this PooledSocket by making sure the incoming buffer of the socket is empty.
        /// If there was any leftover data, this method return true.
        /// </summary>
        public bool Reset()
        {
            try
            {
                if (socket.Available > 0)
                {
                    byte[] b = new byte[socket.Available];
                    Read(b);

                }
                stream.Flush();//��������������
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}