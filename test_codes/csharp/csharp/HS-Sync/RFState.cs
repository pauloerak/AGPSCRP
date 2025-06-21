using DynStacking.HotStorage.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Trees;

namespace csharp.HS_Sync {
    public class Block {
        public Block(int id, bool ready, TimeStamp due) {
            Id = id;
            Ready = ready;
            Due = due;
        }
        public int Id { get; }
        public bool Ready { get; }
        public TimeStamp Due { get; }

        public override string ToString()
        {
            return $"[Block {Id}: {(Ready?"Ready":"Unready")}, Due at {Due}\n";

        }
    }

    public class Stack {
        public int Id { get; }
        public int MaxHeight { get; }
        public Stack<Block> Blocks { get; }

        public Stack(DynStacking.HotStorage.DataModel.Stack stack) {
            Id = stack.Id;
            MaxHeight = stack.MaxHeight;
            Blocks = new Stack<Block>(stack.BottomToTop.Select(b => new Block(b.Id, b.Ready, b.Due)));
        }

        public Stack(Handover stack) {
            Id = stack.Id;
            MaxHeight = 1;
            Blocks = new Stack<Block>();
            if (stack.Block != null)
                Blocks.Push(new Block(stack.Block.Id, stack.Block.Ready, stack.Block.Due));
        }

        public Stack(Stack other) {
            Id = other.Id;
            MaxHeight = other.MaxHeight;
            Blocks = new Stack<Block>(other.Blocks.Reverse());
        }

        public Block Top() {
            return Blocks.Count > 0 ? Blocks.Peek() : null;
        }

        public bool IsSorted => Blocks.IsSorted();

        public bool ContainsReady => Blocks.Any(block => block.Ready);
        public bool IsEmpty => Blocks.Count == 0;
        public int Count => Blocks.Count;
        public int BlocksAboveReady() {
            if (Blocks.Any(block => block.Ready)) {
                int blocksOverReady = 0;
                foreach (var block in Blocks.Reverse()) {
                    if (block.Ready)
                        blocksOverReady = 0;
                    else
                        blocksOverReady++;
                }

                return blocksOverReady;
            } else {
                return 0;
            }
        }

        public int BlocksAboveWanted(int Id)
        {
            if (Blocks.Any(block => block.Id==Id))
            {
                int blocksOverWanted = 0;
                foreach (var block in Blocks.Reverse())
                {
                    if (block.Id==Id)
                        blocksOverWanted = 0;
                    else
                        blocksOverWanted++;
                }

                return blocksOverWanted;
            }
            else
            {
                return 0;
            }
        }
        public bool ContainsDueBelow(TimeStamp due) {
            return Blocks.Any(block => block.Due.MilliSeconds < due.MilliSeconds);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Stack {Id} Top\n");
            foreach (Block block in Blocks){
                sb.AppendLine(block.ToString());
            }
            sb.AppendLine($"Stack {Id} Bottom\n");
            return sb.ToString();

        }
    }

    public class RFState {
        public List<CraneMove> Moves { get; }
        private Stack Production { get; set; }
        private List<Stack> Buffers { get; }
        private Stack Handover { get; set; }
        private long WorldStep { get; set; }

        private Solution_specimen Solver;

        public RFState(World world, Solution_specimen specimen) {
            Moves = new List<CraneMove>();
            Production = new Stack(world.Production);
            Handover = new Stack(world.Handover);
            Buffers = new List<Stack>();
            Buffers.AddRange(world.Buffers.Select(buf => new Stack(buf)));
            WorldStep = world.Now.MilliSeconds;

            Solver = specimen;
        }

        public RFState(RFState other) {
            Moves = other.Moves.ToList();
            Handover = new Stack(other.Handover);
            Production = new Stack(other.Production);
            Buffers = new List<Stack>();
            Buffers.AddRange(other.Buffers.Select(buf => new Stack(buf)));
        }

        public void Update(World world, Solution_specimen specimen) 
        { 
            Moves.Clear();
            Production = new Stack(world.Production);
            Handover = new Stack(world.Handover);
            Buffers.Clear();
            Buffers.AddRange(world.Buffers.Select(buf => new Stack(buf)));
            WorldStep = world.Now.MilliSeconds;
            Solver = specimen;
        }

        public Block FindBlock(int id) {
            foreach (var buffer in Buffers) {
                foreach (var block in buffer.Blocks) {
                    if (block.Id == id)
                        return block;
                }
            }
            return null;
        }

        public Stack FindStack(int id)
        {
            foreach (var block in Production.Blocks)
            {
                if (block.Id == id)
                    return Production;
            }
            foreach (var buffer in Buffers)
            {
                foreach (var block in buffer.Blocks)
                {
                    if (block.Id == id)
                        return buffer;
                }
            }
            return null;
        }

        public bool IsSolved => !Production.Blocks.Any() && !NotEmptyStacks.Any();
        IEnumerable<Stack> NotFullStacks => Buffers.Where(b => b.Blocks.Count < b.MaxHeight);
        IEnumerable<Stack> NotEmptyStacks => Buffers.Where(b => b.Blocks.Count > 0);
        IEnumerable<Stack> StacksWithReady => NotEmptyStacks.Where(b => b.Blocks.Any(block => block.Ready));
        bool HandoverReady => !Handover.Blocks.Any();

        public Block RemoveBlock(int stackId) {
            if (stackId == Production.Id)
                return Production.Blocks.Pop();
            else
                return Buffers.First(b => b.Id == stackId).Blocks.Pop();
        }

        public void AddBlock(int stackId, Block block) {
            if (stackId != Handover.Id && stackId != Production.Id) {
                Buffers.First(b => b.Id == stackId).Blocks.Push(block);
            } else {
                // Production should never be a target
                // If handover is the target, pretend the Block dissappears immediatly
            }
        }

        public RFState Apply(CraneMove move) {
            var result = new RFState(this);
            var block = result.RemoveBlock(move.SourceId);
            result.AddBlock(move.TargetId, block);
            result.Moves.Add(move);
            return result;
        }

        ///Main method that combines answers into CraneMoves 
       public List<CraneMove> GetHandSolution()
        {
            List<CraneMove> craneMoves = new List<CraneMove>();

            Block wanted = PickForMove();

            if (wanted != null)
            {
                Stack wantedStack = FindStack(wanted.Id);

                Tuple<List<int>, List<int>> stacksToMoveTo = FreeBlock(wanted, wantedStack);

                for (int i = 0; i < stacksToMoveTo.Item1.Count; i++)
                {
                    craneMoves.Add(new CraneMove
                    {
                        SourceId = wantedStack.Id,
                        TargetId = stacksToMoveTo.Item2[i],
                        Sequence = 0,
                        BlockId = stacksToMoveTo.Item1[i]
                    });
                }
                if (wanted.Ready)
                {
                    craneMoves.Add(new CraneMove
                    {
                        SourceId = wantedStack.Id,
                        TargetId = Handover.Id,
                        Sequence = 0,
                        BlockId = wanted.Id
                    });
                }
                else
                {
                    if (wanted == Production.Top())
                    {
                        craneMoves.Add(new CraneMove
                        {
                            SourceId = wantedStack.Id,
                            TargetId = PickStackToMoveTo(wantedStack).Id,
                            Sequence = 0,
                            BlockId = wantedStack.Top().Id
                        });
                    }
                }
            }
            return craneMoves;
        }
       public List<CraneMove> GetGPSolution() {
            List<CraneMove> craneMoves = new List<CraneMove>();

            Block wanted = TreePickForMove();


            if (wanted != null)
            {
                Stack wantedStack = FindStack(wanted.Id);

                Tuple<List<int>, List<int>> stacksToMoveTo = TreeFreeBlock(wanted, wantedStack);

                for (int i = 0; i < stacksToMoveTo.Item1.Count; i++)
                {
                    craneMoves.Add(new CraneMove
                    {
                        SourceId = wantedStack.Id,
                        TargetId = stacksToMoveTo.Item2[i],
                        Sequence = 0,
                        BlockId = stacksToMoveTo.Item1[i]
                    });
                }

                if (wanted == Production.Top())
                {
                    craneMoves.Add(new CraneMove
                    {
                        SourceId = wantedStack.Id,
                        TargetId = TreePickStackToMoveTo(wanted, wantedStack).Id,
                        Sequence = 0,
                        BlockId = wantedStack.Top().Id
                    });
                }
                else
                {
                    if (wanted.Ready)
                    {
                        craneMoves.Add(new CraneMove
                        {
                            SourceId = wantedStack.Id,
                            TargetId = Handover.Id,
                            Sequence = 0,
                            BlockId = wanted.Id
                        });
                    }
                }
            }

            return craneMoves;
        }
        
        ///Method for picking which block to move
        public Block PickForMove() {
            Block wanted = null;
            List<Block> possible = new List<Block>();
            possible.AddRange(Production.Blocks.Where(block => block.Ready==true));
            foreach (Stack stack in Buffers) {
                foreach (Block block in stack.Blocks) {
                    if (block.Ready == true) possible.Add(block);
                }
            }
            if (possible.Count == 0)
            {
                if (Production.Top() != null)
                {
                    wanted = Production.Top();
                }
            }
            else {
                wanted = possible.OrderBy(block => block.Due.MilliSeconds).FirstOrDefault();
            }
            return wanted;
        }

        public Block TreePickForMove() 
        {
            Block wanted = null;
            double best = double.MinValue;
            double value;
            foreach (Stack stack in Buffers)
            {
                foreach (Block block in stack.Blocks)
                {
                    if (block.Ready)
                    {
                        value = Solver.Pick(block, stack, WorldStep);
                        if (value > best)
                        {
                            best = value;
                            wanted = block;
                        }
                    }
                }
            }
            foreach (Block block in Production.Blocks)
            {
                value = Solver.Pick(block, Production, WorldStep);

                if (value > best)
                {
                    best = value;
                    wanted = block;
                }
            }

            return wanted;
        }

        ///Method that returns stacks for moving abstructing blocks and ID-s of abstructing blocks
        public Tuple<List<int>,List<int>> FreeBlock(Block wanted,Stack wantedStack) {
            //possible break point 
            int obstructions = wantedStack.BlocksAboveWanted(wanted.Id);
            List<int> blocksToMove = new List<int>();
            List<int> stacksToMoveTo = new List<int>();

            for (int i = 0; i < obstructions; i++)
            {
                if (wantedStack.Blocks.Peek().Ready)
                {
                    stacksToMoveTo.Add(Handover.Id);
                    blocksToMove.Add(wantedStack.Blocks.Peek().Id);
                    Handover.Blocks.Push(wantedStack.Blocks.Pop());
                }
                else
                {
                    Stack target = PickStackToMoveTo(wantedStack);
                    if (target != null)
                    {
                        stacksToMoveTo.Add(target.Id);
                        blocksToMove.Add(wantedStack.Blocks.Peek().Id);
                        target.Blocks.Push(wantedStack.Blocks.Pop());
                    }
                    else break;
                }
            }
            return new Tuple<List<int>,List<int>>(blocksToMove,stacksToMoveTo) ;
        }

        ///Method that returns stacks for moving abstructing blocks and ID-s of abstructing blocks
        public Tuple<List<int>, List<int>> TreeFreeBlock(Block wanted, Stack wantedStack)
        {
            //possible break point 
            int obstructions = wantedStack.BlocksAboveWanted(wanted.Id);
            List<int> blocksToMove = new List<int>();
            List<int> stacksToMoveTo = new List<int>();
            Block moving = null;

            for (int i = 0; i < obstructions; i++)
            {
                moving = wantedStack.Blocks.Peek();
                Stack target = TreePickStackToMoveTo(moving,wantedStack);
                if (target != null)
                {
                    blocksToMove.Add(moving.Id);
                    stacksToMoveTo.Add(target.Id);
                    target.Blocks.Push(wantedStack.Blocks.Pop());
                }
                else break;
            }
            return new Tuple<List<int>, List<int>>(blocksToMove, stacksToMoveTo);
        }

        ///Method that returns ID of a Stack that we want to move a block to
        public Stack PickStackToMoveTo(Stack wanted)
        {
            Stack target = null;
            int min = Buffers[0].MaxHeight;
            foreach (Stack stack in Buffers) 
            {
                if(!new[] { Production.Id, Handover.Id, wanted.Id }.Contains(stack.Id) && min >= stack.Blocks.Count)
                {
                    target = stack;
                    min = stack.Blocks.Count;
                }
            }
            return target;
        }

        ///Method that returns ID of a Stack that we want to move a block to
        public Stack TreePickStackToMoveTo(Block block, Stack stack)
        {
            Stack target = null;
            double best = double.MinValue;
            double value;
            foreach (Stack s in Buffers)
            {
                if (s.Id != Production.Id && s.Id!=stack.Id)
                {
                    value = Solver.Move(block, s, WorldStep);
                    if (value > best)
                    {
                        best = value;
                        target = s;
                    }
                }
            }
            return target;
        }

        ///Method for showing state of the world
        public void ShowState() {
            Console.WriteLine($"Currenty it is:{WorldStep}");
            Console.WriteLine($"Production {Production.ToString()}");
            foreach (Stack stack in Buffers) {
                Console.WriteLine($"{stack.ToString()}");
            }
            Console.WriteLine($"Handover {Handover.ToString()}");
        }
    }

    public static class Extensions {

        public static bool IsSorted(this Stack<Block> stack) {
            // is technically wrong but otherwise empty stacks are avoided
            if (stack.Count == 0)
                return false;
            else if (stack.Count < 2) {
                return true;
            }

            var aux = new Stack<Block>();
            aux.Push(stack.Pop());

            while (stack.Count > 0 && stack.Peek().Due.MilliSeconds > aux.Peek().Due.MilliSeconds) {
                aux.Push(stack.Pop());
            }

            var sorted = stack.Count == 0;

            while (aux.Count > 0)
                stack.Push(aux.Pop());

            return sorted;
        }

        public static double StdDev(this IEnumerable<int> values) {
            double ret = 0;
            int count = values.Count();
            if (count > 1) {
                double avg = values.Average();
                double sum = values.Sum(i => (i - avg) * (i - avg));

                ret = Math.Sqrt(sum / count);
            }

            return ret;
        }

        public static string FormatOutput(this List<CraneMove> list) {
            string ret = "[\n";
            foreach (var move in list) {
                ret += $"\t{move.FormatOutput()}\n";
            }
            return ret + "]";
        }

        public static string FormatOutput(this CraneMove move) {
            return $"Move Block {move.BlockId} from {move.SourceId} to {move.TargetId}";
        }

        public static string FormatOutput(this List<DynStacking.HotStorage.DataModel.Block> blocks) {
            string ret = "{";

            foreach (var block in blocks) {
                ret += $"{block.FormatOutput()}, ";
            }

            return ret + "}";
        }

        public static string FormatOutput(this DynStacking.HotStorage.DataModel.Block block) {
            if (block == null)
                return "";
            return $"B{block.Id}: {(block.Ready ? "R" : "N")}";
        }

        public static string FormatOutput(this World world) {
            string ret = "World {\n";
            ret += $"\tProduction: {world.Production.BottomToTop.ToList().FormatOutput()}\n";
            foreach (var buffer in world.Buffers) {
                ret += $"\tBuffer {buffer.Id} ({buffer.BottomToTop.Count}/{buffer.MaxHeight}): {buffer.BottomToTop.ToList().FormatOutput()}\n";
            }
            ret += $"\tHandover: {world.Handover.Block.FormatOutput()}\n";

            return ret + "}";
        }

        public static List<CraneMove> ConsolidateMoves(this List<CraneMove> moves) {
            List<CraneMove> cleanList = new List<CraneMove>();

            foreach (var move in moves) {
                var similarMoves = cleanList.Where(m => m.BlockId == move.BlockId && m.TargetId == move.SourceId);
                similarMoves.ToList().ForEach(m => m.TargetId = move.SourceId);
                if (similarMoves.Count() == 0)
                    cleanList.Add(move);
            }

            return cleanList;
        }
    }
}
