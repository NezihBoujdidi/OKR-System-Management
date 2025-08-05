namespace NXM.Tensai.Back.OKR.Domain.UnitTests.Entities;

public class KeyResultTests
{
    private readonly Faker<KeyResult> _keyResultFaker;
    private readonly Faker<KeyResultTask> _taskFaker;

    public KeyResultTests()
    {
        _keyResultFaker = new Faker<KeyResult>()
            .RuleFor(kr => kr.Id, f => f.Random.Guid())
            .RuleFor(kr => kr.Title, f => f.Lorem.Sentence(3))
            .RuleFor(kr => kr.Description, f => f.Lorem.Sentence())
            .RuleFor(kr => kr.Status, f => f.PickRandom<Status>())
            .RuleFor(kr => kr.Priority, f => f.PickRandom<Priority>())
            .RuleFor(kr => kr.StartedDate, f => f.Date.Recent())
            .RuleFor(kr => kr.EndDate, f => f.Date.Future())
            .RuleFor(kr => kr.UserId, f => f.Random.Guid())
            .RuleFor(kr => kr.ObjectiveId, f => f.Random.Guid())
            .RuleFor(kr => kr.Progress, f => f.Random.Int(0, 100));

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
    public void KeyResult_Should_Inherit_From_BaseOKREntity()
    {
        // Arrange & Act
        var keyResult = new KeyResult();

        // Assert
        keyResult.Should().BeAssignableTo<BaseOKREntity>();
    }

    [Fact]
    public void KeyResult_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var keyResult = _keyResultFaker.Generate();

        // Assert
        keyResult.Id.Should().NotBeEmpty();
        keyResult.Title.Should().NotBeNullOrEmpty();
        keyResult.UserId.Should().NotBeEmpty();
        keyResult.ObjectiveId.Should().NotBeEmpty();
        keyResult.Progress.Should().BeInRange(0, 100);
    }

    [Fact]
    public void RecalculateProgress_WithNoTasks_Should_SetProgressToZero()
    {
        // Arrange
        var keyResult = _keyResultFaker.Generate();
        var tasks = new List<KeyResultTask>();

        // Act
        keyResult.RecalculateProgress(tasks);

        // Assert
        keyResult.Progress.Should().Be(0);
    }

    [Fact]
    public void RecalculateProgress_WithNullTasks_Should_SetProgressToZero()
    {
        // Arrange
        var keyResult = _keyResultFaker.Generate();

        // Act
        keyResult.RecalculateProgress(null!);

        // Assert
        keyResult.Progress.Should().Be(0);
    }

    [Fact]
    public void RecalculateProgress_WithTasks_Should_CalculatePercentageBasedOnCompletedTasks()
    {
        // Arrange
        var keyResult = _keyResultFaker.Generate();
        var tasks = new List<KeyResultTask>
        {
            _taskFaker.Clone().RuleFor(t => t.Status, Status.Completed).Generate(),
            _taskFaker.Clone().RuleFor(t => t.Status, Status.InProgress).Generate(),
            _taskFaker.Clone().RuleFor(t => t.Status, Status.Completed).Generate(),
            _taskFaker.Clone().RuleFor(t => t.Status, Status.NotStarted).Generate()
        };

        // Act
        keyResult.RecalculateProgress(tasks);

        // Assert
        keyResult.Progress.Should().Be(50); // 2 out of 4 tasks completed = 50%
    }

    [Fact]
    public void RecalculateProgress_WithAllTasksCompleted_Should_SetProgressTo100()
    {
        // Arrange
        var keyResult = _keyResultFaker.Generate();
        var tasks = new List<KeyResultTask>
        {
            _taskFaker.Clone().RuleFor(t => t.Status, Status.Completed).Generate(),
            _taskFaker.Clone().RuleFor(t => t.Status, Status.Completed).Generate(),
            _taskFaker.Clone().RuleFor(t => t.Status, Status.Completed).Generate()
        };

        // Act
        keyResult.RecalculateProgress(tasks);

        // Assert
        keyResult.Progress.Should().Be(100);
    }

    [Fact]
    public void RecalculateProgress_WithNoCompletedTasks_Should_SetProgressToZero()
    {
        // Arrange
        var keyResult = _keyResultFaker.Generate();
        var tasks = new List<KeyResultTask>
        {
            _taskFaker.Clone().RuleFor(t => t.Status, Status.InProgress).Generate(),
            _taskFaker.Clone().RuleFor(t => t.Status, Status.NotStarted).Generate(),
            _taskFaker.Clone().RuleFor(t => t.Status, Status.Overdue).Generate()
        };

        // Act
        keyResult.RecalculateProgress(tasks);

        // Assert
        keyResult.Progress.Should().Be(0);
    }

    [Fact]
    public void RecalculateProgress_WithSingleTask_Should_CalculateCorrectly()
    {
        // Arrange
        var keyResult = _keyResultFaker.Generate();
        var tasks = new List<KeyResultTask>
        {
            _taskFaker.Clone().RuleFor(t => t.Status, Status.Completed).Generate()
        };

        // Act
        keyResult.RecalculateProgress(tasks);

        // Assert
        keyResult.Progress.Should().Be(100);
    }
}
