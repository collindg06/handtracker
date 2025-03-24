using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using NativeWebSocket;



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
    websocket = new WebSocket("wss://service.zenimotion.com/nats");
    //websocket = new WebSocket("wss://echo.websocket.org");
    //websocket = new WebSocket("ws://134.209.218.187:8081/nats");

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
      Debug.Log("OnMessage!");
      //Debug.Log(bytes);
      string message = Encoding.UTF8.GetString(bytes); //new
      Debug.Log($"Received: {message}"); //new
      // getting the message as a string
      // var message = System.Text.Encoding.UTF8.GetString(bytes);
      // Debug.Log("OnMessage! " + message);
    };

    // Keep sending messages at every 0.3s
    InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);
    // waiting for messages
    await websocket.Connect();
    

  }

  void Update()
  {
    #if !UNITY_WEBGL || UNITY_EDITOR
      websocket.DispatchMessageQueue();
    #endif
  }

  async void SendWebSocketMessage()
  {
    byte[] messageBytes = Encoding.UTF8.GetBytes("PUB test.subject 5\r\nHello\r\n");
    Debug.Log("Number of bytes: " + messageBytes.Length);
    if (websocket.State == WebSocketState.Open)
    {
       //Sending bytes
      //await websocket.Send(new byte[] { 10, 20, 30 });

      // Sending plain text
      await websocket.SendText("PUB subject.pose 5\r\nHello\r\n");
      
      //await websocket.SendText(“PUB jointValues xxx\r\nyyy\r\n”);
    }
  }

  private async void OnApplicationQuit()
  {
    await websocket.Close();
  }

}