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

## What is Semantic Kernel?

**Microsoft Semantic Kernel (SK)** is an open-source framework that helps integrate **Large Language Models (LLMs)** like Gemini, GPT-4, or Claude into your .NET applications.

It enables developers to:
- Create **AI-powered workflows** that mix natural language and code.
- Use **prompt templates** to transform user input (like a question) into structured output (like SQL).
- Chain together **functions** (called *skills*) for text generation, data retrieval, or reasoning.
- Easily switch between AI models (OpenAI, Azure OpenAI, Google Gemini, etc.).

In this project, **Semantic Kernel** acts as the **middleware** between:
- The **user’s question**, and
- The **AI model** that generates SQL commands dynamically.

This makes it ideal for *AI-assisted data querying systems* like **SoccerQueryAPI**.

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
│    ├── DatabaseHelper.cs        # Executes SQL against SQLite, with timer
│    └── SqlValidator.cs          # Validates allowed SQL queries
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

### 🔹 Install Required NuGet Packages
 - dotnet add package Microsoft.SemanticKernel  
 - dotnet add package Microsoft.Data.Sqlite 
 - dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI
 - dotnet add package Microsoft.SemanticKernel.Connectors.Google --version 1.61.0-alpha

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

 Then open:

👉 https://localhost:7241/swagger

to test endpoints interactively.

## API Endpoints

 - POST /api/Query/test 
    - Purpose : Test AI connectivity

 Request Body : 
  ```
 {
  "question": "who are you"
  }
  ```
  Response : 
  ```
  {
  "status": "success",
  "data": "I am a large language model trained by Google.",
  "message": null,
  "apiExecutionMs": 1997,
  "databaseExecutionMs": 0
}
  ```

 - POST /api/Query/generate-query
    - Purpose : Generate SQL from a natural language question (no DB execution)

Request Body : 
  ```
  {
  "question": "Which team won the most matches in 2015?"
}
  ```
Response:
```
{
  "status": "success",
  "data": "SELECT\n  T.team_long_name\nFROM Match AS M\nJOIN Team AS T\n  ON T.team_api_id = M.home_team_api_id\nWHERE\n  STRFTIME('%Y', M.date) = '2015' AND M.home_team_goal > M.away_team_goal\nGROUP BY\n  T.team_long_name\nORDER BY\n  COUNT(T.team_api_id) DESC\nLIMIT 1;",
  "message": null,
  "apiExecutionMs": 3783,
  "databaseExecutionMs": 0
}
```


- POST /api/Query/execute-query 
    - Purpose : Execute a provided SQL query directly against the database

Request Body : 
  ```
{
  "sql": "SELECT T.team_long_name FROM Match AS M JOIN Team AS T ON T.team_api_id = M.home_team_api_id WHERE STRFTIME('%Y', M.date) = '2015' AND M.home_team_goal > M.away_team_goal GROUP BY T.team_long_name ORDER BY COUNT(T.team_api_id) DESC LIMIT 1",
  "bypassValidation": false
}

  ```
Response:
```
{
  "status": "success",
  "data": {
    "results": [
      {
        "team_long_name": "Real Madrid CF"
      }
    ]
  },
  "message": null,
  "apiExecutionMs": 157,
  "databaseExecutionMs": 154
}
```

- POST /api/Query/generateAndExecuteQuery 
    - Purpose : End-to-end process → Generate SQL → Validate → Execute

Request Body : 
  ```
 {
  "question": "Which team won the most matches in 2015?",
  "bypassValidation": false
}
  ```
Response:
```
{
  "status": "success",
  "data": {
    "question": "Which team won the most matches in 2015?",
    "generatedSql": "SELECT T.team_long_name FROM (SELECT team_id, COUNT(*) AS wins FROM (SELECT home_team_api_id AS team_id FROM Match WHERE SUBSTR(date, 1, 4) = '2015' AND home_team_goal > away_team_goal UNION ALL SELECT away_team_api_id AS team_id FROM Match WHERE SUBSTR(date, 1, 4) = '2015' AND away_team_goal > home_team_goal) AS all_wins GROUP BY team_id ORDER BY wins DESC LIMIT 1) AS TopWinner JOIN Team AS T ON TopWinner.team_id = T.team_api_id",
    "results": [
      {
        "team_long_name": "Celtic"
      }
    ]
  },
  "message": null,
  "apiExecutionMs": 5290,
  "databaseExecutionMs": 230
}
```

- GET /api/Query/history
- DELETE /api/Query/history

## SQL Validation & Security

To prevent SQL injection or misuse:
 - Only SELECT statements are allowed.
 - Only approved tables/columns are accessible (from config).
 - LIMIT clause automatically added if missing.
 - No INSERT, UPDATE, or DELETE operations allowed.

Handled by SqlValidator.cs.


