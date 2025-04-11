using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Windows.Forms;
using System.IO;
using UnityEngine.Networking;

public class uploadPDF : MonoBehaviour
{

    private readonly string uploadUrl = "https://localhost:8000/pdf/upload/";

    public void OnUploadButtonClicked()
    {
        string filepath = GetPDFFilePath();
        if(!string.IsNullOrEmpty(filepath)){
            StartCoroutine(UploadPDF(filepath));
        }

    }
    

    IEnumerator UploadPDF(string filePath)
    {
        // Read the PDF file as bytes.
        byte[] fileData = System.IO.File.ReadAllBytes(filePath);

        WWWForm form = new WWWForm();
        // The key "pdf" corresponds to the view's request.FILES['pdf']
        form.AddBinaryData("pdf", fileData, System.IO.Path.GetFileName(filePath), "application/pdf");

        UnityWebRequest www = UnityWebRequest.Post(uploadUrl, form);
        // Optionally, add CSRF token if your Django setup requires it (use UnityWebRequest.SetRequestHeader)

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Upload succeeded! Response: " + www.downloadHandler.text);
            // Process the JSON response to display or further handle extracted text
        }
        else
        {
            Debug.LogError("Upload failed: " + www.error);
        }
    }

    // Dummy file picker method; replace with an actual file dialog solution.
    public string GetPDFFilePath()
    {
        // Implement your file selection logic here.
        string filePath = "";
        OpenFileDialog ofd = new OpenFileDialog();
        ofd.Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            filePath = ofd.FileName;
            Debug.Log("Selected PDF: " + filePath);
        }
        return filePath;
    }
}
