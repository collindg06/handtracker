using System;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.IO;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;



public class HandTrackingJoints : MonoBehaviour
{

    public Connection connection;  // new

    [Header("UI Elements")]
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI countdownText;

    private XRHandSubsystem handSubsystem;
    //private string downloadsFolder;
    private string filePath;

    [Header("Clap Detection Settings")]
    public float clapDistanceThreshold = 0.1f;
    public float clapCooldown = 2.0f;

    private bool isWaitingForDataCollection = false;
    private float lastClapTime = -Mathf.Infinity;

    [Header("Sampling Settings")]
    public int numberOfSamples = 100;
    public float sampleInterval = 0.2f;

    private int clapIndex = 0;

    private readonly List<string> gestureList = new List<string>
    {
        "palm open", "thumb", "index", "middle", "ring", "pinky", "palm closed"
    };


    private List<string> jointHeaders = new List<string>();

    void Start()
    {
        clapIndex = 0;
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null)
        {
            handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
        }

        //downloadsFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads", "palms");


        //if (!Directory.Exists(downloadsFolder))
       // {
        //    Directory.CreateDirectory(downloadsFolder);
       // }

Debug.Log("\n \n \n Start here \n \n \n");
        filePath = Path.Combine(Application.persistentDataPath, "hand_tracking_data.csv");
        Debug.Log("xxxxx filepath " + filePath.ToString());
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("Existing CSV file deleted.");
        }

        GenerateJointHeaders();

        string headerLine = $"ClapIndex,TimeStamp,Gesture,SampleNumber,{string.Join(",", jointHeaders)}\n";
        File.WriteAllText(filePath, headerLine);

        UpdateCountdownText("Waiting for Clap...");
        UpdateDebugText("");
    }

    void Update()
    {
        if (handSubsystem != null)
        {
            XRHand leftHand = handSubsystem.leftHand;
            XRHand rightHand = handSubsystem.rightHand;

            string message = "Hand Tracking Active\n";
            message += leftHand.isTracked ? "Left Hand: Tracked\n" : "Left Hand Not Tracked\n";
            message += rightHand.isTracked ? "Right Hand: Tracked\n" : "Right Hand Not Tracked\n";

            UpdateDebugText(message);

            if (!isWaitingForDataCollection)
            {
                CheckForClap(leftHand, rightHand);
            }
        }
        else
        {
            UpdateCountdownText("");
            UpdateDebugText("Hand Tracking Subsystem Not Found!");
        }
    }

    void CheckForClap(XRHand leftHand, XRHand rightHand)
    {
        if (!leftHand.isTracked || !rightHand.isTracked) return;

        XRHandJoint leftPalm = leftHand.GetJoint(XRHandJointID.Palm);
        XRHandJoint rightPalm = rightHand.GetJoint(XRHandJointID.Palm);

        if (!leftPalm.TryGetPose(out Pose leftPose) || !rightPalm.TryGetPose(out Pose rightPose)) return;

        float distance = Vector3.Distance(leftPose.position, rightPose.position);

        if (distance <= clapDistanceThreshold)
        {
            if (Time.time - lastClapTime > clapCooldown)
            {
                Debug.Log("Clap detected!");
                lastClapTime = Time.time;

                UpdateCountdownText("Clap Detected!");
                UpdateDebugText("");

                StartCoroutine(WaitAndCollectSamples(3f));
            }
        }
    }
    [System.Serializable]
    public class JointData
    {
        public float x;
        public float y;
        public float z;

        public JointData(float x, float y, float z)
        {
            this.x = (float)System.Math.Round(x, 2);
            this.y = (float)System.Math.Round(y, 2);
            this.z = (float)System.Math.Round(z, 2);
        }
    }

    [System.Serializable]
    public class SampleData
    {
        public int clapIndex;
        public string timestamp;
        public string gesture;
        public int sampleNumber;
        public Dictionary<string, JointData> jointData = new Dictionary<string, JointData>();
    }


    IEnumerator WaitAndCollectSamples(float waitTime)
    {
        isWaitingForDataCollection = true;

        for (int secondsLeft = (int)waitTime; secondsLeft > 0; secondsLeft--)
        {
            UpdateCountdownText($"Starting in {secondsLeft}...");
            yield return new WaitForSeconds(1f);
        }

        UpdateCountdownText("Starting data collection...");

        XRHand leftHand = handSubsystem.leftHand;

        if (leftHand.isTracked)
        {
            string gesture = GetGestureForClap(clapIndex);
            clapIndex++;

            yield return StartCoroutine(CollectSamples(leftHand, gesture));
        }
        else
        {
            Debug.Log("Left hand not tracked after delay.");
            UpdateCountdownText("Left Hand Not Tracked After Delay!");
        }

        UpdateCountdownText("Done!");

        yield return new WaitForSeconds(2f);

        UpdateCountdownText("Waiting for Clap...");
        UpdateDebugText("");

        isWaitingForDataCollection = false;
    }

    IEnumerator CollectSamples(XRHand hand, string gesture)
    {
        int maxJointID = (int)XRHandJointID.LittleTip;
        string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        for (int sample = 0; sample < numberOfSamples; sample++)
        {
            Dictionary<string, object> jointData = new Dictionary<string, object>
            {
                { "clapIndex", clapIndex },
                { "timestamp", timeStamp },
                { "gesture", gesture },
                { "sampleNumber", sample + 1 }
            };

            // Collect joint data
            for (int i = 0; i <= maxJointID; i++)
            {
                XRHandJointID jointID = (XRHandJointID)i;

                if (jointID == XRHandJointID.Invalid || i < 0 || i > maxJointID)
                    continue;

                XRHandJoint joint = hand.GetJoint(jointID);

                if (joint != null && joint.TryGetPose(out Pose pose))
                {
                    // Include joint names before the data, rounded to 2 decimal places
                    jointData[$"{jointID}_X"] = Math.Round(pose.position.x, 2);
                    jointData[$"{jointID}_Y"] = Math.Round(pose.position.y, 2);
                    jointData[$"{jointID}_Z"] = Math.Round(pose.position.z, 2);
                }
                else
                {
                    jointData[$"{jointID}_X"] = "NaN";
                    jointData[$"{jointID}_Y"] = "NaN";
                    jointData[$"{jointID}_Z"] = "NaN";
                }
            }
            // Create a SampleData object
            SampleData sampleData = new SampleData
            {
                clapIndex = clapIndex,
                timestamp = timeStamp,
                gesture = gesture,
                sampleNumber = sample + 1
            };

            // Populate jointData in SampleData object
            foreach (var joint in jointData)
            {
                // The joint name is already part of the key (e.g., "JointName_X")
                if (joint.Key.EndsWith("_X"))
                {
                    string jointName = joint.Key.Split('_')[0];  // Extract the joint name
                    float x = Convert.ToSingle(jointData[$"{jointName}_X"]);
                    float y = Convert.ToSingle(jointData[$"{jointName}_Y"]);
                    float z = Convert.ToSingle(jointData[$"{jointName}_Z"]);

                    // Store the joint data using the joint name
                    sampleData.jointData[jointName] = new JointData(x, y, z);
                }
            }


            // Save sample data to CSV
            SaveSampleToCSV(sampleData);


            // Convert to JSON string
            string jsonMessage = JsonConvert.SerializeObject(jointData, Newtonsoft.Json.Formatting.Indented);



            // Define JSON file path
            string jsonFileName = $"{gesture}_sample_{sample + 1}.json";
            string jsonFilePath = Path.Combine(Application.persistentDataPath, $"{gesture}_sample_{sample + 1}.json");
File.WriteAllText(jsonFilePath, jsonMessage);


            Debug.Log($"JSON saved to: {jsonFilePath}");



            // Create the PUB message
            string pubMessage = $"PUB hand.jointData {Encoding.UTF8.GetByteCount(jsonMessage)}\r\n{jsonMessage}\r\n";

            Debug.Log($"Sending: {pubMessage}");

            // Send the JSON message via WebSocket
            if (connection != null)
            {
                connection.SendHandData("hand.jointData", jsonMessage);
            }

            // NEW: Take a screenshot for each sample!
            TakeScreenshot(gesture, sample + 1);

            UpdateDebugText($"Collecting Data...\nSamples Collected: {sample + 1}/{numberOfSamples}");

            yield return new WaitForSeconds(sampleInterval);
        }

        Debug.Log($"Saved {numberOfSamples} samples for clap {clapIndex}");

        UpdateDebugText($"Data Collection Complete for Clap {clapIndex}!");
    }


    void SaveSampleToCSV(SampleData sample)
    {
        StringBuilder rowBuilder = new StringBuilder();

        rowBuilder.Append($"{sample.clapIndex},{sample.timestamp},{sample.gesture},{sample.sampleNumber},");

        List<string> jointValues = new List<string>();

        foreach (var joint in sample.jointData)
        {
            jointValues.Add($"{joint.Value.x:F4}");
            jointValues.Add($"{joint.Value.y:F4}");
            jointValues.Add($"{joint.Value.z:F4}");
        }

        rowBuilder.Append(string.Join(",", jointValues));
        rowBuilder.AppendLine();

        File.AppendAllText(filePath, rowBuilder.ToString());
    }
    void TakeScreenshot(string gesture, int sampleIndex)
    {
        string screenshotFileName = $"{gesture}_sample_{sampleIndex}.png";
        string screenshotPath = Path.Combine(screenshotFileName);

        ScreenCapture.CaptureScreenshot(screenshotPath);

        Debug.Log($"Screenshot saved to: {screenshotPath}");

        UpdateDebugText($"Screenshot Taken: {screenshotFileName}");
    }

    void UpdateCountdownText(string message)
    {
        if (countdownText != null)
        {
            countdownText.text = message;
        }
    }

    void UpdateDebugText(string message)
    {
        if (debugText != null)
        {
            debugText.text = message;
        }
    }

    void GenerateJointHeaders()
    {
        int maxJointID = (int)XRHandJointID.LittleTip;

        for (int i = 0; i <= maxJointID; i++)
        {
            XRHandJointID jointID = (XRHandJointID)i;

            if (jointID == XRHandJointID.Invalid || i < 0 || i > maxJointID)
                continue;

            string jointName = jointID.ToString();
            jointHeaders.Add($"{jointName}_X");
            jointHeaders.Add($"{jointName}_Y");
            jointHeaders.Add($"{jointName}_Z");
        }
    }

    string GetGestureForClap(int index)
    {
        if (gestureList == null || gestureList.Count == 0)
            return "Unknown";

        int safeIndex = index % gestureList.Count;
        return gestureList[safeIndex];
    }

}
