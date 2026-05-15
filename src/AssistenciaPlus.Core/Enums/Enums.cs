namespace AssistenciaPlus.Core.Enums;

/// <summary>Rol d'usuari en el sistema.</summary>
public enum UserRole
{
    /// <summary>Professor: passa assistència als seus grups.</summary>
    Teacher = 1,

    /// <summary>Equip directiu: passa assistència + modifica registres d'altres professors.</summary>
    Management = 2,

    /// <summary>Administratiu: consulta totes les assistències, sense modificar.</summary>
    Administrative = 3
}

/// <summary>Estat d'una sessió d'assistència.</summary>
public enum AttendanceSessionStatus
{
    /// <summary>Sessió generada però no oberta pel professor.</summary>
    Pending = 0,

    /// <summary>Professor ha obert la llista, està entrant dades.</summary>
    Open = 1,

    /// <summary>Llista tancada pel professor o automàticament.</summary>
    Closed = 2,

    /// <summary>Sessió cancel·lada (festiu, excursió...).</summary>
    Cancelled = 3
}

/// <summary>Dia de la setmana (alineats amb DayOfWeek de .NET).</summary>
public enum WeekDay
{
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}
