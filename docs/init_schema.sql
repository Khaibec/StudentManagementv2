-- StudentManagementv2Db initial schema
-- Safe, idempotent script with IF NOT EXISTS

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Email NVARCHAR(200) NOT NULL,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Courses')
BEGIN
    CREATE TABLE Courses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Title NVARCHAR(250) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        TeacherId INT NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_Courses_Users_TeacherId FOREIGN KEY (TeacherId) REFERENCES Users(Id)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Enrollments')
BEGIN
    CREATE TABLE Enrollments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CourseId INT NOT NULL,
        StudentId INT NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Enrolled',
        EnrolledAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Enrollments_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Enrollments_Users_StudentId FOREIGN KEY (StudentId) REFERENCES Users(Id),
        CONSTRAINT UQ_Enrollments_Course_Student UNIQUE (CourseId, StudentId)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CourseChapters')
BEGIN
    CREATE TABLE CourseChapters (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CourseId INT NOT NULL,
        Title NVARCHAR(250) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        OrderIndex INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_CourseChapters_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Lessons')
BEGIN
    CREATE TABLE Lessons (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ChapterId INT NOT NULL,
        Title NVARCHAR(250) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        OrderIndex INT NOT NULL DEFAULT 0,
        VideoUrl NVARCHAR(MAX) NULL,
        IsPublished BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Lessons_CourseChapters_ChapterId FOREIGN KEY (ChapterId) REFERENCES CourseChapters(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LessonResources')
BEGIN
    CREATE TABLE LessonResources (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        LessonId INT NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        ContentType NVARCHAR(100) NULL,
        FileSize BIGINT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_LessonResources_Lessons_LessonId FOREIGN KEY (LessonId) REFERENCES Lessons(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LessonProgresses')
BEGIN
    CREATE TABLE LessonProgresses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        LessonId INT NOT NULL,
        StudentId INT NOT NULL,
        IsCompleted BIT NOT NULL DEFAULT 0,
        CompletedAt DATETIME2 NULL,
        CONSTRAINT FK_LessonProgresses_Lessons_LessonId FOREIGN KEY (LessonId) REFERENCES Lessons(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LessonProgresses_Users_StudentId FOREIGN KEY (StudentId) REFERENCES Users(Id),
        CONSTRAINT UQ_LessonProgresses_Lesson_Student UNIQUE (LessonId, StudentId)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Assignments')
BEGIN
    CREATE TABLE Assignments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CourseId INT NOT NULL,
        Title NVARCHAR(250) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        DueDate DATETIME2 NOT NULL,
        MaxScore DECIMAL(5,2) NOT NULL DEFAULT 100.00,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Assignments_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Submissions')
BEGIN
    CREATE TABLE Submissions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AssignmentId INT NOT NULL,
        StudentId INT NOT NULL,
        TextContent NVARCHAR(MAX) NULL,
        FileName NVARCHAR(255) NULL,
        FilePath NVARCHAR(500) NULL,
        SubmittedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Status NVARCHAR(50) NOT NULL DEFAULT 'NOT_SUBMITTED',
        Score DECIMAL(5,2) NULL,
        Feedback NVARCHAR(MAX) NULL,
        GradedAt DATETIME2 NULL,
        GradedById INT NULL,
        CONSTRAINT FK_Submissions_Assignments_AssignmentId FOREIGN KEY (AssignmentId) REFERENCES Assignments(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Submissions_Users_StudentId FOREIGN KEY (StudentId) REFERENCES Users(Id),
        CONSTRAINT FK_Submissions_Users_GradedById FOREIGN KEY (GradedById) REFERENCES Users(Id)
    );
END
GO
