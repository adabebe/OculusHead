using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class TriggerController : MonoBehaviour {
	// Sends sync triggers over serial port to BioSemi

    SerialPort port = new SerialPort(
      "COM3", 115200, Parity.None, 8, StopBits.One);

    // Use this for initialization
    void Start () {
        port.Open();
    }

    void OnDestroy()
    {
        port.Close();
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void TestTrigger()
    {
        Debug.Log("Sending Trigger2");
        byte[] data = { 255 };
        port.Write(data, 0, 1);
    }

    public void SendTrigger(int trigNum)
    {
        Debug.Log("Sending Trigger2");
        byte[] data = {(byte)trigNum};
        port.Write(data, 0, 1);
    }

    public void OpenPort()
    {
        port.Open();
    }

    public void ClosePort()
    {
        port.Close();
    }

}
