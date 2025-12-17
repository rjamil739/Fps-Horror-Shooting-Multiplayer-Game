using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiFPS
{
    public class UIFpsCounter : MonoBehaviour
    {

        //// Variables for tracking FPS
        [SerializeField] Text _fpsText; // UI Text component to display FPS (optional)
                                        //public float updateInterval = 0.5f; // Time interval for updating the FPS display

        //private float timeSinceLastUpdate = 0.0f;
        //private int frameCount = 0;
        //private float fps = 0.0f;

        //void Update()
        //{
        //    float smoothing = Mathf.Pow(0.9f, Time.unscaledDeltaTime * 60 / 1000);
        //}

        // Number of past frames to use for FPS smooth calculation - because 
        // Unity's smoothedDeltaTime, well - it kinda sucks
        private int frameTimesSize = 60;
        // A Queue is the perfect data structure for the smoothed FPS task;
        // new values in, old values out
        private Queue<float> frameTimes = new Queue<float>();
        // Not really needed, but used for faster updating then processing 
        // the entire queue every frame
        private float __frameTimesSum = 0;
        // Flag to ignore the next frame when performing a heavy one-time operation 
        // (like changing resolution)
        private bool _fpsIgnoreNextFrame = false;

        //=============================================================================
        // Call this after doing a heavy operation that will screw up with FPS calculation

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void FPSIgnoreNextFrame()
        {
            this._fpsIgnoreNextFrame = true;
        }

        //=============================================================================
        // Smoothed FPS counter updating
        void Update()
        {
            if (this._fpsIgnoreNextFrame)
            {
                this._fpsIgnoreNextFrame = false;
                return;
            }

            // While looping here allows the frameTimesSize member to be changed dinamically
            while (this.frameTimes.Count >= this.frameTimesSize)
            {
                this.__frameTimesSum -= this.frameTimes.Dequeue();
            }
            while (this.frameTimes.Count < this.frameTimesSize)
            {
                this.__frameTimesSum += Time.deltaTime;
                this.frameTimes.Enqueue(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
#if !UNITY_SERVER
            _fpsText.text = $"FPS: {GetSmoothedFPS()}";
            if (NetworkClient.isConnected)
                _fpsText.text += $"\nLatency: {Math.Round(NetworkTime.rtt * 1000)}ms";
#endif
        }

        //=============================================================================
        // Public function to get smoothed FPS values
        public int GetSmoothedFPS()
        {
            return (int)(this.frameTimesSize / this.__frameTimesSum * Time.timeScale);
        }
    }
}