using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.DTOs;

namespace NguyenHoaiPhong_1681.Helpers;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<T> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }
}
