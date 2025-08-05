using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.Infrastructure;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public static class MockOkrSeedData
{
    public static async Task SeedMockOkrData(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        OKRDbContext context,
        ILogger logger)
    {
        // --- 1. Organizations ---
        var org1 = await context.Organizations.FirstOrDefaultAsync(o => o.Name == "Acme Corp");
        var org2 = await context.Organizations.FirstOrDefaultAsync(o => o.Name == "Beta Ltd");
        if (org1 == null)
        {
            org1 = new Organization { Name = "Acme Corp", Description = "Tech Company", Country = "USA", Industry = "Software", Size = 100, Email = "info@acme.com", Phone = "1234567890", IsActive = true };
            context.Organizations.Add(org1);
        }
        if (org2 == null)
        {
            org2 = new Organization { Name = "Beta Ltd", Description = "Consulting", Country = "UK", Industry = "Consulting", Size = 50, Email = "contact@beta.com", Phone = "0987654321", IsActive = true };
            context.Organizations.Add(org2);
        }
        await context.SaveChangesAsync();

        // --- 2. Users ---
        async Task<User> EnsureUser(string email, User user, string password, RoleType role)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
                return existing;
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role.ToString());
                logger.LogInformation("Created user {Email} with role {Role}", user.Email, role);
                return user;
            }
            else
            {
                logger.LogError("Failed to create user {Email}: {Errors}", user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new Exception($"Failed to create user {email}");
            }
        }

        // Org1: 1 admin, 3 managers, 10 collaborators
        var org1Admin = await EnsureUser(
            "admin1@acme.com",
            new User { SupabaseId = Guid.NewGuid().ToString(), Email = "admin1@acme.com", FirstName = "Alice", LastName = "Admin", Address = "123 Main St", DateOfBirth = DateTime.SpecifyKind(new DateTime(1980,1,1), DateTimeKind.Utc), Position = "Software Developer", IsEnabled = true, Gender = Gender.Female, OrganizationId = org1.Id, UserName = "admin1@acme.com", EmailConfirmed = true },
            "Admin123!", RoleType.OrganizationAdmin);

        var org1Managers = new List<User>();
        for (int i = 1; i <= 3; i++)
        {
            var email = $"manager{i}@acme.com";
            var user = new User { SupabaseId = Guid.NewGuid().ToString(), Email = email, FirstName = $"Manager{i}", LastName = "Acme", Address = "123 Main St", DateOfBirth = DateTime.SpecifyKind(new DateTime(1985,1,i), DateTimeKind.Utc), Position = "UI/UX Designer", IsEnabled = true, Gender = Gender.Male, OrganizationId = org1.Id, UserName = email, EmailConfirmed = true };
            org1Managers.Add(await EnsureUser(email, user, $"Manager{i}123!", RoleType.TeamManager));
        }
        var org1Collaborators = new List<User>();
        for (int i = 1; i <= 10; i++)
        {
            var email = $"collab{i}@acme.com";
            var user = new User { SupabaseId = Guid.NewGuid().ToString(), Email = email, FirstName = $"Collab{i}", LastName = "Acme", Address = "123 Main St", DateOfBirth = DateTime.SpecifyKind(new DateTime(1990,1,i), DateTimeKind.Utc), Position = "QA Engineer", IsEnabled = true, Gender = Gender.Female, OrganizationId = org1.Id, UserName = email, EmailConfirmed = true };
            org1Collaborators.Add(await EnsureUser(email, user, $"Collab{i}123!", RoleType.Collaborator));
        }

        // Org2: 1 admin, 3 managers, 10 collaborators
        var org2Admin = await EnsureUser(
            "admin2@beta.com",
            new User { SupabaseId = Guid.NewGuid().ToString(), Email = "admin2@beta.com", FirstName = "Bob", LastName = "Admin", Address = "456 Side St", DateOfBirth = DateTime.SpecifyKind(new DateTime(1982,2,2), DateTimeKind.Utc), Position = "Software Developer", IsEnabled = true, Gender = Gender.Male, OrganizationId = org2.Id, UserName = "admin2@beta.com", EmailConfirmed = true },
            "Admin223!", RoleType.OrganizationAdmin);

        var org2Managers = new List<User>();
        for (int i = 1; i <= 3; i++)
        {
            var email = $"manager{i}@beta.com";
            var user = new User { SupabaseId = Guid.NewGuid().ToString(), Email = email, FirstName = $"Manager{i}", LastName = "Beta", Address = "456 Side St", DateOfBirth = DateTime.SpecifyKind(new DateTime(1987,2,i), DateTimeKind.Utc), Position = "UI/UX Designer", IsEnabled = true, Gender = Gender.Female, OrganizationId = org2.Id, UserName = email, EmailConfirmed = true };
            org2Managers.Add(await EnsureUser(email, user, $"ManagerB{i}123!", RoleType.TeamManager));
        }
        var org2Collaborators = new List<User>();
        for (int i = 1; i <= 10; i++)
        {
            var email = $"collab{i}@beta.com";
            var user = new User { SupabaseId = Guid.NewGuid().ToString(), Email = email, FirstName = $"Collab{i}", LastName = "Beta", Address = "456 Side St", DateOfBirth = DateTime.SpecifyKind(new DateTime(1992,2,i), DateTimeKind.Utc), Position = "QA Engineer", IsEnabled = true, Gender = Gender.Male, OrganizationId = org2.Id, UserName = email, EmailConfirmed = true };
            org2Collaborators.Add(await EnsureUser(email, user, $"CollabB{i}123!", RoleType.Collaborator));
        }

        // --- 3. Teams ---
        async Task<Team> EnsureTeam(string name, Team team)
        {
            var existing = await context.Teams.FirstOrDefaultAsync(t => t.Name == name && t.OrganizationId == team.OrganizationId);
            if (existing != null)
                return existing;
            context.Teams.Add(team);
            await context.SaveChangesAsync();
            return team;
        }
        var team1 = await EnsureTeam("Acme Alpha", new Team { Name = "Acme Alpha", Description = "Alpha Team", OrganizationId = org1.Id, TeamManagerId = org1Managers[0].Id });
        var team2 = await EnsureTeam("Acme Beta", new Team { Name = "Acme Beta", Description = "Beta Team", OrganizationId = org1.Id, TeamManagerId = org1Managers[1].Id });
        var team3 = await EnsureTeam("Beta Alpha", new Team { Name = "Beta Alpha", Description = "Alpha Team", OrganizationId = org2.Id, TeamManagerId = org2Managers[0].Id });
        var team4 = await EnsureTeam("Beta Beta", new Team { Name = "Beta Beta", Description = "Beta Team", OrganizationId = org2.Id, TeamManagerId = org2Managers[1].Id });

        // --- 4. TeamUser (memberships) ---
        async Task EnsureTeamUser(Guid teamId, Guid userId)
        {
            var exists = await context.TeamUsers.AnyAsync(tu => tu.TeamId == teamId && tu.UserId == userId);
            if (!exists)
            {
                context.TeamUsers.Add(new TeamUser { TeamId = teamId, UserId = userId });
            }
        }
        await EnsureTeamUser(team1.Id, org1Managers[0].Id);
        foreach (var u in org1Collaborators.Take(5)) await EnsureTeamUser(team1.Id, u.Id);
        await EnsureTeamUser(team2.Id, org1Managers[1].Id);
        foreach (var u in org1Collaborators.Skip(5).Take(5)) await EnsureTeamUser(team2.Id, u.Id);
        await EnsureTeamUser(team3.Id, org2Managers[0].Id);
        foreach (var u in org2Collaborators.Take(5)) await EnsureTeamUser(team3.Id, u.Id);
        await EnsureTeamUser(team4.Id, org2Managers[1].Id);
        foreach (var u in org2Collaborators.Skip(5).Take(5)) await EnsureTeamUser(team4.Id, u.Id);
        await context.SaveChangesAsync();

        // --- 5. OKR Sessions ---
        async Task<OKRSession> EnsureOKRSession(string title, OKRSession session)
        {
            var existing = await context.OKRSessions.FirstOrDefaultAsync(s => s.Title == title && s.OrganizationId == session.OrganizationId);
            if (existing != null)
                return existing;
            context.OKRSessions.Add(session);
            await context.SaveChangesAsync();
            return session;
        }
        var okrSession1 = await EnsureOKRSession("Q1 2024 Acme", new OKRSession
        {
            Title = "Q1 2024 Acme",
            OrganizationId = org1.Id,
            UserId = org1Admin.Id,
            StartedDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(60),
            Status = Status.InProgress,
            IsActive = true
        });
        var okrSession2 = await EnsureOKRSession("Q1 2024 Beta", new OKRSession
        {
            Title = "Q1 2024 Beta",
            OrganizationId = org2.Id,
            UserId = org2Admin.Id,
            StartedDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(60),
            Status = Status.InProgress,
            IsActive = true
        });

        // --- 6. OKRSessionTeam ---
        async Task EnsureOKRSessionTeam(Guid okrSessionId, Guid teamId)
        {
            var exists = await context.OKRSessionTeams.AnyAsync(x => x.OKRSessionId == okrSessionId && x.TeamId == teamId);
            if (!exists)
            {
                context.OKRSessionTeams.Add(new OKRSessionTeam { OKRSessionId = okrSessionId, TeamId = teamId });
            }
        }
        await EnsureOKRSessionTeam(okrSession1.Id, team1.Id);
        await EnsureOKRSessionTeam(okrSession1.Id, team2.Id);
        await EnsureOKRSessionTeam(okrSession2.Id, team3.Id);
        await EnsureOKRSessionTeam(okrSession2.Id, team4.Id);
        await context.SaveChangesAsync();

        // --- 7. Objectives ---
        async Task<Objective> EnsureObjective(string title, Objective obj)
        {
            var existing = await context.Objectives.FirstOrDefaultAsync(o => o.Title == title && o.OKRSessionId == obj.OKRSessionId);
            if (existing != null)
                return existing;
            context.Objectives.Add(obj);
            await context.SaveChangesAsync();
            return obj;
        }
        var obj1 = await EnsureObjective("Increase Sales", new Objective
        {
            Title = "Increase Sales",
            OKRSessionId = okrSession1.Id,
            UserId = org1Managers[0].Id,
            ResponsibleTeamId = team1.Id,
            StartedDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(60),
            Status = Status.InProgress,
            Priority = Priority.High,
            Progress = 0
        });
        var obj2 = await EnsureObjective("Improve Product Quality", new Objective
        {
            Title = "Improve Product Quality",
            OKRSessionId = okrSession1.Id,
            UserId = org1Managers[1].Id,
            ResponsibleTeamId = team2.Id,
            StartedDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(60),
            Status = Status.InProgress,
            Priority = Priority.Medium,
            Progress = 0
        });
        var obj3 = await EnsureObjective("Expand Market", new Objective
        {
            Title = "Expand Market",
            OKRSessionId = okrSession2.Id,
            UserId = org2Managers[0].Id,
            ResponsibleTeamId = team3.Id,
            StartedDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(60),
            Status = Status.InProgress,
            Priority = Priority.High,
            Progress = 0
        });
        var obj4 = await EnsureObjective("Enhance Customer Support", new Objective
        {
            Title = "Enhance Customer Support",
            OKRSessionId = okrSession2.Id,
            UserId = org2Managers[1].Id,
            ResponsibleTeamId = team4.Id,
            StartedDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(60),
            Status = Status.InProgress,
            Priority = Priority.Medium,
            Progress = 0
        });

        // --- 8. Key Results ---
        async Task<KeyResult> EnsureKeyResult(string title, KeyResult kr)
        {
            var existing = await context.KeyResults.FirstOrDefaultAsync(x => x.Title == title && x.ObjectiveId == kr.ObjectiveId);
            if (existing != null)
                return existing;
            context.KeyResults.Add(kr);
            await context.SaveChangesAsync();
            return kr;
        }
        var keyResults = new List<KeyResult>
        {
            await EnsureKeyResult("Achieve $1M Sales", new KeyResult
            {
                Title = "Achieve $1M Sales",
                ObjectiveId = obj1.Id,
                UserId = org1Managers[0].Id,
                StartedDate = obj1.StartedDate.ToUniversalTime(),
                EndDate = obj1.EndDate.ToUniversalTime(),
                Status = Status.InProgress,
                Progress = 0
            }),
            await EnsureKeyResult("Sign 10 New Clients", new KeyResult
            {
                Title = "Sign 10 New Clients",
                ObjectiveId = obj1.Id,
                UserId = org1Managers[1].Id,
                StartedDate = obj1.StartedDate.ToUniversalTime(),
                EndDate = obj1.EndDate.ToUniversalTime(),
                Status = Status.NotStarted,
                Progress = 0
            }),
            await EnsureKeyResult("Reduce Bugs by 50%", new KeyResult
            {
                Title = "Reduce Bugs by 50%",
                ObjectiveId = obj2.Id,
                UserId = org1Managers[1].Id,
                StartedDate = obj2.StartedDate.ToUniversalTime(),
                EndDate = obj2.EndDate.ToUniversalTime(),
                Status = Status.InProgress,
                Progress = 0
            }),
            await EnsureKeyResult("Increase Test Coverage", new KeyResult
            {
                Title = "Increase Test Coverage",
                ObjectiveId = obj2.Id,
                UserId = org1Managers[2].Id,
                StartedDate = obj2.StartedDate.ToUniversalTime(),
                EndDate = obj2.EndDate.ToUniversalTime(),
                Status = Status.NotStarted,
                Progress = 0
            }),
            await EnsureKeyResult("Enter 2 New Countries", new KeyResult
            {
                Title = "Enter 2 New Countries",
                ObjectiveId = obj3.Id,
                UserId = org2Managers[0].Id,
                StartedDate = obj3.StartedDate.ToUniversalTime(),
                EndDate = obj3.EndDate.ToUniversalTime(),
                Status = Status.InProgress,
                Progress = 0
            }),
            await EnsureKeyResult("Grow User Base by 20%", new KeyResult
            {
                Title = "Grow User Base by 20%",
                ObjectiveId = obj3.Id,
                UserId = org2Managers[1].Id,
                StartedDate = obj3.StartedDate.ToUniversalTime(),
                EndDate = obj3.EndDate.ToUniversalTime(),
                Status = Status.NotStarted,
                Progress = 0
            }),
            await EnsureKeyResult("Reduce Response Time", new KeyResult
            {
                Title = "Reduce Response Time",
                ObjectiveId = obj4.Id,
                UserId = org2Managers[1].Id,
                StartedDate = obj4.StartedDate.ToUniversalTime(),
                EndDate = obj4.EndDate.ToUniversalTime(),
                Status = Status.InProgress,
                Progress = 0
            }),
            await EnsureKeyResult("Increase CSAT to 90%", new KeyResult
            {
                Title = "Increase CSAT to 90%",
                ObjectiveId = obj4.Id,
                UserId = org2Managers[2].Id,
                StartedDate = obj4.StartedDate.ToUniversalTime(),
                EndDate = obj4.EndDate.ToUniversalTime(),
                Status = Status.NotStarted,
                Progress = 0
            })
        };

        // --- 9. Key Result Tasks ---
        async Task EnsureKeyResultTask(string title, KeyResultTask task)
        {
            var exists = await context.KeyResultTasks.AnyAsync(t => t.Title == title && t.KeyResultId == task.KeyResultId);
            if (!exists)
            {
                context.KeyResultTasks.Add(task);
            }
        }
        var team1Collabs = org1Collaborators.Take(5).ToList();
        var team2Collabs = org1Collaborators.Skip(5).Take(5).ToList();
        var team3Collabs = org2Collaborators.Take(5).ToList();
        var team4Collabs = org2Collaborators.Skip(5).Take(5).ToList();

        int krIndex = 0;
        foreach (var kr in keyResults)
        {
            List<User> collabs;
            if (kr.ObjectiveId == obj1.Id) collabs = team1Collabs;
            else if (kr.ObjectiveId == obj2.Id) collabs = team2Collabs;
            else if (kr.ObjectiveId == obj3.Id) collabs = team3Collabs;
            else collabs = team4Collabs;

            // Task 1: Completed, High priority, last month
            await EnsureKeyResultTask($"Task 1 for {kr.Title}", new KeyResultTask
            {
                Title = $"Task 1 for {kr.Title}",
                KeyResultId = kr.Id,
                UserId = kr.UserId,
                CollaboratorId = collabs[krIndex % collabs.Count].Id,
                StartedDate = DateTime.UtcNow.AddMonths(-1).AddDays(-5),
                EndDate = DateTime.UtcNow.AddMonths(-1).AddDays(-1),
                Status = Status.Completed,
                Priority = Priority.High,
                Progress = 100
            });

            // Task 2: In Progress, Urgent, this month
            await EnsureKeyResultTask($"Task 2 for {kr.Title}", new KeyResultTask
            {
                Title = $"Task 2 for {kr.Title}",
                KeyResultId = kr.Id,
                UserId = kr.UserId,
                CollaboratorId = collabs[(krIndex + 1) % collabs.Count].Id,
                StartedDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(10),
                Status = Status.InProgress,
                Priority = Priority.Urgent,
                Progress = 50
            });

            // Task 3: Not Started, Medium, next month
            await EnsureKeyResultTask($"Task 3 for {kr.Title}", new KeyResultTask
            {
                Title = $"Task 3 for {kr.Title}",
                KeyResultId = kr.Id,
                UserId = kr.UserId,
                CollaboratorId = collabs[(krIndex + 2) % collabs.Count].Id,
                StartedDate = DateTime.UtcNow.AddMonths(1).AddDays(1),
                EndDate = DateTime.UtcNow.AddMonths(1).AddDays(10),
                Status = Status.NotStarted,
                Priority = Priority.Medium,
                Progress = 0
            });

            // Task 4: Overdue, Low, last month, not completed
            await EnsureKeyResultTask($"Task 4 for {kr.Title}", new KeyResultTask
            {
                Title = $"Task 4 for {kr.Title}",
                KeyResultId = kr.Id,
                UserId = kr.UserId,
                CollaboratorId = collabs[(krIndex + 3) % collabs.Count].Id,
                StartedDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(-1),
                Status = Status.Overdue,
                Priority = Priority.Low,
                Progress = 0
            });

            krIndex++;
        }
        await context.SaveChangesAsync();
    }
}
