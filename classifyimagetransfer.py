import torch
import torch.nn as nn
import torchvision.transforms as transforms
from torchvision import models
from PIL import Image

# Device setup
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

# Path to model and image (update these as needed)
MODEL_PATH = 'mobilenetv3_hand_position.pth'
IMAGE_PATH = "samp.png"

# Class labels
label_map = {
    0: "left",
    1: "up",
    2: "right",
    3: "down",
    4: "backwards",
    5: "forward",
    6: "turn left",
    7: "turn right"
}

# Load MobileNetV3 model
def load_model(model_path, num_classes=8):
    model = models.mobilenet_v3_large(weights=None)  # No pre-trained weights
    model.classifier[3] = nn.Linear(model.classifier[3].in_features, num_classes)
    model.load_state_dict(torch.load(model_path, map_location=device))
    model.to(device)
    model.eval()
    return model

# Preprocess image
def preprocess_image(image_path):
    transform = transforms.Compose([
        transforms.Resize((224, 224)),  
        transforms.ToTensor(),
        transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
    ])
    image = Image.open(image_path).convert("RGB")
    image = transform(image).unsqueeze(0)  # Add batch dimension
    return image.to(device)

# Predict class
def predict(image_path, model):
    image = preprocess_image(image_path)
    with torch.no_grad():
        outputs = model(image)
        _, predicted = torch.max(outputs, 1)
    return label_map[predicted.item()]

# Load model and predict
model = load_model(MODEL_PATH)
prediction = predict(IMAGE_PATH, model)

print(f"Predicted class: {prediction}")
