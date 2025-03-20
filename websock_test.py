import asyncio
import websockets
import ssl

async def test_websocket():
    uri = "wss://service.zenimotion.com/nats"  # Replace with any other WebSocket server if needed
    #uri = "wss://echo.websocket.org"  # Replace with any other WebSocket server if needed
    #uri = "wss://ws.ifelse.io"  # Replace with any other WebSocket server if needed
    #websocket = new WebSocket("wss://service.zenimotion.com/nats");
    #websocket = new WebSocket("wss://echo.websocket.org");
    ssl_context = ssl._create_unverified_context()  # Bypass SSL verification
    #async with websockets.connect(uri, ssl=ssl_context) as websocket:
    async with websockets.connect(uri) as websocket:
        await websocket.send("PUB test.subject 5\r\nHello\r\n")
        response = await websocket.recv()
        print("Received:", response)

asyncio.run(test_websocket())

