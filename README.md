# visual-programming-project

# TravelMate AI — Pakistan Tourism Booking Platform

![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-XAMPP-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![C#](https://img.shields.io/badge/C%23-.NET-239120?style=for-the-badge&logo=csharp&logoColor=white)

A complete AI-powered tourism and travel booking platform for Pakistan.

Semester End Project — 2024/2025

---

## Team Members

| Name | Role |
|------|------|
| **Aleeza Maryam** | Authentication, Destinations, Hotels, Reviews, Admin Panel |
| **Shanza Naveed** | AI Trip Planner, Itinerary, Budget, Transport, Booking System |

---

## Project Overview

TravelMate AI is a full-stack web application built with ASP.NET Core MVC that allows users to explore Pakistan tourist destinations, book hotels and transport, plan AI-generated itineraries, and manage travel packages — all in one platform.

---

## Features

### Destination Module
- Browse 50+ Pakistan tourist destinations fetched via OpenStreetMap Nominatim API
- Search destinations by city name
- Each destination shows live weather, map, nearby hotels, transport options, and reviews

### Hotel Booking
- Real hotel data fetched from OpenStreetMap Overpass API
- Hotel name, star rating, price per night, address, phone number
- Book hotel with check-in/check-out dates and number of guests
- Combined booking reference generated automatically

### Live Weather
- Current weather and 5-day forecast via OpenWeatherMap API
- Temperature, humidity, wind speed, feels like
- Weather shown on every destination detail page

### Interactive Map
- Location map using Leaflet.js and OpenStreetMap — completely free, no API key needed
- Marker shown for every destination

### Transport System
- Bus, Train, and Flight options for major Pakistan routes
- Filter by transport type
- Fare, departure time, duration, available seats shown
- Select transport and add to booking

### Tour Packages
- Pre-built travel packages — Budget, Standard, Premium, Honeymoon, Family
- Package details with inclusions, itinerary, and pricing
- Group discount for 4 or more persons
- PDF invoice generated and sent to email on booking confirmation

### AI Trip Planner
- AI-powered itinerary generator based on budget, interests, and duration
- Day-wise activity plan automatically created
- Budget estimator with breakdown covering hotel, food, transport, activities

### Reviews System
- Users can rate destinations 1 to 5 stars and write reviews
- Edit and delete own reviews
- Average rating shown on destination page

### User Account
- Register and login with session-based authentication
- User dashboard with upcoming trips, bookings, and stats
- Profile management with travel preferences
- Wishlist and Favorites for destinations

### Admin Panel
- Manage users — block, unblock, delete
- View all bookings and revenue stats
- Add, edit, delete destinations and hotels
- Admin activity logs

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core MVC (.NET 10) |
| Language | C# |
| Database | MySQL via XAMPP |
| ORM | Entity Framework Core + Pomelo |
| Frontend | Bootstrap 5, Font Awesome, jQuery |
| Map | Leaflet.js + OpenStreetMap |
| Email | MailKit + MimeKit |
| PDF | iTextSharp |
| Session | ASP.NET Core Session |
| Password | SHA-256 Hashing |

---

## APIs Used

| API | Purpose | Cost |
|-----|---------|------|
| OpenStreetMap Nominatim | City coordinates and geocoding | Free |
| Overpass API | Real hotel data by location | Free |
| OpenWeatherMap | Current weather and 5-day forecast | Free — 1000 requests per day |
| Leaflet.js | Interactive maps | Free |
| Unsplash | Destination and hotel images | Free |

---

## Database Schema

22 tables including:

```
users, roles, destinations, categories, hotels, hotel_rooms,
hotel_bookings, transport, transport_bookings, trips, itineraries,
packages, package_bookings, destination_reviews, reviews,
favorites, notifications, payments, weather, ai_recommendations,
chatbot_history, admin_logs
```

---

## Getting Started

### Prerequisites

- Visual Studio 2022
- .NET 10 SDK
- XAMPP with MySQL and Apache
- Git

### Installation

**1. Clone the repository**

```bash
git clone https://github.com/yourusername/TravelMateAI.git
cd TravelMateAI
```

**2. Start XAMPP**

Open XAMPP Control Panel and start Apache and MySQL.

**3. Setup Database**

Open phpMyAdmin at http://localhost/phpmyadmin

Create a new database named ai_tourism_planner

Import the SQL file:

```
database/ai_tourism_planner.sql
```

**4. Configure appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ai_tourism_planner;Uid=root;Pwd=;"
  },
  "WeatherAPI": {
    "Key": "YOUR_OPENWEATHERMAP_KEY",
    "BaseUrl": "https://api.openweathermap.org/data/2.5"
  },
  "OpenTripMap": {
    "Key": "YOUR_OPENTRIPMAP_KEY",
    "Host": "opentripmap-places-v1.p.rapidapi.com"
  }
}
```

**5. Install NuGet Packages**

```powershell
Install-Package Pomelo.EntityFrameworkCore.MySql
Install-Package Microsoft.EntityFrameworkCore.Tools
Install-Package Newtonsoft.Json
Install-Package MailKit
Install-Package MimeKit
Install-Package iTextSharp
```

**6. Run the Project**

```bash
dotnet run
```

Or press F5 in Visual Studio.

Open browser at: https://localhost:7xxx

---

## Project Structure

```
TravelMateAI/
│
├── Controllers/
│   ├── AccountController.cs
│   ├── DestinationsController.cs
│   ├── TripsController.cs
│   ├── PackagesController.cs
│   ├── BookingsController.cs
│   ├── AdminController.cs
│   ├── WeatherController.cs
│   └── RealHotelsController.cs
│
├── Models/
│   ├── User.cs
│   ├── Destination.cs
│   ├── Hotel.cs
│   ├── Trip.cs
│   ├── Booking.cs
│   ├── Package.cs
│   └── ...
│
├── Services/
│   ├── DestinationApiService.cs
│   ├── WeatherService.cs
│   ├── RealHotelService.cs
│   ├── AIRecommendationService.cs
│   └── ItineraryGenerator.cs
│
├── Views/
│   ├── Destination/
│   ├── Account/
│   ├── Trips/
│   ├── Packages/
│   ├── Bookings/
│   ├── Admin/
│   └── Shared/
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── ViewModels/
│   ├── ApiDestinationViewModel.cs
│   ├── TripPlannerViewModel.cs
│   └── ...
│
├── appsettings.json
└── Program.cs
```

---

## Default Admin Access

```
Email: admin@travelmate.com
Password: admin123
```

Change these credentials before deployment.

---

## Email and PDF System

On package booking confirmation:
- PDF invoice is auto-generated using iTextSharp
- Email sent to user via MailKit and Gmail SMTP
- Booking reference number generated
- Downloadable voucher available

---

## Screenshots

Add screenshots here after project completion.

| Page | Description |
|------|-------------|
| Homepage | Featured destinations and packages |
| Destinations | 50+ Pakistan tourist places |
| Destination Detail | Hotels, Weather, Map, Transport, Reviews |
| AI Trip Planner | Budget-based itinerary generator |
| Tour Packages | Complete travel packages |
| My Bookings | Booking history and management |
| Admin Dashboard | Stats and user management |

---

## Acknowledgements

- OpenStreetMap — Map and location data
- OpenWeatherMap — Weather API
- Unsplash — Travel images
- Bootstrap — UI Framework
- Leaflet.js — Interactive maps

---

## License

This project is developed as a Semester End Project for academic purposes.

---

Developed by Aleeza Maryam and Shanza Naveed
