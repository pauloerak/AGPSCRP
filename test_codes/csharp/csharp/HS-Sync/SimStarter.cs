using NetMQ.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace csharp.HS_Sync
{
    public class SimStarter
    {
        // values only for training and testing of application, can and should be changed if using other modes, connections, settings...
        private string simulatorPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "simulation");
        private string simulationProject = "DynStack.SimulationRunner";
        private string baseArgs = $"--sim HS --url tcp://127.0.0.1:8080";
        private string settingsHead = $"--settings";
        private string settingsPathGECCO2022 = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "simulation", "settings", "HS","GECCO2022");
        private string settingsPathGECCO2021 = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "simulation", "settings", "HS", "GECCO2021");
        private string syncurl = $"--syncurl tcp://127.0.0.1:2222 --simulateasync true"; 
        private string id = "658f9b28-6686-40d2-8800-611bd8466215";
        List<string> fileNames;

        private Process simulation = null;
        private int counter = 0;

        public SimStarter(string id)
        {
            this.id = id;
            simulatorPath = Path.GetFullPath(simulatorPath);
            //counter = 1; // I'm counting on someone starting the first sim and continuing from there

            if (Directory.Exists(settingsPathGECCO2022)) // hardcoded
            {
                string[] files = Directory.GetFiles(settingsPathGECCO2022); //hardcoded 

                fileNames = new List<string>();

                foreach (string file in files)
                {
                    fileNames.Add(Path.GetFileName(file));
                }

                // Print the file names
                Console.WriteLine("Files in folder:");
                foreach (var name in fileNames)
                {
                    Console.WriteLine(name);
                }
            }
            else
            {
                Console.WriteLine("Directory does not exist.");
            }
        }
        
        public bool StartAnotherSim(int numOfReps) //return true if after this sim a full roster of settings has been run
        {
            string settings = Path.Combine(settingsPathGECCO2022, fileNames[counter]);
            string fullArgs = $"run --project {simulationProject} {baseArgs} {settingsHead} {settings} {syncurl} --id {id} --numofreps {numOfReps}";

            if (simulation != null)
            {
                Console.WriteLine($"Caught process {simulation.Id}");
                simulation.WaitForExit(); // there is a posibility of the program stopping here if the Exit() has been reached but so far it hasn't happened and there is a chance it won't happen. Look for documentation.

            }

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = fullArgs,
                    WorkingDirectory = simulatorPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            proc.Exited += (sender, args) =>
            {
                try
                {
                    Console.WriteLine($"Child {proc.Id} exited at {DateTime.Now.ToString("yy-MM-dd_HH-mm-ss")} with code: {proc.ExitCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error at {DateTime.Now.ToString("yy-MM-dd_HH-mm-ss")}, reading exit code for PID {proc.Id}: {ex.Message}");
                }
            };

            try
            {
                bool started = proc.Start();
                if (!started)
                {
                    Console.WriteLine("Failed to start child process.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting process: {ex.Message}");
            }

            Console.WriteLine($"{DateTime.Now.ToString("yy-MM-dd_HH-mm-ss")} Started Simulation with PID: {proc.Id} and settings {fileNames[counter]}");
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            simulation = proc;

            if (counter < fileNames.Count-1)
            {
                counter++;
                return false;
            } else counter = 0;
            return true;

        }

        public List<string> GetFileNames() { return fileNames; }
    } 
}
