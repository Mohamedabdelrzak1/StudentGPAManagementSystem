using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using ServiceAbstraction;
using Shared.Common;
using Shared.Dto;

namespace Service;

public class GradingScaleService : IGradingScaleService
{
    private readonly IUnitOfWork _unitOfWork;

    public GradingScaleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Response<IEnumerable<GradingScaleDto>>> GetAllGradingScalesAsync()
    {
        var repo = _unitOfWork.GetRepository<GradingScale, int>();
        var scales = await repo.GetAllQueryable()
            .OrderByDescending(g => g.FromGrade)
            .Select(g => new GradingScaleDto
            {
                Id = g.Id,
                FromGrade = g.FromGrade,
                ToGrade = g.ToGrade,
                Points = g.Points,
                Grade = g.Grade
            })
            .ToListAsync();

        return Response<IEnumerable<GradingScaleDto>>.Success(scales);
    }

    public async Task<GradingScaleDto?> ResolveGradeAsync(decimal score)
    {
        if (score < 0 || score > 100)
            return null;

        var repo = _unitOfWork.GetRepository<GradingScale, int>();

        // Range rule: FromGrade <= score < ToGrade
        // Scale is stored in database and mapped dynamically
        var scale = await repo.GetAllQueryable()
            .Where(g => score >= g.FromGrade && score < g.ToGrade)
            .Select(g => new GradingScaleDto
            {
                Id = g.Id,
                FromGrade = g.FromGrade,
                ToGrade = g.ToGrade,
                Points = g.Points,
                Grade = g.Grade
            })
            .FirstOrDefaultAsync();

        return scale;
    }
}
