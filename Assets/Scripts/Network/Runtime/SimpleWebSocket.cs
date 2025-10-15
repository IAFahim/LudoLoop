using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#else
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
#endif

namespace Network.Runtime
{
    /// <summary>
    /// Simple WebSocket wrapper that works on both WebGL and native platforms
    /// For production, consider using NativeWebSocket or WebSocketSharp
    /// </summary>
    public class SimpleWebSocket
    {
        public event Action OnOpen;
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action OnClose;

        private string url;
        private Queue<string> messageQueue = new Queue<string>();
        private bool isConnected = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL uses JavaScript WebSocket
        [DllImport("__Internal")]
        private static extern void WebSocketConnect(string url);
        
        [DllImport("__Internal")]
        private static extern void WebSocketSend(string message);
        
        [DllImport("__Internal")]
        private static extern void WebSocketClose();
        
        // Called from JavaScript
        public void OnWebSocketOpen()
        {
            isConnected = true;
            OnOpen?.Invoke();
        }
        
        public void OnWebSocketMessage(string message)
        {
            messageQueue.Enqueue(message);
        }
        
        public void OnWebSocketError(string error)
        {
            OnError?.Invoke(error);
        }
        
        public void OnWebSocketClose()
        {
            isConnected = false;
            OnClose?.Invoke();
        }
#else
        // Native platforms use System.Net.WebSockets
        private ClientWebSocket clientWebSocket;
        private CancellationTokenSource cancellationTokenSource;
        private Task receiveTask;
#endif

        public bool IsConnected => isConnected;

        public async void Connect(string serverUrl)
        {
            url = serverUrl;
            
#if UNITY_WEBGL && !UNITY_EDITOR
            WebSocketConnect(url);
#else
            try
            {
                clientWebSocket = new ClientWebSocket();
                cancellationTokenSource = new CancellationTokenSource();
                
                await clientWebSocket.ConnectAsync(new Uri(url), cancellationTokenSource.Token);
                isConnected = true;
                OnOpen?.Invoke();
                
                receiveTask = ReceiveLoop(cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
#endif
        }

        public void Send(string message)
        {
            if (!isConnected)
            {
                Debug.LogWarning("WebSocket not connected");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            WebSocketSend(message);
#else
            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                var segment = new ArraySegment<byte>(bytes);
                clientWebSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
#endif
        }

        public void Close()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebSocketClose();
#else
            if (clientWebSocket != null && clientWebSocket.State == WebSocketState.Open)
            {
                cancellationTokenSource?.Cancel();
                clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
#endif
            isConnected = false;
        }

        public void ProcessMessages()
        {
            // Process queued messages on Unity main thread
            while (messageQueue.Count > 0)
            {
                var message = messageQueue.Dequeue();
                OnMessage?.Invoke(message);
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];
            
            try
            {
                while (clientWebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await clientWebSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        cancellationToken
                    );
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        isConnected = false;
                        OnClose?.Invoke();
                        break;
                    }
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageQueue.Enqueue(message);
                    }
                }
            }
            catch (Exception e)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    OnError?.Invoke(e.Message);
                }
                isConnected = false;
                OnClose?.Invoke();
            }
        }
#endif
    }
}
