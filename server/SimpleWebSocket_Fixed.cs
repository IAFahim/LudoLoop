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
    /// Fixed version with proper async handling and thread safety
    /// </summary>
    public class SimpleWebSocket
    {
        public event Action OnOpen;
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action OnClose;

        private string url;
        private readonly Queue<string> messageQueue = new Queue<string>();
        private readonly object queueLock = new object();
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
            lock (queueLock)
            {
                messageQueue.Enqueue(message);
            }
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
        private bool isReceiving = false;
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
                // Clean up any existing connection
                if (clientWebSocket != null)
                {
                    if (clientWebSocket.State == WebSocketState.Open)
                    {
                        await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
                    }
                    clientWebSocket.Dispose();
                }

                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                
                clientWebSocket = new ClientWebSocket();
                cancellationTokenSource = new CancellationTokenSource();
                
                Debug.Log($"[SimpleWebSocket] Connecting to {url}...");
                await clientWebSocket.ConnectAsync(new Uri(url), cancellationTokenSource.Token);
                
                isConnected = true;
                Debug.Log("[SimpleWebSocket] Connected successfully!");
                OnOpen?.Invoke();
                
                // Start receiving messages (only once)
                if (!isReceiving)
                {
                    _ = ReceiveLoop(cancellationTokenSource.Token);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleWebSocket] Connection failed: {e.Message}");
                OnError?.Invoke(e.Message);
            }
#endif
        }

        public async void Send(string message)
        {
            if (!isConnected)
            {
                Debug.LogWarning("[SimpleWebSocket] Cannot send - not connected");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            WebSocketSend(message);
#else
            try
            {
                if (clientWebSocket == null || clientWebSocket.State != WebSocketState.Open)
                {
                    Debug.LogWarning("[SimpleWebSocket] Cannot send - WebSocket not in Open state");
                    return;
                }

                var bytes = Encoding.UTF8.GetBytes(message);
                var segment = new ArraySegment<byte>(bytes);
                
                // Await the send to ensure it completes
                await clientWebSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleWebSocket] Send failed: {e.Message}");
                OnError?.Invoke(e.Message);
            }
#endif
        }

        public async void Close()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebSocketClose();
            isConnected = false;
#else
            try
            {
                isConnected = false;
                
                if (clientWebSocket != null && clientWebSocket.State == WebSocketState.Open)
                {
                    cancellationTokenSource?.Cancel();
                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleWebSocket] Close error: {e.Message}");
            }
            finally
            {
                clientWebSocket?.Dispose();
                cancellationTokenSource?.Dispose();
            }
#endif
        }

        public void ProcessMessages()
        {
            // Process queued messages on Unity main thread
            lock (queueLock)
            {
                while (messageQueue.Count > 0)
                {
                    var message = messageQueue.Dequeue();
                    OnMessage?.Invoke(message);
                }
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            if (isReceiving)
            {
                Debug.LogWarning("[SimpleWebSocket] ReceiveLoop already running");
                return;
            }

            isReceiving = true;
            var buffer = new byte[8192]; // Larger buffer for bigger messages
            var messageBuilder = new StringBuilder();
            
            try
            {
                Debug.Log("[SimpleWebSocket] Starting receive loop");
                
                while (clientWebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    messageBuilder.Clear();
                    
                    do
                    {
                        var segment = new ArraySegment<byte>(buffer);
                        result = await clientWebSocket.ReceiveAsync(segment, cancellationToken);
                        
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Debug.Log("[SimpleWebSocket] Received close message");
                            isConnected = false;
                            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed", CancellationToken.None);
                            OnClose?.Invoke();
                            return;
                        }
                        
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            messageBuilder.Append(text);
                        }
                    }
                    while (!result.EndOfMessage);
                    
                    if (messageBuilder.Length > 0)
                    {
                        var completeMessage = messageBuilder.ToString();
                        lock (queueLock)
                        {
                            messageQueue.Enqueue(completeMessage);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[SimpleWebSocket] Receive loop cancelled");
            }
            catch (WebSocketException wsEx)
            {
                Debug.LogError($"[SimpleWebSocket] WebSocket error: {wsEx.Message}");
                if (!cancellationToken.IsCancellationRequested)
                {
                    OnError?.Invoke(wsEx.Message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleWebSocket] Receive error: {e.Message}\n{e.StackTrace}");
                if (!cancellationToken.IsCancellationRequested)
                {
                    OnError?.Invoke(e.Message);
                }
            }
            finally
            {
                isReceiving = false;
                if (isConnected)
                {
                    isConnected = false;
                    OnClose?.Invoke();
                }
                Debug.Log("[SimpleWebSocket] Receive loop ended");
            }
        }
#endif
    }
}
