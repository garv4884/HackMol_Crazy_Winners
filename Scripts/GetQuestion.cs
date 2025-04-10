using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Unity;
using System.Collections.Generic;



public class InterviewManager : MonoBehaviour
{

    [System.Serializable]
    public class Part
    {
        public string text;
    }

    [System.Serializable]
    public class Message
    {
        public string role;
        public List<Part> parts;
    }

    [System.Serializable]
    public class ContentRequest
    {
        public List<Message> contents;
    }

    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text evaluationText;

    [Header("Settings")]
    private List<Message> conversationHistory = new List<Message>();

    public string starting_prompt = "You are SDE at google, and you hav to take interview for SDE intern, you have a strict personality, end the converstaion after 5 minutes, and begin the communication with a warm greeting";

    public void Start()
    {
        StartCoroutine(SendInitialPrompt());
    }


    void AddUserMessage(string userInput){
        conversationHistory.Add(new Message{
            role = "user",
            parts = new List<Part>{new Part { text = userInput}}

        });
    }

    void AddModelMessage(string response){
        conversationHistory.Add(new Message{
            role = "Model",
            parts = new List<Part>{new Part{text= response}}
        });
    }

    IEnumerator SendToProxy(string userInput){
        AddUserMessage(userInput);
        ContentRequest contentRequest = new ContentRequest
        {
            contents = conversationHistory
        };

        string jsonPayload = JsonUtility.ToJson(contentRequest);
        // Debug.Log("Sending Payload: ", jsonPayload);

        using (UnityWebRequest req = new UnityWebRequest("http://localhost:8000/api/chat/", "POST")){
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success){
                string jsonResponse = req.downloadHandler.text;
                Debug.Log("response: " + jsonResponse);

                string responseText = ExtractTextFromResponse(jsonResponse);
                
                AddModelMessage(responseText);

                string eval = ExtractBetween(responseText, "Evaluation:", "Next Question:");
                string question = ExtractAfter(responseText, "Next Question:");

                evaluationText.text = eval.Trim();
                questionText.text = question.Trim();

            }

            else{
                Debug.LogError("Error: " + req.error);
            }
        }
    }

    string ExtractTextFromResponse(string json)
    {
        // Basic parsing for {"response": "Your answer"}
        int index = json.IndexOf(":");
        int endIndex = json.LastIndexOf("}");
        if (index >= 0 && endIndex > index)
        {
            return json.Substring(index + 2, endIndex - index - 3); // crude but works
        }
        return "Error parsing response.";
    }

    string ExtractBetween(string input, string start, string end)
    {
        int i0 = input.IndexOf(start);
        int i1 = input.IndexOf(end);
        if (i0 >= 0 && i1 > i0)
            return input.Substring(i0 + start.Length, i1 - i0 - start.Length);
        return "";
    }
    
    string ExtractAfter(string input, string marker)
    {
        int i = input.IndexOf(marker);
        if (i >= 0)
            return input.Substring(i + marker.Length);
        return "";
    }


    public void SubmitAnswer(){
        string answer = GetComponent<SpeechToText>().answer;
        StartCoroutine(SendToProxy(answer));
    }

    IEnumerator SendInitialPrompt()
    {
        AddModelMessage(starting_prompt);

        ContentRequest contentRequest = new ContentRequest
        {
            contents = conversationHistory
        };

        string jsonPayload = JsonUtility.ToJson(contentRequest);

        using (UnityWebRequest req = new UnityWebRequest("http://localhost:8000/api/chat/", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                Debug.Log("Initial AI response: " + jsonResponse);

                string responseText = ExtractTextFromResponse(jsonResponse);
                AddModelMessage(responseText);

                // For the first message, we expect only a greeting or a first question
                evaluationText.text = ""; // No evaluation yet
                questionText.text = responseText.Trim();
            }
            else
            {
                Debug.LogError("Initial AI call failed: " + req.error);
                questionText.text = "Error starting conversation.";
            }
        }
    }


}
