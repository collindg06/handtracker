using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using NativeWebSocket;
using System.Threading.Tasks;




public class Connection : MonoBehaviour
{
  WebSocket websocket;


  // new
  public async void SendHandData(string subject, string jsonMessage)
  {
    if (websocket != null && websocket.State == WebSocketState.Open)
    {
      //Format the NATS publish message: PUB <subject> <bytes>\r\n<messageBody>\r\n
      string message = $"PUB subject.pose {Encoding.UTF8.GetByteCount(jsonMessage)}\r\n{jsonMessage}\r\n";

      Debug.Log($"Sending: {message}");

      await websocket.SendText(message);
    }
    else
    {
      Debug.LogWarning("WebSocket not open. Cannot send message.");
    }
  }



  // Start is called before the first frame update
  async void Start()
  {
    Debug.Log("Starting Connection script on " + gameObject.name);
    ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

    //ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
    //{
    //Debug.Log($"Certificate subject: {certificate.Subject}");
    //Debug.Log($"SSL policy errors: {sslPolicyErrors}");
    //return true;
    //};
    websocket = new WebSocket("wss://service.zenimotion.com/nats");
    //websocket = new WebSocket("wss://echo.websocket.org");
    //websocket = new WebSocket("ws://134.209.218.187:8081/nats");
    Debug.Log("about open!");

    websocket.OnOpen += () =>
    {
      Debug.Log("Connection open!");
      SendWebSocketMessage();
    };

    websocket.OnError += (e) =>
    {
      Debug.Log("Error! " + e);
    };

    websocket.OnClose += (e) =>
    {
      Debug.Log("Connection closed!");
    };

    websocket.OnMessage += (bytes) =>
    {
      Debug.Log("Received OnMessage!");
      //Debug.Log(bytes);
      string message = Encoding.UTF8.GetString(bytes); //new
      Debug.Log($"Received: {message}"); //new
      // getting the message as a string
      // var message = System.Text.Encoding.UTF8.GetString(bytes);
      // Debug.Log("OnMessage! " + message);
    };

    // Keep sending messages at every 0.3s
    InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);
    Debug.Log("before calling Connect!");
    // waiting for messages

    Debug.Log("Before calling Connect...");

    //try
    //{
    //var ips = Dns.GetHostAddresses("service.zenimotion.com");
    //Debug.Log("Resolved DNS: " + string.Join(", ", Array.ConvertAll(ips, ip => ip.ToString())));

    //}
    //catch (Exception e)
    //{
    //Debug.LogError("DNS lookup failed: " + e.Message);
    //}

    //try
    //{
    //await websocket.Connect();
    //if (websocket.State == WebSocketState.Open)
    //{
    //Debug.Log("WebSocket connected successfully.");
    //}
    //else
    //{
    //Debug.LogWarning("WebSocket state after connect: " + websocket.State);
    //}
    //}
    //catch (Exception ex)
    //{
    //Debug.LogError("WebSocket connection threw exception: " + ex.Message);
    //Debug.LogError("Stack trace: " + ex.StackTrace);
    //if (ex.InnerException != null)
    //{
    //Debug.LogError("Inner Exception: " + ex.InnerException.Message);
    //}
    //}


    await websocket.Connect();
    Debug.Log("after calling Connect!");


  }

  void Update()
  {
#if !UNITY_WEBGL || UNITY_EDITOR
    websocket.DispatchMessageQueue();
#endif
  }

  async void SendWebSocketMessage()
  {
    string msg_str = "PUB subject.pose 5\r\nHello\r\n";
    byte[] messageBytes = Encoding.UTF8.GetBytes(msg_str);
    Debug.Log("inside SendWebSocketMessage (timer trigged)!");

    if (websocket.State == WebSocketState.Open)
    {
      //Sending bytes
      //await websocket.Send(new byte[] { 10, 20, 30 });

      // Sending plain text
      Debug.Log("Sending: Number of bytes: " + messageBytes.Length);
      await websocket.SendText(msg_str);

      //await websocket.SendText(“PUB jointValues xxx\r\nyyy\r\n”);
    }
  }

  private async void OnApplicationQuit()
  {
    Debug.Log("\n \n \n end here \n \n \n");
    await websocket.Close();
  }

}