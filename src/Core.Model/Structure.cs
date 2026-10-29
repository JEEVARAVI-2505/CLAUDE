using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Core.Model;

public sealed class Column
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Mark { get; set; } = "";
    public Vec2 Position { get; set; }
    public double B { get; set; } = 230;
    public double D { get; set; } = 300;

    /// <summary>Factored axial load at base, kN.</summary>
    public double Pu { get; set; }

    /// <summary>Required longitudinal steel, mm².</summary>
    public double AscRequired { get; set; }

    public string Reinforcement { get; set; } = "";
    public string Ties { get; set; } = "";
}

public sealed class Beam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Mark { get; set; } = "";

    /// <summary>0 = plinth beam, n = beam under floor n's slab... roof = number of storeys.</summary>
    public int Level { get; set; }

    public Vec2 Start { get; set; }
    public Vec2 End { get; set; }
    public double B { get; set; } = 230;
    public double D { get; set; } = 380;
    public double Wu { get; set; }
    public double Mu { get; set; }
    public double Vu { get; set; }
    public double AstRequired { get; set; }
    public string Bottom { get; set; } = "";
    public string Top { get; set; } = "";
    public string Stirrups { get; set; } = "";
    public bool Ok { get; set; } = true;
}

public sealed class SlabPanel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Mark { get; set; } = "";
    public int Level { get; set; }
    public Rect2 Bounds { get; set; }
    public double Thickness { get; set; } = 125;
    public bool TwoWay { get; set; }
    public string Reinforcement { get; set; } = "";
}

public sealed class Footing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Mark { get; set; } = "";
    public Guid ColumnId { get; set; }
    public Vec2 Position { get; set; }
    public double L { get; set; }
    public double B { get; set; }
    public double Depth { get; set; }
    public string Reinforcement { get; set; } = "";
}

/// <summary>Bar bending schedule row (IS 2502 shape codes simplified).</summary>
public sealed class BbsRow
{
    public string Member { get; set; } = "";
    public string BarMark { get; set; } = "";
    public string Shape { get; set; } = "";
    public double Dia { get; set; }
    public int Count { get; set; }
    public double CutLength { get; set; }

    /// <summary>Total weight, kg (d²/162 kg/m).</summary>
    public double Weight { get; set; }
}

public sealed class StructureModel
{
    public List<double> GridX { get; set; } = [];
    public List<double> GridY { get; set; } = [];
    public List<Column> Columns { get; set; } = [];
    public List<Beam> Beams { get; set; } = [];
    public List<SlabPanel> Slabs { get; set; } = [];
    public List<Footing> Footings { get; set; } = [];
    public List<BbsRow> Bbs { get; set; } = [];
    public List<string> Notes { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public double SeismicBaseShear { get; set; }
    public double WindPressure { get; set; }
}
