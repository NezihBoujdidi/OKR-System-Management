namespace NXM.Tensai.Back.OKR.Application.UnitTests.Common.Extensions;

public class ObjectiveMapperTests
{
    private readonly Faker<CreateObjectiveCommand> _commandFaker;

    public ObjectiveMapperTests()
    {
        _commandFaker = new Faker<CreateObjectiveCommand>()
            .RuleFor(c => c.OKRSessionId, f => f.Random.Guid())
            .RuleFor(c => c.UserId, f => f.Random.Guid())
            .RuleFor(c => c.Title, f => f.Lorem.Sentence(3))
            .RuleFor(c => c.Description, f => f.Lorem.Sentence())
            .RuleFor(c => c.StartedDate, f => f.Date.Recent())
            .RuleFor(c => c.EndDate, f => f.Date.Future())
            .RuleFor(c => c.Status, f => f.PickRandom<Status>())
            .RuleFor(c => c.Priority, f => f.PickRandom<Priority>())
            .RuleFor(c => c.ResponsibleTeamId, f => f.Random.Guid())
            .RuleFor(c => c.IsDeleted, f => f.Random.Bool())
            .RuleFor(c => c.Progress, f => f.Random.Int(0, 100));
    }

    [Fact]
    public void ToEntity_WithValidCommand_Should_MapAllProperties()
    {
        // Arrange
        var command = _commandFaker.Generate();

        // Act
        var entity = command.ToEntity();

        // Assert
        entity.Should().NotBeNull();
        entity.Id.Should().NotBeEmpty();
        entity.OKRSessionId.Should().Be(command.OKRSessionId);
        entity.UserId.Should().Be(command.UserId);
        entity.Title.Should().Be(command.Title);
        entity.Description.Should().Be(command.Description);
        entity.StartedDate.Should().Be(command.StartedDate);
        entity.EndDate.Should().Be(command.EndDate);
        entity.Status.Should().Be(command.Status);
        entity.Priority.Should().Be(command.Priority);
        entity.ResponsibleTeamId.Should().Be(command.ResponsibleTeamId);
        entity.IsDeleted.Should().Be(command.IsDeleted);
        entity.Progress.Should().Be(command.Progress);
        entity.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToEntity_WithNullCommand_Should_ThrowArgumentNullException()
    {
        // Arrange
        CreateObjectiveCommand? command = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => command!.ToEntity());
        exception.ParamName.Should().Be("command");
    }

    [Fact]
    public void ToEntity_Should_GenerateUniqueId()
    {
        // Arrange
        var command1 = _commandFaker.Generate();
        var command2 = _commandFaker.Generate();

        // Act
        var entity1 = command1.ToEntity();
        var entity2 = command2.ToEntity();

        // Assert
        entity1.Id.Should().NotBe(entity2.Id);
    }

    [Fact]
    public void ToEntity_WithNullOptionalProperties_Should_MapCorrectly()
    {
        // Arrange
        var command = _commandFaker.Clone()
            .RuleFor(c => c.Description, (string?)null)
            .RuleFor(c => c.Status, (Status?)null)
            .RuleFor(c => c.Priority, (Priority?)null)
            .Generate();

        // Act
        var entity = command.ToEntity();

        // Assert
        entity.Description.Should().BeNull();
        entity.Status.Should().BeNull();
        entity.Priority.Should().BeNull();
    }
}
