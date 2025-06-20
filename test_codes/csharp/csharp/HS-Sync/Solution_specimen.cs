using Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Trees;

namespace csharp.HS_Sync
{
    public class Solution_specimen : IComparable<Solution_specimen>
    {
        public static int n = 0;
        public int Id;
        public Tree PickTree;
        public Tree MoveTree;

        private List<DynStacking.HotStorage.DataModel.Performance> Performances;

        public List<double> Scores;
        public double TotalScore = 0;

        private double tbotMod = 1;      // total blocks on time modifier 
        private double batMod = 1;       // blocked arrival time modifier        // problem with these is that the lower two are seconds while the upper one is the number of blocks.
        private double tmMod = 0;        // tardiness mean modifier

        private int maxDepth = 0;



        public Solution_specimen(int id, int depth, List<string> pick, List<string> move) 
        {
            Id = id;
            maxDepth = depth;
            PickTree = new Tree(pick,maxDepth);
            MoveTree = new Tree(move,maxDepth);
            Scores = new List<double>();
            Performances = new List<DynStacking.HotStorage.DataModel.Performance> ();
            n = Math.Max(n,id);
        }

        public Solution_specimen(int depth)
        {
            Id = -1;
            maxDepth = depth;
            PickTree = new Tree(depth);
            MoveTree = new Tree(depth);
            Scores = new List<double>();
            Performances = new List<DynStacking.HotStorage.DataModel.Performance>();
        }

        public Solution_specimen(int depth,Tree pick, Tree move) {
            Id = -1;
            maxDepth = depth;
            PickTree = pick.DeepCopyTree();
            MoveTree = move.DeepCopyTree();
            Scores = new List<double>();
            Performances = new List<DynStacking.HotStorage.DataModel.Performance>();
        }

        public Solution_specimen DeepCopySpecimen() 
        {
            return new Solution_specimen(maxDepth, PickTree,MoveTree);
        }

        public double Pick(Block block, Stack stack, long now) 
        {
            return PickTree.Evaluate(block, stack, now);
        }

        public double Move(Block block, Stack stack, long now)
        {
            return MoveTree.Evaluate(block, stack, now);
        }

        public void PrintSpecimen()
        {
            Console.WriteLine($"Specimen {this.Id}\n\nPick Tree: {PickTree.ToString()}\n\nMove Tree: {MoveTree.ToString()}");
        }

        public void PrintPerformace() {
            double performance = 0;
            Console.WriteLine($"Specimen {Id} had scores:");
            for (int i = 0; i < Scores.Count; i++) 
            { 
                Console.WriteLine ($"{i}: {Scores[i]}");
                performance += Scores[i];
            }
            performance = performance / Scores.Count;
            Console.WriteLine($"Specimen's total score devided by the number of tries is: {performance}.");
        }

        public string StringPerformace()
        {
            double performance = 0;
            string output = "";
            output += ($"Specimen {Id} had scores:");
            for (int i = 0; i < Scores.Count; i++)
            {
                output += ($"\n\n{i}: {Scores[i]}\n");
                performance += Scores[i];
                output += $"craneManipulations_ = {Performances[i].CraneManipulations}\n" +
                    $"serviceLevelMean_ = {Performances[i].ServiceLevelMean}\n" +
                    $"leadTimeMean_ = {Performances[i].LeadTimeMean}\n" +
                    $"deliveredBlocks_ = {Performances[i].DeliveredBlocks}\n" +
                    $"totalBlocksOnTime_ = {Performances[i].TotalBlocksOnTime}\n" +
                    $"blockedArrivalTime_ = {Performances[i].BlockedArrivalTime}" +
                    $"\ntardinessMean_ = {Performances[i].TardinessMean}\n" +
                    $"bufferUtilizationMean_ = {Performances[i].BufferUtilizationMean}\n" +
                    $"craneUtilizationMean_ = {Performances[i].CraneUtilizationMean}\n" +
                    $"handoverUtilizationMean_ = {Performances[i].HandoverUtilizationMean}\n" +
                    $"upstreamUtilizationMean_ = {Performances[i].UpstreamUtilizationMean}\n\n";
            }
            output += ($"Specimen's fitness function is as follows: TotalBlocksOnTime * {tbotMod} - BlockedArrivalTime * {batMod} - Abs(TardinessMean) * {tmMod}\n\n");
            performance = performance / Scores.Count;
            output += ($"Specimen's total score devided by the number of tries is: {performance}.\n");
            return output;
        }

        public int FinishedRuns() {
            return Scores.Count;
        }

        public void AddScore(DynStacking.HotStorage.DataModel.Performance performance)
        {
            if (Id == -1)
            {
                Id = n++;
            }
            Performances.Add(performance);
            double result = (performance.TotalBlocksOnTime * tbotMod) + (-performance.BlockedArrivalTime * batMod) + (-Math.Abs(performance.TardinessMean) * tmMod);
            Scores.Add(result);
        }
        public void SumUp() 
        {
            TotalScore = 0;
            foreach (double result in Scores) 
            {
                TotalScore += result;
            }
        }

        public void ClearScores()
        {
            Performances.Clear();
            Scores.Clear();
            TotalScore = 0;
        }

        public int CompareTo(Solution_specimen other)
        {
            if (this.TotalScore < other.TotalScore) return -1;
            else if (this.TotalScore > other.TotalScore) return 1;

            if (this.Id > other.Id) return -1;  // Newer comes first
            else if (this.Id < other.Id) return 1;
            else return 0;
        }

        public string PrefixNotation() {
            string notation = $"\nID: {Id}\nmaxDepth: {maxDepth}\n";
            notation += $"Pick: {PickTree.PrefixNotation()}\n";
            notation += $"Move: {MoveTree.PrefixNotation()}\n";
            return notation ;
        }
    }
}
