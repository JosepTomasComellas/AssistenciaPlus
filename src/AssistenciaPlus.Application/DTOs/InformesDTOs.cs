namespace AssistenciaPlus.Application.DTOs;

public record InformeMensualDto
{
    public string AnyAcademic { get; init; } = string.Empty;
    public int Any { get; init; }
    public int Mes { get; init; }
    public string? GrupNom { get; init; }
    public string? CicleNom { get; init; }
    public int TotalAlumnes { get; init; }
    public List<InformeAlumneDto> Alumnes { get; init; } = [];
}

public record InformeTrimestralDto
{
    public string AnyAcademic { get; init; } = string.Empty;
    public int Trimestre { get; init; }
    public DateOnly DataInici { get; init; }
    public DateOnly DataFi { get; init; }
    public string? GrupNom { get; init; }
    public string? CicleNom { get; init; }
    public int TotalAlumnes { get; init; }
    public List<InformeAlumneDto> Alumnes { get; init; } = [];
}

public record InformeAlumneDto
{
    public Guid AlumneId { get; init; }
    public string AlumneNom { get; init; } = string.Empty;
    public string GrupNom { get; init; } = string.Empty;
    public int TotalFrangesPossibles { get; init; }
    public int FrangesPresents { get; init; }
    public int FrangesAbsents { get; init; }
    public int FrangesTard { get; init; }
    public int FrangesAbsenciaParcial { get; init; }
    public decimal PercentatgeAbsencia { get; init; }
    public bool EsAlerta { get; init; }
    public bool EsCritic { get; init; }
    public Dictionary<string, int> DetallMotius { get; init; } = [];
}

public record EnviarInformeDto
{
    public Guid GrupId { get; init; }
    public int Any { get; init; }
    public int Mes { get; init; }
    public bool IncloureAdjuntPdf { get; init; } = true;
}

public record EnviarInformeTrimestralDto
{
    public Guid GrupId { get; init; }
    public int Trimestre { get; init; }
    public Guid AnyAcademicId { get; init; }
}
