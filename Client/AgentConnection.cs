using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Networking.Connectivity;
using Windows.Networking;
using Windows.ApplicationModel.Background;
using System.Diagnostics;

namespace Client
{
    class AgentConnection
    {
        public event EventHandler<DataReceivedEventArgs> OnDataReceived;

        public event EventHandler OnSlideEnded;

        public event EventHandler OnConnectionFailed;

        public async void ConnectTo(Slide slide)
        {
            var duration = slide.AppearanceDuration;
            var content = slide.Content as ScreenCapture;
            var ipAddress = content.IPTargetAgent;
            HostName localHost = null;
            foreach (var hostName in NetworkInformation.GetHostNames())
            {
                if(hostName.IPInformation != null)
                {
                    if(hostName.Type == Windows.Networking.HostNameType.Ipv4)
                    {
                        localHost = hostName;
                    }
                }
            }
            var TCPClient = new StreamSocket();
            try
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(500);
                var connectAsync = TCPClient.ConnectAsync(
                    new Windows.Networking.EndpointPair(localHost, "54321", new Windows.Networking.HostName(ipAddress), "54321"));
                var connectTask = connectAsync.AsTask(cts.Token);
                await connectTask;
                this.SendToAgent(slide, TCPClient);
            }
            catch(Exception ex)
            {
                TCPClient.Dispose();
                Debug.WriteLine(ex.Message);
                this.FireOnConnectionFailed();
                return;
            }
        }

        public async void SendToAgent(Slide slide, StreamSocket TCPClient)
        {
            var content = slide.Content as ScreenCapture;
            var writer = new DataWriter(TCPClient.OutputStream);
            var reader = new DataReader(TCPClient.InputStream);
            Timer timer = new Timer(async (client) =>
            {
                try
                {
                    var tcpClient = client as StreamSocket;
                    var writerCB = new DataWriter(tcpClient.OutputStream);
                    writerCB.WriteString("::STOP::");
                    await writerCB.StoreAsync();
                    await writerCB.FlushAsync();
                    tcpClient.Dispose();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }, TCPClient, slide.AppearanceDuration, Timeout.Infinite);
            try
            {
                var length = writer.WriteString(content.ProcessName);
                await writer.StoreAsync();
                await writer.FlushAsync();

                var byteList = new List<byte>();
                try
                {
                    while (true)
                    {
                        reader.InputStreamOptions = InputStreamOptions.Partial;
                        uint sizeFieldCount = await reader.LoadAsync(1);
                        if (sizeFieldCount != 1)
                        {
                            // The underlying socket was closed before we were able to read the whole data. 
                            throw new Exception("Connection was closed from Agent with IP: " + content.IPTargetAgent);
                        }

                        // Read the message. 
                        uint messageLength = reader.ReadByte();
                        uint actualMessageLength = await reader.LoadAsync(4);
                        var buffer = new byte[actualMessageLength];
                        reader.ReadBytes(buffer);
                        var msgLength = System.BitConverter.ToUInt32(buffer, 0);
                        byteList.Clear();
                        byteList.Capacity = (int)msgLength;
                        uint remainingLength = msgLength;
                        while (remainingLength > 0)
                        {
                            if (actualMessageLength == 0)
                            {
                                throw new Exception("Connection was closed from Agent with IP: " + content.IPTargetAgent);
                            }
                            actualMessageLength = await reader.LoadAsync(remainingLength);
                            remainingLength -= actualMessageLength;
                            buffer = new byte[actualMessageLength];
                            reader.ReadBytes(buffer);
                            byteList.AddRange(buffer);
                        }
                        this.FireOnDataReceived(byteList.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                this.FireOnConnectionFailed();
            }
        }

        private string SlideToStringParse(Slide slide)
        {
            var msg = "Command:";
            var duration = slide.AppearanceDuration;
            var content = slide.Content;
            
            if(content is ScreenCapture)
            {
                var cont = content as ScreenCapture;
                msg += "screencapture_";
            }
            return msg;
        }

        private void FireOnDataReceived(byte[] buffer)
        {
            if(this.OnDataReceived != null)
            {
                this.OnDataReceived(this, new DataReceivedEventArgs(buffer));
            }
        }

        private void FireOnSlideEnded()
        {
            if(this.OnSlideEnded != null)
            {
                this.OnSlideEnded(this, null);
            }
        }

        private void FireOnConnectionFailed()
        {
            if(this.OnConnectionFailed != null)
            {
                this.OnConnectionFailed(this, null);
            }
        }
    }
}
