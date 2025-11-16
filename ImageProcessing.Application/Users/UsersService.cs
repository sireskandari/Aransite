using Microsoft.EntityFrameworkCore;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.Common;
using ImageProcessing.Domain.Entities.Users;

namespace ImageProcessing.Application.Users;

public sealed class UsersService(IAppDbContext db) : IUsersService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest req, CancellationToken ct)
    {
        var user = new User
        {
            Email = req.Email.Trim(),
            Name = req.Name.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new UserResponse(user.Id, user.Email!, user.Name!, user.CreatedUtc);
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Users
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.Email!, u.Name!, u.CreatedUtc))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<UserResponse>> ListAsync(string? search, int pageNumber, int pageSize, CancellationToken ct)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        var query = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.Email ?? "").Contains(s) ||
                (u.Name ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserResponse(u.Id, u.Email!, u.Name!, u.CreatedUtc))
            .ToListAsync(ct);

        return new PagedResult<UserResponse>(items, total, pageNumber, pageSize);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.Users.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
