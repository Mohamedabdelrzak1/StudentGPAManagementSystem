# Student GPA Management System - Web API

A production-ready ASP.NET Core Web API built with **Clean Architecture** and **SQL Server** for managing student registration, course catalogs, semester enrollments, grading scales, and accurate **Semester GPA** & **Cumulative GPA** calculations.

---

## Table of Contents
- [1. Architecture Overview](#1-architecture-overview)
- [2. Technology Stack](#2-technology-stack)
- [3. Business Rules & Calculations](#3-business-rules--calculations)
- [4. Verification Example Results](#4-verification-example-results)
- [5. API Endpoints](#5-api-endpoints)
- [6. Database & Seed Data](#6-database--seed-data)
- [7. Setup & Running Instructions](#7-setup--running-instructions)
- [8. Running Unit Tests](#8-running-unit-tests)
- [9. Assumptions & Design Decisions](#9-assumptions--design-decisions)

---

## 1. Architecture Overview

The solution strictly adheres to **Clean Architecture** principles and separation of concerns:

```
StudentGPAManagementSystem/
├── Core/
│   ├── Domain/                 # Enterprise Business Rules, Entities (POCOs) & Enums
│   │   ├── Models/             # Student, Course, Semester, Enrollment, GradingScale
│   │   ├── Enums/              # EnrollmentStatus
│   │   ├── Contracts/          # IGenericRepository, IUnitOfWork
│   │   └── Exceptions/         # Domain-level exception definitions
│   ├── ServiceAbstraction/     # Service interfaces (IStudentService, IGpaCalculatorService, etc.)
│   └── Service/                # Application logic, Use Cases & AutoMapper Profiles
│       └── MappingProfiles/    # StudentProfile, CourseProfile, SemesterProfile, EnrollmentProfile, GradingScaleProfile
├── Infrastructure/
│   ├── Persistence/            # EF Core DbContext, Fluent API Configurations, Migrations, Repositories
│   │   ├── Data/Configurations # StudentConfiguration, CourseConfiguration, EnrollmentConfiguration, etc.
│   │   ├── Migrations/         # EF Core Migration files
│   │   └── Repositorys/        # GenericRepository, UnitOfWork
│   └── Presentation/           # Controllers inheriting from BaseApiController
├── Shared/                     # Common Response Wrapper (Response<T>), Error Models & Organized DTOs
│   └── Dto/                    # DTOs segregated per entity/feature
│       ├── Students/           # StudentDto, CreateStudentDto, UpdateStudentDto
│       ├── Courses/            # CourseDto, CreateCourseDto, UpdateCourseDto
│       ├── Semesters/          # SemesterDto, CreateSemesterDto, UpdateSemesterDto
│       ├── Enrollments/        # EnrollmentDto, CreateEnrollmentDto, UpdateEnrollmentDto
│       ├── GradingScales/      # GradingScaleDto
│       └── Transcripts/        # TranscriptDto, SemesterTranscriptDto, CourseTranscriptDto, GpaResultDto, etc.
├── StudentGPAManagementSystem.API/ # Web API Host, Middleware, Swagger, Dependency Injection
└── Tests/
    └── StudentGPAManagementSystem.Tests/ # xUnit test suite (21 automated tests)
```

---

## 2. Technology Stack

- **Framework:** .NET 10.0 (C# 13)
- **Database:** Microsoft SQL Server
- **ORM:** Entity Framework Core 10.0 (Code-First with Migrations & Fluent API)
- **Object Mapping:** AutoMapper 17.0 with Mapping Profiles per entity
- **Architecture:** Clean Architecture + Repository Pattern + Unit of Work
- **API Documentation:** Swagger / OpenAPI via Swashbuckle
- **Testing:** xUnit, EF Core In-Memory Database

---

## 3. Business Rules & Calculations

### 3.1 Course Management
- Each course has between **1 and 3 credit hours** (enforced by Fluent API check constraint `CK_Courses_CreditHours` and Service validation).
- Course Codes must be unique (`IX_Courses_Code`).

### 3.2 18-Hour Semester Enrollment Limit
- Maximum registered credit hours per student per semester is **18 hours**.
- **Important:** All registered courses—**including Withdrawn (`W`) and Incomplete (`I`)**—count toward the 18-hour limit.
- Prevent duplicate enrollment of the same course for the same student in the same semester.
- Enrollment snapshots course credit hours at registration time.

### 3.3 Dynamic Grading Scale (Database-Driven)
The grading scale is stored in SQL Server table `GradingScales` and looked up dynamically:

| FromGrade | ToGrade | Points | Grade | Rule |
|----------:|--------:|-------:|:------|:-----|
| 96.00 | 101.00 | 4.0 | A+ | $96 \le \text{Score} < 101$ |
| 92.00 | 96.00 | 3.7 | A | $92 \le \text{Score} < 96$ |
| 88.00 | 92.00 | 3.4 | A- | $88 \le \text{Score} < 92$ |
| 84.00 | 88.00 | 3.2 | B+ | $84 \le \text{Score} < 88$ |
| 80.00 | 84.00 | 3.0 | B | $80 \le \text{Score} < 84$ |
| 76.00 | 80.00 | 2.8 | B- | $76 \le \text{Score} < 80$ |
| 72.00 | 76.00 | 2.6 | C+ | $72 \le \text{Score} < 76$ |
| 68.00 | 72.00 | 2.4 | C | $68 \le \text{Score} < 72$ |
| 64.00 | 68.00 | 2.2 | C- | $64 \le \text{Score} < 68$ |
| 60.00 | 64.00 | 2.0 | D+ | $60 \le \text{Score} < 64$ |
| 55.00 | 60.00 | 1.5 | D | $55 \le \text{Score} < 60$ |
| 50.00 | 55.00 | 1.0 | D- | $50 \le \text{Score} < 55$ |
| 0.00 | 50.00 | 0.0 | F | $0 \le \text{Score} < 50$ |

- Range rule: Lower bound is **inclusive**, upper bound is **exclusive** (`FromGrade <= Score < ToGrade`).
  - Example: `96` yields `A+`, whereas `95.99` yields `A`.

### 3.4 Special Academic Cases
| Status | Displayed Grade | Included in GPA? | Points Used | Credit Hours in Denominator? |
|:---|:---:|:---:|:---:|:---:|
| **Normal** | A+ to F | Yes | From Grading Scale | Yes |
| **Absent (ABS)** | ABS | Yes | 0.0 | Yes |
| **Withdrawn (W)** | W | No | Excluded | Excluded |
| **Incomplete (I)** | I | No | Excluded | Excluded |

- **Mutual Exclusivity:** A course must have **either** a numeric score **or** a special academic status, never both.

### 3.5 GPA Formulas
- **Course Quality Points:** $\text{Grade Points} \times \text{Course Credit Hours}$
- **Semester GPA:** $\frac{\sum \text{Eligible Quality Points in Semester}}{\sum \text{Eligible Credit Hours in Semester}}$
- **Cumulative GPA:** $\frac{\sum \text{Eligible Quality Points Across All Semesters}}{\sum \text{Eligible Credit Hours Across All Semesters}}$
- **Rounding:** Rounded to **two decimal places** using standard arithmetic midpoint rounding (`MidpointRounding.AwayFromZero`), keeping full decimal precision during intermediate steps.
- **Division by Zero Protection:** When eligible hours equal 0, returns `null` with `"N/A"` display rather than dividing by zero.

---

## 4. Verification Example Results

The system has been verified against the exact example from the technical assessment specification:

### Student: John Doe (STU001)

#### Semester 1 (Fall 2024):
| Course | Credit Hours | Status / Score | Grade | Points | Quality Points | Included in GPA? |
|:---|:---:|:---:|:---:|:---:|:---:|:---:|
| CS101 | 3 | Normal (90) | A- | 3.4 | 10.2 | Yes |
| MATH101 | 2 | Normal (85) | B+ | 3.2 | 6.4 | Yes |
| ENG101 | 3 | Absent | ABS | 0.0 | 0.0 | Yes |
| PHY101 | 2 | Withdrawn | W | - | - | No |
| IT101 | 3 | Incomplete | I | - | - | No |
| **Total** | **13 Registered** | - | - | - | **16.6 QP** / **8 Eligible Hrs** | **GPA = 2.08** |

$$\text{Semester 1 GPA} = \frac{10.2 + 6.4 + 0.0}{3 + 2 + 3} = \frac{16.6}{8} = 2.075 \xrightarrow{\text{round}} \mathbf{2.08}$$

#### Semester 2 (Spring 2025):
| Course | Credit Hours | Status / Score | Grade | Points | Quality Points | Included in GPA? |
|:---|:---:|:---:|:---:|:---:|:---:|:---:|
| CS102 | 3 | Normal (96) | A+ | 4.0 | 12.0 | Yes |
| MATH102 | 2 | Normal (70) | C | 2.4 | 4.8 | Yes |
| **Total** | **5 Registered** | - | - | - | **16.8 QP** / **5 Eligible Hrs** | **GPA = 3.36** |

$$\text{Semester 2 GPA} = \frac{12.0 + 4.8}{3 + 2} = \frac{16.8}{5} = \mathbf{3.36}$$

#### Cumulative GPA:
$$\text{Cumulative GPA} = \frac{16.6 + 16.8}{8 + 5} = \frac{33.4}{13} = 2.56923... \xrightarrow{\text{round}} \mathbf{2.57}$$

*(Note: Weighted cumulative GPA is 2.57, whereas the incorrect unweighted average $(2.08 + 3.36)/2 = 2.72$ is avoided).*

---

## 5. API Endpoints

All endpoints return a standardized response wrapper:
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Operation successful.",
  "errors": [],
  "payload": { ... }
}
```

### Students (`/api/students`)
| Method | Endpoint | Description |
|:---|:---|:---|
| `GET` | `/api/students` | List all students |
| `GET` | `/api/students/{id}` | Get student by ID |
| `GET` | `/api/students/code/{code}` | Get student by unique code |
| `POST` | `/api/students` | Create new student |
| `PUT` | `/api/students/{id}` | Update student details |
| `DELETE` | `/api/students/{id}` | Delete student |
| `GET` | `/api/students/{id}/gpa` | **Calculate Semester & Cumulative GPA** |
| `GET` | `/api/students/{id}/transcript` | **Get full academic transcript** |

### Courses (`/api/courses`)
| Method | Endpoint | Description |
|:---|:---|:---|
| `GET` | `/api/courses` | List all courses |
| `GET` | `/api/courses/{id}` | Get course by ID |
| `GET` | `/api/courses/code/{code}` | Get course by code |
| `POST` | `/api/courses` | Create course (1-3 credit hours) |
| `PUT` | `/api/courses/{id}` | Update course |
| `DELETE` | `/api/courses/{id}` | Delete course |

### Semesters (`/api/semesters`)
| Method | Endpoint | Description |
|:---|:---|:---|
| `GET` | `/api/semesters` | List all semesters |
| `GET` | `/api/semesters/{id}` | Get semester by ID |
| `POST` | `/api/semesters` | Create semester |
| `PUT` | `/api/semesters/{id}` | Update semester |
| `DELETE` | `/api/semesters/{id}` | Delete semester |

### Enrollments (`/api/enrollments`)
| Method | Endpoint | Description |
|:---|:---|:---|
| `GET` | `/api/enrollments` | List enrollments (supports `?studentId=&semesterId=`) |
| `GET` | `/api/enrollments/{id}` | Get enrollment by ID |
| `POST` | `/api/enrollments` | Register course for student in semester |
| `PUT` | `/api/enrollments/{id}` | Update score or academic status |
| `DELETE` | `/api/enrollments/{id}` | Drop/delete enrollment |

### Grading Scales (`/api/gradingscales`)
| Method | Endpoint | Description |
|:---|:---|:---|
| `GET` | `/api/gradingscales` | List all grading scale ranges |
| `GET` | `/api/gradingscales/resolve?score={score}` | Resolve score to letter grade and points |

---

## 6. Database & Seed Data

### Database Schema
The database is structured with relational integrity and check constraints:
- `Students` (Id, Code, Name, RowVersion)
- `Courses` (Id, Code, Name, CreditHours [1-3])
- `Semesters` (Id, Name, StartDate, EndDate)
- `GradingScales` (Id, FromGrade, ToGrade, Points, Grade)
- `Enrollments` (Id, StudentId, SemesterId, CourseId, CreditHours, Status, Score, GradingScaleId)

### Seed Data
Upon initial startup (`await dbContext.Database.MigrateAsync()`), the application seeds:
1. **13 Grading Scales:** Exact scale from A+ (96-101) down to F (0-50).
2. **2 Semesters:** `Fall 2024` and `Spring 2025`.
3. **7 Courses:** CS101, MATH101, ENG101, PHY101, IT101, CS102, MATH102.
4. **2 Students with Enrollments:**
   - **John Doe (STU001):** Exact assessment verification dataset.
   - **Sarah Smith (STU002):** Second student with multi-semester enrollments for additional testing.

---

## 7. Setup & Running Instructions

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server/) (LocalDB, SQL Express, or standard SQL Server)

### Step 1: Configure Connection String
Open `StudentGPAManagementSystem.API/appsettings.json` and set your SQL Server connection:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=StudentGpaDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Step 2: Apply Migrations & Run
From the root solution directory:
```bash
# Run the API application (migrations and seed data apply automatically on startup)
dotnet run --project StudentGPAManagementSystem.API/StudentGPAManagementSystem.API.csproj
```

### Step 3: Access Swagger UI
Open your browser and navigate to:
```
http://localhost:5000/swagger
```
or
```
https://localhost:7000/swagger
```

---

## 8. Running Unit Tests

The test project `StudentGPAManagementSystem.Tests` contains **21 unit tests** validating:
1. **Assessment Verification Match:** Exact match of the assessment verification data.
2. **No Eligible Hours Case:** Handling when student has only `W` or `I` courses (returns `N/A`, prevents division by zero).
3. **Full Transcript Generation:** Validates course breakdown, special statuses (`ABS`, `W`, `I`), and grade points.
4. **AutoMapper Configuration Integrity:** Validates that AutoMapper profiles are registered and resolved cleanly.
5. **18-Hour Semester Limit:** Enforcing limit including non-GPA courses.
6. **Duplicate Enrollment Prevention:** Preventing registering the same course twice in one semester.
7. **Grading Scale Boundaries:** 11 boundary tests ($100$, $96 \to A+$, $95.99 \to A$, $90 \to A-$, $85 \to B+$, $70 \to C$, $50 \to D-$, $49.99 \to F$, $0 \to F$).
8. **Mutual Exclusivity:** Rejecting combinations of numeric scores with special statuses.
9. **Credit Hours Validation:** Rejecting invalid hours ($0$, $4$, $-1$).

Run all tests via CLI:
```bash
dotnet test
```

Expected result:
```
Passed! - Failed: 0, Passed: 21, Skipped: 0, Total: 21
```

---

## 9. Assumptions & Design Decisions

1. **Course Retakes:** Per assessment guidance, each course is considered unique per semester; course retake grade replacement rules were omitted as specified.
2. **Snapshot Credit Hours:** When a student enrolls in a course, the course's `CreditHours` is saved as a snapshot on the `Enrollment` record. This preserves historical academic accuracy even if a course catalog's credit hours are changed later.
3. **Optimistic Concurrency:** A `RowVersion` timestamp token is placed on `Student` to safeguard concurrent enrollment operations.
4. **Deletion Safety:** Foreign keys use `DeleteBehavior.Restrict` to protect against accidental cascade deletion of academic records.
5. **Standardized Response:** Every API endpoint returns `Response<T>` containing HTTP status code, success flag, descriptive message, and payload.
