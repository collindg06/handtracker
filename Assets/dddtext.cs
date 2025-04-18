using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Newtonsoft.Json;
using Unity.Barracuda;

public class HandTrackingAndONNX : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI countdownText;
    public Camera CaptureCamera;
    public Connection connection;

    private XRHandSubsystem handSubsystem;
    private float lastClapTime = -Mathf.Infinity;
    private bool isWaitingForDataCollection = false;

    [Header("Clap Detection Settings")]
    public float clapDistanceThreshold = 0.1f;
    public float clapCooldown = 2.0f;

    [Header("Barracuda Model")]
    public NNModel modelAsset;
    private Model runtimeModel;
    private IWorker worker;

    private bool isPredicting = false;
    private int sampleNumber = 0;

    void Awake()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null)
        {
            handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
        }

        UpdateCountdownText("Waiting for Clap...");
        UpdateDebugText("");
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }

    void Update()
    {
        if (handSubsystem != null && !isWaitingForDataCollection)
        {
            CheckForClap(handSubsystem.leftHand, handSubsystem.rightHand);
        }
    }

    void CheckForClap(XRHand leftHand, XRHand rightHand)
    {
        if (!leftHand.isTracked || !rightHand.isTracked) return;

        XRHandJoint leftPalm = leftHand.GetJoint(XRHandJointID.Palm);
        XRHandJoint rightPalm = rightHand.GetJoint(XRHandJointID.Palm);

        if (!leftPalm.TryGetPose(out Pose leftPose) || !rightPalm.TryGetPose(out Pose rightPose)) return;

        if (Vector3.Distance(leftPose.position, rightPose.position) <= clapDistanceThreshold && Time.time - lastClapTime > clapCooldown)
        {
            lastClapTime = Time.time;
            UpdateCountdownText("Clap Detected!");
            StartCoroutine(HandleClapDetection());
        }
    }

    IEnumerator HandleClapDetection()
    {
        isWaitingForDataCollection = true;
        yield return new WaitForSeconds(0.5f);

        isPredicting = !isPredicting;
        UpdateCountdownText(isPredicting ? "Started Predicting..." : "Stopped Predicting");
        UpdateDebugText(isPredicting ? "Predicting hand gestures..." : "Paused prediction");

        if (isPredicting)
        {
            StartCoroutine(ContinuousPrediction());
        }

        yield return new WaitForSeconds(1f);
        StartCoroutine(ResetClapAfterDelay(1.5f));
    }

    IEnumerator ResetClapAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        lastClapTime = Time.time;
        isWaitingForDataCollection = false;
    }

    IEnumerator ContinuousPrediction()
    {
        while (isPredicting)
        {
            yield return new WaitForEndOfFrame();

            RenderTexture renderTex = new RenderTexture(224, 224, 24);
            CaptureCamera.targetTexture = renderTex;
            CaptureCamera.Render();

            RenderTexture.active = renderTex;
            Texture2D tex = new Texture2D(224, 224, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 224, 224), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            CaptureCamera.targetTexture = null;
            renderTex.Release();

            string predictedGesture = PredictGesture(tex);
            Destroy(tex);

            if (!isPredicting) yield break;

            Debug.Log($"Predicted Gesture: {predictedGesture}");
            UpdateDebugText($"Predicted Gesture: {predictedGesture}");

            SendHandJointData(predictedGesture);

            yield return new WaitForSeconds(0.2f);
        }
    }

    string PredictGesture(Texture2D image)
    {
        using var tensor = ProcessImageToTensor(image);
        worker.Execute(tensor);

        using var output = worker.CopyOutput();
        int predictedIndex = output.ArgMax()[0];

        Debug.Log("Model Raw Output:");
        for (int i = 0; i < output.length; i++)
            Debug.Log($"Class {i}: {output[i]}");

        return GetGestureName(predictedIndex);
    }

    Tensor ProcessImageToTensor(Texture2D texture)
    {
        int width = 224;
        int height = 224;
        Texture2D resized = ResizeAndFlipTexture(texture, width, height);
        Color[] pixels = resized.GetPixels();

        float[] tensorData = new float[width * height * 3];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;
                Color pixel = pixels[pixelIndex];

                tensorData[pixelIndex * 3 + 0] = (pixel.r - 0.485f) / 0.229f;
                tensorData[pixelIndex * 3 + 1] = (pixel.g - 0.456f) / 0.224f;
                tensorData[pixelIndex * 3 + 2] = (pixel.b - 0.406f) / 0.225f;
            }
        }

        return new Tensor(1, height, width, 3, tensorData);
    }

    Texture2D ResizeAndFlipTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        rt.Release();

        Color[] pixels = result.GetPixels();
        Color[] flipped = new Color[pixels.Length];
        int rowLength = width;

        for (int y = 0; y < height; y++)
        {
            Array.Copy(pixels, y * rowLength, flipped, (height - 1 - y) * rowLength, rowLength);
        }

        result.SetPixels(flipped);
        result.Apply();

        return result;
    }

    void SendHandJointData(string gesture)
    {
        if (handSubsystem == null || !handSubsystem.leftHand.isTracked) return;

        XRHand hand = handSubsystem.leftHand;
        int maxJointID = (int)XRHandJointID.LittleTip;
        sampleNumber++;
        string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        Dictionary<string, object> jointData = new Dictionary<string, object>
        {
            { "timestamp", timeStamp },
            { "gesture", gesture },
            { "sampleNumber", sampleNumber }
        };

        for (int i = 0; i <= maxJointID; i++)
        {
            XRHandJointID jointID = (XRHandJointID)i;
            if (jointID == XRHandJointID.Invalid) continue;

            XRHandJoint joint = hand.GetJoint(jointID);
            if (joint.TryGetPose(out Pose pose))
            {
                jointData[$"{jointID}_X"] = Math.Round(pose.position.x, 2);
                jointData[$"{jointID}_Y"] = Math.Round(pose.position.y, 2);
                jointData[$"{jointID}_Z"] = Math.Round(pose.position.z, 2);
            }
        }

        string jsonMessage = JsonConvert.SerializeObject(jointData, Formatting.Indented);
        connection?.SendHandData("hand.jointData", jsonMessage);
        connection?.SendHandData("hand.prediction", $"\"{gesture}\"");
    }

    string GetGestureName(int index)
    {
        string[] gestures = { "left", "up", "right", "down", "back", "forward", "turn left", "turn right" };
        return index >= 0 && index < gestures.Length ? gestures[index] : "Unknown Gesture";
    }

    void UpdateCountdownText(string message)
    {
        if (countdownText != null) countdownText.text = message;
    }

    void UpdateDebugText(string message)
    {
        if (debugText != null) debugText.text = message;
    }
}
