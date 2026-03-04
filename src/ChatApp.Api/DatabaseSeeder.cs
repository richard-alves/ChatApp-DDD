using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence;

namespace ChatApp.Api;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!context.ChatRooms.Any())
        {
            var rooms = new[]
            {
                ChatRoom.Create("General", "General discussion room"),
                ChatRoom.Create("Tech Talk", "Technology discussions"),
                ChatRoom.Create("Random", "Off-topic conversations")
            };

            foreach (var room in rooms)
            {
                context.ChatRooms.Add(room);
                room.ClearDomainEvents();
            }

            await context.SaveChangesAsync();
        }
    }
}
