using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.XR;

public class keyboardController : MonoBehaviour {

    private gameController gameController;
    private Telemetry telemetry;
    private TriggerController triggerController;


    // Use this for initialization
    void Start() {
        gameController = gameObject.GetComponent<gameController>();
        telemetry = gameObject.GetComponent<Telemetry>();
        triggerController = gameObject.GetComponent<TriggerController>();


    }

    // Update is called once per frame
    void Update() { 

        if (Input.GetKeyDown(KeyCode.F2))
        {
            triggerController.TestTrigger();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            gameController.TestTriggerAndAudio();
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            InputTracking.Recenter();
        }

    }
}
