namespace AssistenciaPlus.Web.Services;

public record NotificacioModel(
    Guid AlumneId,
    string AlumneNom,
    string GrupNom,
    string Missatge,
    DateTime Moment,
    bool EsCritic);

public class NotificacioService
{
    private readonly List<NotificacioModel> _alertes = [];

    public event Action? OnChange;

    public IReadOnlyList<NotificacioModel> Alertes => _alertes;

    public int NoLlegides => _alertes.Count;

    public void AfegirAlerta(NotificacioModel alerta)
    {
        _alertes.Insert(0, alerta);
        if (_alertes.Count > 20)
            _alertes.RemoveAt(_alertes.Count - 1);
        OnChange?.Invoke();
    }

    public void EsborrarTotes()
    {
        _alertes.Clear();
        OnChange?.Invoke();
    }
}
