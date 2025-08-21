import torch
import torchvision.models as models

# Define the number of output classes
num_classes = 8  # Since we have 8 gesture classes

# Load the trained EfficientNetV2 model
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model = models.efficientnet_v2_s(pretrained=False)  # No need to load pre-trained weights
num_ftrs = model.classifier[1].in_features
model.classifier[1] = torch.nn.Linear(num_ftrs, num_classes)  # Modify classifier for our classes

# Load the trained weights
model.load_state_dict(torch.load('efficientnetv2_clapsgood.pth', map_location=device))
model.to(device)
model.eval()

# Create a dummy input tensor (batch_size=1, channels=3, height=224, width=224)
dummy_input = torch.randn(1, 3, 224, 224).to(device)

# Convert to ONNX
onnx_filename = "efficientnetv2_clapsgood.onnx"
torch.onnx.export(
    model,                      # Model to convert
    dummy_input,                # Example input tensor
    onnx_filename,              # Output ONNX file name
    export_params=True,         # Store trained parameters
    opset_version=11,           # ONNX version
    do_constant_folding=True,   # Optimize the model
    input_names=['input'],      # Name of input layer
    output_names=['output'],    # Name of output layer
    dynamic_axes={'input': {0: 'batch_size'}, 'output': {0: 'batch_size'}}  # Allow dynamic batch size
)

print(f"Model successfully converted to {onnx_filename}")
