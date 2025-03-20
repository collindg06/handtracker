using UnityEngine; 
using TMPro;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.IO;
using System.Collections;
using System.Text;
using System.Collections.Generic;

public class HandTrackingJoints : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI countdownText;

    private XRHandSubsystem handSubsystem;
    private string downloadsFolder;
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
        "left", "up", "right", "down", "backwards", "forward", "turn left", "turn right"
    };

    private List<string> jointHeaders = new List<string>();

    void Start()
    {
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null)
        {
            handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
        }

        downloadsFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");

        if (!Directory.Exists(downloadsFolder))
        {
            Directory.CreateDirectory(downloadsFolder);
        }

        filePath = Path.Combine(downloadsFolder, "hand_tracking_data.csv");

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
            clapIndex++;
            string gesture = GetGestureForClap(clapIndex);

            // No longer taking one screenshot here!
            // Screenshot now happens in CollectSamples instead.

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
            StringBuilder rowBuilder = new StringBuilder();
            rowBuilder.Append($"{clapIndex},{timeStamp},{gesture},{sample + 1},");

            List<string> jointValues = new List<string>();

            for (int i = 0; i <= maxJointID; i++)
            {
                XRHandJointID jointID = (XRHandJointID)i;

                if (jointID == XRHandJointID.Invalid || i < 0 || i > maxJointID)
                    continue;

                XRHandJoint joint = hand.GetJoint(jointID);

                if (joint != null && joint.TryGetPose(out Pose pose))
                {
                    jointValues.Add($"{pose.position.x:F4}");
                    jointValues.Add($"{pose.position.y:F4}");
                    jointValues.Add($"{pose.position.z:F4}");
                }
                else
                {
                    jointValues.Add("NaN");
                    jointValues.Add("NaN");
                    jointValues.Add("NaN");
                }
            }

            rowBuilder.Append(string.Join(",", jointValues));
            rowBuilder.AppendLine();

            File.AppendAllText(filePath, rowBuilder.ToString());

            // NEW: Take a screenshot for each sample!
            TakeScreenshot(clapIndex, sample + 1);

            UpdateDebugText($"Collecting Data...\nSamples Collected: {sample + 1}/{numberOfSamples}");

            yield return new WaitForSeconds(sampleInterval);
        }

        Debug.Log($"Saved {numberOfSamples} samples for clap {clapIndex}");

        UpdateDebugText($"Data Collection Complete for Clap {clapIndex}!");
    }

    // UPDATED: Screenshot method now includes sample index
    void TakeScreenshot(int clapIndex, int sampleIndex)
    {
        string screenshotFileName = $"clap_{clapIndex}_sample_{sampleIndex}.png";
        string screenshotPath = Path.Combine(downloadsFolder, screenshotFileName);

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
        if (gestureList.Count == 0)
            return "Unknown";

        int gestureIndex = (index - 1) % gestureList.Count;
        return gestureList[gestureIndex];
    }
}
