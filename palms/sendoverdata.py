import yaml
import asyncio
import json
import websockets
import argparse
import os

# Extension server: b'MSG subject.pose 1 245\r\n{"position":{"z":-0.062335304915905,"y":-0.026992065832018852,"x":0.044517479836940765},"orientation":{"x":0.58768230676651,"w":-0.3598931133747101,"y":0.3324637711048126,"z":-0.3720678985118866},"channel":"B11772DB-2A8B-4647-A9D3-3B6CD439358C"}\r\n'

# xyzw: x 30deg
q30 = [0.258819, 0, 0, 0.9659258]
q0 = [0., 0., 0., 1.]
# xyzw: x -30deg   
q30inv = [-0.258819, 0, 0, 0.9659258]

async def send_nats_message(json_data):
    uri = "wss://service.zenimotion.com/nats"
    # uri = "ws://134.209.218.187:8081/nats"
    
    # Message format is assumed to be a JSON object
    msg_json = json.dumps(json_data)
    msglen = len(msg_json)
    print(msg_json)

    async with websockets.connect(uri) as websocket:
        message = f"PUB subject.pose {msglen}\r\n{msg_json}\r\n"
        await websocket.send(message)
        print("Sent message to NATS WebSocket")

def load_map_config(yaml_path):
    with open(yaml_path) as f:
        cfg = yaml.safe_load(f)
    x, y, z = cfg["position"][:3]
    qx, qy, qz, qw = cfg["quat"][:4]
    return x, y, z, qx, qy, qz, qw

def send_json_files_in_directory(directory_path):
    """Iterate through all JSON files in the given directory and send them."""
    for filename in os.listdir(directory_path):
        if filename.endswith(".json"):
            json_file_path = os.path.join(directory_path, filename)
            with open(json_file_path, 'r') as f:
                json_data = json.load(f)
                asyncio.run(send_nats_message(json_data))

# Get the directory where the script is located
script_directory = os.path.dirname(os.path.abspath(__file__))

parser = argparse.ArgumentParser(
    description="send a websocket msg containing target pose")

parser.add_argument("--input", required=False, default="pose_target.yaml", help="Path to raw YAML file with pose config")

args = parser.parse_args()

# Send each JSON file in the directory where the Python script is located
send_json_files_in_directory(script_directory)
