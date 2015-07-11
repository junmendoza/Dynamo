using System;
using System.Text;
using System.Collections.Generic;
using ProtoCore.Utils;
using ProtoCore.DSASM;

namespace ProtoCore.Runtime 
{
    public class MacroblockSequencer
    {
        private List<ProtoCore.Runtime.MacroBlock> macroBlockList = null;

        public MacroblockSequencer(List<Runtime.MacroBlock> macroBlocks)
        {
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

            // Execute all macroblocks
            //foreach (ProtoCore.Runtime.MacroBlock macroBlock in macroBlockList)
            //{
            //    executive.Execute(macroBlock);
            //}

            int i = 0;
            int numblocks = macroBlockList.Count;
            while(i < numblocks)
            {
                ProtoCore.Runtime.MacroBlock macroBlock = macroBlockList[i];
                UpdateMacroblockState(ref macroBlock);

                if (macroBlock.State == MacroBlock.ExecuteState.Ready)
                {
                    executive.Execute(macroBlock);
                    ++i;
                }
            }
        }

        /// <summary>
        /// Updates the state of the macroblock by inspecting its graphnodes
        /// A macroblock is ready if all its inputs are executed
        /// </summary>
        /// <param name="block"></param>
        private void UpdateMacroblockState(ref ProtoCore.Runtime.MacroBlock block)
        {
            // Get the input graphnode
            AssociativeGraph.GraphNode inputNode = block.InputGraphNode;

            // Check if the input graphnodes have already executed
            // This is done by checking if the parent nodes of the input graphnode are clean
            bool inputNodesExecuted = true;
            foreach(AssociativeGraph.GraphNode parentNode in inputNode.ParentNodes)
            {
                if (parentNode.isDirty)
                {
                    // If at least one input is dirty, then it hasnt been executed yet
                    inputNodesExecuted = false;
                    break;
                }
            }

            if (inputNodesExecuted)
            {
                block.State = MacroBlock.ExecuteState.Ready;
            }
            else
            {
                block.State = MacroBlock.ExecuteState.NotReady;
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

 