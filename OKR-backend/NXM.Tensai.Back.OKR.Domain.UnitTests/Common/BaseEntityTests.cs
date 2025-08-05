namespace NXM.Tensai.Back.OKR.Domain.UnitTests.Common;

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_Should_Generate_Unique_Id_On_Creation()
    {
        // Act
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Assert
        entity1.Id.Should().NotBeEmpty();
        entity2.Id.Should().NotBeEmpty();
        entity1.Id.Should().NotBe(entity2.Id);
    }

    [Fact]
    public void BaseEntity_Should_Set_CreatedDate_To_Current_Time()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var entity = new TestEntity();
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        entity.CreatedDate.Should().BeAfter(beforeCreation);
        entity.CreatedDate.Should().BeBefore(afterCreation);
    }

    [Fact]
    public void BaseEntity_Should_Initialize_IsDeleted_To_False()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void BaseEntity_Should_Initialize_ModifiedDate_To_Null()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.ModifiedDate.Should().BeNull();
    }

    [Fact]
    public void BaseEntity_Should_Allow_Setting_ModifiedDate()
    {
        // Arrange
        var entity = new TestEntity();
        var modifiedDate = DateTime.UtcNow;

        // Act
        entity.ModifiedDate = modifiedDate;

        // Assert
        entity.ModifiedDate.Should().Be(modifiedDate);
    }

    [Fact]
    public void BaseEntity_Should_Allow_Setting_IsDeleted()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.IsDeleted = true;

        // Assert
        entity.IsDeleted.Should().BeTrue();
    }

    // Test implementation of BaseEntity for testing purposes
    private class TestEntity : BaseEntity
    {
    }
}
