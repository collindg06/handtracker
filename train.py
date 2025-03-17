import os
import torch
import torch.nn as nn
import torch.optim as optim
from torchvision import transforms
from torch.utils.data import Dataset, DataLoader
from PIL import Image
import random
from sklearn.model_selection import train_test_split

# Device
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(f'Using device: {device}')

# Hyperparameters
num_epochs = 40
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

label_names = [
    'left', 'up', 'right', 'down',
    'backwards', 'forward', 'turn left', 'turn right'
]

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
                break  # Only one match needed

    return file_list, labels

# Dataset loading
data_folder = 'newdata'
file_list, labels = get_image_paths_and_labels(data_folder)

# Shuffle and split dataset
train_files, val_files, train_labels, val_labels = train_test_split(file_list, labels, test_size=0.2, random_state=42)

# Transforms
train_transform = transforms.Compose([
    transforms.Resize((128, 128)),
    transforms.RandomHorizontalFlip(),
    #transforms.RandomRotation(10),
    transforms.ToTensor(),
])

val_transform = transforms.Compose([
    transforms.Resize((128, 128)),
    transforms.ToTensor(),
])

# Datasets
train_dataset = HandPositionDataset(train_files, train_labels, transform=train_transform)
val_dataset = HandPositionDataset(val_files, val_labels, transform=val_transform)

train_loader = DataLoader(train_dataset, batch_size=batch_size, shuffle=True)
val_loader = DataLoader(val_dataset, batch_size=batch_size, shuffle=False)

# CNN model
class SimpleCNN(nn.Module):
    def __init__(self, num_classes):
        super(SimpleCNN, self).__init__()
        self.features = nn.Sequential(
            nn.Conv2d(3, 32, kernel_size=3, stride=1, padding=1),
            nn.ReLU(inplace=True),
            nn.MaxPool2d(2, 2),

            nn.Conv2d(32, 64, kernel_size=3, stride=1, padding=1),
            nn.ReLU(inplace=True),
            nn.MaxPool2d(2, 2),

            nn.Conv2d(64, 128, kernel_size=3, stride=1, padding=1),
            nn.ReLU(inplace=True),
            nn.MaxPool2d(2, 2),
        )
        self.classifier = nn.Sequential(
            nn.Linear(128 * 16 * 16, 256),
            nn.ReLU(inplace=True),
            nn.Dropout(0.5),
            nn.Linear(256, num_classes)
        )

    def forward(self, x):
        x = self.features(x)
        x = x.view(x.size(0), -1)  # Flatten
        x = self.classifier(x)
        return x

# Model, loss, optimizer
num_classes = len(label_map)
model = SimpleCNN(num_classes=num_classes).to(device)
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
torch.save(model.state_dict(), 'hand_position_classifier.pth')
print('Model saved ')
