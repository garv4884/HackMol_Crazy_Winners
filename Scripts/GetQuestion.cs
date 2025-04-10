
// using System.Collections;
// using UnityEngine;
// using UnityEngine.Networking;
// using TMPro;

// public class InterviewManager : MonoBehaviour
// {
//     [Header("UI References")]
//     public TMP_Text questionText;
//     public TMP_Text evaluationText;

//     [Header("Settings")]
//     static string APIKey = "AIzaSyCd3YqJ-0mds_bbtlH4F06xPg8kCeJNOrI";
//     static string ProjectId = "nomadic-buffer-450805-u3";
//     static string modelName = "chat-bison";
//     static string region = "us-central1";
//     public string palmApiUrl = $"https://{region}-aiplatform.googleapis.com/v1/projects/{ProjectId}/locations/{region}/publishers/google/models/{modelName}:predict?key={APIKey}";
//     private string conversationHistory = 
//         "You are an SDE interview AI. " +
//         "Start by asking the first question. " +
//         "After each answer, evaluate it and ask the next question.\n";

//     void Start()
//     {
//         // Kick off the first question
//         AskFirstQuestion();
//     }

//     void AskFirstQuestion()
//     {
//         StartCoroutine(SendToPaLM(isFirst: true, userAnswer: ""));
//     }

//     public void SubmitAnswer(string userAnswer)
//     {
//         // Append the candidate's answer to history
//         conversationHistory += $"Candidate: {userAnswer}\n";
//         StartCoroutine(SendToPaLM(isFirst: false, userAnswer: userAnswer));
//     }

//     IEnumerator SendToPaLM(bool isFirst, string userAnswer)
//     {
//         // Build the prompt based on whether it's the first call
//         string prompt = conversationHistory;
//         if (isFirst)
//         {
//             prompt += "Please ask the first interview question.";
//         }
//         else
//         {
//             prompt += "Evaluate the answer above and then ask the next question.";
//         }

//         // Wrap it into your request format
//         var message = new Message{
//             author = "user",
//             content = prompt
//         };

//         var instance = new Instance
//         {
//             messages = new Message[] { message }
//         };

//         var requestBody = new RequestFormat
//         {
//             instances = new Instance[] { instance },
//             parameters = new Parameters()
//         };
//         string bodyJson = JsonUtility.ToJson(requestBody);

//         using var request = new UnityWebRequest(palmApiUrl, "POST")
//         {
//             uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(bodyJson)),
//             downloadHandler = new DownloadHandlerBuffer()
//         };
//         request.SetRequestHeader("Content-Type", "application/json");

//         yield return request.SendWebRequest();

//         if (request.result == UnityWebRequest.Result.Success)
//         {
//             // Expecting: { "text": "Evaluation: ...\nNext Question: ..." }
//             var resp = JsonUtility.FromJson<ResponseFormat>(request.downloadHandler.text);
//             string full = resp.text;

//             // Parse out evaluation and question
//             string eval = ExtractBetween(full, "Evaluation:", "Next Question:");
//             string nextQ = ExtractAfter(full, "Next Question:");

//             evaluationText.text = eval.Trim();
//             questionText.text   = nextQ.Trim();

//             // Append the AI's turn to history
//             conversationHistory += $"AI: {full}\n";
//         }
//         else
//         {
//             Debug.LogError($"PaLM call failed: {request.error}");
//             evaluationText.text = "Error. Please retry.";
//         }
//     }

//     // Helpers to slice the model's free‑form text
//     string ExtractBetween(string input, string start, string end)
//     {
//         int i0 = input.IndexOf(start);
//         int i1 = input.IndexOf(end);
//         if (i0 >= 0 && i1 > i0)
//             return input.Substring(i0 + start.Length, i1 - i0 - start.Length);
//         return "";
//     }

//     string ExtractAfter(string input, string marker)
//     {
//         int i = input.IndexOf(marker);
//         if (i >= 0)
//             return input.Substring(i + marker.Length);
//         return "";
//     }

//     [System.Serializable]
//     public class ResponseFormat
//     {
//         public string text;
//     }

//     [System.Serializable]
// public class Message
// {
//     public string author = "user";
//     public string content;
// }

// [System.Serializable]
// public class Instance
// {
//     public Message[] messages;
// }

// [System.Serializable]
// public class Parameters
// {
//     public float temperature = 0.5f;
//     public int maxOutputTokens = 256;
//     public float topP = 0.95f;
//     public int topK = 40;
// }

// [System.Serializable]
// public class RequestFormat
// {
//     public Instance[] instances;
//     public Parameters parameters = new Parameters();
// }

// }

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
