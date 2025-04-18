import yaml
import asyncio
import json
import websockets
import argparse
#Extension server: b'MSG subject.pose 1 245\r\n{"position":{"z":-0.062335304915905,"y":-0.026992065832018852,"x":0.044517479836940765},"orientation":{"x":0.58768230676651,"w":-0.3598931133747101,"y":0.3324637711048126,"z":-0.3720678985118866},"channel":"B11772DB-2A8B-4647-A9D3-3B6CD439358C"}\r\n'

#xyzw: x 30deg 
q30 = [ 0.258819, 0, 0, 0.9659258 ]
q0 = [ 0., 0., 0., 1. ]
#xyzw: x -30deg   
q30inv = [ -0.258819, 0, 0, 0.9659258 ]
async def send_nats_message(x=0.,y=0.,z=0., quat_target=None):
  uri = "wss://service.zenimotion.com/nats"
  # uri = "ws://134.209.218.187:8081/nats"
  if not quat_target is None: 
    quat0= quat_target
  else:
    quat0=q0
  msg_json = json.dumps({"position":{"z": z, "y": y, "x": x}, "orientation":{"x": quat0[0], "w": quat0[3], "y": quat0[1], "z": quat0[2]}, "channel":"B11772DB-2A8B-4647-A9D3-3B6CD439350C"})
  msglen=len(msg_json)
  print(msg_json)
  async with websockets.connect(uri) as websocket:
    #message = "PUB subject.pose 5\r\nHello\r\n"
    #message = 'PUB subject.pose 245\r\n{"position":{"z":-0.062335304915905,"y":-0.026992065832018852,"x":0.144517479836940765},"orientation":{"x":0.58768230676651,"w":-0.3598931133747101,"y":0.3324637711048126,"z":-0.3720678985118866},"channel":"B11772DB-2A8B-4647-A9D3-3B6CD439350C"}\r\n'
    message = f"PUB subject.pose {msglen}\r\n{msg_json}\r\n"
    await websocket.send(message)
    print("Sent message to NATS WebSocket")

def load_map_config(yaml_path):
    with open(yaml_path) as f:
        cfg = yaml.safe_load(f)
    x, y,z = cfg["position"][:3]
    qx, qy, qz, qw = cfg["quat"][:4]
    return x, y, z, qx, qy, qz, qw 

parser = argparse.ArgumentParser(
        description="send a websocket msg contain target pose")

parser.add_argument("--input", required=False, default="pose_target.yaml", help="Path to raw CSV file")
args = parser.parse_args()
x,y,z,qx,qy,qz,qw = load_map_config(args.input)

asyncio.run(send_nats_message(x=x, y=y, z=z, quat_target = [qx,qy,qz,qw])) 

