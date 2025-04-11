import json
import google.auth
from google.auth.transport.requests import Request
from google.oauth2 import service_account
import requests
from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework.permissions import AllowAny
from decouple import config

API_KEY = config("API_KEY")

class AIChat(APIView):
    permission_classes = [AllowAny]

    def post(self, req):
        
        contents = req.data.get("contents", [])

        if not contents or not isinstance(contents, list):
            return Response({"response":"missing or invalid contents"}, status = 400)
        
        
        url = f"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={API_KEY}"

        payload = {
            "contents" : contents
        }

        headers = {"Content-Type" : "application/json"}
        response = requests.post(url, json=payload, headers = headers)

        if response.status_code == 200:
            data = response.json()
            try:
                text = data['candidates'][0]['content']['parts'][0]['text']
            except:
                text = "Unexpected response structure."
            
            return Response({"response" : text})
        else:
            return Response({"response", response.text}, status = response.status_code)

class EvaluateInterview(APIView):
    permission_classes = [AllowAny]

    def post(self, request):
        contents = request.data.get("contents", [])
        if not contents:
            return Response({"error": "Conversation history is required"}, status=400)

        # Add system message to guide AI personality
        system_instruction = {
            "role": "user",
            "parts": [{
                "text": "Evaluate the entire interview based on this conversation. Provide insights on strengths, weaknesses, and possible improvements. Format the output like this: Strengths: ..., Weaknesses: ..., Suggestions: ..."
            }]
        }
        contents.append(system_instruction)

        url = f"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro-latest:generateContent?key={API_KEY}"
        headers = {"Content-Type": "application/json"}

        payload = {"contents": contents}

        response = requests.post(url, headers=headers, json=payload)

        if response.status_code == 200:
            try:
                text = response.json()['candidates'][0]['content']['parts'][0]['text']
            except:
                text = "Unexpected response structure."
            return Response({"response": text})
        else:
            return Response({"error": response.text}, status=response.status_code)

