from django.urls import path, include
from . import views

urlpatterns = [
    path('chat/', views.AIChat.as_view(), name = "aichat"),
]