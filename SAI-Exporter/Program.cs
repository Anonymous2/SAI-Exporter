﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                Console.WriteLine("The specified port could not be converted to an unsigned integer. Please try again.");
                goto WriteSqlInformation;
            }

            WorldDatabase worldDatabase = new WorldDatabase(host, port, user, pass, worldDB);

            try
            {
                List<SmartScript> smartScriptsDistinct = await worldDatabase.GetSmartScriptsDistinct();

                if (smartScriptsDistinct.Count == 0)
                {
                    Console.WriteLine("No smart_script entries were found in your database.");
                    return;
                }

                File.Delete("output.sql");

                using (var outputFile = new StreamWriter("output.sql", true))
                {
                    foreach (SmartScript smartScriptDistinct in smartScriptsDistinct)
                    {
                        string fullLine = String.Empty;
                        int entryorguid = smartScriptDistinct.entryorguid;
                        SourceTypes sourceType = (SourceTypes)smartScriptDistinct.source_type;
                        bool isCreatureOrGameobjectGuid = smartScriptDistinct.entryorguid < 0 && (sourceType == SourceTypes.SourceTypeCreature || sourceType == SourceTypes.SourceTypeGameobject);
                        string entryOrGuidSET = "@ENTRY";

                        if (isCreatureOrGameobjectGuid)
                            entryorguid = await worldDatabase.GetObjectIdByGuidAndSourceType(-smartScriptDistinct.entryorguid, (int)sourceType);

                        fullLine += "-- " + await worldDatabase.GetCreatureNameById(entryorguid) + " SAI\n";

                        if (isCreatureOrGameobjectGuid)
                        {
                            fullLine += "SET @GUID := " + smartScriptDistinct.entryorguid + ";\n";
                            entryOrGuidSET = "@GUID";
                        }
                        else
                            fullLine += "SET @ENTRY := " + smartScriptDistinct.entryorguid + ";\n";

                        fullLine += "DELETE FROM `smart_scripts` WHERE `entryorguid`=" + entryOrGuidSET + " AND `source_type`=" + smartScriptDistinct.source_type + ";\n";
                        fullLine += "INSERT INTO `smart_scripts` (`entryorguid`,`source_type`,`id`,`link`,`event_type`,`event_phase_mask`,`event_chance`,`event_flags`,`event_param1`,`event_param2`,`event_param3`,`event_param4`,`action_type`,`action_param1`,`action_param2`,`action_param3`,`action_param4`,`action_param5`,`action_param6`,`target_type`,`target_param1`,`target_param2`,`target_param3`,`target_x`,`target_y`,`target_z`,`target_o`,`comment`) VALUES\n";
                        
                        List<SmartScript> smartScripts = await worldDatabase.GetSmartScripts(smartScriptDistinct.entryorguid, smartScriptDistinct.source_type);

                        for (int i = 0; i < smartScripts.Count; ++i)
                        {
                            SmartScript smartScript = smartScripts[i];

                            fullLine += "(" + entryOrGuidSET + "," + smartScript.source_type + "," + smartScript.id + "," + smartScript.link + "," + smartScript.event_type + "," +
                                                            smartScript.event_phase_mask + "," + smartScript.event_chance + "," + smartScript.event_flags + "," + smartScript.event_param1 + "," +
                                                            smartScript.event_param2 + "," + smartScript.event_param3 + "," + smartScript.event_param4 + "," + smartScript.action_type + "," +
                                                            smartScript.action_param1 + "," + smartScript.action_param2 + "," + smartScript.action_param2 + "," + smartScript.action_param4 + "," +
                                                            smartScript.action_param5 + "," + smartScript.action_param6 + "," + smartScript.target_type + "," + smartScript.target_param1 + "," +
                                                            smartScript.target_param2 + "," + smartScript.target_param3 + "," + smartScript.target_x + "," + smartScript.target_y + "," +
                                                            smartScript.target_z + "," + smartScript.target_o + "," + '"' + smartScript.comment + '"' + ")";

                            if (i != smartScripts.Count - 1)
                                fullLine += ",\n";
                            else
                                fullLine += ";\n";

                            switch (sourceType)
                            {
                                case SourceTypes.SourceTypeCreature:
                                    break;
                                default:
                                    break;
                            }
                        }

                        outputFile.WriteLine(fullLine);
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

            Console.WriteLine("\n\n\nThe converting has finished. If you wish to open the output file with your selected .sql file editor, press Enter.");

            if (Console.ReadKey().Key == ConsoleKey.Enter)
                Process.Start("output.sql");
        }
    }
}
