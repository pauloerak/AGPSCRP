using csharp.HS_Sync;
using csharp.HS_Sync.Factories;
using Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trees;

namespace csharp.HS_Sync
{

    public static class GeneticOP {

        private static int k = 3; // possible variable for size of tournament 

        private static int selectionProcess = 2;  // 1 is for tournamentsInARoundSel1 tournaments in one round and they have 1 winner each (winners become parents), 2 is for tournamentsInARoundSel2 tournaments in a round where the have 2 winners

        private static int tournamentsInARoundSel1 = 2; // DO NOT CHANGE!!! - if you do extensive changes to the code will be needed when making children

        private static int winnersPerRoundSel1 = 1;

        private static int tournamentsInARoundSel2 = 1; // DO NOT CHANGE!!! - if you do extensive changes to the code will be needed when making children

        private static int winnersPerRoundSel2 = 2;

        private static float utilOfPop = 1f;      // between 0 and 1 

        private static double mutationChance = 0.3;
        private static double majorMutationChance = 0.2;

        private static Random random = new Random();

        private static List<Solution_specimen> parents = new List<Solution_specimen>();
        private static List<Solution_specimen> contestants = new List<Solution_specimen>();
        private static int drawn;

        private static double pickFuncChance = 0.9;

        private static int elitism = 1;

        private static bool exclusion = true;

        private static double mutateOnlyOneTreeChance = 0.5;

        private static double crossOnlyOneTreeChance = 0;

        public static List<Solution_specimen> CreateNewGen(List<Solution_specimen> tested, List<Solution_specimen> arena, List<Solution_specimen> passed, int genSize, bool elitist)
        {
            if (elitist) return CreateNewGen1(tested, arena, passed, genSize);
            else return CreateNewGen2(tested, arena, passed, genSize);
        }

        public static List<Solution_specimen> CreateNewGen1(List<Solution_specimen> tested, List<Solution_specimen> arena, List<Solution_specimen> passed, int genSize) 
        {
            exclusion = true;
            List<Solution_specimen> children = new List<Solution_specimen>();
            while (tested.Count > 0)
            {
                if (selectionProcess == 1 && tested.Count - (k * 2) >= genSize * (1 - utilOfPop) && tested.Count / (k * 2) >= 1)     // if the number of specimens in tested is
                                                                                                                                     // bigger than the size of population we
                                                                                                                                     // do not want to use AND if we have enough
                                                                                                                                     // specimens for tournaments we continue
                {
                    parents.Clear();
                    Selection1(tested, new List<Solution_specimen>()); // here I use a new List because I do not want to send loosers to the arena. If you want them to battle in the arena put (tested, arena) and in Generation.cs put wholeArena = false
                    passed.AddRange(parents); // add parents that made it through the selection into new untested
                    List<Solution_specimen> kids = Crossover1(parents); // we make children using crossover
                    int pick;
                    for (int i = 0; i < (k - winnersPerRoundSel1) * 2; i++)
                    {
                        pick = random.Next(kids.Count);
                        children.Add(kids[pick]);
                        kids.RemoveAt(pick);
                    }
                }

                else if (selectionProcess == 2 && tested.Count - k >= genSize * (1 - utilOfPop) && tested.Count / k >= 1)
                {
                    parents.Clear();
                    Selection2(tested, new List<Solution_specimen>());  // here I use a new List because I do not want to send loosers to the arena. If you want them to battle in the arena put (tested, arena) and in Generation.cs put wholeArena = false
                    passed.AddRange(parents); // add parents that made it through the selection into new untested

                    List<Solution_specimen> kids = Crossover2(parents); // we make children using crossover
                    int pick;
                    for (int i = 0; i < k - winnersPerRoundSel2; i++)
                    {
                        pick = random.Next(kids.Count);
                        children.Add(kids[pick]);
                        kids.RemoveAt(pick);
                    }

                }

                else
                {
                    passed.AddRange(tested); // add nonselected specimens to the passed
                    tested.Clear();
                }
            }
            
            return children;
        }

        // This selection process aims to lessen elitsm, only few best specimens continue to the next generation and the rest are children made from the last generation.
        // Specimens can be chosen multiple times for a chance at gentic operations. This change can be later implemented into CreateNewGen1 as well but at a time of writting it
        // is just a part of my experimentation. 
        public static List<Solution_specimen> CreateNewGen2(List<Solution_specimen> tested, List<Solution_specimen> arena, List<Solution_specimen> passed, int genSize)
        {
            exclusion = false;                                                        
            List<Solution_specimen> children = new List<Solution_specimen>();

            tested.Sort();
            passed.AddRange(tested.GetRange(tested.Count - elitism, elitism));

            while (children.Count < genSize - elitism)
            {
                if (selectionProcess == 1)   // if the number of specimens in tested is bigger than the size of population we do not want to use AND if we have enough
                                             // specimens for tournaments we continue
                {
                    parents.Clear();
                    Selection1(tested, tested);
                    List<Solution_specimen> kids = Crossover1(parents); // we make children using crossover
                    int pick;
                    for (int i = 0; i< (k - winnersPerRoundSel1)*2; i++)
                    {
                        pick = random.Next(kids.Count);
                        children.Add(kids[pick]);
                        kids.RemoveAt(pick);
                        if (!(children.Count < genSize - elitism)) break;
                    }
                }

                else if (selectionProcess == 2)
                {
                    parents.Clear();
                    Selection2(tested, tested);
                    List<Solution_specimen> kids = Crossover2(parents); // we make children using crossover
                    int pick;
                    for (int i = 0; i < k - winnersPerRoundSel2;i++)
                    {
                        pick = random.Next(kids.Count);
                        children.Add(kids[pick]);
                        kids.RemoveAt(pick);
                        if (!(children.Count < genSize - elitism)) break;
                    }
                }
            }
            tested.Clear();
            return children;
        }

        public static void Selection1(List<Solution_specimen> population, List<Solution_specimen> arena) // Selection1 will happen in rounds with two tournaments going on
        {
            for (int x = 0; x < tournamentsInARoundSel1; x++) {           // make two selections
                contestants.Clear();
                for (int j = 0; j < k; j++)         // each takes k members 
                {
                    drawn = random.Next(population.Count);
                    contestants.Add(population[drawn]);

                   population.RemoveAt(drawn);

                }
                contestants.Sort();
                parents.Add(contestants[contestants.Count - 1]);
                if (exclusion) contestants.RemoveAt(contestants.Count - 1);
                arena.AddRange(contestants);
            }

        }

        public static void Selection2(List<Solution_specimen> population, List<Solution_specimen> arena) // Selection1 will happen in one tournament with the two winners getting to be parents 
        {
            contestants.Clear();
            for (int j = 0; j < k; j++)         // each takes k members 
            {
                drawn = random.Next(population.Count);
                contestants.Add(population[drawn]);

                population.RemoveAt(drawn);
            }
            contestants.Sort();
            parents.Add(contestants[contestants.Count - 1]);
            parents.Add(contestants[contestants.Count - 2]);
            if (exclusion)
            {
                contestants.RemoveAt(contestants.Count - 1);
                contestants.RemoveAt(contestants.Count - 1);
            }
            arena.AddRange(contestants);
        }


        private static void PrintingPop(List<Solution_specimen> pop)
        {
            foreach (Solution_specimen s in pop)
            {
                Console.Write($"{s.Id}, ");
            }
            Console.WriteLine();
        }

        public static List<Solution_specimen> Crossover1(List<Solution_specimen> parents)
        {
            List<Solution_specimen> children = new List<Solution_specimen>();
            
            for (int i = 0; i < k - winnersPerRoundSel1; i++) // we need to make as many children as there were losers in the tournament
            {
                for (int j = 0; j < tournamentsInARoundSel1; j+=2) // take parents in pairs 
                {
                    Solution_specimen child1 = parents[j % parents.Count].DeepCopySpecimen(); // make the first clone child
                    Solution_specimen child2 = parents[(j+1) % parents.Count].DeepCopySpecimen(); // make the second clone child

                    Cross(child1, child2);       // make them crossover

                    if (random.NextDouble() <= mutationChance)
                    {
                        Mutation(child1);
                    }
                    if (random.NextDouble() <= mutationChance)
                    {
                        Mutation(child2);
                    }

                    children.Add(child1);
                    children.Add(child2);
                }
            }
            return children; 
        }

        public static List<Solution_specimen> Crossover2(List<Solution_specimen> parents)
        {
            List<Solution_specimen> children = new List<Solution_specimen>();

            for (int i = 0; i < k - winnersPerRoundSel2; i += winnersPerRoundSel2) // we need to make as many children as there were losers in the tournament
            {
                Solution_specimen child1 = parents[i % parents.Count].DeepCopySpecimen(); // make the first clone child
                Solution_specimen child2 = parents[(i + 1) % parents.Count].DeepCopySpecimen(); // make the second clone child

                Cross(child1, child2);       // make them crossover

                if (random.NextDouble() <= mutationChance)
                {
                    Mutation(child1);
                }
                if (random.NextDouble() <= mutationChance)
                {
                    Mutation(child2);
                }

                children.Add(child1);
                children.Add(child2);
            }
            return children;
        }

        private static void Cross(Solution_specimen first, Solution_specimen second)
        {
            TreeNode firstSpot = null;
            TreeNode secondSpot = null;
            double flip = random.NextDouble();
            if (flip > crossOnlyOneTreeChance || flip <= crossOnlyOneTreeChance/2)
            {
                PickLoop(ref firstSpot, ref secondSpot, first.PickTree, second.PickTree); // find spots to for them to crossover
                if (firstSpot != null && secondSpot != null) // if we get back compatible spots
                {
                    first.PickTree.Swap(firstSpot, secondSpot, second.PickTree); // we do the crossover
                }
                else // if no compatible spots were found we mutate children
                {
                    Mutate(first.PickTree);
                    Mutate(second.PickTree);
                }
            }

            if (flip > crossOnlyOneTreeChance || (flip > crossOnlyOneTreeChance / 2 && flip <= crossOnlyOneTreeChance)) {
                PickLoop(ref firstSpot, ref secondSpot, first.MoveTree, second.MoveTree); // we do the same for the Move trees as for the Pick trees
                if (firstSpot != null && secondSpot != null)
                {
                    first.MoveTree.Swap(firstSpot, secondSpot, second.MoveTree);
                }
                else
                {
                    Mutate(first.MoveTree);
                    Mutate(second.MoveTree);
                }
            }
        }

        private static void PickLoop(ref TreeNode firstSpot, ref TreeNode secondSpot, Tree first, Tree second)
        {
            int numberOfTries = 30; // number of times that we will try to find compatible spots where to crossover
            for (int i = 0; i < numberOfTries; i++) // we try to find compatible spots to crossover 
            {
                firstSpot = PickNodeRandom(first); // we pick spots/nodes where we want the crossover to happen 
                secondSpot = PickNodeRandom(second);
                if (firstSpot.Depth + secondSpot.DepthOfChildren <= first.maxDepth && secondSpot.Depth + firstSpot.DepthOfChildren <= second.maxDepth) // check if spots are ok for both children
                {
                    break;
                }
                firstSpot = null;
                secondSpot = null;
            }
        }
        private static TreeNode PickNodeRandom(Tree tree, int maxDepth = int.MaxValue)
        {
            TreeNode picked = null;
            List<TreeNode> operations = new List<TreeNode>();
            List<TreeNode> terminals = new List<TreeNode>();
            SortNodes(operations, terminals, tree.root, maxDepth); // we want to sort nodes into operations and terminal nodes to have better selection
            if (random.NextDouble() < pickFuncChance && operations.Count>0) // we pick one eather way by some probability
            {
                picked = operations[random.Next(operations.Count)];
            }
            else
            {
                picked = terminals[random.Next(operations.Count)];
            }
            return picked;
        }

        private static void SortNodes(List<TreeNode> operations, List<TreeNode> terminals, TreeNode current, int maxDepth) // sort nodes into operations and terminal nodes
        {
            if (current is OpNode)
            {
                if (current.Depth <= maxDepth)
                {
                    operations.Add(current);
                }
                SortNodes(operations, terminals, ((OpNode)current).Left, maxDepth);
                SortNodes(operations, terminals, ((OpNode)current).Right, maxDepth);
            }
            else terminals.Add(current);
        }

        public static void Mutation(Solution_specimen child) 
        {
            if(random.NextDouble() <= mutateOnlyOneTreeChance)
            {
                if(random.NextDouble() <= 0.5) Mutate(child.PickTree);
                else Mutate(child.MoveTree);
            }
            else
            {
                Mutate(child.PickTree);
                Mutate(child.MoveTree);
            }  
        }

        private static void Mutate(Tree tree) {

            TreeNode picked = PickNodeRandom(tree); // we pick a random node

            if (random.NextDouble() <= majorMutationChance)
            {
                Tree mutation = new Tree(tree.maxDepth - picked.Depth); // depending on a position of the random node we create a new subtree (mutation)

                tree.Swap(picked, mutation.root, mutation); // we swap the chosen node and the mutation
            }
            else {

                if (picked is FeatureNode)
                {
                    Tree mutation = new Tree(FeatureFactory.GetRandomFeatureNode(picked.Depth, null), tree.maxDepth);
                    tree.Swap(picked, mutation.root, mutation);
                }
                else if (picked is OpNode)
                {
                    Tree mutation = new Tree(OpFactory.GetRandomOperationNode(picked.Depth, null), tree.maxDepth);
                    tree.SwapOPGene((OpNode)picked, (OpNode)mutation.root, mutation);
                }
            }
        }

        public static string getSettings() 
        {
            return $"Settings of GeneticOP:\n" +
                   $"k = {k}\n" +
                   $"selectionProcess = {selectionProcess}\n" +
                   $"tournamentsInARoundSel1 = {tournamentsInARoundSel1}\n" +
                   $"winnersPerRoundSel1 = {winnersPerRoundSel1}\n" +
                   $"tournamentsInARoundSel2 = {tournamentsInARoundSel2}\n" +
                   $"winnersPerRoundSel2 = {winnersPerRoundSel2}\n" +
                   $"utilOfPop = {utilOfPop}\n" +
                   $"mutationChance = {mutationChance}\n" +
                   $"majorMutationChance = {majorMutationChance}\n" +
                   $"pickFuncChance = {pickFuncChance}\n" +
                   $"elitism = {elitism}\n" +
                   $"exclusion = {exclusion}\n" +
                   $"mutateOnlyOneTreeChance = {mutateOnlyOneTreeChance}\n" +
                   $"crossOnlyOneTreeChance = {crossOnlyOneTreeChance}\n" +
                   "\n#####################################################\n\n";
        }
    }
}