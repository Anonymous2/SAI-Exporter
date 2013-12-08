using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SAI_Exporter
{
    class Program
    {
        static void Main(string[] args)
        {
        WriteSqlInformation:
            Console.WriteLine("SQL Information:");
            Console.Write("Host: ");
            string host = Console.ReadLine();
            Console.Write("User: ");
            string user = Console.ReadLine();
            Console.Write("Pass: ");
            string pass = Console.ReadLine();
            Console.Write("World DB: ");
            string worldDB = Console.ReadLine();
            Console.Write("Port: ");
            string port = Console.ReadLine();

            try
            {
                MySqlConnectionStringBuilder connectionString = new MySqlConnectionStringBuilder();
                connectionString.UserID = user;
                connectionString.Password = pass;
                connectionString.Server = host;
                connectionString.Database = worldDB;
                connectionString.Port = Convert.ToUInt32(port);

                using (var connection = new MySqlConnection(connectionString.ToString()))
                {
                    connection.Open();
                    var returnVal = new MySqlDataAdapter(String.Format("SELECT * FROM smart_scripts ORDER BY entryorguid"), connection);
                    var dataTable = new DataTable();
                    returnVal.Fill(dataTable);

                    if (dataTable.Rows.Count <= 0)
                        return;

                    File.Delete("output.sql");

                    using (var outputFile = new StreamWriter("output.sql", true))
                    {
                        SmartScript smartScriptLink = null;

                        foreach (DataRow row in dataTable.Rows)
                        {
                            SmartScript smartScript = BuildSmartScript(row);
                            MySqlCommand command = new MySqlCommand();
                            command.Connection = connection;

                            string fullLine = String.Empty;

                            switch (smartScript.source_type)
                            {
                                case 0: //! Creature
                                    if (smartScript.entryorguid < 0)
                                        entry = GetCreatureIdByGuid(connection, -smartScript.entryorguid);

                                    //! Event type
                                    fullLine += GetCreatureNameByEntry(connection, entry) + " - ";

                                    fullLine += "-- " + GetCreatureName() + " SAI";
                                    fullLine += "SET @ENTRY := ";
                                    fullLine += "INSERT INTO `smart_scripts` (`entryorguid`,`source_type`,`id`,`link`,`event_type`,`event_phase_mask`,`event_chance`,`event_flags`,`event_param1`,`event_param2`,`event_param3`,`event_param4`,`action_type`,`action_param1`,`action_param2`,`action_param3`,`action_param4`,`action_param5`,`action_param6`,`target_type`,`target_param1`,`target_param2`,`target_param3`,`target_x`,`target_y`,`target_z`,`target_o`,`comment`) VALUES\n";


                                    fullLine += "(@ENTRY," + smartScript.source_type + "," + smartScript.id + "," + smartScript.link + "," + smartScript.event_type + "," +
                                                                    smartScript.event_phase_mask + "," + smartScript.event_chance + "," + smartScript.event_flags + "," + smartScript.event_param1 + "," +
                                                                    smartScript.event_param2 + "," + smartScript.event_param3 + "," + smartScript.event_param4 + "," + smartScript.action_type + "," +
                                                                    smartScript.action_param1 + "," + smartScript.action_param2 + "," + smartScript.action_param2 + "," + smartScript.action_param4 + "," +
                                                                    smartScript.action_param5 + "," + smartScript.action_param6 + "," + smartScript.target_type + "," + smartScript.target_param1 + "," +
                                                                    smartScript.target_param2 + "," + smartScript.target_param3 + "," + smartScript.target_x + "," + smartScript.target_y + "," +
                                                                    smartScript.target_z + "," + smartScript.target_o + "," + '"' + smartScript.comment + '"' + ")";

                                    if (i == smartScripts.Count - 1)
                                        fullLine += ";";
                                    else
                                        fullLine += ",";

                                    fullLine += "\n"; //! White line at end of script to make it easier to select
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("\n\n\n" + ex.Message);
                Console.WriteLine("\nPress 'Enter' to write new database information. Any other key exits the application.\n\n");
                Console.ReadKey();

                if (Console.ReadKey().Key == ConsoleKey.Enter)
                    goto WriteSqlInformation;
                else
                    Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n\n" + ex.Message);
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
                Environment.Exit(0);
                return;
            }

            Console.WriteLine("\n\n\nThe converting has finished. A total of {0} scripts were loaded of which {1} were skipped because their comments already fit the correct codestyle.", totalLoadedScripts, totalSkippedScripts);
            Console.WriteLine("If you wish to open the output file with your selected .sql file editor, press Enter.");

            if (Console.ReadKey().Key == ConsoleKey.Enter)
                Process.Start("output.sql");
        }

        private static SmartScript BuildSmartScript(DataRow row)
        {
            var smartScript = new SmartScript();
            smartScript.entryorguid = row["entryorguid"] != DBNull.Value ? Convert.ToInt32(row["entryorguid"]) : -1;
            smartScript.source_type = row["source_type"] != DBNull.Value ? Convert.ToInt32(row["source_type"]) : 0;
            smartScript.id = row["id"] != DBNull.Value ? Convert.ToInt32(row["id"]) : 0;
            smartScript.link = row["link"] != DBNull.Value ? Convert.ToInt32(row["link"]) : 0;
            smartScript.event_type = row["event_type"] != DBNull.Value ? Convert.ToInt32(row["event_type"]) : 0;
            smartScript.event_phase_mask = row["event_phase_mask"] != DBNull.Value ? Convert.ToInt32(row["event_phase_mask"]) : 0;
            smartScript.event_chance = row["event_chance"] != DBNull.Value ? Convert.ToInt32(row["event_chance"]) : 0;
            smartScript.event_flags = row["event_flags"] != DBNull.Value ? Convert.ToInt32(row["event_flags"]) : 0;
            smartScript.event_param1 = row["event_param1"] != DBNull.Value ? Convert.ToInt32(row["event_param1"]) : 0;
            smartScript.event_param2 = row["event_param2"] != DBNull.Value ? Convert.ToInt32(row["event_param2"]) : 0;
            smartScript.event_param3 = row["event_param3"] != DBNull.Value ? Convert.ToInt32(row["event_param3"]) : 0;
            smartScript.event_param4 = row["event_param4"] != DBNull.Value ? Convert.ToInt32(row["event_param4"]) : 0;
            smartScript.action_type = row["action_type"] != DBNull.Value ? Convert.ToInt32(row["action_type"]) : 0;
            smartScript.action_param1 = row["action_param1"] != DBNull.Value ? Convert.ToInt32(row["action_param1"]) : 0;
            smartScript.action_param2 = row["action_param2"] != DBNull.Value ? Convert.ToInt32(row["action_param2"]) : 0;
            smartScript.action_param3 = row["action_param3"] != DBNull.Value ? Convert.ToInt32(row["action_param3"]) : 0;
            smartScript.action_param4 = row["action_param4"] != DBNull.Value ? Convert.ToInt32(row["action_param4"]) : 0;
            smartScript.action_param5 = row["action_param5"] != DBNull.Value ? Convert.ToInt32(row["action_param5"]) : 0;
            smartScript.action_param6 = row["action_param6"] != DBNull.Value ? Convert.ToInt32(row["action_param6"]) : 0;
            smartScript.target_type = row["target_type"] != DBNull.Value ? Convert.ToInt32(row["target_type"]) : 0;
            smartScript.target_param1 = row["target_param1"] != DBNull.Value ? Convert.ToInt32(row["target_param1"]) : 0;
            smartScript.target_param2 = row["target_param2"] != DBNull.Value ? Convert.ToInt32(row["target_param2"]) : 0;
            smartScript.target_param3 = row["target_param3"] != DBNull.Value ? Convert.ToInt32(row["target_param3"]) : 0;
            smartScript.target_x = row["target_x"] != DBNull.Value ? Convert.ToInt32(row["target_x"]) : 0;
            smartScript.target_y = row["target_y"] != DBNull.Value ? Convert.ToInt32(row["target_y"]) : 0;
            smartScript.target_z = row["target_z"] != DBNull.Value ? Convert.ToInt32(row["target_z"]) : 0;
            smartScript.target_o = row["target_o"] != DBNull.Value ? Convert.ToInt32(row["target_o"]) : 0;
            smartScript.comment = row["comment"] != DBNull.Value ? (string)row["comment"] : String.Empty;
            return smartScript;
        }

        private static string GetCreatureNameByEntry(MySqlConnection connection, int entry)
        {
            MySqlCommand command = new MySqlCommand(String.Format("SELECT name FROM creature_template WHERE entry={0}", entry), connection);
            MySqlDataReader readerSourceName = command.ExecuteReader(CommandBehavior.Default);

            if (readerSourceName.Read())
            {
                string str = readerSourceName[0].ToString();
                readerSourceName.Close();
                return str;
            }

            readerSourceName.Close();
            return String.Empty;
        }

        private static int GetCreatureIdByGuid(MySqlConnection connection, int guid)
        {
            MySqlCommand command = new MySqlCommand(String.Format("SELECT id FROM creature WHERE guid={0}", guid), connection);
            MySqlDataReader readerSourceEntry = command.ExecuteReader(CommandBehavior.Default);

            if (readerSourceEntry.Read())
            {
                int entry = Convert.ToInt32(readerSourceEntry[0]);
                readerSourceEntry.Close();
                return entry;
            }

            readerSourceEntry.Close();
            return -1;
        }
    }
}
