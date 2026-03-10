using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

namespace NamPhuThuy.Common
{
    public static class VideoHelper
    {
        /// <summary>
        /// Loads a video from StreamingAssets/Videos folder and prepares it for playback
        /// </summary>
        /// <param name="videoPlayer">The VideoPlayer component to load the video into</param>
        /// <param name="videoName">Name of the video file without extension</param>
        /// <param name="onSuccess">Callback when video is successfully prepared</param>
        /// <param name="onError">Callback when video loading fails</param>
        /// <param name="context">MonoBehaviour context for coroutine</param>
        public static Coroutine LoadVideoFromStreamingAssets(
            VideoPlayer videoPlayer,
            string videoName,
            Action onSuccess = null,
            Action<string> onError = null,
            MonoBehaviour context = null)
        {
            if (context == null)
            {
                Debug.LogError("VideoHelper: Context MonoBehaviour is required to start coroutine");
                return null;
            }

            return context.StartCoroutine(IE_LoadVideo(videoPlayer, videoName, onSuccess, onError));
        }

        private static IEnumerator IE_LoadVideo(
            VideoPlayer videoPlayer,
            string videoName,
            Action onSuccess,
            Action<string> onError)
        {
            if (videoPlayer == null)
            {
                onError?.Invoke("VideoPlayer is null");
                yield break;
            }

            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, $"{videoName}.mp4");;
            
            // Optional: Check file existence only on platforms where it works
            #if !UNITY_ANDROID && !UNITY_WEBGL
            if (!System.IO.File.Exists(filePath))
            {
                string error = $"Video file not found: {filePath}";
                DebugLogger.LogError(error);
                onError?.Invoke(error);
                yield break;
            }
            #endif

            yield return null;

            // Set up error handling
            bool errorOccurred = false;
            string errorMessage = string.Empty;

            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.prepareCompleted += OnPrepareCompleted;

            // Set video URL with proper format
            #if UNITY_ANDROID
            videoPlayer.url = filePath;
            #else
            videoPlayer.url = $"file://{filePath}";
            #endif

            
            /*
            using (UnityWebRequest req = UnityWebRequest.Get(filePath))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    
                }
                else
                {
                    videoPlayer.url = $"file://{filePath}";
                }
            }*/
            
            videoPlayer.Prepare();

            // Wait for prepare to complete or error
            while (!videoPlayer.isPrepared && !errorOccurred)
            {
                yield return null;
            }

            if (errorOccurred)
            {
                DebugLogger.LogError($"Video loading failed: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
            
            
            void OnVideoError(VideoPlayer vp, string message)
            {
                errorOccurred = true;
                errorMessage = message;
                vp.errorReceived -= OnVideoError;
            }

            void OnPrepareCompleted(VideoPlayer vp)
            {
                vp.prepareCompleted -= OnPrepareCompleted;
                if (!errorOccurred)
                {
                    onSuccess?.Invoke();
                }
            }
        }

        /// <summary>
        /// Checks if video files are supported on the current platform
        /// </summary>
        public static bool IsVideoSupported()
        {
            #if UNITY_WEBGL
            return false; // WebGL has limited video support
            #else
            return true;
            #endif
        }
    }
}

// EXAMPLE 
/*
VideoHelper.LoadVideoFromStreamingAssets(
    videoPlayer: videoPlayer,
    videoName: currentPicData.videoName,
    onSuccess: () =>
    {
        DebugLogger.Log("Video loaded successfully", context: this);
        videoPlayer.Play();
        videoPlayer.isLooping = true;
    },
    onError: (errorMsg) =>
    {
        DebugLogger.LogError($"Failed to load video: {errorMsg}", context: this);
        // Fallback to image
    },
    context: this
);
 */