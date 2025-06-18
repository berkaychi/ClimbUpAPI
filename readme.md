# ClimbUpAPI

## Description

ClimbUpAPI is a .NET API designed to support productivity applications by managing tasks (ToDos), focus sessions, user authentication, and more. It provides a robust backend for tracking user activity and enhancing focus.

## Client Applications

This API serves as the backend for the following client applications:

- **ClimbUp iOS App:** The official IOS client for ClimbUp, built with Swift. You can find the repository [here](https://github.com/berkkayarslan/ClimbUpIOS).

## Getting Started

### Prerequisites

- .NET SDK (latest version recommended, e.g., .NET 8.0 or newer) - You can download it from [here](https://dotnet.microsoft.com/download).

### Building the Project

1.  Clone the repository (if you haven't already):
    ```bash
    git clone <https://github.com/berkaychi/ClimbUpAPI>
    cd ClimbUpAPI
    ```
2.  Navigate to the project's root directory (where [`ClimbUpAPI.csproj`](ClimbUpAPI.csproj:0) is located).
3.  Build the project using the .NET CLI:
    ```bash
    dotnet build
    ```

### Running the Project

1.  Ensure you are in the project's root directory.
2.  Run the project using the .NET CLI:
    ```bash
    dotnet run
    ```
    The API will start, and the console output will indicate the URLs it's listening on (commonly `http://localhost:5000` and `https://localhost:5001`).

## Features

- **User Authentication & Authorization:** Secure registration, login, token-based authentication (including refresh tokens), and role-based access control (Admin role).
- **ToDo Management:** Create, read, update, delete, and manage ToDo items with priorities and statuses.
- **Focus Session Tracking:** Start, stop, and log focus sessions, associate them with ToDos and tags.
- **Tag Management:** Create, update, and manage tags for organizing ToDos and focus sessions. Includes admin capabilities for system tags.
- **Session Type Management:** Define and manage different types of focus sessions. Includes admin capabilities.
- **User Statistics:** Track and retrieve user productivity statistics.
- **Admin Panel Functionality:** Endpoints for administrators to manage users, tags, and session types.

## API Documentation

Comprehensive API documentation, detailing all endpoints, request/response formats, and authentication requirements, is available in the [`API Endpoints Doc/`]

## Technologies Used

- **Framework:** ASP.NET Core
- **Language:** C#
- **Database:** Entity Framework Core with SQLite (as suggested by [`ClimbUp.db`](ClimbUp.db:0) and migrations structure)
- **Authentication:** JWT (JSON Web Tokens)
- **Logging:** Configured for request logging (implied by [`Middleware/RequestLogContextMiddleware.cs`](Middleware/RequestLogContextMiddleware.cs:0))
