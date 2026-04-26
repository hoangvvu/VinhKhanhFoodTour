using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.Services;

public class LanguageProvisionJobService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<Guid, LanguageProvisionJobState> _jobs = new();

    public LanguageProvisionJobService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Guid Start(int languageId)
    {
        var id = Guid.NewGuid();
        var state = new LanguageProvisionJobState
        {
            JobId = id,
            LanguageId = languageId,
            Status = "queued",
            Message = "Đã đưa vào hàng đợi.",
            StartedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _jobs[id] = state;

        _ = Task.Run(async () =>
        {
            try
            {
                state.Status = "running";
                state.Message = "Đang khởi chạy đồng bộ...";
                state.UpdatedAt = DateTime.Now;

                using var scope = _scopeFactory.CreateScope();
                var poiService = scope.ServiceProvider.GetRequiredService<PoiService>();
                var result = await poiService.AutoProvisionLanguageForApprovedPoisAsync(languageId, (done, total, msg) =>
                {
                    state.Total = total;
                    state.Completed = done;
                    state.Message = msg;
                    state.UpdatedAt = DateTime.Now;
                });

                state.Total = result.TotalPois;
                state.Completed = result.SuccessCount + result.FailedCount;
                state.Success = result.SuccessCount;
                state.Failed = result.FailedCount;
                state.Errors = result.Errors.Take(20).ToList();
                state.Status = result.FailedCount > 0 ? "warning" : "success";
                state.Message = $"Hoàn tất: {result.SuccessCount}/{result.TotalPois} POI.";
                state.UpdatedAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                state.Status = "failed";
                state.Message = $"Lỗi job: {ex.Message}";
                state.UpdatedAt = DateTime.Now;
            }
        });

        return id;
    }

    public LanguageProvisionJobState? Get(Guid jobId)
        => _jobs.TryGetValue(jobId, out var state) ? state : null;
}

public sealed class LanguageProvisionJobState
{
    public Guid JobId { get; set; }
    public int LanguageId { get; set; }
    public string Status { get; set; } = "queued";
    public string Message { get; set; } = "";
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
