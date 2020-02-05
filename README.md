# OculusHead
Oculus-Moving sound Headtracking

Unity Coded scenario where subjects listened to virtualized moving sound in the horizontal plane while they were allowed to move their heads and received sensory input over a virtual reality headset. We were primarily interested in whether sound is cortically encoded using cranio- or allo-centric coordinates.

For this task we decided to employ the virtual reality (VR) headset OculusVR CV1 for stimulus presentation. This was used as it has a number of advantages over headphone or free-field (loudspeaker array) presentation: Firstly, the Oculus VR facilitates head position tracking, as it has low latency and high precision rotational and head tracking system so no external head-tracking system was needed. Second benefit is that the system automatically adjusts (compensates) the audio for head motion i.e. the perceived sound source location is unchanged with respect to the allocentric coordinates despite the user’s head motion. 

Inputs:
(1) Predefined sound and head trajectories in .csv
(2) Un-spatialized audio files

Ouputs:
(1) Actual sound and head trajectories 
(2) Syncs with EEG amplifier over serial port - triggers

![info(/info.png)
