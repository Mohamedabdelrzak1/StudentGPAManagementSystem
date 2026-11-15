# Student GPA Management System — Full Technical & Frontend Integration Guide

> **Production-ready ASP.NET Core Web API** built with **Clean Architecture**, **SQL Server**, and **Entity Framework Core**.  
> This documentation provides complete API contracts, business rules, and step-by-step UI implementation specifications for building the frontend using **React** or **Next.js**.

---

## Table of Contents
1. [Backend Base Configuration](#1-backend-base-configuration)
2. [Standard API Response Contract](#2-standard-api-response-contract)
3. [Grading Scale & GPA Calculation Rules](#3-grading-scale--gpa-calculation-rules)
4. [Verification Example Dataset](#4-verification-example-dataset)
5. [Complete API Endpoints Reference](#5-complete-api-endpoints-reference)
   - [Students API](#51-students-api)
   - [Courses API](#52-courses-api)
   - [Semesters API](#53-semesters-api)
   - [Enrollments API](#54-enrollments-api)
   - [Grading Scales API](#55-grading-scales-api)
   - [GPA & Transcript API](#56-gpa--transcript-api)
6. [Frontend Screen-by-Screen Implementation Guide (React / Next.js)](#6-frontend-screen-by-screen-implementation-guide-react--nextjs)
   - [Screen 1: Students Management (`/students`)](#screen-1-students-management-students)
   - [Screen 2: Courses Catalog (`/courses`)](#screen-2-courses-catalog-courses)
   - [Screen 3: Academic Semesters (`/semesters`)](#screen-3-academic-semesters-semesters)
   - [Screen 4: Course Registration & Enrollment (`/enrollments`)](#screen-4-course-registration--enrollment-enrollments)
   - [Screen 5: Academic Transcript & GPA Dashboard (`/students/[id]/transcript`)](#screen-5-academic-transcript--gpa-dashboard-studentsidtranscript)
7. [TypeScript Type Definitions (Ready to Copy-Paste)](#7-typescript-type-definitions-ready-to-copy-paste)
8. [Database Seeding (Egyptian Sample Dataset)](#8-database-seeding-egyptian-sample-dataset)
9. [Running the Application](#9-running-the-application)

---

## 1. Backend Base Configuration

- **Development Base URL (HTTP):** `http://localhost:5000`
- **Development Base URL (HTTPS):** `https://localhost:7000`
- **Swagger Documentation:** `http://localhost:5000/swagger`
- **CORS Policy:** Enabled with `AllowAll` (supports `http://localhost:3000`, `http://localhost:5173`, etc.)

---

## 2. Standard API Response Contract

Every endpoint in the system wraps its response in a uniform structure:

```typescript
export interface ApiResponse<T> {
  statusCode: number;      // e.g. 200, 201, 400, 404, 409
  succeeded: boolean;       // true on success, false on error
  message?: string;         // Descriptive message in English
  errors?: string[];        // Array of error messages (if any)
  payload: T;               // Response data payload
}
```

### Validation Error Response (HTTP 400):
```json
{
  "statusCode": 400,
  "errorMessage": "Validations Errors",
  "errors": [
    {
      "field": "CreditHours",
      "errors": ["Credit hours must be between 1 and 3."]
    }
  ]
}
```

---

## 3. Grading Scale & GPA Calculation Rules

### 3.1 Dynamic Database-Driven Grading Scale
The grading scale is stored in SQL Server and queried dynamically. Range rule: `FromGrade <= Score < ToGrade` (**inclusive lower bound, exclusive upper bound**):

| FromGrade | ToGrade | Points | Grade | UI Badge Color | Description |
|----------:|--------:|-------:|:------|:--------------|:------------|
| 96.00 | 101.00 | 4.0 | **A+** | Emerald Green | Excellent / High Distinction ($96 \le \text{Score} < 101$) |
| 92.00 | 96.00 | 3.7 | **A** | Green | Excellent ($92 \le \text{Score} < 96$) |
| 88.00 | 92.00 | 3.4 | **A-** | Light Green | Very Good ($88 \le \text{Score} < 92$) |
| 84.00 | 88.00 | 3.2 | **B+** | Blue | Very Good ($84 \le \text{Score} < 88$) |
| 80.00 | 84.00 | 3.0 | **B** | Sky Blue | Good ($80 \le \text{Score} < 84$) |
| 76.00 | 80.00 | 2.8 | **B-** | Cyan | Above Average ($76 \le \text{Score} < 80$) |
| 72.00 | 76.00 | 2.6 | **C+** | Yellow | Average ($72 \le \text{Score} < 76$) |
| 68.00 | 72.00 | 2.4 | **C** | Amber | Satisfactory ($68 \le \text{Score} < 72$) |
| 64.00 | 68.00 | 2.2 | **C-** | Orange | Pass ($64 \le \text{Score} < 68$) |
| 60.00 | 64.00 | 2.0 | **D+** | Deep Orange | Low Pass ($60 \le \text{Score} < 64$) |
| 55.00 | 60.00 | 1.5 | **D** | Red-Orange | Minimum Pass ($55 \le \text{Score} < 60$) |
| 50.00 | 55.00 | 1.0 | **D-** | Light Red | Marginal ($50 \le \text{Score} < 55$) |
| 0.00 | 50.00 | 0.0 | **F** | Red | Fail ($0 \le \text{Score} < 50$) |

### 3.2 Special Academic Statuses
| Status | Code | Displayed Grade | Included in GPA? | Points Used | Hours in Denominator? | UI Badge Style |
|:---|:---:|:---:|:---:|:---:|:---:|:---|
| **Normal** | `Normal` | Letter (A+ to F) | **Yes** | From scale | **Yes** | Colored by grade |
| **Absent** | `Absent` | **ABS** | **Yes** | `0.0` | **Yes** | Purple / Indigo badge |
| **Withdrawn** | `Withdrawn` | **W** | **No** | `null` | **No** | Gray / Slate badge |
| **Incomplete** | `Incomplete` | **I** | **No** | `null` | **No** | Amber / Warning badge |

### 3.3 Core Formulas & Business Logic
1. **Course Quality Points:**
   $$\text{Quality Points} = \text{Grade Points} \times \text{Course Credit Hours}$$
2. **Semester GPA:**
   $$\text{Semester GPA} = \frac{\sum \text{Eligible Quality Points in Semester}}{\sum \text{Eligible Credit Hours in Semester}}$$
3. **Cumulative GPA (Weighted Across All Semesters):**
   $$\text{Cumulative GPA} = \frac{\sum \text{Eligible Quality Points Across All Semesters}}{\sum \text{Eligible Credit Hours Across All Semesters}}$$
   > **Important:** Cumulative GPA is **weighted by credit hours**; it is **NOT** the arithmetic mean of semester GPAs!
4. **18-Hour Semester Limit:**
   - A student can enroll in a maximum of **18 credit hours per semester**.
   - **All courses, including `W` (Withdrawn) and `I` (Incomplete), count towards this 18-hour enrollment ceiling.**
5. **Mutual Exclusivity:**
   - A course registration must have **either** a numeric score ($0-100$) **or** a special status (`Absent`, `Withdrawn`, `Incomplete`), never both.
6. **Precision & Rounding:**
   - Keep full precision during calculation steps.
   - Display to **two decimal places** with arithmetic rounding (`MidpointRounding.AwayFromZero`).
   - If eligible credit hours equal `0`, display `"N/A"` (avoiding division by zero).

---

## 4. Verification Example Dataset

The seeded student **أحمد محمد علي (STU001)** reflects the exact technical assessment benchmark:

### Semester 1: Fall 2024
| Course | Name | Hours | Status / Score | Grade | Points | Quality Points | In GPA? |
|:---|:---|:---:|:---|:---:|:---:|:---:|:---:|
| `CS101` | Intro to Computer Science | 3 | Normal (90) | A- | 3.4 | 10.2 | Yes |
| `MATH101` | Calculus I | 2 | Normal (85) | B+ | 3.2 | 6.4 | Yes |
| `ENG101` | English Composition | 3 | Absent | ABS | 0.0 | 0.0 | Yes |
| `PHY101` | General Physics I | 2 | Withdrawn | W | — | — | No |
| `IT101` | IT Fundamentals | 3 | Incomplete | I | — | — | No |
| **Total** | | **13 Reg. Hrs** | | | | **16.6 QP** / **8 Eligible Hrs** | **GPA: 2.08** |

$$\text{Sem 1 GPA} = \frac{10.2 + 6.4 + 0.0}{3 + 2 + 3} = \frac{16.6}{8} = 2.075 \xrightarrow{\text{round}} \mathbf{2.08}$$

### Semester 2: Spring 2025
| Course | Name | Hours | Status / Score | Grade | Points | Quality Points | In GPA? |
|:---|:---|:---:|:---|:---:|:---:|:---:|:---:|
| `CS102` | OOP | 3 | Normal (96) | A+ | 4.0 | 12.0 | Yes |
| `MATH102` | Linear Algebra | 2 | Normal (70) | C | 2.4 | 4.8 | Yes |
| **Total** | | **5 Reg. Hrs** | | | | **16.8 QP** / **5 Eligible Hrs** | **GPA: 3.36** |

$$\text{Sem 2 GPA} = \frac{12.0 + 4.8}{3 + 2} = \frac{16.8}{5} = \mathbf{3.36}$$

### Cumulative Across All Semesters:
$$\text{Cumulative GPA} = \frac{16.6 + 16.8}{8 + 5} = \frac{33.4}{13} = 2.56923... \xrightarrow{\text{round}} \mathbf{2.57}$$

---

## 5. Complete API Endpoints Reference

### 5.1 Students API

#### `GET /api/students`
- **Description:** Retrieve all students ordered by code.
- **Response:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Operation completed successfully.",
  "payload": [
    { "id": 1, "code": "STU001", "name": "أحمد محمد علي" },
    { "id": 2, "code": "STU002", "name": "سارة محمود خليل" }
  ]
}
```

#### `GET /api/students/{id}`
- **Description:** Retrieve a single student by numeric ID.

#### `GET /api/students/code/{code}`
- **Description:** Retrieve a single student by unique student code.

#### `POST /api/students`
- **Description:** Create a new student.
- **Request Body:**
```json
{
  "code": "STU011",
  "name": "محمود إبراهيم سعيد"
}
```
- **Constraints:** `code` required (max 20), `name` required (max 150).

#### `PUT /api/students/{id}`
- **Description:** Update an existing student's name.
- **Request Body:**
```json
{
  "name": "محمود إبراهيم الشريف"
}
```

#### `DELETE /api/students/{id}`
- **Description:** Delete student (blocked with 400 if student has enrolled courses).

---

### 5.2 Courses API

#### `GET /api/courses`
- **Description:** Retrieve all courses.
- **Response:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "payload": [
    { "id": 1, "code": "CS101", "name": "Introduction to Computer Science", "creditHours": 3 },
    { "id": 11, "code": "MATH101", "name": "Calculus I", "creditHours": 2 }
  ]
}
```

#### `POST /api/courses`
- **Description:** Create a course.
- **Request Body:**
```json
{
  "code": "CS501",
  "name": "Deep Learning Systems",
  "creditHours": 3
}
```
- **Constraints:** `creditHours` must be **1, 2, or 3**. `code` must be unique.

#### `PUT /api/courses/{id}`
- **Description:** Update course name and/or credit hours.

#### `DELETE /api/courses/{id}`
- **Description:** Delete course (blocked with 400 if enrolled by any student).

---

### 5.3 Semesters API

#### `GET /api/semesters`
- **Description:** Retrieve all semesters ordered by Start Date.
- **Response:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "payload": [
    { "id": 1, "name": "Fall 2024", "startDate": "2024-09-01", "endDate": "2025-01-15" },
    { "id": 2, "name": "Spring 2025", "startDate": "2025-02-01", "endDate": "2025-06-15" }
  ]
}
```

#### `POST /api/semesters`
- **Description:** Create a new semester.
- **Request Body:**
```json
{
  "name": "Fall 2025",
  "startDate": "2025-09-01",
  "endDate": "2026-01-15"
}
```
- **Constraints:** `endDate` must be strictly greater than `startDate`.

---

### 5.4 Enrollments API

#### `GET /api/enrollments?studentId={id}&semesterId={id}`
- **Description:** List course enrollments with optional filtering by student or semester.
- **Response:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "payload": [
    {
      "id": 1,
      "studentId": 1,
      "studentCode": "STU001",
      "studentName": "أحمد محمد علي",
      "semesterId": 4,
      "semesterName": "Fall 2024",
      "courseId": 1,
      "courseCode": "CS101",
      "courseName": "Introduction to Computer Science",
      "creditHours": 3,
      "status": "Normal",
      "score": 90.0,
      "gradingScaleId": 3,
      "grade": "A-",
      "gradePoints": 3.4,
      "qualityPoints": 10.2,
      "includedInGpa": true
    }
  ]
}
```

#### `POST /api/enrollments`
- **Description:** Enroll a student in a course for a semester.
- **Request Body (Normal Score):**
```json
{
  "studentId": 1,
  "semesterId": 4,
  "courseId": 2,
  "status": "Normal",
  "score": 92.5
}
```
- **Request Body (Special Status):**
```json
{
  "studentId": 1,
  "semesterId": 4,
  "courseId": 15,
  "status": "Withdrawn",
  "score": null
}
```
- **Supported `status` values:** `"Normal"`, `"Absent"`, `"Withdrawn"`, `"Incomplete"`.
- **Validation Rules:**
  - If `status == "Normal"`: `score` is required and must be between $0$ and $100$.
  - If `status != "Normal"`: `score` must be `null`.
  - Max 18 registered credit hours per student per semester.
  - Duplicate enrollment in the same semester is rejected (HTTP 409).

#### `PUT /api/enrollments/{id}`
- **Description:** Update enrollment grade/score or status.
- **Request Body:**
```json
{
  "status": "Normal",
  "score": 96.0
}
```

#### `DELETE /api/enrollments/{id}`
- **Description:** Drop/delete an enrollment.

---

### 5.5 Grading Scales API

#### `GET /api/gradingscales`
- **Description:** Retrieve all 13 grading scale ranges.

#### `GET /api/gradingscales/resolve?score={score}`
- **Description:** Real-time score resolution helper (ideal for UI live feedback).
- **Example:** `GET /api/gradingscales/resolve?score=91.5`
- **Response:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Score '91.5' maps to grade 'A-' with 3.4 points.",
  "payload": {
    "id": 3,
    "fromGrade": 88.0,
    "toGrade": 92.0,
    "points": 3.4,
    "grade": "A-"
  }
}
```

---

### 5.6 GPA & Transcript API

#### `GET /api/students/{id}/gpa`
- **Description:** Calculate student Semester GPA and Cumulative GPA.
- **Response:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "payload": {
    "studentId": 1,
    "studentCode": "STU001",
    "studentName": "أحمد محمد علي",
    "totalRegisteredHours": 18,
    "totalGpaEligibleHours": 13,
    "totalQualityPoints": 33.4,
    "cumulativeGpa": 2.57,
    "cumulativeGpaDisplay": "2.57",
    "semesters": [
      {
        "semesterId": 4,
        "semesterName": "Fall 2024",
        "registeredHours": 13,
        "gpaEligibleHours": 8,
        "qualityPoints": 16.6,
        "semesterGpa": 2.08,
        "semesterGpaDisplay": "2.08"
      },
      {
        "semesterId": 5,
        "semesterName": "Spring 2025",
        "registeredHours": 5,
        "gpaEligibleHours": 5,
        "qualityPoints": 16.8,
        "semesterGpa": 3.36,
        "semesterGpaDisplay": "3.36"
      }
    ]
  }
}
```

#### `GET /api/students/{id}/transcript`
- **Description:** Get full academic transcript including all semesters, courses, scores, grades, and quality points.
- **Response:**
```json
{
  "statusCode": 200,
  "succeeded": true,
  "payload": {
    "studentId": 1,
    "studentCode": "STU001",
    "studentName": "أحمد محمد علي",
    "cumulativeGpa": 2.57,
    "cumulativeGpaDisplay": "2.57",
    "totalRegisteredHours": 18,
    "totalGpaEligibleHours": 13,
    "totalQualityPoints": 33.4,
    "semesters": [
      {
        "semesterId": 4,
        "semesterName": "Fall 2024",
        "semesterGpa": 2.08,
        "semesterGpaDisplay": "2.08",
        "registeredHours": 13,
        "gpaEligibleHours": 8,
        "totalQualityPoints": 16.6,
        "courses": [
          {
            "enrollmentId": 1,
            "courseId": 1,
            "courseCode": "CS101",
            "courseName": "Introduction to Computer Science",
            "creditHours": 3,
            "status": "Normal",
            "score": 90.0,
            "displayedGrade": "A-",
            "points": 3.4,
            "qualityPoints": 10.2,
            "includedInGpa": true
          },
          {
            "enrollmentId": 3,
            "courseId": 17,
            "courseCode": "ENG101",
            "courseName": "English Composition",
            "creditHours": 3,
            "status": "Absent",
            "score": null,
            "displayedGrade": "ABS",
            "points": 0.0,
            "qualityPoints": 0.0,
            "includedInGpa": true
          },
          {
            "enrollmentId": 4,
            "courseId": 15,
            "courseCode": "PHY101",
            "courseName": "General Physics I",
            "creditHours": 2,
            "status": "Withdrawn",
            "score": null,
            "displayedGrade": "W",
            "points": null,
            "qualityPoints": null,
            "includedInGpa": false
          }
        ]
      }
    ]
  }
}
```

---

## 6. Frontend Screen-by-Screen Implementation Guide (React / Next.js)

### Screen 1: Students Management (`/students`)
- **Key Features:**
  - Data table displaying `Code`, `Name`, and Action buttons (`View Transcript`, `Calculate GPA`, `Edit`, `Delete`).
  - Search input filtering in real time by Student Code or Name.
  - "Add Student" Modal with validation:
    - Code: required, alphanumeric, max 20 chars.
    - Name: required, max 150 chars.
  - Delete action with confirmation modal (handles backend rejection with friendly toast message if student has registered courses).

---

### Screen 2: Courses Catalog (`/courses`)
- **Key Features:**
  - Table of all available courses (`Code`, `Name`, `Credit Hours`).
  - Credit hours badge:
    - 1 Credit Hour: Blue chip.
    - 2 Credit Hours: Purple chip.
    - 3 Credit Hours: Emerald chip.
  - "Add Course" Modal:
    - Course Code input (e.g. `CS201`).
    - Course Name input.
    - Credit Hours selector: Radio group or strict select ($1$, $2$, or $3$).

---

### Screen 3: Academic Semesters (`/semesters`)
- **Key Features:**
  - Cards or Timeline table of academic terms (`Name`, `Start Date`, `End Date`).
  - "Add Semester" Modal:
    - Term Name (e.g., `Fall 2025`, `Summer 2025`).
    - Start Date & End Date pickers.
    - Client-side validation: End Date must be after Start Date.

---

### Screen 4: Course Registration & Enrollment (`/enrollments`)
- **Key UX Features:**
  - **Student Selector & Semester Selector:** Filters the view to show current registrations for that student in that term.
  - **Live 18-Hour Credit Meter:**
    - Visual progress bar showing e.g. `13 / 18 Credit Hours Registered`.
    - Shows remaining available hours (e.g. `5 hours remaining`).
    - Disables selecting a course if its hours exceed remaining capacity.
  - **Course Enrollment Form:**
    1. Select Course from dropdown (displays credit hours badge next to course name).
    2. Academic Status selector:
       - Segmented Control or Radio Buttons: `[Normal Score] [Absent (ABS)] [Withdrawn (W)] [Incomplete (I)]`.
       - If `Normal Score` is selected: Show numeric Score input ($0 - 100$).
       - If `Normal Score`: Call `/api/gradingscales/resolve?score={value}` with a 300ms debounce to display a live pill showing predicted grade and grade points (e.g., `A- (3.4 points)`).
       - If `Absent`, `Withdrawn`, or `Incomplete`: Automatically set `score = null` and disable the score input.

---

### Screen 5: Academic Transcript & GPA Dashboard (`/students/[id]/transcript`)
- **Header Summary Card:**
  - Student Name (Arabic/English), Code (`STU001`).
  - **Cumulative GPA Banner:** Large badge displaying `2.57` (or `"N/A"`).
  - Quick stats: Total Registered Hours (`18`), Total GPA-Eligible Hours (`13`), Total Quality Points (`33.4`).
  - "Recalculate GPA" button with spinner.
- **Semester Accordions:**
  - Header displays: Semester Name, Semester GPA (e.g. `2.08`), Registered Hours, GPA Eligible Hours.
  - Inside Accordion: Detailed table:
    - Columns: `Course Code`, `Course Name`, `Hours`, `Score`, `Grade`, `Points`, `Quality Points`, `Status Badge`.
    - Status badges:
      - Normal: Grade badge with letter color.
      - Absent: `ABS` badge + tooltip ("Counts in GPA as 0.0 points").
      - Withdrawn: `W` badge + tooltip ("Excluded from GPA").
      - Incomplete: `I` badge + tooltip ("Excluded from GPA").

---

## 7. TypeScript Type Definitions (Ready to Copy-Paste)

```typescript
// types/api.ts

export interface ApiResponse<T> {
  statusCode: number;
  succeeded: boolean;
  message?: string;
  errors?: string[];
  payload: T;
}

export interface StudentDto {
  id: number;
  code: string;
  name: string;
}

export interface CreateStudentDto {
  code: string;
  name: string;
}

export interface UpdateStudentDto {
  name: string;
}

export interface CourseDto {
  id: number;
  code: string;
  name: string;
  creditHours: 1 | 2 | 3;
}

export interface CreateCourseDto {
  code: string;
  name: string;
  creditHours: number;
}

export interface SemesterDto {
  id: number;
  name: string;
  startDate: string; // YYYY-MM-DD
  endDate: string;   // YYYY-MM-DD
}

export type AcademicStatus = 'Normal' | 'Absent' | 'Withdrawn' | 'Incomplete';

export interface EnrollmentDto {
  id: number;
  studentId: number;
  studentCode: string;
  studentName: string;
  semesterId: number;
  semesterName: string;
  courseId: number;
  courseCode: string;
  courseName: string;
  creditHours: number;
  status: AcademicStatus;
  score: number | null;
  gradingScaleId: number | null;
  grade: string | null;
  gradePoints: number | null;
  qualityPoints: number | null;
  includedInGpa: boolean;
}

export interface CreateEnrollmentDto {
  studentId: number;
  semesterId: number;
  courseId: number;
  status: AcademicStatus;
  score: number | null;
}

export interface UpdateEnrollmentDto {
  status: AcademicStatus;
  score: number | null;
}

export interface GradingScaleDto {
  id: number;
  fromGrade: number;
  toGrade: number;
  points: number;
  grade: string;
}

export interface CourseTranscriptDto {
  enrollmentId: number;
  courseId: number;
  courseCode: string;
  courseName: string;
  creditHours: number;
  status: string;
  score: number | null;
  displayedGrade: string;
  points: number | null;
  qualityPoints: number | null;
  includedInGpa: boolean;
}

export interface SemesterTranscriptDto {
  semesterId: number;
  semesterName: string;
  semesterGpa: number | null;
  semesterGpaDisplay: string;
  registeredHours: number;
  gpaEligibleHours: number;
  totalQualityPoints: number;
  courses: CourseTranscriptDto[];
}

export interface TranscriptDto {
  studentId: number;
  studentCode: string;
  studentName: string;
  cumulativeGpa: number | null;
  cumulativeGpaDisplay: string;
  totalRegisteredHours: number;
  totalGpaEligibleHours: number;
  totalQualityPoints: number;
  semesters: SemesterTranscriptDto[];
}

export interface SemesterGpaSummaryDto {
  semesterId: number;
  semesterName: string;
  semesterGpa: number | null;
  semesterGpaDisplay: string;
  registeredHours: number;
  gpaEligibleHours: number;
  qualityPoints: number;
}

export interface GpaResultDto {
  studentId: number;
  studentCode: string;
  studentName: string;
  cumulativeGpa: number | null;
  cumulativeGpaDisplay: string;
  totalRegisteredHours: number;
  totalGpaEligibleHours: number;
  totalQualityPoints: number;
  semesters: SemesterGpaSummaryDto[];
}
```

---

## 8. Database Seeding (Egyptian Sample Dataset)

When launched, the system automatically runs database migrations and seeds comprehensive sample data:

### 10 Students (Egyptian Names):
1. **`STU001` - أحمد محمد علي**: Assessment verification student (Cumulative GPA: `2.57`).
2. **`STU002` - سارة محمود خليل**: Honors student across 3 terms (Cumulative GPA: `3.82`).
3. **`STU003` - عمر خالد عبد الرحمن**: High credit load student ($16$ hours registered in Fall 2024).
4. **`STU004` - مريم طارق فؤاد**: Multi-semester student with `Withdrawn` and `Incomplete` courses.
5. **`STU005` - يوسف إبراهيم حسن**: Upper-level student in advanced CS topics.
6. **`STU006` - منى كمال الشناوي**: Consistent B+ student.
7. **`STU007` - مصطفى أحمد النجار**: Student with an `Absent` course.
8. **`STU008` - نورهان عادل منصور**: Freshman high-achiever ($99$, $97$, $94$).
9. **`STU009` - زياد شريف الديب**: Senior student completing Graduation Capstone projects.
10. **`STU010` - فاطمة حسن السعيد**: Computer science major with A/A- average.

### 5 Semesters:
- `Fall 2023`, `Spring 2024`, `Summer 2024`, `Fall 2024`, `Spring 2025`

### 26 Courses:
- Comprehensive curriculum across Computer Science, Mathematics, Physics, Engineering, IT, and Project Management (all compliant with $1 - 3$ credit hours).

---

## 9. Running the Application

### Prerequisites:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server (LocalDB, SQL Express, or standard instance)

### Setup & Run:
1. In `StudentGPAManagementSystem.API/appsettings.json`, set your connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.;Database=StudentGpaDb;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```
2. Start the API:
   ```bash
   dotnet run --project StudentGPAManagementSystem.API/StudentGPAManagementSystem.API.csproj
   ```
3. Open Swagger UI in browser:
   ```
   http://localhost:5000/swagger
   ```
