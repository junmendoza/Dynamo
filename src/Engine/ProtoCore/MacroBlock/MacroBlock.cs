

using System;
using System.Text;
using System.Collections.Generic;
using ProtoCore.Utils;
using ProtoCore.DSASM;
using ProtoCore.AST.AssociativeAST;

namespace ProtoCore.Runtime 
{
    public class MacroBlock
    {
        public enum ExecuteState
        {
            NotReady,
            Ready,
            Executing,
            Done,
            Paused
        }

        public int UID { get; set; }
        public ExecuteState State { get; set; }
        public AssociativeGraph.GraphNode InputGraphNode { get; set; }
        public List<AssociativeGraph.GraphNode> GraphNodeList { get; set; }

        public MacroBlock(int ID)
        {
            UID = ID;
            State = ExecuteState.NotReady;
            InputGraphNode = null;
            GraphNodeList = new List<AssociativeGraph.GraphNode>();
        }

        /// <summary>
        /// Generates and returns the entrypoint pc of the macroblock
        /// </summary>
        /// <returns></returns>
        public int GenerateEntryPoint()
        {
            int entryPoint = Constants.kInvalidPC;
            if (GraphNodeList.Count > 0)
            {
                AssociativeGraph.GraphNode entryGraphNode = GraphNodeList[0];
                if (entryGraphNode.isDirty)
                {
                    entryPoint = entryGraphNode.updateBlock.startpc;
                }
            }
            return entryPoint;
        }

        /// <summary>
        /// Checks if inputNode already exists in the groupList
        /// Compares the LHS of inputNode with the LHS of the first node in a group
        ///     Given:
        ///         inputNode: a = 1
        ///         groupList: {{a = 1, a = 2}, {b = 3}}
        ///     Here, inputNode exists in groupList
        /// </summary>
        /// <param name="inputNode"></param>
        /// <param name="groupList"></param>
        /// <returns></returns>
        private bool DoesGraphNodeExistInGroupList(AssociativeGraph.GraphNode inputNode, List<List<AssociativeGraph.GraphNode>> groupList)
        {
            foreach (List<AssociativeGraph.GraphNode> group in groupList)
            {
                if (AssociativeEngine.Utils.AreLHSEqual(inputNode, group[0]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Group a list of graphnodes according to their LHS
        ///     Given:
        ///         a = 1
        ///         a = 2
        ///         b = 3
        ///         
        ///     Grouping:
        ///           {a = 1, a = 2}
        ///           {b = 3}
        /// </summary>
        /// <param name="graphNodeList"></param>
        /// <returns></returns>
        public List<List<AssociativeGraph.GraphNode>> GetGraphNodeGroups(List<AssociativeGraph.GraphNode> graphNodeList)
        {
            List<List<AssociativeGraph.GraphNode>> groupList = new List<List<AssociativeGraph.GraphNode>>();

            // Generate a group for every node in graphNodeList
            foreach (AssociativeGraph.GraphNode inputNode in graphNodeList)
            {
                if (!DoesGraphNodeExistInGroupList(inputNode, groupList))
                {
                    groupList.Add(GetGroupForGraphNode(inputNode, graphNodeList));
                }
            }
            return groupList;
        }

        /// <summary>
        /// Gets all the nodes in graphNodeList where the LHS is similar to inputNode
        /// </summary>
        /// <param name="inputNode"></param>
        /// <param name="graphNodeList"></param>
        /// <returns></returns>
        private List<AssociativeGraph.GraphNode> GetGroupForGraphNode(AssociativeGraph.GraphNode inputNode, List<AssociativeGraph.GraphNode> graphNodeList)
        {
            List<AssociativeGraph.GraphNode> group = new List<AssociativeGraph.GraphNode>();
            foreach (AssociativeGraph.GraphNode graphNode in graphNodeList)
            {
                if (AssociativeEngine.Utils.AreLHSEqual(inputNode, graphNode))
                {
                    group.Add(graphNode);
                }
            }
            return group;
        }

        /// <summary>
        /// If at least one graphnode has executed(is not dirty) then the group is ready
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private bool IsGroupReady(List<AssociativeGraph.GraphNode> group)
        {
            foreach (AssociativeGraph.GraphNode graphNode in group)
            {
                if (!graphNode.isDirty)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// A macrobock is dirty if at least one of its graphnode is dirty
        /// </summary>
        /// <returns></returns>
        public bool IsDirty()
        {
            foreach (AssociativeGraph.GraphNode graphNode in GraphNodeList)
            {
                if (graphNode.isDirty)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Inspect the operands of the macroblock to see if it is ready to execute
        /// </summary>
        /// <returns></returns>
        public bool AreOperandsReady()
        {
            // A direct input is a node that is not dependent on any other node value, such as a constant assignment
            //      a = 1 <- this is a directinput node
            bool isDirectInput = InputGraphNode.ParentNodes.Count == 0;
            if (isDirectInput)
            {
                return true;
            }

            List<List<AssociativeGraph.GraphNode>> graphNodeGroup = GetGraphNodeGroups(InputGraphNode.ParentNodes);

            // The nodes operands (parentnodes) must be checked if they are dirty.
            // If at least one operand is dirty, then it means it hasnt executed yet and the node is not ready 
            //      c = a + b <- This is an input node and we must check if 'a' and 'b' have been executed

            // Handle the case where there are multiple parents, but at least one of them already satisfies the condition
            //      a = 1;
            //      b = a; <- the parents of 'b' are 'a = 1' and 'a = 2'. If either one of these executes then macroblock 'b = a' is ready
            //      a = 2;

            foreach (List<AssociativeGraph.GraphNode> group in graphNodeGroup)
            {
                // A group is ready if at least one of it has executed
                if (!IsGroupReady(group))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

namespace ProtoCore
{
    /// <summary>
    /// Generates macroblocks from a list of ASTs
    /// </summary>
    public class MacroBlockGenerator
    {
        private enum MacroblockGeneratorType
        {
            Default,
            NumTypes
        }

        public List<ProtoCore.Runtime.MacroBlock> RuntimeMacroBlockList { get; private set; }

        public MacroBlockGenerator()
        {
            RuntimeMacroBlockList = new List<Runtime.MacroBlock>();
        }

        /// <summary>
        /// Check if the graphnode is the start of a new macroblock
        /// </summary>
        /// <param name="graphnode"></param>
        /// <returns></returns>
        private bool IsMacroblockEntryPoint(AssociativeGraph.GraphNode graphnode)
        {
            Validity.Assert(graphnode != null);

            //
            // Determining if the graphnode is an entrypoint (the start of a new macroblock)
            //      * A graphnode that has no dependency (a constant assignment)
            //      * A graphnode that has at least 2 dependencies (a = b + c)
            //
            //      NoDependent = graphnode.dependentList.Count == 0
            //      HasMoreThanOneDependent = graphnode.dependentList.Count > 1
            //      IsEntryPoint = NoDependent or HasMoreThanOneDependent
            //
            bool hasNoDependency = graphnode.ParentNodes.Count == 0;
            bool hasMoreThanOneDependent = graphnode.ParentNodes.Count > 1;
            bool isEntryPoint = hasNoDependency || hasMoreThanOneDependent;
            return !graphnode.isReturn && isEntryPoint;
        }

        /// <summary>
        /// A node that diverges means that the node is connected to 2 or more nodes
        /// Here, 'a' diverges to 'b' and 'c'
        ///     a = 1
        ///     b = a
        ///     c = a
        ///     
        ///     a = 1 <- An input
        ///     b = a <- An input because it has a sibling 'c'
        ///     c = a <- An input because it has a sibling 'b'
        ///     
        /// </summary>
        /// <param name="?"></param>
        private void GenerateMacroblockForDivergingNodes(List<AssociativeGraph.GraphNode> programSnapshot, ref int macroblockID)
        {
            foreach (AssociativeGraph.GraphNode graphNode in programSnapshot)
            {
                if (!graphNode.isActive)
                {
                    continue;
                }

                if (graphNode.Visited)
                {
                    continue;
                }

                // graphNode.ChildrenNodes are the graphnodes to execute downstream
                // Where:
                //      a = 1
                //      b = a
                //      c = a
                //
                // The children of 'a = 1' are:
                //      b = a
                //      c = a
                if (graphNode.ChildrenNodes.Count > 1)
                {
                    foreach (AssociativeGraph.GraphNode child in graphNode.ChildrenNodes)
                    {
                        if (child.Visited)
                        {
                            continue;
                        }

                        child.Visited = true;
                        CacheGraphnodeToMacroblock(child, macroblockID++, true);
                        BuildMacroblock(child, programSnapshot);
                    }
                }
            }
        }

        /// <summary>
        /// Generate macroblocks using the default method
        /// A macroblock starts with an input node by checking IsMacroblockEntryPoint
        /// The macroblock ID is set for each graphnode 
        /// </summary>
        /// <param name="programSnapshot"></param>
        private int GenerateDefaultMacroblocks(List<AssociativeGraph.GraphNode> programSnapshot)
        {
            Validity.Assert(programSnapshot != null);
            int macroblockID = 0;

            // First pass - Generate macroblocks for diverging nodes
            GenerateMacroblockForDivergingNodes(programSnapshot, ref macroblockID);

            // Second pass - Generate macroblocks for the rest of the unvisited nodes
            foreach (AssociativeGraph.GraphNode graphNode in programSnapshot)
            {
                if (!graphNode.isActive)
                {
                    continue;
                }

                if (graphNode.Visited)
                {
                    continue;
                }

                if (IsMacroblockEntryPoint(graphNode))
                {
                    graphNode.Visited = true;
                    CacheGraphnodeToMacroblock(graphNode, macroblockID++, true);
                    BuildMacroblock(graphNode, programSnapshot);
                }
            }
            return macroblockID;
        }

        /// <summary>
        /// Builds the macroblock grouping starting from the given currentNode
        /// Graphnodes are grouped into a macroblock by setting their macroblockID property
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="programSnapshot"></param>
        private void BuildMacroblock(AssociativeGraph.GraphNode currentNode, List<AssociativeGraph.GraphNode> programSnapshot)
        {
            foreach (AssociativeGraph.GraphNode graphNode in programSnapshot)
            {
                AssociativeGraph.GraphNode depNode = null;
                if (graphNode.Visited)
                {
                    continue;
                }

                // Does graphNode node depend on currentNode and it is not an input node
                bool isInputNode = IsMacroblockEntryPoint(graphNode);
                if (!isInputNode && graphNode.DependsOn(currentNode.updateNodeRefList[0], ref depNode))
                {
                    graphNode.Visited = true;
                    CacheGraphnodeToMacroblock(graphNode, currentNode.MacroblockID, false);
                    BuildMacroblock(graphNode, programSnapshot);
                }
            }
        }

        /// <summary>
        /// Analyze the program snapshot and return the optimal macroblock generator type
        /// </summary>
        /// <param name="programSnapshot"></param>
        /// <returns></returns>
        private MacroblockGeneratorType GetMacroblockTypeFromSnapshot(List<AssociativeGraph.GraphNode> programSnapshot)
        {
            // Perform analysis of program snapshot
            // Extend this implementation to support static analyzers of the snapshot
            return MacroblockGeneratorType.Default;
        }

        /// <summary>
        /// Analyze the program snapshot and generate the optimal macroblock
        /// </summary>
        /// <param name="programSnapshot"></param>
        private int GenerateMacroblocksFromProgramSnapshot(List<AssociativeGraph.GraphNode> programSnapshot)
        {
            int generatedBlocks = Constants.kInvalidIndex;

            MacroblockGeneratorType type = GetMacroblockTypeFromSnapshot(programSnapshot);
            if (type == MacroblockGeneratorType.Default)
            {
                generatedBlocks = GenerateDefaultMacroblocks(programSnapshot);
            }
            else
            {
                throw new NotImplementedException();
            }
            return generatedBlocks;
        }

        private void CacheGraphnodeToMacroblock(AssociativeGraph.GraphNode graphNode, int macroblockID, bool isInputNode)
        {
            graphNode.MacroblockID = macroblockID;

            if (RuntimeMacroBlockList.Count <= macroblockID)
            {
                // This is a new macroblock ID, allocate space for it
                Runtime.MacroBlock newMacroblock = new Runtime.MacroBlock(macroblockID);
                RuntimeMacroBlockList.Add(newMacroblock);
            }

            RuntimeMacroBlockList[macroblockID].GraphNodeList.Add(graphNode);
            if (isInputNode)
            {
                RuntimeMacroBlockList[macroblockID].InputGraphNode = graphNode;
            }
        }


        /// <summary>
        /// Generates the macroblock groupings of the given list of graphnodes (the program snapshot)
        /// </summary>
        /// <param name="programSnapshot"></param>
        /// <returns></returns>
        public List<ProtoCore.Runtime.MacroBlock> GenerateMacroblocks(List<AssociativeGraph.GraphNode> programSnapshot)
        {
            RuntimeMacroBlockList = new List<Runtime.MacroBlock>();

            // Reset the graphnode states
            foreach (AssociativeGraph.GraphNode graphnode in programSnapshot)
            {
                graphnode.Visited = false;
            }

            GenerateMacroblocksFromProgramSnapshot(programSnapshot);

            return RuntimeMacroBlockList;
        }
    }
}