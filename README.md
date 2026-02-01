# Helpdesk Ticket Tracker (ASP.NET Core)

Live Demo: https://helpdesktickettracker.onrender.com/

A helpdesk-style web application built with ASP.NET Core for tracking support tickets in a simple, structured workflow.

## Features
- Ticket creation and editing
- Ticket status tracking (e.g., Open, In Progress, Resolved)
- Basic admin-friendly structure (Controllers/Views/Pages)
- Clean, practical UI for demonstration and learning

## Demo Notes
This is a portfolio demo deployment. The database may reset during redeploys/restarts, so test data might not persist.

## Tech Stack
- ASP.NET Core (.NET 8)
- SQLite (demo environment)
- MVC/Razor structure

## How to Run Locally
1. Open the project folder
2. Run:
   - `dotnet restore`
   - `dotnet run --project .\HelpdeskTicketTracker\HelpdeskTicketTracker.csproj`
3. Open the localhost URL shown in the terminal
