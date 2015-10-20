using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

using ProtoCore;
using ProtoCore.Runtime;
using ProtoCore.AssociativeGraph;
using ProtoScript.Runners;
namespace ProtoTest.Macroblocks
{
    class MacroblockGeneratorTests : ProtoTestBase
    {
        private List<MacroBlock> generatedMacroblocks = null;
        public override void Setup()
        {
            // Compile core
            core = new Core(new Options());
            core.Compilers.Add(ProtoCore.Language.kAssociative, new ProtoAssociative.Compiler(core));
            core.Compilers.Add(ProtoCore.Language.kImperative, new ProtoImperative.Compiler(core));
        }

        [Test]
        public void TestSequential01()
        {
            const string code =
                @"
    a = 1;
";
            CompileAndGenerateMacroblocks(code);

            // Verify
            VerifyMacroblockCount(1);
            VerifyGraphNodesInMacroblock(0, 1);
        }

        [Test]
        public void TestSequential02()
        {
            const string code =
                @"
    a = 1;
    b = 2;
    c = 3;
    d = 4;
";
            CompileAndGenerateMacroblocks(code);

            // Verify
            VerifyMacroblockCount(4);
            VerifyGraphNodesInMacroblock(0, 1);
            VerifyGraphNodesInMacroblock(1, 1);
            VerifyGraphNodesInMacroblock(2, 1);
            VerifyGraphNodesInMacroblock(3, 1);
        }

        [Test]
        public void TestSequential03()
        {
            const string code =
                @"
    a = 1;
    b = a;
";
            CompileAndGenerateMacroblocks(code);

            // Verify
            VerifyMacroblockCount(1);
            VerifyGraphNodesInMacroblock(0, 3);
        }

        [Test]
        public void TestParallel01()
        {
            const string code = 
@"
    a = 1;

    b = 2;
";
            CompileAndGenerateMacroblocks(code);

            // Verify
            VerifyMacroblockCount(2);
            VerifyGraphNodesInMacroblock(0, 1);
            VerifyGraphNodesInMacroblock(1, 1);
        }

        [Test]
        public void TestParallel02()
        {
            const string code =
@"
    a = 1;

    b = 2;
    c = b;
";
            CompileAndGenerateMacroblocks(code);

            // Verify
            VerifyMacroblockCount(2);
            VerifyGraphNodesInMacroblock(0, 1);
            VerifyGraphNodesInMacroblock(1, 3);
        }


        [Test]
        public void TestParallel03()
        {
            const string code =
                @"
    a = 1;
    b = a;

    c = 1;
    d = c;

    e = 1;
    f = e;

    g = 1;
    h = g;
";
            CompileAndGenerateMacroblocks(code);

            // Verify
            VerifyMacroblockCount(4);
            VerifyGraphNodesInMacroblock(0, 3);
            VerifyGraphNodesInMacroblock(1, 3);
            VerifyGraphNodesInMacroblock(2, 3);
            VerifyGraphNodesInMacroblock(3, 3);
        }

        [Test]
        public void TestParallel04()
        {
            const string code =
                @"
a = 10;b = a;c = a;
";
            CompileAndGenerateMacroblocks(code);

            // Verify
            VerifyMacroblockCount(3);
            VerifyGraphNodesInMacroblock(0, 2);
            VerifyGraphNodesInMacroblock(1, 2);
            VerifyGraphNodesInMacroblock(2, 1);
        }

        private void CompileAndGenerateMacroblocks(string code)
        {
            // Compile
            ProtoScriptRunner runner = new ProtoScriptRunner();
            bool compileSucceeded = runner.CompileAndGenerateExe(code, core, new ProtoCore.CompileTime.Context());
            Assert.IsTrue(compileSucceeded == true);

            Assert.NotNull(core.DSExecutable);
            Assert.NotNull(core.DSExecutable.MacroBlockList);

            // Get the generated macroblocks
            generatedMacroblocks = core.DSExecutable.MacroBlockList;
        }

        private void VerifyMacroblockCount(int generatedBlocks)
        {
            Assert.AreEqual(generatedMacroblocks.Count, generatedBlocks);
        }

        private void VerifyGraphNodesInMacroblock(int macroblockID, int graphNodesInMacroblock)
        {
            Assert.NotNull(generatedMacroblocks[macroblockID]);
            Assert.AreEqual(generatedMacroblocks[macroblockID].GraphNodeList.Count, graphNodesInMacroblock);
        }
    }
}