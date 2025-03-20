import asyncio
import websockets

async def test_websocket():
    uri = "wss://service.zenimotion.com/nats"  # Replace with any other WebSocket server if needed
    #uri = "wss://echo.websocket.org"  # Replace with any other WebSocket server if needed
    #uri = "wss://ws.ifelse.io"  # Replace with any other WebSocket server if needed
    #websocket = new WebSocket("wss://service.zenimotion.com/nats");
    #websocket = new WebSocket("wss://echo.websocket.org");

    async with websockets.connect(uri) as websocket:
        await websocket.send("Hello, WebSocket!")
        response = await websocket.recv()
        print("Received:", response)

asyncio.run(test_websocket())

