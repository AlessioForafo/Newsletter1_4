#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.MicroController;
using FTOptix.CommunicationDriver;
using FTOptix.TwinCAT;
using FTOptix.CODESYS;
#endregion

public class TimeShelveLogic : BaseNetLogic
{
    public override void Start()
    {
        presetTimeButton = Owner.Get<ToggleButton>("TimedShelve/Layout/DurationButtonsLayout/PresetShelveDurationButton");
        customTimeButton = Owner.Get<ToggleButton>("TimedShelve/Layout/DurationButtonsLayout/CustomTime/CustomTimeShelveButton");

        PresetShelveDurationButtonPressed();
    }

    [ExportMethod]
    public void PresetShelveDurationButtonPressed()
    {
        presetTimeButton.Active = true;
        customTimeButton.Active = false;
    }

    [ExportMethod]
    public void CustomTimeShelveButtonPressed()
    {
        presetTimeButton.Active = false;
        customTimeButton.Active = true;
    }

    private ToggleButton presetTimeButton;
    private ToggleButton customTimeButton;
}
