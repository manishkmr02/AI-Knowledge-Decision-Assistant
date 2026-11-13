# AI Knowledge & Decision Assistant

## About the Project

AI Knowledge & Decision Assistant is a web-based application that helps organizations create their own AI assistant using their PDF documents.

An organization can create an account, upload its documents, and generate a unique assistant link. Visitors can open that link and ask questions related to the uploaded documents without creating an account.

The main purpose of this project is to provide answers based on the organization's documents instead of giving general or unrelated AI-generated responses.

## Features

- **User Registration and Login:** Organizations can create accounts and securely access their dashboards.
- **Document Management:** Upload, view, and manage PDF documents.
- **PDF Processing:** Extract text from uploaded PDFs and divide it into smaller sections for easier searching.
- **AI Assistant Creation:** Generate a unique assistant link for each organization.
- **Website Integration:** Use the provided embed code to add the assistant to another website.
- **Question and Answer:** Visitors can ask questions and receive answers based on uploaded documents.
- **Document References:** Answers can include document names and page numbers to help users verify the information.
- **Structured Decision-Making:** The application uses structured checks to evaluate conditions instead of depending only on an AI-generated response.
- **Organization Data Isolation:** Each organization can access only its own documents and assistant.

## Technologies Used

The project uses the following technologies:

- **C#** – Main programming language.
- **ASP.NET Core** – For developing the web application and backend APIs.
- **SQL Server** – For storing application and organization data.
- **Google Gemini API** – For understanding questions and generating answers.
- **PDF Processing Library** – For extracting text from PDF documents.
- **HTML, CSS and JavaScript** – For designing and handling the user interface.

*Note: Update this list if your actual project uses additional libraries or technologies.*

## How the Application Works

The application follows these steps:

1. An organization creates an account and logs in.
2. The organization uploads its PDF documents.
3. The application extracts text from the documents.
4. The extracted text is divided into smaller sections called chunks.
5. The application stores the processed information along with document and page details.
6. A unique assistant link is generated for the organization.
7. Visitors open the assistant link and ask questions.
8. The application identifies the correct organization using the assistant token.
9. It searches the relevant organization's documents for information related to the question.
10. Structured decision checks are performed when the question requires them.
11. The AI model prepares the final answer using the retrieved information.
12. The answer is displayed to the visitor with document references wherever available.

## AI Architecture

The application combines document-based search, a language model, and structured decision-making.

Instead of sending every question directly to the AI model and accepting its response, the application follows a step-by-step process.

**1. Question Understanding**

The language model helps understand the user's question and identify the information required to answer it.

**2. Document Search**

The application searches the uploaded documents and retrieves relevant sections.

**3. Structured Decision-Making**

When a question involves multiple conditions, the application performs structured checks using decision logic.

For example, a scholarship eligibility question may require checking the student's age, course type, postgraduate admission status, and institution criteria.

**4. Final Answer Generation**

The retrieved document information and decision results are used to prepare a clear answer for the visitor.

**5. References**

The application provides document and page references where available, allowing users to verify the answer from the original PDF.

## Organization Data Security

The application is designed to keep organization data separate.

- Each organization has its own documents and assistant.
- The assistant token is used to identify the correct organization.
- Visitors can access the assistant without logging in.
- Visitors cannot access the organization's administration dashboard through the assistant link.
- Document searches are restricted to the organization associated with the assistant token.

## Project Setup

Follow these steps to run the project locally.

### Prerequisites

Before starting, make sure you have:

- Visual Studio or Visual Studio Code
- .NET SDK (the version used by this project)
- SQL Server
- SQL Server Management Studio (SSMS)
- A Google Gemini API key

### 1. Clone the Repository

Open a terminal and run:

```bash
git clone YOUR_GITHUB_REPOSITORY_URL
```

Move into the project folder:

```bash
cd YOUR_PROJECT_FOLDER
```

### 2. Configure the Database

Open SQL Server Management Studio.

Create the required database and run the database creation scripts included in the project.

Update the database connection string in your application configuration.

Example:

```json
"ConnectionStrings": {
  "DefaultConnection": "YOUR_SQL_SERVER_CONNECTION_STRING"
}
```

Replace the example value with your own SQL Server connection string.

### 3. Configure the Gemini API Key

Add your Gemini API key using the configuration method expected by the application.

For example, if your application reads it from configuration:

```json
"Gemini": {
  "ApiKey": "YOUR_GEMINI_API_KEY"
}
```

**Important:** Do not upload your actual API key, database password, or other private credentials to GitHub.

Use your local configuration or environment variables for sensitive information.

### 4. Restore Packages

Open the project in Visual Studio or run:

```bash
dotnet restore
```

### 5. Run the Application

Run the project using Visual Studio or execute:

```bash
dotnet run
```
## Note

The answers generated by the assistant depend on the information available in the documents uploaded by the organization. Users should refer to the original documents for confirmation of important information.
