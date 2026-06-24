using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

#r ""E:\DoanLapTrinhWeb\bin\Debug\net10.0\SmartRoomFinder.dll""
#r ""nuget: Microsoft.EntityFrameworkCore.SqlServer, 9.0.0""

using SmartRoomFinder.Data;
using SmartRoomFinder.Models;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer(""Server=.\\SQLEXPRESS;Database=SmartRoomFinder;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"");

using var context = new AppDbContext(optionsBuilder.Options);

var rooms = context.Rooms.OrderByDescending(r => r.PostedAt).Take(10).ToList();
Console.WriteLine($""Total rooms in DB: {context.Rooms.Count()}"");
foreach(var r in rooms) {
    Console.WriteLine($""Room: {r.Title}, IsActive: {r.IsActive}, IsDraft: {r.IsDraft}, PostedAt: {r.PostedAt}, OwnerId: {r.OwnerId}"");
}
