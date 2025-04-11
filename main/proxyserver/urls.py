from django.urls import path, include
from . import views

urlpatterns = [
    path('chat/', views.AIChat.as_view(), name = "aichat"),
    path('chat-end/', views.EvaluateInterview.as_view(), name='end'),
]