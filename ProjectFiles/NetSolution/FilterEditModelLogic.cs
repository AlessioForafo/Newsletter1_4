#region Using directives
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
#endregion

public class FilterEditModelLogic : BaseNetLogic
{
    public static void CreateEditModel(IUANode owner, IUAObject parentNode)
    {
        FilterEditModel.Create(owner, parentNode);
    }

    public static IUAObject GetEditModel(IUAObject parentNode)
    {
        var filterEditModel = parentNode.GetObject(editModelFiltersBrowseName);
        return filterEditModel ?? throw new CoreConfigurationException("Edit model for filters not found");
    }

    public static void DeleteEditModels(IUAObject parentNode)
    {
        FilterEditModel.Delete(parentNode);
    }

    private static class FilterEditModel
    {
        public static void Create(IUANode owner, IUAObject parentNode)
        {
            var editModelFilters = parentNode.FindObject(editModelFiltersBrowseName);
            if (editModelFilters == null)
            {
                editModelFilters = InformationModel.MakeObject(editModelFiltersBrowseName);

                // initalize
                foreach (var child in owner.Children)
                {
                    if (child is CheckBox checkbox)
                    {
                        editModelFilters.Add(InformationModel.MakeVariable(checkbox.Text, OpcUa.DataTypes.Boolean));
                    }
                }
                editModelFilters.Add(InformationModel.MakeVariable(fromEventTimeBrowseName, OpcUa.DataTypes.DateTime));
                editModelFilters.Add(InformationModel.MakeVariable(toEventTimeBrowseName, OpcUa.DataTypes.DateTime));
                editModelFilters.Add(InformationModel.MakeVariable(fromSeverityBrowseName, OpcUa.DataTypes.UInt16)); 
                editModelFilters.Add(InformationModel.MakeVariable(toSeverityBrowseName, OpcUa.DataTypes.UInt16));
                parentNode.Add(editModelFilters);
            }
        }

        public static void Delete(IUAObject parentNode)
        {
            var editModelNetworkInterfaces = parentNode.GetObject(editModelFiltersBrowseName);
            if (editModelNetworkInterfaces != null)
                parentNode.Remove(editModelNetworkInterfaces);
        }
    }

    private static readonly string editModelFiltersBrowseName = "filters";
    private static readonly string fromEventTimeBrowseName = "FromEventTime";
    private static readonly string toEventTimeBrowseName = "ToEventTime";
    private static readonly string fromSeverityBrowseName = "FromSeverity";
    private static readonly string toSeverityBrowseName = "ToSeverity";
}
