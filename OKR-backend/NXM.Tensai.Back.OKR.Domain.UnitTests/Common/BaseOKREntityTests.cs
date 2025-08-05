namespace NXM.Tensai.Back.OKR.Domain.UnitTests.Common;

public class BaseOKREntityTests
{
    private readonly Faker<TestOKREntity> _entityFaker;

    public BaseOKREntityTests()
    {
        _entityFaker = new Faker<TestOKREntity>()
            .RuleFor(e => e.Title, f => f.Lorem.Sentence(3))
            .RuleFor(e => e.Description, f => f.Lorem.Sentence())
            .RuleFor(e => e.Status, f => f.PickRandom<Status>())
            .RuleFor(e => e.Priority, f => f.PickRandom<Priority>())
            .RuleFor(e => e.StartedDate, f => f.Date.Recent())
            .RuleFor(e => e.EndDate, f => f.Date.Future());
    }

    [Fact]
    public void BaseOKREntity_Should_Inherit_From_BaseEntity()
    {
        // Arrange & Act
        var entity = new TestOKREntity();

        // Assert
        entity.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void BaseOKREntity_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var entity = _entityFaker.Generate();

        // Assert
        entity.Title.Should().NotBeNullOrEmpty();
        entity.StartedDate.Should().BeBefore(entity.EndDate);
    }

    [Fact]
    public void BaseOKREntity_Should_Allow_Null_Description()
    {
        // Arrange
        var entity = _entityFaker.Generate();

        // Act
        entity.Description = null;

        // Assert
        entity.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(Status.NotStarted)]
    [InlineData(Status.InProgress)]
    [InlineData(Status.Completed)]
    [InlineData(Status.Overdue)]
    public void BaseOKREntity_Should_Accept_All_Status_Values(Status status)
    {
        // Arrange
        var entity = _entityFaker.Generate();

        // Act
        entity.Status = status;

        // Assert
        entity.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.High)]
    [InlineData(Priority.Urgent)]
    public void BaseOKREntity_Should_Accept_All_Priority_Values(Priority priority)
    {
        // Arrange
        var entity = _entityFaker.Generate();

        // Act
        entity.Priority = priority;

        // Assert
        entity.Priority.Should().Be(priority);
    }

    [Fact]
    public void BaseOKREntity_Should_Allow_Null_Status()
    {
        // Arrange
        var entity = _entityFaker.Generate();

        // Act
        entity.Status = null;

        // Assert
        entity.Status.Should().BeNull();
    }

    [Fact]
    public void BaseOKREntity_Should_Allow_Null_Priority()
    {
        // Arrange
        var entity = _entityFaker.Generate();

        // Act
        entity.Priority = null;

        // Assert
        entity.Priority.Should().BeNull();
    }

    // Test implementation of BaseOKREntity for testing purposes
    private class TestOKREntity : BaseOKREntity
    {
    }
}
