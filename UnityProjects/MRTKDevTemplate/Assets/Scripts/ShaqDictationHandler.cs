// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Disable "missing XML comment" warning for samples. While nice to have, this XML documentation is not required for samples.
#pragma warning disable CS1591

using Microsoft.MixedReality.Toolkit.Subsystems;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Collections;
using System.Web;
using System.Collections.Generic;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    /// <summary>
    /// Demonstration script showing how to subscribe to and handle
    /// events fired by <see cref="DictationSubsystem"/>.
    /// </summary>
    using Speech.Windows;

    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("MRTK/Examples/Dictation Handler")]
    public class ShaqDictationHandler : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The audio source where speech will be played.")]
        private AudioSource audioSource;

        public AudioSource AudioSource
        {
            get { return audioSource; }
            set { audioSource = value; }
        }
        /// <summary>
        /// Wrapper of UnityEvent&lt;string&gt; for serialization.
        /// </summary>
        [System.Serializable]
        public class StringUnityEvent : UnityEvent<string> { }

        /// <summary>
        /// Event raised while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
        /// </summary>
        [field: SerializeField]
        public StringUnityEvent OnSpeechRecognizing { get; private set; }

        /// <summary>
        /// Event raised after the user pauses, typically at the end of a sentence. Contains the full recognized string so far.
        /// </summary>
        [field: SerializeField]
        public StringUnityEvent OnSpeechRecognized { get; private set; }

        /// <summary>
        /// Event raised when the recognizer stops. Contains the final recognized string.
        /// </summary>
        [field: SerializeField]
        public StringUnityEvent OnRecognitionFinished { get; private set; }

        /// <summary>
        /// Event raised when an error occurs. Contains the string representation of the error reason.
        /// </summary>
        [field: SerializeField]
        public StringUnityEvent OnRecognitionFaulted { get; private set; }

        private IDictationSubsystem dictationSubsystem = null;
        private IKeywordRecognitionSubsystem keywordRecognitionSubsystem = null;

        /// <summary>
        /// Start dictation on a DictationSubsystem.
        /// </summary>
        public void StartRecognition()
        {
            // Make sure there isn't an ongoing recognition session
            StopRecognition();

            dictationSubsystem = XRSubsystemHelpers.DictationSubsystem;
            if (dictationSubsystem != null)
            {
                keywordRecognitionSubsystem = XRSubsystemHelpers.KeywordRecognitionSubsystem;
                if (keywordRecognitionSubsystem != null)
                {
                    keywordRecognitionSubsystem.Stop();
                }

                dictationSubsystem.Recognizing += DictationSubsystem_Recognizing;
                dictationSubsystem.Recognized += DictationSubsystem_Recognized;
                dictationSubsystem.RecognitionFinished += DictationSubsystem_RecognitionFinished;
                dictationSubsystem.RecognitionFaulted += DictationSubsystem_RecognitionFaulted;
                dictationSubsystem.StartDictation();
            }
            else
            {
                OnRecognitionFaulted.Invoke("Cannot find a running DictationSubsystem. Please check the MRTK profile settings " +
                    "(Project Settings -> MRTK3) and/or ensure a DictationSubsystem is running.");
            }
        }

        private void DictationSubsystem_RecognitionFaulted(DictationSessionEventArgs obj)
        {
            OnRecognitionFaulted.Invoke("Recognition faulted. Reason: " + obj.ReasonString);
            HandleDictationShutdown();
        }

        private void DictationSubsystem_RecognitionFinished(DictationSessionEventArgs obj)
        {
            OnRecognitionFinished.Invoke("Recognition finished. Reason: " + obj.ReasonString);
            HandleDictationShutdown();
        }

        private void DictationSubsystem_Recognized(DictationResultEventArgs obj)
        {
            OnSpeechRecognized.Invoke("Recognized:" + obj.Result);
            StartCoroutine(GetText(obj.Result));
        }

        private void DictationSubsystem_Recognizing(DictationResultEventArgs obj)
        {
            OnSpeechRecognizing.Invoke("Recognizing:" + obj.Result);
        }

        /// <summary>
        /// Stop dictation on the current DictationSubsystem.
        /// </summary>
        public void StopRecognition()
        {
            if (dictationSubsystem != null)
            {
                dictationSubsystem.StopDictation();
            }
        }

        /// <summary>
        /// Stop dictation on the current DictationSubsystem.
        /// </summary>
        public void HandleDictationShutdown()
        {
            if (dictationSubsystem != null)
            {
                dictationSubsystem.Recognizing -= DictationSubsystem_Recognizing;
                dictationSubsystem.Recognized -= DictationSubsystem_Recognized;
                dictationSubsystem.RecognitionFinished -= DictationSubsystem_RecognitionFinished;
                dictationSubsystem.RecognitionFaulted -= DictationSubsystem_RecognitionFaulted;
                dictationSubsystem = null;
            }

            if (keywordRecognitionSubsystem != null)
            {
                keywordRecognitionSubsystem.Start();
                keywordRecognitionSubsystem = null;
            }
        }
       


        // https://m-ansley.medium.com/unity-web-requests-downloading-and-working-with-json-text-9042b8e001e4
        private IEnumerator GetText(string text)
        {
            string url = "http://ec2-54-84-186-136.compute-1.amazonaws.com:8080/audio?character=shaq&question=";
            url += HttpUtility.UrlEncode(text);
            Debug.Log(url);
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if((request.result == UnityWebRequest.Result.ProtocolError)
                    || (request.result == UnityWebRequest.Result.ConnectionError))
                {
                    Debug.LogError(request.error);
                }
                else
                {
                    Debug.Log("Successfully downloaded text");
                    var responseUrl = request.downloadHandler.text;
                    Debug.Log(responseUrl);
                    OnSpeechRecognized.Invoke("response received");
                    using (UnityWebRequest multimediaRequest = UnityWebRequestMultimedia.GetAudioClip(responseUrl, AudioType.WAV))
                    {
                        yield return multimediaRequest.SendWebRequest();
                        if ((multimediaRequest.result == UnityWebRequest.Result.ProtocolError)
                             || (multimediaRequest.result == UnityWebRequest.Result.ConnectionError))
                        {
                            Debug.LogError(multimediaRequest.error);
                        }
                        else
                        {
                            audioSource.clip = DownloadHandlerAudioClip.GetContent(multimediaRequest);
                            audioSource.Play();
                        }
                    }
 
                    //                    TextToSpeechSubsystem textToSpeechSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<TextToSpeechSubsystem>();
                    //                    textToSpeechSubsystem.TrySpeak(request.downloadHandler.text, audioSource);
                }
            }
        }
    }
}
#pragma warning restore CS1591
