using System.Threading.Tasks;
using Shared.Common;
using Shared.Dto;

namespace ServiceAbstraction;

public interface IGpaCalculatorService
{
    Task<Response<GpaResultDto>> CalculateGpaAsync(int studentId);
    Task<Response<TranscriptDto>> GetTranscriptAsync(int studentId);
}
