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
