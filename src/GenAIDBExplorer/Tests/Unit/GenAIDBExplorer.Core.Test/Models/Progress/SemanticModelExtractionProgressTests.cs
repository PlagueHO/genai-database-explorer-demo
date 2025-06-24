using GenAIDBExplorer.Core.Models.Progress;
using FluentAssertions;

namespace GenAIDBExplorer.Core.Test.Models.Progress;

[TestClass]
public class SemanticModelExtractionProgressTests
{
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var currentStep = 2;
        var totalSteps = 5;
        var currentPhase = "Tables";  
        var message = "Processing tables";
        var estimatedTime = TimeSpan.FromMinutes(5);

        // Act
        var progress = new SemanticModelExtractionProgress(currentStep, totalSteps, currentPhase, message, estimatedTime);

        // Assert
        progress.CurrentStep.Should().Be(currentStep);
        progress.TotalSteps.Should().Be(totalSteps);
        progress.CurrentPhase.Should().Be(currentPhase);
        progress.Message.Should().Be(message);
        progress.EstimatedTimeRemaining.Should().Be(estimatedTime);
    }

    [TestMethod]
    public void Constructor_WithoutEstimatedTime_ShouldSetNullEstimatedTime()
    {
        // Arrange & Act
        var progress = new SemanticModelExtractionProgress(1, 3, "Views", "Processing views");

        // Assert
        progress.EstimatedTimeRemaining.Should().BeNull();
    }

    [TestMethod]
    public void PercentageComplete_WithValidSteps_ShouldCalculateCorrectPercentage()
    {
        // Arrange
        var progress = new SemanticModelExtractionProgress(2, 5, "Phase", "Message");

        // Act
        var percentage = progress.PercentageComplete;

        // Assert
        percentage.Should().Be(40.0);
    }

    [TestMethod]
    public void PercentageComplete_WithZeroTotalSteps_ShouldReturnZero()
    {
        // Arrange
        var progress = new SemanticModelExtractionProgress(1, 0, "Phase", "Message");

        // Act
        var percentage = progress.PercentageComplete;

        // Assert
        percentage.Should().Be(0.0);
    }

    [TestMethod]
    public void PercentageComplete_WithCompletedSteps_ShouldReturn100()
    {
        // Arrange
        var progress = new SemanticModelExtractionProgress(5, 5, "Completed", "All done");

        // Act
        var percentage = progress.PercentageComplete;

        // Assert
        percentage.Should().Be(100.0);
    }

    [TestMethod]
    public void PercentageComplete_WithZeroCurrentStep_ShouldReturnZero()
    {
        // Arrange
        var progress = new SemanticModelExtractionProgress(0, 3, "Starting", "Just started");

        // Act
        var percentage = progress.PercentageComplete;

        // Assert
        percentage.Should().Be(0.0);
    }

    [TestMethod]
    public void PercentageComplete_WithDecimalResult_ShouldReturnCorrectValue()
    {
        // Arrange
        var progress = new SemanticModelExtractionProgress(1, 3, "Phase", "Message");

        // Act
        var percentage = progress.PercentageComplete;

        // Assert
        percentage.Should().BeApproximately(33.333, 0.001);
    }
}