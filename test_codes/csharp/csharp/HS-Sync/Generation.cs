using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csharp.HS_Sync
{
    public class Generation
    {
        public List<Solution_specimen> untested;
        public List<Solution_specimen> tested;
        public List<Solution_specimen> arena;
        public List<Solution_specimen> passed;
        public List<Solution_specimen> best;
        public List<Solution_specimen> worst;
        private int genSize = 0;
        private int generation = 0;
        private bool first = true;

        private int genCap = 100;

        private int recordEvery = 1;

        private string statsFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "stats");

        private string popFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "populations");

        private StreamWriter statsWriter;

        private StreamWriter popWriter;

        private StreamReader popReader;

        private string popPath;

        private bool elitist = true;  // do children have a direct entrance to the next gen

        private bool wholeArena = true;  // does everyone in the arena get to go into the pop

        private SimStarter simStarter;

        private List<string> simSettings;

        private int numOfSettings;

        private int settingsCounter = 0;


        public Generation(string Continue, int genSize, int genCap, int depth, SimStarter simStarter) 
        {
            untested = new List<Solution_specimen>();

            this.genSize = genSize;

            if (Continue.Equals(""))
            {
                for (int i = 0; i < genSize; i++)
                {
                    untested.Add(new Solution_specimen(depth));
                }
            }
            else
            {
                LoadPop(Continue);
            }

            this.simStarter = simStarter;
            simSettings = simStarter.GetFileNames();
            numOfSettings = simSettings.Count;
                
            tested = new List<Solution_specimen>();
            arena = new List<Solution_specimen>();
            passed = new List<Solution_specimen>();
            
            this.genCap = genCap;

            best = new List<Solution_specimen>();
            worst = new List<Solution_specimen>();

            string dateTime = DateTime.Now.ToString("yy-MM-dd_HH-mm-ss");
            string fileNameStats = "stats_" + dateTime + $"_popSize_{genSize}_genCap_{genCap}_solDepth_{depth}" + ".txt";
            string fileNamePop = "pop_" + dateTime + $"_popSize_{genSize}_genCap_{genCap}_solDepth_{depth}" + ".txt";
            string statsPath = Path.Combine(statsFolder, fileNameStats);
            popPath = Path.Combine(popFolder, fileNamePop);
            statsWriter = new StreamWriter(statsPath, append: true);
            Log(statsWriter, GeneticOP.getSettings());
            string files = string.Join("\n", simSettings);
            Log(statsWriter, "Simulation settings loaded from:\n" + files + "\n\n#####################################################\n\n");
            Log(statsWriter, "Loaded pop from:\n" + Continue + "\n\n#####################################################\n\n");
        }

        public Solution_specimen GetSpecimen() 
        {
            if (!untested.Any())
            {
                settingsCounter++;
                if (settingsCounter < numOfSettings)
                {
                    untested.AddRange(arena);
                    arena.Clear();

                    simStarter.StartAnotherSim(untested.Count);
                }
                else
                {
                    settingsCounter = 0;

                    if (arena.Any())
                    {
                        foreach (Solution_specimen s in arena)
                        {
                            s.SumUp();
                        }

                        if (first)
                        {
                            tested.AddRange(arena);
                            tested.Sort();
                            arena.Clear();
                            first = false;
                            Console.WriteLine($"Generation: {generation}");
                            ShowGenerationStats();
                            LogStats();
                            LogPop();
                            if (generation >= genCap)
                            {
                                Console.WriteLine("Population i already at or over generation cap. If you wish to continue press Enter.");
                                Console.ReadLine();
                            }
                            NextGeneration();
                        }
                        else
                        {
                            arena.Sort();

                            if (wholeArena) passed.AddRange(arena);
                            else passed.AddRange(arena.GetRange(arena.Count / 2, arena.Count / 2)); // we make a selection between the children and the losers and the better ones go
                            arena.Clear();
                        }
                    }

                    EvolutionStep();

                    if (!tested.Any() && !arena.Any() && !untested.Any())
                    {

                        if (passed.Count != genSize) // implemented failsafe in case some of my math is wrong. THIS SHOULD NOT ACTIVATE IN NORMAL RUNS!!!!
                        {
                            if (passed.Count < genSize)
                            {
                                Console.WriteLine($"Less specimens than expected!!!!! {passed.Count} out of {genSize}, arena: {arena.Count} , tested: {tested.Count} , untested: {untested.Count}");
                                Console.ReadLine();
                            }
                            else
                            {
                                Console.WriteLine($"More specimens than expected!!!!! {passed.Count} out of {genSize}, arena: {arena.Count} , tested: {tested.Count} , untested: {untested.Count}");
                                Console.ReadLine();
                            }
                        }

                        tested.AddRange(passed);  // after we used up all of our tested specimens we can make another generation and start working on that one
                        passed.Clear();
                        tested.Sort();

                        Console.WriteLine($"Generation: {generation}");
                        ShowGenerationStats();

                        if (generation % recordEvery == 0)
                        {
                            LogStats();
                            LogPop();
                        }
                        if (generation == genCap)
                        {
                            Console.WriteLine("Best scores go:\n");
                            foreach (Solution_specimen b in best)
                            {
                                Console.Write($"{b.TotalScore}, ");
                            }
                            Console.WriteLine("Worst scores go:\n");
                            foreach (Solution_specimen b in worst)
                            {
                                Console.Write($"{b.TotalScore}, ");
                            }
                            Console.ReadLine();
                        }
                        NextGeneration();
                        EvolutionStep();
                    }
                    simStarter.StartAnotherSim(untested.Count);
                }
            }
            return untested[0];
        }

        public void SpecimenDone() {
            if(elitist || first || settingsCounter < numOfSettings) arena.Add(untested[0]);
            else passed.Add(untested[0]);
            untested.RemoveAt(0);
        }

        private void EvolutionStep()
        {
            if(tested.Count > 0) untested = GeneticOP.CreateNewGen(tested,arena,passed,genSize,elitist);
        }

        public void ShowGenerationStats()
        {
            Console.WriteLine("Generation scores are as follows:");
            foreach (Solution_specimen specimen in tested) 
            { 
                specimen.PrintPerformace();
            }
        }

        public int GetGeneration() { return generation; }
        public void NextGeneration() { generation++; }

        public void ShowGeneration() 
        { 
            foreach (Solution_specimen specimen in tested) 
            {
                specimen.PrintSpecimen();
                Console.WriteLine();
            }
        }

        private void LogStats() 
        {
            tested.Sort();
            worst.Add(tested[0]);
            best.Add(tested[tested.Count - 1]);
            string stats = $"Gen {generation}:\nBest: {best[best.Count - 1].StringPerformace()}\nWorst: {worst[worst.Count - 1].StringPerformace()}\n\n#######################################################################################################\n";
            Log(statsWriter,stats);
        }

        private void LogPop() 
        {
            popWriter = new StreamWriter(popPath, append: false);
            string log = $"Generation: {generation}";
            foreach (Solution_specimen solution in tested) 
            {
                log += solution.PrefixNotation();
            }
            Log(popWriter,log);
            popWriter.Close();
        }

        private void Log(StreamWriter streamWriter, string line)
        {
            streamWriter.Write(line);
            streamWriter.Flush();
        }

        private void LoadPop(string Continue) 
        {
            string line;
            string[] parts;
            int size = 0;
            string popPath = Path.Combine(popFolder, Continue);
            popReader = new StreamReader(popPath);
            line = popReader.ReadLine();
            parts = line.Split(' ');
            generation = int.Parse(parts[1]);
            while (!popReader.EndOfStream) 
            {
                parts = popReader.ReadLine().Split(" ");
                int id = int.Parse(parts[1]);

                parts = popReader.ReadLine().Split(" ");
                int depth = int.Parse(parts[1]);

                parts = popReader.ReadLine().Split();
                List<string> pick = parts[1..].ToList();

                parts = popReader.ReadLine().Split();
                List<string> move = parts[1..].ToList();
                popReader.ReadLine();

                Solution_specimen solution = new Solution_specimen(id, depth, pick, move);
                untested.Add(solution);

                size++;
            }
            genSize = size;
            popReader.Close();
        }

        public int GetGenSize() {  return genSize; }
    }
}
