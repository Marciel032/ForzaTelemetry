using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ForzaTelemetry
{
    public delegate void OnTelemetryRead(Telemetry telemetry);

    public class TelemetryReader
    {
        private readonly UdpClient udpClient;
        private bool active;
        private object lockControl;

        public event OnTelemetryRead OnTelemetryRead;

        public TelemetryReader(int port)
        {
            lockControl = new object();
            udpClient = new UdpClient(port);
        }

        public void Start() {
            active = true;
            ReadNextTelemetry();
        }

        public void Stop() {
            active = false;
        }

        private void ReadNextTelemetry() {
            if (active)
                udpClient.ReceiveAsync().ContinueWith(ReceiveResult);
        }

        private void ReceiveResult(Task<UdpReceiveResult> taskResult) {
            try
            {
                if (taskResult.Exception != null)
                    throw taskResult.Exception;

                else
                {
                    var handle = GCHandle.Alloc(taskResult.Result.Buffer, GCHandleType.Pinned);
                    try
                    {
                        var telemetry = (Telemetry)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Telemetry));

                        ReadNextTelemetry();

                        lock (lockControl)
                        {
                            OnTelemetryRead?.Invoke(telemetry);
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
            catch (Exception ex) {
                ReadNextTelemetry();

                Console.WriteLine(ex);
            }
        }
    }
}
