from django.shortcuts import render
import io
from django.http import JsonResponse
from django.views.decorators.csrf import csrf_exempt
from PyPDF2 import PdfReader

@csrf_exempt
def pdfupload(req):
    if req.method == 'POST' and req.FILES.get('pdf'):
        pdf_file = req.FILES('pdf')
        try:
            reader = PdfReader(pdf_file)
            text = ""
            for page in reader.pages:
                text += page.extract_text() or ""
            response_data = {
                'status' : 'success',
                'text' : text
            }
            return JsonResponse(response_data)
        except Exception as e:
            return JsonResponse({'status' : 'error', 'message' : str(e)}, status = 500)
    else:
        return JsonResponse({'status' : 'error', 'message' : 'inavlid request'}, status = 400)