import os 
import torch
import torch.nn as nn
import torch.optim as optim
from torchvision import transforms, models
from torch.utils.data import Dataset, DataLoader
from PIL import Image
from sklearn.model_selection import train_test_split

# Device
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(f'Using device: {device}')

# Hyperparameters
num_epochs = 30
batch_size = 32
learning_rate = 0.001

# Class labels mapping
label_map = {
    'clap_1': 0,  # left
    'clap_2': 1,  # up
    'clap_3': 2,  # right
    'clap_4': 3,  # down
    'clap_5': 4,  # backwards
    'clap_6': 5,  # forward
    'clap_7': 6,  # turn left
    'clap_8': 7   # turn right
}

# Custom dataset
class HandPositionDataset(Dataset):
    def __init__(self, file_list, labels, transform=None):
        self.file_list = file_list
        self.labels = labels
        self.transform = transform

    def __len__(self):
        return len(self.file_list)

    def __getitem__(self, idx):
        img_path = self.file_list[idx]
        image = Image.open(img_path).convert('RGB')

        if self.transform:
            image = self.transform(image)

        label = self.labels[idx]
        return image, label

# Collect image paths and labels
def get_image_paths_and_labels(data_folder):
    all_files = [os.path.join(data_folder, f) for f in os.listdir(data_folder) if f.lower().endswith(('.png', '.jpg', '.jpeg'))]
    
    file_list = []
    labels = []

    for file_path in all_files:
        for clap_key in label_map:
            if clap_key in os.path.basename(file_path):
                file_list.append(file_path)
                labels.append(label_map[clap_key])
                break

    return file_list, labels

# Dataset loading
data_folder = 'newdata'
file_list, labels = get_image_paths_and_labels(data_folder)

# Shuffle and split dataset
train_files, val_files, train_labels, val_labels = train_test_split(file_list, labels, test_size=0.2, random_state=42)

# Transforms
train_transform = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
])

val_transform = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
])

# Datasets
train_dataset = HandPositionDataset(train_files, train_labels, transform=train_transform)
val_dataset = HandPositionDataset(val_files, val_labels, transform=val_transform)

train_loader = DataLoader(train_dataset, batch_size=batch_size, shuffle=True)
val_loader = DataLoader(val_dataset, batch_size=batch_size, shuffle=False)

# Load MobileNetV3 model
model = models.mobilenet_v3_large(pretrained=True)
model.classifier[3] = nn.Linear(model.classifier[3].in_features, len(label_map))
model = model.to(device)

# Loss and optimizer
criterion = nn.CrossEntropyLoss()
optimizer = optim.Adam(model.parameters(), lr=learning_rate)

# Training/Validation loop
def train_and_validate(model, train_loader, val_loader, criterion, optimizer, num_epochs):
    for epoch in range(num_epochs):
        # Training
        model.train()
        running_loss = 0.0
        correct = 0
        total = 0

        for images, labels in train_loader:
            images, labels = images.to(device), labels.to(device)

            optimizer.zero_grad()
            outputs = model(images)
            loss = criterion(outputs, labels)
            loss.backward()
            optimizer.step()

            running_loss += loss.item() * images.size(0)
            _, predicted = outputs.max(1)
            total += labels.size(0)
            correct += predicted.eq(labels).sum().item()

        train_loss = running_loss / len(train_loader.dataset)
        train_acc = 100. * correct / total

        # Validation
        model.eval()
        val_loss = 0.0
        correct = 0
        total = 0

        with torch.no_grad():
            for images, labels in val_loader:
                images, labels = images.to(device), labels.to(device)
                outputs = model(images)
                loss = criterion(outputs, labels)

                val_loss += loss.item() * images.size(0)
                _, predicted = outputs.max(1)
                total += labels.size(0)
                correct += predicted.eq(labels).sum().item()

        val_loss /= len(val_loader.dataset)
        val_acc = 100. * correct / total

        print(f'Epoch [{epoch+1}/{num_epochs}] '
              f'Train Loss: {train_loss:.4f}, Train Acc: {train_acc:.2f}% '
              f'Val Loss: {val_loss:.4f}, Val Acc: {val_acc:.2f}%')

# Train
train_and_validate(model, train_loader, val_loader, criterion, optimizer, num_epochs)

# Save model
torch.save(model.state_dict(), 'mobilenetv3_hand_position.pth')
print('Model saved')
