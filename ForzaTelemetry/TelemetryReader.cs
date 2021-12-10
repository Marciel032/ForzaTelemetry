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
        private const int SledPacketLength = 232;
        private const int CarDashPacketLength = 311;
        private const int HorizonCarDashPacketLength = 324;

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
                    var telemetry = new Telemetry();
                    var bytes = taskResult.Result.Buffer;
                    telemetry.Version = GetVersion(bytes.Length);

                    if (telemetry.Version == ForzaDataVersion.Unknown)
                        throw new Exception("Unknown version.");

                    ReadNextTelemetry();

                    telemetry.Slead = Read<TelemetrySlead>(bytes);
                    var bytesDash = new byte[79];
                    switch (telemetry.Version) {
                        case ForzaDataVersion.CarDash:                            
                            Array.Copy(bytes, SledPacketLength, bytesDash, 0, 79);
                            telemetry.Dash = Read<TelemetryDash>(bytesDash);
                            break;
                        case ForzaDataVersion.HorizonCarDash:
                            Array.Copy(bytes, SledPacketLength + 12, bytesDash, 0, 79);
                            telemetry.Dash = Read<TelemetryDash>(bytes);
                            break;
                    }
                    
                    lock (lockControl)
                    {
                        OnTelemetryRead?.Invoke(telemetry);
                    }
                }
            }
            catch (Exception ex) {
                ReadNextTelemetry();

                Console.WriteLine(ex);
            }
        }

        private T Read<T>(byte[] bytes) {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        private ForzaDataVersion GetVersion(int length)
        {
            switch(length){
                case SledPacketLength: return ForzaDataVersion.Sled;
                case CarDashPacketLength: return ForzaDataVersion.CarDash;
                case HorizonCarDashPacketLength: return ForzaDataVersion.HorizonCarDash;
                default: return ForzaDataVersion.Unknown;
            }
        }
    }
}
