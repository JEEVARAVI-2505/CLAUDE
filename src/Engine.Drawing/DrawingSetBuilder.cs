using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Drawing;

/// <summary>Builds the full drawing set from the model in one call.</summary>
public static class DrawingSetBuilder
{
    public static IReadOnlyList<DrawingSheet> Build(BuildingModel model)
    {
        var sheets = new List<DrawingSheet> { SiteAndScheduleDrawer.SitePlan(model) };
        sheets.AddRange(model.Storeys.Select(s => PlanDrawer.Draw(model, s)));
        sheets.AddRange(new[] { CardinalSide.N, CardinalSide.E, CardinalSide.S, CardinalSide.W }.Select(side => ElevationDrawer.Draw(model, side)));
        sheets.Add(SectionDrawer.Draw(model));
        if (model.Structure.Columns.Count > 0) sheets.Add(StructuralDrawer.Draw(model));
        sheets.Add(SiteAndScheduleDrawer.Schedules(model));
        return sheets;
    }
}
