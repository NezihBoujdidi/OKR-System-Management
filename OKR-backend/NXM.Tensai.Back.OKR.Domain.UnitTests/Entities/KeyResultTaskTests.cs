namespace NXM.Tensai.Back.OKR.Domain.UnitTests.Entities;

public class KeyResultTaskTests
{
    private readonly Faker<KeyResultTask> _taskFaker;

    public KeyResultTaskTests()
    {
        _taskFaker = new Faker<KeyResultTask>()
            .RuleFor(t => t.Id, f => f.Random.Guid())
            .RuleFor(t => t.Title, f => f.Lorem.Sentence(2))
            .RuleFor(t => t.Description, f => f.Lorem.Sentence())
            .RuleFor(t => t.Status, f => f.PickRandom<Status>())
            .RuleFor(t => t.Priority, f => f.PickRandom<Priority>())
            .RuleFor(t => t.StartedDate, f => f.Date.Recent())
            .RuleFor(t => t.EndDate, f => f.Date.Future())
            .RuleFor(t => t.UserId, f => f.Random.Guid())
            .RuleFor(t => t.KeyResultId, f => f.Random.Guid())
            .RuleFor(t => t.Progress, f => f.Random.Int(0, 100))
            .RuleFor(t => t.CollaboratorId, f => f.Random.Guid());
    }

    [Fact]
    public void KeyResultTask_Should_Inherit_From_BaseOKREntity()
    {
        // Arrange & Act
        var task = new KeyResultTask();

        // Assert
        task.Should().BeAssignableTo<BaseOKREntity>();
    }

    [Fact]
    public void KeyResultTask_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var task = _taskFaker.Generate();

        // Assert
        task.Id.Should().NotBeEmpty();
        task.Title.Should().NotBeNullOrEmpty();
        task.UserId.Should().NotBeEmpty();
        task.KeyResultId.Should().NotBeEmpty();
        task.CollaboratorId.Should().NotBeEmpty();
        task.Progress.Should().BeInRange(0, 100);
    }

    [Fact]
    public void KeyResultTask_Should_Initialize_With_Zero_Progress()
    {
        // Arrange & Act
        var task = new KeyResultTask();

        // Assert
        task.Progress.Should().Be(0);
    }

    [Theory]
    [InlineData(Status.NotStarted)]
    [InlineData(Status.InProgress)]
    [InlineData(Status.Completed)]
    [InlineData(Status.Overdue)]
    public void KeyResultTask_Should_Accept_All_Status_Values(Status status)
    {
        // Arrange
        var task = _taskFaker.Generate();

        // Act
        task.Status = status;

        // Assert
        task.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.High)]
    [InlineData(Priority.Urgent)]
    public void KeyResultTask_Should_Accept_All_Priority_Values(Priority priority)
    {
        // Arrange
        var task = _taskFaker.Generate();

        // Act
        task.Priority = priority;

        // Assert
        task.Priority.Should().Be(priority);
    }
}
