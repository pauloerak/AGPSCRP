using DynStacking;
using DynStacking.HotStorage.DataModel;
using Google.Protobuf;
using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trees;
using System.Diagnostics;
using System.IO;

namespace csharp.HS_Sync {
    public class SyncHSPlanner : IPlanner {
        private int seqNr = 0;
        private Solution_specimen specimen;
        private RFState initial = null;
        private SimStarter simStarter;

        private Generation generation;
        private bool readyForNext = false;
        private int popSize = 0;
        private bool testing = false;
        private bool handbuilt = false;

        string sim_id = "658f9b28-6686-40d2-8800-611bd8466215";
        public SyncHSPlanner(string Continue, string id, bool test, bool handbuilt, int popSize = 1000, int nGen = 50, int maxDepth = 5) {

            sim_id = id;

            testing = test;

            this.handbuilt = handbuilt;

            if (!handbuilt)
            {
                simStarter = new SimStarter(id);

                generation = new Generation(Continue, popSize, nGen, maxDepth, simStarter);

                this.popSize = generation.GetGenSize();

                specimen = generation.GetSpecimen();
                Console.WriteLine($"Testing: {testing}");
                if (!testing)
                {
                    SelfTraining();
                }
            }
            else Console.WriteLine($"Testing handbuilt solution");

        }
        public byte[] PlanMoves(byte[] worldData, OptimizerType opt) {
            return PlanMoves(World.Parser.ParseFrom(worldData), opt)?.ToByteArray();
        }


        private CraneSchedule PlanMoves(World world, OptimizerType opt) {
            if (world.Buffers == null || (world.Crane.Schedule.Moves?.Count ?? 0) > 0) {
                if (world.Buffers == null)
                    Console.WriteLine($"Cannot calculate, incomplete world.");
                else
                    //Console.WriteLine($"Crane already has {world.Crane.Schedule.Moves?.Count} moves");
                    return null;
            }

            var schedule = new CraneSchedule() { SequenceNr = seqNr++ };

            if (initial == null)
            {
                initial = new RFState(world, specimen);
            }
            else { initial.Update(world, specimen); }

            List<CraneMove> solution = new List<CraneMove>();

            if (handbuilt)
            {
                solution = initial.GetHandSolution();
            }
            else solution = initial.GetGPSolution();

            List<CraneMove> list = solution.ConsolidateMoves();

            if (solution != null)
                schedule.Moves.AddRange(list.Take(3)
                                .TakeWhile(move => world.Handover.Ready || move.TargetId != world.Handover.Id));
        

            if (schedule.Moves.Count > 0) {
                return schedule;
            } else {
                return null;
            }
        }

        public void EndSounded(byte[] worldData)
        {
            EndSounded(World.Parser.ParseFrom(worldData));
        }

        
        private void EndSounded(World world)
        {
            specimen.AddScore(world.KPIs);
            generation.SpecimenDone();
            specimen = generation.GetSpecimen();
        }

        public void SelfTraining() {
            readyForNext = simStarter.StartAnotherSim(popSize);
        }
    }
}
