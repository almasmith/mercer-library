## Overview

Build a simple book library application where users can record, update, and explore books. The
.NET Core backend will expose RESTful endpoints and persist data via Entity Framework Core.
The React frontend will consume the API, let users manage books, and display basic analytics.

## Requirements

### Backend (C# /.NET Core)
- Scaffold a new ASP.NET Core Web API project.
- Define a Book model with fields:
  - Id (GUID)
  - Title (string)
  - Author (string)
  - Genre (string)
  - PublishedDate (DateTime)
  - Rating (int, 1–5)
- Implement CRUD endpoints for Book:
  1. GET /api/books
  2. GET /api/books/{id}
  3. POST /api/books
  4. PUT /api/books/{id}
  5. DELETE /api/books/{id}
- Add an endpoint GET /api/books/stats that returns the number of books per genre.
- Use Entity Framework Core with migrations to persist data in SQLite or SQL Server.
- Configure Swagger/OpenAPI for API exploration.

### Frontend (React)
- Bootstrap a React app (Create React App, Vite, or equivalent).
- Implement pages/components:
  - Book List: fetch and display all books in a table or cards.
  - Book Form: create and edit books (reuse component for both).
  - Stats View: fetch /api/books/stats and render a bar or pie chart (use a library
    of your choice).

- Organize code into clear “presentation” vs. “API” layers (e.g. custom hooks or services
  for data fetching).

- Handle loading, success, and error states for all API interactions.
- Ensure forms have basic validation (required fields, rating range).

## Data Model Example

json

```
{
"id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
"title": "The Pragmatic Programmer",
"author": "Andy Hunt, Dave Thomas",
"genre": "Software",
"publishedDate": "1999-10-30T00:00:00Z",
"rating": 5
}
```

## Deliverables

- A GitHub repository (or ZIP) containing:
  - backend/ folder with the .NET solution, EF migrations, and instructions.
  - frontend/ folder with the React source and setup instructions.
  - A root README outlining:
    - Prerequisites and setup steps (dotnet ef database update, npm
      install, etc.)
    - How to run both apps concurrently.
    - Any design decisions or trade-offs.
- Swagger/OpenAPI or Postman collection for API testing.

## Evaluation Criteria

- Criterion Weight
- Feature completeness 30%
- Code clarity and organization 25%
- API design and database usage 20%
- Frontend architecture & UX 15%
- Error handling & validation 10%

## Bonus

- Secure endpoints with JWT authentication and allow multiple users to manage their own
  books.
- Containerize backend and frontend with Docker and provide a docker-compose.yml.
- Write unit tests for API controllers and React components.
- Deploy to a cloud provider (e.g., Azure App Service, AWS Elastic Beanstalk).
- Implement sorting and filtering on the Book List (by genre, rating, or published date).

## Further Exploration

Once complete, consider extending the challenge by:
- Adding a “favorites” feature backed by a many-to-many relationship.
- Integrating real-time updates with SignalR so multiple clients see changes live.
- Improving analytics (e.g., average rating over time, most-read genres).
- Setting up a CI/CD pipeline to automate tests and deployment on Git pushes.


