//#define BENCHMARK
#define SERIAL
//#define PARAL
using System;
using System.Text;
using System.Collections.Generic;
using ProtoCore.DSASM.Mirror;
using ProtoCore.Utils;
using ProtoCore.DSASM;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ProtoCore.Runtime 
{
    public class MacroblockSequencer
    {
        private List<ProtoCore.Runtime.MacroBlock> macroBlockList = null;

        public MacroblockSequencer(List<Runtime.MacroBlock> macroBlocks)
        {
            macroBlockList = macroBlocks;
        }

        private void SetupExecutive(
            ProtoCore.DSASM.Executive executive,
            int exeblock, 
            int entry, 
            ProtoCore.DSASM.StackFrame stackFrame, int locals = 0)
        {
            //executive.SetupBounce(exeblock, entry, stackFrame, locals);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Begin excution of macroblocks
        /// </summary>
        public void Execute(
            ProtoCore.DSASM.Executive executive,
            int exeblock,
            int entry,
            ProtoCore.DSASM.StackFrame stackFrame, 
            int locals = 0
            )
        {
            Validity.Assert(executive != null);
            Validity.Assert(macroBlockList != null);

            // Get the list of macroblocks that will be executed in the current run
            List<ProtoCore.Runtime.MacroBlock> validBlocks = GetExecutingBlocks(macroBlockList);
            if (validBlocks.Count == 0)
            {
                return;
            }
#if BENCHMARK
            Stopwatch watch = new Stopwatch();
            string filePath = "c:\\benchMark.txt";
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                string description = "\n===== Benchmarking =====\n";
                sw.WriteLine(description);
                // clean up
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                DateTime now = DateTime.Now;
                sw.WriteLine(now);
                sw.WriteLine("Number of cores : {0}", Environment.ProcessorCount);
                watch.Start();
            
#endif
#if SERIAL
                foreach (ProtoCore.Runtime.MacroBlock macroBlock in validBlocks)
                {
                    executive.SetupBounce(exeblock, entry, stackFrame, locals);
                    executive.Execute(macroBlock);
                }
#endif

#if PARAL
                Parallel.ForEach(validBlocks, currentBlk =>
                    {
                        executive.SetupBounce(exeblock, entry, stackFrame, locals);
                        executive.Execute(currentBlk);
                    });
#endif

#if BENCHMARK
                watch.Stop();
                sw.WriteLine("Time Elapsed {0} ms\n", watch.Elapsed.TotalMilliseconds);
            }
            
#endif
        


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

 