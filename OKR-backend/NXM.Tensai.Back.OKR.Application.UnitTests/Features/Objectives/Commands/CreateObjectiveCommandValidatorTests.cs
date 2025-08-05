namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Objectives.Commands;

public class CreateObjectiveCommandValidatorTests
{
    private readonly CreateObjectiveCommandValidator _validator;

    public CreateObjectiveCommandValidatorTests()
    {
        _validator = new CreateObjectiveCommandValidator();
    }    [Fact]
    public void Should_Have_Error_When_OKRSessionId_Is_Empty()
    {
        // Arrange
        var command = new CreateObjectiveCommand { OKRSessionId = Guid.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.OKRSessionId));
    }

    [Fact]
    public void Should_Have_Error_When_UserId_Is_Empty()
    {
        // Arrange
        var command = new CreateObjectiveCommand { UserId = Guid.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UserId));
    }

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        // Arrange
        var command = new CreateObjectiveCommand { Title = string.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Title));
    }

    [Fact]
    public void Should_Have_Error_When_Title_Is_Null()
    {
        // Arrange
        var command = new CreateObjectiveCommand { Title = null! };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Title));
    }

    [Fact]
    public void Should_Have_Error_When_Title_Exceeds_MaxLength()
    {
        // Arrange
        var command = new CreateObjectiveCommand { Title = new string('a', 101) };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Title));
    }

    [Fact]
    public void Should_Have_Error_When_Description_Exceeds_MaxLength()
    {
        // Arrange
        var command = new CreateObjectiveCommand { Description = new string('a', 501) };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Description));
    }

    [Fact]
    public void Should_Have_Error_When_StartedDate_Is_After_EndDate()
    {
        // Arrange
        var command = new CreateObjectiveCommand 
        { 
            StartedDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.StartedDate));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        // Arrange
        var command = new CreateObjectiveCommand
        {
            OKRSessionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Valid Title",
            Description = "Valid Description",
            StartedDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(30),
            ResponsibleTeamId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Description_Is_Null()
    {
        // Arrange
        var command = new CreateObjectiveCommand
        {
            OKRSessionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Valid Title",
            Description = null,
            StartedDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(30),
            ResponsibleTeamId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(command.Description));
    }
}
