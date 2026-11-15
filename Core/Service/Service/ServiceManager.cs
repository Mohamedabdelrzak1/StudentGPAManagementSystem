using System;
using Domain.Contracts;
using ServiceAbstraction;

namespace Service;

public class ServiceManager : IServiceManager
{
    private readonly Lazy<IStudentService> _studentService;
    private readonly Lazy<ICourseService> _courseService;
    private readonly Lazy<ISemesterService> _semesterService;
    private readonly Lazy<IGradingScaleService> _gradingScaleService;
    private readonly Lazy<IEnrollmentService> _enrollmentService;
    private readonly Lazy<IGpaCalculatorService> _gpaCalculatorService;

    public ServiceManager(IUnitOfWork unitOfWork)
    {
        _gradingScaleService = new Lazy<IGradingScaleService>(() => new GradingScaleService(unitOfWork));
        _studentService = new Lazy<IStudentService>(() => new StudentService(unitOfWork));
        _courseService = new Lazy<ICourseService>(() => new CourseService(unitOfWork));
        _semesterService = new Lazy<ISemesterService>(() => new SemesterService(unitOfWork));
        _enrollmentService = new Lazy<IEnrollmentService>(() => new EnrollmentService(unitOfWork, _gradingScaleService.Value));
        _gpaCalculatorService = new Lazy<IGpaCalculatorService>(() => new GpaCalculatorService(unitOfWork));
    }

    public IStudentService StudentService => _studentService.Value;
    public ICourseService CourseService => _courseService.Value;
    public ISemesterService SemesterService => _semesterService.Value;
    public IGradingScaleService GradingScaleService => _gradingScaleService.Value;
    public IEnrollmentService EnrollmentService => _enrollmentService.Value;
    public IGpaCalculatorService GpaCalculatorService => _gpaCalculatorService.Value;
}
