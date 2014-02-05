using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

using SAI_Exporter;
using MySql.Data.MySqlClient;
using Nito.AsyncEx;

namespace SAI_Exporter
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }

        static async void MainAsync(string[] args)
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
            string portStr = Console.ReadLine();

            UInt32 port;

            if (!UInt32.TryParse(portStr, out port))
            {
                Console.WriteLine("\n\nThe specified port could not be converted to an unsigned integer. Please try again.\n");
                goto WriteSqlInformation;
            }

            WorldDatabase worldDatabase = new WorldDatabase(host, port, user, pass, worldDB);

            try
            {
                List<SmartScript> smartScripts = await worldDatabase.GetSmartScripts();

                if (smartScripts.Count == 0)
                {
                    Console.WriteLine("\n\nThe smart_script table is empty in your database.\n");
                    return;
                }

                File.Delete("output.sql");

                using (var outputFile = new StreamWriter("output.sql", true))
                {
                    int lastEntryOrGuid = 0;
                    string entryOrGuidSET = "@ENTRY";

                    for (int i = 0; i < smartScripts.Count; ++i)
                    {
                        SmartScript smartScript = smartScripts[i];

                        decimal _i = (decimal)i;
                        decimal count = (decimal)smartScripts.Count;
                        decimal pct = (_i / count) * 100;
                        ProgressBarHelper.RenderConsoleProgress((int)pct, '\u2590', Console.ForegroundColor, ((int)pct).ToString() + "%");

                        string fullLine = String.Empty;
                        int entryorguid = smartScript.entryorguid;
                        SourceTypes sourceType = (SourceTypes)smartScript.source_type;
                        bool isCreatureOrGameobjectGuid = smartScript.entryorguid < 0 && (sourceType == SourceTypes.SourceTypeCreature || sourceType == SourceTypes.SourceTypeGameobject);

                        if (isCreatureOrGameobjectGuid)
                            entryorguid = await worldDatabase.GetObjectIdByGuidAndSourceType(-smartScript.entryorguid, (int)sourceType);

                        if (lastEntryOrGuid != smartScript.entryorguid)
                        {
                            fullLine += "-- " + await worldDatabase.GetObjectNameByIdAndSourceType(entryorguid, (int)sourceType) + " SAI\n";

                            if (isCreatureOrGameobjectGuid)
                            {
                                fullLine += "SET @GUID := " + smartScript.entryorguid + ";\n";
                                entryOrGuidSET = "@GUID";
                            }
                            else
                            {
                                fullLine += "SET @ENTRY := " + smartScript.entryorguid + ";\n";
                                entryOrGuidSET = "@ENTRY";
                            }

                            switch (sourceType)
                            {
                                case SourceTypes.SourceTypeCreature:
                                    fullLine += "UPDATE `creature_template` SET `AIName`=" + '"' + "SmartAI" + '"' + " WHERE `entry`=" + entryOrGuidSET + ";\n";
                                    break;
                                case SourceTypes.SourceTypeGameobject:
                                    fullLine += "UPDATE `gameobject_template` SET `AIName`=" + '"' + "SmartGameObjectAI" + '"' + " WHERE `entry`=" + entryOrGuidSET + ";\n";
                                    break;
                                case SourceTypes.SourceTypeAreaTrigger:
                                    fullLine += "DELETE FROM `areatrigger_scripts` WHERE `entry`=" + entryOrGuidSET + ";\n";
                                    fullLine += "INSERT INTO `areatrigger_scripts` VALUES (" + entryOrGuidSET + "," + '"' + "SmartTrigger" + '"' + ");\n";
                                    break;
                                case SourceTypes.SourceTypeNone:
                                case SourceTypes.SourceTypeScriptedActionlist:
                                    continue;
                            }

                            fullLine += "DELETE FROM `smart_scripts` WHERE `entryorguid`=" + entryOrGuidSET + " AND `source_type`=" + smartScript.source_type + ";\n";
                            fullLine += "INSERT INTO `smart_scripts` (`entryorguid`,`source_type`,`id`,`link`,`event_type`,`event_phase_mask`,`event_chance`,`event_flags`,`event_param1`,`event_param2`,`event_param3`,`event_param4`,`action_type`,`action_param1`,`action_param2`,`action_param3`,`action_param4`,`action_param5`,`action_param6`,`target_type`,`target_param1`,`target_param2`,`target_param3`,`target_x`,`target_y`,`target_z`,`target_o`,`comment`) VALUES\n";
                        }

                        string comment = smartScript.comment.Replace('"', '\'');

                        fullLine += "(" + entryOrGuidSET + "," + smartScript.source_type + "," + smartScript.id + "," + smartScript.link + "," + smartScript.event_type + "," +
                                                        smartScript.event_phase_mask + "," + smartScript.event_chance + "," + smartScript.event_flags + "," + smartScript.event_param1 + "," +
                                                        smartScript.event_param2 + "," + smartScript.event_param3 + "," + smartScript.event_param4 + "," + smartScript.action_type + "," +
                                                        smartScript.action_param1 + "," + smartScript.action_param2 + "," + smartScript.action_param3 + "," + smartScript.action_param4 + "," +
                                                        smartScript.action_param5 + "," + smartScript.action_param6 + "," + smartScript.target_type + "," + smartScript.target_param1 + "," +
                                                        smartScript.target_param2 + "," + smartScript.target_param3 + "," + smartScript.target_x + "," + smartScript.target_y + "," +
                                                        smartScript.target_z + "," + smartScript.target_o + "," + '"' + comment + '"' + ")";

                        if (smartScripts.Count > i + 1)
                        {
                            if (smartScripts[i + 1].entryorguid == smartScript.entryorguid)
                                fullLine += ",";
                            else
                                fullLine += ";\n";
                        }
                        else
                            fullLine += ";\n";

                        outputFile.WriteLine(fullLine);
                        lastEntryOrGuid = smartScript.entryorguid;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("\n\n\n" + ex.Message);
                Console.WriteLine("\nPress 'Enter' to write new database information. Any other key exits the application.\n\n");

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

            Console.WriteLine("\n\n\nThe exporting has finished. If you wish to open the output file with your selected .sql file editor, press Enter.");

            if (Console.ReadKey().Key == ConsoleKey.Enter)
                Process.Start("output.sql");
        }
    }
}
