CREATE DATABASE PRN222_26SprB1_2;
GO

USE PRN222_26SprB1_2;
GO

CREATE TABLE Student
(
    StudentId   INT IDENTITY(1,1) PRIMARY KEY,
    StudentName NVARCHAR(100) NOT NULL,
    Email       NVARCHAR(100) NOT NULL,
    Major       NVARCHAR(50)  NOT NULL
);
GO

CREATE TABLE Registration
(
    RegId       INT IDENTITY(1,1) PRIMARY KEY,
    StudentId   INT NOT NULL,
    CourseCode  NVARCHAR(10)  NOT NULL,
    CourseName  NVARCHAR(100) NOT NULL,
    Credits     INT NOT NULL,
    Semester    NVARCHAR(20)  NOT NULL,
    CONSTRAINT FK_Registration_Student
        FOREIGN KEY (StudentId) REFERENCES Student(StudentId)
);
GO

INSERT INTO Student (StudentName, Email, Major)
VALUES
(N'Nguyen Van A', N'a@gmail.com', N'Software Engineering'),
(N'Tran Thi B',   N'b@gmail.com', N'Information Assurance'),
(N'Le Van C',     N'c@gmail.com', N'Software Engineering'),
(N'Pham Thi D',   N'd@gmail.com', N'Artificial Intelligence');
GO

INSERT INTO Registration (StudentId, CourseCode, CourseName, Credits, Semester)
VALUES
(1, N'PRN222', N'Web Application Development', 3, N'Fall2024'),
(1, N'DBI202', N'Database Systems', 3, N'Fall2024'),
(1, N'SWD392', N'Software Architecture and Design', 4, N'Spring2025'),
(2, N'MLN111', N'Introduction to Machine Learning', 3, N'Spring2025'),
(2, N'CSI104', N'Computer Security Basics', 3, N'Fall2024'),
(3, N'PRN221', N'C# Programming and .NET', 3, N'Fall2024'),
(4, N'AIL302', N'Applied AI', 4, N'Spring2025');
GO
