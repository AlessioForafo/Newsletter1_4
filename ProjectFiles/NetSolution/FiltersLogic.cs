#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using System.Collections.Generic;
using FTOptix.HMIProject;
using System;
using System.Linq;
using System.Text;
using FTOptix.Alarm;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
#endregion

public class FiltersLogic : BaseNetLogic
{
    public override void Start()
    {
        alarmFilter = new AlarmFilterLogic(Owner);
    }

    public override void Stop()
    {
        alarmFilter.SaveAll();
    }

    [ExportMethod]
    public void Filter(string filterName)
    {
        alarmFilter.ToggleFilterState(filterName);
        alarmFilter.Refresh();
    }

    [ExportMethod]
    public void Refresh()
    {
        alarmFilter.Refresh();
    }

    [ExportMethod]
    public void ClearAll()
    {
        alarmFilter.ClearAll();
        alarmFilter.Refresh();
    }

    private sealed class AlarmFilterLogic
    {
        public AlarmFilterLogic(IUANode owner)
        {
            Owner = owner;
            Query = AlarmWidget.Get("Layout/AlarmsDataGrid").GetVariable("Query");
            newQuery = new StringBuilder(mandatorySQLpart, 1024);
            FilterEditModelLogic.CreateEditModel(Owner, AlarmWidgetEditModel);

            fromEventTimePicker = owner.Get<DateTimePicker>("FromEventTimeDateAndTime");
            toEventTimePicker = owner.Get<DateTimePicker>("ToEventTimeDateAndTime");
            fromSeverityTextBox = owner.Get<TextBox>("FromSeverityTextBox");
            toSeverityTextBox = owner.Get<TextBox>("ToSeverityTextBox");
            noFilterCheckBox = owner.Get<CheckBox>("NoFilterCheckbox");

            filters = InitializeFilterList();
            groupCount = InitializeGroupCount();
            InitializeCheckBoxes();
            InitializeDateTimePickers();
            InitializeTextBoxes();
        }

        public void SaveAll()
        {
            SaveCheckBoxes();
            SaveDateTimePickers();
            SaveTextBoxes();
        }

        public void ToggleFilterState(string uiFilterName)
        {
            if (string.IsNullOrEmpty(uiFilterName))
                return;

            Filter filter = filters.Find(x => x.uiFilterName == uiFilterName);
            if (filter != null)
            {
                filter.uiChecked = !filter.uiChecked;
            }
        }

        public void ClearAll()
        {
            ClearCheckBoxes();
            ClearFilters();
        }

        public void Refresh()
        {
            BuildQuery();
            RefreshQuery();
            NoFilterCheckBoxUpdate();
        }
        
        public IUANode AlarmWidget
        {
            get
            {
                NodeId aliasNodeId = Owner.GetVariable("ModelAlias").Value;
                var alarmWidget = InformationModel.Get(aliasNodeId);
                return alarmWidget ?? throw new CoreConfigurationException("ModelAlias node id not found");
            }
        }

        public IUAObject AlarmWidgetEditModel
        {
            get
            {
                var alarmWidgetEditModel = AlarmWidget.GetObject("AlarmWidgetEditModel");
                return alarmWidgetEditModel ?? throw new CoreConfigurationException("AlarmWidgetEditModel object not found");
            }
        }
        public IUANode Owner { get; }
        public IUAVariable Query { get; private set; }

        private void ClearCheckBoxes()
        {
            foreach (var child in Owner.Children)
            {
                if (child is CheckBox checkbox)
                    checkbox.Checked = false;
            }
        }
        private void ClearFilters()
        {
            foreach (var filter in filters)
            {
                filter.uiChecked = false;
            }
        }

        private void NoFilterCheckBoxUpdate()
        {
            noFilterCheckBox.Checked = !filters.Exists(x => x.uiChecked);
        }

        private List<Filter> InitializeFilterList()
        {
            return new List<Filter>()
            {
                new(){ uiFilterName = TranslateFilterName("FromEventTime"), uiChecked = false, groupID = 1, sqlCondition = "" },
                new(){ uiFilterName = TranslateFilterName("ToEventTime"), uiChecked = false, groupID = 1, sqlCondition = "" },
                new(){ uiFilterName = TranslateFilterName("PriorityUrgent"), uiChecked = false, groupID = 2, sqlCondition = "(Severity >= 751 AND Severity <= 1000)" },
                new(){ uiFilterName = TranslateFilterName("PriorityHigh"), uiChecked = false, groupID = 2, sqlCondition = "(Severity >= 501 AND Severity <= 750)" },
                new(){ uiFilterName = TranslateFilterName("PriorityMedium"), uiChecked = false, groupID = 2, sqlCondition = "(Severity >= 251 AND Severity <= 500)" },
                new(){ uiFilterName = TranslateFilterName("PriorityLow"), uiChecked = false,  groupID = 2, sqlCondition = "(Severity >= 1 AND Severity <= 250)" },
                new(){ uiFilterName = TranslateFilterName("AlarmStateNormalUnacknowledged"), groupID = 3, uiChecked = false, sqlCondition = "(ActiveState = 'False' AND AckedState = 'False')" },
                new(){ uiFilterName = TranslateFilterName("AlarmStateInAlarmActive"), groupID = 3, uiChecked = false, sqlCondition = "ActiveState = 'True'" },
                new(){ uiFilterName = TranslateFilterName("AlarmStateInAlarmAcked"), groupID = 3, uiChecked = false, sqlCondition = "(ActiveState = 'True' AND AckedState = 'True')" },
                new(){ uiFilterName = TranslateFilterName("AlarmStateInAlarmUnacked"), groupID = 3, uiChecked = false, sqlCondition = "(ActiveState = 'True' AND AckedState = 'False')" },
                new(){ uiFilterName = TranslateFilterName("AlarmStateInAlarmConfirmed"), groupID = 3, uiChecked = false, sqlCondition = "(ActiveState = 'True' AND ConfirmedState = 'True')" },
                new(){ uiFilterName = TranslateFilterName("AlarmStateInAlarmUnconfirmed"), groupID = 3, uiChecked = false, sqlCondition = "(ActiveState = 'True' AND ConfirmedState = 'False')" },
                new(){ uiFilterName = TranslateFilterName("ConditionNameHighHigh"), groupID = 4, uiChecked = false, sqlCondition = "CurrentState IN ('High-High','High-High High')" },
                new(){ uiFilterName = TranslateFilterName("ConditionNameHigh"), groupID = 4, uiChecked = false, sqlCondition = "CurrentState IN ('High','High-High High')" },
                new(){ uiFilterName = TranslateFilterName("ConditionNameLow"), groupID = 4, uiChecked = false, sqlCondition = "CurrentState IN ('Low','Low Low-Low')" },
                new(){ uiFilterName = TranslateFilterName("ConditionNameLowLow"), groupID = 4, uiChecked = false, sqlCondition = "CurrentState IN ('Low Low-Low','Low-Low')" },
                new(){ uiFilterName = TranslateFilterName("Severity"), groupID = 5, uiChecked = false, sqlCondition = "" }
            };
        }

        private void InitializeDateTimePickers()
        {
            if (Owner.Get<CheckBox>("FromEventTimeCheckbox").Checked)
                fromEventTimePicker.Value = GetFiltersModelVariable(fromEventTimeBrowseName).Value;
            else
                fromEventTimePicker.Value = DateTime.Now;

            if (Owner.Get<CheckBox>("ToEventTimeCheckbox").Checked)
                toEventTimePicker.Value = GetFiltersModelVariable(toEventTimeBrowseName).Value;
            else
                toEventTimePicker.Value = DateTime.Now;
        }

        private void InitializeTextBoxes()
        {
            if (Owner.Get<CheckBox>("SeverityCheckbox").Checked)
            {
                fromSeverityTextBox.Text = GetFiltersModelVariable(fromSeverityBrowseName).Value;
                toSeverityTextBox.Text = GetFiltersModelVariable(toSeverityBrowseName).Value;
            }
            else
            {
                fromSeverityTextBox.Text = "1";
                toSeverityTextBox.Text = "1000";
            }

        }

        private void InitializeCheckBoxes()
        {
            foreach (var child in Owner.Children)
            {
                if (child is CheckBox checkbox)
                {
                    var isChecked = GetFiltersModelVariable(checkbox.Text).Value;

                    // set CheckBox
                    checkbox.Checked = isChecked;

                    // set filter uiChecked
                    Filter filter = filters.Find(x => x.uiFilterName == checkbox.Text);
                    if (filter != null)
                    {
                        filter.uiChecked = isChecked;
                    }
                }
            }

            NoFilterCheckBoxUpdate();
        }

        private void BuildQuery()
        {
            newQuery.Clear();
            newQuery.Append(mandatorySQLpart);
            bool wasWHEREadded = false;
            string groupStatement;

            for (int i = 1; i <= groupCount; i++)
            {
                groupStatement = BuildStatementForGroup(i);

                if (!string.IsNullOrEmpty(groupStatement))
                {
                    if (!wasWHEREadded)
                    {
                        newQuery.Append(" WHERE ");
                        wasWHEREadded = true;
                    }

                    newQuery.Append(groupStatement);
                    newQuery.Append(" AND ");
                }
            }

            // remove trailing " AND "
            if (wasWHEREadded)
                newQuery.Remove(newQuery.Length - 5, 5);
        }

        private string BuildStatementForGroup(int groupID)
        {
            StringBuilder result = new StringBuilder();
            const string constantOR = ") OR ";
            int activeGroupFiltersCounter = 0;
            bool isFromEventTimeChecked = false;

            foreach (var filter in filters)
            {
                if (filter.uiChecked && filter.groupID == groupID)
                {
                    if (!string.IsNullOrEmpty(filter.sqlCondition))
                    {
                        result.Append(filter.sqlCondition);
                    }
                    else
                    {
                        if (filter.uiFilterName.Equals(TranslateFilterName("FromEventTime")))
                        {
                            isFromEventTimeChecked = true;
                            result.Append("(Time >= \"");
                            result.Append(fromEventTimePicker.Value.ToUniversalTime().ToString("o"));
                            result.Append("\")");
                        }
                        else if (filter.uiFilterName.Equals(TranslateFilterName("ToEventTime")))
                        {
                            if (isFromEventTimeChecked)
                            {
                                // replace ") OR " to " AND "
                                result.Remove(result.Length - constantOR.Length, constantOR.Length);
                                result.Append(" AND ");
                                result.Append("Time < \"");
                                result.Append(toEventTimePicker.Value.ToUniversalTime().AddSeconds(1).ToString("o"));
                                result.Append("\")");
                            }
                            else
                            {
                                result.Append("(Time < \"");
                                result.Append(toEventTimePicker.Value.ToUniversalTime().AddSeconds(1).ToString("o"));
                                result.Append("\")");
                            }
                        }
                        else if (filter.uiFilterName.Equals(TranslateFilterName("Severity")) && Int32.TryParse(fromSeverityTextBox.Text, out int fromSeverity) &&
                                Int32.TryParse(toSeverityTextBox.Text, out int toSeverity))
                        {
                            result.Append("(Severity >= ");
                            result.Append(fromSeverity);
                            result.Append(" AND ");
                            result.Append("Severity <= ");
                            result.Append(toSeverity);
                            result.Append(")");
                        }
                    }

                    result.Append(" OR ");
                    activeGroupFiltersCounter++;
                }
            }

            // remove trailing " OR "
            if (result.Length > 0)
                result.Remove(result.Length - 4, 4);

            if (activeGroupFiltersCounter >= 2)
            {
                result.Insert(0, "(");
                result.Append(")");
            }

            return result.ToString();
        }

        private void RefreshQuery()
        {
            Query.Value = newQuery.ToString();
        }

        private int InitializeGroupCount()
        {
            var groupCounter = 0;
            var groupID = 0;

            foreach (var filter in filters.Select(filter => filter.groupID))
            {
                if (filter != groupID)
                {
                    groupCounter++;
                    groupID = filter;
                }
            }

            return groupCounter;
        }

        private static string TranslateFilterName(string textId)
        {
            return InformationModel.LookupTranslation(new LocalizedText(textId)).Text;
        }

        private void SaveCheckBoxes()
        {
            foreach (var child in Owner.Children)
            {
                if (child is CheckBox checkbox)
                {
                    GetFiltersModelVariable(checkbox.Text).Value = checkbox.Checked;
                }
            }
        }

        private void SaveDateTimePickers()
        {
            GetFiltersModelVariable(fromEventTimeBrowseName).Value = fromEventTimePicker.Value;
            GetFiltersModelVariable(toEventTimeBrowseName).Value = toEventTimePicker.Value;
        }

        private void SaveTextBoxes()
        {
            GetFiltersModelVariable(fromSeverityBrowseName).Value = fromSeverityTextBox.Text;
            GetFiltersModelVariable(toSeverityBrowseName).Value = toSeverityTextBox.Text;
        }

        private IUAVariable GetFiltersModelVariable(string browseName)
        {
            var filtersModel = FilterEditModelLogic.GetEditModel(AlarmWidgetEditModel);
            return filtersModel.GetVariable(browseName) ?? throw new CoreConfigurationException($"FilterModel {browseName} variable not found");
        }

        private class Filter
        {
            public string uiFilterName;
            public bool uiChecked;
            public int groupID;
            public string sqlCondition;
        }

        private readonly StringBuilder newQuery;
        private readonly List<Filter> filters;
        private readonly int groupCount;
        private const string mandatorySQLpart = "SELECT * FROM Model";
        private readonly DateTimePicker fromEventTimePicker;
        private readonly DateTimePicker toEventTimePicker;
        private readonly TextBox fromSeverityTextBox;
        private readonly TextBox toSeverityTextBox;
        private readonly CheckBox noFilterCheckBox;

        private readonly string fromEventTimeBrowseName = "FromEventTime";
        private readonly string toEventTimeBrowseName = "ToEventTime";
        private readonly string fromSeverityBrowseName = "FromSeverity";
        private readonly string toSeverityBrowseName = "ToSeverity";
    }

    private AlarmFilterLogic alarmFilter;
}
