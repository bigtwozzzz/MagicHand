using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace Leap.Unity.CameraHands
{
    [System.Serializable]
    public class HandData
    {
        public string hand_side;  // "Left" or "Right"
        public string gesture_name;
        public int gesture_id;
        public float[][] landmarks;  // 21个关键点的归一化坐标 [x, y, z]
    }

    [System.Serializable]
    public class HandDataPacket
    {
        public HandData[] hands;
        public double timestamp;
    }

    public class UDPHandDataReceiver : MonoBehaviour
    {
        [Header("UDP Settings")]
        public int udpPort = 12345;
        public bool enableDebugLog = false;

        public event Action<HandData[]> OnHandDataReceived;

        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        private bool isReceiving = false;

        void Start()
        {
            StartUDPReceiver();
        }

        void StartUDPReceiver()
        {
            try
            {
                udpClient = new UdpClient(udpPort);
                remoteEndPoint = new IPEndPoint(IPAddress.Any, udpPort);
                isReceiving = true;
                
                StartCoroutine(ReceiveData());
                
                if (enableDebugLog)
                    Debug.Log($"UDP Hand Data Receiver started on port {udpPort}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start UDP receiver: {e.Message}");
            }
        }

        private IEnumerator ReceiveData()
        {
            while (isReceiving)
            {
                try
                {
                    if (udpClient != null && udpClient.Available > 0)
                    {
                        byte[] data = udpClient.Receive(ref remoteEndPoint);
                        string jsonString = Encoding.UTF8.GetString(data);
                        
                        if (enableDebugLog)
                            Debug.Log($"Received UDP data: {jsonString}");
                        
                        ProcessReceivedData(jsonString);
                    }
                }
                catch (Exception e)
                {
                    if (enableDebugLog)
                        Debug.LogWarning($"UDP receive error: {e.Message}");
                }
                
                yield return null;
            }
        }

        private void ProcessReceivedData(string jsonString)
        {
            try
            {
                HandDataPacket packet = JsonConvert.DeserializeObject<HandDataPacket>(jsonString);
                
                if (packet != null && packet.hands != null && packet.hands.Length > 0)
                {
                    OnHandDataReceived?.Invoke(packet.hands);
                }
            }
            catch (Exception e)
            {
                if (enableDebugLog)
                    Debug.LogError($"Failed to parse hand data JSON: {e.Message}");
            }
        }

        void OnDestroy()
        {
            StopUDPReceiver();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                StopUDPReceiver();
            else
                StartUDPReceiver();
        }

        private void StopUDPReceiver()
        {
            isReceiving = false;
            
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }
            
            if (enableDebugLog)
                Debug.Log("UDP Hand Data Receiver stopped");
        }
    }
}