namespace ReCaD.MediaServer
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public class Client
    {
        private MainWindow owner = null;
        private TcpClient destClient = null;
        private NetworkStream netStream = null;
        private byte[] captureBuffer = null;
        private byte[] readChunk = new byte[4096];
        private Thread captureThread = null;
        private TcpClient netClient = null;

        public Client(MainWindow wnd, TcpClient client)
        {
            this.owner = wnd;
            this.netClient = client;
            this.Remote = this.netClient.Client.RemoteEndPoint;
            this.netStream = client.GetStream();
            this.BeginReadStream();
        }

        public event EventHandler Disconnected;

        public WindowCapture Capture
        {
            get; private set;
        }

        public EndPoint Remote
        {
            get; private set;
        }

        public void StopSending()
        {
            this.Disconnected?.Invoke(this, EventArgs.Empty);

            if (this.netStream != null)
            {
                try
                {
                    this.netStream.Close();
                }
                catch (Exception ex)
                {
                    this.LogException(ex);
                }
            }

            if (this.destClient != null)
            {
                try
                {
                    this.destClient.Close();
                }
                catch (Exception ex)
                {
                    this.LogException(ex);
                }
            }
        }

        public void StopCapturing()
        {
            if (this.captureThread != null)
            {
                try
                {
                    this.LogMessage("Stopping capture");
                    this.captureThread.Abort();
                    this.captureThread = null;
                }
                catch (Exception ex)
                {
                    this.LogException(ex);
                }
            }
        }

        public void Stop()
        {
            if (this.netClient != null)
            {
                try
                {
                    this.netClient.Close();
                    this.netClient = null;
                }
                catch (Exception ex)
                {
                    this.LogException(ex);
                }
            }
        }

        private void LogMessage(string msg)
        {
            this.owner.LogMessage("[" + this.Remote + "] " + msg);
        }

        private void LogException(Exception ex)
        {
            this.LogMessage(ex.Message);
        }

        private void UpdateCaptureBuffer()
        {
            var byteCount = this.Capture.GetRequiredByteCount();
            this.captureBuffer = new byte[byteCount];
        }

        private void StartCapture()
        {
            this.UpdateCaptureBuffer();
            this.StopCapturing();

            this.captureThread = new Thread(this.CaptureHandler);
            this.captureThread.IsBackground = true;
            this.captureThread.Priority = ThreadPriority.AboveNormal;
            this.captureThread.Start();
        }

        private void Capture_SizeChanged(object sender, EventArgs e)
        {
            this.UpdateCaptureBuffer();
        }

        private void StartCaptureProcess(string procName)
        {
            //var allprocs = Process.GetProcesses();
            //var procs = allprocs.Where(x => x.ProcessName == procName).ToArray();
            //var procs = Process.GetProcessesByName(procName);

            var procs = Process.GetProcessesByName(procName);
            foreach (var proc in procs)
            {
                var handle = proc.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    this.Capture = WindowCapture.FromHandle(handle);
                    this.StartCapture();
                    break;
                }
            }
        }

        private void ReceivedMessage(string message)
        {
            if (message == "::STOP::")
            {
                this.StopCapturing();
            }
            else
            {
                this.LogMessage("Received: " + message);
                this.StartCaptureProcess(message);
            }
        }

        private static string ReadString(byte[] data)
        {
            var message = System.Text.Encoding.UTF8.GetString(data);
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0)
                {
                    return message.Substring(0, i);
                }
            }

            return message;
        }

        private void BeginReadStream()
        {
            this.netStream.BeginRead(this.readChunk, 0, this.readChunk.Length, (IAsyncResult ar) =>
            {
                try
                {
                    this.netStream.EndRead(ar);
                    var message = ReadString(this.readChunk);
                    for (int i = 0; i < message.Length; i++)
                    {
                        this.readChunk[i] = 0;
                    }

                    this.ReceivedMessage(message);
                    this.BeginReadStream();
                }
                catch (Exception ex)
                {
                    this.LogException(ex);
                }
            }, null);
        }

        private void CaptureHandler(object args)
        {
            while (true)
            {
                try
                {
                    this.Capture.CaptureIntoBuffer(this.captureBuffer);
                }
                catch (Exception ex)
                {
                    this.StopCapturing();
                    this.LogException(ex);
                }

                try
                {
                    var payload = this.captureBuffer;
                    var datalen = payload.Length;
                    var lenbytes = BitConverter.GetBytes((uint)datalen);
                    this.netStream.Write(new byte[1], 0, 1);
                    this.netStream.Write(lenbytes, 0, lenbytes.Length);
                    this.netStream.Write(payload, 0, datalen);
                }
                catch (Exception ex)
                {
                    this.StopCapturing();
                    this.StopSending();
                    this.LogException(ex);
                }
            }
        }
    }
}
