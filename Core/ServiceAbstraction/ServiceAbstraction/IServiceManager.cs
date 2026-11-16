namespace ServiceAbstraction;

public interface IServiceManager
{
    IStudentService StudentService { get; }
    ICourseService CourseService { get; }
    ISemesterService SemesterService { get; }
    IGradingScaleService GradingScaleService { get; }
    IEnrollmentService EnrollmentService { get; }
    IGpaCalculatorService GpaCalculatorService { get; }
    IDashboardService DashboardService { get; }
}
