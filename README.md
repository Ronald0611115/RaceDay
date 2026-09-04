# RaceDay
 # RaceDay — Event Management System

## 1. Project Overview

RaceDay is a full-stack web-based event management system designed for South African road running, walking, and cycling events.

The purpose of the system is to digitise the management of sporting events that may traditionally rely on paper records and spreadsheets. RaceDay provides a structured database and planned RESTful API that can later be used by a web application.

The system supports two main types of users:

* **Organiser** — responsible for creating and managing events, managing event categories, viewing participant enrolments, and recording race results.
* **Participant** — responsible for creating an account, browsing available events, entering an event by selecting a category, viewing enrolments, and tracking race history.

This repository represents **Part 1** of the RaceDay project. Part 1 focuses on system planning, database design, API planning, database implementation, testing, and project documentation.

---

# 2. Project Objectives

The main objectives of RaceDay are to:

* Digitise the management of running, walking, and cycling events.
* Provide structured storage of event and participant information.
* Allow participants to enrol in specific event categories.
* Allow organisers to manage events and participant results.
* Reduce duplicated and inconsistent data.
* Provide a database structure that can support the RESTful API in Part 2.
* Provide a foundation for the MVC web application in Part 3.

---

# 3. User Roles

## Organiser

An Organiser is responsible for managing sporting events.

Organisers can:

* Create events.
* Update events.
* Delete events.
* Create and manage event categories.
* View participant enrolments.
* Record participant results.
* Manage information relating to their events.

The user's role is stored in the `User` table using the `Role` attribute.

Example:

```text
Role = Organiser
```

## Participant

A Participant is a registered user who can participate in sporting events.

Participants can:

* Register an account.
* Log in.
* Browse upcoming events.
* View event categories.
* Enrol in a specific category.
* View their enrolments.
* View their race history and results.

Example:

```text
Role = Participant
```

A single `User` table is used for both roles because both types of users share common information such as their name, email address, password hash, and account creation date.

---

# 4. Database Design

The RaceDay database consists of the following main entities:

1. **User**
2. **Event**
3. **Category**
4. **EventRoute**
5. **Enrolment**
6. **Result**

These entities represent the core information required to manage sporting events and participant entries.

## Entity Relationships

The major relationships are:

```text
User
 │
 │ 1
 │
 └──────────< Event
               │
               │ 1
               │
               └──────────< Category
                              │
                              │ 1
                              │
                              └──────────< EventRoute


User
 │
 │ 1
 │
 └──────────< Enrolment >──────────1 Category
                                      │
                                      │
                                      └────── Event


Enrolment
 │
 │ 1
 │
 └──────────0..1 Result
```

### User → Event

One Organiser can manage multiple events, while each event is associated with one Organiser.

```text
User 1 ───────────< Event
```

### Event → Category

An event can contain multiple categories.

For example:

```text
Comrades Marathon
├── 21 km
├── 42 km
└── Ultra Marathon
```

Therefore:

```text
Event 1 ───────────< Category
```

### Category → EventRoute

A category can have a route associated with it. The route information stores details such as the distance and route description.

### User → Enrolment

A Participant can have multiple enrolments.

### Category → Enrolment

A category can have multiple participants enrolled in it.

The `Enrolment` entity therefore represents the participant's entry into a specific category.

This means the relationship can be followed as:

```text
Participant
     ↓
Enrolment
     ↓
Category
     ↓
Event
```

### Enrolment → Result

A result belongs to a specific enrolment. This avoids unnecessarily duplicating the Participant and Event relationships.

An enrolment may have no result before the race has taken place, or one result after the result has been recorded.

```text
Enrolment 1 ─────────── 0..1 Result
```

---

# 5. Important Design Decisions

## Single User Entity

RaceDay uses one `User` table rather than separate `Organiser` and `Participant` tables.

The `Role` attribute identifies the type of user.

This approach avoids duplicating common attributes such as:

* Name
* Email
* PasswordHash
* CreatedAt

It also allows the database to support a future situation where a user may need more than one responsibility without maintaining separate authentication records.

---

## Participant Enrols in a Category

Participants do not enrol directly in an event.

Instead, a participant selects a category belonging to an event.

For example:

```text
Event:
Polokwane Road Race

Category:
10 km
```

The `Enrolment` table therefore contains:

```text
ParticipantId
CategoryId
```

The event can then be identified through the category.

This avoids storing both `EventId` and `CategoryId` in `Enrolment`, which could potentially create inconsistent data.

---

## Result Belongs to Enrolment

The `Result` entity is connected to `Enrolment`.

This is because a result describes what happened to a particular participant's entry.

The relationship is:

```text
Participant
     ↓
Enrolment
     ↓
Result
```

This avoids duplicating the Participant and Event relationships inside the `Result` table.

---

## Weather Information

Weather is not currently stored as a RaceDay database entity.

Live weather information can be obtained from an external weather API using the event's location and date.

This means RaceDay consumes external weather information rather than owning the weather data as part of its core database.

---

# 6. Database Entities

## User

Stores registered RaceDay users.

Main attributes include:

* `UserId` — Primary Key
* `Name`
* `Email`
* `PasswordHash`
* `Role`
* `CreatedAt`

---

## Event

Stores information about sporting events.

Main attributes include:

* `EventId` — Primary Key
* `EventName`
* `EventDate`
* `Location`
* `Description`
* `OrganiserId` — Foreign Key

---

## Category

Stores categories available within an event.

Main attributes include:

* `CategoryId` — Primary Key
* `CategoryName`
* `EntryFee`
* `MaxParticipants`
* `EventId` — Foreign Key

---

## EventRoute

Stores route information associated with event categories.

Main attributes include:

* `RouteId` — Primary Key
* `DistanceKm`
* `RouteDescription`
* `MapUrl`
* `CategoryId` — Foreign Key

---

## Enrolment

Stores participant entries into event categories.

Main attributes include:

* `EnrolmentId` — Primary Key
* `ParticipantId` — Foreign Key
* `CategoryId` — Foreign Key
* `EnrolmentDate`
* `Status`

---

## Result

Stores race results for participant enrolments.

Main attributes include:

* `ResultId` — Primary Key
* `EnrolmentId` — Foreign Key
* `FinishTime`
* `Position`
* `RecordedAt`

---

# 7. Database Constraints

The database uses constraints to protect data integrity.

Examples include:

* Primary key constraints.
* Foreign key constraints.
* `NOT NULL` constraints.
* `UNIQUE` constraints.
* `DEFAULT` constraints.
* `CHECK` constraints.

Business rules tested include:

* Invalid user roles are rejected.
* Negative entry fees are rejected.
* Zero maximum participants are rejected.
* Invalid enrolment statuses are rejected.
* Duplicate participant/category enrolments are rejected.
* Duplicate category routes are rejected.
* Events cannot reference non-existent organisers.
* Categories cannot reference non-existent events.
* Enrolments cannot reference non-existent participants.

These tests confirm that SQL Server is enforcing the required database rules.

---

# 8. API Endpoint Plan

The API planned for Part 2 will provide RESTful endpoints for authentication, users, events, categories, enrolments, and results.

The detailed endpoint plan is available in:

```text
/docs/API-Endpoint-Plan.md
```

The planned API areas include:

| Area           | Purpose                            |
| -------------- | ---------------------------------- |
| Authentication | Registration and login             |
| User Profile   | Managing user profile information  |
| Events         | Viewing and managing events        |
| Categories     | Managing event categories          |
| Enrolments     | Managing participant entries       |
| Results        | Recording and viewing race results |

The Part 2 API will be implemented according to this endpoint plan.

---

# 9. Repository Structure

The project is organised as follows:

```text
RaceDay/
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
├── docs/
│   ├── RaceDay-ERD.png
│   ├── API-Endpoint-Plan.md
│   └── RaceDay-Database.sql
│
├── README.md
│
└── ...
```

The `/docs` folder contains the main Part 1 documentation and database artefacts.

---

# 10. SQL Database Script

The SQL database script is located at:

```text
/docs/RaceDay-Database.sql
```

The script creates the RaceDay database tables, constraints, relationships, and sample data.

The database was tested in **Microsoft SQL Server Management Studio (SSMS)**.

The seed data includes:

* At least 2 Organisers.
* At least 2 Participants.
* 3 Events.
* Categories for the events.
* Sample enrolments.
* Sample route information.
* Sample result information where applicable.

The script is intended to run successfully on a clean SQL Server environment.

---

# 11. Database Testing

Database constraint and foreign-key testing was performed to verify that invalid data is rejected.

Examples of tests performed include:

### Test 1 — Invalid User Role

An invalid role was inserted into the `User` table.

**Result:** Rejected successfully.

### Test 2 — Negative Entry Fee

A category with a negative entry fee was attempted.

**Result:** Rejected successfully.

### Test 3 — Invalid Maximum Participants

A category with zero maximum participants was attempted.

**Result:** Rejected successfully.

### Test 4 — Invalid Enrolment Status

An invalid enrolment status was attempted.

**Result:** Rejected successfully.

### Test 5 — Duplicate Participant and Category

The same participant was prevented from enrolling in the same category more than once.

**Result:** Rejected successfully.

### Test 6 — Duplicate Category Route

A duplicate route assignment was attempted.

**Result:** Rejected successfully.

### Foreign Key Testing

Foreign-key constraints were also tested to ensure that records cannot reference non-existent parent records.

For example:

```text
Event → User
Category → Event
Enrolment → User
Enrolment → Category
Result → Enrolment
```

These tests confirmed that referential integrity is being enforced.

---

# 12. CI/CD

GitHub Actions is used to perform automated repository validation.

The workflow is located at:

```text
.github/workflows/ci.yml
```

The workflow checks that the required project structure and documentation are present.

A successful workflow execution is represented by the green GitHub Actions build.

## CI/CD Screenshot

Add your successful GitHub Actions screenshot below:

```text
[INSERT SUCCESSFUL GREEN GITHUB ACTIONS SCREENSHOT HERE]
```

---

# 13. GitHub Repository

The complete project repository is hosted on GitHub.

**Repository:**

```text
[INSERT YOUR GITHUB REPOSITORY LINK HERE]
```

The repository contains the required documentation, database script, ERD, API endpoint plan, and CI/CD configuration.

---

# 14. Video Presentation

The Part 1 video presentation explains:

* The purpose of the RaceDay system.
* The two user roles.
* ERD entities and relationships.
* Primary keys and foreign keys.
* Cardinality decisions.
* API endpoint planning.
* Database design decisions.
* SQL constraints.
* The execution of the SQL script in SSMS.
* Database testing.

**YouTube Video:**

```text
[INSERT YOUR UNLISTED YOUTUBE VIDEO LINK HERE]
```

---

# 15. Technologies Used

The RaceDay project is planned around the following technologies:

* **Microsoft SQL Server** — relational database.
* **SQL Server Management Studio (SSMS)** — database development and testing.
* **C# / .NET** — planned RESTful API for Part 2.
* **ASP.NET Core** — planned API framework.
* **GitHub** — source-code repository and version control.
* **GitHub Actions** — CI/CD automation.
* **MVC / ASP.NET Core MVC** — planned web application for Part 3.
* **Azure Blob Storage** — planned storage integration for Part 3.
* **Docker** — planned containerisation for Part 3.

---

# 16. Part 1 Completion Checklist

| Requirement                      | Status                |
| -------------------------------- | --------------------- |
| ERD created                      |                      |
| Minimum 6 entities               |                      |
| Primary keys defined             |                      |
| Foreign keys defined             |                      |
| Relationships defined            |                      |
| Cardinality defined              |                      |
| API endpoint plan created        |                      |
| Authentication endpoints planned |                      |
| User profile endpoints planned   |                      |
| Event endpoints planned          |                      |
| Category endpoints planned       |                      |
| Enrolment endpoints planned      |                      |
| Result endpoints planned         |                      |
| SQL CREATE TABLE statements      |                      |
| Constraints implemented          |                      |
| Seed data inserted               |                      |
| Constraint testing completed     |                      |
| Foreign-key testing completed    |                    |
| GitHub repository                |  Add repository link |
| 6+ meaningful commits            |  Verify              |
| GitHub Actions workflow          |  Verify              |
| Successful CI/CD screenshot      |  Add screenshot      |
| Unlisted YouTube video           |  Add video link      |

---

# 17. Conclusion

RaceDay provides a structured foundation for managing South African running, walking, and cycling events.

The Part 1 database design establishes clear relationships between users, events, categories, routes, enrolments, and results. The use of primary keys, foreign keys, unique constraints, default values, and validation rules helps maintain data integrity.

The API endpoint plan provides the contract that will guide the development of the RESTful API in Part 2. The database and API have therefore been designed with the later MVC web application in Part 3 in mind.

The project will be developed incrementally so that the implementation remains consistent with the original system design.

