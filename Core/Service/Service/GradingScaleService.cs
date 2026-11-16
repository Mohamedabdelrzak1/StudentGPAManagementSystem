using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
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
    private readonly IMapper _mapper;

    public GradingScaleService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Response<IEnumerable<GradingScaleDto>>> GetAllGradingScalesAsync()
    {
        var repo = _unitOfWork.GetRepository<GradingScale, int>();
        var scales = await repo.GetAllQueryable()
            .OrderByDescending(g => g.FromGrade)
            .ToListAsync();

        var dtos = _mapper.Map<IEnumerable<GradingScaleDto>>(scales);
        return Response<IEnumerable<GradingScaleDto>>.Success(dtos);
    }

    public async Task<GradingScaleDto?> ResolveGradeAsync(decimal score)
    {
        if (score < 0 || score > 100)
            return null;

        var repo = _unitOfWork.GetRepository<GradingScale, int>();

        var scale = await repo.GetAllQueryable()
            .Where(g => score >= g.FromGrade && score < g.ToGrade)
            .FirstOrDefaultAsync();

        return scale != null ? _mapper.Map<GradingScaleDto>(scale) : null;
    }
}
