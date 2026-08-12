/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * CameraDirector.cs
 * Switches between the game's Cinemachine camera angles.
 */

using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Names for every camera angle in the game.
/// </summary>
public enum CameraId
{
    BirdsEye,           // the opening overhead shot
    PlayerFirstPerson,  // walking around
    PedestrianPov,      // through her eyes  (locked during replay)
    DriverPov,          // through his eyes  (locked during replay)
    PassengerSeat,      // sitting beside the driver, talking to him
    Resolve,            // the cinematic "what if" replay at the end
    StartMenu           // slow orbit around the level, behind the main menu
}

/// <summary>
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
/// ALL RAYCASTS COME FROM Camera.main, ALWAYS.
/// </summary>
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

    /// <summary>
    /// Make one camera live and everything else stand by.
    ///
    /// allowLook decides whether the player may move this camera with the mouse.
    /// The SAME camera is used two different ways:
    ///   POV replay in Free Roam  -> allowLook FALSE. The view is welded to where she was
    ///                               actually looking, and you CANNOT look up at the car.
    ///                               That is the entire point of the shot.
    ///   Stepping into her eyes   -> allowLook TRUE, so you can look down at her phone.
    ///   during Intervene
    /// </summary>
    public void Activate(CameraId id, bool allowLook = true)
    {
        Current = id;

        for (int i = 0; i < cameras.Length; i++)
        {
            bool isLive = cameras[i].id == id;

            if (cameras[i].cam != null)
            {
                cameras[i].cam.Priority = isLive ? livePriority : standbyPriority;
            }

            // Only the live camera may read the mouse, or two of them would both bank up
            // rotation and you would get very confusing drift when you switched back.
            if (cameras[i].look != null)
            {
                cameras[i].look.enabled = isLive && allowLook;

                // Always re-centre, even when look is off, so a locked replay starts
                // pointing exactly where the Inspector says it should.
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

    /// <summary>
    /// Warns once at startup about anything you forgot to drag in, rather than letting you
    /// find out when a phase silently shows the wrong angle.
    /// </summary>
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
