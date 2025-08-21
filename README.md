# Hand Tracker

## App that predicts the hand gesture from a Meta Quest and sends the data to a remote server

- I recorded 2,400 hand images using an altered version of the Unity app and then trained the EfficientNetV2 Pytorch model using traineffic.py to be able to classify the hand gesture. I used converteffic.py to change this to an onnx model so it can be used in Unity. Webs.cs is used to send the classifications to a remote server so that the app could be integrated with other robotic or embedded system projects. This would make it easy for them to be able to aquire the classification.

## Requirements

- Must have Meta Quest.
- Must have Computer that is compatible with Meta Quest.
- Must have SideQuest downloaded.

## Instructions to run

- Download ht.apk.
- Sideload the app to your Meta Quest.
- Open the app in Meta Quest library under unknown sources.
