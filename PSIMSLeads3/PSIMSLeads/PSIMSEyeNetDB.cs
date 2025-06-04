using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows.Forms;
using TextBox = System.Windows.Forms.TextBox;

namespace PSIMSLeads;

public class PSIMSEyeNetDB
{
    private static readonly string ConnectionString =
        ConfigurationManager.ConnectionStrings["PSIMSContext"].ConnectionString;

    private Logger _logger;

    public PSIMSEyeNetDB(RichTextBox textLog, Logger logger)
    {
        _logger = logger;
    }

    public void StoreEyeNetQuery(int nAgency, string strUserID, string strStateUserID, string strWSID,
        string strDate, string strKey, string strPlate, string strPlateState,
        string strImage, string strLatitude, string strLongitude, string strQuery)
    {
        var strSQLCommand = $"INSERT INTO [EyeNetQueryLog] ([Agency],[Unit],[QueryDate]," +
                            $"[QueryKey],[User],[StateUser],[Plate],[State],[ImageKey]," +
                            $"[Latitude],[Longitude],[Query],[Remarks]) VALUES (" +
                            $"{nAgency}, {ENDBString(strWSID)}, {ENDBString(strDate)}, {ENDBString(strKey)}, {ENDBString(strUserID)}, {ENDBString(strStateUserID)}, " +
                            $"{ENDBString(strPlate)}, {ENDBString(strPlateState)}, {ENDBString(strImage)}, " +
                            $"{ENDBString(strLatitude)}, {ENDBString(strLongitude)}, {ENDBString(strQuery)}, {ENDBString("")})";
        try
        {
            using (var command = new SqlCommand(strSQLCommand, new SqlConnection(ConnectionString)))
            {
                command.ExecuteNonQuery();
            }
        }
        catch (SqlException ex)
        {
            var strError = "Error inserting into EyeNetQueryLog";
            _logger.LogResponse(strError);
            _logger.LogResponse(strSQLCommand);
            const string strErrorMask = "Database error: {0} - {1}";
            strError = string.Format(strErrorMask, ex.Number, ex.Message);
            _logger.LogResponse(strError);
        }
    }

    public void StoreEyeNetResponse(int nAgency, string strKey, int nSequence, string strWSID,
        string strResult, string strEyeNetWord)
    {
        var strSQLCommand = $"INSERT INTO [EyeNetResponseLog] ([Agency],[Unit],[QueryDate]," +
                            $"[QueryKey],[StateSequence],[StateData],[HitPhrase]) VALUES (" +
                            $"{nAgency}, {ENDBString(strWSID)}, GETDATE(), {ENDBString(strKey)}, {nSequence}, {ENDBString(strResult)}, {ENDBString(strEyeNetWord)})";
        try
        {
            using (var command = new SqlCommand(strSQLCommand, new SqlConnection(ConnectionString)))
            {
                command.ExecuteNonQuery();
            }
        }
        catch (SqlException ex)
        {
            var strError = "Error inserting into EyeNetResponseLog";
            _logger.LogResponse(strError);
            _logger.LogResponse(strSQLCommand);
            const string strErrorMask = "Database error: {0} - {1}";
            strError = string.Format(strErrorMask, ex.Number, ex.Message);
            _logger.LogResponse(strError);
        }
    }

    private string ENDBString(string strArg)
    {
        if (string.IsNullOrEmpty(strArg))
            return "''";
        return $"'{strArg.Replace("'", "''")}'";
    }
}