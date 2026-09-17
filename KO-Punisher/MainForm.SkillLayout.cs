namespace KOPunisher;

public partial class MainForm
{
    private SkillLayoutControl? _skillLayoutEditor;

    private void EnsureSkillLayoutEditor()
    {
        if (_skillLayoutEditor != null) return;
        _skillLayoutEditor = new SkillLayoutControl();
        _skillLayoutEditor.LoadLayout(_settings.ClassType, _settings.SkillLayout);
        _skillLayoutEditor.LayoutChanged += (_, _) =>
        {
            if (_applying) return;
            _settings.SkillLayout = _skillLayoutEditor.GetLayout();
            UpdateCalibratedKeys();
        };
        _skillLayoutEditor.SaveRequested += (_, _) =>
        {
            if (_engine?.IsArmed == true || !_probeTask.IsCompleted || _skillLayoutEditor.Busy) return;
            try
            {
                var draft = ReadUiDraft();
                draft.SkillLayout = _skillLayoutEditor.GetLayout();
                SkillCalibration.Apply(draft, requireComplete: false);
                draft.SaveCurrentToProfile();
                draft.SaveJobDrafts(_settingsPath);
                _settings = draft;
                _skillLayoutEditor.MarkSaved();
                _status.Text = "Skill bar kaydedildi. Eksik saldırı skilleri varsa Başlat engellenir.";
            }
            catch (Exception ex) { _status.Text = "Skill bar kaydedilemedi: " + ex.Message; }
        };
    }

    private void UpdateCalibratedKeys()
    {
        if (_settings.SkillLayout == null) return;
        _settings.ComboPreset = _preset.SelectedItem as string ?? _settings.ComboPreset;
        SkillCalibration.Apply(_settings, requireComplete: false);
        SetKey("ComboKey1", _settings.ComboKey1);
        SetKey("ComboKey2", _settings.ComboKey2);
        SetKey("ComboKey3", _settings.ComboKey3);
        SetKey("MinorPedalKey", _settings.MinorPedalKey);
        SetKey("LightFeetKey", _settings.LightFeetKey);
    }
}
