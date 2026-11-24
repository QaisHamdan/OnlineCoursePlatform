\# 🎓 Online Course Platform



\### A Modern Web-Based Learning Management Platform



A web-based educational platform designed to make online learning more accessible and flexible. It allows students to explore courses, enroll in learning programs, access lessons, and interact with instructors through an organized learning environment.



Built using ASP.NET Core MVC and modern web technologies, the platform focuses on structured course management, user authentication, role-based access control, and online payment integration.



\---



\## 🌐 Live Demo



🚀 \*\*The platform is live and available online!\*\*



Explore the deployed application:



\### 👉 \[Visit Online Course Platform](https://qais-online-course-platform.somee.com)



\*\*Hosting:\*\* Somee  

\*\*Framework:\*\* ASP.NET Core MVC (.NET 8)



Visitors can explore the platform, browse available courses, and experience its features through the live application.



\---



\## 📌 Overview



OnlineCoursePlatform provides an integrated environment for managing online educational content.



The system connects students and instructors through dedicated functionalities, allowing instructors to manage their educational content while students can discover courses, enroll, and access available learning materials.



The platform also includes an administrative area for managing the overall learning environment.



\---



\## ✨ Key Features



\### 👨‍🎓 Student Features



\- User registration and authentication.

\- Browse and explore available courses.

\- View course details and lessons.

\- Enroll in courses.

\- Access lessons according to enrollment permissions.

\- Submit course reviews and ratings.

\- Manage personal learning activities.



\### 👨‍🏫 Instructor Features



\- Instructor account management.

\- Create and manage educational courses.

\- Organize course lessons.

\- Manage course content.



\### 🛡️ Administration



\- Dedicated administrative area.

\- User and instructor management.

\- Course and category management.

\- Centralized platform administration.



\### 💳 Payment Integration



\- PayPal payment integration.

\- Payment processing and verification.

\- Enrollment workflow connected to payment status.



\---



\## 🛠️ Technologies Used



| Technology | Purpose |

|---|---|

| C# | Backend Programming |

| ASP.NET Core MVC | Web Application Framework |

| .NET 8 | Development Platform |

| Entity Framework Core | ORM and Database Management |

| SQL Server | Relational Database |

| ASP.NET Core Identity | Authentication and User Management |

| PayPal | Online Payment Integration |

| HTML5 | Page Structure |

| CSS3 | Styling |

| JavaScript | Frontend Interactivity |

| Bootstrap | Responsive UI |



\---



\## 📸 Screenshots



Here are some screenshots showcasing the platform's interface and main functionalities.



\### 🏠 Home Page

!\[Home Page](Screenshots/home.png)



\### 📚 Courses Page

!\[Courses Page](Screenshots/courses.png)



\### 📖 Course Details

!\[Course Details](Screenshots/course-details.png)



\### 👨‍🎓 Student Dashboard

!\[Student Dashboard](Screenshots/student-dashboard.png)



\### 👨‍🏫 Instructor Dashboard

!\[Instructor Dashboard](Screenshots/instructor-dashboard.png)



\### 🛡️ Admin Dashboard

!\[Admin Dashboard](Screenshots/admin-dashboard.png)



\### 💳 Payment Page

!\[Payment Page](Screenshots/payment.png)



\---



\## 🏗️ Project Structure



```text

OnlineCoursePlatform/

│

├── Controllers/

│   ├── AccountController.cs

│   ├── CourseController.cs

│   ├── InstructorController.cs

│   ├── LessonController.cs

│   ├── EnrollmentController.cs

│   ├── PaymentController.cs

│   ├── AdminController.cs

│   ├── CategoryController.cs

│   └── ReviewController.cs

│

├── Models/

│

├── ViewModels/

│

├── Views/

│

├── Services/

│   └── PayPalService.cs

│

├── Migrations/

│

├── wwwroot/

│

├── Properties/

│

├── Program.cs

├── appsettings.json

└── OnlineCoursePlatform.csproj

```



\*The structure above represents the main application components; individual files may differ in the actual repository.\*



\---



\## 🗄️ Database



The application uses Microsoft SQL Server with Entity Framework Core.



The database handles the platform's main entities and relationships, including:



\- Users and Identity information.

\- Courses and categories.

\- Lessons and educational content.

\- Student enrollments.

\- Payments.

\- Course reviews.



Database migrations are used to manage schema changes.



\---



\## ⚙️ Getting Started



\### Prerequisites



Before running the application, make sure you have:



\- .NET 8 SDK.

\- Microsoft SQL Server.

\- Visual Studio 2022 or Visual Studio Code.

\- Entity Framework Core tools.



\### 1. Clone the Repository



```bash

git clone https://github.com/QaisHamdan/OnlineCoursePlatform.git

```



\### 2. Navigate to the Project



```bash

cd OnlineCoursePlatform

```



\### 3. Configure the Database



Update the database connection string in your local configuration.



Make sure SQL Server is running and the database settings are correct.



\### 4. Configure PayPal



Add your own PayPal credentials through a secure local configuration or environment variables.



Never commit real API credentials or private keys to GitHub.



\### 5. Apply Database Migrations



```bash

dotnet ef database update

```



\### 6. Run the Application



```bash

dotnet restore

dotnet build

dotnet run

```



Open the local URL displayed in the terminal.



\---



\## 🔐 Security



The project uses ASP.NET Core Identity for user authentication and supports role-based access management.



Sensitive configuration values, payment credentials, and database passwords should be stored securely outside the public repository.



\---



\## 🚀 Future Improvements



Potential improvements include:



\- Enhanced course discovery and filtering.

\- Improved learning analytics.

\- Additional payment options.

\- Advanced course progress tracking.

\- Improved administrative reporting.



\---



\## 👨‍💻 Developer



\*\*Qais Hamdan\*\*



Computer Information Systems Graduate  

Tafila Technical University – 2026



Interested in Web Development, Software Development, and building practical web-based systems.



📍 Amman, Jordan



\- GitHub: \[Qais Hamdan](https://github.com/QaisHamdan)

\- LinkedIn: \[Qais Hamdan](https://www.linkedin.com/in/qais-hamdan-b13206377)



\---



\## 📄 License



This project is published for portfolio and educational purposes.



\---



\*\*Built with dedication by Qais Hamdan.\*\*

