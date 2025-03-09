# WGU C971 Mobile App

A .NET MAUI Android mobile application for WGU students to track academic terms, courses, and assessments.

## Screenshots

- **Terms Page**
  
  <img src="https://github.com/MatthewMorrow/C971-mobile-app/blob/main/Screenshots/TermsPage.png" width="300" alt="Terms Page">

- **Term Details**
  
  <img src="https://github.com/MatthewMorrow/C971-mobile-app/blob/main/Screenshots/TermDetails.png" width="300" alt="Term Details">

- **Course Detail Page (View 1)**
  
  <img src="https://github.com/MatthewMorrow/C971-mobile-app/blob/main/Screenshots/CourseDetailPage1.png" width="300" alt="Course Detail Page 1">

- **Course Detail Page (View 2)** 
 
  <img src="https://github.com/MatthewMorrow/C971-mobile-app/blob/main/Screenshots/CourseDetailPage2.png" width="300" alt="Course Detail Page 2">

- **Course Detail Page (Notifications)**  

  <img src="https://github.com/MatthewMorrow/C971-mobile-app/blob/main/Screenshots/CourseDetailPageNotifications.png" width="300" alt="Course Detail Page Notifications">

- **Assessment Detail Page**  

  <img src="https://github.com/MatthewMorrow/C971-mobile-app/blob/main/Screenshots/AssessmentDetailPage.png" width="300" alt="Assessment Detail Page">

- **Assessment Detail Page (Notifications)**  

  <img src="https://github.com/MatthewMorrow/C971-mobile-app/blob/main/Screenshots/AssessmentDetailPageNotifications.png" width="300" alt="Assessment Detail Page Notifications">

## Features

- **Term Management**: Create, edit, and delete academic terms with start/end dates (using date pickers)
- **Course Tracking**: Add up to six courses per term with title, start/end dates, status, and instructor details
- **Assessment Handling**: Track objective and performance assessments with optional notifications
- **Notifications**: Get alerts for important course and assessment dates
- **Data Storage**: Local persistence with SQLite
- **Notes Sharing**: Export course notes via email, SMS, or other apps

## Quick Start
1. Open in Visual Studio with .NET MAUI workload installed
2. Restore NuGet packages and build
3. Deploy to Android emulator or physical device

## Dependencies
* .NET MAUI
* SQLite-net
* Plugin.LocalNotification
