# Library Project

How I went about completing the project using AI.

## Initial Plan
I start out by collaborating with AI on what we're trying to accomplish. My initial goal is to get a comprehensive plan that is actionable. Here was the first prompt:

>We're going to complete a coding challenge. Think hard about the steps necessary to complete this challenge. Break the challenge down into all the necessary tasks required to complete the challenge. We're not writing any code right now. We just want make a detailed plan.
>
>`<challenge>`
>
>`</challenge>`

I made a small mistake here. It would have been better to complete the base challenge first, but I was a bit too greedy:

> We are going to complete all bonus parts of this challenge, so do not list them as bonus. Include them as a core part of the plan. Write the plan to project-plan.md.

This got us our initial plan. Our first thought might be to read the plan to see if it meets the challenge requirements, but AI is great at that too. I opened a new chat for a clean context and asked.

> Read project-plan.md. Compare it to the following challenge and try to identify at least 3 parts of the challenge that are not adequately addressed in the plan. Do not hallucinate. If you cannot find any problems with the plan, let me know that. Be very critical. Make sure that if we follow the plan, then the project will be completed successfully. As we are going to complete the bonus parts of the challenge as well, treat them as integral to project success.
>
> `<challenge>`
>
> `</challenge>`

This resulted in some clarifying points such as which frontend we're going to use, being clear about unit tests, and other details.

One thing that can commonly happen is for AI to overshoot what is needed and add scope. So then I go the opposite direction of making sure everything is there. Giving the AI a persona can often help it perform better.

> You are a seasoned project manager who helps teams complete projects on time and under budget. One way that you do this is by helping teams stay laser focused on exactly what is needed to accomplish the task at hand while avoiding scope creep. Review the project-plan.md and evaluate it against the challenge.md. Note any parts of the plan that are not directly related to successful completion of the challenge.

This resulted in some good advice, that I should have followed (trimming things):

> Summary:
> - Plan covers auth, sorting/filtering, tests, Docker, deployment, CI/CD.
> - Add favorites, SignalR, and advanced analytics (plus data collection for “most-read”) to meet the full (expanded) scope.
> - Consider trimming ETag/concurrency, rate limiting, health/correlation, and over-specific JSON/monitoring if schedule/budget are tight.

Now that we "spoke" to the project manager, it was time to get principal engineers involved.

> You are a principal backend engineer. You're an expert with ASP.NET, Entity Framework, and related technologies. You value creating APIs that make sense and provide data to frontend applications in simple yet performant ways. You are the one primarily responsible for successful completion of the backend part of the challenge below. Evaluate the plan and identify any missing information that would help lead to project success. The completeness of the plan is critical as it will be used to direct the work of others. If the plan looks correct, simply say so, don't find problems that don't exist.

This resulted in adding a level of detail that wasn't there before such as field requirements and constraints, api contracts, and more.

Sometimes it's also beneficial to have a debate between different parties. This can surface thoughts and ideas that were not present, or just give confidence in the current direction.

> The project manager has provided the following feedback. Please evaluate it and give me your thoughts.
>
> `<feedback>`
>
> `</feedback>`

And then to the frontend:

> You are a principal frontend engineer. You are an expert in react and related technologies. You value readable, maintainable code that creates stunning UIs and performant consumption of REST APIs. You are the primary one responsible for the successful implementation of the challenge below as it relates to frontend work. Evaluate the plan and identify any missing information that would help lead to project success. The completeness of the plan is critical as it will be used to direct the work of others. If the plan looks correct, simply say so, don't find problems that don't exist.

After the additions that came from these prompts, I felt like the plan was pretty solid.

## Task Breakdown

With the project plan in place it was time to break things down into tasks that agents could complete. The principal engineers were the ones to do that. This resulted in the backend-tasks.md and frontend-tasks.md that you see now. It would be a simple matter to hook up to linear's MCP server here and have the tasks created directly in linear. I've done this before and it worked well.

> Looking at the project plan think deeply about how your going to break up the tasks for your backend team. Each task should represent a single responsibility. As much as possible, tasks should be able to be completed in parallel. The tasks should cover all responsibilities of the backend team and none of the frontend team. Write the tasks and subtasks to backend-tasks.md.

> *duplicate prompt for frontend*

Just like we double checked the plan we do the same thing for our tasks. That means a new chat and a new "manager" persona for backend and frontend.

> You are a seasoned backend engineering professional who leads and orchestrates the efforts of backend engineering teams. Based on project-plan.md a series of backend tasks have been created focusing on cohesive tasks that can be completed in parallel. Review backend-tasks.md to identify if the task breakdown fully addresses the project plan. Try to identify whether or not if adherence to the plan will lead to successful project completion. Identify any missing considerations or other discrepencies.

> *duplicate prompt for frontend*

With the adjustments from these finished, we had a full set of tasks that should fully represent project completion. Now it was time to start prompting the individual agents that will complete the tasks. Only we don't do that ourseles either. We ask our "managers" to do it.

>Create a prompt for an agent to complete task B1 according to project requirements.

Which created this:
```
You are a backend automation agent. Complete task [B1] “Initialize solution and projects” per the project plan.

Context
- Repo root: /Users/jason/Projects/mercer
- Target stack: .NET 8 (net8.0)
- Expected structure:
  backend/
    Library.sln
    src/
      Library.Api/
      Library.Tests/

Constraints
- Use absolute paths in commands.
- Be idempotent: if artifacts already exist, skip creation and proceed.
- Scope strictly to B1: no EF, Identity, CORS, JWT, or other config yet.
- Enable Swagger only in Development for now.
- Use controllers-based Web API template.

Steps
1) Ensure directories exist:
- Create /Users/jason/Projects/mercer/backend/src if missing.

2) Create solution:
- dotnet new sln -n Library -o /Users/jason/Projects/mercer/backend

3) Create Web API project with controllers (net8.0):
- dotnet new webapi -n Library.Api -o /Users/jason/Projects/mercer/backend/src/Library.Api --framework net8.0 --use-controllers
- Verify Program.cs includes:
  - builder.Services.AddControllers();
  - app.MapControllers();
  - builder.Services.AddEndpointsApiExplorer();
  - builder.Services.AddSwaggerGen();
  - if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

4) Create xUnit test project (net8.0):
- dotnet new xunit -n Library.Tests -o /Users/jason/Projects/mercer/backend/src/Library.Tests --framework net8.0

5) Add projects to solution:
- dotnet sln /Users/jason/Projects/mercer/backend/Library.sln add /Users/jason/Projects/mercer/backend/src/Library.Api/Library.Api.csproj
- dotnet sln /Users/jason/Projects/mercer/backend/Library.sln add /Users/jason/Projects/mercer/backend/src/Library.Tests/Library.Tests.csproj
- dotnet add /Users/jason/Projects/mercer/backend/src/Library.Tests/Library.Tests.csproj reference /Users/jason/Projects/mercer/backend/src/Library.Api/Library.Api.csproj

6) Build:
- dotnet build /Users/jason/Projects/mercer/backend/Library.sln -c Debug

7) Run API (non-blocking verification):
- dotnet run --project /Users/jason/Projects/mercer/backend/src/Library.Api/Library.Api.csproj
- Verify Swagger UI reachable at http://localhost:5000/swagger or https://localhost:5001/swagger
- Stop the process when verified.

Deliverables
- /Users/jason/Projects/mercer/backend/Library.sln
- /Users/jason/Projects/mercer/backend/src/Library.Api/* with controllers and Swagger in Development
- /Users/jason/Projects/mercer/backend/src/Library.Tests/*
- Test project references API project

Acceptance Criteria
- Projects build successfully.
- API runs locally and Swagger UI is reachable.
- No configuration beyond controllers and Swagger in Development.
- Directory structure matches the project plan.

Output
- Print the exact commands executed and their status.
- Print the final URLs verified for Swagger.
- If idempotent skips occurred, log which steps were skipped and why.
```

Probably not the prompt a human would have create from this task:
> - [B1] Initialize solution and projects
  >     - Create .NET solution, Web API project `Library.Api`, and test project `Library.Tests`.
  >     - Add basic `Program.cs` with controllers and Swagger enabled in Development.
  >     - DoD: Projects build and run; Swagger UI reachable.

And a nice thing about having acceptance criteria in the prompt was that I could do it myself to see if the agent was successful. Like in this case, running the project and viewing the swagger UI. And once I was satisfied that the prompts were working well I moved from generating one prompt at a time to many.

> Create prompts for Track C

While I mostly ran each task sequentially, it's easy to see how agents could complete many of these tasks in parallel.

So I would run the tasks one at a time and while they were running I would take a look at the generated code, or run the app and play around with Swagger. I also did a couple tutorials on ASP.NET and Entity Framework.

## Learnings

### Scope
Estimation is something humans are really bad at. Even knowing that I should try to limit the scope, I still ended up biting off more than I could chew. And while all the backend tasks were accomplished (sans rate limiting), only about 3/4 of the front-end ones were. This resulted in a fair bit of stress trying to make sure the base requirements of the challenge were complete before submitting the project. I should have focused on that from the beginning and only added additional scope after that was complete.

I should have been particularly sensitive to this since my backend experience is primarily with Go and Node. Getting dotnet up and running and everything working took time that I did not account for.

### Task Prompts
I really liked the format that was generated for the task prompts. I'm going to refine this a bit and use it going forward in all my work.

### .NET & EF
All the core concepts make sense and map directly to equivalents in other frameworks. But I do have things to learn here (standards, best practices, patterns). My personal philosophy is that AI is most powerful in the hands of experts and I would dedicate myself to becoming an expert in this stack if I were to get the job.

## Challenge Completion

### Backend (C# /.NET Core)
- [x] Scaffold a new ASP.NET Core Web API project.
- [x] Define a Book model with fields:
- [x] Implement CRUD endpoints for Book:
- [x] Add an endpoint GET /api/books/stats that returns the number of books per genre.
- [x] Use Entity Framework Core with migrations to persist data in SQLite or SQL Server.
- [x] Configure Swagger/OpenAPI for API exploration.

### Frontend (React)
- [x] Bootstrap a React app (Create React App, Vite, or equivalent).
- [x] Implement pages/components:
  - [x] Book List: fetch and display all books in a table or cards.
  - [x] Book Form: create and edit books (reuse component for both).
  - [x] Stats View: fetch /api/books/stats and render a bar or pie chart (use a library
    of your choice).

- [x] Organize code into clear “presentation” vs. “API” layers (e.g. custom hooks or services
  for data fetching).

- [x] Handle loading, success, and error states for all API interactions.
- [x] Ensure forms have basic validation (required fields, rating range).

### Deliverables

- [x] A GitHub repository (or ZIP) containing:
  - [x] backend/ folder with the .NET solution, EF migrations, and instructions.
  - [x] frontend/ folder with the React source and setup instructions.
  - [x] A root README outlining:
    - [x] Prerequisites and setup steps (dotnet ef database update, npm
      install, etc.)
    - [x] How to run both apps concurrently.
    - [x] Any design decisions or trade-offs.
- [x] Swagger/OpenAPI collection for API testing.

### Bonus

- [x] Secure endpoints with JWT authentication and allow multiple users to manage their own
  books.
- [ ] Containerize backend and frontend with Docker and provide a docker-compose.yml.
- [x] Write unit tests for API controllers
- [ ] Write unit tests for React components.
- [ ] Deploy to a cloud provider (e.g., Azure App Service, AWS Elastic Beanstalk).
- [x] Implement sorting and filtering on the Book List (by genre, rating, or published date).

### Further Exploration

- [x] Adding a “favorites” feature backed by a many-to-many relationship.
- [x] Integrating real-time updates with SignalR so multiple clients see changes live.
- [x] Improving analytics (e.g., average rating over time, most-read genres).
- [ ] Setting up a CI/CD pipeline to automate tests and deployment on Git pushes.
