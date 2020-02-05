using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;

public class gameController : MonoBehaviour
{

    public class TrajectoryPoint
	// holds target position in 3D (cartesian) for given time point
    {
        public float x, y, z, t;
        public TrajectoryPoint(float x_in, float y_in, float z_in, float t_in)
        {
            x = x_in;
            y = y_in;
            z = z_in;
            t = t_in;
        }
    }
    public class HeadTrajectoryPoint
	// holds head anglefor given time point

    {
        public float phi, t;
        public HeadTrajectoryPoint(float phi_in, float t_in)
        {
            phi = phi_in;
            t = t_in;
        }
    }

    private Telemetry telemetry;
    private TriggerController triggerController; // talks over serial port with BioSemi ampt

	// Arrays for holdign trajectories and targets
    public List<TrajectoryPoint> trajL = new List<TrajectoryPoint>();
    public List<TrajectoryPoint> teleL = new List<TrajectoryPoint>();
    public List<TrajectoryPoint> actualSoundPosL = new List<TrajectoryPoint>();
    public List<HeadTrajectoryPoint> headTrajL = new List<HeadTrajectoryPoint>();
    public List<float> targetTimesL = new List<float>();


    public LineRenderer lineRenderer;
    public AudioSource soundSource;
    public GameObject soundSourceContainer;
    public GameObject preTrialText;
    public GameObject postTrialText;
    public GameObject introText;
    public GameObject epilogueText;
    public GameObject scoreText;
    public GameObject xrrig;

    public Text fixationCross;

	// Experimental conditions
    public int trialNumber = -1;
    public int nTrials = 80;
    public int nHeadTrials = 1;
    public int trajPointCounter = 0;
    public float timeInTrial = 0;
    public float limitDistance = 0.5f;
    public bool onTrial = false;
    public List<float> subResponses;
    public List<bool> subResponsesLoc;
    public List<bool> subResponsesDet;
    public int detectionScore=0;
    public int locationScore=0;
    public float headTrackTolerance = 25; // in deg
    public float responseTolerance = 3; //in sec
    public string subjectPath;
    private string noHeadstr = "Listen to the sound and do not move your head.\n Keep your eyes on the fixation cross.\n \n Press button when ready to start new trial";
    private string yesHeadstr = "Listen to the sound and nove your head as indicated:\n '<' move left, '>' move right, '+' do not move \n\n Keep your eyes on the movement indicator. \n\n Press button when ready to start new trial";
    private string mousestr = "Listen to the sound.\n Don't move your head but nove your avatar using mouse as indicated. :\n '<' move left, '>' move right, '+' do not move \n\n Keep your eyes on the movement indicator. \n\n Press button when ready to start new trial";


    // Use this for initialization
    void Start()
    {
        telemetry = gameObject.GetComponent<Telemetry>();
        triggerController = gameObject.GetComponent<TriggerController>();
        xrrig = GameObject.FindWithTag("xrrig");
        string Todaysdate = DateTime.Now.ToString("dd-MMM-yyyy-hh-mm");
        if (!Directory.Exists("C:\\Users\\Adam\\Desktop\\OculusOutput\\" + Todaysdate))
        {
            Directory.CreateDirectory("C:\\Users\\Adam\\Desktop\\OculusOutput\\" + Todaysdate);
        }
        subjectPath = "C:\\Users\\Adam\\Desktop\\OculusOutput\\" + Todaysdate;
        Debug.Log(subjectPath);


        if (trialNumber == -1) // intro
        {
            introText.SetActive(true);
            preTrialText.SetActive(false);
        }
        else if (trialNumber < nHeadTrials)
        {
            preTrialText.SetActive(true);
            preTrialText.GetComponent<TextMesh>().text = noHeadstr+"\nTrial " + (trialNumber + 1).ToString() + "/" + nTrials.ToString();
        }
        else if (trialNumber >= nHeadTrials && trialNumber < 60)
        {
            preTrialText.SetActive(true);
            preTrialText.GetComponent<TextMesh>().text = yesHeadstr + "\nTrial " + (trialNumber + 1).ToString() + "/" + nTrials.ToString();
        }
        else
        {
            preTrialText.SetActive(true);
            preTrialText.GetComponent<TextMesh>().text = mousestr + "\nTrial " + (trialNumber + 1).ToString() + "/" + nTrials.ToString();
        }

    }




    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetKeyDown(KeyCode.Mouse0))  // subject response
        {
            if (onTrial == true)
            {
                Debug.Log("sub response");
                subResponses.Add(timeInTrial);
                TestResponse();
            }
            else
            {
                if (trialNumber == -1) // Show intro screen before the actual experiment
                {
                    trialNumber++;
                    introText.SetActive(false);
                    preTrialText.SetActive(true);
                    preTrialText.GetComponent<TextMesh>().text = noHeadstr + "\nTrial " + (trialNumber + 1).ToString() + "/" + nTrials.ToString();
                }
                else if (trialNumber < nTrials) // Run each trial
                {
                    RunTrial();
                }
                else // Show finish screen
                {
                    Debug.Log("finished");
                    Application.Quit();
                }
            }
        }
        if (onTrial == true) //When trial is on, move sound source and log trajectories
        {
            MoveSource();

            if (trialNumber > nHeadTrials){
                IndicateHeadMovement();
            }
            LogTele();
            LogSoundPos();
        }
    }

    public void TestController()
    {
        Debug.Log("Controller Works");
    }

 
    public List<TrajectoryPoint> LoadTrajectory(string trajPath)
	// Loads sound trajectories from CSV files
    {
        var trajL = new List<TrajectoryPoint>();
        StreamReader inp_stm = new StreamReader(trajPath);
        while (!inp_stm.EndOfStream)
        {
            string inp_ln = inp_stm.ReadLine();
            string[] fields = inp_ln.Split(',');
            float phi, x, y, z, t;
            float.TryParse(fields[0], out phi);
            float.TryParse(fields[1], out x);
            float.TryParse(fields[2], out y);
            float.TryParse(fields[3], out z);
            float.TryParse(fields[4], out t);
            TrajectoryPoint point = new TrajectoryPoint(x, z+1, y, t); // Adjust coordinate system to unity -> flipped Y and Z
            trajL.Add(point);
        }
        inp_stm.Close();
        if (trajL.Count == 1)
        {
            Debug.Log("Error in loading trajectory file");
        }
        return trajL;
    }

    public List<HeadTrajectoryPoint> LoadHeadTrajectory(string headPath)
	// Loads head trajectories from CSV files
    {
        var headTrajL = new List<HeadTrajectoryPoint>();
        StreamReader inp_stm = new StreamReader(headPath);
        while (!inp_stm.EndOfStream)
        {
            string inp_ln = inp_stm.ReadLine();
            string[] fields = inp_ln.Split(',');
            float phi, t;
            float.TryParse(fields[0], out phi);
            float.TryParse(fields[1], out t);

            HeadTrajectoryPoint point = new HeadTrajectoryPoint(phi, t); // 
            headTrajL.Add(point);
        }
        inp_stm.Close();
        if (headTrajL.Count == 1)
        {
            Debug.Log("Error in " +
                "loading head trajectory file");
        }
        Debug.Log("Loaded headtraj points: " + headTrajL.Count);
        return headTrajL;
    }


    public void RunTrial()
	// Run trial- send trigger, play sound, log data
    {
        trialNumber++;
        Debug.Log("RunTrial: " + trialNumber);
        preTrialText.SetActive(false);
        scoreText.SetActive(false);

        // save trigger
        triggerController.SendTrigger(126);

        string trajPath, headTrajPath, targettimesPath;
        AudioClip clip1;

        if (trialNumber <= 60)
        {
            // Source path
            trajPath = "G:\\My Drive\\MATLAB\\OculusPilot\\Stimuli\\ResourcesPilot2\\CSVtraj2\\traj_" + trialNumber + ".csv";
            headTrajPath = "G:\\My Drive\\MATLAB\\OculusPilot\\Stimuli\\ResourcesPilot2\\CSVheadtraj2\\headtraj_" + trialNumber + ".csv";
            //  string wavePath =  "G:\\My Drive\\MATLAB\\OculusPilot\\Stimuli\\AudioStim2\\audio_" + trialNumber + ".csv";
            targettimesPath = "G:\\My Drive\\MATLAB\\OculusPilot\\Stimuli\\ResourcesPilot2\\CSVtarget2\\target_" + trialNumber + ".csv";
            // LoadWav(wavPath);
            clip1 = Resources.Load<AudioClip>("AudioPinkNoise2/audio_" + trialNumber);
        }
        else
        {
            // Source path
            trajPath = "G:\\My Drive\\MATLAB\\OculusPilot\\Stimuli\\ResourcesPilot2\\CSVtraj2\\traj_" + (trialNumber-20) + ".csv";
            headTrajPath = "G:\\My Drive\\MATLAB\\OculusPilot\\Stimuli\\ResourcesPilot2\\CSVheadtraj2\\headtraj_" + (trialNumber - 20) + ".csv";
            //  string wavePath =  "G:\\My Drive\\MATLAB\\OculusPilot\\Stimuli\\AudioStim2\\audio_" + trialNumber + ".csv";
            targettimesPath = "G:\\My Drive\\MATLAB\\OculusPilot\\Stimuli\\ResourcesPilot2\\CSVtarget2\\target_" + (trialNumber - 20) + ".csv";
            // LoadWav(wavPath);
            clip1 = Resources.Load<AudioClip>("AudioPinkNoise2/audio_" + (trialNumber - 20));
        }
        Debug.Log("Audio Loaded: " + clip1);
        soundSource.clip = clip1;
        trajL = LoadTrajectory(trajPath);
        headTrajL = LoadHeadTrajectory(headTrajPath);

        // Load Target times
        targetTimesL = LoadTargetTimes(targettimesPath);

        // clear run-time variables
        subResponses.Clear();
        subResponsesLoc.Clear();
        subResponsesDet.Clear();
        actualSoundPosL.Clear();
        teleL.Clear();
        detectionScore = 0;
        locationScore = 0;
        trajPointCounter = 0;

        // if trial number>60 enable mouse look 
        if (trialNumber > 60)
        {
            xrrig.GetComponent<MouseLook>().sensitivityX = 3;
        }           
            // Send trigger
            triggerController.SendTrigger(trialNumber);
       // soundSource.PlayDelayed(0);
        soundSource.Play();
        timeInTrial = 0;
        onTrial = true;
    }

    public void MoveSource()
	// Generates sound trajectories from checkpoint (loaded from csv)
    {
        if (soundSource.isPlaying)
        {
            Vector3 targetPos = new Vector3(trajL[trajPointCounter].x, trajL[trajPointCounter].y, trajL[trajPointCounter].z);
            float targetDist =  Vector3.Distance(soundSourceContainer.transform.position, targetPos);
            float timeLeft = trajL[trajPointCounter].t - timeInTrial;
            float travelDist = targetDist / (timeLeft / Time.deltaTime); // (remaining time/last frame duration) is approximately how many "steps" are left

            soundSourceContainer.transform.position = Vector3.MoveTowards(soundSourceContainer.transform.position, targetPos, Math.Abs(travelDist));
            if (targetDist < limitDistance || timeLeft<0)// if we are close enough or if we have no time left to reach checkpoint
            {
                if (trajPointCounter < trajL.Count - 1) //switch to the nex waypoint if exists
                {
                    trajPointCounter++;
                }
            }
            timeInTrial = timeInTrial + Time.deltaTime;


        }
        else
        {
            TerminateTrial();
        }
    }

    void TerminateTrial()
	// Finish trial, save telemetry log, show score
    {
        onTrial = false;
        WriteTele();
        WriteSoundPos();
        // save trigger
        triggerController.SendTrigger(127);
        Debug.Log("Last checkpoint");
        CalcScore();
        ShowScore();
        WriteScore();
    }
    void LogTele()
	// Log head trajectory
    {
        Vector3 cameraAngle = Camera.main.gameObject.transform.rotation.eulerAngles;
        float x = cameraAngle.x;
        float y = cameraAngle.y - 90;
        if (y > 180)
        {
            y = -180f + (y - 180f);
        }
        float z = cameraAngle.z;
        gameController.TrajectoryPoint telePoint = new gameController.TrajectoryPoint(x, y, z, timeInTrial); // Adjust coordinate system to unity -> flipped Y and Z
        teleL.Add(telePoint);
    }

    void WriteTele()
	// Exports head movement 
    {
        string telemetryPath = subjectPath + "\\tele_" + trialNumber + ".txt";
        StreamWriter streamWriter;
        streamWriter = System.IO.File.AppendText(telemetryPath);
        for (int i = 0; i < teleL.Count; i++)
        {
            streamWriter.WriteLine("{0},{1},{2},{3}", teleL[i].t, teleL[i].x, teleL[i].y, teleL[i].z);
        }
        streamWriter.Close();
    }

    void IndicateHeadMovement()
	// Show arrow on the screen that indicated head motion
    {
        Vector3 cameraAngle = Camera.main.gameObject.transform.rotation.eulerAngles;
        float currentHeadAngle = cameraAngle.y - 90;
        if (currentHeadAngle > 180)
        {
            currentHeadAngle = -180f + (currentHeadAngle - 180f);
        }

        float head_error = currentHeadAngle - headTrajL[trajPointCounter].phi;
        if (Math.Abs(head_error) > headTrackTolerance)
        {
            if (currentHeadAngle < headTrajL[trajPointCounter].phi)
            {
                fixationCross.text = ">"; // head too on the left
            }
            else
            {
                fixationCross.text = "<"; // head too on the right
            }
        }
        else
        {
            fixationCross.text = "+";
        }
    }

    public List<float> LoadTargetTimes(string targettimesPath)
	// Load target times
    {
        var targetTimes = new List<float>();
        StreamReader inp_stm = new StreamReader(targettimesPath);
        while (!inp_stm.EndOfStream)
        {
            string inp_ln = inp_stm.ReadLine();
            string[] fields = inp_ln.Split(',');
            float t;
            float.TryParse(fields[0], out t);
            targetTimes.Add(t);
        }
        inp_stm.Close();
        return targetTimes;

    }

    void ShowScore()
	// Show score on the screen
    {
        scoreText.GetComponent<TextMesh>().text = "SCORE: " + detectionScore.ToString() + "/" + targetTimesL.Count.ToString();
        scoreText.SetActive(true);

        if (trialNumber < nTrials) {


            if (trialNumber < nHeadTrials)
            {
                preTrialText.SetActive(true);
                preTrialText.GetComponent<TextMesh>().text = noHeadstr + "\nTrial " + (trialNumber + 1).ToString() + "/" + nTrials.ToString();
            }
            else if (trialNumber >= nHeadTrials && trialNumber<=60)
            {
                preTrialText.SetActive(true);
                preTrialText.GetComponent<TextMesh>().text = yesHeadstr + "\nTrial " + (trialNumber + 1).ToString() + "/" + nTrials.ToString();
            }
            else
            {
                preTrialText.SetActive(true);
                preTrialText.GetComponent<TextMesh>().text = mousestr + "\nTrial " + (trialNumber + 1).ToString() + "/" + nTrials.ToString();
            }
        }
        else
        {
            /*
            scoreText.GetComponent<TextMesh>().text = "SCORE: \nDetection: " + detectionScore.ToString() +
            "/" + targetTimesL.Count.ToString() + "  Localization: " + locationScore.ToString() + "/" + targetTimesL.Count.ToString() +
            "\n\n\n Your are finished.  Press <return>";
            */
            scoreText.GetComponent<TextMesh>().text = "SCORE: " + detectionScore.ToString() +
            "/" + targetTimesL.Count.ToString() +
            "\n\n\n Your are finished.  Press button to exit";
            soundSource.enabled = false;
            onTrial = false;


        }
    }

    void CalcScore()
	// Calculate hitrate
    {
        detectionScore = 0;
        locationScore = 0;
        for (int t = 0; t < subResponsesDet.Count; t++)
        {
            if (subResponsesDet[t] == true)
            {
                detectionScore++;
                if (subResponsesLoc[t] == true)
                {
                    locationScore++;
                }
            }
        }
    }

    void TestResponse()
	// Check if subjects detexting targets
    {
        bool targetDetected = false;
        for (int t=0; t < targetTimesL.Count; t++)
        {
            if ((timeInTrial- targetTimesL[t]) > 0 && (timeInTrial- targetTimesL[t]) < responseTolerance)
            {
                targetDetected = true;
                break;
            }
        }

        // Check for response time
        if (targetDetected == true) 
        {
            subResponsesDet.Add(true);
        }
        else
        {
            subResponsesDet.Add(false);
        }

        if (((soundSourceContainer.transform.position.z > 0) && (Input.GetKeyDown(KeyCode.LeftArrow) == true)) || ((soundSourceContainer.transform.position.z < 0) && (Input.GetKeyDown(KeyCode.RightArrow) == true))) // WITH RESPECT TO ENVIRONEMT
        { // Left Z is positive; left button is positive
            subResponsesLoc.Add(true);
        }
        else
        {
            subResponsesLoc.Add(false);
        }
    }
    

    void WriteScore()
    {
        string scorePath = subjectPath + "\\score_" + trialNumber + ".txt";

        StreamWriter streamWriter;
        streamWriter = System.IO.File.AppendText(scorePath);
        for (int i = 0; i < subResponsesDet.Count; i++)
        {
            streamWriter.WriteLine("{0},{1},{2}", subResponsesDet[i], subResponsesLoc[i], subResponses[i]);
        }
        streamWriter.Close();
    }

    public void TestTriggerAndAudio()
    {
        Debug.Log("TriggerAndAudio");
       // soundSource.Play();

        soundSource.PlayDelayed(0);
        triggerController.SendTrigger(255);
    }

    public bool IsDirectoryEmpty(string path)
    {
        IEnumerable<string> items = Directory.EnumerateFileSystemEntries(path);
        using (IEnumerator<string> en = items.GetEnumerator())
        {
            return !en.MoveNext();
        }
    }
    void LogSoundPos()
    {       
        gameController.TrajectoryPoint soundPoint = new gameController.TrajectoryPoint(soundSourceContainer.transform.position.x, soundSourceContainer.transform.position.y, soundSourceContainer.transform.position.z, timeInTrial); // Adjust coordinate system to unity -> flipped Y and Z
        actualSoundPosL.Add(soundPoint);
    }

    void WriteSoundPos()
    {
        string actualSoundPath = subjectPath + "\\actSoundPOs_" + trialNumber + ".txt";

        StreamWriter streamWriter;
        streamWriter = System.IO.File.AppendText(actualSoundPath);
        for (int i = 0; i < actualSoundPosL.Count; i++)
        {
            streamWriter.WriteLine("{0},{1},{2},{3}", actualSoundPosL[i].t, actualSoundPosL[i].x, actualSoundPosL[i].y, actualSoundPosL[i].z);
        }
        streamWriter.Close();
    }
}




