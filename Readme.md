# SoccerQueryAPI — AI-Powered SQL Query Engine

> **Smart Natural Language to SQL API** built with **.NET 8**, **Semantic Kernel**, and **Google Gemini (or OpenAI)**.  
> Converts user questions about soccer data into executable SQL queries and returns results from a SQLite database.

---

## Overview

**SoccerQueryAPI** allows users to ask natural language questions (NLQs) such as:

> “Which team scored the most goals in 2015?”

The API uses **Semantic Kernel** + **Google Gemini (or OpenAI)** to:
1. Convert the natural language into a valid **SQL query**.
2. **Validate and execute** the SQL safely against a SQLite database.
3. Return **structured JSON results** along with query execution time.

---

## Architecture Overview

```
+----------------------------+
|  User / Client Application |
+-------------+--------------+
              |
              v
    [1] /api/Query/generateAndExecuteQuery  → QueryController
              |
              v
    [2] SemanticKernelService  →  Google Gemini / OpenAI
              |
              v
    [3] SQL Generator (Prompt)
              |
              v
    [4] SqlValidator  →  Whitelist Check
              |
              v
    [5] DatabaseHelper → SQLite Query Executor
              |
              v
    [6]  JSON Response
```

## Project Structure
```
SoccerQueryAPI/
│
├── Controllers/
│   └── QueryController.cs       # All API endpoints (generate, execute, combined, test)
│
├── Data/
│    └── database.sqlite          # SQLite database with Soccer dataset
│   ├── DatabaseHelper.cs        # Executes SQL against SQLite, with timer
│   └── SqlValidator.cs          # Validates allowed SQL queries
│
├── Models/
│   └── DTOs.cs                  # API request/response models
│
├── Services/
│   └── SemanticKernelService.cs # Handles AI prompt → SQL generation  
│
├── Program.cs                   # App startup configuration, DI, Swagger, etc.
├── appsettings.json             # Configuration (API key, model, connection, rules)
└── README.md                    # Project documentation
```

## Setup Instructions

### 🔹 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Google Gemini API Key](https://makersuite.google.com/) or OpenAI API Key
- SQLite database file (`Data/database.sqlite`)

### 🔹 Dataset Information 
 - Source: European Soccer Database - [Kaggle](https://www.kaggle.com/datasets/hugomathien/soccer )

### 🔹 Configuration

Edit **`appsettings.json`** to include your credentials and settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data/database.sqlite"
  },
  "OpenAI": {
    "ApiKey": "YOUR_GEMINI_OR_OPENAI_KEY",
    "Model": "gemini-2.5-flash"
  },"SqlExecution": {
  "MaxRows": 1000,
  "CommandTimeoutSeconds": 15
},
"AllowedSql": {
  "Tables": [ "Match", "Team", "Player", "Player_Attributes" ],
  "Columns": [
    "match_api_id",
    "date",
    "home_team_api_id",
    "away_team_api_id",
    "home_team_goal",
    "away_team_goal",
    "team_api_id",
    "team_long_name",
    "team_short_name",
    "player_api_id",
    "player_name",
    "birthday",
    "overall_rating",
    "potential",
    "sprint_speed"
  ]
},
}
```
### 🔹 Install Dependencies
 - dotnet restore

### 🔹 Run the API
 - dotnet run


