# RaceDay API Endpoint Plan

## 1. Overview

RaceDay is a REST API for managing South African running, walking and cycling events. It uses ASP.NET Core, Entity Framework Core and Microsoft SQL Server.

Base URL during local development: `https://localhost:<port>/api`

Authentication: JWT bearer tokens are required for protected endpoints.

## 2. Authentication

| Method | Endpoint | Access | Purpose |
|---|---|---|---|
| POST | /api/auth/register | Public | Register a participant account |
| POST | /api/auth/login | Public | Authenticate and receive a JWT |

Registration assigns the Participant role. Organiser access must be provisioned through an authorised process.

## 3. Events

| Method | Endpoint | Access | Purpose |
|---|---|---|---|
| GET | /api/events | Public | List events |
| GET | /api/events/{id} | Public | View an event |
| POST | /api/events | Organiser | Create an event |
| PUT | /api/events/{id} | Event owner | Update an event |
| DELETE | /api/events/{id} | Event owner | Cancel an event |

Organiser operations must verify ownership of the event. Confirm the exact route names against EventsController.cs before final submission.

## 4. Categories

| Method | Endpoint | Access | Purpose |
|---|---|---|---|
| GET | /api/categories/event/{eventId} | Public | List categories for an event |
| GET | /api/categories/{id} | Public | View a category |
| POST | /api/categories/event/{eventId} | Organiser who owns event | Create a category |
| PUT | /api/categories/{id} | Organiser who owns event | Update a category |
| DELETE | /api/categories/{id} | Organiser who owns event | Delete a category if no dependent enrolments or route exist |

Category data includes CategoryName, EntryFee and MaxParticipants.

## 5. Enrolments

| Method | Endpoint | Access | Purpose |
|---|---|---|---|
| GET | /api/enrolments/mine | Participant | View own enrolments |
| POST | /api/enrolments/category/{categoryId} | Participant | Enrol in a category |
| GET | /api/enrolments/event/{eventId} | Organiser who owns event | View enrolments for an event |

The participant identity is taken from the authenticated JWT, not from a client-supplied participant ID. The API checks for duplicate enrolments and category capacity.

## 6. Results

| Method | Endpoint | Access | Purpose |
|---|---|---|---|
| POST | /api/results/enrolment/{enrolmentId} | Organiser who owns event | Record a race result |
| GET | /api/results/mine | Participant | View own results |
| GET | /api/results/enrolment/{enrolmentId} | Enrolled participant or event owner | View a result |

Results contain FinishTime, Position and RecordedAt. A result is linked to an enrolment.

## 7. Database entities

- User: account details and role.
- Event: event information and organiser.
- Category: entry fee and participant limit for an event.
- EventRoute: route information for a category.
- Enrolment: participant registration in a category.
- Result: finishing time and position for an enrolment.

Weather information is obtained from an external service rather than stored as a dedicated entity in this database design.

## 8. Security and validation

- JWT authentication protects restricted operations.
- Role-based authorisation separates participant and organiser actions.
- Organisers may manage only their own events and related resources.
- Request models validate category details and result positions.
- The API checks for missing resources and conflicting operations.
- Passwords must be stored as hashes, never as plaintext.

## 9. Testing and continuous integration

The automated test project is RaceDay.API.Tests. GitHub Actions restores dependencies, builds the API and test project, and runs the automated tests on pushes and pull requests targeting main.
