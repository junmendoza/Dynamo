using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DSTestCaseUtils
{
    class TestCaseUtils
    {
        public static string AssemblyFilePath
        {
            get
            {
                return @"C:\Users\qing\Documents\github\Dynamo\bin\AnyCPU\Debug\ProtoTest.dll";
            }
        }


        //TODO: delete
        public static string ProjPath
        {
            get
            {
                return @"C:\Users\qing\Documents\github\Dynamo\test\Engine\ProtoTest\ProtoTest.csproj";
            }
        }

        public static string SolutionPath
        {
            get
            {
                // Roslyn not working with VS2013 solution 
                return @"C:\Users\qing\Documents\github\Dynamo\src\Dynamo.All.2012.sln";
            }
        }

        public static IProject ProtoTestProj { get; set; }

        public static bool IsTestCase(MethodInfo method)
        {
            foreach (var attribute in method.GetCustomAttributes(true))
            {
                if (attribute.ToString() == "NUnit.Framework.TestAttribute")
                {
                    return true;
                }
            }
            return false;
        }



        private static string SearchAndReplaceMethodsForTextCSharp(IDocument document, string textToSearch)
        {
            StringBuilder result = new StringBuilder();

            CommonSyntaxTree syntax = document.GetSyntaxTree();
            var root = (Roslyn.Compilers.CSharp.CompilationUnitSyntax)syntax.GetRoot();

            var syntaxNodes = from methodDeclaration in root.DescendantNodes()
                                  .Where(x => x is MethodDeclarationSyntax)
                              select methodDeclaration;

            if (syntaxNodes != null && syntaxNodes.Count() > 0)
            {
                foreach (MemberDeclarationSyntax method in syntaxNodes)
                {
                    if (method != null)
                    {
                        string methodText = method.GetText().ToString();
                        if (methodText.ToUpper().Contains(textToSearch.ToUpper()))
                        {
                            result.Append(ReplaceMethodTextCSharp(method, document, textToSearch.ToUpper()));
                        }
                    }
                }
            }

            return result.ToString();
        }

        private static string ReplaceMethodTextCSharp(SyntaxNode node, IDocument document, string textToSearch)
        {
            StringBuilder resultStringBuilder = new StringBuilder();

            string methodText = node.GetText().ToString();
            bool isMethod = node is MethodDeclarationSyntax;
            string methodOrPropertyDefinition = isMethod ? "Method: " : " Invalid - not Method ";

            object methodName = ((MethodDeclarationSyntax)node).Identifier.Value;
            SyntaxList<StatementSyntax> newStatements;
            foreach(var statement in (node as MethodDeclarationSyntax).Body.Statements)
            {
                string stmtString = statement.ToString();
                if(stmtString.ToUpper().Contains(textToSearch))
                {

                    stmtString.Replace(textToSearch, "thisTest.GenericVerify");
                    StatementSyntax newStatement = new StatementSyntax(stmtString);
                }
            }

            resultStringBuilder.AppendLine("//[test case]");
            resultStringBuilder.AppendLine(document.FilePath);
            resultStringBuilder.AppendLine(methodOrPropertyDefinition + (string)methodName);
            resultStringBuilder.AppendLine(methodText);

            return resultStringBuilder.ToString();
        }



        internal static void ReplaceBasic()
        {
            IProject proj = ProtoTestProj;
            foreach (var doc in proj.Documents)
            {
                string result = SearchAndReplaceMethodsForTextCSharp(doc, "thisTest.Verify");
                //Console.WriteLine(result);
            }

        }

        internal static void InitProcess()
        {
            ProtoTestProj = GetProject();
        }

        private static IProject GetProject()
        {
            try
            {
                var w = Workspace.LoadSolution(SolutionPath, "Debug","AnyCPU", true);
                //var w = Workspace.LoadStandAloneProject(ProjPath);
                return w.CurrentSolution.Projects.Where(proj => proj.Name == "ProtoTest").First();

                //var solution = Solution.Load(SolutionPath);
                //return solution.Projects.Where(proj => proj.Name == "ProtoTest").First();


            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        //private static ISolution GetSolution()
        //{

        //    IWorkspace workspace = Workspace.LoadSolution(SlnPath);
        //    ISolution sln = workspace.CurrentSolution;
        //    return sln;
        //}

        
    }
}
