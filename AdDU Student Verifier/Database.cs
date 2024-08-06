using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace AdDU_Student_Verifier
{
    internal class Database
    {
        public Database()
        {
        }

        public static Student GetStudentInfo(string barcode, string code)
        {
            string connectionString = "Data Source=DatabaseServer;Server=SQLCLUS01;Database=ACADEMIC;User Id=mis_dev;Password=@dDUM1SD3P4RTM3nT2O22;";
            Student student = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string sqlQuery = "get_student_status";
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    command.CommandType = CommandType.StoredProcedure;
                    // Define and set the parameter values
                    command.Parameters.AddWithValue("@barcode", barcode);
                    command.Parameters.AddWithValue("@code", code);

                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        student = new Student(
                            reader["Code"].ToString(),
                            reader["Firstname"].ToString(),
                            reader["Lastname"].ToString(),
                            reader["Enrolment Status"].ToString().Equals("2"),
                             //reader["Image"] != null ? (byte[]) reader["Image"] : null,
                             DBNull.Value.Equals(reader["Image"]) ? null : (byte[])reader["Image"],

                            reader["PE Schedule"].ToString()[0],
                            reader["Practicum Schedule"].ToString()[0],
                            reader["Nurse Type C Schedule"].ToString()[0]
                        );

                    }
                    reader.Close();

                    connection.Close();
                    return student;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
        }
        public static string SaveRFIDDATA(string code, string datetime_logged, string gate_entry, string terminal)
        {
            string connectionString = "Data Source=DatabaseServer;Server=SQLCLUS01;Database=verify_system;User Id=mis_dev;Password=@dDUM1SD3P4RTM3nT2O22;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Read the gate_entry from file
                    string filePath = Path.Combine(Application.StartupPath, "technical_logs.txt");
                    string gate = "";
                    if (File.Exists(filePath))
                    {
                        FileAttributes attributes = File.GetAttributes(filePath);
                        if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        {
                            File.SetAttributes(filePath, attributes & ~FileAttributes.Hidden);
                        }

                        try
                        {
                            gate = File.ReadAllText(filePath).Trim();
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            MessageBox.Show($"Access denied to read file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            File.SetAttributes(filePath, attributes);
                        }
                    }

                    // Construct the SQL query to check if the same code was logged within the last 5 minutes and for the same gate_entry
                    string checkQuery = @"
                SELECT COUNT(1) 
                FROM rfidlogs 
                WHERE code = @code 
                AND CAST(datetime_logged AS datetime) >= DATEADD(minute, -5, GETDATE()) 
                AND gate_entry = @gate";

                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@code", code);
                        checkCommand.Parameters.AddWithValue("@gate", gate_entry);

                        int count = (int)checkCommand.ExecuteScalar();

                        if (count > 0)
                        {
                            // Return a string indicating the code exists within the last 5 minutes
                            return "Logged 5 mins ago";
                        }
                        else
                        {
                            // Construct the SQL query to insert a new record if the code wasn't logged within the last 5 minutes
                            string insertQuery = @"
                        INSERT INTO rfidlogs (code, datetime_logged, gate_entry, terminal) 
                        VALUES (@code, @datetime_logged, @gate_entry, @terminal);
                        SELECT SCOPE_IDENTITY();";

                            using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                            {
                                // Add parameters
                                insertCommand.Parameters.AddWithValue("@code", code);
                                insertCommand.Parameters.AddWithValue("@datetime_logged", datetime_logged);
                                insertCommand.Parameters.AddWithValue("@gate_entry", gate_entry);
                                insertCommand.Parameters.AddWithValue("@terminal", terminal);

                                // Execute the SQL command
                                object result = insertCommand.ExecuteScalar();
                                int insertedId = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : -1;
                                return insertedId.ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return "error: " + ex.Message;
                }
            }
        }

        private static long oldLogId = -1;
        public static List<string[]> GateEntrance(string gate_entry)
        {
            string connectionString = "Data Source=DatabaseServer;Server=SQLCLUS01;Database=verify_system;User Id=mis_dev;Password=@dDUM1SD3P4RTM3nT2O22;";
            List<string[]> entry = new List<string[]>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Connection opened successfully.");

                    // Call the logschecker stored procedure to get the latest log ID
                    string logsCheckerQuery = "logschecker"; // Specify the stored procedure name
                    long newLogId = 0; // Changed to long

                    try
                    {
                        using (SqlCommand command = new SqlCommand(logsCheckerQuery, connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@gate_entry", gate_entry);
                            Console.WriteLine("Executing logschecker stored procedure.");

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    newLogId = reader["log"] != DBNull.Value ? Convert.ToInt64(reader["log"]) : 0; // Use Convert.ToInt64
                                    Console.WriteLine("New log ID: " + newLogId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while executing logschecker stored procedure: " + ex.Message);
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                        }
                        Console.WriteLine("Stack Trace: " + ex.StackTrace);
                        throw; // Re-throw the exception to be caught by the outer catch block
                    }

                    // Compare the old log ID with the new one
                    if (oldLogId != newLogId)
                    {
                        // Update the old log ID with the new one
                        oldLogId = newLogId;
                        Console.WriteLine("Log ID changed. Fetching new entries.");

                        // Call the gate_entry_list stored procedure to get the entry list
                        string gateEntryListQuery = "gate_entry_list"; // Specify the stored procedure name

                        using (SqlCommand command = new SqlCommand(gateEntryListQuery, connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@gate_entry", gate_entry);
                            Console.WriteLine("Executing gate_entry_list stored procedure.");

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string lastname = reader["lastname"].ToString();
                                    string firstname = reader["firstname"].ToString();
                                    string middleinitial = reader["middleinitial"].ToString();
                                    string suffix = reader["suffix"].ToString();
                                    string datetime_logged = reader["datetime_logged"].ToString();
                                    byte[] picture = reader["picture"] != DBNull.Value ? (byte[])reader["picture"] : null;
                                    string pictureBase64 = picture != null ? Convert.ToBase64String(picture) : string.Empty;
                                    string enrolmentStatus = reader["ENROLMENTSTATUS"].ToString();
                                    string peSchedule = reader["PE_Schedule"].ToString();
                                    string practicumSchedule = reader["Practicum_Schedule"].ToString();
                                    string nurseTypeCSchedule = reader["Nurse_Type_C_Schedule"].ToString();
                                    string osaViolation = reader["osaviolation"].ToString();
                                    string[] entryArray = new string[]
                                    {
                                firstname,
                                middleinitial,
                                lastname,
                                suffix,
                                datetime_logged,
                                pictureBase64,
                                enrolmentStatus,
                                peSchedule,
                                practicumSchedule,
                                nurseTypeCSchedule,
                                osaViolation,
                                newLogId.ToString(),
                                    };

                                    entry.Add(entryArray);
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Log ID has not changed. No new entries.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                    }
                    Console.WriteLine("Stack Trace: " + ex.StackTrace);
                }
            }

            Console.WriteLine("Total entries returned: " + entry.Count);
            return entry;
        }




    }



}