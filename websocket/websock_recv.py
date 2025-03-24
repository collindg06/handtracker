import asyncio
import websockets

async def receive_nats_messages():
  uri = "wss://service.zenimotion.com/nats"
  async with websockets.connect(uri) as websocket:
    #  Correct NATS WebSocket subscription command
    subscribe_command = "SUB subject.pose 1\r\n"
    await websocket.send(subscribe_command)
    print("Subscribed to 'test.subject' on NATS")

    while True:
      try:
        message = await websocket.recv()
        print(f" Received: {message}")
      except websockets.exceptions.ConnectionClosed:
        print(" Connection closed by server")
        break

asyncio.run(receive_nats_messages()) 
