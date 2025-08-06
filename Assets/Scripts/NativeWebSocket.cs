using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

namespace NativeWebSocket
{
    public enum WebSocketState
    {
        Connecting,
        Open,
        Closing,
        Closed
    }

    public enum WebSocketCloseCode
    {
        Normal = 1000,
        GoingAway = 1001,
        ProtocolError = 1002,
        UnsupportedData = 1003,
        NoStatusReceived = 1005,
        AbnormalClosure = 1006,
        InvalidFramePayloadData = 1007,
        PolicyViolation = 1008,
        MessageTooBig = 1009,
        InternalServerError = 1011
    }

    public class WebSocket : IDisposable
    {
        private WebSocketState state = WebSocketState.Closed;
        private string url;
        private Queue<string> messageQueue = new Queue<string>();
        private bool isDisposed = false;

        // Events
        public event Action OnOpen;
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action<WebSocketCloseCode> OnClose;

        // Properties
        public WebSocketState State => state;
        public bool IsConnected => state == WebSocketState.Open;

        public WebSocket(string url)
        {
            this.url = url;
        }

        public async Task Connect()
        {
            if (state != WebSocketState.Closed)
            {
                Debug.LogWarning("WebSocket is already connected or connecting");
                return;
            }

            state = WebSocketState.Connecting;
            
            try
            {
                // For WebGL builds, we'll use the browser's WebSocket
                #if UNITY_WEBGL && !UNITY_EDITOR
                    await ConnectWebGL();
                #else
                    await ConnectNative();
                #endif
            }
            catch (Exception e)
            {
                state = WebSocketState.Closed;
                OnError?.Invoke($"Connection failed: {e.Message}");
            }
        }

        private async Task ConnectWebGL()
        {
            // WebGL implementation will be handled by JavaScript interop
            // For now, we'll simulate the connection
            await Task.Delay(100);
            state = WebSocketState.Open;
            OnOpen?.Invoke();
        }

        private async Task ConnectNative()
        {
            // For standalone builds, we'll use a simple HTTP upgrade simulation
            // In a real implementation, you'd use a proper WebSocket library
            await Task.Delay(100);
            state = WebSocketState.Open;
            OnOpen?.Invoke();
        }

        public void Send(string message)
        {
            if (state != WebSocketState.Open)
            {
                Debug.LogWarning("Cannot send message - WebSocket is not open");
                return;
            }

            messageQueue.Enqueue(message);
        }

        public void Send(byte[] data)
        {
            if (state != WebSocketState.Open)
            {
                Debug.LogWarning("Cannot send data - WebSocket is not open");
                return;
            }

            string message = Encoding.UTF8.GetString(data);
            messageQueue.Enqueue(message);
        }

        public async Task Close(WebSocketCloseCode code = WebSocketCloseCode.Normal)
        {
            if (state == WebSocketState.Closed)
            {
                return;
            }

            state = WebSocketState.Closing;
            
            try
            {
                // Simulate closing delay
                await Task.Delay(50);
                state = WebSocketState.Closed;
                OnClose?.Invoke(code);
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Error during close: {e.Message}");
            }
        }

        public void DispatchMessageQueue()
        {
            if (state != WebSocketState.Open)
            {
                return;
            }

            while (messageQueue.Count > 0)
            {
                string message = messageQueue.Dequeue();
                
                // Simulate receiving a response
                // In a real implementation, this would come from the WebSocket
                SimulateResponse(message);
            }
        }

        private void SimulateResponse(string sentMessage)
        {
            // Simulate a response based on the sent message
            // This is for testing purposes - remove in production
            try
            {
                // Parse the sent message to generate a realistic response
                if (sentMessage.Contains("\"game\""))
                {
                    // Generate a score response
                    string response = $"{{\"messageId\":\"response_{UnityEngine.Random.Range(1000, 9999)}\",\"value\":{UnityEngine.Random.Range(50f, 200f)}}}";
                    OnMessage?.Invoke(response);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error simulating response: {e.Message}");
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                Close().Wait();
            }
        }
    }
} 