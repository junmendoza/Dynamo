using System;
using System.Text;
using System.Collections.Generic;
using ProtoCore.DSASM.Mirror;
using ProtoCore.Utils;
using ProtoCore.DSASM;

namespace ProtoCore.Runtime 
{
    public class MacroblockSequencer
    {
        private ProtoCore.DSASM.Executive executive = null;
        private List<ProtoCore.Runtime.MacroBlock> macroBlockList = null;

        public MacroblockSequencer()
        {
            macroBlockList = new List<Runtime.MacroBlock>();
        }

        public void Setup(
            ProtoCore.DSASM.Executive exec, 
            int exeblock, 
            int entry, 
            StackFrame stackFrame, int locals = 0)
        {
            executive = exec;
            executive.SetupBounce(exeblock, entry, stackFrame, locals);
        }

        /// <summary>
        /// Begin excution of macroblocks
        /// </summary>
        public void Execute(
            ProtoCore.DSASM.Executive exec,
            int exeblock,
            int entry,
            StackFrame stackFrame, int locals = 0)
        {
            Validity.Assert(exec != null);

            macroBlockList = exec.exe.MacroBlockList;

            List<ProtoCore.Runtime.MacroBlock> validBlocks = GetExecutingBlocks(macroBlockList);
            if (validBlocks.Count == 0)
            {
                return;
            }

            Setup(exec, exeblock, entry, stackFrame, locals);

            foreach (ProtoCore.Runtime.MacroBlock macroBlock in validBlocks)
            {
                executive.Execute(macroBlock);
            }
        }

        /// <summary>
        /// Get all macroblocks that can be executed
        /// </summary>
        /// <returns></returns>
        private List<ProtoCore.Runtime.MacroBlock> GetExecutingBlocks(List<ProtoCore.Runtime.MacroBlock> macroBlocks)
        {
            Validity.Assert(macroBlocks != null);
            List<ProtoCore.Runtime.MacroBlock> validBlocks = new List<Runtime.MacroBlock>();
            foreach (ProtoCore.Runtime.MacroBlock block in macroBlocks)
            {
                if (IsBlockReady(block))
                {
                    validBlocks.Add(block);
                }
            }
            return validBlocks;
        }

        /// <summary>
        /// Determines if a block is ready for execution
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private bool IsBlockReady(ProtoCore.Runtime.MacroBlock block)
        {
            return true;
        }
    }
}

 