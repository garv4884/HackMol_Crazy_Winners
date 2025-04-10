using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System;
using TMPro;
using System.Linq;

public class SpeechToText : MonoBehaviour
{

    public AudioClip audio_clip;
    public AudioSource audio_source;
    private string microphonename;
    private string apikey;
    // private string convoai_apikey = "5b6eea40c46047a7f7669c9843890367";

    public string answer = "";

    public TextMeshProUGUI transcriptionText;

    void Awake()
    {
        TextAsset envFile = Resources.Load<TextAsset>("env");
        if(!envFile) Debug.LogError("Error: env.json not found");
        else{
            EnvData data = JsonUtility.FromJson<EnvData>(envFile.text);
            apikey = data.API_KEY_SPEECH_TO_TXT;
            Debug.Log("Loaded API key");
        }
    }

    void Start()
    {
        if(audio_source == null) audio_source = gameObject.AddComponent<AudioSource>();

        if(Microphone.devices.Length > 0){
            microphonename = Microphone.devices[0];
            Debug.Log($"Microphone found: {microphonename}");
        }
        else Debug.Log("Microphone not found");

    }

    public void StartRecording(){
        if(Microphone.IsRecording(microphonename)){
            Debug.Log("already recording...");
            return;
        }

        audio_source.clip = Microphone.Start(microphonename, false, 10, 44100);
        Debug.Log("Recording...");
    }

    public void StopRecording(){
        if(Microphone.IsRecording(microphonename)){
            Microphone.End(microphonename);
            Debug.Log("Recording Ended");
        }
    }

    public void PlayRecording(){
        audio_source.Play();
    }

    public void SendToGoogleSpeechAPI(){
        byte[] wavData = ConvertAudioClipToWav(audio_source.clip);
        string base64Audio = Convert.ToBase64String(wavData);
        StartCoroutine(SendRequest(base64Audio));
    }

    private string ParseTranscription(string jsonResponse)
    {
        try
        {
            GoogleSpeechResponse response = JsonUtility.FromJson<GoogleSpeechResponse>(jsonResponse);
            if (response.results != null && response.results.Length > 0)
            {
                return response.results[0].alternatives[0].transcript;
            }
            else
            {
                return "No transcription found.";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to parse response: " + e.Message);
            return "Error parsing transcription.";
        }
    }

    IEnumerator SendRequest(string base64Audio){
        string url = $"https://speech.googleapis.com/v1/speech:recognize?key={apikey}";
        string jsonPayload = "{" +
            "\"config\": {" +
            "\"encoding\": \"LINEAR16\"," +
            "\"sampleRateHertz\": 44100," +
            "\"languageCode\": \"en-US\"" +
            "}," +
            "\"audio\": {" +
            "\"content\": \"" + base64Audio + "\"" +
            "}" +
            "}";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success){
            Debug.Log("Error: " + request.error);
        }
        else{
            Debug.Log("Transcription Result: " + request.downloadHandler.text);
            string responseText = request.downloadHandler.text;

            string transcription = ParseTranscription(responseText);

            if (transcription != null){;
                transcriptionText.text = transcription;
                answer = transcription;
            }
        }

    }

    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        // WAV header
        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + samples.Length * 2);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)clip.channels);
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * clip.channels * 2);
        writer.Write((short)(clip.channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(samples.Length * 2);

        // WAV data
        foreach (float sample in samples)
        {
            short intSample = (short)(sample * short.MaxValue);
            writer.Write(intSample);
        }

        writer.Flush();
        return stream.ToArray();
    }

    // public void SubmitAnswerMain(){
    //     InterviewManager interviewManager = GetComponent<InterviewManager>();
    //     interviewManager.SubmitAnswer(answer);
    // }

    [System.Serializable]
    private class EnvData
    {
        public string API_KEY_SPEECH_TO_TXT;
    }

    [Serializable]
    public class GoogleSpeechResponse
    {
        public Result[] results;
    }
    [Serializable]
    public class Result
    {
        public Alternative[] alternatives;
    }

    [Serializable]
    public class Alternative
    {
        public string transcript;
    }
}
