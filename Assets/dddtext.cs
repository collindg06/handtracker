using UnityEngine;
using TMPro;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.IO;

public class HandTrackingJoints : MonoBehaviour
{
    public TextMeshProUGUI debugText; // Assign in Unity UI
    private XRHandSubsystem handSubsystem;
    private string downloadsFolder;

    void Start()
    {
        // Get the Hand Tracking Subsystem
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null)
        {
            handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
        }

        // Get the system Downloads folder path (platform-independent)
        downloadsFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");

        // Ensure the Downloads folder exists
        if (!Directory.Exists(downloadsFolder))
        {
            Directory.CreateDirectory(downloadsFolder);
        }

        // Set up the path to save CSV file in Downloads
        string filePath = Path.Combine(downloadsFolder, "hand_tracking_data.csv");

        // Optionally, if you want to create a new CSV with headers
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Time,Hand,Joint,Position_X,Position_Y,Position_Z\n");
        }
    }

    void Update()
    {
        if (handSubsystem != null)
        {
            XRHand leftHand = handSubsystem.leftHand;
            XRHand rightHand = handSubsystem.rightHand;

            string message = "Hand Tracking Active\n";

            if (leftHand.isTracked)
            {
                message += "Left Hand:\n";
                message += GetFingerData(leftHand, "Left");
            }
            else
            {
                message += "Left Hand Not Tracked\n";
            }

            if (rightHand.isTracked)
            {
                message += "Right Hand:\n";
                message += GetFingerData(rightHand, "Right");
            }
            else
            {
                message += "Right Hand Not Tracked\n";
            }

            debugText.text = message;

            // If screenshot is taken, save hand data to CSV
            if (Input.GetKeyDown(KeyCode.S))  // Press "S" to simulate taking a screenshot
            {
                SaveHandDataToCSV(leftHand, rightHand);
                TakeScreenshot();
            }
        }
        else
        {
            debugText.text = "Hand Tracking Subsystem Not Found!";
        }
    }

    string GetFingerData(XRHand hand, string handName)
    {
        string data = "";

        // Ensure hand is tracked before accessing joints
        if (!hand.isTracked)
            return $"{handName} Hand not tracked.\n";

        // Get valid joint range
        int maxJointID = (int)XRHandJointID.LittleTip; // Highest valid joint ID

        for (int i = 0; i <= maxJointID; i++)
        {
            XRHandJointID jointID = (XRHandJointID)i;

            // Ensure jointID is within the correct range
            if (jointID == XRHandJointID.Invalid || i < 0 || i > maxJointID)
                continue;

            XRHandJoint joint = hand.GetJoint(jointID);

            if (joint != null && joint.TryGetPose(out Pose pose))
            {
                data += $"{jointID}: {pose.position}\n";
            }
        }
        return data;
    }

    void SaveHandDataToCSV(XRHand leftHand, XRHand rightHand)
    {
        // Get the current time for this tracking data entry
        string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Save left hand data
        if (leftHand.isTracked)
        {
            SaveHandDataToFile(leftHand, "Left", timeStamp);
        }

        // Save right hand data
        if (rightHand.isTracked)
        {
            SaveHandDataToFile(rightHand, "Right", timeStamp);
        }
    }

    void SaveHandDataToFile(XRHand hand, string handName, string timeStamp)
    {
        int maxJointID = (int)XRHandJointID.LittleTip;

        for (int i = 0; i <= maxJointID; i++)
        {
            XRHandJointID jointID = (XRHandJointID)i;

            if (jointID == XRHandJointID.Invalid || i < 0 || i > maxJointID)
                continue;

            XRHandJoint joint = hand.GetJoint(jointID);

            if (joint != null && joint.TryGetPose(out Pose pose))
            {
                string line = $"{timeStamp},{handName},{jointID},{pose.position.x},{pose.position.y},{pose.position.z}\n";
                string filePath = Path.Combine(downloadsFolder, "hand_tracking_data.csv");
                File.AppendAllText(filePath, line);
            }
        }
    }

    void TakeScreenshot()
    {
        // Define the screenshot filename in the Downloads folder
        string screenshotPath = Path.Combine(downloadsFolder, "screenshot.png");

        // Take screenshot
        ScreenCapture.CaptureScreenshot(screenshotPath);
        Debug.Log("Screenshot saved to: " + screenshotPath);
    }
}
