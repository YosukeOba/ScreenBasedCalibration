using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;

public class Calibration : MonoBehaviour
{

    [SerializeField]

    public IEyeTracker _eyeTracker;

    protected GazeDataEventArgs gazeData;

    public static GameObject image;

    private static bool calibration_check = false;


    // Start is called before the first frame update
    void Awake()
    {
        image = GameObject.Find("Image");

        var trackers = EyeTrackingOperations.FindAllEyeTrackers();

        foreach (IEyeTracker eyeTracker in trackers)
        {
            if (eyeTracker.Model == "Tobii TX300") _eyeTracker = eyeTracker;
        }
        if (_eyeTracker != null)
        {
            _eyeTracker.GazeDataReceived += GetEyeTrackingData;
        }



    }

    // Update is called once per frame
    void Update()
    {
        if (_eyeTracker != null && calibration_check == false)
        {
            Calibrate(_eyeTracker);
        }
        else if (_eyeTracker != null && calibration_check == true)
        {

        }
    }

    private static void Calibrate(IEyeTracker eyeTracker)
    {
        // Create a calibration object.
        var calibration = new ScreenBasedCalibration(eyeTracker);
        // Enter calibration mode.
        calibration.EnterCalibrationMode();
        // Define the points on screen we should calibrate at.
        // The coordinates are normalized, i.e. (0.0f, 0.0f) is the upper left corner and (1.0f, 1.0f) is the lower right corner.
        var pointsToCalibrate = new NormalizedPoint2D[] {
                new NormalizedPoint2D(0.5f, 0.5f),
                new NormalizedPoint2D(0.1f, 0.1f),
                new NormalizedPoint2D(0.1f, 0.9f),
                new NormalizedPoint2D(0.9f, 0.1f),
                new NormalizedPoint2D(0.9f, 0.9f),
            };
        // Collect data.
        foreach (var point in pointsToCalibrate)
        {
            // Show an image on screen where you want to calibrate.
            Debug.Log(string.Format("Show point on screen at ({0}, {1})", point.X, point.Y));

            // Wait a little for user to focus.
            System.Threading.Thread.Sleep(700);

            draw_calibration_point(point);

            // yield return new WaitForSeconds(.7f);
            // Collect data.
            CalibrationStatus status = calibration.CollectData(point);
            if (status != CalibrationStatus.Success)
            {
                // Try again if it didn't go well the first time.
                // Not all eye tracker models will fail at this point, but instead fail on ComputeAndApply.
                calibration.CollectData(point);
                calibration_check = true;
            }
        }

        // Compute and apply the calibration.
        CalibrationResult calibrationResult = calibration.ComputeAndApply();
        Debug.Log(string.Format("Compute and apply returned {0} and collected at {1} points.",
            calibrationResult.Status, calibrationResult.CalibrationPoints.Count));
        // Analyze the data and maybe remove points that weren't good.
        calibration.DiscardData(new NormalizedPoint2D(0.1f, 0.1f));
        // Redo collection at the discarded point.
        Debug.Log(string.Format("Show point on screen at ({0}, {1})", 0.1f, 0.1f));
        calibration.CollectData(new NormalizedPoint2D(0.1f, 0.1f));
        // Compute and apply again.
        calibrationResult = calibration.ComputeAndApply();
        Debug.Log(string.Format("Second compute and apply returned {0} and collected at {1} points.",
            calibrationResult.Status, calibrationResult.CalibrationPoints.Count));
        // See that you're happy with the result.
        // The calibration is done. Leave calibration mode.
        calibration.LeaveCalibrationMode();
    }

    public static void draw_calibration_point(NormalizedPoint2D point)
    {
        image.transform.position = new Vector3(-1920 / 2 + 1920 * point.X, 1080 / 2 - 1080 * point.Y, 0);
        //System.Threading.Thread.Sleep(2500);
    }

    public void GetEyeTrackingData(object sender, GazeDataEventArgs e)
    {
        if (e != null)
        {
            gazeData = e;
        }
    }

    protected virtual void OnDisable()
    {
        if (_eyeTracker != null)
        {
            _eyeTracker.GazeDataReceived -= GetEyeTrackingData;
        }
    }

    protected virtual void OnDestroy()
    {
        EyeTrackingOperations.Terminate();
    }
}
