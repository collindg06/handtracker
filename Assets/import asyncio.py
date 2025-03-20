import asyncio
import websockets

async def send_nats_message():
  uri = "wss://service.zenimotion.com/nats"
  async with websockets.connect(uri) as websocket:
    message = "PUB test.subject 5\r\nHello\r\n"
    await websocket.send(message)
    print("Sent message to NATS WebSocket")

asyncio.run(send_nats_message()) 