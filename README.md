# Career Desk

An English-language job search workspace for organizing applications, resumes, and interview preparation.

## Stack

- C# with ASP.NET Core 8 minimal APIs
- HTML, CSS, and JavaScript
- JSON file persistence for job listings and application records
- Browser local storage for resume drafts

## Features

- Edit and preview a resume, save it in the current browser, and print or export it as PDF.
- Save, search, and filter job listings from different sources.
- Add real openings manually with their source, link, location, and deadline.
- Track application status, notes, and next follow-up dates.
- Prepare for interviews by application: save interview logistics, preparation notes, draft answers to role-relevant prompts, and questions for the interviewer.
- Compare a job description with your resume and see matched keywords, gaps, and tailoring suggestions.
- Responsive layout for desktop and mobile screens.

Sample listings use fictional organizations and cover library services, student programs, and research support. They are labeled as samples and are not real openings.

## Run locally

Install the .NET 8 SDK, then run:

```sh
dotnet run --urls http://localhost:5178
```

Open `http://localhost:5178` in a browser. The app creates `App_Data/career-data.json` to keep job and application records on the local machine. Resume content and interview preparation notes stay in that browser's local storage. Job listings are added manually.

## API overview

- `GET /api/jobs` and `POST /api/jobs`
- `PATCH /api/jobs/{id}` to change the saved state
- `GET /api/applications` and `POST /api/applications`
- `PATCH /api/applications/{id}` to update status, notes, or follow-up date
- `DELETE /api/applications/{id}`

This is a local portfolio project and does not include sign-in or multi-user access controls.
