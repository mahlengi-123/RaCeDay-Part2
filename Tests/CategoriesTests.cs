namespace RaCeDay.Tests;

public class CategoriesControllerTests
{
    private static RaCeDay.Controllers.CategoriesController CreateController() => new(null!, null!);

    [Fact]
    public async Task Edit_Get_NullId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.Edit((int?)null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Get_NullId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.Delete(null);

        Assert.IsType<NotFoundResult>(result);
    }
}
