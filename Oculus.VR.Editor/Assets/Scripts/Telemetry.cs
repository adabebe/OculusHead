using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Telemetry : MonoBehaviour {
	// Samples head position over time using invoke-repeat
	
    public StreamWriter streamWriter;
    public float samplingRate = 32f; // sample rate in Hz
    public float trialStartTime;
    public List<gameController.TrajectoryPoint> teleL = new List<gameController.TrajectoryPoint>();

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void TelemetryEnable(string outputFilePath)
    {
        trialStartTime = Time.time;
        streamWriter = System.IO.File.AppendText(outputFilePath);
        InvokeRepeating("TelemetrySampleNow", 0, 1 / samplingRate);
    }

    public void TelemetryDisable()
    {
        streamWriter.Close();
        CancelInvoke();
    }

    public void TelemetrySampleNow()
    {
        streamWriter.WriteLine("t {0}",
        Time.time - trialStartTime);
        /*
        streamWriter.WriteLine("t {0} x {1} y {2} z {3}",
           Time.time- trialStartTime, OcuCamera.position.x, OcuCamera.position.z, OcuCamera.forward.x);
           */
    }

    public void BuffTelemetryEnable()
    {
        trialStartTime = Time.time;
        InvokeRepeating("BuffTelemetrySampleNow", 0, 1 / samplingRate);
    }

    public List<gameController.TrajectoryPoint> BuffTelemetryDisable()
    {
        CancelInvoke();
        return teleL;
    }

    public void BuffTelemetrySampleNow()
    {
        float t = Time.time - trialStartTime;
        Vector3 cameraAngle = Camera.main.gameObject.transform.rotation.eulerAngles;
        float x = cameraAngle.x;
        float y = cameraAngle.y;
        float z = cameraAngle.z;
        gameController.TrajectoryPoint point = new gameController.TrajectoryPoint(x, y, z, t); // Adjust coordinate system to unity -> flipped Y and Z
        teleL.Add(point);

    }



}
