using Application.DTOs.Admin;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementitions
{
    public class ActivityLogService(IGeneric<ActivityLog> activityLogs, ILogger<ActivityLogService> logger) : IActivityLogService
    {
        public async Task LogAsync(string? userId, string userName, string action, string entityType, string? entityId, string description)
        {
            try
            {
                await activityLogs.AddAsync(new ActivityLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserName = string.IsNullOrWhiteSpace(userName) ? "Unknown" : userName,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Failed to write activity log {Action} for {EntityType} {EntityId} after the primary operation completed.",
                    action,
                    entityType,
                    entityId);
            }
        }

        public async Task<ServiceResponse> GetLogsAsync(ActivityLogQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var logs = (await activityLogs.GetAllAsync()).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Action))
                logs = logs.Where(l => l.Action.ToLower().Contains(query.Action.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(query.EntityType))
                logs = logs.Where(l => l.EntityType.ToLower().Contains(query.EntityType.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                logs = logs.Where(l =>
                    (l.UserName ?? string.Empty).ToLower().Contains(search) ||
                    (l.Description ?? string.Empty).ToLower().Contains(search) ||
                    (l.EntityId ?? string.Empty).ToLower().Contains(search));
            }

            var orderedLogs = logs.OrderByDescending(l => l.CreatedAt).ToList();
            var totalCount = orderedLogs.Count;
            var response = new PaginatedResponse<ActivityLogDto>
            {
                Items = orderedLogs.Skip((page - 1) * pageSize).Take(pageSize).Select(Map).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return new ServiceResponse(true, "Activity logs retrieved successfully", response);
        }

        public async Task<IEnumerable<ActivityLogDto>> GetLatestAsync(int count)
        {
            return (await activityLogs.GetAllAsync())
                .OrderByDescending(l => l.CreatedAt)
                .Take(Math.Max(count, 1))
                .Select(Map)
                .ToList();
        }

        public async Task<IEnumerable<ActivityLogDto>> GetByUserAsync(string userId, int count)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return [];

            return (await activityLogs.GetAllAsync())
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(Math.Max(count, 1))
                .Select(Map)
                .ToList();
        }

        private static ActivityLogDto Map(ActivityLog log)
        {
            return new ActivityLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserName = log.UserName,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Description = log.Description,
                CreatedAt = log.CreatedAt
            };
        }
    }
}
