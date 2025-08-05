namespace NXM.Tensai.Back.OKR.Domain.UnitTests.Entities;

public class ObjectiveTests
{
    private readonly Faker<Objective> _objectiveFaker;
    private readonly Faker<KeyResult> _keyResultFaker;

    public ObjectiveTests()
    {
        _objectiveFaker = new Faker<Objective>()
            .RuleFor(o => o.Id, f => f.Random.Guid())
            .RuleFor(o => o.Title, f => f.Company.CompanyName())
            .RuleFor(o => o.Description, f => f.Lorem.Sentence())
            .RuleFor(o => o.Status, f => f.PickRandom<Status>())
            .RuleFor(o => o.Priority, f => f.PickRandom<Priority>())
            .RuleFor(o => o.StartedDate, f => f.Date.Recent())
            .RuleFor(o => o.EndDate, f => f.Date.Future())
            .RuleFor(o => o.OKRSessionId, f => f.Random.Guid())
            .RuleFor(o => o.UserId, f => f.Random.Guid())
            .RuleFor(o => o.ResponsibleTeamId, f => f.Random.Guid())
            .RuleFor(o => o.Progress, f => f.Random.Int(0, 100));

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
    }

    [Fact]
    public void Objective_Should_Inherit_From_BaseOKREntity()
    {
        // Arrange & Act
        var objective = new Objective();

        // Assert
        objective.Should().BeAssignableTo<BaseOKREntity>();
    }

    [Fact]
    public void Objective_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var objective = _objectiveFaker.Generate();

        // Assert
        objective.Id.Should().NotBeEmpty();
        objective.Title.Should().NotBeNullOrEmpty();
        objective.OKRSessionId.Should().NotBeEmpty();
        objective.UserId.Should().NotBeEmpty();
        objective.ResponsibleTeamId.Should().NotBeEmpty();
        objective.Progress.Should().BeInRange(0, 100);
    }

    [Fact]
    public void RecalculateProgress_WithNoKeyResults_Should_SetProgressToZeroAndStatusToNotStarted()
    {
        // Arrange
        var objective = _objectiveFaker.Generate();
        var keyResults = new List<KeyResult>();

        // Act
        objective.RecalculateProgress(keyResults);

        // Assert
        objective.Progress.Should().Be(0);
        objective.Status.Should().Be(Status.NotStarted);
    }

    [Fact]
    public void RecalculateProgress_WithNullKeyResults_Should_SetProgressToZeroAndStatusToNotStarted()
    {
        // Arrange
        var objective = _objectiveFaker.Generate();        // Act
        objective.RecalculateProgress(null!);

        // Assert
        objective.Progress.Should().Be(0);
        objective.Status.Should().Be(Status.NotStarted);
    }

    [Fact]
    public void RecalculateProgress_WithKeyResults_Should_CalculateAverageProgress()
    {
        // Arrange
        var objective = _objectiveFaker.Generate();
        var keyResults = new List<KeyResult>
        {
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 20).Generate(),
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 40).Generate(),
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 60).Generate()
        };

        // Act
        objective.RecalculateProgress(keyResults);

        // Assert
        objective.Progress.Should().Be(40); // (20 + 40 + 60) / 3 = 40
    }

    [Fact]
    public void RecalculateProgress_WithAllKeyResultsCompleted_Should_SetStatusToCompleted()
    {
        // Arrange
        var objective = _objectiveFaker.Generate();
        var keyResults = new List<KeyResult>
        {
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 100).Generate(),
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 100).Generate(),
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 100).Generate()
        };

        // Act
        objective.RecalculateProgress(keyResults);

        // Assert
        objective.Progress.Should().Be(100);
        objective.Status.Should().Be(Status.Completed);
    }

    [Fact]
    public void RecalculateProgress_WithSomeKeyResultsInProgress_Should_SetStatusToInProgress()
    {
        // Arrange
        var objective = _objectiveFaker.Generate();
        var keyResults = new List<KeyResult>
        {
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 0).Generate(),
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 50).Generate(),
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 0).Generate()
        };

        // Act
        objective.RecalculateProgress(keyResults);

        // Assert
        objective.Status.Should().Be(Status.InProgress);
    }

    [Fact]
    public void RecalculateProgress_WithAllKeyResultsAtZero_Should_SetStatusToNotStarted()
    {
        // Arrange
        var objective = _objectiveFaker.Generate();
        var keyResults = new List<KeyResult>
        {
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 0).Generate(),
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 0).Generate(),
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 0).Generate()
        };

        // Act
        objective.RecalculateProgress(keyResults);

        // Assert
        objective.Status.Should().Be(Status.NotStarted);
    }

    [Fact]
    public void RecalculateProgress_WithEndDatePassedAndNotCompleted_Should_SetStatusToOverdue()
    {
        // Arrange
        var objective = _objectiveFaker.Clone()
            .RuleFor(o => o.EndDate, DateTime.UtcNow.AddDays(-1)) // Past date
            .Generate();
        
        var keyResults = new List<KeyResult>
        {
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 50).Generate()
        };

        // Act
        objective.RecalculateProgress(keyResults);

        // Assert
        objective.Status.Should().Be(Status.Overdue);
    }

    [Fact]
    public void RecalculateProgress_WithEndDatePassedButCompleted_Should_RemainCompleted()
    {
        // Arrange
        var objective = _objectiveFaker.Clone()
            .RuleFor(o => o.EndDate, DateTime.UtcNow.AddDays(-1)) // Past date
            .Generate();
        
        var keyResults = new List<KeyResult>
        {
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 100).Generate(),
            _keyResultFaker.Clone().RuleFor(kr => kr.Progress, 100).Generate()
        };

        // Act
        objective.RecalculateProgress(keyResults);

        // Assert
        objective.Status.Should().Be(Status.Completed);
    }
}
