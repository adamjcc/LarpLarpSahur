using System;
using Unity.Cinemachine;
using UnityEngine;

/// Names for every camera angle in the game.
public enum CameraId
{
    BirdsEye,           // the opening overhead shot
    PlayerFirstPerson,  // walking around
    PedestrianPov,      // through her eyes  (locked during replay)
    DriverPov,          // through his eyes  (locked during replay)
    PassengerSeat,      // sitting beside the driver, talking to him
    Resolve             // the cinematic "what if" replay at the end
}

/// Switches between camera angles.
///
/// HOW CINEMACHINE WORKS, because this trips up everyone:
/// There is only ONE real Unity Camera in the whole game — the Main Camera, which carries a
/// CinemachineBrain. Everything named CAM_* is a CinemachineCamera, which is NOT a camera.
/// It is a set of instructions telling the real camera where to stand.
///
/// Whichever active CinemachineCamera has the highest Priority wins, and the Brain moves the
/// real camera to match it. So "switching cameras" just means changing which one is highest.
///
/// >>> ALL RAYCASTS COME FROM Camera.main, ALWAYS. <<<
public class CameraDirector : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        public CameraId id;
        public CinemachineCamera cam;

        [Tooltip("Optional. Mouse-look script on this camera, enabled only while it is live.")]
        public PovLook look;
    }

    [SerializeField] private Entry[] cameras;

    [SerializeField] private int livePriority = 20;
    [SerializeField] private int standbyPriority = 0;

    public CameraId Current { get; private set; }

    /// Make one camera live and everything else stand by.
    public void Activate(CameraId id)
    {
        Current = id;

        for (int i = 0; i < cameras.Length; i++)
        {
            bool isLive = cameras[i].id == id;

            if (cameras[i].cam != null)
            {
                cameras[i].cam.Priority = isLive ? livePriority : standbyPriority;
            }

            // Only the live camera is allowed to read the mouse, or two of them would
            // both accumulate rotation and you'd get very confusing drift.
            if (cameras[i].look != null)
            {
                cameras[i].look.enabled = isLive;
                if (isLive) cameras[i].look.ResetLook();
            }
        }
    }

    public CinemachineCamera Get(CameraId id)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].id == id) return cameras[i].cam;
        }
        return null;
    }

    /// Warns once at startup about anything you forgot to drag in, rather than letting you
    /// find out when a phase silently shows the wrong angle.
    private void Start()
    {
        foreach (CameraId id in Enum.GetValues(typeof(CameraId)))
        {
            if (Get(id) == null)
            {
                Debug.LogWarning($"[CameraDirector] No camera assigned for '{id}'. " +
                                 "Any phase using it will keep the previous angle.", this);
            }
        }
    }
}
