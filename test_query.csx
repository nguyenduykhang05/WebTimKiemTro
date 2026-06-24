#r "E:\DoanLapTrinhWeb\bin\Debug\net10.0\SmartRoomFinder.dll"
#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 9.0.0"

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using SmartRoomFinder.Data;
using SmartRoomFinder.Models;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=SmartRoomFinder;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

using var context = new AppDbContext(optionsBuilder.Options);

var query = context.Rooms.Where(r => r.IsActive && !r.IsDraft);
var rooms = query.OrderByDescending(r => r.PostedAt).ToList();

Console.WriteLine($"Total active rooms: {rooms.Count}");
foreach(var r in rooms) {
    Console.WriteLine($"- {r.Title} (ApprovalStatus: {r.ApprovalStatus}, PostedAt: {r.PostedAt})");
}
