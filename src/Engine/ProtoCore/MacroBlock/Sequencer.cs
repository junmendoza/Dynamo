using System;
using System.Text;
using System.Collections.Generic;
using ProtoCore.Utils;
using ProtoCore.DSASM;

namespace ProtoCore.Runtime 
{
    public class MacroblockSequencer
    {
        private RuntimeCore runtimeCore = null;
        private List<ProtoCore.Runtime.MacroBlock> macroBlockList = null;

        public MacroblockSequencer(List<Runtime.MacroBlock> macroBlocks, RuntimeCore runtimeCore)
        {
            this.runtimeCore = runtimeCore;
            macroBlockList = macroBlocks;
        }

        /// <summary>
        /// Begin excution of macroblocks
        /// </summary>
        public void Execute(
            ProtoCore.DSASM.Executive executive,
            int exeblock,
            int entry,
            StackFrame stackFrame, 
            int locals = 0
            )
        {
            Validity.Assert(executive != null);
            Validity.Assert(macroBlockList != null);

            // Setup the executive prior to execution
            executive.SetupBounce(exeblock, entry, stackFrame, locals);

            int i = 0;
            int executedNodes = 0;
            int blockCount = macroBlockList.Count;

            // A simple heuristic to make sure the sequencer does not go into an infinit loop
            int iterNum = 0;
            int threshold = blockCount * 10;

            while (executedNodes < blockCount)
            {
                ProtoCore.Runtime.MacroBlock macroBlock = macroBlockList[i];
                UpdateMacroblockState(ref macroBlock);
                if (IsBlockReady(macroBlock))
                {
                    executive.Execute(macroBlock);
                    macroBlock.State = MacroBlock.ExecuteState.Done;
                    executedNodes++;
                }

                // Go to the next macroblock index
                // Reset to 0 if 'i' is the last one
                i = (i < blockCount - 1) ? i + 1 : 0;
                if (iterNum++ >= threshold)
                {
                    LogWarningNotAllExecuted(macroBlockList);
                    break;
                }
            }

            // Reset after execution
            ResetMacroblocksToReady(macroBlockList);
        }

        /// <summary>
        /// Logs a warning that lists which macroblocks did not execute
        /// </summary>
        /// <param name="macroBlockList"></param>
        private void LogWarningNotAllExecuted(List<ProtoCore.Runtime.MacroBlock> macroBlockList)
        {
            // Get all macroblocks that did not execute
            StringBuilder blocksNotExecuted = new StringBuilder();
            foreach (MacroBlock block in macroBlockList)
            {
                if (block.State == MacroBlock.ExecuteState.Ready)
                {
                    blocksNotExecuted.Append(block.UID.ToString());
                    blocksNotExecuted.Append(" ");
                }
            }
            runtimeCore.RuntimeStatus.LogWarning(
                WarningID.kSequencerError,
                string.Format("Sequencer could not execute macroblocks: {0}", blocksNotExecuted.ToString()));
        }

        /// <summary>
        /// Update the macroblock state by inspecting the input graphnode
        /// </summary>
        /// <param name="macroBlock"></param>
        private void UpdateMacroblockState(ref Runtime.MacroBlock macroBlock)
        {
            if (macroBlock.State == MacroBlock.ExecuteState.Done)
            {
                return;
            }

            // Check if the node is a direct input.
            AssociativeGraph.GraphNode inputNode = macroBlock.InputGraphNode;

            // A direct input is a node that is not dependent on any other node value, such as a constant assignment
            //      a = 1 <- this is a directinput node
            bool isDirectInput = inputNode.ParentNodes.Count == 0;
            if (!isDirectInput)
            {
                // The nodes operands (parentnodes) must be checked if the are dirty.
                // If at least one operand is dirty, then it means it hasnt executed yet and the node is not ready 
                //      c = a + b <- This is an input node and we must check if 'a' and 'b' have been executed
                foreach (AssociativeGraph.GraphNode parent in inputNode.ParentNodes)
                {
                    if (parent.isDirty)
                    {
                        macroBlock.State = MacroBlock.ExecuteState.NotReady;
                        return;
                    }
                }
            }
            macroBlock.State = MacroBlock.ExecuteState.Ready;
        }

        /// <summary>
        /// Reset the macroblocks into its ready state
        /// </summary>
        /// <param name="macroBlockList"></param>
        private void ResetMacroblocksToReady(List<ProtoCore.Runtime.MacroBlock> macroBlockList)
        {
            Validity.Assert(macroBlockList != null);
            foreach(ProtoCore.Runtime.MacroBlock block in macroBlockList)
            {
                block.State = MacroBlock.ExecuteState.Ready;
            }
        }

        /// <summary>
        /// Determines if a block is ready for execution
        /// A block is ready if all its operands have executed
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private bool IsBlockReady(ProtoCore.Runtime.MacroBlock block)
        {
            Validity.Assert(macroBlockList != null);
            return block.State == MacroBlock.ExecuteState.Ready;
        }
    }
}

 