using UnityEngine;
using TMPro;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.IO;
using System.Collections;
using System.Text;

public class HandTrackingJoints : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI debugText;              // For hand tracking data/status
    public TextMeshProUGUI countdownText;          // For countdown and progress display

    private XRHandSubsystem handSubsystem;
    private string downloadsFolder;
    private string filePath;

    [Header("Clap Detection Settings")]
    public float clapDistanceThreshold = 0.1f;     // Distance to consider a clap (meters)
    public float clapCooldown = 2.0f;              // Cooldown time between claps (seconds)

    private bool isWaitingForDataCollection = false;
    private float lastClapTime = -Mathf.Infinity;

    [Header("Sampling Settings")]
    public int numberOfSamples = 100;              // How many samples to collect after clap
    public float sampleInterval = 0.2f;            // Interval between samples (now 0.2s)

    private int clapIndex = 0;

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

        // DELETE existing CSV file if it exists
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("Existing CSV file deleted.");
        }

        // Create a new CSV file with headers
        File.WriteAllText(filePath, "ClapIndex,TimeStamp,Data\n");

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

        // Whole second countdown before starting data collection
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

            TakeScreenshot(clapIndex);

            yield return StartCoroutine(CollectSamples(leftHand));
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

    IEnumerator CollectSamples(XRHand hand)
    {
        StringBuilder rowBuilder = new StringBuilder();

        string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        rowBuilder.Append($"{clapIndex},{timeStamp},");

        int maxJointID = (int)XRHandJointID.LittleTip;

        float totalCollectionTime = numberOfSamples * sampleInterval;

        for (int sample = 0; sample < numberOfSamples; sample++)
        {
            if (sample > 0)
            {
                rowBuilder.Append("|"); // Separate sample blocks
            }

            for (int i = 0; i <= maxJointID; i++)
            {
                XRHandJointID jointID = (XRHandJointID)i;

                if (jointID == XRHandJointID.Invalid || i < 0 || i > maxJointID)
                    continue;

                XRHandJoint joint = hand.GetJoint(jointID);

                if (joint != null && joint.TryGetPose(out Pose pose))
                {
                    rowBuilder.Append($"{jointID}-{pose.position.x:F4},{pose.position.y:F4},{pose.position.z:F4};");
                }
                else
                {
                    rowBuilder.Append($"{jointID}-NaN,NaN,NaN;");
                }
            }

            UpdateDebugText($"Collecting Data...\nSamples Collected: {sample + 1}/{numberOfSamples}");

            yield return new WaitForSeconds(sampleInterval);
        }

        rowBuilder.AppendLine(); // End of the entire row (after 100 samples)

        File.AppendAllText(filePath, rowBuilder.ToString());

        Debug.Log($"Saved {numberOfSamples} samples for clap {clapIndex}");

        UpdateDebugText($"Data Collection Complete for Clap {clapIndex}!");
    }

    void TakeScreenshot(int index)
    {
        string screenshotFileName = $"screenshot_{index}.png";
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
}
